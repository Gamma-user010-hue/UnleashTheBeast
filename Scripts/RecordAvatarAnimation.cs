using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RecordAvatarAnimation : MonoBehaviour
{
    public ReadyPlayerAvatar avatar;   // assign in inspector or find at runtime
    public float recordDuration = 5f;
    public KeyCode startKey = KeyCode.K;

    private bool recording = false;
    private float timer = 0f;

    private AnimationClip clip;
    private Dictionary<Transform, string> bonePaths;
    private List<Transform> trackedBones = new List<Transform>();

    void Start()
    {
        // Wait until avatar is loaded, then register bones
        InvokeRepeating(nameof(TrySetupBones), 0.5f, 0.5f);
    }

    void TrySetupBones()
    {
        if (avatar == null) avatar = FindAnyObjectByType<ReadyPlayerAvatar>();

        if (avatar != null && avatar.isLoaded())
        {
            SetupBones();
            CancelInvoke(nameof(TrySetupBones));
        }
    }

    void SetupBones()
    {
        clip = new AnimationClip();
        clip.frameRate = 30;

        bonePaths = new Dictionary<Transform, string>();

        // Add bones you want to record
        AddBone(avatar.getLeftHand().transform);
        AddBone(avatar.getRightHand().transform);
        AddBone(avatar.getLeftForeArm().transform);
        AddBone(avatar.getRightForeArm().transform);
        AddBone(avatar.getLeftShoulder().transform);
        AddBone(avatar.getRightShoulder().transform);
        AddBone(avatar.getLeftLeg().transform);
        AddBone(avatar.getRightLeg().transform);
        AddBone(avatar.getLeftFoot().transform);
        AddBone(avatar.getRightFoot().transform);
        AddBone(avatar.getLeftUpLeg().transform);
        AddBone(avatar.getRightUpLeg().transform);

        Debug.Log("Avatar Recorder: Bones registered.");
    }

    void AddBone(Transform bone)
    {
        if (bone == null) return;

        trackedBones.Add(bone);
        string path = AnimationUtility.CalculateTransformPath(bone, avatar.transform);
        bonePaths[bone] = path;
    }

    void Update()
    {
        if (!recording && Input.GetKeyDown(startKey))
        {
            StartCoroutine(StartCountdown());
        }

        if (recording)
        {
            timer += Time.deltaTime;

            float t = timer;

            // Record bone rotations as animation keys
            foreach (Transform bone in trackedBones)
            {
                Quaternion rot = bone.localRotation;
                string path = bonePaths[bone];

                AnimationCurve curveX = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.x")) ?? new AnimationCurve();
                AnimationCurve curveY = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.y")) ?? new AnimationCurve();
                AnimationCurve curveZ = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.z")) ?? new AnimationCurve();
                AnimationCurve curveW = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.w")) ?? new AnimationCurve();

                curveX.AddKey(t, rot.x);
                curveY.AddKey(t, rot.y);
                curveZ.AddKey(t, rot.z);
                curveW.AddKey(t, rot.w);

                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.x"), curveX);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.y"), curveY);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.z"), curveZ);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.w"), curveW);
            }

            if (timer >= recordDuration)
            {
                SaveClip();
                recording = false;
                Debug.Log("Recording finished.");
            }
        }
    }

    System.Collections.IEnumerator StartCountdown()
    {
        Debug.Log("Recording in: 3");
        yield return new WaitForSeconds(1);
        Debug.Log("Recording in: 2");
        yield return new WaitForSeconds(1);
        Debug.Log("Recording in: 1");
        yield return new WaitForSeconds(1);

        Debug.Log("Recording STARTED!");
        recording = true;
        timer = 0;
        clip = new AnimationClip();
    }

    void SaveClip()
    {
#if UNITY_EDITOR
        string path = "Assets/RecordedAnimations";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", "RecordedAnimations");

        string filePath = $"{path}/Recorded_{System.DateTime.Now:HH-mm-ss}.anim";

        AssetDatabase.CreateAsset(clip, filePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Saved animation to {filePath}");
#else
        Debug.LogError("Saving .anim only works in Unity Editor.");
#endif
    }
}
