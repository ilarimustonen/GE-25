using UnityEngine;
using Ilumisoft.HealthSystem;
using System.Collections;
using StarterAssets;
public class PlayerCombat : MonoBehaviour
{
    [Header("Player Combat")]
    public SwordController swordController;

    [Tooltip("Attack damage")]
    public float AttackDamage = 75f;

    [Tooltip("Attack cooldown")]
    public float AttackCooldown = 1.3f;

    [Header("Weapon Switching")]
    [Tooltip("The sword attached to the spine/back (cosmetic).")]
    public GameObject swordOnBack;

    [Tooltip("The sword attached to the hand (functional).")]
    public GameObject swordInHand;

    [Header("Force Blast")]
    [Tooltip("The force of the blast pushing objects away.")]
    public float ForceBlastForce = 1500f;

    [Tooltip("Max blast damage to hitboxes")]
    public float maxDamage = 100.0f;

    [Tooltip("The radius of the blast effect.")]
    public float ForceBlastRadius = 5f;

    [Tooltip("An upward force applied to objects to make them fly up a bit.")]
    public float ForceBlastUpwardsModifier = 1.5f;

    [Tooltip("The cooldown time in seconds for the force blast.")]
    public float ForceBlastCooldown = 2f;

    [Tooltip("The cooldown time in seconds for world-positioned blasts.")]
    public float WorldBlastCooldown = 0.5f;

    [Tooltip("VFX and AOE size multiplier for world-positioned blasts.")]
    public float WorldBlastScale = 3f;

    [Tooltip("Audio multiplier for world-positioned blasts.")]
    public float WorldBlastAudioMultiplier = 1.2f;

    [Tooltip("Blast force multiplier for world-positioned blasts.")]
    public float WorldBlastForceMultiplier = 1.5f;

    [Tooltip("Blast damage multiplier for world-positioned blasts.")]
    public float WorldBlastDamageMultiplier = 1.2f;



    [Header("Force Blast Object Pool")]
    [Tooltip("How many ForceBlastVFX objects to pre-instantiate.")]
    public int PoolSize = 5;
    private GameObject[] _vfxPool;
    private int _currentVfxIndex = 0;

    [Tooltip("Vertical offset for the force blast VFX spawn position relative to the player.")]
    public Vector3 ForceBlastVFXOffset = new Vector3(0f, 1f, 0f);

    [Tooltip("The VFX prefab to spawn when the blast occurs.")]
    public GameObject ForceBlastVFX;

    [Tooltip("How long the VFX prefab will exist before being destroyed (in seconds).")]
    public float VFXLifetime = 3f;

    // ability timeout deltatime
    public float _forceBlastTimeoutDelta { get; set; }
    public float _attackTimeoutDelta { get; set; }
    public bool _isAttacking { get; set; }

    // Player controller reference
    private ThirdPersonController _thirdPersonController;

    // Audio manager reference
    private PlayerAudioManager _audioManager;

    // Speedster VFX manager reference
    private SpeedsterVFXManager _speedsterVFXManager;

    // Animator reference
    private Animator _animator;

    // Super sonic collider script reference
    private SuperSonicCollider _superSonicColliderScript;
    private Collider _PlayerCollider;
    private Collider[] EnemyColliders;

    // Animation IDs
    private int _animIDForceBlast;
    private int _animIDAttack;

    // Force blast colliders
    Collider[] blastColliders;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get component references
        _audioManager = GetComponent<PlayerAudioManager>();
        _superSonicColliderScript = GetComponent<SuperSonicCollider>();
        _PlayerCollider = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _thirdPersonController = GetComponent<ThirdPersonController>();
        _speedsterVFXManager = GetComponent<SpeedsterVFXManager>();

        // Get assign the animation IDs
        AssignAnimationIDs();

        // Initialize the force blast VFX pool
        if (ForceBlastVFX != null)
        {
            _vfxPool = new GameObject[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                _vfxPool[i] = Instantiate(ForceBlastVFX, transform.position, Quaternion.identity, transform);
                _vfxPool[i].SetActive(false); // Turn them off
            }
        }
        // Reset timeouts on start
        _forceBlastTimeoutDelta = ForceBlastCooldown;
        _attackTimeoutDelta = AttackCooldown;
    }

    // Update is called once per frame
    void Update()
    {
        // Handle ability cooldown
        if (_forceBlastTimeoutDelta >= 0.0f)
        {
            _forceBlastTimeoutDelta -= Time.deltaTime;
        }

        // Handle attack cooldown
        if (_attackTimeoutDelta >= 0.0f)
        {
            _attackTimeoutDelta -= Time.deltaTime;
        }
    }

    public void SpeedsterEffects()
    {
        bool enableEffects = (_thirdPersonController._animationBlend >= _speedsterVFXManager.SpeedEffectThreshold);

        if (enableEffects)
        {
            // Enable the speedster force blast
            HandleSuperSonicForceBlast(true);
            // Disable the enemy-player collision when moving fast
            Collider[] EnemyColliders = GatherEnemyColliders();
            SetEnemyCollision(EnemyColliders, true);
        }
        else
        {
            // Disable the speedster force blast
            HandleSuperSonicForceBlast(false);

            // Re-enable the enemy-player collision when moving slow
            Collider[] EnemyColliders = GatherEnemyColliders();
            SetEnemyCollision(EnemyColliders, false);
        }

    }

    public void TriggerBlastEffect(Vector3 forceBlastPos)
    {
        bool isWorldBlast = false;
        if (!Vector3.Equals(forceBlastPos, Vector3.zero))
        {
            isWorldBlast = true;
        }

        // --- VFX POOL LOGIC ---
        if (_vfxPool != null && _vfxPool.Length > 0)
        {
            // 1. Get the next available VFX object in the cycle
            GameObject spawnedVFX = _vfxPool[_currentVfxIndex];

            // 2. Cycle the index for the next use
            _currentVfxIndex = (_currentVfxIndex + 1) % PoolSize;

            // 3. Position and setup the object
            if (isWorldBlast)
            {
                spawnedVFX.transform.parent = null; // Unparent first
                spawnedVFX.transform.position = forceBlastPos;
                spawnedVFX.transform.localScale = new Vector3(WorldBlastScale, WorldBlastScale, WorldBlastScale);
            }
            else
            {
                Vector3 spawnPosition = transform.position + ForceBlastVFXOffset;
                spawnedVFX.transform.parent = transform; // Re-parent
                spawnedVFX.transform.position = spawnPosition;
                spawnedVFX.transform.rotation = Quaternion.identity;
                spawnedVFX.transform.localScale = new Vector3(1f, 1f, 1f);
            }

            // 4. Activate the VFX
            spawnedVFX.SetActive(true);

            // 5. Play audio after activation
            _audioManager.PlayActionSound(PlayerAudioManager.ActionSoundType.ForceBlastWorld);

            // 6. Start the Coroutine to deactivate it after its lifetime
            StartCoroutine(DeactivateVFXAfterTime(spawnedVFX, VFXLifetime));
        }

        // Find all colliders within the blast radius
        if (isWorldBlast)
            blastColliders = Physics.OverlapSphere(forceBlastPos, ForceBlastRadius * WorldBlastScale); // Use scaled radius too
        else
            blastColliders = Physics.OverlapSphere(transform.position, ForceBlastRadius);

        // Play the sword hit sound if there's at least one hitbox in the array
        if (blastColliders.Length > 0)
        {
            _audioManager.PlayActionSound(PlayerAudioManager.ActionSoundType.SwordHit);
        }

        // Apply force to each collider
        foreach (Collider hit in blastColliders)
        {
            // Try to get the specialized enemy controller first
            EnemyAIController enemyAI = hit.GetComponent<EnemyAIController>();

            if (enemyAI != null)
            {
                // Use the correct blast origin position
                Vector3 blastOrigin = isWorldBlast ? forceBlastPos : transform.position;
                if (isWorldBlast)
                    enemyAI.ApplyKnockback(blastOrigin, ForceBlastForce * WorldBlastForceMultiplier, ForceBlastRadius * WorldBlastScale, ForceBlastUpwardsModifier);
                else
                    enemyAI.ApplyKnockback(blastOrigin, ForceBlastForce, ForceBlastRadius, ForceBlastUpwardsModifier);
            }
            else
            {
                // Check if it's a simple physics object
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    Vector3 blastOrigin = isWorldBlast ? forceBlastPos : transform.position;
                    if (isWorldBlast)
                        rb.AddExplosionForce(ForceBlastForce * WorldBlastForceMultiplier, blastOrigin, ForceBlastRadius * WorldBlastScale, ForceBlastUpwardsModifier, ForceMode.Impulse);
                    else
                        rb.AddExplosionForce(ForceBlastForce, blastOrigin, ForceBlastRadius, ForceBlastUpwardsModifier, ForceMode.Impulse);
                }
            }

            HitboxComponent hitbox = hit.GetComponent<HitboxComponent>();
            if (hitbox != null && !hitbox.gameObject.CompareTag("Player"))
            {
                // Use correct blast origin for distance calculation
                Vector3 blastOrigin = isWorldBlast ? forceBlastPos : transform.position;
                float distance = Vector3.Distance(blastOrigin, hit.transform.position);
                float radius = isWorldBlast ? ForceBlastRadius * WorldBlastScale : ForceBlastRadius;

                float damageFalloff = 1 - (distance / radius);
                float calculatedDamage;

                if (isWorldBlast)
                    calculatedDamage = (maxDamage * WorldBlastDamageMultiplier) * damageFalloff;
                else
                    calculatedDamage = maxDamage * damageFalloff;

                if (calculatedDamage > 0)
                {
                    hitbox.ApplyDamage(calculatedDamage);
                }
            }
        }
    }

    private IEnumerator DeactivateVFXAfterTime(GameObject vfxObject, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Only deactivate if it hasn't been re-activated by a subsequent blast
        if (vfxObject.activeSelf)
        {
            vfxObject.SetActive(false);
        }
    }

    public void HandleForceBlast()
    {
        // Reset the cooldown timer
        _forceBlastTimeoutDelta = ForceBlastCooldown;

        // Also reset the cooldown timer for world-positioned blasts
        _superSonicColliderScript._forceBlastTimeoutDelta = WorldBlastCooldown;

        // Trigger animation
        _animator.SetTrigger(_animIDForceBlast);

    }
    public void HandleAttack()
    {
        // Set attacking state
        _isAttacking = true;

        // Reset the cooldown timer
        _attackTimeoutDelta = AttackCooldown;

        // Trigger animation
        _animator.applyRootMotion = true; // Enable root motion for the attack
        _animator.SetTrigger(_animIDAttack);
        
    }

    // Called by the Animation Event in the 'Gunslinger_Attack04' clip
    public void Unsheathe()
    {
        if (swordOnBack != null)
            swordOnBack.SetActive(false); // Hide the cosmetic sword

        if (swordInHand != null)
            swordInHand.SetActive(true);  // Show the functional sword in the hand

        // Send the set damage value to the sword controller
        if (swordController != null)
        {
            swordController.Damage = AttackDamage;
        }
        // Play sword swing sound
        _audioManager.PlayActionSound(PlayerAudioManager.ActionSoundType.SwordSwing);
    }

    public void HitboxActivate()
    {
        if (swordController != null)
        {
            // Delegates the command to the actual sword script.
            swordController.HitboxActivate();
        }
    }

    public void HitboxDeactivate()
    {
        if (swordController != null)
        {
            // Delegates the command to the actual sword script.
            swordController.HitboxDeactivate();
        }
    }


    public void Sheathe()
    {
        if (swordController != null)
        {
            // Sword switching logic
            if (swordInHand != null)
                swordInHand.SetActive(false); // Hide the functional sword

            if (swordOnBack != null)
                swordOnBack.SetActive(true);  // Show the cosmetic sword again
        }
    }
    public void EndAttack()
    {
        _animator.applyRootMotion = false; // Disable root motion after the attack
        _isAttacking = false; // Reset attacking state
    }

    private void HandleSuperSonicForceBlast(bool ForceBlastToggle)
    {
        if (ForceBlastToggle) _superSonicColliderScript.SonicHitboxActivate();
        else _superSonicColliderScript.SonicHitboxDeactivate();
    }
    public void TriggerBlastEffectAnimCall() { TriggerBlastEffect(Vector3.zero); }

    public void SetEnemyCollision(Collider[] enemyColliders, bool ignore)
    {
        if (_PlayerCollider != null && enemyColliders != null)
        {
            foreach (Collider enemyCollider in enemyColliders)
            {
                // This tells the physics engine to ignore collision between these two colliders.
                Physics.IgnoreCollision(_PlayerCollider, enemyCollider, ignore);
            }
        }
    }

    private Collider[] GatherEnemyColliders()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Collider[] enemyColliders = new Collider[enemies.Length];
        for (int i = 0; i < enemies.Length; i++)
        {
            enemyColliders[i] = enemies[i].GetComponent<Collider>();
        }
        return enemyColliders;
    }

    private void AssignAnimationIDs()
    {
        _animIDForceBlast = Animator.StringToHash("ForceBlast");
        _animIDAttack = Animator.StringToHash("Attack");
    }
}
