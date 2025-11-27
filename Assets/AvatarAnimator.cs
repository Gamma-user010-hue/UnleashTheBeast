using UnityEngine;

public class AvatarAnimator : MonoBehaviour
{
    private AvatarBoneMapper mapper;
    private PoseReceiver pose;

    // Smoothing factor (0 = raw, 1 = very smooth)
    [Range(0f, 1f)]
    public float smooth = 0.35f;

    // MediaPipe -> Unity axis correction
    private readonly Quaternion axisFix = Quaternion.AngleAxis(-90f, Vector3.right);

    // Per-bone smoothing buffer
    private Quaternion[] smoothedRot = new Quaternion[10];

    void Start()
    {
        // Initialize smoothing buffer
        for (int i = 0; i < 10; i++)
            smoothedRot[i] = Quaternion.identity;
    }

    void Update()
    {
        // ================================
        // STEP 1 — Wait for mapper to appear
        // ================================
        if (mapper == null)
        {
            mapper = GetComponentInChildren<AvatarBoneMapper>(); // dynamic load
            pose = PoseReceiver.Instance;

            if (mapper != null)
            {
                Debug.Log("AvatarAnimator: Mapper found!");
            }
            else
            {
                // Avatar not loaded yet, try again next frame
                return;
            }
        }

        if (pose == null) return;

        // ================================
        // STEP 2 — Apply bone rotations
        // ================================
        for (int i = 0; i < 10; i++)
        {
            if (!mapper.boneMap.ContainsKey(i)) continue;  // safety check

            Quaternion raw = pose.boneRot[i];

            // (1) Axis correction: MediaPipe -> Unity
            Quaternion corrected = axisFix * raw;

            // (2) Smoothing
            smoothedRot[i] = Quaternion.Slerp(
                smoothedRot[i],
                corrected,
                1f - smooth
            );

            // (3) Apply rotation
            mapper.boneMap[i].localRotation = smoothedRot[i];
        }

        // ================================
        // STEP 3 — Pelvis stabilization
        // ================================
        StabilizePelvis();
    }

    /// <summary>
    /// Reduces unwanted twisting & jitter on the hips. (UPose style)
    /// </summary>
    private void StabilizePelvis()
    {
        if (!mapper.boneMap.ContainsKey(0)) return;

        Transform pelvis = mapper.boneMap[0];

        Vector3 e = pelvis.localEulerAngles;

        // Reduce left-right twisting noise
        e.y = Mathf.LerpAngle(e.y, 0, 0.4f);

        pelvis.localEulerAngles = e;
    }
}
