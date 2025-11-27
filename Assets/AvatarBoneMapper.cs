using System.Collections.Generic;
using UnityEngine;

public class AvatarBoneMapper : MonoBehaviour
{
    public Dictionary<int, Transform> boneMap = new Dictionary<int, Transform>();

    // Call this manually AFTER GLB is loaded
    public void FindBones()
    {
        Transform root = transform;

        // ReadyPlayerMe bone paths
        Transform hips           = root.Find("Armature/Hips");
        Transform spine          = root.Find("Armature/Hips/Spine");

        Transform leftShoulder   = root.Find("Armature/Hips/Spine/Chest/UpperChest/LeftShoulder");
        Transform rightShoulder  = root.Find("Armature/Hips/Spine/Chest/UpperChest/RightShoulder");

        Transform leftUpperArm   = leftShoulder?.Find("LeftUpperArm");
        Transform leftLowerArm   = leftUpperArm?.Find("LeftLowerArm");

        Transform rightUpperArm  = rightShoulder?.Find("RightUpperArm");
        Transform rightLowerArm  = rightUpperArm?.Find("RightLowerArm");

        Transform leftUpperLeg   = hips?.Find("LeftUpperLeg");
        Transform leftLowerLeg   = leftUpperLeg?.Find("LeftLowerLeg");

        Transform rightUpperLeg  = hips?.Find("RightUpperLeg");
        Transform rightLowerLeg  = rightUpperLeg?.Find("RightLowerLeg");

        // Map MediaPipe indices
        boneMap[0] = hips;
        boneMap[1] = spine;
        boneMap[2] = leftShoulder;
        boneMap[3] = rightShoulder;
        boneMap[4] = leftLowerArm;
        boneMap[5] = rightLowerArm;
        boneMap[6] = leftUpperLeg;
        boneMap[7] = rightUpperLeg;
        boneMap[8] = leftLowerLeg;
        boneMap[9] = rightLowerLeg;

        Debug.Log("AvatarBoneMapper: Bones mapped successfully.");
    }
}
