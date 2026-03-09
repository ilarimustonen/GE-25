using UnityEngine;
using StarterAssets;
public abstract class Pickup : MonoBehaviour
{
    public abstract string objectiveName { get; }

    public abstract string description { get; }

    protected abstract GameObject Prefab { get; }

    protected abstract string itemTag { get; }

    protected abstract BoxCollider pickupCollider { get; }

    private GameObject playerBack;
    private Vector3 playerBackPos;

    private bool playerInRange;
    private StarterAssetsInputs _playerInputs;
    private GameObject _player;
    private bool attachingToBack;
    private float factor;
    Vector3 originalPosition;
    private GameObject item;

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
        if (playerInRange)
        {
            if (_playerInputs.interact)
            {
                // Save the original position of the item before starting the attachment process.
                originalPosition = transform.position;

                // Set the flag to start attaching the item to the player's back.
                attachingToBack = true;

                // TESTING PURPOSES
                Debug.Log($"Picked up: {objectiveName} - {description}");
                
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

            // The position of the back is -0.2 from the player's position on the x-axis, and 1 on the y-axis , so we can use the player's position and add the offset to it by using the player's forward direction.
            Vector3 playerBackPosition = _player.transform.position - _player.transform.forward * 0.2f + Vector3.up * 1f;

            // Interpolate the position to the player's back with the factor counted by the ticker
            transform.position = Vector3.Lerp(originalPosition, playerBackPosition, factor);

            // Interpolate the rotation to match the player's rotation reversed, but only on the Y-axis to keep the item upright.
            Quaternion targetRotation = Quaternion.Euler(0, (_player.transform.eulerAngles.y + 180f), 0);

            // Stop attaching after the target time (1s)
            if (factor >= 1f)
            {
                attachingToBack = false;

                // Enable item on the back.
                item = GameObject.FindWithTag(itemTag);
                try { item.GetComponent<SkinnedMeshRenderer>().enabled = true; }
                catch { item.GetComponent<MeshRenderer>().enabled = true; }

                factor = 0f; // Reset factor for future use
                gameObject.SetActive(false); // Deactivate the original item
            }


        }
    }
}
