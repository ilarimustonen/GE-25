using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAIController : MonoBehaviour
{
    // --- Public Fields (Editable in Inspector) ---
    [Header("AI Settings")]
    public float detectionRadius = 15f;
    public float attackRadius = 2f;

    [Tooltip("A layer mask to specify what can block the AI's line of sight.")]
    public LayerMask obstacleMask;

    [Header("Attack Settings")]
    public float attackCooldown = 2f; // Time in seconds between attacks

    // --- Private Fields ---
    private Transform player;
    private NavMeshAgent agent;
    private Animator animator; // Optional: for animations

    // State Machine
    private enum State { Idle, Chasing, Attacking }
    private State currentState;

    // Attack timing
    private float lastAttackTime;

    void Start()
    {
        // Find the player GameObject by its tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("Player not found! Make sure your player has the 'Player' tag.");
        }

        // Get components attached to this GameObject
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); // Can be null if no animator

        // Set initial state
        currentState = State.Idle;
    }

    void Update()
    {
        if (player == null) return; // Don't run AI if there's no player

        // The core of our AI: a state machine
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
        }
    }

    // --- State Handlers ---

    private void HandleIdleState()
    {
        // Optional: Trigger idle animation
        if (animator != null) animator.SetBool("IsWalking", false);

        // Check if the player is within detection radius and has line of sight
        if (IsPlayerInSight())
        {
            currentState = State.Chasing;
        }
    }

    private void HandleChasingState()
    {
        // Optional: Trigger walking/running animation
        if (animator != null) animator.SetBool("IsWalking", true);

        // Move towards the player
        agent.SetDestination(player.position);

        // If player is out of sight, go back to idle
        if (!IsPlayerInSight())
        {
            currentState = State.Idle;
            agent.ResetPath(); // Stop moving
            return;
        }

        // If player is within attack range, switch to attacking state
        if (Vector3.Distance(transform.position, player.position) <= attackRadius)
        {
            currentState = State.Attacking;
        }
    }

    private void HandleAttackingState()
    {
        // Stop moving to attack
        agent.ResetPath();

        // Optional: Stop walking animation
        if (animator != null) animator.SetBool("IsWalking", false);

        // Make sure the enemy is facing the player
        transform.LookAt(player);

        // Check if enough time has passed since the last attack
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }

        // If the player moves out of attack range, go back to chasing
        if (Vector3.Distance(transform.position, player.position) > attackRadius)
        {
            currentState = State.Chasing;
        }
    }

    // --- Helper Methods ---

    private bool IsPlayerInSight()
    {
        // First check: Is player within the detection radius?
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRadius)
        {
            return false;
        }

        // Second check: Is there a clear line of sight?
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        // We use a Raycast to check for obstacles between the enemy and the player.
        if (Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask))
        {
            // If the raycast hits an obstacle, there is no line of sight.
            return false;
        }

        // If both checks pass, the player is in sight.
        return true;
    }

    private void Attack()
    {
        // This is where your attack logic goes.
        // e.g., dealing damage, playing an attack animation, creating a projectile.

        // Optional: Trigger attack animation
        if (animator != null) animator.SetTrigger("Attack");

        Debug.Log(gameObject.name + " is attacking " + player.name);

        // --- PLACE YOUR DAMAGE LOGIC HERE ---
        // Example: player.GetComponent<PlayerHealth>().TakeDamage(10);

        // Record the time of this attack to manage cooldown
        lastAttackTime = Time.time;
    }

    // Optional: Draw gizmos in the editor to visualize the AI's ranges
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}