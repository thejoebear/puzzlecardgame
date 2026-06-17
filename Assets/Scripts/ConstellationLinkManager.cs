using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ConstellationLinkManager : MonoBehaviour
{
    public Sprite linkSprite;
    private List<Image> activeLinks = new List<Image>();
    private List<Image> linkPool = new List<Image>();

    public void UpdateLinks()
    {
        // 1. Clear existing links
        foreach (var link in activeLinks)
        {
            link.gameObject.SetActive(false);
            linkPool.Add(link);
        }
        activeLinks.Clear();

        // 2. Scan children for same-category stacks
        // Skip index 0 as that is usually the Highlight
        List<CardDisplay> cards = new List<CardDisplay>();
        foreach (Transform child in transform)
        {
            if (child.name == "Highlight" || child.name == "LinkContainer") continue;
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if (cd != null && cd.IsFaceUp && !cd.IsMystery)
            {
                cards.Add(cd);
            }
        }

        if (cards.Count < 2) return;

        for (int i = 0; i < cards.Count - 1; i++)
        {
            CardDisplay c1 = cards[i];
            CardDisplay c2 = cards[i + 1];

            if (c1.cardData.category == c2.cardData.category)
            {
                CreateLink(c1.transform.localPosition, c2.transform.localPosition, c1.categoryText.color);
            }
        }
    }

    private void Update()
    {
        float pulse = 0.7f + Mathf.PingPong(Time.time * 2f, 0.3f);
        foreach (var link in activeLinks)
        {
            if (link != null)
            {
                Color c = link.color;
                c.a = pulse;
                link.color = c;
            }
        }
    }

    private void CreateLink(Vector3 p1, Vector3 p2, Color color)
{
        Image link = GetLinkImage();
        activeLinks.Add(link);
        
        RectTransform rt = link.rectTransform;
        
        // Midpoint
        Vector3 direction = p2 - p1;
        float distance = direction.magnitude;
        
        rt.localPosition = p1 + direction * 0.5f;
        rt.sizeDelta = new Vector2(distance, 10f); // Width is the distance between cards
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0, 0, angle);
        
        link.color = color;
        link.gameObject.SetActive(true);
    }

    private Image GetLinkImage()
    {
        if (linkPool.Count > 0)
        {
            Image img = linkPool[linkPool.Count - 1];
            linkPool.RemoveAt(linkPool.Count - 1);
            return img;
        }

        GameObject go = new GameObject("CelestialLink", typeof(RectTransform), typeof(Image));
        
        // Find or create container
        Transform container = transform.Find("LinkContainer");
        if (container == null)
        {
            GameObject containerGo = new GameObject("LinkContainer", typeof(RectTransform));
            containerGo.transform.SetParent(transform, false);
            containerGo.transform.SetAsFirstSibling(); // Behind cards
            container = containerGo.transform;
        }
        
        go.transform.SetParent(container, false);
        Image imgComp = go.GetComponent<Image>();
        imgComp.sprite = linkSprite;
        imgComp.raycastTarget = false;
        return imgComp;
    }
}
