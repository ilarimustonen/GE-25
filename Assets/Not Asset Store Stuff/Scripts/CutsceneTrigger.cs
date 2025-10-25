using UnityEngine;
using UnityEngine.Playables; // MUST include this namespace

public class SceneTrigger : MonoBehaviour
{
    // Public variable to hold the reference to the other GameObject's component
    [Tooltip("Drag the Cutscene_Director's Playable Director component here.")]
    public PlayableDirector cutsceneDirector;

    // This method will be called to start the cutscene
    public void StartCutscene()
    {
        if (cutsceneDirector != null)
        {
            // The Play() method starts the Timeline from the beginning
            cutsceneDirector.Play();
            Debug.Log("Cutscene activated by external trigger: " + gameObject.name);

            // Optional: Disable the trigger so it only runs once
            // this.enabled = false; 
        }
        else
        {
            Debug.LogError("The Playable Director reference is missing on the " + gameObject.name + " trigger!");
        }
    }
}