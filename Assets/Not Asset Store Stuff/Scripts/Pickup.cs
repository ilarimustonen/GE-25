using UnityEngine;
using StarterAssets;
public abstract class Pickup : MonoBehaviour
{
    public abstract string objectiveName { get; }

    public abstract string description { get; }


    protected abstract BoxCollider pickupCollider { get; }

    private GameObject playerBack;
    private bool playerInRange;
    private StarterAssetsInputs _playerInputs;
    private GameObject _player;
    private bool attachingToBack;
    private float factor;
    Vector3 originalPosition;
    private static bool attached;
    

    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _playerInputs = _player.GetComponent<StarterAssetsInputs>();
        playerBack = GameObject.FindGameObjectWithTag("PlayerBack");
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == _player)
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == _player)
        {
            playerInRange = false;
        }
    }
    public abstract void OnPickedUp();

    private void Update()
    {
        if (playerInRange && !attachingToBack && !attached)
        {
            if (_playerInputs.interact)
            {
                // Save the original position of the item before starting the attachment process.
                originalPosition = transform.position;

                // Set the flag to start attaching the item to the player's back.
                attachingToBack = true;

                // TESTING PURPOSES
                Debug.Log($"Picked up: {objectiveName} - {description}");
                
                // Set the attached bool
                attached = true;
                Debug.Log(attached);

                // Call the pick up logic.
                OnPickedUp();

                // consume the input
                _playerInputs.interact = false;
            }
        }
        if (attachingToBack)
        {
            // timer to keep track of time elapsed
            factor += Time.deltaTime; // Increment the factor by the time elapsed since the last frame

            // Interpolate the position to the player's back with the factor counted by the ticker
            transform.position = Vector3.Lerp(originalPosition, playerBack.transform.position, factor);

            // Stop attaching after the target time (1s)
            if (factor >= 1f)
            {
                attachingToBack = false;

                // Snap the item to the back slot.
                transform.parent = playerBack.transform;
                transform.rotation = playerBack.transform.rotation;

                factor = 0f; // Reset factor for future use
            }


        }
    }
}
