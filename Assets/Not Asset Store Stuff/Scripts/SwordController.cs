using UnityEngine;
using System.Collections.Generic;
using Ilumisoft.HealthSystem; // Use the namespace from your health system asset

public class SwordController : MonoBehaviour
{
    [Tooltip("The base damage this sword will deal on hit. This can be updated by the player controller.")]
    public float Damage = 25f;

    private Collider _swordCollider;
    private List<Collider> _hitTargets; // List to track targets hit in a single swing

    private Collider DamageCollider
    {
        get
        {
            if (_swordCollider == null)
            {
                // This is the safety check that runs just-in-time
                _swordCollider = GetComponent<CapsuleCollider>();

                if (_swordCollider == null)
                {
                    Debug.LogError("FATAL: SwordController could not find a Capsule Collider on this object.", this);
                }
            }
            return _swordCollider;
        }
    }


    private void Awake()
    {
        // Initialization for other non-component variables is safe here
        _hitTargets = new List<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // If we have already hit this target in the current swing, ignore it
        if (_hitTargets.Contains(other))
        {
            return;
        }

        // First, try to find a HitboxComponent on the object we struck.
        if (other.TryGetComponent<HitboxComponent>(out var hitbox))
        {
            hitbox.ApplyDamage(Damage);
            _hitTargets.Add(other);
            Debug.Log($"Hit {other.name}'s hitbox, dealing {Damage} damage.");
        }
        // If no hitbox is found, check for a root HealthComponent as a fallback.
        else if (other.TryGetComponent<HealthComponent>(out var health))
        {
            health.ApplyDamage(Damage);
            _hitTargets.Add(other);
            Debug.Log($"Hit {other.name}, dealing {Damage} damage.");
        }
    }

    // Called by an Animation Event at the start of the swing
    public void EnableCollider()
    {
        _hitTargets.Clear();
        if (_swordCollider != null)
        {
            _swordCollider.enabled = true;
        }
    }

    // Called by an Animation Event at the end of the swing
    public void DisableCollider()
    {
        if (_swordCollider != null)
        {
            _swordCollider.enabled = false;
        }
    }
}