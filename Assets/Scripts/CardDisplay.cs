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
            if (def != null)
            {
                if (backgroundImage != null)
                {
                    backgroundImage.color = def.themeColor;
                }
                if (iconImage != null)
                {
                    iconImage.color = def.themeColor;
                }
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
            if (rankText != null)
            {
                rankText.text = "?";
                rankText.gameObject.SetActive(true);
            }
            if (categoryText != null)
            {
                categoryText.text = "???";
                categoryText.color = Color.gray;
                categoryText.gameObject.SetActive(true);
            }
            if (factText != null)
            {
                factText.text = "This celestial archive is hidden in shadow. Sort it to reveal its truth.";
                factText.gameObject.SetActive(true);
            }
            if (iconImage != null) iconImage.gameObject.SetActive(false);
            if (borderImage != null)
            {
                borderImage.color = Color.gray;
                borderImage.gameObject.SetActive(true);
            }
            return;
        }

        bool isHeader = cardData.rank == 1;
        
        if (rankText != null)
        {
            rankText.text = "";
            rankText.gameObject.SetActive(false);
        }
        
        if (categoryText != null)
        {
            categoryText.text = "";
            categoryText.gameObject.SetActive(false);
        }
        
        if (factText != null)
        {
            factText.text = "";
            factText.gameObject.SetActive(false);
        }

        if (borderImage != null)
        {
            borderImage.gameObject.SetActive(false);
        }

        if (iconImage != null)
        {
            RectTransform iconRt = iconImage.GetComponent<RectTransform>();
            if (iconRt != null)
            {
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = Vector2.zero;
                iconRt.SetAsFirstSibling();
            }

            if (currentTheme != null)
            {
                var visuals = currentTheme.GetVisuals(cardData.category);
                if (visuals != null)
                {
                    bool useFullBleed = false;
                    if (isHeader)
                    {
                        iconImage.sprite = cardData.icon != null ? cardData.icon : visuals.categoryIcon;
                        useFullBleed = (cardData.icon != null);
                    }
                    else
                    {
                        iconImage.sprite = visuals.standardCardArt != null ? visuals.standardCardArt : visuals.categoryIcon;
                        useFullBleed = (visuals.standardCardArt != null);
                    }

                    if (iconRt != null)
                    {
                        if (useFullBleed)
                        {
                            iconRt.anchorMin = Vector2.zero;
                            iconRt.anchorMax = Vector2.one;
                            iconRt.sizeDelta = Vector2.zero;
                        }
                        else
                        {
                            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                            iconRt.sizeDelta = new Vector2(80f, 80f);
                        }
                    }

                    iconImage.gameObject.SetActive(iconImage.sprite != null);
                    iconImage.color = Color.white;
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }
            else
            {
                iconImage.sprite = isHeader ? cardData.icon : null;
                if (iconRt != null)
                {
                    if (iconImage.sprite != null)
                    {
                        iconRt.anchorMin = Vector2.zero;
                        iconRt.anchorMax = Vector2.one;
                        iconRt.sizeDelta = Vector2.zero;
                    }
                    else
                    {
                        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                        iconRt.sizeDelta = new Vector2(80f, 80f);
                    }
                }
                iconImage.gameObject.SetActive(iconImage.sprite != null);
                iconImage.color = Color.white;
            }
        }

        if (currentTheme != null)
        {
            var visuals = currentTheme.GetVisuals(cardData.category);
            if (visuals != null)
            {
                if (borderImage != null && borderImage.gameObject.activeSelf)
                {
                    Color glowColor = visuals.categoryColor;
                    float h, s, v;
                    Color.RGBToHSV(glowColor, out h, out s, out v);
                    glowColor = Color.HSVToRGB(h, s, Mathf.Max(v, 1.5f)); // HDR Boost
                    borderImage.color = glowColor;
                }
                if (categoryText != null)
                {
                    categoryText.color = visuals.categoryColor;
                }
            }
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
        UpdateAnomalyVisuals();
    }
}
