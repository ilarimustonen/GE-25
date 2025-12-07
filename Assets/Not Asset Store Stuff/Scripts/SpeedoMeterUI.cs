using UnityEngine;
using StarterAssets;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Attaches to a UI Text component to display the player's current speed with realistic speedometer scaling.
/// </summary>
public class SpeedometerUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the ThirdPersonController script.")]
    public ThirdPersonController playerController;

    [SerializeField]
    Image fillImage;

    [Header("Speed Settings")]
    [Tooltip("Maximum speed for scaling the speedometer fill (in km/h).")]
    public float MaxSpeedScaling = 300f;

    [Tooltip("Controls how fast the text changes animate in km/h per second")]
    [SerializeField, Min(1f)]
    float textChangeSpeed = 150f;

    [Tooltip("Controls how fast the needle/fill animates in km/h per second")]
    [SerializeField, Min(1f)]
    float needleChangeSpeed = 200f;

    [Header("Scaling Type")]
    [Tooltip("How the speedometer scales visually")]
    public ScalingType scalingType = ScalingType.Quadratic;

    [Tooltip("For polynomial scaling: higher = more space for low speeds (1.5-3.0 recommended)")]
    [SerializeField, Range(1f, 4f)]
    float polynomialPower = 2.0f;

    [Tooltip("For logarithmic scaling: controls curve intensity (5-20 recommended)")]
    [SerializeField, Range(1f, 50f)]
    float logarithmicScale = 10f;

    private TextMeshProUGUI speedText;
    private float displayedSpeed = 0.0f;
    private float smoothNeedleSpeed = 0.0f;

    public enum ScalingType
    {
        Linear,          // Standard linear scaling (what you had)
        Quadratic,       // Square root scaling (realistic, more space for low speeds)
        Cubic,           // Cubic root scaling (even more space for low speeds)
        Polynomial,      // Custom power curve (adjustable)
        Logarithmic,     // Log curve (most space for low speeds)
        Exponential      // More space for high speeds
    }

    void Start()
    {
        speedText = GetComponent<TextMeshProUGUI>();
        if (speedText == null)
        {
            Debug.LogWarning("SpeedometerUI: No TextMeshProUGUI component found!");
            enabled = false;
            return;
        }

        // Try to automatically find the player controller if not assigned
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<ThirdPersonController>();
        }
        if (playerController == null)
        {
            Debug.LogWarning("SpeedometerUI: ThirdPersonController not found!");
            enabled = false;
        }
    }

    void Update()
    {
        if (playerController != null && speedText != null)
        {
            // Get the current speed from the public property
            float currentSpeedMS = playerController.CurrentSpeed;
            float currentSpeed = currentSpeedMS * 3.6f; // Convert m/s to km/h

            // Smoothly interpolate the displayed text speed
            displayedSpeed = Mathf.MoveTowards(displayedSpeed, currentSpeed, Time.deltaTime * textChangeSpeed);
            speedText.text = $"{displayedSpeed:F0} km/h";

            // Smoothly interpolate the needle/fill speed
            smoothNeedleSpeed = Mathf.MoveTowards(smoothNeedleSpeed, currentSpeed, Time.deltaTime * needleChangeSpeed);

            UpdateFillbar();
        }
    }

    void UpdateFillbar()
    {
        // Normalize the speed to 0-1 range
        float normalizedSpeed = Mathf.Clamp01(smoothNeedleSpeed / MaxSpeedScaling);

        // Apply the selected scaling curve
        float scaledValue = ApplyScaling(normalizedSpeed);

        // Update the fill amount
        fillImage.fillAmount = scaledValue;
    }

    float ApplyScaling(float normalizedValue)
    {
        switch (scalingType)
        {
            case ScalingType.Linear:
                return normalizedValue;

            case ScalingType.Quadratic:
                // Square root gives more space to lower speeds (like real speedometers)
                return Mathf.Sqrt(normalizedValue);

            case ScalingType.Cubic:
                // Cubic root gives even more space to lower speeds
                return Mathf.Pow(normalizedValue, 1f / 3f);

            case ScalingType.Polynomial:
                // Custom power curve (user adjustable)
                return Mathf.Pow(normalizedValue, 1f / polynomialPower);

            case ScalingType.Logarithmic:
                // Logarithmic curve - most space for low speeds
                if (normalizedValue <= 0) return 0;
                return Mathf.Log(1 + normalizedValue * logarithmicScale) / Mathf.Log(1 + logarithmicScale);

            case ScalingType.Exponential:
                // Exponential - compresses low speeds, expands high speeds
                return (Mathf.Exp(normalizedValue * 2) - 1) / (Mathf.Exp(2) - 1);

            default:
                return normalizedValue;
        }
    }
}