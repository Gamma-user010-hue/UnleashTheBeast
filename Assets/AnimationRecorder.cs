using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;    // Must be at the top!
#endif

public class AnimationRecorder : MonoBehaviour
{
    [Header("Bones to Record")]
    public Transform[] bones;

    [Header("Recording Settings")]
    public float recordDuration = 5f;

    private bool countingDown = false;
    private bool recording = false;
    private float timer = 0f;

    private float countdown = 3f;

    private Dictionary<Transform, List<Keyframe[]>> rotationKeys;

    void Update()
    {
        // --- Start recording when pressing K ---
        if (Input.GetKeyDown(KeyCode.K) && !recording && !countingDown)
        {
            StartCountdown();
        }

        // --- Handle countdown ---
        if (countingDown)
        {
            countdown -= Time.deltaTime;

            if (countdown <= 3 && countdown > 2.9f) Debug.Log("3");
            if (countdown <= 2 && countdown > 1.9f) Debug.Log("2");
            if (countdown <= 1 && countdown > 0.9f) Debug.Log("1");

            if (countdown <= 0f)
            {
                countingDown = false;
                BeginRecording();
            }
        }

        // --- Recording the animation ---
        if (!recording) return;

        timer += Time.deltaTime;

        if (timer >= recordDuration)
        {
            EndRecording();
            return;
        }

        foreach (Transform bone in bones)
        {
            Quaternion q = bone.localRotation;

            rotationKeys[bone].Add(new Keyframe[]
            {
                new Keyframe(timer, q.x),
                new Keyframe(timer, q.y),
                new Keyframe(timer, q.z),
                new Keyframe(timer, q.w)
            });
        }
    }

    // ---------------------------------------------------------
    // COUNTDOWN SYSTEM
    // ---------------------------------------------------------
    private void StartCountdown()
    {
        countdown = 3f;
        countingDown = true;

        Debug.Log("Recording starts in 3 seconds...");
    }

    // ---------------------------------------------------------
    // START RECORDING
    // ---------------------------------------------------------
    public void BeginRecording()
    {
        rotationKeys = new Dictionary<Transform, List<Keyframe[]>>();

        foreach (Transform bone in bones)
            rotationKeys.Add(bone, new List<Keyframe[]>());

        timer = 0f;
        recording = true;

        Debug.Log("🎥 Recording started...");
    }

    // ---------------------------------------------------------
    // STOP RECORDING
    // ---------------------------------------------------------
    public void EndRecording()
    {
        recording = false;

        Debug.Log("🎞 Recording finished.");

        AnimationClip clip = BuildClip();
        SaveClip(clip);
    }

    // ---------------------------------------------------------
    // BUILD ANIMATION CLIP
    // ---------------------------------------------------------
    private AnimationClip BuildClip()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60;

        foreach (var pair in rotationKeys)
        {
            Transform bone = pair.Key;
            List<Keyframe[]> frames = pair.Value;

            AnimationCurve curveX = new AnimationCurve();
            AnimationCurve curveY = new AnimationCurve();
            AnimationCurve curveZ = new AnimationCurve();
            AnimationCurve curveW = new AnimationCurve();

            foreach (Keyframe[] k in frames)
            {
                curveX.AddKey(k[0]);
                curveY.AddKey(k[1]);
                curveZ.AddKey(k[2]);
                curveW.AddKey(k[3]);
            }

            string path = AnimationUtility.CalculateTransformPath(bone, transform);

            clip.SetCurve(path, typeof(Transform), "localRotation.x", curveX);
            clip.SetCurve(path, typeof(Transform), "localRotation.y", curveY);
            clip.SetCurve(path, typeof(Transform), "localRotation.z", curveZ);
            clip.SetCurve(path, typeof(Transform), "localRotation.w", curveW);
        }

        return clip;
    }

    // ---------------------------------------------------------
    // SAVE ANIMATION CLIP
    // ---------------------------------------------------------
#if UNITY_EDITOR
    private void SaveClip(AnimationClip clip)
    {
        string folder = "Assets/RecordedAnimations";
        if (!System.IO.Directory.Exists(folder))
            System.IO.Directory.CreateDirectory(folder);

        string path = folder + "/" + gameObject.name + "_Recorded.anim";

        AssetDatabase.CreateAsset(clip, path);
        AssetDatabase.SaveAssets();

        Debug.Log("Animation saved to: " + path);
    }
#else
    private void SaveClip(AnimationClip clip)
    {
        Debug.LogWarning("Saving animations only works inside the Unity Editor.");
    }
#endif
}
