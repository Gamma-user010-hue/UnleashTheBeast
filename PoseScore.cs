using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PoseScorer : MonoBehaviour
{
    // --- Configuration ---
    
    [Header("Avatars")]
    [Tooltip("The avatar receiving live data from MediaPipe.")]
    public GameObject LiveAvatar; 

    [Tooltip("The pre-animated avatar with the correct motion (The instructor/reference model).")]
    public GameObject ReferenceAvatar; 

    [Header("Scoring Parameters")]
    // Increased to 45f to give the user more room for minor errors when matching the pose, allowing scores to reach 80-100%.
    [Tooltip("The maximum angular difference (in degrees) allowed before the score hits 0. This controls the sensitivity.")]
    public float MaxAngleDeviation = 45f; 

    [Tooltip("If true, leg bones are excluded from the score. Useful for upper-body tracking where legs are noisy.")]
    public bool IgnoreLegs = true; // Default to TRUE to fix the jittery leg issue

    [Header("Weighted Scoring")]
    // Increased to 10.0f to heavily prioritize the accuracy of actively moving limbs.
    [Tooltip("The weight applied to bones actively moving in the reference animation.")]
    public float ActiveBoneWeight = 10.0f; 

    // Significantly decreased to 0.01f to virtually eliminate the impact of noise on static (non-moving) bones when idle.
    [Tooltip("The weight applied to bones that are stationary in the reference animation (to minimize noise impact).")]
    public float PassiveBoneWeight = 0.01f; 

    // Increased to 3.0f to ensure only intentional animation movement is counted as 'active'.
    [Tooltip("Angular movement threshold (degrees per frame) for a reference bone to be considered 'active'.")]
    public float MovementThresholdDegrees = 3.0f; 

    [Header("Scoring Output")]
    [Tooltip("The current accuracy score (0 to 100).")]
    [Range(0, 100)]
    public float AccuracyScore = 0f;

    [Tooltip("Root Mean Square deviation (punishes large errors more than average).")]
    public float RMSDeviation = 0f;

    // --- Internal State ---
    private bool isMapped = false;
    
    private Dictionary<string, Transform> liveBones = new Dictionary<string, Transform>();
    private Dictionary<string, Transform> referenceBones = new Dictionary<string, Transform>();
    
    private Animator referenceAnimator;

    // State for Weighted Scoring
    private Dictionary<string, Quaternion> previousRefRotations = new Dictionary<string, Quaternion>();
    private Dictionary<string, float> currentBoneWeights = new Dictionary<string, float>();

    // New variables for Summary Scoring
    private float scoreAccumulator = 0f;
    private int scoreFrameCount = 0;
    
    // Timer variables for periodic logging (New in this version)
    private float scoringTimer = 0f;
    private const float SummaryIntervalSeconds = 10f; // Log a summary every 10 seconds


    // --- Bone Path Definitions ---
    // Includes paths for both the Live Avatar (with Armature prefix) and the Reference Avatar (without Armature prefix)
    public static readonly Dictionary<string, string[]> BonePathsToCompare = new Dictionary<string, string[]>
    {
        // Key: Bone Name for Logging/Grouping. Value: Array of possible full paths.
        {"Hips", new[] {"Armature/Hips", "Hips"}},
        {"Spine1", new[] {"Armature/Hips/Spine/Spine1", "Hips/Spine/Spine1"}},
        {"Spine2", new[] {"Armature/Hips/Spine/Spine1/Spine2", "Hips/Spine/Spine1/Spine2"}},
        
        // Left Arm
        {"LShoulder", new[] {"Armature/Hips/Spine/Spine1/Spine2/LeftShoulder", "Hips/Spine/Spine1/Spine2/LeftShoulder"}},
        {"LArm", new[] {"Armature/Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm", "Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm"}},
        {"LForeArm", new[] {"Armature/Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm", "Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm"}},
        
        // Right Arm
        {"RShoulder", new[] {"Armature/Hips/Spine/Spine1/Spine2/RightShoulder", "Hips/Spine/Spine1/Spine2/RightShoulder"}},
        {"RArm", new[] {"Armature/Hips/Spine/Spine1/Spine2/RightShoulder/RightArm", "Hips/Spine/Spine1/Spine2/RightShoulder/RightArm"}},
        {"RForeArm", new[] {"Armature/Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm", "Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm"}},
        
        // Left Leg (Excluded if IgnoreLegs is true)
        {"LUpLeg", new[] {"Armature/Hips/LeftUpLeg", "Hips/LeftUpLeg"}},
        {"LLeg", new[] {"Armature/Hips/LeftUpLeg/LeftLeg", "Hips/LeftUpLeg/LeftLeg"}}, 

        // Right Leg (Excluded if IgnoreLegs is true)
        {"RUpLeg", new[] {"Armature/Hips/RightUpLeg", "Hips/RightUpLeg"}},
        {"RLeg", new[] {"Armature/Hips/RightUpLeg/RightLeg", "Hips/RightUpLeg/RightLeg"}}, 
    };

    void Start()
    {
        if (LiveAvatar == null || ReferenceAvatar == null)
        {
            Debug.LogError("[PoseScorer] Live Avatar and/or Reference Avatar are not assigned.");
            enabled = false;
            return;
        }

        if (LiveAvatar == ReferenceAvatar)
        {
            Debug.LogError("[PoseScorer] LiveAvatar and ReferenceAvatar are the SAME GameObject! Please duplicate the avatar model in the scene.");
            enabled = false;
            return;
        }
        
        this.referenceAnimator = ReferenceAvatar.GetComponent<Animator>();
        if (this.referenceAnimator == null)
        {
            Debug.LogError("[PoseScorer] Reference Avatar missing Animator!");
            enabled = false;
            return;
        }

        var liveAnim = LiveAvatar.GetComponent<Animator>();
        if (liveAnim != null && liveAnim.enabled)
        {
            Debug.LogWarning("[PoseScorer] Warning: Live Avatar has an enabled Animator. This might conflict with the AvatarController script.");
        }

        Debug.Log("[PoseScorer] Initialized. Waiting for bones...");
    }

    private void MapBones(Transform root, Dictionary<string, Transform> map)
    {
        map.Clear();
        foreach(var kvp in BonePathsToCompare)
        {
            string boneKey = kvp.Key;
            string[] possiblePaths = kvp.Value;

            // Skip legs if the option is checked
            if (IgnoreLegs && (boneKey.Contains("Leg") || boneKey.Contains("UpLeg"))) continue;
            
            Transform foundBone = null;
            // Check all possible paths for this bone
            foreach (string path in possiblePaths)
            {
                foundBone = root.Find(path);
                if (foundBone != null) break;
            }

            if (foundBone != null) 
            {
                // Store the bone using the simplified Bone Key (e.g., "Hips", "LArm")
                map.Add(boneKey, foundBone);
            }
        }
    }

    void Update()
    {
        if (!isMapped)
        {
            MapBones(LiveAvatar.transform, liveBones);
            MapBones(ReferenceAvatar.transform, referenceBones);
            
            if (liveBones.Count > 0 && referenceBones.Count > 0)
            {
                // Check if the number of mapped bones is consistent between Live and Reference
                if (liveBones.Count != referenceBones.Count)
                {
                    Debug.LogWarning($"[PoseScorer] Mismatch in mapped bone count! Live: {liveBones.Count}, Reference: {referenceBones.Count}. Scoring may be inaccurate.");
                }
                
                isMapped = true;
                
                string mappedBoneNames = string.Join(", ", liveBones.Keys);
                Debug.Log($"[PoseScorer] SUCCESSFULLY MAPPED {liveBones.Count} BONES: {mappedBoneNames}");
            }
            return;
        }
        
        // --- 1. Perform Scoring and Tracking ---
        // Scoring runs every frame the PoseScorer component is active.
        CalculateBoneWeights();
        CalculateRMSScore(); // Accumulates scores into scoreAccumulator/scoreFrameCount

        // --- 2. Update Timer ---
        scoringTimer += Time.deltaTime;

        // --- 3. Timer Check: Log Summary if 10 seconds have passed ---
        if (scoringTimer >= SummaryIntervalSeconds)
        {
            if (scoreFrameCount > 0)
            {
                float averageScore = scoreAccumulator / scoreFrameCount;
                string starRating = GetStarRatingString(averageScore); 
                
                // Log the summary for the completed 10-second interval, including the star rating
                Debug.Log($"[Score] Score: {averageScore:F1}% Stars: ({starRating})");
            }
            else
            {
                // This happens if the user was AFK/untracked for the entire interval
                Debug.Log("[Score] 10 Second Interval passed, but no scoring data was recorded.");
            }

            // Reset timer and accumulators for the next interval
            scoringTimer = 0f;
            scoreAccumulator = 0f;
            scoreFrameCount = 0;
        }
    }

    // NEW: Helper function to determine and return the star rating string based on the average score.
    private string GetStarRatingString(float score)
    {
        int starCount = 0;

        if (score >= 68f)
        {
            starCount = 5;
        }
        else if (score >= 62f)
        {
            starCount = 4;
        }
        else if (score >= 55f)
        {
            starCount = 3;
        }
        else if (score >= 40f)
        {
            starCount = 2;
        }
        else // score < 40f
        {
            starCount = 1;
        }

        // Return a string composed of the appropriate number of star characters
        return new string('*', starCount);
    }

    private void CalculateBoneWeights()
    {
        // Iterate over all reference bones to determine if they are active (moving)
        foreach (var refBoneEntry in referenceBones)
        {
            string boneKey = refBoneEntry.Key;
            Transform refBone = refBoneEntry.Value;

            if (previousRefRotations.TryGetValue(boneKey, out Quaternion previousRot))
            {
                // Calculate angular movement (velocity) since the last frame
                float angularDelta = Quaternion.Angle(refBone.rotation, previousRot);
                
                // If the bone has moved more than the threshold, assign the active weight
                if (angularDelta > MovementThresholdDegrees)
                {
                    currentBoneWeights[boneKey] = ActiveBoneWeight;
                }
                else
                {
                    currentBoneWeights[boneKey] = PassiveBoneWeight;
                }
            }
            else
            {
                // Initialize the weight and previous rotation for the first frame
                currentBoneWeights[boneKey] = PassiveBoneWeight;
            }

            // Update previous rotation for the next frame's calculation
            previousRefRotations[boneKey] = refBone.rotation;
        }
    }

    private void CalculateRMSScore()
    {
        float sumWeightedSquaredError = 0f;
        float totalWeight = 0f; // Sum of all applied weights
        
        string worstBoneName = "None";
        float worstBoneError = -1f;

        // --- Step 1: Establish World-Relative Alignment ---
        // We use the Hips bone's World Rotation to normalize the two avatar spaces.
        Quaternion refHipsInverseWorld = Quaternion.identity;
        
        if (liveBones.TryGetValue("Hips", out Transform liveHips) && 
            referenceBones.TryGetValue("Hips", out Transform referenceHips))
        {
            // Get the inverse world rotation of the reference hips. 
            // This transforms all bone rotations into the reference hips' local frame.
            refHipsInverseWorld = Quaternion.Inverse(referenceHips.rotation);
        }

        // --- Step 2: Calculate Weighted RMS Error ---
        foreach (var liveBoneEntry in liveBones)
        {
            string boneKey = liveBoneEntry.Key;
            Transform liveBone = liveBoneEntry.Value;

            if (referenceBones.TryGetValue(boneKey, out Transform refBone) && 
                currentBoneWeights.TryGetValue(boneKey, out float weight))
            {
                Quaternion liveRotRelative;
                Quaternion refRotRelative;
                
                // Transform both Live and Reference bone world rotations into the Reference Hips' local space.
                liveRotRelative = refHipsInverseWorld * liveBone.rotation;
                refRotRelative = refHipsInverseWorld * refBone.rotation;
                
                // 1. Calculate Pose Error (Angular difference)
                float poseError = Quaternion.Angle(liveRotRelative, refRotRelative);
                
                // Track the worst bone for debugging
                if (poseError > worstBoneError)
                {
                    worstBoneError = poseError;
                    worstBoneName = boneKey; // Use the simplified key name
                }

                // 2. Accumulate Weighted Squared Error (RMS Method)
                sumWeightedSquaredError += (weight * poseError * poseError);
                totalWeight += weight;
            }
        }
        
        if (totalWeight == 0f) return;

        // 3. Calculate Weighted Root Mean Square
        RMSDeviation = Mathf.Sqrt(sumWeightedSquaredError / totalWeight);

        // 4. Convert to Score
        float normalizedError = Mathf.Clamp01(RMSDeviation / MaxAngleDeviation);
        AccuracyScore = (1f - normalizedError) * 100f;
        
        // Accumulate score for summary calculation
        scoreAccumulator += AccuracyScore;
        scoreFrameCount++;
    }
}
// 55 - 3 stars
// 62 - 4 stars
// 68+ - 5 stars