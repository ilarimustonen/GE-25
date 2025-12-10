using UnityEngine;
using TMPro;
using System.Collections;

public class ChallengeTimerManager : MonoBehaviour
{
    public GameObject scorePopup;
    public GameObject rings;
    public GameObject timerObject;
    bool timerRunning = false;
    TextMeshProUGUI textBox;
    public int score;
    [SerializeField] float timeRemaining = 15f;

    public void StartTimer()
    {
        timerObject.SetActive(true);
        textBox = GetComponentInChildren<TextMeshProUGUI>();
        textBox.text = timeRemaining + ":00";
        timerRunning = true;
        //rings.SetActive(true);
    }

    public void StopTimer()
    {
        timerRunning = false;
        textBox.text = "0:00";
        scorePopup.GetComponentInChildren<TextMeshProUGUI>().text = "You got " + score + " score";
        scorePopup.SetActive(true);
        //rings.SetActive(false);
        StartCoroutine(boxTimer());
        score = 0;
    }

    public void AddScore()
    {
        if (!timerRunning) return;

        score++;
    }

    IEnumerator boxTimer()
    {


        yield return new WaitForSeconds(5);

        scorePopup.SetActive(false);
        timerObject.SetActive(false);
    }

    private void Update()
    {
        if (!timerRunning) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0)
        {
            StopTimer();
            return;
        }

        int seconds = (int)timeRemaining;
        float fraction = timeRemaining - seconds;
        int milliseconds = (int)(fraction * 100);
        string timeString = string.Format("{0:D2}:{1:D2}", seconds, milliseconds);

        textBox.text = timeString;
    }
}
