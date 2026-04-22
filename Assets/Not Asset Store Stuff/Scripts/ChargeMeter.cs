using UnityEngine;
using StarterAssets;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Attaches to a UI Text component to display the charge of the carried item.
/// </summary>
public class ChargeMeter : MonoBehaviour
{
    [SerializeField]
    Image fillImage;

    public float maxCharge = 0f;

    [Tooltip("Controls how fast the text changes animate in km/h per second")]
    [SerializeField, Min(1f)]
    float textChangeSpeed = 150f;

    [Tooltip("Controls how fast the needle/fill animates per second")]
    [SerializeField, Min(1f)]
    float needleChangeSpeed = 200f;

    private TextMeshProUGUI chargeText;
    private float displayedCharge = 0.0f;
    private float smoothCharge = 0.0f;
    public Pickup pickup;


    void Start()
    {
        chargeText = GetComponent<TextMeshProUGUI>();
        if (chargeText == null)
        {
            Debug.LogWarning("ChargemeterUI: No TextMeshProUGUI component found!");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (pickup != null && chargeText != null)
        {
            // Get the current distance travelled from the public property
            float currentDistanceTraveled = Pickup.DistanceTraveled;

            // Get the current max distance for charge
            maxCharge = pickup.maxTravelledDistance;

            if (currentDistanceTraveled >= maxCharge)
            {
                fillImage.fillAmount = 1;
                chargeText.text = "CHARGED!";
                return;
            }

            // Smoothly interpolate the displayed text speed
            displayedCharge = Mathf.MoveTowards(displayedCharge, currentDistanceTraveled, Time.deltaTime * textChangeSpeed);
            chargeText.text = $"{(displayedCharge/maxCharge*100f):F0} %";

            // Smoothly interpolate the needle/fill speed
            smoothCharge = Mathf.MoveTowards(smoothCharge, currentDistanceTraveled, Time.deltaTime * needleChangeSpeed);

            UpdateFillbar();
        }
    }

    void UpdateFillbar()
    {
        // Normalize the speed to 0-1 range
        float normalizedCharge = Mathf.Clamp01(smoothCharge / maxCharge);

        // Update the fill amount
        fillImage.fillAmount = normalizedCharge;
    }
}