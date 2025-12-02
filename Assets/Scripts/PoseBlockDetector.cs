using UnityEngine;
using System.Collections.Generic;

public class PoseBlockDetector : MonoBehaviour
{
    [Header("Avatar root loaded at runtime")]
    public Transform avatarRoot;  // Assign at runtime

    // Found bones
    private Transform leftHand;
    private Transform rightHand;
    private Transform leftFoot;
    private Transform rightFoot;
    private Transform torso;
    private List<PoseBlockIndex> allBlocks = new();


    private Dictionary<string, int> blockHits = new();

    void Start()
    {
        blockHits["LeftHand"] = -1;
        blockHits["RightHand"] = -1;
        blockHits["LeftFoot"] = -1;
        blockHits["RightFoot"] = -1;
        blockHits["Torso"] = -1;

        FindBlocks();
    }

    void FindBones()
    {
        // Works for Mixamo / Wolf3D / ReadyPlayerMe style rigs
        leftHand = avatarRoot.Find("Armature/Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
        rightHand = avatarRoot.Find("Armature/Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand");

        leftFoot = avatarRoot.Find("Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot");
        rightFoot = avatarRoot.Find("Armature/Hips/RightUpLeg/RightLeg/RightFoot");

        torso = avatarRoot.Find("Armature/Hips/Spine/Spine1");

        if (!leftHand) Debug.LogWarning("LeftHand not found");
        if (!rightHand) Debug.LogWarning("RightHand not found");
        if (!leftFoot) Debug.LogWarning("LeftFoot not found");
        if (!rightFoot) Debug.LogWarning("RightFoot not found");
        if (!torso) Debug.LogWarning("Torso not found");
    }

    void Update()
    {
        blockHits["LeftHand"] = CheckBlock(leftHand);
        blockHits["RightHand"] = CheckBlock(rightHand);
        blockHits["LeftFoot"] = CheckBlock(leftFoot);
        blockHits["RightFoot"] = CheckBlock(rightFoot);
        blockHits["Torso"] = CheckBlock(torso);

        // Example debug:
        Debug.Log(
            $"LH:{blockHits["LeftHand"]}  RH:{blockHits["RightHand"]}  " +
            $"LF:{blockHits["LeftFoot"]}  RF:{blockHits["RightFoot"]}  " +
            $"T:{blockHits["Torso"]}"
        );
    }

    int CheckBlock(Transform bone)
    {
        if (!bone) return -1;

        Collider[] hits = Physics.OverlapSphere(bone.position, 0.1f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("PoseBlock"))
            {
                PoseBlockIndex index = hit.GetComponent<PoseBlockIndex>();
                if (index != null)
                    return index.index;  // return cube ID
            }
        }
        return -1;
    }

    void FindBlocks()
    {
        allBlocks.Clear();

        GameObject[] blocks = GameObject.FindGameObjectsWithTag("PoseBlock");

        foreach (var b in blocks)
        {
            var index = b.GetComponent<PoseBlockIndex>();
            if (index != null)
                allBlocks.Add(index);
            else
                Debug.LogWarning($"PoseBlock '{b.name}' is missing PoseBlockIndex script!");
        }

        Debug.Log($"PoseBlockDetector: Found {allBlocks.Count} pose blocks.");
    }

    public void InitializeForAvatar(Transform avatar)
    {
        avatarRoot = avatar;

        FindBones();   
        Debug.Log("PoseBlockDetector initialized with avatar.");
    }


}
