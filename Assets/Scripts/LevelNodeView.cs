using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class LevelNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform rectTransform;
    public Button button;
    public Image image;
    public Image glowImage;
    public TextMeshProUGUI label;
    public GameObject[] starIndicators;

    [Header("Interaction Settings")]
    public float hoverScale = 1.1f;
    public float pressScale = 0.9f;
    public float transitionSpeed = 10f;

    private Vector3 targetScale = Vector3.one;

    [Header("State Colors")]
    public float glowIntensity = 1.5f;
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public Color lockedGlow = new Color(0.0f, 1.0f, 1.0f, 0.15f);
    
    public Color currentGlow = new Color(0.8f, 1.0f, 1.0f, 1.0f);
    public Color nebulaGlow = new Color(1.0f, 0.5f, 1.0f, 1.0f);
    public Color currentLabel = Color.white;

    public Color completedGlow1Star = new Color(0.8f, 0.5f, 0.2f, 0.8f); // Bronze
    public Color completedGlow2Star = new Color(0.7f, 0.7f, 0.8f, 0.8f); // Silver
    public Color completedGlow3Star = new Color(1.0f, 0.9f, 0.4f, 1.0f); // Gold

    private bool isPulsing = false;
    private float pulseTime = 0;

    public void SetState(bool isUnlocked, bool isCurrent, int stars, string levelName, bool isNebula, bool isBoss = false)
    {
        if (button != null) button.interactable = isUnlocked;

        isPulsing = isCurrent;
        
        // Scale boss nodes slightly larger
        targetScale = isBoss ? Vector3.one * 1.5f : Vector3.one;
        float currentHoverScale = isBoss ? hoverScale * 1.3f : hoverScale;

        if (!isUnlocked)
        {
            if (image != null) image.color = lockedColor;
            if (glowImage != null)
            {
                Color c = isNebula ? new Color(0.5f, 0.0f, 0.5f, 0.2f) : lockedGlow;
                glowImage.color = new Color(c.r * glowIntensity, c.g * glowIntensity, c.b * glowIntensity, c.a);
            }
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = "???";
                label.color = new Color(1, 1, 1, 0.5f); // Dimmed but visible
            }
        }
        else if (isCurrent)
        {
            if (image != null) image.color = Color.white;
            if (glowImage != null)
            {
                Color c = isNebula ? nebulaGlow : currentGlow;
                glowImage.color = new Color(c.r * glowIntensity, c.g * glowIntensity, c.b * glowIntensity, c.a);
            }
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = levelName;
                label.color = currentLabel;
            }
        }
        else // Completed (Unlocked but not current)
        {
            if (image != null) image.color = Color.white;
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = levelName;
                label.color = Color.white;
            }

            if (glowImage != null)
            {
                if (stars > 0)
                {
                    Color c = Color.white;
                    switch (stars)
                    {
                        case 3: c = completedGlow3Star; break;
                        case 2: c = completedGlow2Star; break;
                        case 1: c = completedGlow1Star; break;
                    }
                    glowImage.color = new Color(c.r * glowIntensity, c.g * glowIntensity, c.b * glowIntensity, c.a);
                }
                else
                {
                    Color c = isNebula ? new Color(1.0f, 0.5f, 1.0f, 0.5f) : new Color(0.6f, 0.8f, 1.0f, 0.5f);
                    glowImage.color = new Color(c.r * glowIntensity, c.g * glowIntensity, c.b * glowIntensity, c.a);
                }
            }
        }

        if (starIndicators != null)
        {
            for (int i = 0; i < starIndicators.Length; i++)
            {
                bool active = i < stars;
                if (active && !starIndicators[i].activeSelf)
                {
                    // If turning on, trigger pop
                    starIndicators[i].SetActive(true);
                    StartCoroutine(PopStar(starIndicators[i].transform, i * 0.1f));
                }
                else
                {
                    starIndicators[i].SetActive(active);
                }
            }
        }
    }

    private System.Collections.IEnumerator PopStar(Transform star, float delay)
    {
        star.localScale = Vector3.zero;
        yield return new WaitForSeconds(delay);
        
        float elapsed = 0;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Overshoot curve: 0 -> 1.2 -> 1.0
            float s = 0;
            if (t < 0.7f) s = Mathf.Lerp(0, 1.2f, t / 0.7f);
            else s = Mathf.Lerp(1.2f, 1.0f, (t - 0.7f) / 0.3f);
            
            star.localScale = Vector3.one * s;
            yield return null;
        }
        star.localScale = Vector3.one;
    }

    private void Update()
{
        // Smooth scaling
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * transitionSpeed);

        if (isPulsing && glowImage != null)
        {
            pulseTime += Time.deltaTime * 3f;
            float alpha = 0.5f + Mathf.PingPong(pulseTime, 0.5f);
            Color c = glowImage.color;
            c.a = alpha;
            glowImage.color = c;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.interactable)
            targetScale = Vector3.one * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && button.interactable)
            targetScale = Vector3.one * pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button != null && button.interactable)
            targetScale = eventData.hovered.Contains(gameObject) ? Vector3.one * hoverScale : Vector3.one;
    }

    private void Awake()
{
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (button == null) button = GetComponent<Button>();
        if (image == null) image = GetComponent<Image>();
        if (image != null) image.alphaHitTestMinimumThreshold = 0.5f;
        
        if (glowImage == null)
        {
            Transform glow = transform.Find("Glow");
            if (glow != null) glowImage = glow.GetComponent<Image>();
        }
        
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();
        
        if (starIndicators == null || starIndicators.Length == 0)
        {
            Transform stars = transform.Find("Stars");
            if (stars != null)
            {
                starIndicators = new GameObject[stars.childCount];
                for (int i = 0; i < stars.childCount; i++)
                {
                    starIndicators[i] = stars.GetChild(i).gameObject;
                }
            }
        }
    }
}
