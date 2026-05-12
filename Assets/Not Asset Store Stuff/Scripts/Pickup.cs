using UnityEngine;
using StarterAssets;
public abstract class Pickup : MonoBehaviour, IInteractable
{
    public abstract string objectiveName { get; }
    public abstract string description { get; }
    public abstract float maxTravelledDistance { get; }
    protected abstract BoxCollider pickupCollider { get; }

    protected static GameObject deliveryObj;

    private bool playerInRange;
    private float counter;
    private Vector3 originalPosition;
    private static Vector3 playerPreviousPos;
    private static float distanceTravelled;
    private bool attachingToBack;
    private static GameObject playerBack;
    private static StarterAssetsInputs _playerInputs;
    private static GameObject _player;
    private static bool attached;
    private static bool charged;
    private ChargeMeter ChargeMeter;
    public static bool Charged { get { return charged; } }
    public static float DistanceTraveled { get { return distanceTravelled; } }
    public bool CanInteract => !attached && !attachingToBack;



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
    public abstract void OnGrab();

    private void Update()
    {
        if (attachingToBack)
        {
            // timer to keep track of time elapsed
            counter += Time.deltaTime; // Increment the counter by the time elapsed since the last frame

            // Interpolate the position to the player's back with the counter counted by the ticker
            transform.position = Vector3.Lerp(originalPosition, playerBack.transform.position, counter);

            // Spin the object smoothly while attaching
            transform.Rotate(0, 15f, 0);

            // Stop attaching after the target time (1s)
            if (counter >= 1f)
            {
                attachingToBack = false;
                Debug.Log(attached);

                // Snap the item to the back slot.
                transform.parent = playerBack.transform;
                transform.rotation = playerBack.transform.rotation;

                counter = 0f; // Reset counter for future use
            }

            return;
        }

        if (playerInRange  && _playerInputs.interact && CanInteract)
        {
                Attach();
                Interact(_player);
        }

    }
    public void Interact(GameObject interactor)
    {
        OnPickUp();
        OnGrab();
        attached = true;
        _playerInputs.interact = false;
    }
    private void Attach()
    {
        // Save the original position of the item before starting the attachment process.
        originalPosition = transform.position;

        // Set the flag to start attaching the item to the player's back.
        attachingToBack = true;
    }

    private void FixedUpdate()
    {
        if (attached && !attachingToBack && !charged && transform.parent == playerBack.transform)
        {
            if (playerPreviousPos == new Vector3(0, 0, 0))
            {
                playerPreviousPos = _player.transform.position;
            }

            distanceTravelled += Vector3.Distance(playerPreviousPos, _player.transform.position);
            Debug.Log("Player has traveled " + distanceTravelled);

            if (distanceTravelled >= maxTravelledDistance)
            {
                charged = true;
                Debug.Log("charged");
            }

            playerPreviousPos = _player.transform.position;
        }
    }
    public void Deliver()
    {
        attached = false;
        charged = false;
        distanceTravelled = 0f;
        playerPreviousPos = new Vector3(0, 0, 0);
        Debug.Log("Delivered object");
        gameObject.SetActive(false);
    }

    private void OnPickUp()
    {
        ChargeMeter = GameObject.FindGameObjectWithTag("ChargeMeter").GetComponent<ChargeMeter>();
        ChargeMeter.maxCharge = maxTravelledDistance;
        ChargeMeter.pickup = this;
    }
}
