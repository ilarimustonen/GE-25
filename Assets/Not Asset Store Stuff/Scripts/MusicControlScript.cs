using StarterAssets;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioTools
{
    // Script for controlling the main music and transitioning between two AudioMixerSnapshots.
    public class MusicControlScript : MonoBehaviour
    {
        public AudioSource[] sourceOfMusic;
        public AudioMixerSnapshot DefaultSnapshot;
        public AudioMixerSnapshot TransitionedSnapshot;
        public AudioClip[] m_TransitionEffects;
        public AudioSource m_TransitionEffectSource;
        public float bpm = 128f; // Use 'f' for float literals
        public ThirdPersonController playerController; // Reference to the player's controller script

        // PRIVATE FIELDS
        private bool isTransitioned = false; // Tracks the current audio state
        private float m_TransitionIn;
        private float m_TransitionOut;
        private float m_QuarterNote;
        private const float SpeedThreshold = 15f; // Define the threshold once

        // Use this for initialization
        void Start()
        {
            // Calculate timing based on BPM
            m_QuarterNote = 60f / bpm;
            m_TransitionIn = m_QuarterNote * 2f;
            m_TransitionOut = m_QuarterNote * 5f;

            // Start playing all attached music sources
            sourceOfMusic = GetComponents<AudioSource>();
            foreach (AudioSource source in sourceOfMusic)
                source.Play();

            // Set the initial state without transition (if needed)
            DefaultSnapshot.TransitionTo(0f); // Transition time of 0 sets the initial state instantly
        }

        private void Update()
        {
            // Check if the player is currently over the speed threshold
            bool isOverThreshold = playerController._speed > SpeedThreshold;

            // --- STATE MACHINE LOGIC ---

            // 1. Transition TO the fast music state
            if (isOverThreshold && !isTransitioned)
            {
                // We were slow, now we are fast. Transition NOW.
                PlayTransitionStep();
                TransitionedSnapshot.TransitionTo(m_TransitionIn);
                isTransitioned = true; // Set the flag so we don't transition again next frame
            }
            // 2. Transition OUT of the fast music state (back to default)
            else if (!isOverThreshold && isTransitioned)
            {
                // We were fast, now we are slow. Transition back NOW.
                PlayTransitionStep();
                DefaultSnapshot.TransitionTo(m_TransitionOut);
                isTransitioned = false; // Set the flag so we don't transition again next frame
            }

            // If none of these conditions are met, the state hasn't changed, 
            // and we do NOTHING, saving performance and preventing audio artifacts.
        }

        // Play random Transition step audio from the selected steps
        void PlayTransitionStep()
        {
            if (m_TransitionEffects.Length == 0) return; // Safety check

            int randClip = Random.Range(0, m_TransitionEffects.Length);
            m_TransitionEffectSource.clip = m_TransitionEffects[randClip];
            m_TransitionEffectSource.Play();
        }
    }
}