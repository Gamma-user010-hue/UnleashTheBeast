using UnityEngine;
using GLTFast;
using System.Linq;
using System.Xml.Serialization;
using System;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class ReadyPlayerAvatarTarget : MonoBehaviour
{
    private MotionTrackingPose server;

    public int Delay=0;

    private PoseBlockDetector poseDetector;


    private Transform Hips;
    private Transform Spine;
    private Transform LeftUpLeg;
    private Transform LeftLeg;
    private Transform LeftFoot;
    private Transform RightUpLeg;
    private Transform RightLeg;
    private Transform RightFoot;
    private Transform LeftShoulder;
    private Transform LeftArm;
    private Transform LeftForeArm;
    private Transform LeftHand;
    private Transform LeftPalm;
    private Transform RightShoulder;
    private Transform RightArm;
    private Transform RightForeArm;
    private Transform RightHand;
    private Transform RightPalm;

    private bool AVATAR_LOADED=false;

    public enum AvatarChoice { UseLocalFile, FemaleGymClothing, FemaleDress,FemaleCasual, MaleCasual, MaleTshirt, MaleArmored, FemaleYogaOutfit}
    public AvatarChoice onlineAvatar;

    //avatar filename inside the StreamingAssets folder
    public String localFilename = "67e21d1a79ac9bcf81a46385.glb";

    public bool moveToFloor = false;
    public float floorLevel = -1;

    private void Start()
    {

        server = FindFirstObjectByType<PoseMemory>();
        if (server == null)
        {
            server = FindFirstObjectByType<UPose>();
            if (server == null)
            {
                Debug.LogError("You must have a MotionTracking server in the scene!");
                return;
            }
        }

        InitializeAvatar();

    }

    private async void InitializeAvatar(){
        var gltfImport = new GltfImport();
        String avatar_url="";
        switch (onlineAvatar)
        {
            case AvatarChoice.UseLocalFile:
                avatar_url = "";
                break;
            case AvatarChoice.FemaleGymClothing:
                break;
            case AvatarChoice.FemaleDress:
                break;
            case AvatarChoice.FemaleCasual:
                break;
            case AvatarChoice.MaleCasual:
                break;
            case AvatarChoice.MaleTshirt:
                avatar_url="67e21d1a79ac9bcf81a46385.glb";
                break;
            case AvatarChoice.MaleArmored:
                break;
            case AvatarChoice.FemaleYogaOutfit:
                break;
            default:
                break;
        }

        if (avatar_url.Length == 0)
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath,localFilename);
            await gltfImport.Load(path);
        }
        else
        {
            await gltfImport.Load("https://digitalworlds.github.io/UPose/UPose/Assets/StreamingAssets/" + avatar_url);
        }
        var instantiator = new GameObjectInstantiator(gltfImport,transform);
        var success = await gltfImport.InstantiateMainSceneAsync(instantiator);
        if (success) {
            Debug.Log("GLTF file is loaded.");
            transform.localScale = new Vector3(6.5f, 6.5f, 6.5f);
            transform.position = new Vector3(0.3f, 0f, -18.75f);
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);


            
        
            
            Hips = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Hips");
            Spine = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Spine");
            Transform Spine1 = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Spine1");
            Spine1.localRotation=Quaternion.Euler(0,0,0);
            Transform Spine2 = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Spine2");
            Spine2.localRotation=Quaternion.Euler(0,0,0);

            LeftUpLeg = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "LeftUpLeg");
            LeftLeg = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "LeftLeg");
            
            RightUpLeg = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "RightUpLeg");
            RightLeg = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "RightLeg");
            
            LeftFoot=GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "LeftFoot");
            
            GameObject colliderHolder = new GameObject("LeftFootCollider");
            colliderHolder.transform.SetParent(LeftFoot);
            colliderHolder.transform.localPosition = new Vector3(0, 0.125f, 0);
            colliderHolder.transform.localRotation = Quaternion.Euler(-55,0,0);
            Rigidbody rb=colliderHolder.AddComponent<Rigidbody>();
            rb.isKinematic=true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            BoxCollider footCollider = colliderHolder.AddComponent<BoxCollider>();
            footCollider.size = new Vector3(0.15f, 0.1f, 0.3f);


            RightFoot=GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "RightFoot");
        
            colliderHolder = new GameObject("RightFootCollider");
            colliderHolder.transform.SetParent(RightFoot);
            colliderHolder.transform.localPosition = new Vector3(0, 0.125f, 0);
            colliderHolder.transform.localRotation = Quaternion.Euler(-55,0,0);
            rb=colliderHolder.AddComponent<Rigidbody>();
            rb.isKinematic=true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            footCollider = colliderHolder.AddComponent<BoxCollider>();
            footCollider.size = new Vector3(0.15f, 0.1f, 0.3f);

            LeftShoulder = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "LeftShoulder");
            LeftShoulder.localRotation=Quaternion.Euler(0,0,90);
            LeftArm = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "LeftArm");
            
            LeftForeArm = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "LeftForeArm");
            
            LeftHand=GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "LeftHand");
        
            GameObject leftPalm = new GameObject("LeftPalm");
            leftPalm.transform.parent=LeftHand;
            leftPalm.transform.localPosition = new Vector3(0, 0.07f, 0.04f);
            leftPalm.transform.localRotation = Quaternion.Euler(0,0,0);
            LeftPalm=leftPalm.transform;

            colliderHolder = new GameObject("LeftHandCollider");
            colliderHolder.transform.SetParent(LeftHand);
            colliderHolder.transform.localPosition = new Vector3(0, 0.1f, 0);
            colliderHolder.transform.localRotation = Quaternion.Euler(-90,0,0);
            rb=colliderHolder.AddComponent<Rigidbody>();
            rb.isKinematic=true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            footCollider = colliderHolder.AddComponent<BoxCollider>();
            footCollider.size = new Vector3(0.15f, 0.1f, 0.2f);

            RightShoulder = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "RightShoulder");
            RightShoulder.localRotation=Quaternion.Euler(0,0,-90);
            RightArm = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "RightArm");
            
            RightForeArm = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "RightForeArm");
            
            RightHand=GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "RightHand");
        
            GameObject rightPalm = new GameObject("RightPalm");
            rightPalm.transform.parent=RightHand;
            rightPalm.transform.localPosition = new Vector3(0, 0.07f, 0.04f);
            rightPalm.transform.localRotation = Quaternion.Euler(0,0,0);
            RightPalm=rightPalm.transform;

            colliderHolder = new GameObject("RightHandCollider");
            colliderHolder.transform.SetParent(RightHand);
            colliderHolder.transform.localPosition = new Vector3(0, 0.1f, 0);
            colliderHolder.transform.localRotation = Quaternion.Euler(-90,0,0);
            rb=colliderHolder.AddComponent<Rigidbody>();
            rb.isKinematic=true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            footCollider = colliderHolder.AddComponent<BoxCollider>();
            footCollider.size = new Vector3(0.15f, 0.1f, 0.2f);
            
            
            AVATAR_LOADED=true;

        }else{
            Debug.Log("ERROR: GLTF file is NOT loaded!");
        }
    }

    public bool isLoaded(){return AVATAR_LOADED;}
    public GameObject getLeftHand(){return LeftHand.gameObject;}
    public GameObject getRightHand(){return RightHand.gameObject;}
    public GameObject getLeftFoot(){return LeftFoot.gameObject;}
    public GameObject getRightFoot(){return RightFoot.gameObject;}
    public GameObject getLeftForeArm(){return LeftForeArm.gameObject;}
    public GameObject getRightForeArm(){return RightForeArm.gameObject;}
    public GameObject getLeftLeg(){return LeftLeg.gameObject;}
    public GameObject getRightLeg(){return RightLeg.gameObject;}
    public GameObject getLeftShoulder(){return LeftShoulder.gameObject;}
    public GameObject getRightShoulder(){return RightShoulder.gameObject;}
    public GameObject getLeftUpLeg(){return LeftUpLeg.gameObject;}
    public GameObject getRightUpLeg(){return RightUpLeg.gameObject;}
    public GameObject getLeftPalm(){return LeftPalm.gameObject;}
    public GameObject getRightPalm(){return RightPalm.gameObject;}

    public Quaternion getRightHipRotation() { return server.GetRotation(Landmark.RIGHT_HIP); }
    public Quaternion getLeftHipRotation() { return server.GetRotation(Landmark.LEFT_HIP); }
    public Quaternion getRightElbowRotation() { return server.GetRotation(Landmark.RIGHT_ELBOW); }
    public Quaternion getLeftElbowRotation() { return server.GetRotation(Landmark.LEFT_ELBOW); }

    public void MoveToFloor(float floorY)
    {
        Vector3 pos = transform.position;
        float min = Mathf.Min(LeftFoot.position.y, RightFoot.position.y);
        transform.position = new Vector3(pos.x, pos.y + (floorY - min), pos.z);
    }
    private void Update()
    {
        if (!AVATAR_LOADED || server == null) return;

    }

}


