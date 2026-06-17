using UnityEngine;
using System.Collections;

public class CardMotion : MonoBehaviour
{
    private Coroutine moveCoroutine;
    private Coroutine shakeCoroutine;
    private CardDisplay cardDisplay;

    private void Awake()
    {
        cardDisplay = GetComponent<CardDisplay>();
    }
    
    public bool IsMoving => moveCoroutine != null;

    public void MoveTo(Vector3 localPosition, float duration = 0.2f, System.Action onComplete = null, Quaternion? localRotation = null, Vector3? localScale = null)
    {
        StopAll();
        moveCoroutine = StartCoroutine(MoveRoutine(localPosition, localRotation, localScale, duration, onComplete, false));
    }

    public void MoveToWorld(Vector3 worldPosition, float duration = 0.2f, System.Action onComplete = null, Quaternion? worldRotation = null, Vector3? targetScale = null)
    {
        StopAll();
        moveCoroutine = StartCoroutine(MoveRoutine(worldPosition, worldRotation, targetScale, duration, onComplete, true));
    }

    private void StopAll()
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        moveCoroutine = null;
        shakeCoroutine = null;
    }

    private IEnumerator MoveRoutine(Vector3 targetPos, Quaternion? targetRot, Vector3? targetScale, float duration, System.Action onComplete, bool worldSpace)
    {
        Vector3 startPos = worldSpace ? transform.position : transform.localPosition;
        Quaternion startRot = worldSpace ? transform.rotation : transform.localRotation;
        Vector3 startScale = transform.localScale;

        Quaternion endRot = targetRot ?? startRot;
        Vector3 endScale = targetScale ?? startScale;

        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Back-Out easing formula
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float easedT = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            
            float lift = Mathf.Sin(t * Mathf.PI);
            cardDisplay?.SetLift(lift);

            if (worldSpace)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, easedT);
                transform.rotation = Quaternion.Lerp(startRot, endRot, easedT);
            }
            else
            {
                transform.localPosition = Vector3.Lerp(startPos, targetPos, easedT);
                transform.localRotation = Quaternion.Lerp(startRot, endRot, easedT);
            }
            
            float liftScale = 1f + (lift * 0.05f);
            transform.localScale = Vector3.Lerp(startScale, endScale, easedT) * liftScale;
            yield return null;
}

        cardDisplay?.SetLift(0);

        if (worldSpace)
        {
            transform.position = targetPos;
            transform.rotation = endRot;
        }
        else
        {
            transform.localPosition = targetPos;
            transform.localRotation = endRot;
        }
        transform.localScale = endScale;
        moveCoroutine = null;
        onComplete?.Invoke();
    }

    public void Shake()
    {
        StopAll();
        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0;
        float duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-5f, 5f);
            transform.localPosition = originalPos + new Vector3(x, 0, 0);
            yield return null;
        }
        transform.localPosition = originalPos;
        shakeCoroutine = null;
    }
}
