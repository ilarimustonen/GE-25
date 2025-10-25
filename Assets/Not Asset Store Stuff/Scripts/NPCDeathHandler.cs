using UnityEngine;
using Ilumisoft.HealthSystem;
using UnityEngine.AI;

// This component requires a HealthComponent and an Animator to be on the same GameObject
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(Animator))] // Add requirement for Animator
public class NPCDeathHandler : MonoBehaviour
{
    private HealthComponent healthComponent;
    private Animator animator;
    private DialogueStart dialogueSystem;
    public bool isAlreadyDead;
    public string deathanimTrigger = "Died";


    // Awake is called when the script instance is being loaded
    void Awake()
    {
        // Get the HealthComponent attached to this GameObject
        healthComponent = GetComponent<HealthComponent>();
        // Get the Animator attached to this GameObject
        animator = GetComponent<Animator>();
        // Get the DialogueStart component if it exists
        dialogueSystem = GetComponent<DialogueStart>();
    }


    void Start()
    {
        // Subscribe our HandleDeath method to the OnHealthEmpty event
        healthComponent.OnHealthEmpty += HandleDeath;
    }

    /// <summary>
    /// This method is called by the OnHealthEmpty event from the HealthComponent.
    /// Initiates the death sequence.
    /// </summary>
    private void HandleDeath()
    {

        // check if already dead (from cutscene) before playing animation.
        if (isAlreadyDead) return;

        // Disable dialogue system if present
        if (dialogueSystem != null)
        {
            dialogueSystem.enabled = false;
        }

        // Play the death animation
        animator.SetTrigger(deathanimTrigger);
    }

    public void SetDeathBool()
    {
        isAlreadyDead = true;
    }
}