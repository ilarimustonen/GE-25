using UnityEngine;
using StarterAssets;
public class ObjectiveCollider : MonoBehaviour
{

    private bool playerInRangeWithItem;
    private StarterAssetsInputs playerInputs;
    private GameObject player;
    private Pickup item;
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
            playerInRangeWithItem = true;
            Debug.Log("Player entered range with item"); 
        }
        catch { Debug.Log("other is not player with item"); }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) { playerInRangeWithItem = false; Debug.Log("Player left range"); }
    }

    private void Update()
    {
        if (!playerInRangeWithItem) return;
        if (playerInputs.interact) { playerInputs.interact = false; OnInteract(); }
    }

    private void OnInteract()
    {
        item.Deliver();
    }
}
