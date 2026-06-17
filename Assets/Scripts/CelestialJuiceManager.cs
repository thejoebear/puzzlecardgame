using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class CelestialJuiceManager : MonoBehaviour
{
    public static CelestialJuiceManager Instance;

    public Volume postProcessVolume;
    private Bloom bloom;
    private ChromaticAberration chromaticAberration;
    private ColorAdjustments colorAdjustments;

    private float baseBloomIntensity;
    private float baseChromaticIntensity;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (postProcessVolume != null && postProcessVolume.sharedProfile != null)
        {
            postProcessVolume.sharedProfile.TryGet(out bloom);
            postProcessVolume.sharedProfile.TryGet(out chromaticAberration);
            postProcessVolume.sharedProfile.TryGet(out colorAdjustments);
        }

        if (bloom != null) baseBloomIntensity = bloom.intensity.value;
        if (chromaticAberration != null) baseChromaticIntensity = chromaticAberration.intensity.value;
    }

    public void PulseBloom(float boost, float duration)
    {
        if (bloom == null) return;
        StopCoroutine("BloomPulseRoutine");
        StartCoroutine(BloomPulseRoutine(boost, duration));
    }

    private IEnumerator BloomPulseRoutine(float boost, float duration)
    {
        float elapsed = 0;
        float targetIntensity = baseBloomIntensity + boost;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease out
            float curve = 1.0f - Mathf.Pow(t - 1.0f, 2.0f); 
            bloom.intensity.value = Mathf.Lerp(targetIntensity, baseBloomIntensity, t);
            yield return null;
        }
        bloom.intensity.value = baseBloomIntensity;
    }

    public void SetNebulaJuice(bool active, float transitionTime = 1.0f)
    {
        StopCoroutine("NebulaTransitionRoutine");
        StartCoroutine(NebulaTransitionRoutine(active, transitionTime));
    }

    private IEnumerator NebulaTransitionRoutine(bool active, float duration)
    {
        float elapsed = 0;
        float startChrom = chromaticAberration != null ? chromaticAberration.intensity.value : 0;
        float targetChrom = active ? 0.25f : baseChromaticIntensity;
        
        float startSat = colorAdjustments != null ? colorAdjustments.saturation.value : 0;
        float targetSat = active ? 30f : 10f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (chromaticAberration != null) chromaticAberration.intensity.value = Mathf.Lerp(startChrom, targetChrom, t);
            if (colorAdjustments != null) colorAdjustments.saturation.value = Mathf.Lerp(startSat, targetSat, t);
            yield return null;
        }
    }
}
