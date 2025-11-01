 using UnityEngine;
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

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        public float _speed { get; set; }
        private float _currentSprintTime = 0.0f;
        public float _animationBlend { get; set; }
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float _jumpBufferTimer;
        private float _coyoteTimer;
        private float _intendedTargetSpeed = 0.0f;
        private float _fallTimeoutDelta;
        public float _speedsterPercent { get; set; }

        // animation IDs
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private PlayerCombat _combatManager;
        private SpeedsterVFXManager _speedsterVFXManager;

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
            _speedsterVFXManager = GetComponent<SpeedsterVFXManager>();
            _combatManager = GetComponent<PlayerCombat>();
            _fallTimeoutDelta = FallTimeout;
            AssignAnimationIDs();

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
        }

        private void Update()
        {
            // Input handling for combat.
            if (_input.forceBlast && _combatManager._forceBlastTimeoutDelta <= 0.0f && !_combatManager._isAttacking)
            {
                _combatManager.HandleForceBlast();
            }

            if (_input.attack && _combatManager._attackTimeoutDelta <= 0.0f)
            {
                _combatManager.HandleAttack();
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
            if (_combatManager._isAttacking)
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


            if (_combatManager._isAttacking && Grounded)
            {
                _speed = 0.0f; // Force instant stop
            }

            // Use _speed (our tracked speed) instead of CharacterController velocity for calculations
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? moveInput.magnitude : 1f;

            // Accelerate or decelerate to the target speed
            if (!_combatManager._isAttacking || Grounded)
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

            float animationTarget = 0.0f;

            if (_speed > 0.0f)
            {
                if (_speed <= MoveSpeed)
                {
                    animationTarget = Mathf.InverseLerp(0.0f, MoveSpeed, _speed);
                }
                else if (_speed <= SprintSpeed)
                {
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

            // Send the final blend value to the animator
            if (_hasAnimator)
            {
                // Use _animIDMotionSpeed, NOT _animationBlend (which is the float value)
                _animator.SetFloat(_animIDMotionSpeed, _animationBlend);
            }

            // Handle speed-based effects and physics adjustments
            HandleSpeedsterUpdates();

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
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
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
            _speedsterPercent = Mathf.InverseLerp(0.0f, InitialMaxSprintSpeed, _speed);

            if (_speed <= InitialMaxSprintSpeed)
            {
                // Phase 1: Lerp from min to max slope limit up to InitialMaxSprintSpeed
                newSlopeLimit = Mathf.Lerp(MinSlopeLimit, MaxSlopeLimit, _speedsterPercent);
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

            // Call the speedster VFX manager.
            _speedsterVFXManager.VFXMain();

            // Call the Player Combat manager to enable/disable speed-based combat effects.
            _combatManager.SpeedsterEffects();


        }
    }
}