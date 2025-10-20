using cherrydev;
using UnityEngine;
using Unity.VisualScripting; // Included for consistency
using StarterAssets; // Required to access the StarterAssetsInputs script

public class DialogueStart : MonoBehaviour
{
    // The components for the dialogue system
    [SerializeField] private DialogBehaviour DialogBehaviour;
    [SerializeField] private DialogNodeGraph DialogGraph;

    // Flag to ensure the dialogue only starts once
    private bool dialogueStarted = false;

    // References to the player's components
    private const string PlayerTag = "Player";
    private StarterAssetsInputs _playerInputs;

    // Flag to track if the player is currently inside the trigger
    private bool playerInTrigger = false;


    // NOTE: This script assumes the GameObject has a Collider set to 'Is Trigger'

    // --- ADDED THIS METHOD ---
    // We use Start() to bind the functions once when the script loads.
    // This must be done before the functions are called[cite: 148].
    private void Start()
    {
        // Bind your C# functions to string names [cite: 146, 166]
        DialogBehaviour.BindExternalFunction("Cutscene1", Cutscene1);
        DialogBehaviour.BindExternalFunction("Cutscene2", Cutscene2);
    }
    // -------------------------

    private void Update()
    {
        // Only run the check if the player is inside the trigger
        if (playerInTrigger && !dialogueStarted && _playerInputs != null)
        {
            // The StarterAssetsInputs script uses boolean flags that are automatically
            // set by the Input System. We check the 'interact' flag.
            // If you don't have an 'interact' flag, use an existing one like '_playerInputs.attack'

            // --- IMPORTANT: You must manually implement a public 'interact' bool in StarterAssetsInputs.cs ---
            // and ensure your Input Action is setting it to true on press, and false on release.

            if (_playerInputs.interact)
            {
                // Start the dialogue
                DialogBehaviour.StartDialog(DialogGraph);
                dialogueStarted = true;

                // OPTIONAL: Immediately consume the input so it doesn't trigger other systems
                _playerInputs.interact = false;

                // Optional: Disable the collider so the interaction can't be triggered again
                // GetComponent<Collider>().enabled = false;
            }
        }
    }

    // -----------------------------------------------------------------------------

    // Use OnTriggerEnter/Exit to manage the 'playerInTrigger' flag and get the Player's Input component

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PlayerTag))
        {
            // Get the StarterAssetsInputs component from the player
            _playerInputs = other.GetComponent<StarterAssetsInputs>();

            if (_playerInputs != null)
            {
                playerInTrigger = true;
                // Optional: Display a prompt to the player (e.g., "Press E to Interact")
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PlayerTag))
        {
            playerInTrigger = false;
            _playerInputs = null; // Clear the reference when the player leaves
            // Optional: Hide the player prompt
        }
    }

    // Optional: Add a function to reset the dialogue state if you want it to be replayable
    public void ResetDialogue()
    {
        dialogueStarted = false;
    }

    // --- ADDED THESE FUNCTIONS ---

    // This function can now be called from a Sentence Node
    // by using the name "Cutscene1"
    private void Cutscene1()
    {
        Debug.Log("External Function: Cutscene1 is running!");
        // Add your cutscene logic here
    }

    // This function can now be called from a Sentence Node
    // by using the name "Cutscene2"
    private void Cutscene2()
    {
        Debug.Log("External Function: Cutscene2 is running!");
        // Add your other cutscene logic here
    }
    // -----------------------------
}