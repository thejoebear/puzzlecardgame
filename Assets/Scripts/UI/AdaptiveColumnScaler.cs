using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class AdaptiveColumnScaler : MonoBehaviour
{
    [Header("References")]
    public RectTransform tableausContainer;
    
    [Header("Settings")]
    public float targetColumnWidth = 200f; 
    public float minSpacing = 30f;
    public float maxSpacing = 100f;
    public float horizontalPadding = 120f; // Total L+R padding
    public bool useDynamicSpacing = true;
    public float maxScale = 1.0f;
    public float minScale = 0.4f;

    [Header("Optional Alignment")]
    public RectTransform topPilesContainer;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (tableausContainer == null || rectTransform == null) return;

        int columnCount = 0;
        foreach (Transform child in tableausContainer)
        {
            if (child.gameObject.activeSelf && child.name != "Highlight")
                columnCount++;
        }

        if (columnCount == 0) 
        {
            transform.localScale = Vector3.one;
            return;
        }

        HorizontalLayoutGroup tableauHLG = tableausContainer.GetComponent<HorizontalLayoutGroup>();

        // 1. Calculate base needed width at 1.0 scale with minSpacing
        float baseNeededWidth = (columnCount * targetColumnWidth) + ((columnCount - 1) * minSpacing) + horizontalPadding;
        if (tableauHLG != null)
        {
            baseNeededWidth += tableauHLG.padding.left + tableauHLG.padding.right;
        }
        float availableWidth = rectTransform.rect.width;

        if (availableWidth > 0 && baseNeededWidth > availableWidth)
        {
            // Case: Too many columns, need to scale down
            float scale = availableWidth / baseNeededWidth;
            scale = Mathf.Clamp(scale, minScale, maxScale);
            transform.localScale = new Vector3(scale, scale, 1f);
            
            if (tableauHLG != null) tableauHLG.spacing = minSpacing;
        }
        else
        {
            // Case: Columns fit at scale 1.0, potentially spread them out
            transform.localScale = Vector3.one;

            if (useDynamicSpacing && columnCount > 1 && tableauHLG != null)
            {
                float extraWidth = availableWidth - baseNeededWidth;
                float extraSpacing = extraWidth / (columnCount - 1);
                tableauHLG.spacing = Mathf.Clamp(minSpacing + extraSpacing, minSpacing, maxSpacing);
            }
            else if (tableauHLG != null)
            {
                tableauHLG.spacing = minSpacing;
            }
        }

        // Sync top piles spacing if they exist
        if (topPilesContainer != null && tableauHLG != null)
        {
            HorizontalLayoutGroup topHLG = topPilesContainer.GetComponent<HorizontalLayoutGroup>();
            if (topHLG != null)
            {
                // We want the top piles to span roughly the same width as the tableaus
                // If there are 2 groups (Draw/Foundations) on top, they should align with the outer tableaus
                if (columnCount > 1)
                {
                    float tableauTotalWidth = (columnCount * targetColumnWidth) + ((columnCount - 1) * tableauHLG.spacing);
                    
                    float stockWidth = 0f;
                    float foundationWidth = 0f;

                    if (topPilesContainer.childCount >= 2)
                    {
                        RectTransform stockRT = topPilesContainer.GetChild(0) as RectTransform;
                        RectTransform foundationRT = topPilesContainer.GetChild(1) as RectTransform;

                        if (stockRT != null)
                        {
                            LayoutElement stockLE = stockRT.GetComponent<LayoutElement>();
                            stockWidth = (stockLE != null && stockLE.preferredWidth > 0) ? stockLE.preferredWidth : stockRT.rect.width;
                        }
                        if (foundationRT != null)
                        {
                            LayoutElement foundationLE = foundationRT.GetComponent<LayoutElement>();
                            foundationWidth = (foundationLE != null && foundationLE.preferredWidth > 0) ? foundationLE.preferredWidth : foundationRT.rect.width;
                        }
                    }

                    // Fallback to defaults if not found
                    if (stockWidth <= 0) stockWidth = 320f;
                    if (foundationWidth <= 0) foundationWidth = 660f;

                    float neededSpacing = (tableauTotalWidth / 2f) - (foundationWidth / 2f) - stockWidth;
                    topHLG.spacing = Mathf.Max(30f, neededSpacing);
                    
                    topHLG.padding.left = tableauHLG.padding.left;
                    topHLG.padding.right = tableauHLG.padding.right;
                }
            }
        }
    }
}
