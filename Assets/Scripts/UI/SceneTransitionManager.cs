using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Resources")]
    [SerializeField] private string overlayPrefabPath = "Prefabs/UI/SceneTransitionOverlay";

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.25f;
    [SerializeField, Min(0f)] private float blackHoldSeconds = 0.08f;
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.25f;
    [SerializeField] private bool fadeInOnFirstScene = false;

    [Header("Canvas")]
    [SerializeField] private int sortingOrder = 32767;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Header("Scene Cleanup")]
    [SerializeField] private string menuSceneName = "MenuScene";
    [SerializeField] private string menuShellObjectName = "MenuShell";
    [SerializeField] private string menuRulePanelObjectName = "RulePanel";

    private Canvas transitionCanvas;
    private CanvasGroup overlayGroup;
    private RectTransform overlayRect;
    private Coroutine transitionRoutine;
    private Coroutine initialFadeRoutine;
    private bool initialFadePlayed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        SceneTransitionManager manager = EnsureInstance();
        if (manager == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        manager.BeginTransition(sceneName);
    }

    public static void ReloadActiveScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public static bool IsTransitionCanvas(Canvas canvas)
    {
        if (canvas == null)
        {
            return false;
        }

        return canvas.name == "SceneTransitionCanvas" || canvas.GetComponentInParent<SceneTransitionManager>() != null;
    }

    private static SceneTransitionManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        SceneTransitionManager existingManager = FindObjectOfType<SceneTransitionManager>();
        if (existingManager != null)
        {
            Instance = existingManager;
            return Instance;
        }

        GameObject managerObject = new GameObject("SceneTransitionManager");
        DontDestroyOnLoad(managerObject);
        Instance = managerObject.AddComponent<SceneTransitionManager>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!fadeInOnFirstScene || initialFadePlayed)
        {
            return;
        }

        initialFadePlayed = true;
        EnsureOverlay();
        if (overlayGroup != null)
        {
            overlayGroup.alpha = 1f;
            overlayGroup.blocksRaycasts = true;
            initialFadeRoutine = StartCoroutine(FadeInOnly());
        }
    }

    private void BeginTransition(string sceneName)
    {
        EnsureOverlay();
        AudioManager.Instance.PlaySfx(AudioIds.Transition);

        if (initialFadeRoutine != null)
        {
            StopCoroutine(initialFadeRoutine);
            initialFadeRoutine = null;
        }

        if (transitionRoutine != null)
        {
            return;
        }

        transitionRoutine = StartCoroutine(TransitionToScene(sceneName));
    }

    private IEnumerator TransitionToScene(string sceneName)
    {
        EnsureOverlay();
        SetOverlayBlocking(true);

        yield return FadeOverlayTo(1f, fadeOutSeconds);

        if (blackHoldSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(blackHoldSeconds);
        }

        CleanupMenuUiForTarget(sceneName);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        if (loadOperation == null)
        {
            Debug.LogError($"[SceneTransition] Failed to load scene '{sceneName}'.");
            yield return FadeOverlayTo(0f, fadeInSeconds);
            SetOverlayBlocking(false);
            transitionRoutine = null;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return null;
        EnsureOverlay();
        CleanupMenuUiForTarget(sceneName);

        if (blackHoldSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(blackHoldSeconds);
        }

        yield return FadeOverlayTo(0f, fadeInSeconds);
        SetOverlayBlocking(false);
        transitionRoutine = null;
    }

    private IEnumerator FadeInOnly()
    {
        SetOverlayBlocking(true);
        yield return null;
        yield return FadeOverlayTo(0f, fadeInSeconds);
        SetOverlayBlocking(false);
        initialFadeRoutine = null;
    }

    private IEnumerator FadeOverlayTo(float targetAlpha, float duration)
    {
        EnsureOverlay();
        if (overlayGroup == null)
        {
            yield break;
        }

        float startAlpha = overlayGroup.alpha;
        if (duration <= 0f)
        {
            overlayGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            overlayGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, SmoothStep(t));
            yield return null;
        }

        overlayGroup.alpha = targetAlpha;
    }

    private void EnsureOverlay()
    {
        EnsureCanvas();

        if (overlayRect == null && transitionCanvas != null)
        {
            GameObject prefab = Resources.Load<GameObject>(overlayPrefabPath);
            GameObject overlayObject = prefab != null
                ? Instantiate(prefab, transitionCanvas.transform, false)
                : CreateFallbackOverlay(transitionCanvas.transform);

            overlayObject.name = "SceneTransitionOverlay";
            overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayGroup = overlayObject.GetComponent<CanvasGroup>();
            if (overlayGroup == null)
            {
                overlayGroup = overlayObject.AddComponent<CanvasGroup>();
            }

            Image image = overlayObject.GetComponent<Image>();
            if (image != null)
            {
                image.color = Color.black;
                image.raycastTarget = true;
            }
        }

        if (overlayRect != null)
        {
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.sizeDelta = Vector2.zero;
            overlayRect.localScale = Vector3.one;
            overlayRect.SetAsLastSibling();
        }

        if (overlayGroup != null)
        {
            overlayGroup.alpha = Mathf.Clamp01(overlayGroup.alpha);
            overlayGroup.blocksRaycasts = overlayGroup.alpha > 0.001f;
            overlayGroup.interactable = overlayGroup.blocksRaycasts;
        }
    }

    private void EnsureCanvas()
    {
        if (transitionCanvas != null)
        {
            transitionCanvas.sortingOrder = sortingOrder;
            return;
        }

        GameObject canvasObject = new GameObject("SceneTransitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        transitionCanvas = canvasObject.GetComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;
    }

    private GameObject CreateFallbackOverlay(Transform parent)
    {
        GameObject overlayObject = new GameObject("SceneTransitionOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        overlayObject.layer = parent.gameObject.layer;
        overlayObject.transform.SetParent(parent, false);
        return overlayObject;
    }

    private void CleanupMenuUiForTarget(string targetSceneName)
    {
        if (string.Equals(targetSceneName, menuSceneName, System.StringComparison.Ordinal))
        {
            return;
        }

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = transforms.Length - 1; i >= 0; i--)
        {
            Transform candidate = transforms[i];
            if (candidate == null)
            {
                continue;
            }

            GameObject candidateObject = candidate.gameObject;
            if (candidateObject == null || !candidateObject.scene.IsValid())
            {
                continue;
            }

            if (candidateObject.name != menuShellObjectName && candidateObject.name != menuRulePanelObjectName)
            {
                continue;
            }

            Destroy(candidateObject);
        }
    }

    private void SetOverlayBlocking(bool blocking)
    {
        EnsureOverlay();
        if (overlayGroup == null)
        {
            return;
        }

        overlayGroup.blocksRaycasts = blocking;
        overlayGroup.interactable = blocking;
    }

    private static float SmoothStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
