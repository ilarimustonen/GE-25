using System.Linq.Expressions;
using StarterAssets;
using UnityEngine;
public class PlayerAudioManager : MonoBehaviour
{
    // Public fields
    [Header("Footstep Audio Clips")]


    [Header("Footstep Audio")]
    public AudioSource[] FootstepSources;
    public AudioClip[] FootstepAudioClips;
    public AudioClip[] WaterFootstepAudioClips;
    public AudioClip LandingAudioClip;

    [Space(5)]
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
    [Range(0, 1)] public float WaterFootstepAudioVolume = 0.5f;
    [Space(5)]

    [Tooltip("Speed at which foottep delay starts")]
    public float FootstepDelayStartSpeed = 2f;

    [Tooltip("Speed at which footsteps stop entirely")]
    public float FootstepSilenceSpeed = 6f;

    [Tooltip("Maximum delay for footsteps at the highest speed")]
    public float MaxFootstepDelay = 0.5f;

    [Header("Combat Audio")]
    public AudioClip SwordSwingAudioClip;
    public AudioClip SwordHitAudioClip;
    public AudioClip ForceBlastAudioClip;
    public AudioSource[] ActionSources;

    [Range(0, 1)] public float ForceBlastAudioVolume = 0.5f;
    [Range(0, 1)] public float SwordSwingAudioVolume = 0.5f;
    [Range(0, 1)] public float SwordHitAudioVolume = 0.5f;


    [Header("Supersonic Running Audio")]
    [Tooltip("Audio source for the looping supersonic/wind sound effect.")]
    public AudioSource SupersonicSource;

    [Tooltip("The looping sound clip to play at high speeds.")]
    public AudioClip SupersonicLoopClip;

    [Tooltip("Audio clip to play when starting the supersonic run")]
    public AudioClip FoomAudioClip;

    [Tooltip("Speed (m/s) at which the supersonic sound starts playing.")]
    public float SupersonicStartSpeed = 25.0f;

    [Tooltip("Speed (m/s) at which the supersonic sound reaches full volume.")]
    public float SupersonicMaxSpeed = 75.0f;

    [Range(0, 1)]
    [Tooltip("Maximum volume for the supersonic audio loop.")]
    public float SupersonicMaxVolume = 0.8f;

    [Range(0, 1)]
    [Tooltip("Volume for the 'Foom' sound played when starting supersonic speed.")]
    public float FoomAudioVolume = 0.7f;

    [Header("Misc Audio")]
    [Tooltip("Audio clip played when scoring points.")]
    public AudioClip ScoredAudioClip;
    [Range(0, 1)] public float ScoredAudioVolume = 0.5f;


    // Private fields
    private CharacterController _controller;
    private WaterRunning _waterRunningScript;
    private PlayerCutsceneHelper _playerCutsceneHelper;
    private ThirdPersonController _thirdPersonScript;
    private Animator _animator;

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

    // Helper enum to define action sound types
    public enum ActionSoundType
    {
        SwordSwing,
        SwordHit,
        ForceBlast,
        ForceBlastWorld,
        Scored,
        Foom
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Cache references to required components
        _controller = GetComponent<CharacterController>();
        _waterRunningScript = GetComponent<WaterRunning>();
        _playerCutsceneHelper = GetComponent<PlayerCutsceneHelper>();
        _thirdPersonScript = GetComponent<ThirdPersonController>();
        _animator = GetComponent<Animator>();

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
    }

    // Update is called once per frame
    void Update()
    {
        ProcessFootsteps();
        HandleSupersonicAudio();
    }

    // Plays a clip on the next available source from the provided pool.
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

    // Plays a footstep sound when called by an animation event.
    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f && !_playerCutsceneHelper.cutsceneRunning)
        {
            if (FootstepAudioClips.Length > 0)
            {
                if (_waterRunningScript.IsRunningOnWater)
                {
                    // Pick a random water footstep clip
                    var index = UnityEngine.Random.Range(0, WaterFootstepAudioClips.Length);
                    StartFootstep(WaterFootstepAudioClips[index], WaterFootstepAudioVolume);
                }
                else
                {
                    // Pick a random footstep clip
                    var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
                    StartFootstep(FootstepAudioClips[index], FootstepAudioVolume);
                }

            }
        }
    }

    // Plays a landing sound when called by an animation event.
    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f && !_playerCutsceneHelper.cutsceneRunning)
        {
            if (_waterRunningScript.isNearWater)
            {
                // Play water landing sound
                var index = UnityEngine.Random.Range(0, WaterFootstepAudioClips.Length);
                StartFootstep(WaterFootstepAudioClips[index], WaterFootstepAudioVolume);
            }
            else StartFootstep(LandingAudioClip, FootstepAudioVolume);
        }
    }
    public void ProcessFootsteps()
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

                // Calculate distance-based volume (sound gets quieter if you're far away)
                float distance = Vector3.Distance(transform.position, footstep.position);
                float volumeFalloff = Mathf.Clamp01(1.0f - (distance / 10f)); // Audible within 10 units

                // Only play if loud enough to hear
                if (volumeFalloff > 0.01f)
                {
                    // --- MODIFICATION ---

                    // 1. Calculate the final volume based on the footstep's base volume
                    //    and the distance-based falloff.
                    float finalVolume = footstep.volume * volumeFalloff;

                    // 2. Call your pooling method with the correct clip, pool, and volume.
                    //    (Assumes _footstepSourcePool is the field holding your sources)
                    PlayClipFromPool(footstep.clip, FootstepSources, finalVolume);

                    // --- END MODIFICATION ---
                }
            }
            else
            {
                // Not time yet, stop checking
                break;
            }
        }
    }
    private void StartFootstep(AudioClip clip, float volume)
    {
        if (clip == null) return;

        // If moving too fast, don't even queue the sound (outrun it completely)
        if (_thirdPersonScript._speed >= FootstepSilenceSpeed)
        {
            return;
        }

        // Calculate delay based on current speed
        float delay = 0f;

        if (_thirdPersonScript._speed >= FootstepDelayStartSpeed)
        {
            // Calculate how much delay based on speed percentage
            float speedPercent = Mathf.InverseLerp(FootstepDelayStartSpeed, FootstepSilenceSpeed, _thirdPersonScript._speed);
            speedPercent = Mathf.Clamp01(speedPercent);
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

    public void HandleSupersonicAudio()
    {
        if (SupersonicSource == null || SupersonicLoopClip == null) return;

        // Check if we're above the supersonic threshold
        if (_thirdPersonScript._speed >= SupersonicStartSpeed)
        {
            // Start playing if not already playing
            if (!SupersonicSource.isPlaying)
            {
                SupersonicSource.Play();
            }
            

            // Calculate volume based on speed (fade in from start to max)
            float speedPercent = Mathf.InverseLerp(SupersonicStartSpeed, SupersonicMaxSpeed, _thirdPersonScript._speed);
            speedPercent = Mathf.Clamp01(speedPercent);
            float targetVolume = Mathf.Lerp(0f, SupersonicMaxVolume, speedPercent);


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

    public void PlayActionSound(ActionSoundType actionType)
        {
        switch (actionType)
        {
            case ActionSoundType.SwordSwing:
                PlayClipFromPool(SwordSwingAudioClip, ActionSources, SwordSwingAudioVolume);
                break;
            case ActionSoundType.ForceBlast:
                PlayClipFromPool(ForceBlastAudioClip, ActionSources, ForceBlastAudioVolume);
                break;
            case ActionSoundType.ForceBlastWorld:
                PlayClipFromPool(ForceBlastAudioClip, ActionSources, (ForceBlastAudioVolume * 1.5f));
                break;
            case ActionSoundType.Foom:
                PlayClipFromPool(FoomAudioClip, ActionSources, FoomAudioVolume);
                break;
            case ActionSoundType.SwordHit:
                PlayClipFromPool(SwordHitAudioClip, ActionSources, SwordHitAudioVolume);
                break;
            case ActionSoundType.Scored:
                PlayClipFromPool(ScoredAudioClip, ActionSources, ScoredAudioVolume);
                break;
            default:
                Debug.LogWarning("Unknown action sound type: " + actionType);
                break;
        }
    }

}
