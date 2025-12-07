using UnityEngine;
using StarterAssets;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Attaches to a UI Image component to display the force blast ability cooldown state.
/// </summary>
public class AbilityHUD : MonoBehaviour
{
    [SerializeField]
    Image overlayImage;

    public PlayerCombat playerCombat;

    private void Start()
    {
        // Try to automatically find the PlayerCombat if not assigned
        if (playerCombat == null)
        {
            playerCombat = FindAnyObjectByType<PlayerCombat>();
        }
        if (playerCombat == null)
        {
            Debug.LogWarning("PlayerCombat not found! AbilityHUD disabled.");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // Only check the main force blast cooldown (the manual ability)
        if (playerCombat._forceBlastTimeoutDelta > 0f)
        {
            OnCooldown();
        }
        else
        {
            OffCooldown();
        }
    }

    public void OffCooldown()
    {
        // Disable the overlay image (ability is ready)
        overlayImage.enabled = false;
    }

    public void OnCooldown()
    {
        // Enable the overlay image (ability is on cooldown)
        overlayImage.enabled = true;
    }
}