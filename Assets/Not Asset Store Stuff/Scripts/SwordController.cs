using UnityEngine;
using System.Collections.Generic;
using Ilumisoft.HealthSystem;

public class SwordController : MonoBehaviour
{
    [Tooltip("The base damage this sword will deal on hit. This can be updated by the player controller.")]
    public float Damage = 25f;

    private Collider _swordCollider;
    private List<HealthComponent> _hitTargets;

    // --- NEW: Tracks if the sword is currently swinging and active ---
    private bool _isHitboxActive = false;

    private Collider DamageCollider
    {
        get
        {
            if (_swordCollider == null)
            {
                _swordCollider = GetComponent<CapsuleCollider>();

                if (_swordCollider == null)
                {
                    Debug.LogError("FATAL: SwordController could not find a Collider on this object.", this);
                }
            }
            return _swordCollider;
        }
    }


    private void Awake()
    {
        _hitTargets = new List<HealthComponent>();

        // Force collider initialization to prevent NullRef
        Collider temp = DamageCollider;

        // Ensure the collider starts disabled
        if (DamageCollider != null)
        {
            DamageCollider.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Safety check: Don't process hits if we are not marked as active
        if (!_isHitboxActive) return;

        HealthComponent targetHealth = other.GetComponentInParent<HealthComponent>();

        if (targetHealth == null)
        {
            return;
        }

        if (_hitTargets.Contains(targetHealth))
        {
            Debug.Log("Prevented re-hit on: " + targetHealth.gameObject.name, this);
            return;
        }

        // Apply Damage and track the target.
        Debug.Log("APPLIED DAMAGE to: " + targetHealth.gameObject.name, this);
        targetHealth.ApplyDamage(Damage);

        _hitTargets.Add(targetHealth);
    }

    // Called by an Animation Event at the start of the swing
    public void HitboxActivate()
    {
        // --- MODIFIED LOGIC ---
        // ONLY clear the list if the hitbox is NOT already active.
        // This stops multiple activation events from resetting the list mid-swing.
        if (!_isHitboxActive)
        {
            _hitTargets.Clear();
        }

        if (DamageCollider != null)
        {
            DamageCollider.enabled = true;
        }

        _isHitboxActive = true;
    }

    // Called by an Animation Event at the end of the swing
    public void HitboxDeactivate()
    {
        if (DamageCollider != null)
        {
            DamageCollider.enabled = false;
        }

        // --- NEW --- Mark the hitbox as inactive
        _isHitboxActive = false;
    }
}