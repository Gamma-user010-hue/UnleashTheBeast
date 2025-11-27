using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using GLTFast;

public class RuntimeAvatarLoader : MonoBehaviour
{
    public string fileName = "localplayer.glb";
    public Transform avatarParent;

    private GameObject loadedAvatar;
    private UdpClient udpClient;
    private bool listening = false;

    const int PORT = 8200;
    private bool avatarReady = false;

    // -------------------------------------------------
    // START
    // -------------------------------------------------
    private async void Start()
    {
        ListenUDP();
        await LoadAvatar();
    }

    // -------------------------------------------------
    // LOAD AVATAR (glTFast)
    // -------------------------------------------------
    private async Task LoadAvatar()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.Log("Loading avatar from: " + fullPath);

        if (!File.Exists(fullPath))
        {
            Debug.LogError("Avatar file DOES NOT EXIST: " + fullPath);
            return;
        }

        GltfImport gltf = new GltfImport();
        bool loaded = await gltf.Load(fullPath);

        if (!loaded)
        {
            Debug.LogError("Failed loading GLB model.");
            return;
        }

        bool instOk = await gltf.InstantiateMainSceneAsync(
            avatarParent != null ? avatarParent : this.transform
        );

        if (!instOk)
        {
            Debug.LogError("Failed instantiating GLB scene.");
            return;
        }

        // Avatar root (first child)
        Transform root = (avatarParent != null ? avatarParent : this.transform).GetChild(0);
        loadedAvatar = root.gameObject;

        // Add bone mapper automatically
        loadedAvatar.AddComponent<AvatarBoneMapper>();

        Debug.Log("Avatar loaded successfully: " + loadedAvatar.name);
        avatarReady = true;
    }

    // -------------------------------------------------
    // UDP LISTENER
    // -------------------------------------------------
    private void ListenUDP()
    {
        try
        {
            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, PORT));

            listening = true;
            Debug.Log($"UDP Listener started on port {PORT}");

            Task.Run(async () =>
            {
                while (listening)
                {
                    try
                    {
                        UdpReceiveResult result = await udpClient.ReceiveAsync();
                        string message = Encoding.UTF8.GetString(result.Buffer);

                        // Debug incoming UDP
                        Debug.Log("UDP Received: " + message);

                        // Forward to PoseReceiver
                        PoseReceiver.Instance?.ProcessMessage(message);
                    }
                    catch (Exception e)
                    {
                        if (listening)
                            Debug.LogError("UDP Error: " + e);
                    }
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError("FAILED to bind UDP on port " + PORT + " → " + e.Message);
        }
    }

    // -------------------------------------------------
    // CLEANUP
    // -------------------------------------------------
    private void OnDisable()
    {
        listening = false;
        udpClient?.Close();
    }

    private void OnApplicationQuit()
    {
        listening = false;
        udpClient?.Close();
    }
}
