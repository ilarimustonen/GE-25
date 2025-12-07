using UnityEngine;
using TMPro;

public class ScoreHUD : MonoBehaviour
{
    private TextMeshProUGUI scoreText;
    private int pendingScore = -1;
    private bool needsUpdate = false;
    private string pendingStringScore = "";

    void Start()
    {
        scoreText = GetComponent<TextMeshProUGUI>();

        if (scoreText == null)
        {
            Debug.LogError("ScoreHUD must be attached to a GameObject with TextMeshProUGUI component!");
        }
    }

    void Update()
    {
        // Only update text on the main thread during Update
        if (needsUpdate && scoreText != null)
        {
            if (pendingScore < 10)
                scoreText.text = '0' + pendingStringScore;
            else
                scoreText.text = pendingStringScore;
                needsUpdate = false;
        }
    }

    // This can be called from any thread/context
    public void UpdateScore(int newScore)
    {
        pendingScore = newScore;
        pendingStringScore = pendingScore.ToString();
        needsUpdate = true;
    }
}