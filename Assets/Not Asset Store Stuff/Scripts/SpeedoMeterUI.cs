using UnityEngine;
using StarterAssets; // Required to access the ThirdPersonController class
using TMPro; // Required if using TextMeshPro

/// <summary>
/// Attaches to a UI Text component to display the player's current speed.
/// </summary>
public class SpeedometerUI : MonoBehaviour
{
    [Tooltip("Reference to the ThirdPersonController script.")]
    public ThirdPersonController playerController;

    [Tooltip("Canvas for the speedometer UI.")]
    private Canvas speedCanvas;

    private TextMeshProUGUI speedText;

    private const double PlanckLengthInMeters = 1.616229E-35;

    private float displayedSpeed = 0.0f;


    void Start()
    {
        
        speedText = GetComponent<TextMeshProUGUI>(); 

        if (speedText == null)
        {
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

            //Smoothly interpolate the displayed speed
            displayedSpeed = Mathf.Lerp(displayedSpeed, currentSpeed, Time.deltaTime * 5f);
            speedText.text = $"Speed: {displayedSpeed:F0} km/h";

        }
    }
}