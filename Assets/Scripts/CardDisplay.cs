using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    public CardData cardData;
    public Image backgroundImage;
    public Image iconImage;
    public Image borderImage;
    public TextMeshProUGUI rankText;
public TextMeshProUGUI categoryText;
    public TextMeshProUGUI factText;
    public GameObject backVisuals;
    public GameObject frontVisuals;
    private Shadow[] shadows;

    private bool isFaceUp = false;
    public bool IsFaceUp => isFaceUp;

    private bool isMystery = false;
    public bool IsMystery => isMystery;

    private void OnEnable()
    {
        shadows = GetComponentsInChildren<Shadow>(true);
        if (CardRegistry.Instance != null)
        {
            CardRegistry.Instance.Register(this);
        }
    }

    public void SetLift(float lift)
    {
        float baseDistance = 2f;
        float maxDistance = 15f;
        float currentDistance = Mathf.Lerp(baseDistance, maxDistance, lift);

        if (shadows != null)
        {
            foreach (var shadow in shadows)
            {
                if (shadow != null)
                {
                    shadow.effectDistance = new Vector2(currentDistance, -currentDistance);
                }
            }
        }
        transform.localScale = Vector3.one * (1f + (lift * 0.05f));
    }

    private void OnDisable()
    {
        if (CardRegistry.Instance != null)
        {
            CardRegistry.Instance.Unregister(this);
        }
    }

    public void SetCard(CardData data)
{
        cardData = data;
        UpdateVisuals();
        UpdateAnomalyVisuals();
    }

    private void UpdateAnomalyVisuals()
    {
        if (cardData == null || cardData.anomaly == AnomalyType.None) return;

        if (AnomalySystem.Instance != null)
        {
            var def = AnomalySystem.Instance.GetDefinition(cardData.anomaly);
            if (def != null && backgroundImage != null)
            {
                backgroundImage.color = def.themeColor;
            }
        }
    }

    public void SetMystery(bool mystery)
{
        isMystery = mystery;
        UpdateVisuals();
    }

    public void RevealMystery()
    {
        if (isMystery)
        {
            isMystery = false;
            UpdateVisuals();
            // Optional: Juice/VFX
            GetComponent<CardMotion>()?.Shake();
        }
    }

    public void Flip(bool faceUp)
    {
        isFaceUp = faceUp;
        frontVisuals.SetActive(isFaceUp);
        backVisuals.SetActive(!isFaceUp);
    }

    private VisualTheme currentTheme;

    public void UpdateVisuals()
    {
        if (cardData == null) return;

        if (isMystery)
        {
            rankText.text = "?";
            categoryText.text = "???";
            factText.text = "This celestial archive is hidden in shadow. Sort it to reveal its truth.";
            if (iconImage != null) iconImage.gameObject.SetActive(false);
            if (borderImage != null) borderImage.color = Color.gray;
            categoryText.color = Color.gray;
            return;
        }

        bool isHeader = cardData.rank == 1;
rankText.text = isHeader ? "TOPIC" : cardData.GetRankString();
        categoryText.text = cardData.category.ToString();
        factText.text = cardData.fact;
        
        rankText.fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;

        if (currentTheme != null)
        {
            var visuals = currentTheme.GetVisuals(cardData.category);
            if (visuals != null)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = visuals.categoryIcon;
                    iconImage.gameObject.SetActive(visuals.categoryIcon != null);
                    iconImage.color = Color.white; // Or keep original
                }

                if (borderImage != null)
                {
                    // Professional Polish: Boost saturation/intensity for Bloom
                    Color glowColor = visuals.categoryColor;
                    float h, s, v;
                    Color.RGBToHSV(glowColor, out h, out s, out v);
                    glowColor = Color.HSVToRGB(h, s, Mathf.Max(v, 1.5f)); // HDR Boost
                    borderImage.color = glowColor;
                }
                
                // Optional: color the category text or rank text
                categoryText.color = visuals.categoryColor;
}
        }
        else
        {
            if (iconImage != null) iconImage.gameObject.SetActive(false);
        }
    }

    public void ApplyTheme(VisualTheme theme)
    {
        if (theme == null) return;
        currentTheme = theme;

        bool isHeader = cardData != null && cardData.rank == 1;
        backgroundImage.color = isHeader ? theme.headerColor : theme.cardFrontColor;
        
        // Find back image component
        if (backVisuals != null)
        {
            Image backImage = backVisuals.GetComponent<Image>();
            if (backImage != null && theme.cardBackSprite != null)
            {
                backImage.sprite = theme.cardBackSprite;
            }
        }

        UpdateVisuals(); // Re-apply visuals with theme data
    }
}
