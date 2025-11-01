using UnityEngine;
using UnityEngine.Rendering;
// If you are using URP, you might need this for specific effects:
// using UnityEngine.Rendering.Universal; 

/// <summary>
/// Manages the visual and auditory effects of being submerged in water using the Volume system.
/// </summary>
public class UnderwaterEffectController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The WaterRunning script to get the current water surface level.")]
    [SerializeField] private WaterRunning waterRunningScript;

    [Tooltip("The Global Volume component for the underwater effect.")]
    [SerializeField] private Volume underwaterVolume;

    [Tooltip("The Low Pass Filter attached to the AudioListener (usually on this same GameObject).")]
    [SerializeField] private AudioLowPassFilter audioFilter;

    [Header("Settings")]
    [Tooltip("How far below the surface the camera needs to be to trigger the effect.")]
    [SerializeField] private float triggerDepth = 0.15f;

    [Header("Audio Effects")]
    [Tooltip("The cutoff frequency for the audio low pass filter when submerged.")]
    [SerializeField] private float muffledCutoffFrequency = 1200f; // Muffles high frequencies

    private bool isUnderwater = false;
    private float waterSurfaceLevel = 0.0f;

    // Store original scene settings for restoration
    private Color originalFogColor;
    private float originalFogDensity;
    private bool originalFogState;

    void Start()
    {
        // 1. Component Checks
        if (underwaterVolume == null)
        {
            Debug.LogError("Underwater Volume component not assigned. Please assign the Global Volume.");
            enabled = false;
            return;
        }

        if (waterRunningScript == null)
        {
            Debug.LogError("WaterRunning script not assigned. Cannot get water level.");
            enabled = false;
            return;
        }

        // 2. Audio Filter Setup
        audioFilter = GetComponent<AudioLowPassFilter>();
        if (audioFilter == null)
        {
            Debug.LogWarning("Audio Low Pass Filter not found on camera. Audio effects will be skipped.");
        }

        // 3. Store Original Render Settings
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogState = RenderSettings.fog;

        // 4. Initial State Sync
        waterSurfaceLevel = waterRunningScript.waterSurfaceLevel + 1f;
        underwaterVolume.weight = 0f; // Ensure effect starts disabled
    }

    void Update()
    {
        // Always sync the water level from the source script
        waterSurfaceLevel = waterRunningScript.waterSurfaceLevel;

        // Calculate the camera's depth relative to the water surface
        float cameraY = transform.position.y;
        bool shouldBeUnderwater = cameraY < (waterSurfaceLevel - triggerDepth);

        if (shouldBeUnderwater && !isUnderwater)
        {
            SetUnderwater(true);
        }
        else if (!shouldBeUnderwater && isUnderwater)
        {
            SetUnderwater(false);
        }
    }

    private void SetUnderwater(bool submerged)
    {
        isUnderwater = submerged;

        if (submerged)
        {
            // 1. Volume Weight (Visuals)
            // Immediately set the volume's weight to 1 to activate its effects
            underwaterVolume.weight = 1f;

            // 2. Audio (Muffle)
            if (audioFilter != null)
            {
                audioFilter.enabled = true;
                audioFilter.cutoffFrequency = muffledCutoffFrequency;
            }

            // NOTE: Fog and other environment settings are now handled inside the 
            // UnderwaterVolume Profile using the Fog/Density/Color Adjustment components.
        }
        else
        {
            // 1. Volume Weight (Visuals)
            // Restore default view
            underwaterVolume.weight = 0f;

            // 2. Audio (Clear)
            if (audioFilter != null)
            {
                audioFilter.enabled = false;
            }

            // NOTE: RenderSettings for Fog are NOT touched here to allow the 
            // main scene's Global Volume (if one exists) to handle the surface fog.
        }
    }
}