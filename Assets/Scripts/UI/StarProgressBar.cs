using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StarProgressBar : MonoBehaviour
{
    private enum StarIndex { Three = 0, Two = 1, One = 2 }
    [Header("Components")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI[] starTexts; // 0: Star 3, 1: Star 2, 2: Star 1
    [SerializeField] private RectTransform[] starMarkers; // 0: Marker 3, 1: Marker 2, 2: Marker 1

    [Header("Settings")]
    [SerializeField] private Color activeColor = new Color(1.5f, 1.2f, 0.5f, 1.0f); // HDR-ish Gold
    [SerializeField] private Color inactiveColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
    [SerializeField] private Color lossFlashColor = new Color(2.0f, 0.0f, 0.0f, 1.0f); // Bright Red
    [SerializeField] private float maxMovesMultiplier = 1.5f;
    [SerializeField] private float oneStarMarkerPosition = 0.95f;
    [SerializeField] private float starLossAnimationDuration = 0.5f;
    [SerializeField] private float barFlashDuration = 0.15f;
    [SerializeField] private float popEffectScale = 0.8f;

    private bool[] lastStarStates = new bool[3] { true, true, true };
    private Coroutine[] activeStarRoutines = new Coroutine[3];
    private RectTransform[] starTextRectTransforms;

    private void OnEnable()
    {
        CacheStarRectTransforms();
    }

    /// <summary>
    /// Caches RectTransform components for all star texts to avoid repeated GetComponent calls.
    /// </summary>
    private void CacheStarRectTransforms()
    {
        if (starTexts == null || starTexts.Length < 3)
            return;

        starTextRectTransforms = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            starTextRectTransforms[i] = starTexts[i]?.GetComponent<RectTransform>();
        }
    }

    /// <summary>
    /// Updates the progress bar based on current moves and thresholds.
    /// </summary>
    public void UpdateProgress(int moves, int threeStar, int twoStar)
    {
        float maxMovesForBar = twoStar * maxMovesMultiplier;
        float progress = Mathf.Clamp01((float)moves / maxMovesForBar);

        SetFillAmount(progress);

        bool[] currentStates = new bool[] {
            moves <= threeStar,
            moves <= twoStar,
            true // 1 star is always active
        };

        UpdateStarVisuals(currentStates);
        UpdateMarkerPositions(threeStar, twoStar, maxMovesForBar);
    }

    /// <summary>
    /// Updates star text colors and animations based on current vs previous states.
    /// </summary>
    private void UpdateStarVisuals(bool[] currentStates)
    {
        if (starTexts == null || starTexts.Length < 3)
            return;

        for (int i = 0; i < 3; i++)
        {
            // Detect loss transition (Active -> Inactive) and trigger animation
            if (lastStarStates[i] && !currentStates[i])
            {
                PlayStarLossAnimation(i);
            }
            else if (currentStates[i])
            {
                // Star is active, set to active color
                SetStarColor(i, activeColor);
            }
            else if (!lastStarStates[i])
            {
                // Star was already inactive, keep it inactive
                SetStarColor(i, inactiveColor);
            }
            // Otherwise, let the animation handle the color transition

            lastStarStates[i] = currentStates[i];
        }
    }

    /// <summary>
    /// Updates marker positions based on thresholds.
    /// </summary>
    private void UpdateMarkerPositions(int threeStar, int twoStar, float maxMovesForBar)
    {
        if (starMarkers == null || starMarkers.Length < 3)
            return;

        PositionMarker(starMarkers[(int)StarIndex.Three], (float)threeStar / maxMovesForBar);
        PositionMarker(starMarkers[(int)StarIndex.Two], (float)twoStar / maxMovesForBar);
        PositionMarker(starMarkers[(int)StarIndex.One], oneStarMarkerPosition);
    }

    /// <summary>
    /// Safely sets the fill amount of the bar image.
    /// </summary>
    private void SetFillAmount(float amount)
    {
        if (fillImage != null)
            fillImage.fillAmount = amount;
    }

    /// <summary>
    /// Safely sets the color of a star text.
    /// </summary>
    private void SetStarColor(int index, Color color)
    {
        if (starTexts != null && index >= 0 && index < starTexts.Length && starTexts[index] != null)
            starTexts[index].color = color;
    }

    /// <summary>
    /// Plays the star loss animation and manages coroutine lifecycle.
    /// </summary>
    private void PlayStarLossAnimation(int index)
    {
        // Stop any existing coroutine for this star
        if (activeStarRoutines[index] != null)
            StopCoroutine(activeStarRoutines[index]);

        activeStarRoutines[index] = StartCoroutine(StarLossRoutine(index));
    }

    /// <summary>
    /// Animates a star being lost with flash, pop, and audio feedback.
    /// </summary>
    private IEnumerator StarLossRoutine(int index)
    {
        if (starTexts == null || index >= starTexts.Length)
            yield break;

        TextMeshProUGUI txt = starTexts[index];
        RectTransform rt = starTextRectTransforms?[index];
        
        if (txt == null || rt == null)
            yield break;

        Vector3 originalScale = Vector3.one;

        // Sound feedback
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayError();

        // Flash the bar briefly
        StartCoroutine(BarFlashRoutine());

        // Animate star: Flash color and scale pop
        float elapsed = 0;
        while (elapsed < starLossAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / starLossAnimationDuration;

            // Flash color: Red -> Gray
            txt.color = Color.Lerp(lossFlashColor, inactiveColor, t);

            // Scale pop: 1.0 -> (1.0 + popEffectScale) -> 1.0
            float s = 1.0f + Mathf.Sin(t * Mathf.PI) * popEffectScale;
            rt.localScale = originalScale * s;

            yield return null;
        }

        // Ensure final state is clean
        rt.localScale = originalScale;
        txt.color = inactiveColor;
        activeStarRoutines[index] = null;
    }

    /// <summary>
    /// Briefly flashes the fill bar red during star loss.
    /// </summary>
    private IEnumerator BarFlashRoutine()
    {
        if (fillImage == null)
            yield break;

        Color originalColor = fillImage.color;
        fillImage.color = lossFlashColor;
        yield return new WaitForSeconds(barFlashDuration);
        fillImage.color = originalColor;
    }

    private void PositionMarker(RectTransform marker, float t)
    {
        if (marker == null) return;
        marker.anchorMin = new Vector2(t, marker.anchorMin.y);
        marker.anchorMax = new Vector2(t, marker.anchorMax.y);
        marker.anchoredPosition = new Vector2(0, marker.anchoredPosition.y);
    }
}
