using System.Collections.Generic;
using UnityEngine;

public class CardPool : MonoBehaviour
{
    public static CardPool Instance { get; private set; }

    public GameObject cardPrefab;
    public int initialPoolSize = 60;

    private readonly Stack<GameObject> pool = new Stack<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (cardPrefab != null)
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewCard();
            }
        }
    }

    private GameObject CreateNewCard()
    {
        GameObject card = Instantiate(cardPrefab, transform);
        card.SetActive(false);
        pool.Push(card);
        return card;
    }

    public GameObject GetCard(Transform parent)
    {
        GameObject card;
        if (pool.Count > 0)
        {
            card = pool.Pop();
        }
        else
        {
            card = Instantiate(cardPrefab);
        }

        card.transform.SetParent(parent);
        card.SetActive(true);
        return card;
    }

    public void ReturnCard(GameObject card)
    {
        if (card == null) return;

        // Strip runtime drag handlers so pooled redeals don't stack components
        foreach (var drag in card.GetComponents<CardDraggable>())
        {
            if (Application.isPlaying) Object.Destroy(drag);
            else Object.DestroyImmediate(drag);
        }

        CardDisplay display = card.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.SetMystery(false);
            display.Flip(false);
            display.SetLift(0f);
        }

        CardMotion motion = card.GetComponent<CardMotion>();
        if (motion != null)
            motion.Cancel();

        card.transform.localScale = Vector3.one;
        card.transform.localRotation = Quaternion.identity;
        card.transform.localPosition = Vector3.zero;
        card.SetActive(false);
        card.transform.SetParent(transform);
        pool.Push(card);
    }
}
