using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class UIButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scaling Settings")]
    public float hoverScale = 1.05f;
    public float clickScale = 0.95f;
    public float transitionDuration = 0.1f;

    [Header("Color Settings")]
    public float hoverBrightenFactor = 1.2f;
    public bool useColorTint = true;

    [Header("Pulse Settings")]
    public bool pulseOnEnable = false;
    public float pulseScaleAmount = 0.05f;
    public float pulseSpeed = 2.0f;

    private Vector3 originalScale;
    private Color originalColor;
    private Image targetImage;
    private Coroutine scaleCoroutine;
    private Coroutine pulseCoroutine;
    private bool isPulsing = false;
    private bool isHovered = false;

    void Awake()
    {
        originalScale = transform.localScale;
        targetImage = GetComponent<Image>();
        if (targetImage != null) originalColor = targetImage.color;
    }

    void OnEnable()
    {
        if (pulseOnEnable) SetPulse(true);
    }

    void OnDisable()
    {
        SetPulse(false);
        transform.localScale = originalScale;
        if (targetImage != null) targetImage.color = originalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        DoScale(originalScale * hoverScale);
        if (useColorTint && targetImage != null)
        {
            targetImage.color = originalColor * hoverBrightenFactor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        DoScale(originalScale);
        if (useColorTint && targetImage != null)
        {
            targetImage.color = originalColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        DoScale(originalScale * clickScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        DoScale(isHovered ? originalScale * hoverScale : originalScale);
    }

    public void SetPulse(bool active)
    {
        if (active == isPulsing) return;
        isPulsing = active;

        if (isPulsing)
        {
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseRoutine());
        }
        else
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
            if (!isHovered) transform.localScale = originalScale;
        }
    }

    private void DoScale(Vector3 targetScale)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleRoutine(targetScale));
    }

    public void Shake()
    {
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Vector3 pos = transform.localPosition;
        float elapsed = 0;
        float duration = 0.25f;
        float magnitude = 5f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = new Vector3(pos.x + x, pos.y + y, pos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = pos;
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / transitionDuration);
            yield return null;
        }
        transform.localScale = targetScale;
    }

    private IEnumerator PulseRoutine()
    {
        while (isPulsing)
        {
            if (!isHovered)
            {
                float wave = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
                transform.localScale = originalScale * (1.0f + wave * pulseScaleAmount);
            }
            yield return null;
        }
    }
}
