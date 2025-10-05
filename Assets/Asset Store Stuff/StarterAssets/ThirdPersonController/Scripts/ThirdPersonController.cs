 using UnityEngine;
 using Ilumisoft.HealthSystem;
 using System.Collections;
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
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Header("Global Audio")]
        [Range(0, 1)] public float GlobalAudioVolume = 1f;

        [Header("Footstep Audio Stuff")]
        public AudioSource FootstepSource1;
        public AudioSource FootstepSource2;
        private AudioSource[] _footstepSources;
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

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
        private int _currentVfxIndex = 0; // Index to cycle through the pool

        [Tooltip("Vertical offset for the force blast VFX spawn position relative to the player.")]
        public Vector3 ForceBlastVFXOffset = new Vector3(0f, 1f, 0f);

        [Tooltip("The VFX prefab to spawn when the blast occurs.")]
        public GameObject ForceBlastVFX;

        [Tooltip("How long the VFX prefab will exist before being destroyed (in seconds).")]
        public float VFXLifetime = 3f;

        // ability timeout deltatime
        private float _forceBlastTimeoutDelta;
        private float _attackTimeoutDelta;
        private bool _isAttacking;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float _jumpBufferTimer;
        private float _coyoteTimer;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDForceBlast;
        private int _animIDAttack;

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

            // Setup audio sources array
            _footstepSources = new AudioSource[] { FootstepSource1, FootstepSource2 };

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

                JumpAndGravity();
                GroundedCheck();
                Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
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
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

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
            // If attacking, we simply set the input vector to zero to stop horizontal movement.
            Vector2 moveInput = _input.move;
            if (_isAttacking)
            {
                moveInput = Vector2.zero;
            }

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (moveInput == Vector2.zero) targetSpeed = 0.0f;

            if (_isAttacking && Grounded)
            {
                _speed = 0.0f; // Force instant stop/prevent acceleration
            }

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? moveInput.magnitude : 1f;

            // accelerate or decelerate to target speed, but only if not attacking and not grounded
            if (!_isAttacking || Grounded)
            {

                if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
                {
                    // creates curved result rather than a linear one giving a more organic speed change
                    // note T in Lerp is clamped, so we don't need to clamp our speed
                    _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                        Time.deltaTime * SpeedChangeRate);

                    // round speed to 3 decimal places
                    _speed = Mathf.Round(_speed * 1000f) / 1000f;
                }
                else
                {
                    _speed = targetSpeed;
                }
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
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


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player (THIS LINE NOW APPLIES GRAVITY CORRECTLY)
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
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
            if (Grounded)
            {
                // Reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // Update animator
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // Stop our velocity from dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
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
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }
        public void TriggerBlastEffect()
        {
            // --- VFX POOL LOGIC ---
            if (_vfxPool != null && _vfxPool.Length > 0)
            {
                // 1. Get the next available VFX object in the cycle
                GameObject spawnedVFX = _vfxPool[_currentVfxIndex];

                // 2. Cycle the index for the next use
                _currentVfxIndex = (_currentVfxIndex + 1) % PoolSize;

                // 3. Position and activate the object
                Vector3 spawnPosition = transform.position + ForceBlastVFXOffset;
                spawnedVFX.transform.position = spawnPosition;
                spawnedVFX.transform.rotation = Quaternion.identity;
                spawnedVFX.transform.parent = transform; // Keep it parented

                spawnedVFX.SetActive(true);

                // 4. Start the Coroutine to deactivate it after its lifetime
                StartCoroutine(DeactivateVFXAfterTime(spawnedVFX, VFXLifetime));
            }

            // Play sound effect
            PlayClipFromPool(ForceBlastAudioClip, _actionSources, ForceBlastAudioVolume * GlobalAudioVolume);

            // Find all colliders within the blast radius
            Collider[] colliders = Physics.OverlapSphere(transform.position, ForceBlastRadius);

            // Apply force to each collider that has a Rigidbody
            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    // AddExplosionForce is perfect for this!
                    rb.AddExplosionForce(ForceBlastForce, transform.position, ForceBlastRadius, ForceBlastUpwardsModifier);
                }

                HitboxComponent hitbox = hit.GetComponent<HitboxComponent>();
                if (hitbox != null)
                {
                    // Calculate distance from the explosion center
                    float distance = Vector3.Distance(transform.position, hit.transform.position);

                    // Calculate damage based on distance (the closer, the more damage)
                    // This creates a linear falloff from maxDamage to 0.
                    float damageFalloff = 1 - (distance / ForceBlastRadius);
                    float calculatedDamage = maxDamage * damageFalloff;

                    // Ensure damage is not negative if something is outside the radius (shouldn't happen with OverlapSphere but good practice)
                    if (calculatedDamage > 0)
                    {
                        // Call the ApplyDamage method on the hitbox
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
                _animator.SetTrigger(_animIDAttack);
            }
        }

        // Called by the Animation Event in the 'Gunslinger_Attack04' clip
        public void StartAttack()
        {
            if (swordOnBack != null)
                swordOnBack.SetActive(false); // Hide the cosmetic sword

            if (swordInHand != null)
                swordInHand.SetActive(true);  // Show the functional sword in the hand

            if (swordController != null)
            {
                // Delegates the command to the actual sword script.
                swordController.EnableCollider();
            }
            PlayClipFromPool(SwordSwingAudioClip, _actionSources, SwordSwingAudioVolume * GlobalAudioVolume);
        }

        // Called by the Animation Event in the 'Gunslinger_Attack04' clip
        public void EndAttackDamageWindow()
        {
            if (swordController != null)
            {
                // Delegates the command to the actual sword script (the Spoke)
                swordController.DisableCollider();

                // Sword switching logic
                if (swordInHand != null)
                    swordInHand.SetActive(false); // Hide the functional sword

                if (swordOnBack != null)
                    swordOnBack.SetActive(true);  // Show the cosmetic sword again
            }
            // No error needed here, as it's fine if the sword is null while ending the attack.
        }
        public void EndAttack()
        {
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
                if (!source.isPlaying)
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
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    PlayClipFromPool(FootstepAudioClips[index], _footstepSources, GlobalAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                PlayClipFromPool(LandingAudioClip, _footstepSources, GlobalAudioVolume);
            }
        }
    }
}