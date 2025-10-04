using UnityEngine;
using Ilumisoft.HealthSystem; // Don't forget the namespace!

// This component requires a HealthComponent to be on the same GameObject
[RequireComponent(typeof(HealthComponent))]
public class DeathHandler : MonoBehaviour
{
    private HealthComponent healthComponent;

    // Awake is called when the script instance is being loaded
    void Awake()
    {
        // Get the HealthComponent attached to this GameObject
        healthComponent = GetComponent<HealthComponent>();
    }

    // OnEnable is called when the object becomes enabled and active
    void OnEnable()
    {
        // Subscribe our HandleDeath method to the OnHealthEmpty event
        // Now, whenever OnHealthEmpty is invoked, HandleDeath will be called
        healthComponent.OnHealthEmpty += HandleDeath;
    }

    // OnDisable is called when the object becomes disabled or is destroyed
    void OnDisable()
    {
        // It's very important to unsubscribe from events when the object is disabled
        // to prevent errors and memory leaks.
        healthComponent.OnHealthEmpty -= HandleDeath;
    }

    /// <summary>
    /// This method is called by the OnHealthEmpty event from the HealthComponent.
    /// </summary>
    private void HandleDeath()
    {
        // --- PUT YOUR DEATH LOGIC HERE ---

        Debug.Log(gameObject.name + " has died!");

        // Example 1: Destroy the GameObject after 2 seconds
        // Destroy(gameObject, 2.0f);

        // Example 2: Just destroy it immediately
        Destroy(gameObject);

        // Example 3: More complex logic
        // - Play a death animation
        // - Disable character controller/AI
        // - Enable a ragdoll
        // - Drop loot
        // - etc.
    }
}