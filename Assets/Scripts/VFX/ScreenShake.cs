using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance;

    private Vector3 originalPos;
    private float currentDuration;
    private float currentAmount;

    void Awake()
    {
        Instance = this;
        originalPos = transform.localPosition;
    }

    public void Shake(float duration = 0.2f, float amount = 5f)
    {
        currentDuration = duration;
        currentAmount = amount;
    }

    void Update()
    {
        if (currentDuration > 0)
        {
            transform.localPosition = originalPos + Random.insideUnitSphere * currentAmount;
            currentDuration -= Time.deltaTime;
            
            if (currentDuration <= 0)
            {
                transform.localPosition = originalPos;
            }
        }
    }
}
