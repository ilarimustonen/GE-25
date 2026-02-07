using UnityEngine;
using StarterAssets;
public class ObjectiveItem : MonoBehaviour
{
    [SerializeField]
    private string objectiveName;
    [SerializeField]
    private string description;

    [SerializeField]
    private GameObject prefab;

    private GameObject playerBack;
    private BoxCollider pickupCollider;
    private bool playerInRange;
    private StarterAssetsInputs _playerInputs;
    private GameObject _player;
    private bool attachingToBack;
    private float factor;
    Vector3 originalPosition;

    private void Awake()
    {
        pickupCollider = GetComponent<BoxCollider>();
        _player = GameObject.FindWithTag("Player");
        _playerInputs = _player.GetComponent<StarterAssetsInputs>();
        playerBack = GameObject.FindWithTag("PlayerBack");
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == _player)
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject == _player)
        {
            playerInRange = false;
        }
    }

    private void Update()
    {
        if(playerInRange)
        {
            if (_playerInputs.interact)
            {
                // Save the original position of the item before starting the attachment process.
                originalPosition = transform.position;

                // Set the flag to start attaching the item to the player's back.
                attachingToBack = true;

                // TESTING PURPOSES
                Debug.Log($"Picked up: {objectiveName} - {description}");

                // consume the input
                _playerInputs.interact = false;
            }
        }
        if (attachingToBack)
        {
            // timer to keep track of time elapsed
            factor += Time.deltaTime; // Increment the factor by the time elapsed since the last frame
            Vector3 playerBackPosition = playerBack.transform.position;
            // Interpolate the position to the player's back with the factor counted by the ticker
            transform.position = Vector3.Lerp(originalPosition, playerBackPosition, factor);
            // Stop attaching after the target time (1s)
            if (factor >= 1f)
            {
                attachingToBack = false;
                Instantiate(prefab, playerBackPosition, Quaternion.identity, playerBack.transform);
                factor = 0f; // Reset factor for future use
                gameObject.SetActive(false); // Deactivate the original item
            }


        }
    }
}
