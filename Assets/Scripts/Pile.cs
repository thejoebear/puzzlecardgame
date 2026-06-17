using UnityEngine;
using UnityEngine.UI;

public enum PileType
{
    Stock,
    Waste,
    Foundation,
    Tableau
}

public class Pile : MonoBehaviour
{
    public PileType type;
    public int index; // For foundation or tableau index
    public Image highlightImage;
    public Image silhouetteImage;
    public TMPro.TextMeshProUGUI countText;

    [Header("Refinement")]
    public float pulseSpeed = 1.5f;
    public float minHighlightAlpha = 0.15f;
    public float maxHighlightAlpha = 0.5f;
    public float silhouetteAlpha = 0.25f;
    public GameObject deckThicknessVisual; // New: Container for "fake" card layers to show depth
    public Sprite recycleSprite; // New: Icon for when stock is empty

    [Header("Foundation Milestones")]
    public Image auraImage;
    public float maxAuraScale = 1.2f;
    public float auraPulseSpeed = 0.25f;

    public bool isLocked = false;
    private GameObject lockOverlay;
    private Sprite originalSilhouette;

    private void Start()
    {
        if (silhouetteImage != null) originalSilhouette = silhouetteImage.sprite;
        if (auraImage != null) auraImage.gameObject.SetActive(false);
    }

    private float baseAuraScale = 1.0f;

    private void Update()
    {
        // ... (existing highlight logic)
        if (highlightImage != null && highlightImage.gameObject.activeSelf)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float alpha = Mathf.Lerp(minHighlightAlpha, maxHighlightAlpha, pulse);
            Color c = highlightImage.color;
            c.a = alpha;
            highlightImage.color = c;
        }

        // Aura pulse logic
        if (auraImage != null && auraImage.gameObject.activeSelf)
        {
            float pulse = Mathf.PingPong(Time.time * auraPulseSpeed, 0.05f); // Very gentle pulse
            auraImage.transform.localScale = Vector3.one * (baseAuraScale + pulse);
        }

        UpdateStockVisuals();
        UpdateWasteVisuals();
    }

    public void UpdateFoundationAura(int currentCount, int targetCount, Color color)
    {
        if (auraImage == null) return;
        
        if (currentCount == 0)
        {
            auraImage.gameObject.SetActive(false);
            return;
        }

        auraImage.gameObject.SetActive(true);
        Color auraColor = color;
        auraColor.a = 0.2f; 
        auraImage.color = auraColor;
        
        float progress = (float)currentCount / targetCount;
        baseAuraScale = 1.0f + (progress * 0.1f); 
        auraImage.transform.localScale = Vector3.one * baseAuraScale;
    }

    public void UpdateStockVisuals()
    {
        if (type != PileType.Stock) return;

        var manager = FindAnyObjectByType<SolitaireManager>();
        int cardCount = 0;
        if (manager != null && manager.stock != null)
        {
            cardCount = manager.stock.Count;
        }
        else
        {
            foreach (Transform t in transform)
            {
                if (t.name != "Highlight" && t.name != "LockOverlay" && t.name != "Silhouette" && t.gameObject.activeSelf)
                    cardCount++;
            }
        }

        // 1. Deck Thickness
        if (deckThicknessVisual != null)
        {
            deckThicknessVisual.SetActive(cardCount > 1);
            float thicknessScale = Mathf.Min(2.0f, 1f + (cardCount * 0.025f));
            deckThicknessVisual.transform.localScale = new Vector3(1, thicknessScale, 1);
        }

        bool hasCards = cardCount > 0;
        bool canReshuffle = manager != null && manager.waste != null && manager.waste.Count > 0;

        // Stock Pile button/image visibility
        var stockImage = GetComponent<UnityEngine.UI.Image>();
        if (stockImage != null)
        {
            stockImage.enabled = hasCards || canReshuffle;
        }

        var stockButton = GetComponent<UnityEngine.UI.Button>();
        if (stockButton != null)
        {
            stockButton.interactable = hasCards || canReshuffle;
        }

        // 2. Silhouette/Recycle Logic
        if (silhouetteImage != null)
        {
            bool showSilhouette = !hasCards && canReshuffle;
            silhouetteImage.gameObject.SetActive(showSilhouette);
            
            if (showSilhouette)
            {
                if (silhouetteImage.sprite != recycleSprite)
                {
                    silhouetteImage.sprite = recycleSprite;
                }
                
                // Add a subtle rotation to the recycle icon
                silhouetteImage.transform.localRotation = Quaternion.Euler(0, 0, Time.time * 50f);
            }
            else
            {
                silhouetteImage.transform.localRotation = Quaternion.identity;
            }
        }

        // 3. Count Text Overlay
        if (countText == null)
        {
            Transform existingText = transform.Find("CountText");
            if (existingText != null)
            {
                countText = existingText.GetComponent<TMPro.TextMeshProUGUI>();
            }
            else
            {
                GameObject txtGo = new GameObject("CountText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                txtGo.transform.SetParent(transform, false);
                txtGo.transform.SetAsLastSibling();
                
                RectTransform rt = txtGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                
                countText = txtGo.GetComponent<TMPro.TextMeshProUGUI>();
                countText.alignment = TMPro.TextAlignmentOptions.Center;
                countText.fontSize = 32f;
                countText.fontStyle = TMPro.FontStyles.Bold;
                countText.raycastTarget = false;
            }
        }

        // Clean up any duplicate CountTexts if they exist
        foreach (Transform child in transform)
        {
            if (child.name == "CountText" && (countText == null || child.gameObject != countText.gameObject))
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        countText.gameObject.SetActive(hasCards || canReshuffle);
        if (hasCards || canReshuffle)
        {
            countText.text = cardCount.ToString();
        }
    }

    public void UpdateWasteVisuals()
    {
        if (type != PileType.Waste) return;

        int cardCount = 0;
        foreach (Transform t in transform)
        {
            if (t.name != "Highlight" && t.name != "LockOverlay" && t.name != "Silhouette" && t.gameObject.activeSelf)
                cardCount++;
        }

        bool hasCards = cardCount > 0;

        var wasteImage = GetComponent<UnityEngine.UI.Image>();
        if (wasteImage != null)
        {
            wasteImage.enabled = hasCards;
        }

        if (silhouetteImage != null)
        {
            silhouetteImage.gameObject.SetActive(hasCards);
        }
    }

    public void SetAsRecycle(bool isRecycle)
    {
        if (silhouetteImage == null) return;
        silhouetteImage.sprite = isRecycle ? recycleSprite : originalSilhouette;
        silhouetteImage.gameObject.SetActive(true);
    }

    public void SetLocked(bool locked)
{
        isLocked = locked;
        if (isLocked)
        {
            if (lockOverlay == null)
            {
                lockOverlay = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
                lockOverlay.transform.SetParent(transform, false);
                lockOverlay.transform.SetAsLastSibling();
                RectTransform rt = lockOverlay.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                Image img = lockOverlay.GetComponent<Image>();
                img.color = new Color(0.05f, 0.05f, 0.15f, 0.9f); // Celestial Navy
                
                // Add a text icon
                GameObject lockIcon = new GameObject("Icon", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                lockIcon.transform.SetParent(lockOverlay.transform, false);
                TMPro.TextMeshProUGUI tmp = lockIcon.GetComponent<TMPro.TextMeshProUGUI>();
                tmp.text = "LOCKED";
                tmp.fontSize = 12;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.color = new Color(1f, 0.84f, 0f); // Gold
}
            lockOverlay.SetActive(true);
        }
        else if (lockOverlay != null)
        {
            lockOverlay.SetActive(false);
        }
    }

    public void SetHighlight(bool active, Color? color = null)
{
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(active);
            if (active && color.HasValue) highlightImage.color = color.Value;
        }
    }
}
