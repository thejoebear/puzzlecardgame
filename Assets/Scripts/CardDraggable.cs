using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CardDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private Transform originalParent;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private CardMotion motion;

    private bool isDragging = false;
    private Vector2 lastMousePos;
    private float velocityX;
    private List<Transform> stackChildren = new List<Transform>();
    private List<Vector3> childrenOriginalLocalPos = new List<Vector3>();

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        motion = GetComponent<CardMotion>();
        if (motion == null) motion = gameObject.AddComponent<CardMotion>();
    }

    private Vector3 originalLocalPos;
    private Vector3 originalScale;
    private GameObject shadowObj;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (motion.IsMoving) return;

        CardDisplay myDisplay = GetComponent<CardDisplay>();
        if (myDisplay == null || !myDisplay.IsFaceUp) return;

        // ... Identify stack logic remains ...
        stackChildren.Clear();
        childrenOriginalLocalPos.Clear();
        originalParent = transform.parent;
        
        List<CardData> sequenceData = new List<CardData>();
        sequenceData.Add(myDisplay.cardData);

        // Only allow stack dragging if parent is a Tableau
        Pile sourcePile = originalParent.GetComponent<Pile>();
        if (sourcePile != null && sourcePile.type == PileType.Tableau)
        {
            int myIndex = transform.GetSiblingIndex();
            for (int i = myIndex + 1; i < originalParent.childCount; i++)
            {
                Transform child = originalParent.GetChild(i);
                CardDisplay childDisplay = child.GetComponent<CardDisplay>();
                if (childDisplay != null)
                {
                    if (!childDisplay.IsFaceUp) return;
                    stackChildren.Add(child);
                    childrenOriginalLocalPos.Add(child.localPosition);
                    sequenceData.Add(childDisplay.cardData);
                }
            }

            if (!SolitaireRules.IsValidSequence(sequenceData))
            {
                stackChildren.Clear();
                childrenOriginalLocalPos.Clear();
                return;
            }
        }
        else if (transform.GetSiblingIndex() < originalParent.childCount - 1)
        {
            return;
        }

        isDragging = true;
        originalLocalPos = transform.localPosition;
        originalScale = transform.localScale;
        
        transform.SetParent(canvas.transform, true);
        
        // Lift Effect
        transform.localScale = originalScale * 1.05f;

        // Shadow Effect
        if (shadowObj == null)
        {
            shadowObj = new GameObject("DragShadow", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            shadowObj.transform.SetParent(transform, false);
            shadowObj.transform.SetAsFirstSibling();
            
            RectTransform sRT = shadowObj.GetComponent<RectTransform>();
            sRT.anchorMin = Vector2.zero;
            sRT.anchorMax = Vector2.one;
            sRT.sizeDelta = Vector2.zero;
            sRT.anchoredPosition = new Vector2(10, -10); // Offset for shadow

            UnityEngine.UI.Image sImg = shadowObj.GetComponent<UnityEngine.UI.Image>();
            sImg.color = new Color(0, 0, 0, 0.4f);
            sImg.raycastTarget = false;
            
            // Try to match the card sprite if possible, else solid color is fine
            UnityEngine.UI.Image myImg = GetComponent<UnityEngine.UI.Image>();
            if (myImg != null) sImg.sprite = myImg.sprite;
        }
        shadowObj.SetActive(true);
        
        // Parent stack to this card for movement
        foreach (var child in stackChildren)
        {
            child.SetParent(transform, true);
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.9f;
        lastMousePos = eventData.position;
        velocityX = 0;

        // Highlight valid targets
        SolitaireManager manager = Object.FindAnyObjectByType<SolitaireManager>();
        if (manager != null)
        {
            manager.HighlightValidMoves(myDisplay.cardData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        Vector2 currentMousePos = eventData.position;
        Vector2 delta = currentMousePos - lastMousePos;
        rectTransform.anchoredPosition += delta / canvas.scaleFactor;
        
        // Smoothly update velocity for tilt
        velocityX = Mathf.Lerp(velocityX, delta.x / Time.deltaTime, Time.deltaTime * 10f);
        float tilt = -velocityX * 0.005f; // Scale down for Euler angles
        transform.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(tilt, -20f, 20f));

        // Premium Fanning: cards spread slightly based on horizontal movement
        float fanStrength = Mathf.Abs(velocityX) * 0.002f;
        for (int i = 0; i < stackChildren.Count; i++)
        {
            Vector3 targetLocalPos = new Vector3(0, -(i + 1) * (30f + fanStrength), 0);
            stackChildren[i].localPosition = Vector3.Lerp(stackChildren[i].localPosition, targetLocalPos, Time.deltaTime * 15f);
        }

        lastMousePos = currentMousePos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;
        
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1.0f;
        transform.localRotation = Quaternion.identity;
        transform.localScale = originalScale;
        if (shadowObj != null) shadowObj.SetActive(false);

        // Clear highlights
SolitaireManager manager = Object.FindAnyObjectByType<SolitaireManager>();
        if (manager != null) manager.ClearHighlights();

        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;
        bool success = false;
        if (dropTarget != null)
        {
            if (manager != null && manager.TryMoveCard(gameObject, dropTarget, originalParent))
            {
                success = true;
            }
        }

        if (!success)
        {
            // Smooth return of the entire stack
            transform.SetParent(originalParent, true);
            motion.MoveTo(originalLocalPos, 0.2f, () => {
                transform.localPosition = originalLocalPos;
                // Re-parent stack back to the original pile
                for (int i = 0; i < stackChildren.Count; i++)
                {
                    stackChildren[i].SetParent(originalParent, true);
                    stackChildren[i].localPosition = childrenOriginalLocalPos[i];
                }
                stackChildren.Clear();
                
                // Shake at home to show rejection
                motion.Shake();
            });
            
            if (AudioManager.Instance != null) AudioManager.Instance.PlayError();
        }
else
{
            // Success is handled by SolitaireManager which re-parents properly
            stackChildren.Clear();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!eventData.dragging && eventData.clickCount == 2)
        {
            SolitaireManager manager = Object.FindAnyObjectByType<SolitaireManager>();
            if (manager != null)
            {
                manager.TryAutoMove(gameObject, transform.parent);
            }
        }
    }
}
