using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Ilumisoft.HealthSystem;
using StarterAssets;
public class SuperSonicCollider : MonoBehaviour
{
    [Tooltip("The cooldown time in seconds for the supersonic force blast.")]
    public float ForceBlastCooldown = 5f;

    [Header("References")]
    [Tooltip("Reference to the ThirdPersonController script.")]
    public ThirdPersonController characterControllerScript;

    private BoxCollider superSonicTrigger;
    private HitboxComponent otherHitBoxComponent;
    private Rigidbody otherRigidBodyComponent;


    private float _forceBlastTimeoutDelta;

    // Trigger when an object with a hitbox enters the collider.
    private void OnTriggerEnter(Collider other)
    {
        //Check if the object has a HitboxComponent and is not the player
        otherHitBoxComponent = other.GetComponent<HitboxComponent>();
        if (otherHitBoxComponent != null && !other.gameObject.CompareTag("Player") && !other.gameObject.CompareTag("NPC") && _forceBlastTimeoutDelta <= 0.0f)
        {
            SpawnForceBlast(true);
        }
        else
        {
            // Also check if it's a simple physics object
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic && _forceBlastTimeoutDelta <= 0.0f && !other.gameObject.CompareTag("NPC"))
            {
                otherRigidBodyComponent = rb;
                SpawnForceBlast(false);
            }
        }
    }
    private Collider superSonicCollider
    {
        get
        {
            if (superSonicTrigger == null)
            {
                superSonicTrigger = GetComponent<BoxCollider>();

                if (superSonicTrigger == null)
                {
                    Debug.LogError("FATAL: Script could not find a Collider on this object.", this);
                }
            }
            return superSonicTrigger;
        }
    }
    private void Awake()
    {
        // Initialize cooldown timer
        _forceBlastTimeoutDelta = ForceBlastCooldown;

        // Ensure character controller reference is set
        if (characterControllerScript == null)
        {
            characterControllerScript = GetComponent<ThirdPersonController>();
            if (characterControllerScript == null)
            {
                Debug.LogError("ThirdPersonController component not found on " + gameObject.name);
            }
        }

        // Ensure the collider starts disabled
        if (superSonicCollider != null)
        {
            superSonicCollider.enabled = false;
        }
    }
    private void Update()
    {
        // Handle ability cooldown
        if (_forceBlastTimeoutDelta >= 0.0f)
        {
            _forceBlastTimeoutDelta -= Time.deltaTime;
        }
    }

    public void SpawnForceBlast(bool isEnemy)
    {
        // Reset the cooldown timer
        _forceBlastTimeoutDelta = ForceBlastCooldown;

        // Call the player controller to spawn a force blast on the position of the hit enemy.
        if (isEnemy) characterControllerScript.TriggerBlastEffect(otherHitBoxComponent.transform.position);
        else characterControllerScript.TriggerBlastEffect(otherRigidBodyComponent.transform.position);

    }
    public void SonicHitboxActivate() { superSonicCollider.enabled = true; }
    public void SonicHitboxDeactivate() { superSonicCollider.enabled = false; }
}