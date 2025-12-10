using cherrydev;
using UnityEngine;
using Unity.VisualScripting; // Included for consistency
using StarterAssets; // Required to access the StarterAssetsInputs script
using UnityEngine.Playables; // Required for PlayableDirector

public class DialogueStart : MonoBehaviour
{
    // Challenge timer
    public ChallengeTimerManager challengeTimerManager;

    // The components for the dialogue system
    [SerializeField] private DialogBehaviour DialogBehaviour;
    [SerializeField] private DialogNodeGraph DialogGraph;
    [SerializeField] private Animator playerAnimator;


    [Tooltip("Drag the Cutscene_Director's Playable Director components here.")]
    public PlayableDirector fadeDirector1;

    public PlayableDirector fadeDirector2;

    public Animator myCharacterAnimator;

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
        DialogBehaviour.BindExternalFunction("Challenge", Challenge);
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
                // Start the dialogue and make the player idle
                playerAnimator.SetFloat("MotionSpeed", 0f);
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

        if (fadeDirector1 != null)
        {
            //Play the fade out animation
            fadeDirector1.Play();


            // Optional: Disable the trigger so it only runs once
            // this.enabled = false; 
        }
        else
        {
            Debug.LogError("The Playable Director reference is missing on the " + gameObject.name + " trigger!");
        }
    }

    // This function can now be called from a Sentence Node
    // by using the name "Cutscene2"
    private void Cutscene2()
    {
        if (fadeDirector2 != null)
        {
            //Play the fade out animation
            fadeDirector2.Play();


            // Optional: Disable the trigger so it only runs once
            // this.enabled = false; 
        }
        else
        {
            Debug.LogError("The Playable Director reference is missing on the " + gameObject.name + " trigger!");
        }
    }

    private void Challenge()
    {
        if (challengeTimerManager != null)
        {
            challengeTimerManager.StartTimer();
        }
        else
        {
            Debug.LogError("Missing challenge script");
        }
    }
    // -----------------------------
}