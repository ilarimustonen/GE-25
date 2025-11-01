using UnityEngine;
using Ilumisoft.HealthSystem;
using StarterAssets;
public class SuperSonicCollider : MonoBehaviour
{
    [Tooltip("The cooldown time in seconds for the supersonic force blast.")]
    public float ForceBlastCooldown = 5f;

    private PlayerCombat _combatManager;
    private BoxCollider superSonicTrigger;
    private HitboxComponent otherHitBoxComponent;
    private Rigidbody otherRigidBodyComponent;
    public float _forceBlastTimeoutDelta { get; set; }

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
        // Get reference to the combat manager.
        _combatManager = GetComponent<PlayerCombat>();

        // Get the set force blast cooldown from the combat manager.
        ForceBlastCooldown = _combatManager.WorldBlastCooldown;

        // Initialize cooldown timer
        _forceBlastTimeoutDelta = ForceBlastCooldown;

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

        // Also set the normal force blast to be on cooldown.
        _combatManager._forceBlastTimeoutDelta = _combatManager.ForceBlastCooldown;

        // Call the player controller to spawn a force blast on the position of the hit enemy.
        if (isEnemy) _combatManager.TriggerBlastEffect(otherHitBoxComponent.transform.position);
        else _combatManager.TriggerBlastEffect(otherRigidBodyComponent.transform.position);

    }
    public void SonicHitboxActivate() { superSonicCollider.enabled = true; }
    public void SonicHitboxDeactivate() { superSonicCollider.enabled = false; }
}