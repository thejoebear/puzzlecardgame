using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CelestialSagaMap : MonoBehaviour
{
    [Header("Components")]
    public RectTransform content;
    public TextMeshProUGUI constellationTitle;
    public Button nextButton;
    public Button prevButton;

    [Header("Prefabs")]
    public GameObject levelNodePrefab;
    public GameObject linePrefab;

    [Header("Pagination Settings")]
    public int levelsPerPage = 10;
    public string[] constellationNames = new string[] { "The Silver Veil", "Glow of the Ancients", "The Celestial Forge", "Obsidian Expanse", "Nebula's Whisper" };
    private int currentPage = 0;

    [Header("Layout Settings")]
public float nodeSpacing = 150f;
    public Vector2 centerOffset = new Vector2(0, 0);

    private SolitaireLevelManager levelManager;
    private MainMenuManager mainMenuManager;
    private ProgressionManager cachedProg;

    private List<LevelNodeView> activeNodes = new List<LevelNodeView>();
    private List<LineView> activeLines = new List<LineView>();

    private void Awake()
    {
        levelManager = Object.FindAnyObjectByType<SolitaireLevelManager>();
        mainMenuManager = Object.FindAnyObjectByType<MainMenuManager>();
        cachedProg = ProgressionManager.Instance;

        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (prevButton != null) prevButton.onClick.AddListener(PreviousPage);
    }

    public void InitializeSagaMap()
    {
        if (levelManager == null) levelManager = Object.FindAnyObjectByType<SolitaireLevelManager>();
        if (mainMenuManager == null) mainMenuManager = Object.FindAnyObjectByType<MainMenuManager>();
        
        if (cachedProg == null)
        {
            cachedProg = ProgressionManager.Instance;
            if (cachedProg == null) cachedProg = Object.FindAnyObjectByType<ProgressionManager>();
        }

        // Determine current page based on progress
        int lastUnlocked = 0;
        if (cachedProg != null)
        {
            for (int i = 0; i < levelManager.levels.Count; i++)
            {
                if (cachedProg.IsLevelUnlocked(i)) lastUnlocked = i;
            }
        }
        currentPage = lastUnlocked / levelsPerPage;

        UpdatePage();
    }

    public void NextPage()
    {
        int maxPage = (levelManager.levels.Count - 1) / levelsPerPage;
        if (currentPage < maxPage)
        {
            int bossIndex = (currentPage * levelsPerPage) + 9;
            if (cachedProg != null && (cachedProg.GetStars(bossIndex) > 0 || cachedProg.unlockAllLevels))
            {
                currentPage++;
                UpdatePage();
            }
            else
            {
                Debug.Log("Complete the Boss level to unlock the next constellation!");
            }
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
    }

    private void UpdatePage()
    {
        ClearMap();

        if (constellationTitle != null)
        {
            string cName = (currentPage < constellationNames.Length) ? constellationNames[currentPage] : $"Constellation {currentPage + 1}";
            constellationTitle.text = cName;
        }

        int startIdx = currentPage * levelsPerPage;
        int endIdx = Mathf.Min(startIdx + levelsPerPage, levelManager.levels.Count);

        List<Vector2> positions = new List<Vector2>();

        for (int i = startIdx; i < endIdx; i++)
        {
            int localIdx = i - startIdx;
            // Distribute 9 levels in a circle, 10th (Boss) in the center
            float angle = localIdx * (2f * Mathf.PI / (levelsPerPage - 1)) + (Mathf.PI / 2f);
            float radius = (localIdx == 9) ? 0 : nodeSpacing * (1 + (localIdx * 0.05f));
            
            Vector2 pos = new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );

            if (localIdx == 9) pos = Vector2.zero; 

            positions.Add(pos);
            CreateNode(i, pos, localIdx == 9);
        }

        // Connect sequence
        for (int i = 0; i < positions.Count - 1; i++)
        {
            if (i < 8)
                CreateLine(positions[i], positions[i+1], startIdx + i, startIdx + i + 1);
            else if (i == 8 && positions.Count > 9)
                CreateLine(positions[8], positions[9], startIdx + 8, startIdx + 9);
        }

        if (nextButton != null)
        {
            int bossIndex = (currentPage * levelsPerPage) + 9;
            bool unlocked = (cachedProg != null && (cachedProg.GetStars(bossIndex) > 0 || cachedProg.unlockAllLevels));
            nextButton.interactable = unlocked && (currentPage < (levelManager.levels.Count - 1) / levelsPerPage);
        }
        if (prevButton != null) prevButton.interactable = currentPage > 0;
    }

    private void CreateNode(int index, Vector2 anchoredPos, bool isBoss)
    {
        GameObject go = Instantiate(levelNodePrefab, content);
        LevelNodeView view = go.GetComponent<LevelNodeView>();
        if (view == null) view = go.AddComponent<LevelNodeView>();

        if (view.rectTransform == null) view.rectTransform = go.GetComponent<RectTransform>();
        if (view.button == null) view.button = go.GetComponent<Button>();
        if (view.image == null) view.image = go.GetComponent<Image>();

        view.rectTransform.anchoredPosition = anchoredPos + centerOffset;

        bool isUnlocked = cachedProg != null && cachedProg.IsLevelUnlocked(index);
        int stars = cachedProg != null ? cachedProg.GetStars(index) : 0;
        LevelData data = levelManager.levels[index];

        bool isCurrent = isUnlocked && stars == 0;

        view.SetState(isUnlocked, isCurrent, stars, data.levelName, false, isBoss);

        if (view.button != null)
        {
            view.button.onClick.RemoveAllListeners();
            view.button.onClick.AddListener(() => mainMenuManager.StartLevel(index));
        }

        activeNodes.Add(view);
    }

    private void CreateLine(Vector2 p1, Vector2 p2, int fromIndex, int toIndex)
    {
        GameObject lineGo = Instantiate(linePrefab, content);
        LineView lineView = lineGo.GetComponent<LineView>();
        if (lineView == null) lineView = lineGo.AddComponent<LineView>();

        if (lineView.rectTransform == null) lineView.rectTransform = lineGo.GetComponent<RectTransform>();
        if (lineView.image == null) lineView.image = lineGo.GetComponent<Image>();

        lineView.transform.SetAsFirstSibling();

        Vector2 dir = p2 - p1;
        float dist = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        lineView.rectTransform.anchoredPosition = (p1 + p2) * 0.5f + centerOffset;
        lineView.rectTransform.sizeDelta = new Vector2(dist, 10f);
        lineView.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);

        if (lineView.image != null)
        {
            bool isRevealed = cachedProg != null && cachedProg.IsLevelUnlocked(fromIndex) && cachedProg.IsLevelUnlocked(toIndex);
            lineView.image.color = isRevealed ? new Color(1, 1, 1, 0.6f) : new Color(1, 1, 1, 0.1f);
        }

        activeLines.Add(lineView);
    }

    private void ClearMap()
    {
        if (content != null)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                GameObject child = content.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }
        activeNodes.Clear();
        activeLines.Clear();
    }
}