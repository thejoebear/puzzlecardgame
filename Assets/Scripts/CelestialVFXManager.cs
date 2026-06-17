using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CelestialVFXManager : MonoBehaviour
{
    public static CelestialVFXManager Instance;

    public ParticleSystem starDustPrefab;
    public Sprite starDustSprite;
    
    [Header("Ability Effects")]
    public ParticleSystem gravityVortexPrefab;
    public GameObject nebulaOverlay;
    public ParticleSystem focusBeamPrefab;

    [Header("Hint Visualization")]
    public LineRenderer hintPathRenderer;
    public ParticleSystem hintSparklePrefab;

    [Header("Background Settings")]
    public ParticleSystem backgroundStarSystem;
    public Transform backgroundContainer;
    public int starsPerConstellation = 5;
    private List<Vector3> constellationPoints = new List<Vector3>();

    private Dictionary<ParticleSystem, List<ParticleSystem>> pools = new Dictionary<ParticleSystem, List<ParticleSystem>>();

    void Awake()
    {
        Instance = this;
        // Generate some random points in background space if not assigned
        for (int i = 0; i < 50; i++)
        {
            constellationPoints.Add(new Vector3(Random.Range(-800f, 800f), Random.Range(-500f, 500f), 0f));
        }

        if (hintPathRenderer != null)
        {
            hintPathRenderer.enabled = false;
            hintPathRenderer.sortingOrder = 100; // Above UI
            hintPathRenderer.startWidth = 0.2f;
            hintPathRenderer.endWidth = 0.05f;
        }
        if (nebulaOverlay != null) nebulaOverlay.SetActive(false);

        InitializePools();
    }

    private void InitializePools()
    {
        if (starDustPrefab != null) pools[starDustPrefab] = new List<ParticleSystem>();
        if (gravityVortexPrefab != null) pools[gravityVortexPrefab] = new List<ParticleSystem>();
        if (focusBeamPrefab != null) pools[focusBeamPrefab] = new List<ParticleSystem>();
        if (hintSparklePrefab != null) pools[hintSparklePrefab] = new List<ParticleSystem>();
    }

    private ParticleSystem GetFromPool(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null) return null;
        if (!pools.ContainsKey(prefab)) pools[prefab] = new List<ParticleSystem>();
        
        List<ParticleSystem> pool = pools[prefab];
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null && !pool[i].gameObject.activeInHierarchy)
            {
                pool[i].transform.position = position;
                pool[i].gameObject.SetActive(true);
                return pool[i];
            }
        }

        ParticleSystem newInstance = Instantiate(prefab, position, Quaternion.identity);
        pool.Add(newInstance);
        return newInstance;
    }

    private IEnumerator ReturnToPoolAfter(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps != null) ps.gameObject.SetActive(false);
    }

    void Start()
    {
        RefreshBackgroundStars();
    }

    public void RefreshBackgroundStars()
    {
        if (backgroundContainer == null || ProgressionManager.Instance == null) return;

        // Clear existing legacy stars
        foreach (Transform child in backgroundContainer)
        {
            if (child.name == "Star") Destroy(child.gameObject);
        }

        if (backgroundStarSystem == null) return;
        backgroundStarSystem.Clear();

        // Calculate total stars earned
        int totalStars = 0;
        for (int i = 0; i < 100; i++)
        {
            totalStars += ProgressionManager.Instance.GetStars(i);
        }

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        for (int i = 0; i < totalStars; i++)
        {
            if (i < constellationPoints.Count)
            {
                emitParams.position = constellationPoints[i];
                emitParams.startColor = new Color(1, 0.9f, 0.5f, 0.8f);
                emitParams.startSize = Random.Range(3f, 6f);
                backgroundStarSystem.Emit(emitParams, 1);
            }
        }
    }

    public void ShowStarlightPath(Vector3 start, Vector3 end)
    {
        if (hintPathRenderer == null) return;
        StartCoroutine(StarlightPathRoutine(start, end));
    }

    private IEnumerator StarlightPathRoutine(Vector3 start, Vector3 end)
    {
        hintPathRenderer.enabled = true;
        hintPathRenderer.positionCount = 2;
        hintPathRenderer.SetPosition(0, start);
        hintPathRenderer.SetPosition(1, start);

        float elapsed = 0;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            hintPathRenderer.SetPosition(1, Vector3.Lerp(start, end, elapsed / duration));
            yield return null;
        }

        ParticleSystem ps = GetFromPool(hintSparklePrefab, end);
        if (ps != null)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null) renderer.sortingOrder = 101;
            ps.Play();
            StartCoroutine(ReturnToPoolAfter(ps, 2f));
        }

        yield return new WaitForSeconds(1f);
        hintPathRenderer.enabled = false;
    }

    public void PlayGravityEffect(Vector3 center)
    {
        ParticleSystem ps = GetFromPool(gravityVortexPrefab, center);
        if (ps != null)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null) renderer.sortingOrder = 101;
            ps.Play();
            StartCoroutine(ReturnToPoolAfter(ps, 3f));
        }
    }

    public void PlayNebulaEffect(bool active)
    {
        if (nebulaOverlay != null)
        {
            nebulaOverlay.SetActive(active);
        }
    }

    public void PlayFocusEffect(Vector3 position)
    {
        ParticleSystem ps = GetFromPool(focusBeamPrefab, position);
        if (ps != null)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null) renderer.sortingOrder = 101;
            ps.Play();
            StartCoroutine(ReturnToPoolAfter(ps, 2f));
        }
    }

    [Header("Win Sequence")]
    public CanvasGroup hudCanvasGroup;
    public GameObject constellationLinePrefab;
    public float winSequenceDelay = 0.5f;

    public void FadeHUD(float targetAlpha, float duration)
    {
        if (hudCanvasGroup != null) StartCoroutine(FadeHUDRoutine(targetAlpha, duration));
    }

    private IEnumerator FadeHUDRoutine(float target, float duration)
    {
        float start = hudCanvasGroup.alpha;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            hudCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        hudCanvasGroup.alpha = target;
        hudCanvasGroup.interactable = target > 0.5f;
        hudCanvasGroup.blocksRaycasts = target > 0.5f;
    }

    public void PlayWinConstellation(List<Transform> cards, List<Vector2> shape)
    {
        Debug.Log($"[WinSequence] Starting constellation with {cards.Count} cards and {shape.Count} points.");
        if (shape == null || shape.Count == 0) 
        {
            Debug.LogWarning("[WinSequence] Shape is null or empty!");
            return;
        }
        
        // Fade HUD out first
        FadeHUD(0, 0.5f);
        
        StartCoroutine(WinConstellationRoutine(cards, shape));
    }

    private IEnumerator WinConstellationRoutine(List<Transform> cards, List<Vector2> shape)
    {
        // Optional: Darken the board background slightly to pop the stars
        var board = GameObject.Find("Board")?.GetComponent<UnityEngine.UI.Image>();
        Color originalBoardColor = Color.white;
        if (board != null)
        {
            originalBoardColor = board.color;
            StartCoroutine(FadeBoardColor(board, new Color(0.1f, 0.1f, 0.2f, 1f), 1.0f));
        }

        // 1. Gather target points
        List<Vector3> worldPoints = new List<Vector3>();
        for (int i = 0; i < shape.Count; i++)
        {
            worldPoints.Add(new Vector3(shape[i].x, shape[i].y, 0));
        }

        // 2. Fly cards to points
        int count = Mathf.Min(cards.Count, shape.Count);
        for (int i = 0; i < count; i++)
        {
            Transform card = cards[i];
            Vector3 target = worldPoints[i];

            // Reparent to background or this manager to avoid fading with HUD
            // And ensure they are in front of the background but behind UI
            card.SetParent(backgroundContainer != null ? backgroundContainer : transform, true);

            ParticleSystem ps = GetFromPool(starDustPrefab, card.position);
            if (ps != null) StartCoroutine(MoveParticleSystem(ps, target));

            CardMotion motion = card.GetComponent<CardMotion>();
            if (motion != null)
            {
                motion.MoveToWorld(target, 0.8f, () => {
                    card.gameObject.SetActive(false);
                    EmitSingleBackgroundStar(target);
                }, null, Vector3.zero);
            }
            else
            {
                card.gameObject.SetActive(false);
                EmitSingleBackgroundStar(target);
            }

            yield return new WaitForSeconds(0.1f);
        }

        // Handle remaining cards if any
        if (cards.Count > count)
        {
            for (int i = count; i < cards.Count; i++)
            {
                Transform card = cards[i];
                Vector3 randomTarget = constellationPoints[Random.Range(0, constellationPoints.Count)];
                CardMotion motion = card.GetComponent<CardMotion>();
                if (motion != null)
                {
                    motion.MoveToWorld(randomTarget, 0.6f, () => card.gameObject.SetActive(false), null, Vector3.zero);
                }
                else card.gameObject.SetActive(false);
            }
        }

        yield return new WaitForSeconds(0.5f);

        // 3. Connect dots
        if (constellationLinePrefab != null)
        {
            for (int i = 0; i < worldPoints.Count - 1; i++)
            {
                GameObject lineObj = Instantiate(constellationLinePrefab, backgroundContainer);
                lineObj.name = "WinConstellationLine";
                LineRenderer lr = lineObj.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.sortingOrder = 50; // Above board, below main UI
                    lr.positionCount = 2;
lr.SetPosition(0, worldPoints[i]);
                    lr.SetPosition(1, worldPoints[i]);
                    StartCoroutine(AnimateLine(lr, worldPoints[i], worldPoints[i + 1]));
                }
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    public void ClearWinConstellation()
    {
        if (backgroundContainer == null) return;
        foreach (Transform child in backgroundContainer)
        {
            if (child.name == "WinConstellationLine") Destroy(child.gameObject);
        }
    }

    private IEnumerator FadeBoardColor(UnityEngine.UI.Image img, Color target, float duration)
    {
        Color start = img.color;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            img.color = Color.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        img.color = target;
    }

    private IEnumerator AnimateLine(LineRenderer lr, Vector3 start, Vector3 end)
{
        float elapsed = 0;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            lr.SetPosition(1, Vector3.Lerp(start, end, elapsed / duration));
            yield return null;
        }
    }

    public void PlayCleanupEffect(List<Transform> cards, Vector3 targetPoint)
{
        Vector3 backgroundTarget = constellationPoints[Random.Range(0, constellationPoints.Count)];
        StartCoroutine(CleanupRoutine(cards, backgroundTarget));
    }

    private IEnumerator CleanupRoutine(List<Transform> cards, Vector3 backgroundTarget)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            Transform card = cards[i];
            if (card == null) continue;

            ParticleSystem ps = GetFromPool(starDustPrefab, card.position);
            if (ps != null)
            {
                StartCoroutine(MoveParticleSystem(ps, backgroundTarget));
            }

            CardMotion motion = card.GetComponent<CardMotion>();
            if (motion != null)
            {
                motion.MoveToWorld(backgroundTarget, 0.6f, () => {
                    card.gameObject.SetActive(false);
                    EmitSingleBackgroundStar(backgroundTarget);
                }, null, Vector3.zero);
            }
            else
            {
                card.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(0.05f);
        }
    }

    private void EmitSingleBackgroundStar(Vector3 position)
    {
        if (backgroundStarSystem == null) return;
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.position = position;
        emitParams.startColor = new Color(1, 0.9f, 0.5f, 0.8f);
        emitParams.startSize = Random.Range(3f, 6f);
        backgroundStarSystem.Emit(emitParams, 1);
    }

    private IEnumerator MoveParticleSystem(ParticleSystem ps, Vector3 target)
    {
        float elapsed = 0;
        float duration = 0.6f;
        Vector3 startPos = ps.transform.position;
        ps.Play();

        while (elapsed < duration)
        {
            if (ps == null || !ps.gameObject.activeInHierarchy) yield break;
            elapsed += Time.deltaTime;
            ps.transform.position = Vector3.Lerp(startPos, target, elapsed / duration);
            yield return null;
        }
        
        if (ps != null) ps.Stop();
        StartCoroutine(ReturnToPoolAfter(ps, 2f));
    }
}
