using UnityEngine;
using Ilumisoft.HealthSystem;
using UnityEngine.AI;

// This component requires a HealthComponent and an Animator to be on the same GameObject
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(Animator))] // Add requirement for Animator
public class DeathHandler : MonoBehaviour
{
    private HealthComponent healthComponent;
    private Animator animator;

    // A string to hold the name or trigger parameter for the death animation
    // Make it a serialized field so it can be set in the Inspector
    [SerializeField]
    private string deathAnimationTrigger = "Die";

    // Awake is called when the script instance is being loaded
    void Awake()
    {
        // Get the HealthComponent attached to this GameObject
        healthComponent = GetComponent<HealthComponent>();
        // Get the Animator attached to this GameObject
        animator = GetComponent<Animator>();
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
        // 1. Disable components that shouldn't run during/after death (e.g., movement, AI).

        NavMeshAgent agent = gameObject.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        EnemyAIController enemyAIController = gameObject.GetComponent<EnemyAIController>();
        if (enemyAIController != null) enemyAIController.enabled = false;

        // 2. Play the death animation, which will calls DisableGameObject via Animation Events.
        animator.SetTrigger(deathAnimationTrigger);
    }

    /// <summary>
    /// This method is designed to be called by an Animation Event 
    /// at the end of the death animation. It disables the GameObject.
    /// </summary>

    public void DisableGameObject()
    {
        // 3. Disable the root GameObject (which effectively "removes" it from the scene 
        // until it's re-enabled, perhaps by a pooling system).
        gameObject.SetActive(false);
    }
}