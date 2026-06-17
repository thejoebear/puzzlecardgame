using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class VictoryStarIgnition : MonoBehaviour
{
    [Header("Components")]
    public Image[] starIcons; // Array of 3 star icons
    public GameObject sparklePrefab; // Optional sparkle effect

    [Header("Visual Settings")]
    public Color inactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
    public Color activeColor = new Color(1.0f, 0.9f, 0.4f, 1.0f); // Celestial Gold
    public float initialDelay = 0f;
    public float delayBetweenStars = 0.4f;
    public float scalePopAmount = 1.4f;
    public float popDuration = 0.4f;

    public void IgniteStars(int starCount)
    {
        // Reset state
        for (int i = 0; i < starIcons.Length; i++)
        {
            if (starIcons[i] != null)
            {
                starIcons[i].color = inactiveColor;
                starIcons[i].transform.localScale = Vector3.one;
            }
        }
        
        StopAllCoroutines();
        StartCoroutine(IgniteRoutine(starCount));
    }

    private IEnumerator IgniteRoutine(int starCount)
    {
        yield return new WaitForSeconds(initialDelay);

        for (int i = 0; i < starIcons.Length; i++)
        {
            if (i < starCount)
            {
                // Star is earned
                StartCoroutine(AnimateStar(starIcons[i], true));
                
                // Play pop sound
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCardFlick(); // Use flick as a temp pop sound
                
                // Optional: Spawn stardust/sparkle at star position
                if (CelestialVFXManager.Instance != null)
                {
                    // If there's a sparkle prefab we can use
                }
            }
            else
            {
                // Star not earned - stays inactive but maybe does a tiny "thud" or just stays gray
                starIcons[i].color = inactiveColor;
            }

            yield return new WaitForSeconds(delayBetweenStars);
        }
    }

    private IEnumerator AnimateStar(Image star, bool isActive)
    {
        if (star == null) yield break;

        float elapsed = 0;
        Vector3 initialScale = Vector3.one;
        
        // Color transition
        star.color = activeColor;

        // Scale pop animation
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            
            // Elastic pop curve
            float s = 1.0f + Mathf.Sin(t * Mathf.PI) * (scalePopAmount - 1.0f);
            star.transform.localScale = initialScale * s;
            
            yield return null;
        }
        
        star.transform.localScale = initialScale;
    }
}
