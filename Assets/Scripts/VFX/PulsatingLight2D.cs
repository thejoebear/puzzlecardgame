using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class PulsatingLight2D : MonoBehaviour
{
    public float minIntensity = 0.8f;
    public float maxIntensity = 1.2f;
    public float speed = 1.5f;

    private Light2D light2D;
    private float offset;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        offset = Random.Range(0f, 10f); // Randomized start
    }

    void Update()
    {
        float t = Mathf.PingPong(Time.time * speed + offset, 1f);
        light2D.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}
