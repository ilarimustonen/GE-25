using UnityEngine;
using System;
using Unity.VisualScripting;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

        [Tooltip("Absolute maximum speed cap. Set to 0 for unlimited speed.")]
        public float MaxSpeedCap = 200.0f;

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
        public float _speed { get; private set; }
        private float _currentSprintTime = 0.0f;
        public float _animationBlend { get; private set; }
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float _jumpBufferTimer;
        private float _coyoteTimer;
        private float _intendedTargetSpeed = 0.0f;
        private float _fallTimeoutDelta;
        public float _speedsterPercent { get; private set; }

        private bool skyRunning;
        public bool isSkyrunning { get { return skyRunning; } }
        private Vector3 inputDirection;


        // animation IDs
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDSkyRun;

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
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
        }

        private void Update()
        {
            HandleCombatInput();
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
            _animIDSkyRun = Animator.StringToHash("SkyRun");
        }

        private void HandleCombatInput()
        {
            if (_input.forceBlast && _combatManager._forceBlastTimeoutDelta <= 0.0f && !_combatManager._isAttacking)
            {
                _combatManager.HandleForceBlast();
            }

            if (_input.attack && _combatManager._attackTimeoutDelta <= 0.0f)
            {
                _combatManager.HandleAttack();
            }
        }

        public void SkyRunningStart()
        {
            skyRunning = true;
            Grounded = false;
            _animator.SetBool(_animIDSkyRun, true);
            _controller.Move(new Vector3 (0, 10, 0));
            CameraAngleOverride = -20f;

            // Boost so skyrunning is fast from the start
            _currentSprintTime += 30;
        }
        public void SkyRunningEnd()
        {
            skyRunning = false;
            CameraAngleOverride = 0f;
            _animator.SetBool(_animIDSkyRun, false);
            transform.rotation = Quaternion.Euler(0.0f, transform.rotation.y, 0.0f);
        }
        private void GroundedCheck()
        {
            if (skyRunning) { return; }
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);

            bool physicsGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);
            Grounded = physicsGrounded || OverrideGrounded;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            if (!skyRunning)
            {
                _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
            }
            else
            {
                _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, -30f, 80f);

            }


            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                    _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            Vector2 moveInput = _combatManager._isAttacking ? Vector2.zero : _input.move;
            if (skyRunning) { moveInput = new Vector2(0, 1); }
            float targetSpeed = CalculateTargetSpeed(moveInput);

            if (_combatManager._isAttacking && Grounded)
            {
                _speed = 0.0f;
            }
            else
            {
                UpdateSpeed(targetSpeed, moveInput);
            }

            UpdateAnimationBlend();
            HandleSpeedsterUpdates();
            PerformMovement(moveInput);
        }

        private float CalculateTargetSpeed(Vector2 moveInput)
        {
            if (moveInput == Vector2.zero)
            {
                _intendedTargetSpeed = 0.0f;
                _currentSprintTime = 0.0f;
                return 0.0f;
            }

            if (_input.sprint || skyRunning)
            {
                _currentSprintTime += Time.deltaTime;

                if (_currentSprintTime <= SprintAccelerationTime)
                {
                    // Phase 1: Accelerate from SprintSpeed to InitialMaxSprintSpeed
                    float sprintLerp = _currentSprintTime / SprintAccelerationTime;
                    _intendedTargetSpeed = Mathf.Lerp(SprintSpeed, InitialMaxSprintSpeed, sprintLerp);
                }
                else
                {
                    // Phase 2: Infinite linear acceleration beyond InitialMaxSprintSpeed
                    float timeAfterMax = _currentSprintTime - SprintAccelerationTime;
                    _intendedTargetSpeed = InitialMaxSprintSpeed + (InfiniteAccelerationRate * timeAfterMax);
                }

                // Apply max speed cap if set
                if (MaxSpeedCap > 0 && !skyRunning)
                {
                    _intendedTargetSpeed = Mathf.Min(_intendedTargetSpeed, MaxSpeedCap);
                }

                return _intendedTargetSpeed;
            }

            // Walking
            _intendedTargetSpeed = MoveSpeed;
            _currentSprintTime = 0.0f;
            return MoveSpeed;
        }

        private void UpdateSpeed(float targetSpeed, Vector2 moveInput)
        {
            const float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? moveInput.magnitude : 1f;

            if (Mathf.Abs(_speed - targetSpeed) > speedOffset)
            {
                _speed = Mathf.Lerp(_speed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }
        }

        private void UpdateAnimationBlend()
        {
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

            _animationBlend = Mathf.Lerp(_animationBlend, animationTarget, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDMotionSpeed, _animationBlend);
            }
        }

        private void PerformMovement(Vector2 moveInput)
        {
            if (skyRunning)
            {
                inputDirection = new Vector3(moveInput.x, _mainCamera.transform.forward.y, moveInput.y);
            }
            else
            {
                inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;
            }

            if (moveInput != Vector2.zero)
            {
                if (skyRunning)
                {
                    _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                  _mainCamera.transform.eulerAngles.y;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                        RotationSmoothTime);

                    transform.rotation = Quaternion.Euler(_mainCamera.transform.eulerAngles.x, rotation, 0.0f);
                }
                else
                {
                    _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                  _mainCamera.transform.eulerAngles.y;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                        RotationSmoothTime);

                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }

            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            if (skyRunning)
            {
                targetDirection = Quaternion.Euler(_mainCamera.transform.eulerAngles.x, _targetRotation, 0.0f) * Vector3.forward;
            }

            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            UpdateCoyoteTimer();
            UpdateJumpBuffer();
            ProcessJump();
            ApplyGravity();
        }

        private void UpdateCoyoteTimer()
        {
            if (Grounded)
            {
                _coyoteTimer = CoyoteTime;
            }
            else
            {
                _coyoteTimer -= Time.deltaTime;
            }
        }

        private void UpdateJumpBuffer()
        {
            if (skyRunning) { return; }
            if (_input.jump)
            {
                _jumpBufferTimer = JumpBufferTime;
                _input.jump = false;
            }
            else
            {
                _jumpBufferTimer -= Time.deltaTime;
            }
        }

        private void ProcessJump()
        {
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }

                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
            }
            else if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f && !OverrideGrounded)
                {
                    _verticalVelocity = CalculateGroundGravity();
                }
            }
            else
            {
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }
        }

        private float CalculateGroundGravity()
        {
            // Use capped speed for gravity calculation
            float effectiveSpeed = MaxSpeedCap > 0 ? Mathf.Min(_speed, MaxSpeedCap) : _speed;

            if (effectiveSpeed <= InitialMaxSprintSpeed)
            {
                float speedPercent = Mathf.InverseLerp(0.0f, InitialMaxSprintSpeed, effectiveSpeed);
                return Mathf.Lerp(BaseGroundGravity, MaxSpeedGroundGravity, speedPercent);
            }

            float speedBeyondMax = effectiveSpeed - InitialMaxSprintSpeed;
            return MaxSpeedGroundGravity + (GravityScalingRate * speedBeyondMax);
        }

        private void ApplyGravity()
        {
            if (!Grounded && _verticalVelocity < _terminalVelocity && !skyRunning)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void HandleSpeedsterUpdates()
        {
            UpdateSlopeLimit();
            _speedsterVFXManager.VFXMain();
            _combatManager.SpeedsterEffects();
        }

        private void UpdateSlopeLimit()
        {
            // Use capped speed for slope limit calculation
            float effectiveSpeed = MaxSpeedCap > 0 ? Mathf.Min(_speed, MaxSpeedCap) : _speed;
            _speedsterPercent = Mathf.InverseLerp(0.0f, InitialMaxSprintSpeed, effectiveSpeed);

            float newSlopeLimit;
            if (effectiveSpeed <= InitialMaxSprintSpeed)
            {
                newSlopeLimit = Mathf.Lerp(MinSlopeLimit, MaxSlopeLimit, _speedsterPercent);
            }
            else
            {
                float speedBeyondMax = effectiveSpeed - InitialMaxSprintSpeed;
                newSlopeLimit = MaxSlopeLimit + (SlopeLimitScalingRate * speedBeyondMax);
                newSlopeLimit = Mathf.Min(newSlopeLimit, 89.0f);
            }

            _controller.slopeLimit = newSlopeLimit;
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
    }
}