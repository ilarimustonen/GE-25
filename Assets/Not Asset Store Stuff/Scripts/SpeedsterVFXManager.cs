using Cinemachine;
using StarterAssets;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class SpeedsterVFXManager : MonoBehaviour
{
    [Header("Speed Effect Activation Threshold")]
    [Tooltip("The 'animationBlend' value (e.g., 2.0) to activate speed effects at.")]
    public float SpeedEffectThreshold = 2.0f;

    [Header("Clone Trail Settings")]

    [Tooltip("Material to apply to player during high-speed movement (same as clone material).")]
    public Material SpeedMaterial;

    [Tooltip("Particle system to spawn when transitioning to speed material.")]
    public GameObject MaterialTransitionVFX;

    [Tooltip("The 'animationBlend' value at which max brightness is reached.")]
    public float MaxBrightnessThreshold = 2.5f;

    [Tooltip("Reference to the GameObject containing the Motion Blur (e.g., a Post-Processing Volume).")]
    public GameObject SpeedEffectParent;

    [Tooltip("The prefab to spawn as a 'clone' or 'afterimage'.")]
    public GameObject ClonePrefab;

    [Tooltip("How often a clone is spawned (in seconds) when at max speed.")]
    public float CloneSpawnRate = 0.01f;

    [Tooltip("Minimum spawn rate (longest delay between clones) at low speed.")]
    public float MaxCloneSpawnRate = 0.02f;

    [Tooltip("Should clone spawn rate scale with speed dynamically?")]
    public bool DynamicSpawnRate = true;

    [Tooltip("Minimum fade time for clones at low speed.")]
    public float MinCloneFadeTime = 0.01f;

    [Tooltip("Maximum fade time for clones at max speed.")]
    public float MaxCloneFadeTime = 0.5f;

    [Tooltip("Brightness multiplier at low speed (dimmer).")]
    public float MinBrightness = 0.1f;

    [Tooltip("Brightness multiplier at max speed (brighter).")]
    public float MaxBrightness = 1.0f;

    [Header("Clone Object Pool")]
    [Tooltip("How many clone objects to pre-instantiate.")]
    public int ClonePoolSize = 100;
    private GameObject[] _clonePool;

    [Tooltip("Parent object to organize clones under (optional, for hierarchy organization).")]
    public Transform CloneParent;

    [Header("Camera Effect Settings")]
    [Tooltip("The camera to change the fov of during supersonic speeds.")]
    public CinemachineVirtualCamera Camera;

    [Tooltip("Max field of view during supersonic speed")]
    public float MaxFOV = 70.0f;

    [Tooltip("Max lens distortion during supersonic speed")]
    public float MaxLensD = -0.35f;

    [Tooltip("Max motion blur values during supersonic speed")]
    public float MaxMotionBlur = 1.0f;
    public float MaxMotionBlurClamp = 0.2f;

    [Tooltip("Max chromatic aberration during supersonic speed")]
    public float MaxChromaticAberration = 1.0f;

    [Tooltip("How quickly to interpolate to the post-processing values.")]
    public float SpeedEffectChangeSpeed = 0.1f;

    [Tooltip("The 'animationBlend' value (0-3) at which the camera effects start")]
    public float camEffectSpeedThreshold = 2.0f;


    private SkinnedMeshRenderer[] _playerSkinnedMeshes;
    private Volume _SpeedsterVolume;
    private ThirdPersonController _thirdPersonController;
    private float MainFOV;
    private int _clonePoolIndex;
    private float _cloneSpawnTimer;
    private LensDistortion _lensDistortion;
    private MotionBlur _motionBlur;
    private ChromaticAberration _chromaticAberration;
    private bool _postProcessingCached = false;
    private float _lastCamBlendValue = -1f;
    private const float CAM_UPDATE_THRESHOLD = 0.02f;
    private CloneFade[] _cloneFadeScripts;
    private Material[] _originalMaterials;
    private bool _materialsSwapped = false;
    private Material _playerSpeedMaterialInstance;
    private Vector3 _lastPlayerPosition;

    void Start()
    {
        _playerSkinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        _SpeedsterVolume = SpeedEffectParent.GetComponent<Volume>();
        _thirdPersonController = GetComponent<ThirdPersonController>();

        // Cache post-processing components
        if (_SpeedsterVolume != null && _SpeedsterVolume.profile != null)
        {
            _postProcessingCached = _SpeedsterVolume.profile.TryGet(out _lensDistortion) &&
                                    _SpeedsterVolume.profile.TryGet(out _motionBlur) &&
                                    _SpeedsterVolume.profile.TryGet(out _chromaticAberration);
        }

        // Check the FOV of the camera
        if (Camera != null)
        {
            MainFOV = Camera.m_Lens.FieldOfView;
        }

        // Initialize last player position
        _lastPlayerPosition = transform.position;

        // --- Initialize The Clone Object Pool FIRST ---
        if (ClonePrefab != null)
        {
            _clonePool = new GameObject[ClonePoolSize];
            _cloneFadeScripts = new CloneFade[ClonePoolSize];

            for (int l = 0; l < ClonePoolSize; l++)
            {
                _clonePool[l] = Instantiate(ClonePrefab, transform.position, Quaternion.identity);

                if (CloneParent != null)
                {
                    _clonePool[l].transform.parent = CloneParent;
                }

                // Cache the CloneFade component while we're instantiating
                _cloneFadeScripts[l] = _clonePool[l].GetComponent<CloneFade>();

                _clonePool[l].SetActive(false);
            }
        }
    }

    public void CloneSpawn()
    {
        if (ClonePrefab == null || _clonePool == null || _playerSkinnedMeshes == null) return;

        float currentBlend = _thirdPersonController._animationBlend;
        bool showEffects = (currentBlend >= SpeedEffectThreshold);

        if (!showEffects)
        {
            _cloneSpawnTimer = CloneSpawnRate;
            _lastPlayerPosition = transform.position;
            return;
        }

        _cloneSpawnTimer -= Time.deltaTime;

        if (_cloneSpawnTimer <= 0f)
        {
            // Dynamic spawn rate based on speed
            float currentSpawnRate;
            if (DynamicSpawnRate)
            {
                // Calculate speed as distance traveled per frame
                float distanceTraveled = Vector3.Distance(_lastPlayerPosition, transform.position);
                float speed = distanceTraveled / Time.deltaTime;

                // Base spawn rate adjusted by speed
                // At higher speeds, spawn more frequently to maintain trail density
                float speedMultiplier = Mathf.Clamp(speed / 100f, 0.5f, 10f); // Adjust 100f based on your speed scale
                currentSpawnRate = CloneSpawnRate / speedMultiplier;

                // Clamp to prevent too frequent or too slow spawning
                currentSpawnRate = Mathf.Clamp(currentSpawnRate, CloneSpawnRate * 0.1f, MaxCloneSpawnRate);
            }
            else
            {
                currentSpawnRate = Mathf.Lerp(MaxCloneSpawnRate, CloneSpawnRate, _thirdPersonController._speedsterPercent);
            }

            int clonesToSpawn = Mathf.Max(1, Mathf.CeilToInt(-_cloneSpawnTimer / currentSpawnRate) + 1);

            // Calculate once outside loop for optimization
            float speedPercent = Mathf.InverseLerp(SpeedEffectThreshold, MaxBrightnessThreshold, currentBlend);
            float fadeTime = Mathf.Lerp(MinCloneFadeTime, MaxCloneFadeTime, speedPercent);
            float finalBrightnessMultiplier = MinBrightness + ((MaxBrightness - MinBrightness) * speedPercent);

            // Current position and rotation
            Vector3 currentPos = transform.position;
            Quaternion currentRot = transform.rotation;

            for (int i = 0; i < clonesToSpawn; i++)
            {
                int currentIndex = _clonePoolIndex;
                GameObject clone = _clonePool[currentIndex];
                CloneFade fadeScript = _cloneFadeScripts[currentIndex];

                _clonePoolIndex = (_clonePoolIndex + 1) % ClonePoolSize;

                clone.SetActive(false);

                // Interpolate position along the path from last position to current position
                // This creates a smooth trail even when spawning multiple clones
                float t = (float)(i + 1) / (clonesToSpawn + 1);
                Vector3 interpolatedPos = Vector3.Lerp(_lastPlayerPosition, currentPos, t);

                clone.transform.SetPositionAndRotation(interpolatedPos, currentRot);

                if (fadeScript != null)
                {
                    fadeScript.Initialize(_playerSkinnedMeshes, fadeTime, finalBrightnessMultiplier);
                }

                clone.SetActive(true);
            }

            _cloneSpawnTimer = currentSpawnRate;
            _lastPlayerPosition = currentPos;
        }
    }

    private void HandleCameraChanges(CinemachineVirtualCamera camera)
    {
        if (camera == null || !_postProcessingCached) return;

        // Skip update if speed hasn't changed significantly
        float currentBlend = _thirdPersonController._animationBlend;
        if (Mathf.Abs(currentBlend - _lastCamBlendValue) < CAM_UPDATE_THRESHOLD)
            return;

        _lastCamBlendValue = currentBlend;

        // Calculate speed percentage once
        float camSpeedThresholdPercent = Mathf.InverseLerp(0.0f, camEffectSpeedThreshold, currentBlend);
        float deltaLerp = Time.deltaTime * SpeedEffectChangeSpeed;
        float deltaLerpFast = Time.deltaTime * (SpeedEffectChangeSpeed * 5f);

        // FOV
        float targetFOV = Mathf.Lerp(MainFOV, MaxFOV, camSpeedThresholdPercent);
        camera.m_Lens.FieldOfView = Mathf.Lerp(camera.m_Lens.FieldOfView, targetFOV, deltaLerp);

        // Lens Distortion
        float targetLensD = Mathf.Lerp(0f, MaxLensD, camSpeedThresholdPercent);
        _lensDistortion.intensity.value = Mathf.Lerp(_lensDistortion.intensity.value, targetLensD, deltaLerpFast);

        // Motion Blur
        float targetMotionBlur = Mathf.Lerp(0f, MaxMotionBlur, camSpeedThresholdPercent);
        _motionBlur.intensity.value = Mathf.Lerp(_motionBlur.intensity.value, targetMotionBlur, deltaLerpFast);

        float targetMotionBlurClamp = Mathf.Lerp(0f, MaxMotionBlurClamp, camSpeedThresholdPercent);
        _motionBlur.clamp.value = Mathf.Lerp(_motionBlur.clamp.value, targetMotionBlurClamp, deltaLerpFast);

        // Chromatic Aberration
        float targetChromatic = Mathf.Lerp(0f, MaxChromaticAberration, camSpeedThresholdPercent);
        _chromaticAberration.intensity.value = Mathf.Lerp(_chromaticAberration.intensity.value, targetChromatic, deltaLerpFast);
    }

    private void HandleMaterialSwap()
    {
        if (SpeedMaterial == null || _playerSkinnedMeshes == null) return;

        float currentBlend = _thirdPersonController._animationBlend;
        bool shouldSwap = (currentBlend >= SpeedEffectThreshold);

        // Swap to speed material
        if (shouldSwap && !_materialsSwapped)
        {
            // Store original materials if not already stored
            if (_originalMaterials == null)
            {
                _originalMaterials = new Material[_playerSkinnedMeshes.Length];
                for (int i = 0; i < _playerSkinnedMeshes.Length; i++)
                {
                    _originalMaterials[i] = _playerSkinnedMeshes[i].material;
                }
            }

            // Create an instance of the speed material for the player
            if (_playerSpeedMaterialInstance == null)
            {
                _playerSpeedMaterialInstance = new Material(SpeedMaterial);
            }

            // Apply speed material instance to all meshes
            for (int i = 0; i < _playerSkinnedMeshes.Length; i++)
            {
                _playerSkinnedMeshes[i].material = _playerSpeedMaterialInstance;
            }
            _materialsSwapped = true;

            // Spawn transition VFX when entering speed mode
            SpawnTransitionVFX();
        }
        // Revert to original materials
        else if (!shouldSwap && _materialsSwapped)
        {
            for (int i = 0; i < _playerSkinnedMeshes.Length; i++)
            {
                if (_originalMaterials[i] != null)
                {
                    _playerSkinnedMeshes[i].material = _originalMaterials[i];
                }
            }
            _materialsSwapped = false;

            // Spawn transition VFX when exiting speed mode
            SpawnTransitionVFX();
        }

        // Update brightness while swapped
        if (_materialsSwapped && _playerSpeedMaterialInstance != null)
        {
            // Calculate the same brightness as clones
            float speedPercent = Mathf.InverseLerp(SpeedEffectThreshold, MaxBrightnessThreshold, currentBlend);
            float brightnessMultiplier = MinBrightness + ((MaxBrightness - MinBrightness) * speedPercent);

            // Update the material's emission or brightness property
            // Assuming your clone material uses "_EmissionColor" - adjust if different
            Color baseEmission = SpeedMaterial.GetColor("_EmissionColor");
            _playerSpeedMaterialInstance.SetColor("_EmissionColor", baseEmission * brightnessMultiplier);

            // If using URP's base color instead/additionally:
            // Color baseColor = SpeedMaterial.GetColor("_BaseColor");
            // _playerSpeedMaterialInstance.SetColor("_BaseColor", baseColor * brightnessMultiplier);
        }
    }

    private void SpawnTransitionVFX()
    {
        if (MaterialTransitionVFX != null)
        {
            GameObject vfx = Instantiate(MaterialTransitionVFX, transform.position, transform.rotation);

            // Auto-destroy the particle system after it finishes playing
            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(vfx, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // Fallback: destroy after 5 seconds if no particle system found
                Destroy(vfx, 5f);
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up the material instance to prevent memory leaks
        if (_playerSpeedMaterialInstance != null)
        {
            Destroy(_playerSpeedMaterialInstance);
        }
    }

    public void VFXMain()
    {
        // Handle Material Swapping
        HandleMaterialSwap();

        // Handle Clone Spawning
        CloneSpawn();

        // Handle Camera Changes
        HandleCameraChanges(Camera);
    }
}