using UnityEngine;

public class PlayerCutsceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void TeleportTo(Transform target)
    {
        // Instantly move the player to the target's position and rotation
        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}
