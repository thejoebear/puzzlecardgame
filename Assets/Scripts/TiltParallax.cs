using UnityEngine;
using UnityEngine.InputSystem;

public class TiltParallax : MonoBehaviour
{
    public float maxTiltAngle = 3f;
    public float smoothing = 5f;

    private Quaternion targetRotation;

    void Update()
    {
        if (Mouse.current == null) return;

        // Get mouse position in normalized screen coordinates (-1 to 1)
        Vector2 mousePos = Mouse.current.position.ReadValue();
        float x = (mousePos.x / Screen.width) * 2f - 1f;
        float y = (mousePos.y / Screen.height) * 2f - 1f;

        // Calculate target rotation based on mouse position
        float tiltX = -y * maxTiltAngle;
        float tiltY = x * maxTiltAngle;

        targetRotation = Quaternion.Euler(tiltX, tiltY, 0);

        // Apply rotation with smoothing
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smoothing);
    }
}
