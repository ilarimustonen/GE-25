using UnityEngine;
using StarterAssets;
public abstract class PowerUp : MonoBehaviour
{
    public abstract string powerupName { get; }
    protected abstract SphereCollider pickupCollider { get; }

    protected abstract float powerupTime {  get; }

    private bool playerInRange;
    private float counter;
    public static GameObject _player;
    public static ThirdPersonController _controller;
    private static bool powerupActive;
    private MeshRenderer[] renderers;
    private bool disabled = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _controller = _player.GetComponent<ThirdPersonController>();
        renderers = GetComponentsInChildren<MeshRenderer>();
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
    public abstract void OnEnd();

    private void Update()
    {
        if (powerupActive)
        {
            // Count the ticker for the powerup's duration
            counter += Time.deltaTime; // Increment the counter by the time elapsed since the last frame

            // Stop attaching after the target time)
            if (counter >= powerupTime)
            {
                powerupActive = false;
                OnEnd();
                Debug.Log("powerup " + powerupName + " ended");
                counter = 0f; // Reset counter for future use
            }
            return;
        }

        if (playerInRange && !disabled)
        {

            // TESTING PURPOSES
            Debug.Log($"Picked up powerup: {powerupName}");

            // Call the pick up logic on the item.
            OnPickedUp();

            // Set the flag for activation
            powerupActive = true;

            // Disable the rendering and pickup of the powerup
            foreach (MeshRenderer r in renderers) { r.enabled = false; }

            disabled = true;


        }

    }
}
