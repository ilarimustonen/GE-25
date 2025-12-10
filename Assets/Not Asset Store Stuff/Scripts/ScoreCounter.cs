using UnityEngine;
using System.Collections;

public class ScoreCounter : MonoBehaviour
{
    // Singleton pattern to ensure only one instance exists
    public static ScoreCounter Instance { get; private set; }
    public PlayerAudioManager audioManager;
    public ChallengeTimerManager challengeTimerManager;

    [Header("")]
    [SerializeField] private ScoreHUD scoreHUD;

    private int score = 0;
    private bool needsUIUpdate = false;
    private bool needsSoundEffect = false;
    

    private void Awake()
    {
        // Ensure only one ScoreCounter exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // Reset score whenever enabled (fixes editor persistence issue)
        score = 0;
        needsUIUpdate = false;
        needsSoundEffect = false;

        // Find all Scorer objects in the scene and subscribe to their events
        RegisterAllScorers();
    }

    private void OnDisable()
    {
        // Unsubscribe from all events when disabled
        UnregisterAllScorers();
    }

    private void Start()
    {
        // Initialize display
        if (scoreHUD != null)
        {
            scoreHUD.UpdateScore(score);
        }
    }

    private void Update()
    {
        // Update UI on the main thread during Update
        if (needsUIUpdate)
        {
            needsUIUpdate = false;

            if (scoreHUD != null)
            {
                scoreHUD.UpdateScore(score);
                challengeTimerManager.AddScore();
            }
        }
        // Play sound effect if needed
        if (needsSoundEffect)
        {
            needsSoundEffect = false;
            audioManager.PlayActionSound(PlayerAudioManager.ActionSoundType.Scored);
        }
    }

    private void OnDestroy()
    {
        // Clear the instance when destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void RegisterAllScorers()
    {
        // Find all Scorer components in the scene (including inactive ones)
        Scorer[] scorers = Object.FindObjectsByType<Scorer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Scorer scorer in scorers)
        {
            // Subscribe to each scorer's OnScore event
            scorer.OnScore.AddListener(Receiver);
        }
    }

    private void UnregisterAllScorers()
    {
        // Find all Scorer components and unsubscribe
        Scorer[] scorers = Object.FindObjectsByType<Scorer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Scorer scorer in scorers)
        {
            scorer.OnScore.RemoveListener(Receiver);
        }
    }

    public void Receiver()
    {
        // Increment score immediately
        score++;

        // Flag that the sound effect should play
        needsSoundEffect = true;

        // Flag that UI needs updating (will happen in Update on main thread)
        needsUIUpdate = true;
    }
}