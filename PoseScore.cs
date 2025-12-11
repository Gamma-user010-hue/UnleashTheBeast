using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class PoseScorer : MonoBehaviour
{
    // --- Configuration ---
    
    [Header("Avatars")]
    [Tooltip("The avatar receiving live data from MediaPipe.")]
    public GameObject LiveAvatar; 

    [Tooltip("The pre-animated avatar with the correct motion (The instructor/reference model).")]
    public GameObject ReferenceAvatar; 

    [Header("Scoring Parameters")]
    [Tooltip("The maximum angular difference (in degrees) allowed before the score hits 0. This controls the sensitivity.")]
    public float MaxAngleDeviation = 45f; 

    [Tooltip("If true, leg bones are excluded from the score. Useful for upper-body tracking where legs are noisy.")]
    public bool IgnoreLegs = true;

    [Header("Weighted Scoring")]
    [Tooltip("The weight applied to bones actively moving in the reference animation.")]
    public float ActiveBoneWeight = 10.0f; 

    [Tooltip("The weight applied to bones that are stationary in the reference animation (to minimize noise impact).")]
    public float PassiveBoneWeight = 0.01f; 

    [Tooltip("Angular movement threshold (degrees per frame) for a reference bone to be considered 'active'.")]
    public float MovementThresholdDegrees = 3.0f; 

    [Header("Scoring Output")]
    [Tooltip("The current accuracy score (0 to 100).")]
    [Range(0, 100)]
    public float AccuracyScore = 0f;

    [Tooltip("Root Mean Square deviation (punishes large errors more than average).")]
    public float RMSDeviation = 0f;

    [Tooltip("The average score over the current interval.")]
    public float AverageScore = 0f;

    [Header("Text Display")]
    [Tooltip("TextMeshPro object to display the score")]
    public TextMeshProUGUI scoreText;

    [Tooltip("Interval in seconds to display the score")]
    public float displayInterval = 7f;

    [Tooltip("Duration in seconds to keep the text visible")]
    public float displayDuration = 2f;

    [Tooltip("The Animator on the Reference Avatar")]
    public Animator referenceAnimatorForDisplay;

    [Tooltip("Name of the default/idle animation to skip scoring display for")]
    public string defaultAnimationName = "Idle";

    // --- Internal State ---
    private bool isMapped = false;
    
    private Dictionary<string, Transform> liveBones = new Dictionary<string, Transform>();
    private Dictionary<string, Transform> referenceBones = new Dictionary<string, Transform>();
    
    private Animator referenceAnimator;

    private Dictionary<string, Quaternion> previousRefRotations = new Dictionary<string, Quaternion>();
    private Dictionary<string, float> currentBoneWeights = new Dictionary<string, float>();

    private float scoreAccumulator = 0f;
    private int scoreFrameCount = 0;
    
    private float scoringTimer = 0f;
    private const float SummaryIntervalSeconds = 10f;

    // Text display variables
    private float displayTimer = 0f;
    private float visibilityTimer = 0f;
    private bool isTextVisible = false;

    // --- Bone Path Definitions ---
    public static readonly Dictionary<string, string[]> BonePathsToCompare = new Dictionary<string, string[]>
    {
        {"Hips", new[] {"Armature/Hips", "Hips"}},
        {"Spine1", new[] {"Armature/Hips/Spine/Spine1", "Hips/Spine/Spine1"}},
        {"Spine2", new[] {"Armature/Hips/Spine/Spine1/Spine2", "Hips/Spine/Spine1/Spine2"}},
        
        {"LShoulder", new[] {"Armature/Hips/Spine/Spine1/Spine2/LeftShoulder", "Hips/Spine/Spine1/Spine2/LeftShoulder"}},
        {"LArm", new[] {"Armature/Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm", "Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm"}},
        {"LForeArm", new[] {"Armature/Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm", "Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm"}},
        
        {"RShoulder", new[] {"Armature/Hips/Spine/Spine1/Spine2/RightShoulder", "Hips/Spine/Spine1/Spine2/RightShoulder"}},
        {"RArm", new[] {"Armature/Hips/Spine/Spine1/Spine2/RightShoulder/RightArm", "Hips/Spine/Spine1/Spine2/RightShoulder/RightArm"}},
        {"RForeArm", new[] {"Armature/Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm", "Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm"}},
        
        {"LUpLeg", new[] {"Armature/Hips/LeftUpLeg", "Hips/LeftUpLeg"}},
        {"LLeg", new[] {"Armature/Hips/LeftUpLeg/LeftLeg", "Hips/LeftUpLeg/LeftLeg"}}, 

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

        // Initialize text display
        if (scoreText != null)
        {
            scoreText.enabled = false;
            isTextVisible = false;
        }
        else
        {
            Debug.LogWarning("[PoseScorer] scoreText TextMeshProUGUI is not assigned!");
        }

        if (referenceAnimatorForDisplay == null)
        {
            referenceAnimatorForDisplay = ReferenceAvatar.GetComponent<Animator>();
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

            if (IgnoreLegs && (boneKey.Contains("Leg") || boneKey.Contains("UpLeg"))) continue;
            
            Transform foundBone = null;
            foreach (string path in possiblePaths)
            {
                foundBone = root.Find(path);
                if (foundBone != null) break;
            }

            if (foundBone != null) 
            {
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
        
        // Perform Scoring
        CalculateBoneWeights();
        CalculateRMSScore();

        // Update console logging timer
        scoringTimer += Time.deltaTime;
        if (scoringTimer >= SummaryIntervalSeconds)
        {
            if (scoreFrameCount > 0)
            {
                AverageScore = scoreAccumulator / scoreFrameCount;
                string starRating = GetStarRatingString(AverageScore); 
                Debug.Log($"[Score] Score: {AverageScore:F1}% Stars: ({starRating})");
            }
            else
            {
                Debug.Log("[Score] 10 Second Interval passed, but no scoring data was recorded.");
                AverageScore = 0f;
            }

            scoringTimer = 0f;
            scoreAccumulator = 0f;
            scoreFrameCount = 0;
        }

        // Update text display timer
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText == null) return;

        // Check if the default animation is currently playing
        if (referenceAnimatorForDisplay != null)
        {
            AnimatorStateInfo stateInfo = referenceAnimatorForDisplay.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(defaultAnimationName))
            {
                // Default animation is playing, don't show score
                if (isTextVisible)
                {
                    scoreText.enabled = false;
                    isTextVisible = false;
                }
                return;
            }
        }

        displayTimer += Time.deltaTime;

        // Check if it's time to show the score
        if (displayTimer >= displayInterval)
        {
            // Display the score
            scoreText.text = $"Score: {AccuracyScore:F0}";
            scoreText.enabled = true;
            isTextVisible = true;
            visibilityTimer = 0f;
            displayTimer = 0f;
        }

        // If text is visible, count down the visibility duration
        if (isTextVisible)
        {
            visibilityTimer += Time.deltaTime;

            if (visibilityTimer >= displayDuration)
            {
                scoreText.enabled = false;
                isTextVisible = false;
            }
        }
    }

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
        else
        {
            starCount = 1;
        }

        return new string('*', starCount);
    }

    private void CalculateBoneWeights()
    {
        foreach (var refBoneEntry in referenceBones)
        {
            string boneKey = refBoneEntry.Key;
            Transform refBone = refBoneEntry.Value;

            if (previousRefRotations.TryGetValue(boneKey, out Quaternion previousRot))
            {
                float angularDelta = Quaternion.Angle(refBone.rotation, previousRot);
                
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
                currentBoneWeights[boneKey] = PassiveBoneWeight;
            }

            previousRefRotations[boneKey] = refBone.rotation;
        }
    }

    private void CalculateRMSScore()
    {
        float sumWeightedSquaredError = 0f;
        float totalWeight = 0f;
        
        string worstBoneName = "None";
        float worstBoneError = -1f;

        Quaternion refHipsInverseWorld = Quaternion.identity;
        
        if (liveBones.TryGetValue("Hips", out Transform liveHips) && 
            referenceBones.TryGetValue("Hips", out Transform referenceHips))
        {
            refHipsInverseWorld = Quaternion.Inverse(referenceHips.rotation);
        }

        foreach (var liveBoneEntry in liveBones)
        {
            string boneKey = liveBoneEntry.Key;
            Transform liveBone = liveBoneEntry.Value;

            if (referenceBones.TryGetValue(boneKey, out Transform refBone) && 
                currentBoneWeights.TryGetValue(boneKey, out float weight))
            {
                Quaternion liveRotRelative;
                Quaternion refRotRelative;
                
                liveRotRelative = refHipsInverseWorld * liveBone.rotation;
                refRotRelative = refHipsInverseWorld * refBone.rotation;
                
                float poseError = Quaternion.Angle(liveRotRelative, refRotRelative);
                
                if (poseError > worstBoneError)
                {
                    worstBoneError = poseError;
                    worstBoneName = boneKey;
                }

                sumWeightedSquaredError += (weight * poseError * poseError);
                totalWeight += weight;
            }
        }
        
        if (totalWeight == 0f) return;

        RMSDeviation = Mathf.Sqrt(sumWeightedSquaredError / totalWeight);

        float normalizedError = Mathf.Clamp01(RMSDeviation / MaxAngleDeviation);
        AccuracyScore = (1f - normalizedError) * 100f;
        
        scoreAccumulator += AccuracyScore;
        scoreFrameCount++;
    }
}
