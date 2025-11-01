using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(CharacterController), typeof(ThirdPersonController), typeof(Animator))]
public class WaterRunning : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the ThirdPersonController script. Will be auto-assigned.")]
    public ThirdPersonController playerController;

    [Tooltip("The Animator component. Will be auto-assigned.")]
    public Animator animator;

    [Header("Water Surface")]
    [Tooltip("The fixed Y-coordinate of the flat water surface. Set this in the Inspector.")]
    [SerializeField] public float waterSurfaceLevel = 0.0f;

    [Header("Water Running Settings")]
    [Tooltip("Minimum 'MotionSpeed' value from the Animator to enable running on water.")]
    [SerializeField] private float minAnimatorSpeedToRunOnWater = 2.0f;

    [Tooltip("How far above the water surface the player should run.")]
    [SerializeField] private float waterRunHeight = 0.2f;

    [Tooltip("How strongly the player is pushed to the 'waterRunHeight'. Higher = stiffer, springier.")]
    [SerializeField] private float waterSurfaceStickiness = 60f;

    [Tooltip("How much to dampen the 'stickiness' force. Prevents bouncing. Higher = more dampening.")]
    [SerializeField] private float waterDampingFactor = 10f;

    [Tooltip("Speed reduction when running on water (0 = no reduction, 1 = full stop).")]
    [Range(0f, 1f)]
    [SerializeField] private float waterDragAmount = 0.1f;

    [Header("Sinking Settings")]
    [Tooltip("Speed reduction when in the water (0 = no reduction, 1 = full stop).")]
    [Range(0f, 1f)]
    [SerializeField] private float sinkingHorizontalDrag = 0.95f;

    [Tooltip("Vertical speed reduction when sinking (0 = no reduction, 1 = full stop).")]
    [Range(0f, 1f)]
    [SerializeField] private float sinkingVerticalDrag = 0.5f;

    [Header("Splash Effect Pool")]
    [Tooltip("The prefab to use for water splash particle systems.")]
    public GameObject waterSplashPrefab;

    [Tooltip("How many splash particle systems to pre-instantiate.")]
    public int SplashPoolSize = 3;

    [Tooltip("Parent object to organize splash effects under (optional, for hierarchy organization).")]
    public Transform SplashParent;

    [Tooltip("How far in front of the player to spawn splash effects (in units).")]
    public float splashForwardOffset = 2.0f;

    [Tooltip("How much to add to the offset based on player speed. (FinalOffset = baseOffset + speed * scalar)")]
    [SerializeField] private float splashSpeedScalar = 0.3f;

    [Tooltip("The maximum distance the splash can spawn, to prevent it from getting too far at extreme speeds.")]
    [SerializeField] private float splashMaxDistance = 15.0f;

    [SerializeField] private float splashInterval = 0.1f;

    private GameObject[] _splashPool;
    private int _currentSplashIndex = 0;
    private bool _isRunningOnWater = false;
    private bool _wasRunningOnWater = false;
    private float _lastSplashTime;
    public bool isNearWater = false;

    public bool IsRunningOnWater => _isRunningOnWater;

    void Start()
    {
        playerController = GetComponent<ThirdPersonController>();
        animator = GetComponent<Animator>();

        // --- Initialize The Splash Object Pool ---
        if (waterSplashPrefab != null)
        {
            _splashPool = new GameObject[SplashPoolSize];
            for (int i = 0; i < SplashPoolSize; i++)
            {
                _splashPool[i] = Instantiate(waterSplashPrefab, transform.position, Quaternion.identity);

                if (SplashParent != null)
                {
                    _splashPool[i].transform.parent = SplashParent;
                }

                _splashPool[i].SetActive(false); // Turn them off initially
            }
        }
    }

    void Update()
    {
        if (playerController == null || animator == null)
        {
            return;
        }

        // --- Get Player State ---
        float motionSpeed = animator.GetFloat("MotionSpeed");
        bool isRunningFast = motionSpeed >= minAnimatorSpeedToRunOnWater;
        float playerY = transform.position.y;

        // --- Water Surface Check ---
        float waterSurfaceY = waterSurfaceLevel;
        float distanceToWater = playerY - waterSurfaceY; // Positive = Above, Negative = Below

        // We activate water physics if the player is within a reasonable vertical range of the surface.
        float activationHeightAboveWater = 5f;
        float activationDepthBelowWater = -100f;

        isNearWater = distanceToWater <= activationHeightAboveWater && distanceToWater >= activationDepthBelowWater;

        // --- Ground Check (The Fix) ---
        Vector3 spherePosition = new Vector3(transform.position.x, playerY - playerController.GroundedOffset, transform.position.z);

        // Check for solid ground (Physics.CheckSphere)
        bool physicsCheckGrounded = Physics.CheckSphere(spherePosition, playerController.GroundedRadius, playerController.GroundLayers, QueryTriggerInteraction.Ignore);

        // CRITICAL FIX: isGroundedOnLand is ONLY true if the ground detected is ABOVE the water level.
        // This stops the player from "standing" on the submerged floor.
        bool isGroundedOnLand = physicsCheckGrounded && (playerY <= waterSurfaceY + waterRunHeight || playerY > waterSurfaceY);


        // --- State Reset ---
        playerController.OverrideGrounded = false;
        _isRunningOnWater = false;

        // --- Main Logic (RESTRUCTURED for land-to-water transition) ---
        if (isNearWater)
        {
            if (isRunningFast)
            {
                // **PRIORITY 1: Land-to-Water Transition and Water Running**
                // We engage water running if the player is running fast AND is either:
                // a) NOT on land OR 
                // b) Just running onto the water surface (playerY is close to waterSurfaceY)
                if (!isGroundedOnLand || playerY <= waterSurfaceY + playerController.GroundedOffset + waterRunHeight)
                {
                    RunOnWater(waterSurfaceY);
                }
            }

            if (!isGroundedOnLand && !_isRunningOnWater)
            {
                // **PRIORITY 2: Sinking/Floating**
                // If the player is NOT on solid land and NOT running on water, they sink/float.
                SinkIntoWater(distanceToWater);
            }
        }

        // --- Handle State Changes ---
        HandleStateChange();
    }

    private void RunOnWater(float waterSurfaceY)
    {
        _isRunningOnWater = true;

        // Tell the ThirdPersonController we are "Grounded" on the water.
        playerController.OverrideGrounded = true;

        float targetY = waterSurfaceY + waterRunHeight;
        float currentY = transform.position.y;
        float yDifference = targetY - currentY;

        float currentVerticalVelocity = playerController.VerticalVelocity;

        // Proportional-Derivative (PD) Controller
        float proportionalForce = yDifference * waterSurfaceStickiness;
        float dampingForce = -currentVerticalVelocity * waterDampingFactor;

        float newVerticalVelocity = currentVerticalVelocity + (proportionalForce + dampingForce) * Time.deltaTime;

        playerController.VerticalVelocity = newVerticalVelocity;

        // Horizontal Drag
        ApplyHorizontalDrag(waterDragAmount);

        // Effects
        SpawnSplashEffects();
    }

    private void SinkIntoWater(float distanceToWater)
    {
        // We are NOT running on water, so the ThirdPersonController's Gravity WILL be applied.

        // We only apply drag to slow the player's horizontal speed when sinking.
        // We DO NOT apply vertical drag here. The TPC's Gravity() method handles vertical movement.

        // Apply drag if the player is near or below the surface.
        // This ensures the initial "gliding" is killed and swimming drag is applied.
        if (distanceToWater < 0.2f)
        {
            // Horizontal Drag: Kills horizontal momentum when not running.
            ApplyHorizontalDrag(sinkingHorizontalDrag);

            // Immediately kill residual speed if movement input has stopped (to stop creeping).
            if (playerController.CurrentSpeed < 0.1f)
            {
                playerController.CurrentSpeed = 0.0f;
            }
        }

        // --- NEW: Handle vertical drag separately for true sinking ---
        if (distanceToWater < 0) // If the player's center is below the surface
        {
            float currentVerticalVelocity = playerController.VerticalVelocity;

            // Apply light vertical drag/resistance to simulate buoyancy slowing gravity
            // This is the only vertical interference we allow when sinking.
            if (currentVerticalVelocity < 0)
            {
                // Note: We use a different drag amount for vertical movement if needed,
                // but for simplicity, we use the sinkingVerticalDrag property.
                playerController.VerticalVelocity = ApplyDrag(currentVerticalVelocity, sinkingVerticalDrag);
            }
        }
    }

    private void ApplyHorizontalDrag(float dragAmount)
    {
        float currentSpeed = playerController.CurrentSpeed;
        playerController.CurrentSpeed = ApplyDrag(currentSpeed, dragAmount);
    }

    /// <summary>
    /// Applies a frame-rate-independent drag to a value.
    /// </summary>
    private float ApplyDrag(float value, float dragAmount)
    {
        if (dragAmount <= 0f) return value;
        // This formula provides a frame-rate independent drag
        return value * Mathf.Pow(1f - dragAmount, Time.deltaTime * 60f);
    }

    private void SpawnSplashEffects()
    {
        if (_splashPool == null || _splashPool.Length == 0) return;

        if (Time.time - _lastSplashTime > splashInterval)
        {
            // 1. Get the next available splash object in the cycle
            GameObject splash = _splashPool[_currentSplashIndex];

            // 2. Cycle the index for the next use
            _currentSplashIndex = (_currentSplashIndex + 1) % SplashPoolSize;


            // 3. Calculate the dynamic offset based on current speed
            float currentSpeed = playerController.CurrentSpeed;
            float dynamicOffset = splashForwardOffset + (currentSpeed * splashSpeedScalar);

            // 4. Clamp the offset to the maximum allowed distance
            dynamicOffset = Mathf.Min(dynamicOffset, splashMaxDistance);

            // 5. Position the splash in front of the player's forward direction
            Vector3 forwardOffset = transform.forward * dynamicOffset;

            // --- MODIFICATION END ---

            Vector3 splashPos = transform.position + forwardOffset + Vector3.up * 0.1f;
            splash.transform.position = splashPos;
            splash.transform.rotation = Quaternion.identity;

            // 6. Activate or restart the particle system
            ParticleSystem ps = splash.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // If already active, just restart it
                if (splash.activeSelf)
                {
                    ps.Stop();
                    ps.Clear();
                }
                else
                {
                    splash.SetActive(true);
                }

                // Immediately stop/clear any remnants from a previous life
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }
            else
            {
                // Fallback if no ParticleSystem component
                splash.SetActive(true);
            }

            _lastSplashTime = Time.time;
        }
    }

    private void HandleStateChange()
    {
        if (_isRunningOnWater && !_wasRunningOnWater)
        {
            OnStartRunningOnWater();
        }
        else if (!_isRunningOnWater && _wasRunningOnWater)
        {
            OnStopRunningOnWater();
        }

        _wasRunningOnWater = _isRunningOnWater;
    }

    private void OnStartRunningOnWater()
    {
        animator.SetBool("IsRunningOnWater", true);
    }

    private void OnStopRunningOnWater()
    {
        animator.SetBool("IsRunningOnWater", false);
    }
}