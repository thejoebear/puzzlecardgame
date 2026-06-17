using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GrandOrreryManager : MonoBehaviour, IDragHandler, IScrollHandler
{
    public RectTransform mapContainer;
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 3.0f;

    [Header("Level Markers")]
    public GameObject levelNodePrefab;
    public Transform nodesParent;

    private SolitaireLevelManager levelManager;
    private MainMenuManager mainMenuManager;

    [Header("Constellation Lines")]
    public GameObject linePrefab;

    // --- Optimization: Object Pooling ---
    private Stack<LevelNodeView> nodePool = new Stack<LevelNodeView>();
    private Stack<LineView> linePool = new Stack<LineView>();
    private List<LevelNodeView> activeNodes = new List<LevelNodeView>();
    private List<LineView> activeLines = new List<LineView>();

    void Awake()
    {
        levelManager = FindFirstObjectByType<SolitaireLevelManager>();
        mainMenuManager = FindFirstObjectByType<MainMenuManager>();
    }

    public void InitializeOrrery()
    {
        if (levelManager == null) levelManager = Object.FindAnyObjectByType<SolitaireLevelManager>(FindObjectsInactive.Include);
        if (mainMenuManager == null) mainMenuManager = Object.FindAnyObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        
        var prog = ProgressionManager.Instance;
        if (prog == null) prog = Object.FindAnyObjectByType<ProgressionManager>(FindObjectsInactive.Include);

        // Ensure MapContent is reset and centered
        if (mapContainer != null)
        {
            mapContainer.anchoredPosition = Vector2.zero;
            mapContainer.localScale = Vector3.one;
            mapContainer.localPosition = Vector3.zero;
            
            // Fix: Ensure the container itself is capable of receiving raycasts but doesn't obscure content
            var containerImg = mapContainer.GetComponent<UnityEngine.UI.Image>();
            if (containerImg != null) 
            {
                containerImg.color = new Color(0, 0, 0, 0); // Completely transparent
                containerImg.raycastTarget = true; 
            }
        }

        // Return current active objects to their respective pools
        foreach (var node in activeNodes)
        {
            if (node != null)
            {
                node.gameObject.SetActive(false);
                nodePool.Push(node);
            }
        }
        activeNodes.Clear();

        foreach (var line in activeLines)
        {
            if (line != null)
            {
                line.gameObject.SetActive(false);
                linePool.Push(line);
            }
        }
        activeLines.Clear();

        // Create nodes for each level
        if (levelManager != null && prog != null)
        {
            List<Vector2> nodePositions = new List<Vector2>();
            for (int i = 0; i < levelManager.levels.Count; i++)
            {
                bool isUnlocked = prog.unlockAllLevels || prog.IsLevelUnlocked(i);
                int stars = prog.GetStars(i);
                
                Vector2 pos = CreateLevelNode(i, levelManager.levels[i], isUnlocked, stars);
                nodePositions.Add(pos);
            }

            // Draw lines between sequential levels
            for (int i = 0; i < nodePositions.Count - 1; i++)
            {
                bool isRevealed = (prog.unlockAllLevels || prog.IsLevelUnlocked(i)) && (prog.unlockAllLevels || prog.IsLevelUnlocked(i + 1));
                CreateLine(nodePositions[i], nodePositions[i+1], isRevealed);
            }
            Debug.Log($"[GrandOrreryManager] Initialized {levelManager.levels.Count} levels.");
        }
        else
        {
            Debug.LogError($"[GrandOrreryManager] Critical Error: Managers not found! levelManager: {levelManager}, prog: {prog}");
        }
    }

    private LevelNodeView GetNode()
    {
        LevelNodeView view;
        if (nodePool.Count > 0)
        {
            view = nodePool.Pop();
        }
        else
        {
            GameObject go = Instantiate(levelNodePrefab, nodesParent);
            view = go.GetComponent<LevelNodeView>();
            if (view == null) view = go.AddComponent<LevelNodeView>();
        }
        view.gameObject.SetActive(true);
        activeNodes.Add(view);
        return view;
    }

    private LineView GetLine()
    {
        LineView view;
        if (linePool.Count > 0)
        {
            view = linePool.Pop();
        }
        else
        {
            GameObject go = Instantiate(linePrefab, nodesParent);
            view = go.GetComponent<LineView>();
            if (view == null) view = go.AddComponent<LineView>();
        }
        view.gameObject.SetActive(true);
        activeLines.Add(view);
        return view;
    }

    Vector2 CreateLevelNode(int index, LevelData data, bool isUnlocked, int stars)
    {
        LevelNodeView view = GetNode();
        view.gameObject.name = $"LevelNode_{index}";
        
        Random.InitState(index + 12345);
        float x = Random.Range(-800f, 800f);
        float y = Random.Range(-600f, 600f);
        Vector2 pos = new Vector2(x, y);
        
        if (view.rectTransform != null)
        {
            view.rectTransform.anchoredPosition = pos;
            view.rectTransform.sizeDelta = new Vector2(80f, 80f); // Slightly larger
            float baseScale = isUnlocked ? 1.0f : 0.7f;
            float starScale = 1.0f + (stars * 0.2f);
            view.rectTransform.localScale = Vector3.one * baseScale * starScale;
            
            // Force local Z to 0 to be safe
            view.transform.localPosition = new Vector3(view.transform.localPosition.x, view.transform.localPosition.y, 0f);
        }

        if (view.button != null)
        {
            view.button.interactable = isUnlocked;
            view.button.onClick.RemoveAllListeners();
            view.button.onClick.AddListener(() => mainMenuManager.StartLevel(index));
        }

        if (view.image != null)
        {
            view.image.color = isUnlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }

        if (view.glowImage != null)
        {
            view.glowImage.color = isUnlocked ? new Color(0.8f, 1.0f, 1.0f, 0.8f) : new Color(0.0f, 1.0f, 1.0f, 0.15f);
        }

        if (view.label != null) 
        {
            view.label.text = isUnlocked ? data.levelName : "???";
            view.label.color = isUnlocked ? Color.white : new Color(1, 1, 1, 0.3f);
        }

        if (view.starIndicators != null)
        {
            for (int i = 0; i < view.starIndicators.Length; i++)
            {
                view.starIndicators[i].SetActive(i < stars);
            }
        }

        return pos;
    }

    void CreateLine(Vector2 p1, Vector2 p2, bool isRevealed)
    {
        if (linePrefab == null) return;
        
        LineView view = GetLine();
        view.transform.SetAsFirstSibling(); // Ensure lines are behind nodes
        
        Vector2 direction = p2 - p1;
        float distance = direction.magnitude;
        
        if (view.rectTransform != null)
        {
            view.rectTransform.anchoredPosition = p1 + direction * 0.5f;
            view.rectTransform.sizeDelta = new Vector2(distance, isRevealed ? 12f : 3f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            view.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        if (view.image != null)
        {
            view.image.color = isRevealed ? new Color(0.6f, 0.8f, 1.0f, 0.5f) : new Color(1, 1, 1, 0.05f);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        mapContainer.anchoredPosition += eventData.delta;
    }

    public void OnScroll(PointerEventData eventData)
    {
        float scale = mapContainer.localScale.x + eventData.scrollDelta.y * zoomSpeed;
        scale = Mathf.Clamp(scale, minZoom, maxZoom);
        mapContainer.localScale = new Vector3(scale, scale, 1f);
    }
}
