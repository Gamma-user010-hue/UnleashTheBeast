using UnityEngine;

public class PoseReceiver : MonoBehaviour
{
    public static PoseReceiver Instance;

    // Quaternion per MediaPipe index (0–9)
    public Quaternion[] boneRot = new Quaternion[10];

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Receive raw UDP message from RuntimeAvatarLoader
    /// </summary>
    public void ProcessMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        if (!msg.StartsWith("mprot")) return;   // UPose style header check

        string[] lines = msg.Split('\n');

        // line 0 = "mprot", skip
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            if (!lines[i].Contains("|")) continue;

            string[] p = lines[i].Split('|');
            if (p.Length < 5) continue;

            int index = int.Parse(p[0]);
            if (index < 0 || index > 9) continue;

            float x = float.Parse(p[1]);
            float y = float.Parse(p[2]);
            float z = float.Parse(p[3]);
            float w = float.Parse(p[4]);

            // Save quaternion for AvatarAnimator
            boneRot[index] = new Quaternion(x, y, z, w);
        }
    }
}
