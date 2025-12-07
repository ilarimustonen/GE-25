using UnityEngine;

public class UIManager : MonoBehaviour
{
    public PlayerCutsceneHelper cutsceneHelper;
    public GameObject healthCanvas;
    public GameObject speedCanvas;
    public GameObject scoreCanvas;
    public GameObject cooldownCanvas;


    private void Update()
    {
        if (cutsceneHelper != null)
        {
            HandleCutsceneState(cutsceneHelper.cutsceneRunning);
        }
    }
    private void HandleCutsceneState(bool isRunning)
    {
        if (isRunning)
        {
            healthCanvas.SetActive(false);
            speedCanvas.SetActive(false);
            scoreCanvas.SetActive(false);
            cooldownCanvas.SetActive(false);
        }
        else
        {
            healthCanvas.SetActive(true);
            speedCanvas.SetActive(true);
            scoreCanvas.SetActive(true);
            cooldownCanvas.SetActive(true);
        }
    }
}