using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    Transform player;

    void Start()
    {
        player = FindFirstObjectByType<StarterAssets.ThirdPersonController>().transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = player.transform.position;
    }
}