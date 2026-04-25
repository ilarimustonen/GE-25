using UnityEngine;

public class UIManager : MonoBehaviour
{
    public PlayerCutsceneHelper cutsceneHelper;
    public GameObject healthCanvas;
    public GameObject speedCanvas;
    public GameObject scoreCanvas;
    public GameObject cooldownCanvas;

    [SerializeField]
    private bool healthToggle;
    [SerializeField]
    private bool speedToggle;
    [SerializeField]
    private bool cooldownToggle;
    [SerializeField]
    private bool scoreToggle;

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
            if (healthToggle) healthCanvas.SetActive(true);
            if (speedToggle) speedCanvas.SetActive(true);
            if (scoreToggle) scoreCanvas.SetActive(true);
            if (cooldownToggle) cooldownCanvas.SetActive(true);
        }
    }
}