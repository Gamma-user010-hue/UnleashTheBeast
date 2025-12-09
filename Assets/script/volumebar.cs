using UnityEngine;
using UnityEngine.UI; // Needed for UI Images
using TMPro;

public class AudioVisualizerWithColor : MonoBehaviour
{
    [Header("Mic Settings")]
    public string micName;
    private AudioClip micClip;
    int sampleWindow = 256;

    // smoothing
    float smoothedDb = -80f;
    [Range(0f, 1f)] 
    public float smoothFactor = 0.05f; // lower = smoother
    
    [Header("UI References")]
    public TextMeshProUGUI text;
    public Transform visualBar; // Drag your Bar Image/Cube here

    [Header("Visual Scale Settings")]
    public float minDb = -65f; // The noise floor (silence)
    public float maxDb = -10f; // The volume at which the bar hits max height

    [Header("Color Settings")]
    public Color lowColor = Color.red;   // Color when quiet
    public Color highColor = Color.green; // Color when loud

    // Internal references for coloring
    private Image uiImage;
    private Renderer objRenderer;

    void Start()
    {
        // ---- Mic Setup ----
        if (Microphone.devices.Length > 0)
        {
            micName = Microphone.devices[0];
            micClip = Microphone.Start(micName, true, 1, 44100);
            if(text) text.text = "Mic Active";
        }
        else
        {
            if(text) text.text = "No Mic Found";
            Debug.LogError("No microphone detected!");
        }

        // ---- Component Detection for Coloring ----
        if (visualBar != null)
        {
            // Check if it's a UI Image first
            uiImage = visualBar.GetComponent<Image>();

            // If it's not a UI Image, check if it's a 3D object renderer
            if (uiImage == null)
            {
                objRenderer = visualBar.GetComponent<Renderer>();
            }
        }
    }

    void Update()
    {
        // 1. Get and smooth data
        float rawDb = GetDecibels();
        smoothedDb = Mathf.Lerp(smoothedDb, rawDb, smoothFactor);
        
        if(text != null) text.text = smoothedDb.ToString("F1") + " dB";

        if (visualBar != null)
        {
            // 2. Calculate Intensity (0.0 to 1.0)
            // InverseLerp converts the dB range into a percentage between 0 and 1
            float intensity = Mathf.InverseLerp(minDb, maxDb, smoothedDb);

            // 3. Apply Scaling
            // We keep X and Z as 1 so we don't distort the width/depth
            visualBar.localScale = new Vector3(1, intensity * 10, 1);

            // 4. Apply Coloring based on the same intensity
            // Color.Lerp blends between red and green based on the intensity percentage
            Color currentColor = Color.Lerp(lowColor, highColor, intensity);

            if (uiImage != null)
            {
                // It's a UI Image
                uiImage.color = currentColor;
            }
            else if (objRenderer != null)
            {
                // It's a 3D Cube/Object. We access the Material.
                // Note: Using .material creates a unique instance so they don't all share one color
                objRenderer.material.color = currentColor;
            }
        }
    }
    
    // Standard dB calculation
    float GetDecibels()
    {
        if (micClip == null) return minDb;

        float[] samples = new float[sampleWindow];
        // Ensure we aren't asking for a position before the mic has actually recorded enough data
        int micPos = Microphone.GetPosition(micName);
        if (micPos < sampleWindow + 1) return minDb; 

        micClip.GetData(samples, micPos - sampleWindow);

        float sum = 0;
        for (int i = 0; i < sampleWindow; i++)
        {
            sum += samples[i] * samples[i];
        }

        float rms = Mathf.Sqrt(sum / sampleWindow);

        if (rms <= 1e-07f) return minDb; // Safety against extremely low numbers

        return 20f * Mathf.Log10(rms);
    }
}