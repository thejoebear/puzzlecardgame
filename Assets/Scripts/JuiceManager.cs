using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class JuiceManager : MonoBehaviour
{
    public static JuiceManager Instance { get; private set; }

    public Volume postProcessVolume;
    private Bloom bloom;
    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out bloom);
            postProcessVolume.profile.TryGet(out colorAdjustments);
        }
    }

    public void PulseBloom(float targetIntensity, float duration)
    {
        if (bloom != null) StartCoroutine(BloomPulseRoutine(targetIntensity, duration));
    }

    private IEnumerator BloomPulseRoutine(float target, float duration)
    {
        float start = bloom.intensity.value;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bloom.intensity.Override(Mathf.Lerp(start, target, Mathf.PingPong(elapsed * 2 / duration, 1f)));
            yield return null;
        }
        bloom.intensity.Override(start);
    }

    public void FlashColor(Color color, float duration)
    {
        // For simple feedback, we could flash a UI overlay or shift saturation
        if (colorAdjustments != null) StartCoroutine(SaturationFlashRoutine(duration));
    }

    private IEnumerator SaturationFlashRoutine(float duration)
    {
        float start = colorAdjustments.saturation.value;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            colorAdjustments.saturation.Override(Mathf.Lerp(start, start + 30f, Mathf.PingPong(elapsed * 2 / duration, 1f)));
            yield return null;
        }
        colorAdjustments.saturation.Override(start);
    }
}
