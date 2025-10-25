using UnityEngine;
using StarterAssets;
public class PlayerCutsceneHelper : MonoBehaviour
{
    private GameObject player;
    private MonoBehaviour playerControllerScript;
    private StarterAssetsInputs _playerInputs;
    private Animator _animator;



    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Get the main controller script
            playerControllerScript = player.GetComponent<ThirdPersonController>();
            // Get the inputs script
            _playerInputs = player.GetComponent<StarterAssetsInputs>();
            // Get the Animator component
            _animator = player.GetComponent<Animator>();

            if (playerControllerScript == null || _playerInputs == null)
            {
                Debug.LogError("Required player components (ThirdPersonController or StarterAssetsInputs) not found!");
            }
        }
        else
        {
            Debug.LogError("Player object not found! Make sure your player is tagged 'Player'.");
        }
    }
    public void DisablePlayerInput()
    {
        // Stop any running coroutines just in case, though none should be running now.
        StopAllCoroutines();

        if (playerControllerScript != null)
        {
            playerControllerScript.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void EnablePlayerInput()
    {
        // 1. CRITICAL: Consume the stale inputs (like Jump from pressing space for dialogue)
        if (_playerInputs != null)
        {
            _playerInputs.jump = false;
            // Also good practice to clear interact/other action flags used in dialogue.
            _playerInputs.interact = false;
            // You may need to add other action flags here if they advance dialogue (e.g., _playerInputs.attack = false;)
        }
        // 2. Re-enable player controller instantly
        if (playerControllerScript != null)
        {
            playerControllerScript.enabled = true;
        }
        // 3. Lock and hide the cursor for normal gameplay.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}