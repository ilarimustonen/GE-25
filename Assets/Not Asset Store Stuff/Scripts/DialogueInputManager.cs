using UnityEngine;

public class DialogueInputManager : MonoBehaviour
{
    // Drag your main Dialogue UI Prefab/GameObject here in the Inspector.
    public GameObject dialogueUI;

    private GameObject player;
    // IMPORTANT: Replace "PlayerController" with the actual name of your player input script.
    private MonoBehaviour playerControllerScript;

    void Start()
    {
        // Find the player GameObject using its tag.
        // Make sure your player object is tagged "Player" in the Inspector.
        player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Get the player's controller script.
            // Replace "PlayerController" with the name of your script that handles movement, camera, etc.
            playerControllerScript = player.GetComponent("ThirdPersonController") as MonoBehaviour;
            if (playerControllerScript == null)
            {
                Debug.LogError("Player Controller script not found! Please make sure the script name is correct and it's attached to the player.");
            }
        }
        else
        {
            Debug.LogError("Player object not found! Make sure your player is tagged 'Player'.");
        }

        // Ensure the dialogue UI is hidden at the start.
        if (dialogueUI != null)
        {
            dialogueUI.SetActive(false);
        }
    }

    /// <summary>
    /// This function disables the player's control script, shows the dialogue UI, and handles the mouse cursor.
    /// </summary>
    public void DisablePlayerInput()
    {
        if (playerControllerScript != null)
        {
            playerControllerScript.enabled = false;
        }

        if (dialogueUI != null)
        {
            dialogueUI.SetActive(true);
        }

        // Show and unlock the cursor so the player can click on dialogue options.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// This function re-enables the player's control script, hides the dialogue UI, and handles the mouse cursor.
    /// </summary>
    public void EnablePlayerInput()
    {
        if (playerControllerScript != null)
        {
            playerControllerScript.enabled = true;
        }

        if (dialogueUI != null)
        {
            dialogueUI.SetActive(false);
        }

        // Lock and hide the cursor for normal gameplay.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

