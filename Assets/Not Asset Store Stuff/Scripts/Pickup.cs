using UnityEngine;
using StarterAssets;
public abstract class Pickup : MonoBehaviour
{
    public abstract string objectiveName { get; }
    public abstract string description { get; }
    protected abstract BoxCollider pickupCollider { get; }
    protected static GameObject deliveryObj;

    private bool playerInRange;
    private float factor;
    private Vector3 originalPosition;
    private Vector3 deliverLocation;
    private Transform playerLocation;
    private bool attachingToBack;
    private static GameObject playerBack;
    private static StarterAssetsInputs _playerInputs;
    private static GameObject _player;
    private static bool attached;
    private static float distanceToDelivery;

    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _playerInputs = _player.GetComponent<StarterAssetsInputs>();
        playerBack = GameObject.FindGameObjectWithTag("PlayerBack");
        playerLocation = _player.transform;
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
        if (attachingToBack)
        {
            // timer to keep track of time elapsed
            factor += Time.deltaTime; // Increment the factor by the time elapsed since the last frame

            // Interpolate the position to the player's back with the factor counted by the ticker
            transform.position = Vector3.Lerp(originalPosition, playerBack.transform.position, factor);

            // Spin the object smoothly while attaching
            transform.Rotate(0, 15f, 0);

            // Stop attaching after the target time (1s)
            if (factor >= 1f)
            {
                attachingToBack = false;
                Debug.Log(attached);

                // Snap the item to the back slot.
                transform.parent = playerBack.transform;
                transform.rotation = playerBack.transform.rotation;

                factor = 0f; // Reset factor for future use
            }

            return;
        }

        if (attached)
        {
            distanceToDelivery = Vector3.Distance(playerLocation.position, deliverLocation);
            if (distanceToDelivery <= 5 && _playerInputs.interact)
            {
                // Call the deliver method
                Deliver();

                // Consume input
                _playerInputs.interact = false;

                // Debug
                Debug.Log("Delivered");
            }

            return;
        }

        if (playerInRange  && _playerInputs.interact)
        {
                // Save the original position of the item before starting the attachment process.
                originalPosition = transform.position;

                // Set the flag to start attaching the item to the player's back.
                attachingToBack = true;

                // TESTING PURPOSES
                Debug.Log($"Picked up: {objectiveName} - {description}");

                // Call the pick up logic on the item.
                OnPickedUp();
                
                // Set the flag for attached
                attached = true;

                // Set the delivery location
                deliverLocation = deliveryObj.transform.position;

                // consume the input
                _playerInputs.interact = false;
        }

    }
    public void Deliver()
    {
        attached = false;
        Debug.Log("Disabling object");
        gameObject.SetActive(false);
    }
}
