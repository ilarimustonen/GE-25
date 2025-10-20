using UnityEngine;
using System.Collections;

namespace cherrydev
{
    /// <summary>
    /// Manages 3D game state when dialogue system is active
    /// Handles cursor visibility, camera controls, and player input
    /// </summary>
    public class DialogManager3D : MonoBehaviour
    {
        [Header("Dialog Components")]
        [SerializeField] private DialogBehaviour _dialogBehaviour;
        [SerializeField] private Canvas _dialogCanvas;

        [Header("Player Controls")]
        [SerializeField] private MonoBehaviour[] _playerControlScripts;
        [SerializeField] private MonoBehaviour _cameraControlScript;

        [Header("Settings")]
        [SerializeField] private bool _pauseGameDuringDialog = false;
        [Tooltip("Check if dialog is active by monitoring if the canvas is visible")]
        [SerializeField] private bool _useCanvasVisibilityCheck = true;

        private bool _isDialogActive;
        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;

        private void Start()
        {
            // Ensure canvas is set to Screen Space - Overlay or Screen Space - Camera
            if (_dialogCanvas != null)
            {
                if (_dialogCanvas.renderMode == RenderMode.WorldSpace)
                {
                    Debug.LogWarning("Dialog canvas is in WorldSpace mode. Consider using Screen Space - Overlay for UI interaction.");
                }
            }
        }

        private void OnEnable()
        {
            if (_dialogBehaviour != null)
            {
                // Subscribe to the dialog finished event
                _dialogBehaviour.AddListenerToDialogFinishedEvent(OnDialogFinished);

                // Subscribe to dialog disabled event
                _dialogBehaviour.DialogDisabled += OnDialogFinished;
            }
        }

        private void OnDisable()
        {
            if (_dialogBehaviour != null)
            {
                // Unsubscribe from dialog disabled event
                _dialogBehaviour.DialogDisabled -= OnDialogFinished;
            }
        }

        private void Update()
        {
            // Monitor dialog state by checking if canvas is active
            if (_useCanvasVisibilityCheck && _dialogCanvas != null)
            {
                bool canvasActive = _dialogCanvas.gameObject.activeInHierarchy;

                // Dialog just started
                if (canvasActive && !_isDialogActive)
                {
                    OnDialogStarted();
                }
                // Dialog just ended (handled by events, but this is a backup)
                else if (!canvasActive && _isDialogActive)
                {
                    OnDialogFinished();
                }
            }
        }

        /// <summary>
        /// Called when dialogue starts - unlocks cursor and disables player controls
        /// </summary>
        private void OnDialogStarted()
        {
            _isDialogActive = true;

            // Store previous cursor state
            _previousCursorLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;

            // Show and unlock cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Disable player movement and camera controls
            DisablePlayerControls();

            // Pause game if enabled (NOTE: Typewriter effect may not work with Time.timeScale = 0)
            if (_pauseGameDuringDialog)
            {
                Time.timeScale = 0f;
            }
        }

        /// <summary>
        /// Called when dialogue ends - restores cursor and enables player controls
        /// </summary>
        private void OnDialogFinished()
        {
            if (!_isDialogActive) return; // Already finished

            _isDialogActive = false;

            // Restore previous cursor state
            Cursor.lockState = _previousCursorLockMode;
            Cursor.visible = _previousCursorVisible;

            // Re-enable player movement and camera controls
            EnablePlayerControls();

            // Resume game if it was paused
            if (_pauseGameDuringDialog)
            {
                Time.timeScale = 1f;
            }
        }

        /// <summary>
        /// Disables all player control scripts
        /// </summary>
        private void DisablePlayerControls()
        {
            // Disable player movement scripts
            foreach (var script in _playerControlScripts)
            {
                if (script != null)
                    script.enabled = false;
            }

            // Disable camera control script
            if (_cameraControlScript != null)
                _cameraControlScript.enabled = false;
        }

        /// <summary>
        /// Enables all player control scripts
        /// </summary>
        private void EnablePlayerControls()
        {
            // Enable player movement scripts
            foreach (var script in _playerControlScripts)
            {
                if (script != null)
                    script.enabled = true;
            }

            // Enable camera control script
            if (_cameraControlScript != null)
                _cameraControlScript.enabled = true;
        }

        /// <summary>
        /// Public method to manually start dialogue with proper 3D game handling
        /// This will automatically trigger OnDialogStarted when the canvas becomes active
        /// </summary>
        /// <param name="dialogGraph">The DialogNodeGraph to start</param>
        public void StartDialog(DialogNodeGraph dialogGraph)
        {
            if (_dialogBehaviour != null)
            {
                _dialogBehaviour.StartDialog(dialogGraph);

                // If not using canvas visibility check, manually trigger start
                if (!_useCanvasVisibilityCheck)
                {
                    StartCoroutine(DelayedDialogStart());
                }
            }
        }

        /// <summary>
        /// Waits one frame for the canvas to be activated, then triggers dialog started
        /// </summary>
        private IEnumerator DelayedDialogStart()
        {
            yield return null; // Wait one frame
            if (!_isDialogActive)
            {
                OnDialogStarted();
            }
        }

        /// <summary>
        /// Get the DialogBehaviour component for direct access if needed
        /// </summary>
        public DialogBehaviour GetDialogBehaviour() => _dialogBehaviour;

        /// <summary>
        /// Check if dialog is currently active
        /// </summary>
        public bool IsDialogActive() => _isDialogActive;
    }
}