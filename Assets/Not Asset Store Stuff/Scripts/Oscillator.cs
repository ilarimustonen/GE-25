using UnityEngine;
using System.Collections; // Required for Coroutines

public class Oscillator : MonoBehaviour
{
    // How far the element moves up and down
    [Tooltip("The total distance the element will move from its starting position.")]
    public float oscillationDistance = 50f;

    // How fast the movement occurs
    [Tooltip("The time it takes to complete one full up or down movement.")]
    public float movementTime = 1.5f;

    private Vector3 startPosition;
    private Vector2 startPosition2;
    public RectTransform rectTransform; // Used for UI elements
    public RectTransform rectTransform2; // Used for UI elements

    void Start()
    {

        // Store the initial position of the HUD element
        startPosition = rectTransform.anchoredPosition;
        startPosition2 = rectTransform2.anchoredPosition;

        // Start the repeating movement coroutine
        StartCoroutine(OscillateMovement());
    }

    private IEnumerator OscillateMovement()
    {
        // Define the target positions based on the start position
        Vector3 targetUp = startPosition + new Vector3(0, oscillationDistance, 0);
        Vector3 targetDown = startPosition - new Vector3(0, oscillationDistance, 0);

        while (true)
        {
            // --- Move UP to the targetUp position ---
            yield return StartCoroutine(MoveToPosition(targetUp));

            // --- Move DOWN to the targetDown position ---
            yield return StartCoroutine(MoveToPosition(targetDown));
        }
    }

    private IEnumerator MoveToPosition(Vector3 targetPos)
    {
        float elapsedTime = 0f;
        Vector3 currentPos = rectTransform.anchoredPosition;
        Vector2 currentPos2 = rectTransform2.anchoredPosition;

        while (elapsedTime < movementTime)
        {
            // Calculate the linear interpolation value (0 to 1)
            float t = elapsedTime / movementTime;

            // **This is the key change for the 'floaty' effect (SmoothStep function)**
            // It remaps the linear 't' value to a non-linear curve.
            t = t * t * (3f - 2f * t);

            // Interpolate the position smoothly using the new 't'
            rectTransform.anchoredPosition = Vector3.Lerp(currentPos, targetPos, t);
            rectTransform2.anchoredPosition = Vector2.Lerp(currentPos2, targetPos, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the element snaps exactly to the target position at the end
        rectTransform.anchoredPosition = targetPos;
        rectTransform2.anchoredPosition = targetPos;
    }
}