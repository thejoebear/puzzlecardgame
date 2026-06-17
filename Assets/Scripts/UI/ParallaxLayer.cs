using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("0 = fixed, 1 = move with camera/mouse, higher = move faster")]
    public Vector2 depthFactor = new Vector2(0.1f, 0.1f);
    public bool smoothMovement = true;
    public float smoothTime = 0.1f;

    private RectTransform rectTransform;
    private Vector2 initialPosition;
    private Vector2 targetOffset;
    private Vector2 currentVelocity;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // Get mouse position in normalized screen space (-1 to 1)
        Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
#else
        mousePos = Input.mousePosition;
#endif
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 normalizedMousePos = new Vector2(
            (mousePos.x / screenSize.x) * 2f - 1f,
            (mousePos.y / screenSize.y) * 2f - 1f
        );

        // Calculate offset based on depth
        targetOffset = new Vector2(
            normalizedMousePos.x * depthFactor.x * 100f,
            normalizedMousePos.y * depthFactor.y * 100f
        );

        if (smoothMovement)
        {
            rectTransform.anchoredPosition = Vector2.SmoothDamp(
                rectTransform.anchoredPosition, 
                initialPosition + targetOffset, 
                ref currentVelocity, 
                smoothTime
            );
        }
        else
        {
            rectTransform.anchoredPosition = initialPosition + targetOffset;
        }
    }
}
