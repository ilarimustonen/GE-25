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
    [Tooltip("Material to apply to player during high-speed movement.")]
    public Material SpeedMaterial;

    [Tooltip("Particle system to spawn when transitioning to speed material.")]
    public GameObject MaterialTransitionVFX;

    [Tooltip("Reference to the GameObject containing the Motion Blur Volume.")]
    public GameObject SpeedEffectParent;

    [Tooltip("The prefab to spawn as a 'clone'.")]
    public GameObject ClonePrefab;

    [Header("Trail Density Optimization")]
    [Tooltip("Distance between clones at LOW speed (e.g., 0.2).")]
    public float MinDistance = 0.2f;

    [Tooltip("Distance between clones at HIGH speed (e.g., 2.0). Increases spacing to save performance.")]
    public float MaxDistance = 2.0f;

    [Tooltip("Minimum fade time for clones at low speed.")]
    public float MinCloneFadeTime = 0.1f;

    [Tooltip("Maximum fade time for clones at max speed.")]
    public float MaxCloneFadeTime = 0.4f; 

    [Tooltip("Brightness multiplier at low speed.")]
    public float MinBrightness = 0.1f;

    [Tooltip("Brightness multiplier at max speed.")]
    public float MaxBrightness = 1.0f;

    [Tooltip("The 'animationBlend' value at which max brightness is reached.")]
    public float MaxBrightnessThreshold = 2.5f;

    [Header("Clone Object Pool")]
    public int ClonePoolSize = 100; // Recommend 150-200 for 500km/h
    private GameObject[] _clonePool;
    public Transform CloneParent;

    [Header("Camera Effect Settings")]
    public CinemachineVirtualCamera Camera;
    public float MaxFOV = 70.0f;
    public float MaxLensD = -0.35f;
    public float MaxMotionBlur = 1.0f;
    public float MaxMotionBlurClamp = 0.2f;
    public float MaxChromaticAberration = 1.0f;
    public float SpeedEffectChangeSpeed = 5.0f;
    public float camEffectSpeedThreshold = 2.0f;
    public float maxCamDistance = 7f;

    // Internal State
    private SkinnedMeshRenderer[] _playerSkinnedMeshes;
    private Volume _SpeedsterVolume;
    private ThirdPersonController _thirdPersonController;
    private float MainFOV;
    private float mainCamDistance;
    private Cinemachine3rdPersonFollow cameraBody;

    // Pooling logic
    private int _clonePoolIndex;

    // Post Processing
    private LensDistortion _lensDistortion;
    private MotionBlur _motionBlur;
    private ChromaticAberration _chromaticAberration;
    private bool _postProcessingCached = false;

    // Material Management
    private Material[] _originalMaterials;
    private bool _materialsSwapped = false;
    private Material _playerSpeedMaterialInstance;

    // Movement Tracking
    private Vector3 _lastPlayerPosition;
    private float _distanceAccumulator = 0f;
    private CloneFade[] _cloneFadeScripts;

    // rotation for vfx spawn
    private Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f);

    // Audio controller reference
    private PlayerAudioManager _audioManager;

    void Start()
    {
        _thirdPersonController = GetComponent<ThirdPersonController>();
        _audioManager = GetComponent<PlayerAudioManager>();

        // 1. Setup Meshes & Materials immediately to prevent Index Errors
        _playerSkinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        if (_playerSkinnedMeshes != null && _playerSkinnedMeshes.Length > 0)
        {
            _originalMaterials = new Material[_playerSkinnedMeshes.Length];
            for (int i = 0; i < _playerSkinnedMeshes.Length; i++)
            {
                // Use sharedMaterial to store the asset reference safely
                _originalMaterials[i] = _playerSkinnedMeshes[i].sharedMaterial;
            }
        }

        // 2. Setup Post Processing
        _SpeedsterVolume = SpeedEffectParent.GetComponent<Volume>();
        if (_SpeedsterVolume != null && _SpeedsterVolume.profile != null)
        {
            _postProcessingCached = _SpeedsterVolume.profile.TryGet(out _lensDistortion) &&
                                    _SpeedsterVolume.profile.TryGet(out _motionBlur) &&
                                    _SpeedsterVolume.profile.TryGet(out _chromaticAberration);
        }

        if (Camera != null) MainFOV = Camera.m_Lens.FieldOfView;

        cameraBody = (Camera.GetCinemachineComponent(CinemachineCore.Stage.Body) as Cinemachine3rdPersonFollow);
        if (Camera != null) mainCamDistance = cameraBody.CameraDistance;

        _lastPlayerPosition = transform.position;

        // 3. Initialize Pool
        InitializePool();
    }

    private void InitializePool()
    {
        if (ClonePrefab == null) return;

        _clonePool = new GameObject[ClonePoolSize];
        _cloneFadeScripts = new CloneFade[ClonePoolSize];

        // Create a container if not provided to keep hierarchy clean
        if (CloneParent == null)
        {
            GameObject group = new GameObject("SpeedsterClonePool");
            CloneParent = group.transform;
        }

        for (int l = 0; l < ClonePoolSize; l++)
        {
            _clonePool[l] = Instantiate(ClonePrefab, transform.position, Quaternion.identity);
            _clonePool[l].transform.parent = CloneParent;

            // Cache the script now to avoid GetComponent calls during high-speed updates
            _cloneFadeScripts[l] = _clonePool[l].GetComponent<CloneFade>();
            _clonePool[l].SetActive(false);
        }
    }

    public void VFXMain()
    {
        if (_thirdPersonController == null) return;

        HandleMaterialSwap();
        HandleCameraChanges();
        HandleTrailSpawning();
    }

    private void HandleTrailSpawning()
    {
        if (ClonePrefab == null || _clonePool == null) return;

        float currentBlend = _thirdPersonController._animationBlend;
        bool showEffects = (currentBlend >= SpeedEffectThreshold);

        // Calculate distance traveled this frame
        float distanceThisFrame = Vector3.Distance(transform.position, _lastPlayerPosition);

        if (!showEffects || distanceThisFrame <= 0.001f)
        {
            _lastPlayerPosition = transform.position;
            _distanceAccumulator = 0f;
            return;
        }

        _distanceAccumulator += distanceThisFrame;

        // --- CALCULATE DYNAMIC DENSITY ---
        float speedPercent = Mathf.InverseLerp(SpeedEffectThreshold, MaxBrightnessThreshold, currentBlend);

        // As we get faster, we INCREASE the gap between clones to prevent pool exhaustion
        float currentSpacing = Mathf.Lerp(MinDistance, MaxDistance, speedPercent);

        float fadeTime = Mathf.Lerp(MinCloneFadeTime, MaxCloneFadeTime, speedPercent);
        float brightness = Mathf.Lerp(MinBrightness, MaxBrightness, speedPercent);

        // Spawn logic
        while (_distanceAccumulator >= currentSpacing)
        {
            _distanceAccumulator -= currentSpacing;

            // Interpolate position backwards
            float t = 1.0f - (_distanceAccumulator / distanceThisFrame);
            Vector3 spawnPos = Vector3.Lerp(_lastPlayerPosition, transform.position, t);

            SpawnSingleClone(spawnPos, transform.rotation, fadeTime, brightness);
        }

        _lastPlayerPosition = transform.position;
    }

    // --- THIS IS THE METHOD THAT WAS MISSING ---
    private void SpawnSingleClone(Vector3 position, Quaternion rotation, float fadeTime, float brightness)
    {
        int idx = _clonePoolIndex;
        GameObject clone = _clonePool[idx];
        CloneFade fadeScript = _cloneFadeScripts[idx];

        // Increment index (Loop around)
        _clonePoolIndex = (_clonePoolIndex + 1) % ClonePoolSize;

        clone.SetActive(false);
        clone.transform.SetPositionAndRotation(position, rotation);

        if (fadeScript != null)
        {
            fadeScript.Initialize(_playerSkinnedMeshes, fadeTime, brightness);
        }

        clone.SetActive(true);
    }

    private void HandleCameraChanges()
    {
        if (Camera == null || !_postProcessingCached) return;

        float currentBlend = _thirdPersonController._animationBlend;
        float camSpeedThresholdPercent = Mathf.InverseLerp(0.0f, camEffectSpeedThreshold, currentBlend);

        float deltaLerp = Time.deltaTime * SpeedEffectChangeSpeed;

        // FOV
        float targetFOV = Mathf.Lerp(MainFOV, MaxFOV, camSpeedThresholdPercent);
        Camera.m_Lens.FieldOfView = Mathf.Lerp(Camera.m_Lens.FieldOfView, targetFOV, deltaLerp);

        // Lens Distortion
        if (_lensDistortion != null)
            _lensDistortion.intensity.value = Mathf.Lerp(0f, MaxLensD, camSpeedThresholdPercent);

        // Motion Blur
        if (_motionBlur != null)
        {
            _motionBlur.intensity.value = Mathf.Lerp(0f, MaxMotionBlur, camSpeedThresholdPercent);
            _motionBlur.clamp.value = Mathf.Lerp(0.05f, MaxMotionBlurClamp, camSpeedThresholdPercent);
        }

        // Chromatic Aberration
        if (_chromaticAberration != null)
            _chromaticAberration.intensity.value = Mathf.Lerp(0f, MaxChromaticAberration, camSpeedThresholdPercent);

        // Camera distance for skyrunning
        if (_thirdPersonController.isSkyrunning)
        {
            float targetDistance = Mathf.Lerp(mainCamDistance, maxCamDistance, camSpeedThresholdPercent);
            cameraBody.CameraDistance = targetDistance;
        }

    }

    private void HandleMaterialSwap()
    {
        if (SpeedMaterial == null || _playerSkinnedMeshes == null) return;

        // SAFETY CHECK: Ensure arrays match
        if (_originalMaterials == null || _playerSkinnedMeshes.Length != _originalMaterials.Length)
        {
            _originalMaterials = new Material[_playerSkinnedMeshes.Length];
            for (int i = 0; i < _playerSkinnedMeshes.Length; i++)
                _originalMaterials[i] = _playerSkinnedMeshes[i].sharedMaterial;
        }

        float currentBlend = _thirdPersonController._animationBlend;
        bool shouldSwap = (currentBlend >= SpeedEffectThreshold);

        // 1. Swap TO Speed Material
        if (shouldSwap && !_materialsSwapped)
        {
            if (_playerSpeedMaterialInstance == null)
                _playerSpeedMaterialInstance = new Material(SpeedMaterial);

            for (int i = 0; i < _playerSkinnedMeshes.Length; i++)
            {
                if (_playerSkinnedMeshes[i] != null)
                    _playerSkinnedMeshes[i].material = _playerSpeedMaterialInstance;
            }

            _materialsSwapped = true;
            SpawnTransitionVFX();
        }
        // 2. Swap BACK to Original
        else if (!shouldSwap && _materialsSwapped)
        {
            for (int i = 0; i < _playerSkinnedMeshes.Length; i++)
            {
                if (_playerSkinnedMeshes[i] != null && _originalMaterials[i] != null)
                {
                    _playerSkinnedMeshes[i].material = _originalMaterials[i];
                }
            }
            _materialsSwapped = false;
            SpawnTransitionVFX();
        }

        // 3. Update Emission/Brightness while swapped
        if (_materialsSwapped && _playerSpeedMaterialInstance != null)
        {
            float speedPercent = Mathf.InverseLerp(SpeedEffectThreshold, MaxBrightnessThreshold, currentBlend);
            float brightnessMultiplier = MinBrightness + ((MaxBrightness - MinBrightness) * speedPercent);

            if (SpeedMaterial.HasProperty("_EmissionColor"))
            {
                Color baseEmission = SpeedMaterial.GetColor("_EmissionColor");
                _playerSpeedMaterialInstance.SetColor("_EmissionColor", baseEmission * brightnessMultiplier);
            }
        }
    }

    private void SpawnTransitionVFX()
    {
        if (MaterialTransitionVFX != null)
        { 
            GameObject vfx = Instantiate(MaterialTransitionVFX, transform.position, rotation, transform);
            var ps = vfx.GetComponent<ParticleSystem>();
            Destroy(vfx, ps != null ? ps.main.duration + 0.5f : 2.0f);

            // Play the sound effect
            if (_audioManager != null)
            {
                _audioManager.PlayActionSound(PlayerAudioManager.ActionSoundType.Foom);
            }
        }
    }

    private void OnDestroy()
    {
        if (_playerSpeedMaterialInstance != null)
        {
            Destroy(_playerSpeedMaterialInstance);
        }
    }
}