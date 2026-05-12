using StarterAssets;
using UnityEngine;
public class ObjectiveCollider : MonoBehaviour, IInteractable
{

    private bool playerInRangeWithChargedItem;
    private StarterAssetsInputs playerInputs;
    private GameObject player;
    private Pickup item;
    public bool CanInteract => true;
    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerInputs = player.GetComponent<StarterAssetsInputs>();
    }

    private void OnTriggerEnter(Collider other)
    {
        try 
        { 
            item = other.GetComponentInChildren<Pickup>();
            if (!Pickup.Charged) { return; }
            playerInRangeWithChargedItem = true;
            Debug.Log("Player entered range with charged item"); 
        }
        catch { Debug.Log("other is not specifically a player with a charged item"); }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) { playerInRangeWithChargedItem = false; Debug.Log("Player left range"); }
    }

    private void Update()
    {
        if (!playerInRangeWithChargedItem || !playerInputs.interact) return;
        playerInputs.interact = false; 
        Interact(this.gameObject);
    }

    public void Interact(GameObject interactor)
    {
        item.Deliver();
    }
}
