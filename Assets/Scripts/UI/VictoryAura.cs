using UnityEngine;
using UnityEngine.UI;

public class VictoryAura : MonoBehaviour
{
    [Header("Rotation")]
    public float rotateSpeed = 15.0f;

    [Header("Pulsing")]
    public float pulseSpeed = 0.8f;
    public float minScale = 0.85f;
    public float maxScale = 1.15f;
    public float baseScale = 1.0f;

    [Header("Fading")]
    public float fadeSpeed = 2.0f;
    private Image auraImage;
    private float targetAlpha = 0.4f;

    void Awake()
    {
        auraImage = GetComponent<Image>();
        if (auraImage != null)
        {
            Color c = auraImage.color;
            c.a = 0;
            auraImage.color = c;
        }
    }

    void OnEnable()
    {
        transform.localScale = Vector3.one * baseScale;
        if (auraImage != null) StartCoroutine(FadeIn());
    }

    void Update()
    {
        // Rotation
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);

        // Pulse
        float s = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) / 2.0f);
        transform.localScale = new Vector3(s, s, 1) * baseScale;
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float elapsed = 0;
        Color c = auraImage.color;
        while (elapsed < 1.0f)
        {
            elapsed += Time.deltaTime * fadeSpeed;
            c.a = Mathf.Lerp(0, targetAlpha, elapsed);
            auraImage.color = c;
            yield return null;
        }
    }
}
