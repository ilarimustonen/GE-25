using UnityEngine;
using Ilumisoft.HealthSystem;

// This component requires a HealthComponent and an Animator to be on the same GameObject
[RequireComponent(typeof(HealthComponent))]
public class CubeDestroy : MonoBehaviour
{
    private HealthComponent healthComponent;
    void Awake()
    {
        // Get the HealthComponent attached to this GameObject
        healthComponent = GetComponent<HealthComponent>();
    }

    // OnEnable is called when the object becomes enabled and active
    void OnEnable()
    {
        // Subscribe our HandleDeath method to the OnHealthEmpty event
        healthComponent.OnHealthEmpty += HandleDeath;
    }

    // OnDisable is called when the object becomes disabled or is destroyed
    void OnDisable()
    {
        // Unsubscribe from events
        if (healthComponent != null)
        {
            healthComponent.OnHealthEmpty -= HandleDeath;
        }
    }

    /// <summary>
    /// This method is called by the OnHealthEmpty event from the HealthComponent.
    /// Initiates the death sequence.
    /// </summary>
    private void HandleDeath()
    {
        gameObject.SetActive(false);
    }
}