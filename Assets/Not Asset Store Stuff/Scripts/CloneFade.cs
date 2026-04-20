using UnityEngine;

using System.Collections.Generic;


public class CloneFade : MonoBehaviour

{

    [Tooltip("Total time the clone will last before being disabled.")]
    public float FadeTime = 0.5f;

    [Header("Fade Timing")]
    [Tooltip("Duration in seconds for the clone to fade in (Alpha 0 -> 1).")]
    public float FadeInDuration = 0.15f;
    [Tooltip("Duration in seconds for the clone to fade out (Alpha 1 -> 0).")]
    public float FadeOutDuration = 0.25f;

    // --- Private Variables ---
    private Mesh _bakedMesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private float _timeElapsed;
    private float _currentFadeTime;
    private Color _baseColor; // The original base color from prefab
    private Color _baseEmission; // The original emission color from prefab
    private Color _emissionScaled; // The brightness-multiplied emission to scale from
    private int _shaderColorID;
    private int _shaderEmissionID;

    // --- Material Caching ---
    private Material _materialInstance;
    private Material _originalMaterial; // Store the original material to reset from

    void Awake()
    {
        _shaderColorID = Shader.PropertyToID("_BaseColor");
        _shaderEmissionID = Shader.PropertyToID("_EmissionColor");

        // Get the pre-configured mesh parts from the prefab
        _meshFilter = GetComponentInChildren<MeshFilter>();
        _meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (_meshFilter == null || _meshRenderer == null)
        {
            Debug.LogError("ClonePrefab must have a child with MeshFilter and MeshRenderer!", this);
            return;
        }

        // Store the original material and its colors
        _originalMaterial = _meshRenderer.sharedMaterial;
        if (_originalMaterial != null)
        {
            // Note: Use _BaseColor, falling back to _Color if necessary
            if (_originalMaterial.HasProperty(_shaderColorID))
            {
                _baseColor = _originalMaterial.GetColor(_shaderColorID);
            }
            else
            {
                _shaderColorID = Shader.PropertyToID("_Color");
                if (_originalMaterial.HasProperty(_shaderColorID))
                {
                    _baseColor = _originalMaterial.GetColor(_shaderColorID);
                }
            }

            if (_originalMaterial.HasProperty(_shaderEmissionID))
            {
                _baseEmission = _originalMaterial.GetColor(_shaderEmissionID);
            }
        }
    }

    /// <summary>
    /// This is called by the player script to set up the clone with the player's current pose.
    /// </summary>
    public void Initialize(SkinnedMeshRenderer[] playerSkinnedMeshes, float fadeTime = -1f, float brightnessMultiplier = 1f)
    {
        if (_meshFilter == null || _meshRenderer == null) return;

        // Use custom fade time if provided, otherwise use default. This is the clone's absolute lifespan.
        _currentFadeTime = fadeTime > 0 ? fadeTime : FadeTime;

        // Clean up old mesh
        Destroy(_bakedMesh);

        // --- Mesh Combination Logic ---
        CombineInstance[] combine = new CombineInstance[playerSkinnedMeshes.Length];

        for (int i = 0; i < playerSkinnedMeshes.Length; i++)
        {
            // Bake the current pose
            Mesh bakedMesh = new Mesh();
            playerSkinnedMeshes[i].BakeMesh(bakedMesh);

            combine[i].mesh = bakedMesh;
            // Use localToWorldMatrix to correctly combine meshes into the clone's local space
            combine[i].transform = transform.worldToLocalMatrix * playerSkinnedMeshes[i].transform.localToWorldMatrix;
        }

        // Combine all meshes into one
        _bakedMesh = new Mesh();
        _bakedMesh.CombineMeshes(combine, true, true);

        // Apply to the mesh filter
        _meshFilter.mesh = _bakedMesh;

        // Clean up temporary baked meshes
        foreach (var ci in combine)
        {
            Destroy(ci.mesh);
        }

        // --- Material Setup Logic ---

        // Clean up the previous material instance
        Destroy(_materialInstance);

        // Create a fresh material instance from the original material
        _materialInstance = new Material(_originalMaterial);
        _meshRenderer.material = _materialInstance;

        // --- BASE COLOR: Only apply the Alpha (start at 0.0). RGB stays at original base. ---
        if (_materialInstance.HasProperty(_shaderColorID))
        {
            // Use the base RGB color, ignore the multiplier for the main color channel
            Color baseRGB = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 1f);
            _materialInstance.SetColor(_shaderColorID, new Color(baseRGB.r, baseRGB.g, baseRGB.b, 0.0f));
        }

        // --- EMISSION COLOR: Use brightnessMultiplier as absolute value with color tint ---
        if (_materialInstance.HasProperty(_shaderEmissionID))
        {
            // Your original color: RGB(0, 138, 255) converted to 0-1 range
            // R: 0/255 = 0.0
            // G: 138/255 = 0.541
            // B: 255/255 = 1.0
            Color blueColor = new Color(0f, 0.541f, 1f);

            // Apply the brightness multiplier to the blue color
            _emissionScaled = new Color(
                blueColor.r * brightnessMultiplier,
                blueColor.g * brightnessMultiplier,
                blueColor.b * brightnessMultiplier,
                1.0f // Keep alpha at 1 for emission
            );

            // Set emission at full brightness immediately
            // NOTE: For HDR values > 1.0 to work, your material must have HDR enabled!
            _materialInstance.SetColor(_shaderEmissionID, _emissionScaled);
            _materialInstance.EnableKeyword("_EMISSION");
        }

        // Make sure the renderer is enabled
        _meshRenderer.enabled = true;
    }

    void OnEnable()
    {
        // Reset time immediately on activation
        _timeElapsed = 0f;
    }

    void Update()
    {
        if (_materialInstance == null || _meshRenderer == null || !_meshRenderer.enabled) return;

        _timeElapsed += Time.deltaTime;

        float newAlpha = 0f;
        float fadeOutStart = _currentFadeTime - FadeOutDuration;

        // --- 1. FADE IN Phase ---
        if (_timeElapsed < FadeInDuration)
        {
            float t = _timeElapsed / FadeInDuration;
            newAlpha = Mathf.Lerp(0.0f, 1.0f, t);
        }
        // --- 2. FULL OPACITY Phase (or immediate Fade Out if times overlap) ---
        else if (_timeElapsed < fadeOutStart)
        {
            newAlpha = 1.0f;
        }
        // --- 3. FADE OUT Phase ---
        else if (_timeElapsed < _currentFadeTime)
        {
            // Calculate time counter (t) over the FadeOutDuration
            float t = (_timeElapsed - fadeOutStart) / FadeOutDuration;
            newAlpha = Mathf.Lerp(1.0f, 0.0f, t);
        }
        // --- 4. DISABLE (Lifespan End) ---
        else
        {
            gameObject.SetActive(false);
            return;
        }

        // Apply the calculated alpha to the materials
        ApplyAlpha(newAlpha);
    }

    private void ApplyAlpha(float alpha)
    {
        // Fade the base color alpha for transparency
        if (_materialInstance.HasProperty(_shaderColorID))
        {
            Color currentColor = _materialInstance.GetColor(_shaderColorID);
            _materialInstance.SetColor(_shaderColorID,
                new Color(currentColor.r, currentColor.g, currentColor.b, alpha));
        }

        // DON'T fade emission - it stays at the brightness determined by speed at spawn time
        // This way: slow speed = dim clones, fast speed = bright clones
        // The base color alpha handles the fade in/out for transparency
    }


    void OnDisable()
    {
        // Clean up the baked mesh
        Destroy(_bakedMesh);
        _bakedMesh = null;

        // Clean up the material instance
        Destroy(_materialInstance);
        _materialInstance = null;

        // Ensure the MeshRenderer is disabled when pooling to prevent rendering nothing
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = false;
        }
    }

}