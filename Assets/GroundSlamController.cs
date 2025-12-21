using UnityEngine;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(ThirdPersonController))]
    [RequireComponent(typeof(CharacterController))]
    public class GroundSlamController : MonoBehaviour
    {
        [Header("Activation Settings")]
        [Tooltip("Minimum height above ground required to activate ground slam")]
        public float MinActivationHeight = 5.0f;

        [Header("Range Calculation")]
        [Tooltip("The minimum range the player can slam (e.g. 5 meters in front)")]
        public float BaseRange = 5.0f;

        [Tooltip("How much distance is added per meter of height. (e.g. 2.0 = 2m distance for every 1m height)")]
        public float RangePerHeightUnit = 1.5f;

        [Tooltip("Absolute hard cap on distance to prevent slamming across the map if falling from space")]
        public float AbsoluteMaxRange = 50.0f;

        [Header("Movement Settings")]
        [Tooltip("Speed at which player moves toward target")]
        public float SlamSpeed = 30.0f;

        [Tooltip("Additional slam speed based on current movement speed")]
        public float SlamSpeedMultiplier = 0.5f;

        [Tooltip("Curve for slam movement (0-1 represents start to end of slam)")]
        public AnimationCurve SlamCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("How close to target before transitioning to ground state")]
        public float LandingThreshold = 2.0f;

        [Header("Visual Settings")]
        public GameObject TargetIndicatorPrefab;
        public Color ValidColor = new Color(0, 1, 0, 0.5f);
        public Color InvalidColor = new Color(1, 0, 0, 0.5f);
        public float BaseIndicatorSize = 2.0f;

        [Header("VFX Pool Settings")]
        public GameObject LandingVFXPrefab;
        public GameObject TrailVFXPrefab;
        public int VFXPoolSize = 5;

        [Header("Raycast Settings")]
        public float MaxGroundCheckDistance = 200.0f;
        public LayerMask GroundLayers;

        [Header("Debug")]
        public bool ShowDebugGizmos = true;

        // Private variables
        private ThirdPersonController _controller;
        private CharacterController _characterController;
        private StarterAssetsInputs _input;
        private Camera _mainCamera;
        private GameObject _targetIndicator;
        private Renderer _indicatorRenderer;
        private Material _indicatorMaterial;

        private Vector3 _targetPosition;
        private Vector3 _slamStartPosition;

        private float _currentAllowedMaxRange; // calculated dynamically based on height
        private float _slamProgress;
        private bool _isSlammingToGround;
        private bool _canActivateSlam;
        private float _heightAboveGround;
        private float _currentSpeed;
        private float _effectiveSlamSpeed;
        private GameObject _activeTrailVFX;

        // VFX Pooling
        private Queue<GameObject> _landingVFXPool;
        private Queue<GameObject> _trailVFXPool;

        private void Start()
        {
            _controller = GetComponent<ThirdPersonController>();
            _characterController = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _mainCamera = Camera.main;

            InitializeVFXPools();
            CreateTargetIndicator();
        }

        private void Update()
        {
            _currentSpeed = _controller.CurrentSpeed;
            UpdateHeightCheck();

            if (_isSlammingToGround)
            {
                PerformSlam();
            }
            else if (_canActivateSlam)
            {
                CalculateAllowedRange();
                UpdateTargetPosition();
                UpdateIndicatorVisuals();
                CheckForSlamInput();
            }
            else
            {
                HideIndicator();
            }
        }

        private void UpdateHeightCheck()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, MaxGroundCheckDistance, GroundLayers))
            {
                _heightAboveGround = hit.distance;
                _canActivateSlam = _heightAboveGround >= MinActivationHeight && !_controller.Grounded;
            }
            else
            {
                _heightAboveGround = MaxGroundCheckDistance;
                _canActivateSlam = false;
            }
        }

        private void CalculateAllowedRange()
        {
            // Logic: You get BaseRange + (Height * Multiplier).
            // Example: 10m high * 1.5 multiplier = 15m bonus. Total = 5 + 15 = 20m range.
            float calculatedRange = BaseRange + (_heightAboveGround * RangePerHeightUnit);

            // Clamp to absolute max to prevent game-breaking distances
            _currentAllowedMaxRange = Mathf.Min(calculatedRange, AbsoluteMaxRange);
        }

        private void UpdateTargetPosition()
        {
            // 1. Raycast from Center of Screen
            Ray camRay = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit camHit;

            Vector3 rawTargetPoint;
            bool hitGeometry = Physics.Raycast(camRay, out camHit, MaxGroundCheckDistance * 2, GroundLayers);

            if (hitGeometry)
            {
                rawTargetPoint = camHit.point;
            }
            else
            {
                // If aiming at sky, project a point at max distance along camera forward
                rawTargetPoint = transform.position + (camRay.direction * _currentAllowedMaxRange);
            }

            // 2. Calculate Horizontal Distance from player to that point
            Vector3 directionToTarget = rawTargetPoint - transform.position;
            directionToTarget.y = 0; // Flatten to ignore height difference in distance check
            float horizontalDistance = directionToTarget.magnitude;

            // 3. Clamp Distance logic
            Vector3 finalFlatPosition;

            if (horizontalDistance > _currentAllowedMaxRange)
            {
                // Target is too far: Pull it back to the max allowed range
                Vector3 clampedDirection = directionToTarget.normalized * _currentAllowedMaxRange;
                finalFlatPosition = transform.position + clampedDirection;
            }
            else
            {
                // Target is within range
                finalFlatPosition = transform.position + directionToTarget;
            }

            // 4. Snap to Ground (The "Drop Down" check)
            // We now have the X/Z coordinate, but we need the correct Y coordinate on the floor.
            RaycastHit groundSnapHit;
            // Start raycast slightly above player height to catch uneven terrain
            Vector3 rayStart = new Vector3(finalFlatPosition.x, transform.position.y + 2f, finalFlatPosition.z);

            if (Physics.Raycast(rayStart, Vector3.down, out groundSnapHit, MaxGroundCheckDistance * 2, GroundLayers))
            {
                _targetPosition = groundSnapHit.point;
                ShowIndicator(true);
            }
            else
            {
                // If we can't find ground directly below the target point (e.g. over a pit), 
                // we invalidate the indicator or just show it floating
                _targetPosition = new Vector3(finalFlatPosition.x, transform.position.y - _heightAboveGround, finalFlatPosition.z);
                ShowIndicator(false);
            }
        }

        // --- Visuals & Slam Logic (Simplified for brevity as logic is unchanged) ---

        private void ShowIndicator(bool valid)
        {
            if (_targetIndicator == null) return;

            _targetIndicator.SetActive(true);
            _targetIndicator.transform.position = _targetPosition + Vector3.up * 0.1f;

            if (_indicatorMaterial != null)
                _indicatorMaterial.color = valid ? ValidColor : InvalidColor;
        }

        private void HideIndicator()
        {
            if (_targetIndicator != null) _targetIndicator.SetActive(false);
        }

        private void UpdateIndicatorVisuals()
        {
            if (_targetIndicator != null && _targetIndicator.activeSelf)
            {
                _targetIndicator.transform.Rotate(Vector3.up, 100f * Time.deltaTime);
            }
        }

        private void CheckForSlamInput()
        {
            if (_input.groundSlam)
            {
                InitiateSlam();
                _input.groundSlam = false;
            }
        }

        private void InitiateSlam()
        {
            _isSlammingToGround = true;
            _slamStartPosition = transform.position;
            _slamProgress = 0f;
            _effectiveSlamSpeed = SlamSpeed + (_currentSpeed * SlamSpeedMultiplier);
            _controller.OverrideGrounded = true;
            _controller.VerticalVelocity = 0f;

            if (TrailVFXPrefab != null)
            {
                _activeTrailVFX = GetPooledVFX(_trailVFXPool);
                if (_activeTrailVFX != null)
                {
                    _activeTrailVFX.transform.SetParent(transform);
                    _activeTrailVFX.transform.localPosition = Vector3.zero;
                    _activeTrailVFX.SetActive(true);
                }
            }
        }

        private void PerformSlam()
        {
            float distance = Vector3.Distance(_slamStartPosition, _targetPosition);
            _slamProgress += Time.deltaTime * (_effectiveSlamSpeed / distance);
            _slamProgress = Mathf.Clamp01(_slamProgress);

            float curvedProgress = SlamCurve.Evaluate(_slamProgress);
            Vector3 currentPos = Vector3.Lerp(_slamStartPosition, _targetPosition, curvedProgress);

            Vector3 movement = currentPos - transform.position;
            _characterController.Move(movement);

            if (Vector3.Distance(transform.position, _targetPosition) <= LandingThreshold || _slamProgress >= 1.0f)
            {
                CompleteSlam();
            }
        }

        private void CompleteSlam()
        {
            _isSlammingToGround = false;
            _controller.OverrideGrounded = false;
            _controller.VerticalVelocity = -5f; // Harder landing velocity
            HideIndicator();

            if (_activeTrailVFX != null)
            {
                _activeTrailVFX.transform.SetParent(null);
                ReturnVFXToPool(_activeTrailVFX, _trailVFXPool);
            }
            OnLandingComplete();
        }

        private void OnLandingComplete()
        {
            if (LandingVFXPrefab != null)
            {
                GameObject landingVFX = GetPooledVFX(_landingVFXPool);
                if (landingVFX != null)
                {
                    landingVFX.transform.position = transform.position;
                    landingVFX.SetActive(true);
                    StartCoroutine(ReturnVFXAfterDelay(landingVFX, _landingVFXPool, 2.0f));
                }
            }
        }

        // --- Pooling & Helpers ---

        private void InitializeVFXPools()
        {
            _landingVFXPool = new Queue<GameObject>();
            _trailVFXPool = new Queue<GameObject>();

            for (int i = 0; i < VFXPoolSize; i++)
            {
                if (LandingVFXPrefab) { var obj = Instantiate(LandingVFXPrefab); obj.SetActive(false); _landingVFXPool.Enqueue(obj); }
                if (TrailVFXPrefab) { var obj = Instantiate(TrailVFXPrefab); obj.SetActive(false); _trailVFXPool.Enqueue(obj); }
            }
        }

        private GameObject GetPooledVFX(Queue<GameObject> pool) => pool.Count > 0 ? pool.Dequeue() : null;
        private void ReturnVFXToPool(GameObject vfx, Queue<GameObject> pool) { vfx.SetActive(false); pool.Enqueue(vfx); }
        private System.Collections.IEnumerator ReturnVFXAfterDelay(GameObject vfx, Queue<GameObject> pool, float delay) { yield return new WaitForSeconds(delay); ReturnVFXToPool(vfx, pool); }

        private void CreateTargetIndicator()
        {
            if (TargetIndicatorPrefab) _targetIndicator = Instantiate(TargetIndicatorPrefab);
            else
            {
                _targetIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _targetIndicator.transform.localScale = new Vector3(BaseIndicatorSize, 0.1f, BaseIndicatorSize);
                Destroy(_targetIndicator.GetComponent<Collider>());
            }
            _indicatorRenderer = _targetIndicator.GetComponent<Renderer>();
            if (_indicatorRenderer) { _indicatorMaterial = new Material(Shader.Find("Standard")); _indicatorRenderer.material = _indicatorMaterial; }
            _targetIndicator.SetActive(false);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !ShowDebugGizmos || !_canActivateSlam) return;

            // Draw current Max Range Circle
            Gizmos.color = Color.cyan;
            Vector3 center = transform.position;
            center.y = _targetPosition.y + 0.5f; // Draw slightly above ground
            DrawGizmoCircle(center, _currentAllowedMaxRange);

            // Draw line to target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _targetPosition);
            Gizmos.DrawWireSphere(_targetPosition, 1.0f);
        }

        private void DrawGizmoCircle(Vector3 center, float radius)
        {
            float theta = 0;
            float x = radius * Mathf.Cos(theta);
            float z = radius * Mathf.Sin(theta);
            Vector3 startPos = center + new Vector3(x, 0, z);
            Vector3 endPos = Vector3.zero;

            for (int i = 0; i < 36; i++)
            {
                theta += (2.0f * Mathf.PI) / 36;
                x = radius * Mathf.Cos(theta);
                z = radius * Mathf.Sin(theta);
                endPos = center + new Vector3(x, 0, z);
                Gizmos.DrawLine(startPos, endPos);
                startPos = endPos;
            }
        }
    }
}