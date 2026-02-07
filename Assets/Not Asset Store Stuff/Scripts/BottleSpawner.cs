using UnityEngine;
using StarterAssets;
using AH2714;
using System.Collections.Generic;
public class BottleSpawner : MonoBehaviour
{
    private BoxCollider Collider;
    private bool playerInRange;
    private StarterAssetsInputs _playerInputs;
    private GameObject _player;
    [SerializeField]
    private GameObject bottlePrefab;

    private void Awake()
    {
        Collider = GetComponent<BoxCollider>();
        _player = GameObject.FindWithTag("Player");
        _playerInputs = _player.GetComponent<StarterAssetsInputs>();
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

    private void Update()
    {
        if (playerInRange)
        {
            if (_playerInputs.interact)
            {
                // Instantiate a new bottle at the spawner's position and rotation.
                GameObject bottle = Instantiate(bottlePrefab, transform.position, transform.rotation);
                bottle.name = "Bottle of void";
                AH2714.Bottle bottleScript = bottle.GetComponent<AH2714.Bottle>();
                bottleScript.UpdateContents(0.5f);
                bottleScript.volume = 1f;
                bottleScript.color = Color.black;

                List<Bottle> bottles = new List<Bottle>(FindObjectsByType<Bottle>(FindObjectsSortMode.None));
                foreach (Bottle b in bottles)
                {
                    Debug.Log($"Found a {b.name} with volume: " + b.volume);
                }


                // consume the input
                _playerInputs.interact = false;
            }
        }
    }
}