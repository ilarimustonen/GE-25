using UnityEngine;
using UnityEngine.AI;
using System.Collections; // Required for Coroutines

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyAIController : MonoBehaviour
{
    // ... (Your existing Header fields) ...
    [Header("AI Settings")]
    public float detectionRadius = 15f;
    public float attackRadius = 1f;
    public LayerMask obstacleMask;
    [Header("Attack Settings")]
    public float attackCooldown = 1f;

    // --- NEW KNOCKBACK SETTINGS ---
    [Header("Knockback Settings")]
    [Tooltip("The radius for the grounded check sphere.")]
    public float GroundedRadius = 0.3f;
    [Tooltip("The layers that are considered ground.")]
    public LayerMask GroundLayers;
    [Tooltip("Vertical offset for the grounded check sphere from the object's pivot.")]
    public float GroundedOffset = 0f;
    // ----------------------------

    // --- Private Fields ---
    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private Rigidbody rb;

    // State Machine
    private enum State { Idle, Chasing, Attacking, KnockedBack }
    private State currentState;

    private float lastAttackTime;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        else Debug.LogError("Player not found!");

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;

        agent.stoppingDistance = attackRadius;
        currentState = State.Idle;
    }

    void Update()
    {
        if (player == null || currentState == State.KnockedBack) return;

        switch (currentState)
        {
            case State.Idle:
                HandleIdleState();
                break;
            case State.Chasing:
                HandleChasingState();
                break;
            case State.Attacking:
                HandleAttackingState();
                break;
            case State.KnockedBack:
                // Logic is handled by the coroutine
                break;
        }
    }

    public void ApplyKnockback(Vector3 explosionPosition, float explosionForce, float explosionRadius, float explosionUpwardsModifier)
    {
        if (currentState == State.KnockedBack) return;
        StartCoroutine(KnockbackRoutine(explosionPosition, explosionForce, explosionRadius, explosionUpwardsModifier));
    }

    // --- NEW HELPER METHOD FOR GROUND CHECK ---
    private bool IsGrounded()
    {
        // Calculate the sphere position with the vertical offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);

        // Use Physics.CheckSphere to see if the feet are touching the ground layers
        return Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }
    // ----------------------------------------

    private IEnumerator KnockbackRoutine(Vector3 explosionPosition, float explosionForce, float explosionRadius, float explosionUpwardsModifier)
    {
        currentState = State.KnockedBack;

        // --- GRACEFUL SHUTDOWN OF NAVMESHAGENT ---
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.enabled = false;

        // --- GIVE CONTROL TO PHYSICS ---
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        bool wasRootMotionEnabled = animator.applyRootMotion;
        if (wasRootMotionEnabled)
        {
            animator.applyRootMotion = false;
        }

        // --- APPLY THE FORCE ---
        rb.AddExplosionForce(explosionForce, explosionPosition, explosionRadius, explosionUpwardsModifier, ForceMode.Impulse);

        // Allow physics to calculate force application before we start checking
        yield return new WaitForFixedUpdate();

        // --- MODIFIED WAIT FOR KNOCKBACK TO END ---
        float timer = 0f;
        float knockbackDuration = 5.0f; // Increased timeout to prevent permanent lockup if something goes wrong
        float velocityThreshold = 0.5f; // Slightly increased threshold for better responsiveness after landing

        while (timer < knockbackDuration)
        {
            // NEW CONDITION: Only break if it is grounded AND has slowed down.
            if (IsGrounded() && rb.linearVelocity.magnitude < velocityThreshold)
            {
                break;
            }

            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // --- RETURN CONTROL TO NAVMESHAGENT ---
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        if (wasRootMotionEnabled)
        {
            animator.applyRootMotion = true;
        }

        agent.enabled = true;

        // --- IMPORTANT: WARP TO THE NEW POSITION ---
        if (agent.isOnNavMesh)
        {
            agent.Warp(transform.position);
        }
        else
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning(gameObject.name + " was knocked too far from the NavMesh.");
            }
        }

        agent.isStopped = false;
        currentState = State.Chasing;
    }


    // ... (The rest of your methods like HandleIdleState, HandleChasingState, etc., remain the same) ...

    #region State Handlers and Helpers
    private void HandleIdleState()
    {
        if (animator != null) animator.SetBool("IsIdle", true);
        agent.isStopped = true;
        if (IsPlayerInSight())
        {
            currentState = State.Chasing;
        }
    }

    private void HandleChasingState()
    {
        if (animator != null) animator.SetBool("IsIdle", false);
        agent.isStopped = false;
        agent.SetDestination(player.position);
        if (!IsPlayerInSight())
        {
            currentState = State.Idle;
            return;
        }
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = State.Attacking;
        }
    }

    private void HandleAttackingState()
    {
        if (animator != null) animator.SetBool("IsIdle", true);
        agent.isStopped = true;
        FaceTarget();
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
        if (Vector3.Distance(transform.position, player.position) > attackRadius)
        {
            currentState = State.Chasing;
        }
    }

    private void FaceTarget()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * agent.angularSpeed);
    }

    private bool IsPlayerInSight()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRadius) return false;
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask)) return false;
        return true;
    }

    private void Attack()
    {
        if (animator != null) animator.SetTrigger("Attack");
        lastAttackTime = Time.time;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
    #endregion
}