 using UnityEngine;
 using Ilumisoft.HealthSystem;
 using System.Collections;
 using Cinemachine;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine.Rendering;
using System;




#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 10.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 15f;

        [Tooltip("The speed threshold after which infinite acceleration begins.")]
        public float InitialMaxSprintSpeed = 50.0f;

        [Tooltip("Time in seconds to accelerate from base SprintSpeed to InitialMaxSprintSpeed.")]
        public float SprintAccelerationTime = 10.0f;

        [Tooltip("Speed increase per second after reaching InitialMaxSprintSpeed (linear acceleration).")]
        public float InfiniteAccelerationRate = 5.0f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Header("External Control")]
        [Tooltip("Allows external scripts to override the controller's grounded state.")]
        public bool OverrideGrounded { get; set; } = false;

        [Tooltip("Allows external scripts to read/write the vertical velocity.")]
        public float VerticalVelocity { get => _verticalVelocity; set => _verticalVelocity = value; }

        [Tooltip("Allows external scripts to read/write the current horizontal speed.")]
        public float CurrentSpeed { get => _speed; set => _speed = value; }

        [Header("Speedster Physics")]
        [Tooltip("The 'stick to ground' force at zero speed.")]
        public float BaseGroundGravity = -2.0f;

        [Tooltip("The 'stick to ground' force at InitialMaxSprintSpeed to prevent flying off slopes.")]
        public float MaxSpeedGroundGravity = -50.0f;

        [Tooltip("Additional gravity per m/s beyond InitialMaxSprintSpeed (for infinite speed scaling).")]
        public float GravityScalingRate = 0f;

        [Tooltip("The slope limit (in degrees) at zero speed.")]
        public float MinSlopeLimit = 45.0f;

        [Tooltip("The maximum slope limit (in degrees) at InitialMaxSprintSpeed.")]
        public float MaxSlopeLimit = 80.0f;

        [Tooltip("Additional slope limit per m/s beyond InitialMaxSprintSpeed (for infinite speed scaling).")]
        public float SlopeLimitScalingRate = 0.5f;

        [Tooltip("The 'animationBlend' value (0-3) at which max gravity and slope limit are applied.")]
        public float MaxSpeedThreshold = 2.0f;

        [Header("Speedster Effects")]
        [Tooltip("The 'animationBlend' value (e.g., 2.0) to activate speed effects.")]
        public float SpeedEffectThreshold = 2.0f;

        [Tooltip("The 'animationBlend' value at which max brightness is reached.")]
        public float MaxBrightnessThreshold = 2.5f;

        [Tooltip("Reference to the GameObject containing the Motion Blur (e.g., a Post-Processing Volume).")]
        public GameObject SpeedEffectParent;

        [Tooltip("The prefab to spawn as a 'clone' or 'afterimage'.")]
        public GameObject ClonePrefab;

        [Tooltip("How often a clone is spawned (in seconds) when at max speed.")]
        public float CloneSpawnRate = 0.1f;

        [Tooltip("Minimum fade time for clones at low speed.")]
        public float MinCloneFadeTime = 0.05f;

        [Tooltip("Maximum fade time for clones at max speed.")]
        public float MaxCloneFadeTime = 0.3f;

        [Tooltip("Brightness multiplier at low speed (dimmer).")]
        public float MinBrightness = 0.3f;

        [Tooltip("Brightness multiplier at max speed (brighter).")]
        public float MaxBrightness = 1.5f;

        [Header("Clone Object Pool")]
        [Tooltip("How many clone objects to pre-instantiate.")]
        public int ClonePoolSize = 5;
        private GameObject[] _clonePool;

        [Tooltip("Parent object to organize clones under (optional, for hierarchy organization).")]
        public Transform CloneParent;

        [Header("Global Audio")]
        [Range(0, 1)] public float GlobalAudioVolume = 1f;

        [Header("Footstep Audio Stuff")]
        public AudioSource FootstepSource;
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        public AudioClip[] WaterFootstepAudioClips;

        [Range(0, 1)] public float WaterFootstepAudioVolume = 0.5f;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Tooltip("Speed (m/s) at which footstep audio starts to lag behind.")]
        public float FootstepDelayStartSpeed = 30.0f;

        [Tooltip("Maximum delay for footstep sounds at max speed (in seconds).")]
        public float MaxFootstepDelay = 0.5f;

        [Tooltip("Speed (m/s) at which footsteps become inaudible (outrun the sound).")]
        public float FootstepSilenceSpeed = 55.0f;

        [Header("Supersonic Stuff")]
        [Tooltip("Audio source for the looping supersonic/wind sound effect.")]
        public AudioSource SupersonicSource;

        [Tooltip("The looping sound clip to play at high speeds.")]
        public AudioClip SupersonicLoopClip;

        [Tooltip("Speed (m/s) at which the supersonic sound starts playing.")]
        public float SupersonicStartSpeed = 25.0f;

        [Tooltip("Speed (m/s) at which the supersonic sound reaches full volume.")]
        public float SupersonicMaxSpeed = 75.0f;

        [Range(0, 1)]
        [Tooltip("Maximum volume for the supersonic loop.")]
        public float SupersonicMaxVolume = 0.8f;

        [Tooltip("The camera to change the fov of during supersonic speeds.")]
        public CinemachineVirtualCamera Camera;

        [Tooltip("Max field of view during supersonic speed")]
        public float MaxFOV = 90.0f;

        [Tooltip("Max lens distortion during supersonic speed")]
        public float MaxLensD = -0.5f;

        [Tooltip("Max motion blur values during supersonic speed")]
        public float MaxMotionBlur = 1.0f;
        public float MaxMotionBlurClamp = 0.2f;

        [Tooltip("Max chromatic aberration during supersonic speed")]
        public float MaxChromaticAberration = 1.0f;

        [Tooltip("How quickly to interpolate the speed effect changes.")]
        public float SpeedEffectChangeSpeed = 0.1f;

        [Header("Combat Audio Stuff")]
        public AudioClip SwordSwingAudioClip;
        public AudioClip SwordHitAudioClip;
        public AudioClip ForceBlastAudioClip;
        public AudioSource ActionSource1;
        public AudioSource ActionSource2;
        public AudioSource ActionSource3;
        public AudioSource ActionSource4;

        private AudioSource[] _actionSources;
        [Range(0, 1)] public float ForceBlastAudioVolume = 0.5f;
        [Range(0, 1)] public float SwordSwingAudioVolume = 0.5f;
        [Range(0, 1)] public float SwordHitAudioVolume = 0.5f;

        [Header("Player Movement Variables")]
        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("How long the jump input will be buffered (in seconds). Allows jumping before landing.")]
        public float JumpBufferTime = 0.2f;

        [Tooltip("How long you can still jump after walking off a ledge (in seconds).")]
        public float CoyoteTime = 0.2f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Player Combat")]
        public SwordController swordController;

        [Header("Attack speed")]
        [Tooltip("The cooldown time in seconds for the sword attack.")]
        public float AttackCooldown = 3f;

        [Header("Weapon Switching")]
        [Tooltip("The sword attached to the spine/back (cosmetic).")]
        public GameObject swordOnBack;

        [Tooltip("The sword attached to the hand (functional).")]
        public GameObject swordInHand;

        [Header("Force Blast")]
        [Tooltip("The force of the blast pushing objects away.")]
        public float ForceBlastForce = 1000f;

        [Tooltip("Max blast damage to hitboxes")]
        public float maxDamage = 100.0f;

        [Tooltip("The radius of the blast effect.")]
        public float ForceBlastRadius = 5f;

        [Tooltip("An upward force applied to objects to make them fly up a bit.")]
        public float ForceBlastUpwardsModifier = 1.5f;

        [Tooltip("The cooldown time in seconds for the force blast.")]
        public float ForceBlastCooldown = 2f;

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

        [Header("Supersonic Force Blast")]

        // ability timeout deltatime
        private float _forceBlastTimeoutDelta;
        private float _attackTimeoutDelta;
        private bool _isAttacking;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _currentSprintTime = 0.0f;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float _jumpBufferTimer;
        private float _coyoteTimer;
        private float _intendedTargetSpeed = 0.0f;
        private SkinnedMeshRenderer[] _playerSkinnedMeshes;
        private float MainFOV;
        private SuperSonicCollider _superSonicColliderScript;
        private Collider _PlayerCollider;
        private Collider[] EnemyColliders;
        private Volume _SpeedsterVolume;
        private WaterRunning _waterRunningScript;

        // Queue to store pending footstep sounds
        private System.Collections.Generic.Queue<FootstepData> _pendingFootsteps = new System.Collections.Generic.Queue<FootstepData>();

        // Helper struct to store footstep data
        private struct FootstepData
        {
            public AudioClip clip;
            public float volume;
            public Vector3 position;
            public float playTime;
        }

        // Force blast colliders
        Collider[] blastColliders;

        // clone spawn pool
        private int _clonePoolIndex; // The current index for the circular pool
        private float _cloneSpawnTimer; // Timer is still needed

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDForceBlast;
        private int _animIDAttack;

        // cutscene flag
        public bool cutsceneRunning;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _playerSkinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
            _superSonicColliderScript = GetComponent<SuperSonicCollider>();
            _PlayerCollider = GetComponent<CharacterController>();
            _SpeedsterVolume = SpeedEffectParent.GetComponent<Volume>();
            _waterRunningScript = GetComponent<WaterRunning>();

            if (_playerSkinnedMeshes.Length == 0)
            {
                Debug.LogError("Player is missing SkinnedMeshRenderers. Clone effect will not work.", this);
            }

            // Check the FOV of the camera
            if (Camera != null)
            {
                MainFOV = Camera.m_Lens.FieldOfView;
            }

            // Initialize the action audio sources array
            _actionSources = new AudioSource[] { ActionSource1, ActionSource2, ActionSource3, ActionSource4 };

            // Initialize the VFX pool
            if (ForceBlastVFX != null)
            {
                _vfxPool = new GameObject[PoolSize];
                for (int i = 0; i < PoolSize; i++)
                {
                    _vfxPool[i] = Instantiate(ForceBlastVFX, transform.position, Quaternion.identity, transform);
                    _vfxPool[i].SetActive(false); // Turn them off
                }
            }

            // --- Initialize The Clone Object Pool ---
            if (ClonePrefab != null)
            {
                _clonePool = new GameObject[ClonePoolSize];
                for (int l = 0; l < ClonePoolSize; l++)
                {
                    _clonePool[l] = Instantiate(ClonePrefab, transform.position, Quaternion.identity);

                    if (CloneParent != null)
                    {
                        _clonePool[l].transform.parent = CloneParent;
                    }

                    _clonePool[l].SetActive(false); // Turn them off
                }
            }

            // --- Supersonic Audio Pre-Warming ---
            if (SupersonicSource != null && SupersonicLoopClip != null)
            {
                // 1. Assign the clip and configure the source
                SupersonicSource.clip = SupersonicLoopClip;
                SupersonicSource.loop = true;
                SupersonicSource.volume = 0f; // Start silent

                // 2. Play and immediately stop to force load/buffer initialization (Pre-warm)
                SupersonicSource.Play();
                SupersonicSource.Stop();

                // Note: For some platforms, calling Play() and then Stop() immediately may not be enough.
                // A safer alternative is to call source.time = float.MaxValue before Play(), 
                // but Play/Stop is usually sufficient for pre-buffering.
            }

            // Force-load combat audio clips into memory to prevent
            // audio gaps on first use (especially for pooled/disabled sources).
            if (ForceBlastAudioClip != null)
            {
                ForceBlastAudioClip.LoadAudioData();
            }
            if (SwordSwingAudioClip != null)
            {
                SwordSwingAudioClip.LoadAudioData();
            }
            if (SwordHitAudioClip != null)
            {
                SwordHitAudioClip.LoadAudioData();
            }


#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _forceBlastTimeoutDelta = ForceBlastCooldown;
            _attackTimeoutDelta = AttackCooldown;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

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

            // Check for force blast input
            if (_input.forceBlast && _forceBlastTimeoutDelta <= 0.0f && !_isAttacking)
            {
                HandleForceBlast();
            }

            if (_input.attack && _attackTimeoutDelta <= 0.0f)
            {
                _isAttacking = true;
                HandleAttack();
            }
            GroundedCheck();
            JumpAndGravity();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDForceBlast = Animator.StringToHash("ForceBlast");
            _animIDAttack = Animator.StringToHash("Attack");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);

            // check if grounded
            bool physicsGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);
            Grounded = physicsGrounded || OverrideGrounded;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            float targetSpeed;

            // If attacking, stop horizontal movement.
            Vector2 moveInput = _input.move;
            if (_isAttacking)
            {
                moveInput = Vector2.zero;
            }

            if (moveInput == Vector2.zero)
            {
                // No input: Stop
                targetSpeed = 0.0f;
                _intendedTargetSpeed = 0.0f;
                _currentSprintTime = 0.0f; // Reset sprint timer
            }
            else if (_input.sprint)
            {
                // Sprinting: Calculate intended target speed based purely on time
                _currentSprintTime += Time.deltaTime;

                if (_currentSprintTime <= SprintAccelerationTime)
                {
                    // Phase 1: Accelerate from SprintSpeed to InitialMaxSprintSpeed
                    float sprintLerp = Mathf.Clamp01(_currentSprintTime / SprintAccelerationTime);
                    _intendedTargetSpeed = Mathf.Lerp(SprintSpeed, InitialMaxSprintSpeed, sprintLerp);
                }
                else
                {
                    // Phase 2: Infinite linear acceleration beyond InitialMaxSprintSpeed
                    float timeAfterMax = _currentSprintTime - SprintAccelerationTime;
                    _intendedTargetSpeed = InitialMaxSprintSpeed + (InfiniteAccelerationRate * timeAfterMax);
                }

                targetSpeed = _intendedTargetSpeed;
            }
            else
            {
                // Walking
                targetSpeed = MoveSpeed;
                _intendedTargetSpeed = MoveSpeed;
                _currentSprintTime = 0.0f; // Reset sprint timer
            }
            // --- END TARGET SPEED LOGIC ---


            if (_isAttacking && Grounded)
            {
                _speed = 0.0f; // Force instant stop
            }

            // Use _speed (our tracked speed) instead of CharacterController velocity for calculations
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? moveInput.magnitude : 1f;

            // Accelerate or decelerate to the target speed
            if (!_isAttacking || Grounded)
            {
                if (_speed < targetSpeed - speedOffset ||
                    _speed > targetSpeed + speedOffset)
                {
                    // Lerp the speed using our tracked _speed, not the CharacterController velocity
                    _speed = Mathf.Lerp(_speed, targetSpeed * inputMagnitude,
                        Time.deltaTime * SpeedChangeRate);

                    // Round to 3 decimal places
                    _speed = Mathf.Round(_speed * 1000f) / 1000f;
                }
                else
                {
                    _speed = targetSpeed;
                }
            }

            // --- ANIMATION BLEND LOGIC (Remaps 0-30 speed to 0-3 range) ---
            float animationTarget = 0.0f;

            if (_speed > 0.0f)
            {
                if (_speed <= MoveSpeed)
                {
                    // We are between Idle and Walk (Remap 0-6 to 0-1)
                    animationTarget = Mathf.InverseLerp(0.0f, MoveSpeed, _speed);
                }
                else if (_speed <= SprintSpeed)
                {
                    // We are between Walk and Run (Remap 6-12 to 1-2)
                    animationTarget = 1.0f + Mathf.InverseLerp(MoveSpeed, SprintSpeed, _speed);
                }
                else
                {
                    float clampedSpeed = Mathf.Min(_speed, InitialMaxSprintSpeed);
                    animationTarget = 2.0f + Mathf.InverseLerp(SprintSpeed, InitialMaxSprintSpeed, clampedSpeed);
                }
            }

            // Lerp the animation blend value for smooth transitions
            _animationBlend = Mathf.Lerp(_animationBlend, animationTarget, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // --- THIS IS THE FIX from the previous step ---
            // Send the final blend value to the animator
            if (_hasAnimator)
            {
                // Use _animIDMotionSpeed, NOT _animationBlend (which is the float value)
                _animator.SetFloat(_animIDMotionSpeed, _animationBlend);
            }

            // Handle speed-based effects and physics adjustments
            HandleSpeedsterUpdates();
            ProcessDelayedFootsteps();
            HandleSupersonicAudio();
            HandleCameraFovChanges(Camera);

            // normalise input direction
            Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

            // if there is a move input rotate player when the player is moving
            if (moveInput != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                    _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            // Calculate the direction we want to move in based on the camera
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // Apply the final movement to the CharacterController
            // This single line handles both horizontal movement (_speed) and vertical movement (_verticalVelocity)
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            // --- Handle Coyote Time ---
            if (Grounded)
            {
                _coyoteTimer = CoyoteTime; // Reset coyote timer when on the ground
            }
            else
            {
                _coyoteTimer -= Time.deltaTime; // Tick down coyote timer when in the air
            }

            // --- Handle Jump Input Buffering ---
            if (_input.jump)
            {
                _jumpBufferTimer = JumpBufferTime; // Set the buffer timer when jump is pressed
                _input.jump = false; // Consume the input immediately
            }
            else
            {
                _jumpBufferTimer -= Time.deltaTime; // Tick down buffer timer
            }

            // --- The Actual Jump Logic ---
            // A jump can occur if the buffer is active AND we are either grounded or within the coyote time window.
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                // Calculate jump velocity
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                // Update animator
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }

                // Reset timers to prevent re-jumping
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
            }

            // --- Gravity and Fall State ---
            else if (Grounded)
            {
                // Reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // Update animator
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f && Grounded && !OverrideGrounded)
                {
                    // Calculate gravity based on actual speed (not animation blend)
                    float groundGravity;

                    if (_speed <= InitialMaxSprintSpeed)
                    {
                        // Phase 1: Lerp from base to max gravity up to InitialMaxSprintSpeed
                        float speedPercent = Mathf.InverseLerp(0.0f, InitialMaxSprintSpeed, _speed);
                        groundGravity = Mathf.Lerp(BaseGroundGravity, MaxSpeedGroundGravity, speedPercent);
                    }
                    else
                    {
                        // Phase 2: Continue scaling gravity beyond InitialMaxSprintSpeed
                        float speedBeyondMax = _speed - InitialMaxSprintSpeed;
                        groundGravity = MaxSpeedGroundGravity + (GravityScalingRate * speedBeyondMax);
                    }

                    _verticalVelocity = groundGravity;
                }
            }
            else
            {
                // Fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // Update animator
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }
            }

            // Apply gravity over time if under terminal velocity
            if (!Grounded && _verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
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
                    spawnedVFX.transform.localScale = new Vector3(3f, 3f, 3f);
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
                if (isWorldBlast) PlayClipFromPool(ForceBlastAudioClip, _actionSources, ForceBlastAudioVolume * 2f * GlobalAudioVolume);
                else PlayClipFromPool(ForceBlastAudioClip, _actionSources, ForceBlastAudioVolume * GlobalAudioVolume);

                // 6. Start the Coroutine to deactivate it after its lifetime
                StartCoroutine(DeactivateVFXAfterTime(spawnedVFX, VFXLifetime));
            }

            // Find all colliders within the blast radius
            if (isWorldBlast)
                blastColliders = Physics.OverlapSphere(forceBlastPos, ForceBlastRadius * 3f); // Use scaled radius too
            else
                blastColliders = Physics.OverlapSphere(transform.position, ForceBlastRadius);

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
                        enemyAI.ApplyKnockback(blastOrigin, ForceBlastForce * 3f, ForceBlastRadius * 3f, ForceBlastUpwardsModifier);
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
                            rb.AddExplosionForce(ForceBlastForce * 3f, blastOrigin, ForceBlastRadius * 3f, ForceBlastUpwardsModifier, ForceMode.Impulse);
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
                    float radius = isWorldBlast ? ForceBlastRadius * 3f : ForceBlastRadius;

                    float damageFalloff = 1 - (distance / radius);
                    float calculatedDamage;

                    if (isWorldBlast)
                        calculatedDamage = (maxDamage * 1.5f) * damageFalloff;
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

        private void HandleForceBlast()
        {
            // Reset the cooldown timer
            _forceBlastTimeoutDelta = ForceBlastCooldown;

            // Trigger animation
            if (_hasAnimator)
            {
                _animator.SetTrigger(_animIDForceBlast);
            }

        }
        private void HandleAttack()
        {

            // Reset the cooldown timer
            _attackTimeoutDelta = AttackCooldown;

            // Trigger animation
            if (_hasAnimator)
            {
                _animator.applyRootMotion = true; // Enable root motion for the attack
                _animator.SetTrigger(_animIDAttack);
            }
        }

        // Called by the Animation Event in the 'Gunslinger_Attack04' clip
        public void Unsheathe()
        {
            if (swordOnBack != null)
                swordOnBack.SetActive(false); // Hide the cosmetic sword

            if (swordInHand != null)
                swordInHand.SetActive(true);  // Show the functional sword in the hand

            PlayClipFromPool(SwordSwingAudioClip, _actionSources, SwordSwingAudioVolume * GlobalAudioVolume);
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
            if (_hasAnimator)
            {
                _animator.applyRootMotion = false; // Disable root motion after the attack
            }

            _isAttacking = false;
        }
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        // NEW HELPER FUNCTION: Plays a clip on the next available source from the provided pool.
        private void PlayClipFromPool(AudioClip clip, AudioSource[] sourcePool, float volume)
        {
            if (clip == null || sourcePool == null || sourcePool.Length == 0) return;

            // Cycle through the available sources to find one that is not playing
            AudioSource availableSource = null;
            foreach (var source in sourcePool)
            {
                if (source != null && !source.isPlaying)
                {
                    availableSource = source;
                    break;
                }
            }

            // Fallback: If no source is free, reuse the first one (will cut off previous sound)
            if (availableSource == null)
            {
                availableSource = sourcePool[0];
            }

            // Configure and play the sound
            availableSource.clip = clip;
            availableSource.volume = volume;
            availableSource.transform.position = transform.TransformPoint(_controller.center); // Set spatial position
            availableSource.Play();
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && !cutsceneRunning)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    if (_waterRunningScript.isNearWater)
                    {
                        // Pick a random water footstep clip
                        var index = UnityEngine.Random.Range(0, WaterFootstepAudioClips.Length);
                        PlayDelayedFootstep(WaterFootstepAudioClips[index], WaterFootstepAudioVolume * GlobalAudioVolume);
                    }
                    else
                    {
                        // Pick a random footstep clip
                        var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
                        PlayDelayedFootstep(FootstepAudioClips[index], FootstepAudioVolume * GlobalAudioVolume);
                    }
                    
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && !cutsceneRunning)
            {
                if (_waterRunningScript.isNearWater)
                {
                    // Play water landing sound
                    var index = UnityEngine.Random.Range(0, WaterFootstepAudioClips.Length);
                    PlayDelayedFootstep(WaterFootstepAudioClips[index], WaterFootstepAudioVolume * GlobalAudioVolume);
                }
                else PlayDelayedFootstep(LandingAudioClip, FootstepAudioVolume * GlobalAudioVolume);
            }
        }

        /// <summary>
        /// Calculates delay based on actual speed and queues the footstep sound.
        /// </summary>
        private void PlayDelayedFootstep(AudioClip clip, float volume)
        {
            if (clip == null) return;

            // If moving too fast, don't even queue the sound (outrun it completely)
            if (_speed >= FootstepSilenceSpeed)
            {
                return;
            }

            // Calculate delay based on current speed
            float delay = 0f;

            if (_speed >= FootstepDelayStartSpeed)
            {
                // Calculate how much delay based on speed percentage
                float speedPercent = Mathf.InverseLerp(FootstepDelayStartSpeed, FootstepSilenceSpeed, _speed);
                delay = Mathf.Lerp(0f, MaxFootstepDelay, speedPercent);
            }

            // Create footstep data
            FootstepData footstep = new FootstepData
            {
                clip = clip,
                volume = volume,
                position = transform.position, // Store where the footstep happened
                playTime = Time.time + delay
            };

            // Add to queue
            _pendingFootsteps.Enqueue(footstep);
        }
        public void CutsceneRunning()
        {
            cutsceneRunning = true;
        }
        public void CutsceneEnded()
        {
            cutsceneRunning = false;
        }

        /// <summary>
        /// Handles all speed-based effects and physics adjustments.
        /// This is called from the Move() method.
        /// </summary>
        /// <summary>
        /// Handles all speed-based effects and physics adjustments.
        /// This is called from the Move() method.
        /// </summary>
        private void HandleSpeedsterUpdates()
        {
            // --- 1. Dynamic Slope Limit with Infinite Scaling ---
            float newSlopeLimit;
            float speedPercent = Mathf.InverseLerp(0.0f, InitialMaxSprintSpeed, _speed);

            if (_speed <= InitialMaxSprintSpeed)
            {
                // Phase 1: Lerp from min to max slope limit up to InitialMaxSprintSpeed
                newSlopeLimit = Mathf.Lerp(MinSlopeLimit, MaxSlopeLimit, speedPercent);
            }
            else
            {
                // Phase 2: Continue scaling slope limit beyond InitialMaxSprintSpeed
                float speedBeyondMax = _speed - InitialMaxSprintSpeed;
                newSlopeLimit = MaxSlopeLimit + (SlopeLimitScalingRate * speedBeyondMax);
                // Cap at reasonable maximum (e.g., 89 degrees to avoid 90° vertical walls)
                newSlopeLimit = Mathf.Min(newSlopeLimit, 89.0f);
            }

            _controller.slopeLimit = newSlopeLimit;

            // --- 3. Handle Effects ---

            // Check if the _animationBlend is past the threshold
            bool showEffects = (_animationBlend >= SpeedEffectThreshold);

            // --- NEW Clone Spawner Logic ---
            if (ClonePrefab != null)
            {
                if (showEffects)
                {
                    // Use the showeffects flag to also control the threshold of the supersonic collider.
                    HandleSuperSonicForceBlast(true);

                    // Disable the player enemy collision when moving fast
                    Collider[] EnemyColliders = GatherEnemyColliders();
                    SetEnemyCollision(EnemyColliders, true);

                    // We are moving fast enough, so tick down the timer
                    _cloneSpawnTimer -= Time.deltaTime;

                    if (_cloneSpawnTimer <= 0f)
                    {
                        // Calculate how many clones to spawn this frame
                        // This allows spawning multiple clones if we're going REALLY fast
                        float currentSpawnRate = Mathf.Lerp(CloneSpawnRate * 2f, CloneSpawnRate, speedPercent);

                        // If timer is very negative, spawn multiple clones to fill the gap
                        int clonesToSpawn = Mathf.Max(1, Mathf.CeilToInt(-_cloneSpawnTimer / currentSpawnRate) + 1);

                        for (int i = 0; i < clonesToSpawn; i++)
                        {
                            SpawnClone();
                        }

                        // Reset timer
                        _cloneSpawnTimer = currentSpawnRate;
                    }
                }
                else
                {
                    // Not moving fast, so reset the timer
                    _cloneSpawnTimer = CloneSpawnRate;

                    // Use the showeffects flag to also control the threshold of the supersonic collider.
                    HandleSuperSonicForceBlast(false);

                    // Re-enable the player collider when moving slow
                    Collider[] EnemyColliders = GatherEnemyColliders();
                    SetEnemyCollision(EnemyColliders, false);
                }
            }

        }

        /// <summary>
        /// Grabs the next clone from the object pool and activates it.
        /// </summary>
        private void SpawnClone()
        {
            // Sanity checks
            if (_clonePool == null || _clonePool.Length == 0 || _playerSkinnedMeshes == null || _playerSkinnedMeshes.Length == 0)
            {
                return;
            }

            // Calculate speed percentage (0-1) based on animation blend
            float speedPercent = Mathf.InverseLerp(SpeedEffectThreshold, MaxBrightnessThreshold, _animationBlend);

            // Calculate dynamic fade time and brightness based on speed
            float fadeTime = Mathf.Lerp(MinCloneFadeTime, MaxCloneFadeTime, speedPercent);

            // --- REVISED BRIGHTNESS CALCULATION ---

            // 1. Calculate the total range of brightness desired (e.g., 2.0 - 0.5 = 1.5)
            float totalBrightnessRange = MaxBrightness - MinBrightness;

            // 2. Determine the brightness value based on the speed percentage within that range.
            // This value goes from 0 to totalBrightnessRange (e.g., 0 to 1.5).
            float brightnessOffset = totalBrightnessRange * speedPercent;

            // 3. The final multiplier is the MinBrightness (our floor/baseline) plus the offset.
            float finalBrightnessMultiplier = MinBrightness + brightnessOffset;

            // --- Object Pool Logic ---
            GameObject clone = _clonePool[_clonePoolIndex];
            _clonePoolIndex = (_clonePoolIndex + 1) % ClonePoolSize;

            // --- Setup the Clone ---
            clone.SetActive(false);

            clone.transform.position = transform.position;
            clone.transform.rotation = transform.rotation;

            CloneFade fadeScript = clone.GetComponent<CloneFade>();
            if (fadeScript != null)
            {
                // Pass the calculated parameters
                fadeScript.Initialize(_playerSkinnedMeshes, fadeTime, finalBrightnessMultiplier);
            }
            else
            {
                Debug.LogWarning("ClonePrefab is missing the 'CloneFade' script.", this);
            }

            clone.SetActive(true);
        }

        /// <summary>
        /// Processes the queue of delayed footstep sounds based on speed.
        /// </summary>
        private void ProcessDelayedFootsteps()
        {
            // Check if there are any pending footsteps
            while (_pendingFootsteps.Count > 0)
            {
                FootstepData footstep = _pendingFootsteps.Peek();

                // Check if it's time to play this footstep
                if (Time.time >= footstep.playTime)
                {
                    // Remove from queue
                    _pendingFootsteps.Dequeue();

                    // Play the sound at the stored position (3D spatial audio)
                    if (FootstepSource != null)
                    {
                        // Calculate distance-based volume (sound gets quieter if you're far away)
                        float distance = Vector3.Distance(transform.position, footstep.position);
                        float volumeFalloff = Mathf.Clamp01(1.0f - (distance / 10f)); // Audible within 10 units

                        // Only play if loud enough to hear
                        if (volumeFalloff > 0.01f)
                        {
                            AudioSource.PlayClipAtPoint(footstep.clip, footstep.position, footstep.volume * volumeFalloff);
                        }
                    }
                }
                else
                {
                    // Not time yet, stop checking
                    break;
                }
            }
        }

        /// <summary>
        /// Handles the looping supersonic sound effect based on speed.
        /// Fades in as speed increases, fades out as speed decreases.
        /// </summary>
        private void HandleSupersonicAudio()
        {
            if (SupersonicSource == null || SupersonicLoopClip == null) return;

            // Check if we're above the supersonic threshold
            if (_speed >= SupersonicStartSpeed)
            {
                // Start playing if not already playing
                if (!SupersonicSource.isPlaying)
                {
                    SupersonicSource.Play();
                }

                // Calculate volume based on speed (fade in from start to max)
                float speedPercent = Mathf.InverseLerp(SupersonicStartSpeed, SupersonicMaxSpeed, _speed);
                float targetVolume = Mathf.Lerp(0f, SupersonicMaxVolume, speedPercent) * GlobalAudioVolume;

                // Smoothly lerp the volume for fade in/out
                SupersonicSource.volume = Mathf.Lerp(SupersonicSource.volume, targetVolume, Time.deltaTime * 5f);
            }
            else
            {
                // Below threshold - fade out and stop
                if (SupersonicSource.isPlaying)
                {
                    // Fade out
                    SupersonicSource.volume = Mathf.Lerp(SupersonicSource.volume, 0f, Time.deltaTime * 10f);

                    // Stop once volume is very low
                    if (SupersonicSource.volume < 0.01f)
                    {
                        SupersonicSource.Stop();
                        SupersonicSource.volume = 0f;
                    }
                }
            }
        }

        private void HandleCameraFovChanges(CinemachineVirtualCamera camera)
        {
            if (camera == null) return;

            // Calculate speed percentage (0-1) based on animation blend
            float speedPercent = Mathf.InverseLerp(0.0f, MaxSpeedThreshold, _animationBlend);

            // Determine target FOV
            float targetFOV = Mathf.Lerp(MainFOV, MaxFOV, speedPercent);
            // Smoothly interpolate to the target FOV
            camera.m_Lens.FieldOfView = Mathf.Lerp(camera.m_Lens.FieldOfView, targetFOV, Time.deltaTime * SpeedEffectChangeSpeed);

            // Determine target lens distortion intensity
            float TargetLensDistortionIntensity = Mathf.Lerp(0, MaxLensD, speedPercent);

            // Smoothly interpolate the lens distortion intensity.
            _SpeedsterVolume.profile.TryGet(out UnityEngine.Rendering.Universal.LensDistortion lensDistortion);
            lensDistortion.intensity.value = Mathf.Lerp(lensDistortion.intensity.value, TargetLensDistortionIntensity, Time.deltaTime * (SpeedEffectChangeSpeed * 5));

            // Determine target motion blur intensity
            float TargetMotionBlurIntensity = Mathf.Lerp(0, MaxMotionBlur, speedPercent);

            // Smoothly interpolate the motion blur intensity.
            _SpeedsterVolume.profile.TryGet(out UnityEngine.Rendering.Universal.MotionBlur motionBlur);
            motionBlur.intensity.value = Mathf.Lerp(motionBlur.intensity.value, TargetMotionBlurIntensity, Time.deltaTime * (SpeedEffectChangeSpeed * 5));
            
            // Determine target motion blur clamp.
            float TargetMotionBlurClamp = Mathf.Lerp(0, MaxMotionBlurClamp, speedPercent);
            motionBlur.clamp.value = Mathf.Lerp(motionBlur.clamp.value, TargetMotionBlurClamp, Time.deltaTime * (SpeedEffectChangeSpeed * 5));

            // Determine target chromatic aberration intensity.
            float TargetChromaticAberrationIntensity = Mathf.Lerp(0, MaxChromaticAberration, speedPercent);
            
            // Smoothly interpolate the chromatic aberration intensity.
            _SpeedsterVolume.profile.TryGet(out UnityEngine.Rendering.Universal.ChromaticAberration chromaticAberration);
            chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberration.intensity.value, TargetChromaticAberrationIntensity, Time.deltaTime * (SpeedEffectChangeSpeed * 5));

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
    }
}