using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Scorer : MonoBehaviour
{
    public UnityEvent OnScore;

    private bool hasScored = false;

    private void OnTriggerEnter(Collider other)
    {
        // Prevent double-triggering
        if (hasScored) return;

        if (other.CompareTag("Player"))
        {
            hasScored = true;

            // Invoke the event safely on the next frame
            StartCoroutine(InvokeScoreNextFrame());
        }
    }

    private IEnumerator InvokeScoreNextFrame()
    {
        // Wait until the next frame (ensures we're on the main thread)
        yield return null;

        OnScore?.Invoke();

        // Disable after invoking
        gameObject.SetActive(false);
    }
}