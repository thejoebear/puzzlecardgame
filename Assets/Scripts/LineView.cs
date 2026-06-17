using UnityEngine;
using UnityEngine.UI;

public class LineView : MonoBehaviour
{
    public RectTransform rectTransform;
    public Image image;

    private void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (image == null) image = GetComponent<Image>();
    }
}
