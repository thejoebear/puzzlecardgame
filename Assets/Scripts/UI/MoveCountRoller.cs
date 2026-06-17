using UnityEngine;
using TMPro;
using System.Collections;

public class MoveCountRoller : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI moveText;

    [Header("Animation Settings")]
    public float rollDuration = 1.2f;
    public float finalPopAmount = 1.2f;
    public float popDuration = 0.3f;
    public string labelFormat = "MOVES: {0}";

    public void RollTo(int targetValue)
    {
        if (moveText == null) moveText = GetComponent<TextMeshProUGUI>();
        if (moveText == null) return;

        StopAllCoroutines();
        StartCoroutine(RollRoutine(targetValue));
    }

    private IEnumerator RollRoutine(int targetValue)
    {
        float elapsed = 0;
        
        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rollDuration;
            
            // Out-cubic easing
            t = 1f - Mathf.Pow(1f - t, 3);
            
            int currentValue = Mathf.RoundToInt(Mathf.Lerp(0, targetValue, t));
            moveText.text = string.Format(labelFormat, currentValue);
            
            // Subtle rhythmic "tick" could be added here
            
            yield return null;
        }

        moveText.text = string.Format(labelFormat, targetValue);
        
        // Final pop effect
        yield return StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        float elapsed = 0;
        Vector3 originalScale = Vector3.one;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            float s = 1.0f + Mathf.Sin(t * Mathf.PI) * (finalPopAmount - 1.0f);
            moveText.transform.localScale = originalScale * s;
            yield return null;
        }

        moveText.transform.localScale = originalScale;
    }
}
