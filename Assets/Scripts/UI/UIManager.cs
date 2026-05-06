using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Refs")]
    public GameManager gameManager;
    public Text turnText;
    public Text diceText;
    public Text statusText;
    public Button BuyButton;
    public Button MoveButton;
    public Button SkipButton;
    public RectTransform topPanel;
    public RectTransform actionPanel;
    public RectTransform playerPanel;
    public RectTransform playerHudRoot;
    [FormerlySerializedAs("playerInfoItemPrefab")]
    public PlayerInfoItem playerCardPrefab;
    public List<PlayerInfoItem> playerInfoItems = new List<PlayerInfoItem>();
    public Text logText;
    public RectTransform logPanel;
    public RectTransform handPanel;
    public RectTransform handBodyRoot;
    public Text handTitleText;
    public List<Button> handCardButtons = new List<Button>();
    public RectTransform propertyInfoPanel;
    public RectTransform propertyInfoImageMask;
    public Image propertyInfoImage;
    public Text propertyInfoTitleText;
    public Text propertyInfoBodyText;
    public RectTransform questionOverlay;
    public Text questionTitleText;
    public Text questionBodyText;
    public List<Button> questionOptionButtons = new List<Button>();
    public RectTransform noticeOverlay;
    public Text noticeTitleText;
    public Text noticeBodyText;
    public Button noticeConfirmButton;
    public RectTransform settingsOverlay;

    [Header("Player Card Layout")]
    [FormerlySerializedAs("playerHudItemSpacing")]
    public float playerCardSpacing = 18f;
    public float playerCardScale = 1f;
    public Vector2 playerCardOffset = Vector2.zero;

    [Header("Runtime")]
    public int maxLogLines = 16;
    public bool hookUnityLogs = true;

    private bool runtimeReady;
    private bool subscribedToLogs;
    private bool callbacksBound;
    private readonly List<PlayerInfoItem> playerItems = new List<PlayerInfoItem>();
    private readonly List<Button> questionButtons = new List<Button>();
    private readonly List<string> logLines = new List<string>();
    private readonly List<Text> handCardTexts = new List<Text>();
    private readonly List<Image> handCardImages = new List<Image>();
    private readonly List<Button> handCardUseButtons = new List<Button>();
    private readonly List<Image> handCardSelectionFrames = new List<Image>();
    private readonly List<HandCardView> handCardViews = new List<HandCardView>();
    private readonly Dictionary<string, Sprite> cardSpritesByName = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, Sprite> propertySpritesByPath = new Dictionary<string, Sprite>();
    private readonly Dictionary<RoleId, Sprite> roleAvatarSprites = new Dictionary<RoleId, Sprite>();
    private readonly Dictionary<int, int> moneyDisplayOverrides = new Dictionary<int, int>();
    private readonly Dictionary<int, int> moneyPendingTargets = new Dictionary<int, int>();
    private readonly Dictionary<int, Queue<MoneyChangeFxRequest>> moneyFxQueues = new Dictionary<int, Queue<MoneyChangeFxRequest>>();
    private readonly Dictionary<int, Coroutine> moneyFxCoroutines = new Dictionary<int, Coroutine>();
    private readonly Dictionary<Text, RectTransform> moneyFxRootLookup = new Dictionary<Text, RectTransform>();
    private Action<int> questionCallback;
    private Action noticeCallback;
    private string handSignature = string.Empty;
    private int selectedHandCardIndex = -1;
    private bool debugLogPanelVisible;
    private bool propertyInfoVisible;
    private Canvas rootCanvas;
    private RectTransform rootCanvasRect;
    private Font feedbackFont;
    private GameObject moneyFxPrefab;
    private bool cardSpritesLoaded;
    private Image noticeCardImage;
    private HandPanelLayout handPanelLayout;
    private RectTransform handCardDialogOverlay;
    private Text handCardDialogTitleText;
    private Text handCardDialogBodyText;
    private Image handCardDialogImage;
    private Button handCardDialogUseButton;
    private Button handCardDialogCancelButton;
    private int pendingHandCardIndex = -1;
    private Dropdown settingsResolutionDropdown;
    private Toggle settingsFullscreenToggle;
    private Button settingsMenuButton;
    private Button settingsCloseButton;
    private int selectedResolutionIndex;
    private bool selectedFullscreen;
    private bool suppressSettingsControlEvents;
    private bool noticeBodyLayoutCaptured;
    private Vector2 noticeBodyAnchorMin;
    private Vector2 noticeBodyAnchorMax;
    private Vector2 noticeBodyAnchoredPosition;
    private Vector2 noticeBodySizeDelta;
    private Vector2 noticeBodyPivot;
    private bool propertyInfoPanelVisibleLastFrame;

    private static readonly Vector2 DialogPanelSize = new Vector2(880f, 620f);
    private static readonly Vector2 QuestionOptionButtonSize = new Vector2(232f, 48f);
    private static readonly Vector2 NoticeButtonSize = new Vector2(180f, 52f);
    private static readonly Vector2 HandDialogButtonSize = new Vector2(168f, 52f);
    private static readonly Vector2 PlayerHudItemFallbackSize = new Vector2(220f, 100f);
    private static readonly Vector2 ActionButtonSize = new Vector2(228f, 56f);
    private static readonly Vector2 PropertyInfoPanelSize = new Vector2(284f, 304f);
    private static readonly Vector2 PropertyInfoPanelOffset = new Vector2(-36f, 96f);
    private static readonly Vector2 PropertyInfoImageMaskSize = new Vector2(252f, 156f);
    private static readonly Vector2 PropertyInfoTextSize = new Vector2(252f, 34f);
    private static readonly Vector2 PropertyInfoBodySize = new Vector2(252f, 64f);
    private static readonly Vector2 SettingsPanelSize = new Vector2(640f, 440f);
    private static readonly Vector2 SettingsWideButtonSize = new Vector2(220f, 52f);
    private static readonly Vector2Int[] SettingsResolutionOptions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440)
    };
    private const string PlayerCardPrefabPath = "Prefabs/UI/PlayerCard";
    private const string PropertyInfoPanelPrefabPath = "Prefabs/UI/PropertyInfoPanel";
    private const string PropertyImageRootPath = "PropertyImages";
    private const string MenuSceneName = "MenuScene";
    private const string ResolutionWidthPrefsKey = "display_width";
    private const string ResolutionHeightPrefsKey = "display_height";
    private const string FullscreenPrefsKey = "display_fullscreen";
    private const float PropertyInfoPanelMargin = 16f;
    private const float ActionPanelWidth = 228f;
    private const float ActionPanelVerticalPadding = 0f;
    private const float ActionButtonSpacing = 8f;

    private sealed class MoneyChangeFxRequest
    {
        public int fromMoney;
        public int toMoney;
        public int delta;
        public Vector3 worldStart;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        EnsureRuntimeUi();
    }

    private void Update()
    {
        EnsureRuntimeUi();
        HandleDebugLogToggle();
        HandleSettingsShortcut();

        if (!runtimeReady || gameManager == null)
        {
            return;
        }

#if UNITY_EDITOR
        HandleEditorHandCardShortcut();
        HandleEditorQuestionShortcut();
        HandleEditorMoneyShortcut();
#endif
        UpdateTopInfo();
        UpdatePropertyInfoPanel();
        UpdateButtons();
        UpdatePlayerHud();
        UpdateHandPanel();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (subscribedToLogs)
        {
            Application.logMessageReceived -= OnUnityLog;
            subscribedToLogs = false;
        }
    }

    public void Log(string message)
    {
        AddLogLine(message);
    }

    public void ShowQuestion(QuestionData question, Action<int> onAnswer)
    {
        EnsureRuntimeUi();
        if (question == null || questionOverlay == null)
        {
            return;
        }

        questionCallback = onAnswer;

        if (questionTitleText != null)
        {
            questionTitleText.text = string.IsNullOrEmpty(question.category)
                ? "\u5609\u5174\u6587\u5316\u95ee\u7b54"
                : $"\u5609\u5174\u6587\u5316\u95ee\u7b54 \u00b7 {question.category}";
        }

        if (questionBodyText != null)
        {
            questionBodyText.text = question.text ?? string.Empty;
        }

        for (int i = 0; i < questionButtons.Count; i++)
        {
            Button button = questionButtons[i];
            if (button == null)
            {
                continue;
            }

            bool active = question.options != null && i < question.options.Length;
            button.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            Text buttonText = button.GetComponentInChildren<Text>(true);
            if (buttonText != null)
            {
                char optionName = (char)('A' + i);
                buttonText.text = $"{optionName}. {question.options[i]}";
            }
        }

        questionOverlay.gameObject.SetActive(true);
        PlayPopupOpen(questionOverlay, "QuestionCard");
    }

    public void HideQuestion()
    {
        if (questionOverlay != null)
        {
            questionOverlay.gameObject.SetActive(false);
        }

        questionCallback = null;
    }

    public void ShowNotice(string title, string body, string buttonText = "\u786e\u5b9a", Action onConfirm = null)
    {
        EnsureRuntimeUi();
        if (noticeOverlay == null)
        {
            return;
        }

        noticeCallback = onConfirm;

        if (noticeTitleText != null)
        {
            noticeTitleText.text = string.IsNullOrEmpty(title) ? "\u63d0\u793a" : title;
        }

        if (noticeBodyText != null)
        {
            noticeBodyText.text = body ?? string.Empty;
        }

        if (noticeConfirmButton != null)
        {
            Text text = noticeConfirmButton.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = buttonText;
            }
        }

        SetNoticeCardVisual(null);
        noticeOverlay.gameObject.SetActive(true);
        PlayPopupOpen(noticeOverlay, "NoticeCard");
    }

    public void ShowCardNotice(string title, CardData card, string buttonText = "\u7ee7\u7eed", Action onConfirm = null)
    {
        string cardTitle = string.IsNullOrEmpty(title) ? "\u62bd\u5230\u5361\u724c" : title;
        string body = card == null
            ? string.Empty
            : $"<b>{card.cardName}</b>\n\n{card.description}";

        ShowNotice(cardTitle, body, buttonText, onConfirm);
        SetNoticeCardVisual(GetCardSprite(card));
    }

    public void QueueMoneyChange(int playerIndex, int oldMoney, int newMoney, int delta, Vector3 worldStart)
    {
        EnsureRuntimeUi();
        if (delta == 0 || playerIndex < 0)
        {
            return;
        }

        if (!moneyFxQueues.TryGetValue(playerIndex, out Queue<MoneyChangeFxRequest> queue))
        {
            queue = new Queue<MoneyChangeFxRequest>();
            moneyFxQueues[playerIndex] = queue;
        }

        int fromMoney = oldMoney;
        if (moneyPendingTargets.TryGetValue(playerIndex, out int pendingTarget))
        {
            fromMoney = pendingTarget;
        }
        else if (moneyDisplayOverrides.TryGetValue(playerIndex, out int visibleMoney))
        {
            fromMoney = visibleMoney;
        }

        queue.Enqueue(new MoneyChangeFxRequest
        {
            fromMoney = fromMoney,
            toMoney = newMoney,
            delta = delta,
            worldStart = worldStart
        });

        moneyPendingTargets[playerIndex] = newMoney;

        if (!moneyFxCoroutines.ContainsKey(playerIndex))
        {
            moneyFxCoroutines[playerIndex] = StartCoroutine(ProcessMoneyChangeQueue(playerIndex));
        }
    }

    public void HideNotice()
    {
        SetNoticeCardVisual(null);
        if (noticeOverlay != null)
        {
            noticeOverlay.gameObject.SetActive(false);
        }

        noticeCallback = null;
    }

    private void EnsureRuntimeUi()
    {
        if (runtimeReady)
        {
            return;
        }

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        CacheCanvasRefs();
        EnsureHandPanelLayout();
        CacheSceneCollections();
        EnsurePropertyInfoPanel();
        ConfigureActionPanelVisuals();
        EnsureSettingsUi();
        ConfigureSceneDialogLayouts();
        BindSceneDialogs();
        BindRuntimeCallbacks();
        InitPlayerHud();
        EnsureHandCardDialog();

        if (hookUnityLogs && !subscribedToLogs)
        {
            Application.logMessageReceived += OnUnityLog;
            subscribedToLogs = true;
        }

        if (questionOverlay != null)
        {
            questionOverlay.gameObject.SetActive(false);
        }

        if (noticeOverlay != null)
        {
            noticeOverlay.gameObject.SetActive(false);
        }

        if (handCardDialogOverlay != null)
        {
            handCardDialogOverlay.gameObject.SetActive(false);
        }

        if (settingsOverlay != null)
        {
            settingsOverlay.gameObject.SetActive(false);
        }

        SetDebugLogPanelVisible(false);

        runtimeReady = true;
    }

    private void CacheCanvasRefs()
    {
        if (rootCanvas != null && rootCanvasRect != null && feedbackFont != null && moneyFxPrefab != null)
        {
            return;
        }

        Graphic referenceGraphic = turnText != null
            ? turnText
            : (questionTitleText != null ? questionTitleText : (noticeTitleText != null ? noticeTitleText : handTitleText));

        if (referenceGraphic != null)
        {
            rootCanvas = referenceGraphic.canvas != null ? referenceGraphic.canvas.rootCanvas : null;
        }

        if (rootCanvas == null)
        {
            rootCanvas = FindSceneCanvas();
        }

        if (rootCanvas != null)
        {
            rootCanvasRect = rootCanvas.GetComponent<RectTransform>();
        }

        if (feedbackFont == null)
        {
            feedbackFont = handTitleText != null && handTitleText.font != null
                ? handTitleText.font
                : (questionTitleText != null && questionTitleText.font != null
                    ? questionTitleText.font
                    : (noticeTitleText != null && noticeTitleText.font != null
                        ? noticeTitleText.font
                        : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")));
        }

        if (moneyFxPrefab == null)
        {
            moneyFxPrefab = Resources.Load<GameObject>("Prefabs/UI/MoneyFxText");
        }
    }

    private Canvas FindSceneCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || SceneTransitionManager.IsTransitionCanvas(canvas))
            {
                continue;
            }

            if (canvas.gameObject.scene == gameObject.scene)
            {
                return canvas;
            }
        }

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && !SceneTransitionManager.IsTransitionCanvas(canvas))
            {
                return canvas;
            }
        }

        return null;
    }

    private void EnsureHandCardDialog()
    {
        if (handCardDialogOverlay != null || rootCanvasRect == null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("HandCardUseDialog", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.layer = rootCanvasRect.gameObject.layer;
        overlayObject.transform.SetParent(rootCanvasRect, false);
        handCardDialogOverlay = overlayObject.GetComponent<RectTransform>();
        SetDialogStretch(handCardDialogOverlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.45f);
        overlayImage.raycastTarget = true;

        RectTransform panelRect = CreateDialogRect("Panel", handCardDialogOverlay);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = DialogPanelSize;

        Image panelImage = panelRect.gameObject.AddComponent<Image>();
        Sprite panelSprite = GetQuestionPanelSprite();
        ConfigureDialogImage(panelImage, new Color(0.98f, 0.91f, 0.76f, 1f), panelSprite);

        if (panelSprite == null)
        {
            Outline panelOutline = panelRect.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.25f, 0.15f, 0.08f, 0.65f);
            panelOutline.effectDistance = new Vector2(2f, -2f);
        }

        handCardDialogTitleText = CreateDialogText("Title", panelRect, string.Empty, 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.31f, 0.17f, 0.09f, 1f));
        SetCenteredRect(handCardDialogTitleText.rectTransform, new Vector2(0f, 214f), new Vector2(360f, 48f));

        RectTransform previewBackRect = CreateDialogRect("CardPreviewBack", panelRect);
        SetCenteredRect(previewBackRect, new Vector2(-125f, 5f), new Vector2(194f, 194f));

        Image previewBackImage = previewBackRect.gameObject.AddComponent<Image>();
        ConfigureDialogImage(previewBackImage, new Color(0.16f, 0.22f, 0.13f, 1f));

        GameObject imageObject = new GameObject("CardImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.layer = panelRect.gameObject.layer;
        imageObject.transform.SetParent(panelRect, false);
        handCardDialogImage = imageObject.GetComponent<Image>();
        handCardDialogImage.preserveAspect = true;
        handCardDialogImage.raycastTarget = false;
        RectTransform imageRect = handCardDialogImage.rectTransform;
        SetCenteredRect(imageRect, new Vector2(-125f, 5f), new Vector2(178f, 178f));

        handCardDialogBodyText = CreateDialogText("Effect", panelRect, string.Empty, 22, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.31f, 0.17f, 0.09f, 1f));
        RectTransform bodyRect = handCardDialogBodyText.rectTransform;
        SetCenteredRect(bodyRect, new Vector2(120f, 10f), new Vector2(250f, 210f));
        handCardDialogBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        handCardDialogBodyText.verticalOverflow = VerticalWrapMode.Truncate;

        handCardDialogUseButton = CreateDialogButton("UseButton", panelRect, "\u4f7f\u7528", new Color(0.24f, 0.58f, 0.33f, 1f), new Vector2(-98f, 94f), HandDialogButtonSize);
        handCardDialogCancelButton = CreateDialogButton("CancelButton", panelRect, "\u53d6\u6d88", new Color(0.62f, 0.52f, 0.42f, 1f), new Vector2(98f, 94f), HandDialogButtonSize);

        handCardDialogOverlay.gameObject.SetActive(false);
    }

    private RectTransform CreateDialogRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        rectObject.layer = parent.gameObject.layer;
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private Text CreateDialogText(string objectName, Transform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);

        Text uiText = textObject.GetComponent<Text>();
        uiText.font = feedbackFont != null ? feedbackFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.fontStyle = fontStyle;
        uiText.alignment = alignment;
        uiText.color = color;
        uiText.raycastTarget = false;
        uiText.resizeTextForBestFit = true;
        uiText.resizeTextMinSize = 10;
        uiText.resizeTextMaxSize = fontSize;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Truncate;
        return uiText;
    }

    private Button CreateDialogButton(string objectName, Transform parent, string label, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.layer = parent.gameObject.layer;
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        ConfigureDialogImage(image, color, GetQuestionButtonSprite(), Image.Type.Sliced);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        Text text = CreateDialogText("Text", buttonObject.transform, label, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetDialogStretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 3f), new Vector2(0f, 3f));
        return button;
    }

    private Button CreateTopRightButton(string objectName, Transform parent, string label, Vector2 offset, Vector2 size)
    {
        Button button = CreateDialogButton(objectName, parent, label, new Color(0.24f, 0.32f, 0.44f, 1f), Vector2.zero, size);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;

        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.fontSize = 18;
            text.resizeTextMaxSize = 18;
        }

        return button;
    }

    private Button CreatePlainButton(string objectName, Transform parent, string label, Vector2 center, Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.layer = parent.gameObject.layer;
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetCenteredRect(rect, center, size);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.28f, 1f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Text text = CreateDialogText("Text", buttonObject.transform, label, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetDialogStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private Toggle CreatePlainToggle(string objectName, Transform parent, string label, Vector2 center, Vector2 size)
    {
        GameObject toggleObject = new GameObject(objectName, typeof(RectTransform), typeof(Toggle));
        toggleObject.layer = parent.gameObject.layer;
        toggleObject.transform.SetParent(parent, false);

        RectTransform rect = toggleObject.GetComponent<RectTransform>();
        SetCenteredRect(rect, center, size);

        RectTransform boxRect = CreateDialogRect("Box", toggleObject.transform);
        boxRect.anchorMin = new Vector2(0f, 0.5f);
        boxRect.anchorMax = new Vector2(0f, 0.5f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        boxRect.anchoredPosition = new Vector2(0f, 0f);
        boxRect.sizeDelta = new Vector2(28f, 28f);

        Image boxImage = boxRect.gameObject.AddComponent<Image>();
        boxImage.color = Color.white;

        RectTransform checkRect = CreateDialogRect("Checkmark", boxRect);
        SetDialogStretch(checkRect, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
        Image checkImage = checkRect.gameObject.AddComponent<Image>();
        checkImage.color = new Color(0.16f, 0.55f, 0.28f, 1f);

        Text labelText = CreateDialogText("Label", toggleObject.transform, label, 20, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.18f, 0.18f, 0.18f, 1f));
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(42f, 0f);
        labelRect.offsetMax = Vector2.zero;

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        return toggle;
    }

    private Dropdown CreatePlainDropdown(string objectName, Transform parent, Vector2 center, Vector2 size)
    {
        GameObject dropdownObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Dropdown));
        dropdownObject.layer = parent.gameObject.layer;
        dropdownObject.transform.SetParent(parent, false);

        RectTransform rect = dropdownObject.GetComponent<RectTransform>();
        SetCenteredRect(rect, center, size);

        Image background = dropdownObject.GetComponent<Image>();
        background.color = Color.white;

        Text label = CreateDialogText("Label", dropdownObject.transform, string.Empty, 20, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.12f, 0.12f, 0.12f, 1f));
        label.resizeTextForBestFit = false;
        SetDialogStretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-42f, 0f));

        Text arrow = CreateDialogText("Arrow", dropdownObject.transform, "\u25be", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.12f, 0.12f, 0.12f, 1f));
        RectTransform arrowRect = arrow.rectTransform;
        arrowRect.anchorMin = new Vector2(1f, 0f);
        arrowRect.anchorMax = new Vector2(1f, 1f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = Vector2.zero;
        arrowRect.sizeDelta = new Vector2(38f, 0f);

        RectTransform template = CreateDropdownTemplate(dropdownObject.transform);

        Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
        dropdown.targetGraphic = background;
        dropdown.captionText = label;
        dropdown.template = template;
        dropdown.itemText = template.Find("Viewport/Content/Item/Item Label").GetComponent<Text>();
        dropdown.itemImage = null;
        return dropdown;
    }

    private RectTransform CreateDropdownTemplate(Transform parent)
    {
        RectTransform template = CreateDialogRect("Template", parent);
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, -2f);
        template.sizeDelta = new Vector2(0f, 150f);

        Image templateImage = template.gameObject.AddComponent<Image>();
        templateImage.color = Color.white;
        template.gameObject.AddComponent<ScrollRect>();

        RectTransform viewport = CreateDialogRect("Viewport", template);
        SetDialogStretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.1f);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        RectTransform content = CreateDialogRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 32f);

        RectTransform item = CreateDialogRect("Item", content);
        item.anchorMin = new Vector2(0f, 0.5f);
        item.anchorMax = new Vector2(1f, 0.5f);
        item.pivot = new Vector2(0.5f, 0.5f);
        item.anchoredPosition = Vector2.zero;
        item.sizeDelta = new Vector2(0f, 32f);
        Toggle itemToggle = item.gameObject.AddComponent<Toggle>();
        Image itemBackground = item.gameObject.AddComponent<Image>();
        itemBackground.color = new Color(0.9f, 0.93f, 0.97f, 1f);
        itemToggle.targetGraphic = itemBackground;

        Text itemLabel = CreateDialogText("Item Label", item, "Option", 18, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.12f, 0.12f, 0.12f, 1f));
        itemLabel.resizeTextForBestFit = false;
        SetDialogStretch(itemLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), Vector2.zero);

        ScrollRect scrollRect = template.GetComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        template.gameObject.SetActive(false);
        return template;
    }

    private Sprite GetQuestionPanelSprite()
    {
        if (questionOverlay == null)
        {
            return null;
        }

        Transform cardTransform = questionOverlay.Find("QuestionCard");
        Image image = cardTransform != null ? cardTransform.GetComponent<Image>() : null;
        return image != null ? image.sprite : null;
    }

    private Sprite GetQuestionButtonSprite()
    {
        for (int i = 0; i < questionButtons.Count; i++)
        {
            Button button = questionButtons[i];
            Sprite sprite = GetButtonSprite(button);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return GetButtonSprite(noticeConfirmButton);
    }

    private Sprite GetButtonSprite(Button button)
    {
        if (button == null)
        {
            return null;
        }

        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
        }

        return image != null ? image.sprite : null;
    }

    private void ConfigureDialogImage(Image image, Color fallbackColor, Sprite sprite = null, Image.Type imageType = Image.Type.Simple)
    {
        image.color = sprite != null ? Color.white : fallbackColor;
        image.raycastTarget = true;
        image.sprite = sprite;
        image.type = imageType;
        image.fillCenter = true;
    }

    private void SetDialogStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }

    private void EnsureHandPanelLayout()
    {
        if (handPanelLayout != null && handPanelLayout.gameObject != null)
        {
            SyncHandPanelSlotCount();
            handPanel = handPanelLayout.GetComponent<RectTransform>();
            handBodyRoot = handPanelLayout.BodyRoot;
            return;
        }

        HandPanelLayout existingLayout = handPanel != null ? handPanel.GetComponent<HandPanelLayout>() : null;
        if (existingLayout == null)
        {
            GameObject handPanelPrefab = Resources.Load<GameObject>("Prefabs/UI/HandPanel");
            HandPanelLayout prefabLayout = handPanelPrefab != null ? handPanelPrefab.GetComponent<HandPanelLayout>() : null;
            if (prefabLayout != null)
            {
                Transform parent = handPanel != null && handPanel.parent != null
                    ? handPanel.parent
                    : (rootCanvasRect != null ? rootCanvasRect : null);
                int siblingIndex = handPanel != null ? handPanel.GetSiblingIndex() : -1;
                bool shouldStayActive = handPanel == null || handPanel.gameObject.activeSelf;

                if (handPanel != null)
                {
                    handPanel.gameObject.SetActive(false);
                }

                GameObject instance = Instantiate(handPanelPrefab, parent, false);
                instance.name = handPanelPrefab.name;
                if (siblingIndex >= 0)
                {
                    instance.transform.SetSiblingIndex(siblingIndex);
                }

                instance.SetActive(shouldStayActive);
                existingLayout = instance.GetComponent<HandPanelLayout>();
                handPanel = instance.GetComponent<RectTransform>();
            }
        }

        if (existingLayout == null && handPanel != null)
        {
            existingLayout = handPanel.gameObject.AddComponent<HandPanelLayout>();
        }

        handPanelLayout = existingLayout;
        if (handPanelLayout != null)
        {
            SyncHandPanelSlotCount();
            handPanel = handPanelLayout.GetComponent<RectTransform>();
            handBodyRoot = handPanelLayout.BodyRoot;
        }
    }

    private void SyncHandPanelSlotCount()
    {
        if (handPanelLayout == null)
        {
            return;
        }

        int slotCount = gameManager != null ? gameManager.maxToolCardsPerPlayer : handPanelLayout.SlotCount;
        handPanelLayout.SetSlotCount(slotCount);
    }

    private void CacheSceneCollections()
    {
        questionButtons.Clear();
        if (questionOptionButtons != null)
        {
            for (int i = 0; i < questionOptionButtons.Count; i++)
            {
                if (questionOptionButtons[i] != null)
                {
                    questionButtons.Add(questionOptionButtons[i]);
                }
            }
        }

        EnsureHandPanelLayout();
        if (handPanelLayout != null)
        {
            SyncHandPanelSlotCount();
            handBodyRoot = handPanelLayout.BodyRoot;
            handCardViews.Clear();
            handCardButtons = new List<Button>();
            List<HandCardView> views = handPanelLayout.CardViews;
            for (int i = 0; i < views.Count; i++)
            {
                HandCardView view = views[i];
                if (view == null)
                {
                    continue;
                }

                handCardViews.Add(view);
                if (view.RootButton != null)
                {
                    handCardButtons.Add(view.RootButton);
                }
            }
        }
        else if ((handCardButtons == null || handCardButtons.Count == 0 || HasNullHandCardButton()) && handBodyRoot != null)
        {
            handCardViews.Clear();
            handCardButtons = new List<Button>();
            for (int i = 0; i < handBodyRoot.childCount; i++)
            {
                Button cardButton = handBodyRoot.GetChild(i).GetComponent<Button>();
                if (cardButton != null)
                {
                    handCardButtons.Add(cardButton);
                }
            }
        }

        CacheHandCardVisuals();

        EnsurePropertyInfoPanel();
    }

    private void EnsurePropertyInfoPanel()
    {
        CacheCanvasRefs();

        RectTransform propertyPanelParent = rootCanvasRect != null ? rootCanvasRect : actionPanel;
        if (propertyPanelParent == null)
        {
            return;
        }

        if (propertyInfoPanel == null)
        {
            Transform existingPanel = propertyPanelParent.Find("PropertyInfoPanel");
            if (existingPanel == null && actionPanel != null)
            {
                existingPanel = actionPanel.Find("PropertyInfoPanel");
            }
            if (existingPanel != null)
            {
                propertyInfoPanel = existingPanel.GetComponent<RectTransform>();
            }
        }

        if (propertyInfoPanel == null)
        {
            GameObject prefab = Resources.Load<GameObject>(PropertyInfoPanelPrefabPath);
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, propertyPanelParent, false);
                instance.name = "PropertyInfoPanel";
                propertyInfoPanel = instance.GetComponent<RectTransform>();
            }
        }

        if (propertyInfoPanel == null)
        {
            CacheLegacyPropertyInfoText();
            return;
        }

        if (propertyInfoPanel.parent != propertyPanelParent)
        {
            propertyInfoPanel.SetParent(propertyPanelParent, false);
        }
        ConfigurePropertyInfoPanelRect();

        propertyInfoImageMask = FindChildRect(propertyInfoPanel, "ImageMask");

        Transform imageTransform = propertyInfoPanel.Find("ImageMask/PropertyImage");
        if (imageTransform == null)
        {
            imageTransform = propertyInfoPanel.Find("PropertyImage");
        }
        if (imageTransform != null)
        {
            propertyInfoImage = imageTransform.GetComponent<Image>();
        }

        Transform titleTransform = propertyInfoPanel.Find("PropertyTitle");
        if (titleTransform != null)
        {
            Text titleText = titleTransform.GetComponent<Text>();
            if (titleText != null)
            {
                propertyInfoTitleText = titleText;
            }
        }

        Transform bodyTransform = propertyInfoPanel.Find("PropertyBody");
        if (bodyTransform != null)
        {
            Text bodyText = bodyTransform.GetComponent<Text>();
            if (bodyText != null)
            {
                propertyInfoBodyText = bodyText;
            }
        }

        HideLegacyPropertyInfoTransform("PropertyTitle");
        HideLegacyPropertyInfoTransform("PropertyBody");
        ConfigurePropertyInfoContentLayout();
    }

    private void ConfigurePropertyInfoContentLayout()
    {
        if (propertyInfoPanel == null)
        {
            return;
        }

        if (propertyInfoImageMask != null)
        {
            SetTopCenteredRect(propertyInfoImageMask, -PropertyInfoPanelMargin, PropertyInfoImageMaskSize);
        }

        if (propertyInfoImage != null)
        {
            SetCenteredRect(propertyInfoImage.rectTransform, Vector2.zero, PropertyInfoImageMaskSize);
        }

        if (propertyInfoTitleText != null)
        {
            SetTopCenteredRect(propertyInfoTitleText.rectTransform, -184f, PropertyInfoTextSize);
        }

        if (propertyInfoBodyText != null)
        {
            SetTopCenteredRect(propertyInfoBodyText.rectTransform, -224f, PropertyInfoBodySize);
        }
    }

    private void ConfigureActionPanelVisuals()
    {
        if (actionPanel == null)
        {
            return;
        }

        Image panelImage = actionPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.enabled = false;
            panelImage.raycastTarget = false;
        }
    }

    private void EnsureSettingsUi()
    {
        CacheCanvasRefs();
        if (rootCanvasRect == null)
        {
            return;
        }

        Transform legacyButton = rootCanvasRect.Find("SettingsButton");
        if (legacyButton != null)
        {
            legacyButton.gameObject.SetActive(false);
        }

        if (settingsOverlay == null)
        {
            Transform existingOverlay = rootCanvasRect.Find("SettingsOverlay");
            settingsOverlay = existingOverlay != null ? existingOverlay.GetComponent<RectTransform>() : null;
        }

        if (settingsOverlay == null)
        {
            CreateSettingsOverlay();
        }

        BindSettingsCallbacks();
        RefreshSettingsPanel();
    }

    private void CreateSettingsOverlay()
    {
        GameObject overlayObject = new GameObject("SettingsOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.layer = rootCanvasRect.gameObject.layer;
        overlayObject.transform.SetParent(rootCanvasRect, false);
        settingsOverlay = overlayObject.GetComponent<RectTransform>();
        SetDialogStretch(settingsOverlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.45f);
        overlayImage.raycastTarget = true;

        RectTransform panelRect = CreateDialogRect("SettingsPanel", settingsOverlay);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = SettingsPanelSize;

        Image panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.94f, 0.95f, 0.97f, 1f);
        panelImage.raycastTarget = true;

        Text titleText = CreateDialogText("Title", panelRect, "\u8bbe\u7f6e", 28, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.12f, 0.14f, 0.18f, 1f));
        SetCenteredRect(titleText.rectTransform, new Vector2(0f, 145f), new Vector2(360f, 52f));

        Text resolutionLabel = CreateDialogText("ResolutionLabel", panelRect, "\u5206\u8fa8\u7387", 20, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.18f, 0.18f, 0.18f, 1f));
        resolutionLabel.resizeTextForBestFit = false;
        SetCenteredRect(resolutionLabel.rectTransform, new Vector2(-174f, 72f), new Vector2(128f, 44f));

        settingsResolutionDropdown = CreatePlainDropdown("ResolutionDropdown", panelRect, new Vector2(86f, 72f), new Vector2(300f, 44f));

        settingsFullscreenToggle = CreatePlainToggle("FullscreenToggle", panelRect, "\u5168\u5c4f", new Vector2(0f, 4f), new Vector2(300f, 44f));

        Text hintText = CreateDialogText("Hint", panelRect, "\u6309 ESC \u6253\u5f00\u6216\u5173\u95ed\u8bbe\u7f6e", 16, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.34f, 0.36f, 0.4f, 1f));
        hintText.resizeTextForBestFit = false;
        SetCenteredRect(hintText.rectTransform, new Vector2(0f, -64f), new Vector2(420f, 32f));

        settingsMenuButton = CreatePlainButton("BackToMenuButton", panelRect, "\u56de\u5230\u83dc\u5355", new Vector2(-120f, -145f), SettingsWideButtonSize);
        SetCenteredRect(settingsMenuButton.GetComponent<RectTransform>(), new Vector2(-120f, -145f), SettingsWideButtonSize);

        settingsCloseButton = CreatePlainButton("CloseButton", panelRect, "\u5173\u95ed", new Vector2(120f, -145f), SettingsWideButtonSize);
        SetCenteredRect(settingsCloseButton.GetComponent<RectTransform>(), new Vector2(120f, -145f), SettingsWideButtonSize);

        settingsOverlay.gameObject.SetActive(false);
    }

    private void BindSettingsCallbacks()
    {
        if (settingsResolutionDropdown != null)
        {
            settingsResolutionDropdown.onValueChanged.RemoveAllListeners();
            settingsResolutionDropdown.onValueChanged.AddListener(OnSettingsResolutionChanged);
        }

        if (settingsFullscreenToggle != null)
        {
            settingsFullscreenToggle.onValueChanged.RemoveAllListeners();
            settingsFullscreenToggle.onValueChanged.AddListener(OnSettingsFullscreenChanged);
        }

        BindButton(settingsMenuButton, ReturnToMenuScene);
        BindButton(settingsCloseButton, CloseSettingsPanel);
    }

    private void OpenSettingsPanel()
    {
        EnsureSettingsUi();
        if (settingsOverlay == null)
        {
            return;
        }

        LoadDisplaySettingsFromPrefsOrScreen();
        RefreshSettingsPanel();
        settingsOverlay.gameObject.SetActive(true);
        settingsOverlay.SetAsLastSibling();
        PlayPopupOpen(settingsOverlay, "SettingsPanel");
    }

    private void CloseSettingsPanel()
    {
        if (settingsOverlay != null)
        {
            settingsOverlay.gameObject.SetActive(false);
        }
    }

    private void HandleSettingsShortcut()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (settingsOverlay != null && settingsOverlay.gameObject.activeSelf)
        {
            CloseSettingsPanel();
        }
        else
        {
            OpenSettingsPanel();
        }
    }

    private void OnSettingsResolutionChanged(int index)
    {
        if (suppressSettingsControlEvents)
        {
            return;
        }

        selectedResolutionIndex = Mathf.Clamp(index, 0, SettingsResolutionOptions.Length - 1);
        ApplyDisplaySettings();
    }

    private void OnSettingsFullscreenChanged(bool fullscreen)
    {
        if (suppressSettingsControlEvents)
        {
            return;
        }

        selectedFullscreen = fullscreen;
        ApplyDisplaySettings();
    }

    private void ReturnToMenuScene()
    {
        SaveDisplaySettings();
        SceneTransitionManager.LoadScene(MenuSceneName);
    }

    private void LoadDisplaySettingsFromPrefsOrScreen()
    {
        int width = PlayerPrefs.GetInt(ResolutionWidthPrefsKey, Screen.width);
        int height = PlayerPrefs.GetInt(ResolutionHeightPrefsKey, Screen.height);
        selectedResolutionIndex = FindClosestResolutionIndex(width, height);
        selectedFullscreen = PlayerPrefs.GetInt(FullscreenPrefsKey, Screen.fullScreen ? 1 : 0) != 0;
    }

    private int FindClosestResolutionIndex(int width, int height)
    {
        int bestIndex = 0;
        int bestScore = int.MaxValue;
        for (int i = 0; i < SettingsResolutionOptions.Length; i++)
        {
            Vector2Int resolution = SettingsResolutionOptions[i];
            int dx = resolution.x - width;
            int dy = resolution.y - height;
            int score = dx * dx + dy * dy;
            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void ApplyDisplaySettings()
    {
        if (SettingsResolutionOptions.Length == 0)
        {
            return;
        }

        selectedResolutionIndex = Mathf.Clamp(selectedResolutionIndex, 0, SettingsResolutionOptions.Length - 1);
        Vector2Int resolution = SettingsResolutionOptions[selectedResolutionIndex];
        FullScreenMode mode = selectedFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        Screen.SetResolution(resolution.x, resolution.y, mode);
        SaveDisplaySettings();
        RefreshSettingsPanel();
    }

    private void SaveDisplaySettings()
    {
        if (SettingsResolutionOptions.Length == 0)
        {
            return;
        }

        selectedResolutionIndex = Mathf.Clamp(selectedResolutionIndex, 0, SettingsResolutionOptions.Length - 1);
        Vector2Int resolution = SettingsResolutionOptions[selectedResolutionIndex];
        PlayerPrefs.SetInt(ResolutionWidthPrefsKey, resolution.x);
        PlayerPrefs.SetInt(ResolutionHeightPrefsKey, resolution.y);
        PlayerPrefs.SetInt(FullscreenPrefsKey, selectedFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void RefreshSettingsPanel()
    {
        suppressSettingsControlEvents = true;

        if (settingsResolutionDropdown != null)
        {
            settingsResolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            for (int i = 0; i < SettingsResolutionOptions.Length; i++)
            {
                Vector2Int resolution = SettingsResolutionOptions[i];
                options.Add($"{resolution.x} x {resolution.y}");
            }

            settingsResolutionDropdown.AddOptions(options);
            selectedResolutionIndex = Mathf.Clamp(selectedResolutionIndex, 0, SettingsResolutionOptions.Length - 1);
            settingsResolutionDropdown.value = selectedResolutionIndex;
            settingsResolutionDropdown.RefreshShownValue();
        }

        if (settingsFullscreenToggle != null)
        {
            settingsFullscreenToggle.isOn = selectedFullscreen;
        }

        suppressSettingsControlEvents = false;
    }

    private void ConfigurePropertyInfoPanelRect()
    {
        if (propertyInfoPanel == null)
        {
            return;
        }

        propertyInfoPanel.anchorMin = new Vector2(1f, 0.5f);
        propertyInfoPanel.anchorMax = new Vector2(1f, 0.5f);
        propertyInfoPanel.pivot = new Vector2(1f, 0.5f);
        propertyInfoPanel.anchoredPosition = PropertyInfoPanelOffset;
        propertyInfoPanel.sizeDelta = PropertyInfoPanelSize;
        propertyInfoPanel.localScale = Vector3.one;
    }

    private void CacheLegacyPropertyInfoText()
    {
        if (actionPanel == null)
        {
            return;
        }

        if (propertyInfoTitleText == null)
        {
            Transform titleTransform = actionPanel.Find("PropertyTitle");
            if (titleTransform != null)
            {
                propertyInfoTitleText = titleTransform.GetComponent<Text>();
            }
        }

        if (propertyInfoBodyText == null)
        {
            Transform bodyTransform = actionPanel.Find("PropertyBody");
            if (bodyTransform != null)
            {
                propertyInfoBodyText = bodyTransform.GetComponent<Text>();
            }
        }
    }

    private void HideLegacyPropertyInfoTransform(string childName)
    {
        if (actionPanel == null)
        {
            return;
        }

        Transform legacyTransform = actionPanel.Find(childName);
        if (legacyTransform != null
            && (propertyInfoPanel == null || !legacyTransform.IsChildOf(propertyInfoPanel)))
        {
            legacyTransform.gameObject.SetActive(false);
        }
    }

    private void ConfigureSceneDialogLayouts()
    {
        ConfigureQuestionDialogLayout();
        ConfigureNoticeDialogLayout();
    }

    private void ConfigureQuestionDialogLayout()
    {
        RectTransform cardRect = FindChildRect(questionOverlay, "QuestionCard");
        ConfigureDialogPanelRect(cardRect);

        if (questionTitleText != null)
        {
            ConfigureExistingDialogText(questionTitleText, 26, 28, TextAnchor.MiddleCenter, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            SetCenteredRect(questionTitleText.rectTransform, new Vector2(0f, 146f), new Vector2(420f, 48f));
        }

        if (questionBodyText != null)
        {
            ConfigureExistingDialogText(questionBodyText, 22, 24, TextAnchor.UpperLeft, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            SetCenteredRect(questionBodyText.rectTransform, new Vector2(0f, 42f), new Vector2(500f, 118f));
        }

        for (int i = 0; i < questionButtons.Count; i++)
        {
            Button button = questionButtons[i];
            if (button == null)
            {
                continue;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector2 center = GetQuestionOptionCenter(i);
                SetCenteredRect(rect, center, QuestionOptionButtonSize);
            }

            Image image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            if (image != null)
            {
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
            }

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                ConfigureExistingDialogText(text, 16, 18, TextAnchor.MiddleCenter, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
                SetDialogStretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 3f), new Vector2(-16f, 3f));
            }
        }
    }

    private Vector2 GetQuestionOptionCenter(int optionIndex)
    {
        float x = optionIndex % 2 == 0 ? -128f : 128f;
        float y = optionIndex < 2 ? -102f : -174f;
        return new Vector2(x, y);
    }

    private void ConfigureNoticeDialogLayout()
    {
        RectTransform cardRect = FindChildRect(noticeOverlay, "NoticeCard");
        ConfigureDialogPanelRect(cardRect);

        if (noticeTitleText != null)
        {
            ConfigureExistingDialogText(noticeTitleText, 26, 28, TextAnchor.MiddleCenter, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            SetCenteredRect(noticeTitleText.rectTransform, new Vector2(0f, 146f), new Vector2(420f, 48f));
        }

        if (noticeBodyText != null)
        {
            ConfigureExistingDialogText(noticeBodyText, 22, 24, TextAnchor.UpperLeft, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            SetCenteredRect(noticeBodyText.rectTransform, new Vector2(0f, -18f), new Vector2(500f, 260f));
            noticeBodyLayoutCaptured = false;
            CaptureNoticeBodyLayout();
        }

        if (noticeConfirmButton != null)
        {
            RectTransform rect = noticeConfirmButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                SetCenteredRect(rect, new Vector2(0f, -192f), NoticeButtonSize);
            }

            Image image = noticeConfirmButton.targetGraphic as Image;
            if (image == null)
            {
                image = noticeConfirmButton.GetComponent<Image>();
            }

            if (image != null)
            {
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
            }

            Text text = noticeConfirmButton.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                ConfigureExistingDialogText(text, 20, 24, TextAnchor.MiddleCenter, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
                SetDialogStretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 3f), new Vector2(0f, 3f));
            }
        }
    }

    private RectTransform FindChildRect(RectTransform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private void PlayPopupOpen(RectTransform root, string panelName)
    {
        if (root == null)
        {
            return;
        }

        RectTransform target = string.IsNullOrEmpty(panelName)
            ? root
            : FindChildRect(root, panelName);
        if (target == null)
        {
            target = root;
        }

        PopupScaleAnimator animator = target.GetComponent<PopupScaleAnimator>();
        if (animator == null)
        {
            animator = target.gameObject.AddComponent<PopupScaleAnimator>();
        }

        animator.PlayOpen();
    }

    private void ConfigureDialogPanelRect(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = DialogPanelSize;
        rect.localScale = Vector3.one;

        Image image = rect.GetComponent<Image>();
        if (image != null)
        {
            image.type = Image.Type.Simple;
            image.fillCenter = true;
        }
    }

    private void ConfigureExistingDialogText(Text text, int fontSize, int maxSize, TextAnchor alignment, HorizontalWrapMode horizontalWrap, VerticalWrapMode verticalWrap)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = fontSize;
        text.resizeTextForBestFit = false;
        text.resizeTextMinSize = fontSize;
        text.resizeTextMaxSize = fontSize;
        text.alignment = alignment;
        text.horizontalOverflow = horizontalWrap;
        text.verticalOverflow = verticalWrap;
    }

    private void SetCenteredRect(RectTransform rect, Vector2 center, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = center;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private void SetTopCenteredRect(RectTransform rect, float topY, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, topY);
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private void SetBottomCenteredRect(RectTransform rect, float bottomY, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottomY);
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private void CacheHandCardVisuals()
    {
        handCardTexts.Clear();
        handCardImages.Clear();
        handCardUseButtons.Clear();
        handCardSelectionFrames.Clear();
        if (handPanelLayout == null)
        {
            handCardViews.Clear();
        }

        if (handCardButtons == null)
        {
            return;
        }

        for (int i = 0; i < handCardButtons.Count; i++)
        {
            Button cardButton = handCardButtons[i];
            Transform cardRoot = cardButton != null ? cardButton.transform : null;

            Text cardText = null;
            Image cardImage = null;
            Button useButton = null;
            Image selectionFrame = null;

            HandCardView cardView = i < handCardViews.Count ? handCardViews[i] : null;
            if (cardView == null && cardRoot != null)
            {
                cardView = cardRoot.GetComponent<HandCardView>();
            }

            if (cardView != null)
            {
                cardView.EnsureReferences();
                cardText = cardView.EffectText;
                cardImage = cardView.CardImage;
                useButton = cardView.UseButton;
                selectionFrame = cardView.SelectionFrame;
            }
            else if (cardRoot != null)
            {
                Transform cardImageTransform = cardRoot.Find("CardImage");
                if (cardImageTransform != null)
                {
                    cardImage = cardImageTransform.GetComponent<Image>();
                }
                else
                {
                    cardImage = CreateHandCardImage(cardRoot);
                }

                Transform cardTextTransform = cardRoot.Find("CardText");
                if (cardTextTransform == null)
                {
                    cardTextTransform = cardRoot.Find("Text");
                }

                if (cardTextTransform != null)
                {
                    cardText = cardTextTransform.GetComponent<Text>();
                }

                Transform useButtonTransform = cardRoot.Find("UseButton");
                if (useButtonTransform != null)
                {
                    useButton = useButtonTransform.GetComponent<Button>();
                }

                Transform selectionFrameTransform = cardRoot.Find("SelectionFrame");
                if (selectionFrameTransform != null)
                {
                    selectionFrame = selectionFrameTransform.GetComponent<Image>();
                }
            }

            handCardTexts.Add(cardText);
            handCardImages.Add(cardImage);
            handCardUseButtons.Add(useButton);
            handCardSelectionFrames.Add(selectionFrame);
        }
    }

    private bool HasNullHandCardButton()
    {
        if (handCardButtons == null)
        {
            return true;
        }

        for (int i = 0; i < handCardButtons.Count; i++)
        {
            if (handCardButtons[i] == null)
            {
                return true;
            }
        }

        return false;
    }

    private Image CreateHandCardImage(Transform cardRoot)
    {
        GameObject imageObject = new GameObject("CardImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(cardRoot, false);
        imageObject.transform.SetAsFirstSibling();

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    private void InitPlayerHud()
    {
        playerItems.Clear();

        if (TryInitPrefabPlayerHud())
        {
            return;
        }

        if (playerInfoItems != null)
        {
            for (int i = 0; i < playerInfoItems.Count; i++)
            {
                if (playerInfoItems[i] != null)
                {
                    playerItems.Add(playerInfoItems[i]);
                }
            }
        }

        if (playerItems.Count == 0 && playerHudRoot != null)
        {
            PlayerInfoItem[] items = playerHudRoot.GetComponentsInChildren<PlayerInfoItem>(true);
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    playerItems.Add(items[i]);
                }
            }
        }
    }

    private void UpdateTopInfo()
    {
        if (turnText != null)
        {
            turnText.text = gameManager.turnNumber.ToString();
        }
        if (diceText != null)
        {
            diceText.text = gameManager.lastDiceValue > 0 ? gameManager.lastDiceValue.ToString() : string.Empty;
        }
        if (statusText != null)
        {
            statusText.text = GetStatusLabel(gameManager.status);
        }
    }

    private void UpdatePropertyInfoPanel()
    {
        propertyInfoVisible = false;
        EnsurePropertyInfoPanel();

        if (propertyInfoPanel == null && propertyInfoTitleText == null && propertyInfoBodyText == null)
        {
            return;
        }

        if (gameManager == null || gameManager.status == Status.GameOver || !gameManager.IsPlayerAlive(gameManager.currentPlayerIndex))
        {
            SetPropertyInfoVisible(false);
            return;
        }

        int playerIndex = gameManager.currentPlayerIndex;
        int tileIndex = gameManager.GetPlayerCurrentTileIndexSafe(playerIndex);
        TileData tileData = gameManager.GetTileDataByIndex(tileIndex);
        if (tileData == null || tileData.tileCost <= 0)
        {
            SetPropertyInfoVisible(false);
            return;
        }

        int ownerIndex = gameManager.GetTileOwnerIndex(tileIndex);
        string ownerText = ownerIndex < 0
            ? "\u65e0\u4e3b"
            : (ownerIndex == playerIndex ? "\u4f60" : GetPlayerName(ownerIndex));
        string stateText;
        int currentRent = gameManager.GetTileCurrentRent(tileIndex, tileData);
        if (ownerIndex < 0)
        {
            stateText = gameManager.CanBuyCurrentTile(playerIndex)
                ? "\u53ef\u8d2d\u4e70"
                : "\u8d44\u91d1\u4e0d\u8db3";
        }
        else if (ownerIndex == playerIndex)
        {
            stateText = "\u4f60\u7684\u5730\u4ea7";
        }
        else
        {
            stateText = $"\u505c\u7559\u9700\u4ed8 {currentRent}";
        }

        if (propertyInfoTitleText != null)
        {
            propertyInfoTitleText.text = tileData.tileName;
        }

        if (propertyInfoBodyText != null)
        {
            propertyInfoBodyText.text =
                $"\u552e\u4ef7 {tileData.tileCost} / \u8def\u8d39 {currentRent}\n" +
                $"\u5f52\u5c5e\uff1a{ownerText}\n" +
                stateText;
        }

        SetPropertyInfoImage(GetPropertySprite(tileData));
        SetPropertyInfoVisible(true);
    }

    private void SetPropertyInfoVisible(bool visible)
    {
        bool wasVisible = propertyInfoPanelVisibleLastFrame
            || (propertyInfoPanel != null && propertyInfoPanel.gameObject.activeSelf);
        propertyInfoVisible = visible;
        propertyInfoPanelVisibleLastFrame = visible;

        if (propertyInfoPanel != null)
        {
            propertyInfoPanel.gameObject.SetActive(visible);
            if (visible && !wasVisible)
            {
                PlayPopupOpen(propertyInfoPanel, null);
            }
        }

        if (propertyInfoTitleText != null)
        {
            propertyInfoTitleText.gameObject.SetActive(visible);
        }

        if (propertyInfoBodyText != null)
        {
            propertyInfoBodyText.gameObject.SetActive(visible);
        }

        if (!visible && propertyInfoImage != null)
        {
            propertyInfoImage.sprite = null;
            propertyInfoImage.enabled = false;
        }
    }

    private void SetPropertyInfoImage(Sprite sprite)
    {
        if (propertyInfoImage == null)
        {
            return;
        }

        bool hasSprite = sprite != null;
        if (propertyInfoImageMask != null)
        {
            propertyInfoImageMask.gameObject.SetActive(hasSprite);
        }

        propertyInfoImage.sprite = sprite;
        propertyInfoImage.enabled = hasSprite;
        propertyInfoImage.gameObject.SetActive(hasSprite);

        if (hasSprite)
        {
            FitPropertyInfoImageToMask(sprite);
        }
    }

    private void FitPropertyInfoImageToMask(Sprite sprite)
    {
        if (sprite == null || propertyInfoImage == null)
        {
            return;
        }

        RectTransform imageRect = propertyInfoImage.rectTransform;
        RectTransform maskRect = propertyInfoImageMask != null
            ? propertyInfoImageMask
            : imageRect.parent as RectTransform;
        if (maskRect == null)
        {
            return;
        }

        Vector2 maskSize = maskRect.rect.size;
        if (maskSize.x <= 0f || maskSize.y <= 0f)
        {
            maskSize = maskRect.sizeDelta;
        }

        if (maskSize.x <= 0f || maskSize.y <= 0f || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
        {
            return;
        }

        float maskAspect = maskSize.x / maskSize.y;
        float spriteAspect = sprite.rect.width / sprite.rect.height;
        Vector2 targetSize = spriteAspect >= maskAspect
            ? new Vector2(maskSize.y * spriteAspect, maskSize.y)
            : new Vector2(maskSize.x, maskSize.x / spriteAspect);

        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = targetSize;
        imageRect.localScale = Vector3.one;
    }

    private Sprite GetPropertySprite(TileData tileData)
    {
        if (tileData == null || string.IsNullOrEmpty(tileData.tileName))
        {
            return null;
        }

        int levelNumber = GetTileLevelNumber(tileData);
        if (levelNumber > 0)
        {
            Sprite levelSprite = LoadPropertySprite($"{PropertyImageRootPath}/Level{levelNumber}/{tileData.tileName}");
            if (levelSprite != null)
            {
                return levelSprite;
            }
        }

        return LoadPropertySprite($"{PropertyImageRootPath}/{tileData.tileName}");
    }

    private Sprite LoadPropertySprite(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        if (propertySpritesByPath.TryGetValue(resourcePath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                sprite.name = texture.name;
            }
        }

        propertySpritesByPath[resourcePath] = sprite;
        return sprite;
    }

    private int GetTileLevelNumber(TileData tileData)
    {
        if (tileData == null || string.IsNullOrWhiteSpace(tileData.tileID))
        {
            return 0;
        }

        string tileId = tileData.tileID.Trim();
        if (tileId.Length < 2 || char.ToUpperInvariant(tileId[0]) != 'L')
        {
            return 0;
        }

        int digitEnd = 1;
        while (digitEnd < tileId.Length && char.IsDigit(tileId[digitEnd]))
        {
            digitEnd++;
        }

        if (digitEnd <= 1)
        {
            return 0;
        }

        return int.TryParse(tileId.Substring(1, digitEnd - 1), out int levelNumber)
            ? levelNumber
            : 0;
    }

    private void UpdateButtons()
    {
        bool canRoll = gameManager.CanHumanRoll();
        bool canBuy = gameManager.CanHumanBuyCurrentTile();
        bool canEndTurn = gameManager.CanHumanEndTurn();

        UpdateButtonState(MoveButton, canRoll);
        UpdateBuyButtonLabel();
        UpdateButtonState(BuyButton, canBuy);
        UpdateButtonState(SkipButton, canEndTurn);
        ArrangeActionPanelButtons(canRoll, canBuy, canEndTurn);

        if (actionPanel != null)
        {
            actionPanel.gameObject.SetActive(canRoll || canBuy || canEndTurn);
        }

        if (handPanel != null)
        {
            bool visible = gameManager.ShouldShowHumanHand();
            handPanel.gameObject.SetActive(visible);
            if (!visible)
            {
                handSignature = string.Empty;
                selectedHandCardIndex = -1;
                HideHandCardDialog();
            }
            else
            {
                RefreshHandCardDialogUseState();
            }
        }
    }

    private void ArrangeActionPanelButtons(bool canRoll, bool canBuy, bool canEndTurn)
    {
        if (actionPanel == null)
        {
            return;
        }

        int visibleCount = 0;
        if (canRoll) visibleCount++;
        if (canBuy) visibleCount++;
        if (canEndTurn) visibleCount++;

        if (visibleCount <= 0)
        {
            return;
        }

        float panelHeight = ActionPanelVerticalPadding * 2f
            + ActionButtonSize.y * visibleCount
            + ActionButtonSpacing * Mathf.Max(0, visibleCount - 1);
        actionPanel.sizeDelta = new Vector2(ActionPanelWidth, panelHeight);

        float buttonTopY = -ActionPanelVerticalPadding;
        if (canRoll)
        {
            PositionActionButton(MoveButton, buttonTopY);
            buttonTopY -= ActionButtonSize.y + ActionButtonSpacing;
        }

        if (canBuy)
        {
            PositionActionButton(BuyButton, buttonTopY);
            buttonTopY -= ActionButtonSize.y + ActionButtonSpacing;
        }

        if (canEndTurn)
        {
            PositionActionButton(SkipButton, buttonTopY);
        }
    }

    private void PositionActionButton(Button button, float topY)
    {
        if (button == null)
        {
            return;
        }

        RectTransform buttonRect = button.transform as RectTransform;
        if (buttonRect == null)
        {
            return;
        }

        buttonRect.anchorMin = new Vector2(0.5f, 1f);
        buttonRect.anchorMax = new Vector2(0.5f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 1f);
        buttonRect.anchoredPosition = new Vector2(0f, topY);
        buttonRect.sizeDelta = ActionButtonSize;
        buttonRect.localScale = Vector3.one;
    }

    private void UpdateBuyButtonLabel()
    {
        if (BuyButton == null || gameManager == null)
        {
            return;
        }

        string label = "\u8d2d\u4e70\u5730\u4ea7";

        Text buttonText = BuyButton.GetComponentInChildren<Text>(true);
        if (buttonText != null && buttonText.text != label)
        {
            buttonText.text = label;
        }
    }

    private bool TryInitPrefabPlayerHud()
    {
        if (playerHudRoot == null)
        {
            return false;
        }

        PlayerInfoItem prefab = ResolvePlayerInfoItemPrefab();
        if (prefab == null)
        {
            return false;
        }

        HideScenePlayerHudItems();

        int playerCount = gameManager != null
            ? Mathf.Max(1, gameManager.totalPlayers)
            : Mathf.Max(1, playerInfoItems != null ? playerInfoItems.Count : 4);

        List<PlayerInfoItem> generatedItems = GetGeneratedPlayerHudItems();
        while (generatedItems.Count < playerCount)
        {
            PlayerInfoItem item = Instantiate(prefab, playerHudRoot, false);
            item.name = $"PlayerCard_{generatedItems.Count + 1}";
            generatedItems.Add(item);
        }

        for (int i = 0; i < generatedItems.Count; i++)
        {
            PlayerInfoItem item = generatedItems[i];
            if (item == null)
            {
                continue;
            }

            bool active = i < playerCount;
            item.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            LayoutPlayerHudItem(item, i);
            playerItems.Add(item);
        }

        return playerItems.Count > 0;
    }

    private PlayerInfoItem ResolvePlayerInfoItemPrefab()
    {
        if (playerCardPrefab != null)
        {
            return playerCardPrefab;
        }

        GameObject prefabObject = Resources.Load<GameObject>(PlayerCardPrefabPath);
        if (prefabObject == null)
        {
            return null;
        }

        playerCardPrefab = prefabObject.GetComponent<PlayerInfoItem>();
        return playerCardPrefab;
    }

    private List<PlayerInfoItem> GetGeneratedPlayerHudItems()
    {
        List<PlayerInfoItem> generatedItems = new List<PlayerInfoItem>();
        PlayerInfoItem[] allItems = playerHudRoot.GetComponentsInChildren<PlayerInfoItem>(true);
        for (int i = 0; i < allItems.Length; i++)
        {
            PlayerInfoItem item = allItems[i];
            if (item == null || item.transform.parent != playerHudRoot || IsScenePlayerHudItem(item))
            {
                continue;
            }
            generatedItems.Add(item);
        }

        return generatedItems;
    }

    private bool IsScenePlayerHudItem(PlayerInfoItem item)
    {
        if (playerInfoItems == null)
        {
            return false;
        }

        for (int i = 0; i < playerInfoItems.Count; i++)
        {
            if (playerInfoItems[i] == item)
            {
                return true;
            }
        }

        return false;
    }

    private void HideScenePlayerHudItems()
    {
        if (playerInfoItems == null)
        {
            return;
        }

        for (int i = 0; i < playerInfoItems.Count; i++)
        {
            PlayerInfoItem item = playerInfoItems[i];
            if (item != null)
            {
                item.gameObject.SetActive(false);
            }
        }
    }

    private void LayoutPlayerHudItem(PlayerInfoItem item, int index)
    {
        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        float scale = Mathf.Max(0.01f, playerCardScale);
        float spacing = Mathf.Max(0f, playerCardSpacing);
        float itemWidth = rect.rect.width > 0f ? rect.rect.width : PlayerHudItemFallbackSize.x;
        rect.localScale = new Vector3(scale, scale, rect.localScale.z);
        rect.anchoredPosition = playerCardOffset + new Vector2(index * (itemWidth * scale + spacing), 0f);
    }

    private void UpdatePlayerHud()
    {
        bool needsPrefabRefresh = playerItems.Count < gameManager.totalPlayers && ResolvePlayerInfoItemPrefab() != null;
        if (playerItems.Count == 0 || needsPrefabRefresh)
        {
            InitPlayerHud();
        }
        int playerCount = Mathf.Min(playerItems.Count, gameManager.totalPlayers);
        for (int i = 0; i < playerItems.Count; i++)
        {
            if (playerItems[i] != null)
            {
                playerItems[i].gameObject.SetActive(i < playerCount);
                if (i < playerCount)
                {
                    LayoutPlayerHudItem(playerItems[i], i);
                }
            }
        }
        for (int i = 0; i < playerCount; i++)
        {
            PlayerInfoItem item = playerItems[i];
            if (item == null)
            {
                continue;
            }
            item.SetName(GetPlayerHudRoleName(i));
            item.SetControlTag(GetPlayerControlTag(i));
            item.SetAvatar(GetPlayerAvatar(i));
            item.SetMoney(GetDisplayedMoneyValue(i, gameManager.GetMoney(i)));
            item.SetOwnedPropertySummary(gameManager.GetOwnedPropertySummary(i));
            item.SetAlive(gameManager.IsPlayerAlive(i));
            item.SetHighlight(i == gameManager.currentPlayerIndex && gameManager.status != Status.GameOver);
        }
    }

    private void UpdateHandPanel()
    {
        if (handPanel == null || !gameManager.ShouldShowHumanHand())
        {
            return;
        }

        List<CardData> cards = gameManager.GetPlayerToolCards(gameManager.currentPlayerIndex);
        bool canUseCards = gameManager.CanHumanUseToolCards(gameManager.currentPlayerIndex);
        if (selectedHandCardIndex >= cards.Count || !canUseCards)
        {
            selectedHandCardIndex = -1;
        }

        if (pendingHandCardIndex >= cards.Count)
        {
            HideHandCardDialog();
        }

        if ((handCardViews.Count == 0 && handCardButtons.Count == 0) || HasNullHandCardButton())
        {
            CacheSceneCollections();
        }

        int slotCount = handCardViews.Count > 0 ? handCardViews.Count : handCardButtons.Count;
        string nextSignature = gameManager.currentPlayerIndex + ":" + cards.Count + ":" + canUseCards + ":" + selectedHandCardIndex + ":" + slotCount;
        for (int i = 0; i < cards.Count; i++)
        {
            nextSignature += "|" + cards[i].id + ":" + cards[i].spriteName + ":" + cards[i].description;
        }

        if (handSignature == nextSignature)
        {
            return;
        }

        handSignature = nextSignature;
        if (handTitleText != null)
        {
            int maxSlots = slotCount > 0 ? slotCount : gameManager.maxToolCardsPerPlayer;
            handTitleText.text = cards.Count > 0 ? $"\u9053\u5177\u624b\u724c \u00b7 {cards.Count}/{maxSlots}" : "\u9053\u5177\u624b\u724c \u00b7 \u6682\u65e0\u9053\u5177";
        }

        for (int i = 0; i < slotCount; i++)
        {
            HandCardView cardView = i < handCardViews.Count ? handCardViews[i] : null;
            if (cardView != null)
            {
                if (i < cards.Count)
                {
                    int cardIndex = i;
                    cardView.SetCard(
                        cards[i],
                        GetCardSprite(cards[i]),
                        true,
                        false,
                        () => ShowHandCardDialog(cardIndex),
                        null);
                }
                else
                {
                    cardView.SetEmpty();
                }

                continue;
            }

            Button cardButton = i < handCardButtons.Count ? handCardButtons[i] : null;
            if (cardButton == null)
            {
                continue;
            }

            Text cardText = i < handCardTexts.Count ? handCardTexts[i] : null;
            Image cardImage = i < handCardImages.Count ? handCardImages[i] : null;
            Button useButton = i < handCardUseButtons.Count ? handCardUseButtons[i] : null;
            Image selectionFrame = i < handCardSelectionFrames.Count ? handCardSelectionFrames[i] : null;
            Text buttonText = cardText;
            cardButton.onClick.RemoveAllListeners();
            if (useButton != null)
            {
                useButton.onClick.RemoveAllListeners();
            }

            if (i < cards.Count)
            {
                int cardIndex = i;
                cardButton.gameObject.SetActive(true);
                cardButton.interactable = true;
                Sprite cardSprite = GetCardSprite(cards[i]);
                bool hasCardSprite = cardSprite != null;
                if (cardImage != null)
                {
                    cardImage.sprite = cardSprite;
                    cardImage.enabled = hasCardSprite;
                }

                if (cardText != null)
                {
                    cardText.gameObject.SetActive(true);
                    ApplyHandCardTextVisual(cardText, true);
                    cardText.text = cards[i].cardName;
                }

                if (selectionFrame != null)
                {
                    selectionFrame.gameObject.SetActive(false);
                }

                if (useButton != null)
                {
                    useButton.gameObject.SetActive(false);
                    useButton.interactable = false;
                }

                cardButton.onClick.AddListener(() => ShowHandCardDialog(cardIndex));
            }
            else
            {
                cardButton.gameObject.SetActive(true);
                cardButton.interactable = false;
                if (selectionFrame != null)
                {
                    selectionFrame.gameObject.SetActive(false);
                }

                if (cardImage != null)
                {
                    cardImage.sprite = null;
                    cardImage.enabled = false;
                }

                if (useButton != null)
                {
                    useButton.gameObject.SetActive(false);
                    useButton.interactable = false;
                }

                if (cardText != null)
                {
                    cardText.gameObject.SetActive(true);
                    ApplyHandCardTextVisual(cardText, false);
                    buttonText.text = "\u7a7a\u69fd";
                }
            }
        }
    }

    private void ShowHandCardDialog(int cardIndex)
    {
        if (gameManager == null)
        {
            return;
        }

        List<CardData> cards = gameManager.GetPlayerToolCards(gameManager.currentPlayerIndex);
        if (cardIndex < 0 || cardIndex >= cards.Count)
        {
            HideHandCardDialog();
            return;
        }

        EnsureHandCardDialog();
        if (handCardDialogOverlay == null)
        {
            return;
        }

        CardData card = cards[cardIndex];
        pendingHandCardIndex = cardIndex;

        if (handCardDialogTitleText != null)
        {
            handCardDialogTitleText.gameObject.SetActive(true);
            handCardDialogTitleText.text = card != null && !string.IsNullOrEmpty(card.cardName) ? card.cardName : "\u4f7f\u7528\u9053\u5177";
        }

        if (handCardDialogBodyText != null)
        {
            string effect = card != null
                ? (!string.IsNullOrEmpty(card.description) ? card.description : card.effect)
                : string.Empty;
            handCardDialogBodyText.text = string.IsNullOrEmpty(effect) ? "\u6682\u65e0\u6548\u679c\u8bf4\u660e" : effect;
        }

        if (handCardDialogImage != null)
        {
            Sprite sprite = GetCardSprite(card);
            handCardDialogImage.sprite = sprite;
            handCardDialogImage.enabled = sprite != null;
            handCardDialogImage.gameObject.SetActive(sprite != null);
        }

        if (handCardDialogUseButton != null)
        {
            handCardDialogUseButton.onClick.RemoveAllListeners();
            handCardDialogUseButton.onClick.AddListener(UsePendingHandCard);
        }

        if (handCardDialogCancelButton != null)
        {
            handCardDialogCancelButton.onClick.RemoveAllListeners();
            handCardDialogCancelButton.onClick.AddListener(HideHandCardDialog);
        }

        handCardDialogOverlay.gameObject.SetActive(true);
        handCardDialogOverlay.SetAsLastSibling();
        PlayPopupOpen(handCardDialogOverlay, "Panel");
        RefreshHandCardDialogUseState();
    }

    private void HideHandCardDialog()
    {
        pendingHandCardIndex = -1;
        if (handCardDialogOverlay != null)
        {
            handCardDialogOverlay.gameObject.SetActive(false);
        }
    }

    private void RefreshHandCardDialogUseState()
    {
        if (handCardDialogOverlay == null || !handCardDialogOverlay.gameObject.activeSelf)
        {
            return;
        }

        List<CardData> cards = gameManager != null
            ? gameManager.GetPlayerToolCards(gameManager.currentPlayerIndex)
            : null;
        bool validCard = cards != null && pendingHandCardIndex >= 0 && pendingHandCardIndex < cards.Count;
        if (!validCard)
        {
            HideHandCardDialog();
            return;
        }

        if (handCardDialogUseButton != null)
        {
            handCardDialogUseButton.interactable = gameManager.CanHumanUseToolCard(gameManager.currentPlayerIndex, pendingHandCardIndex);
        }
    }

    private void UsePendingHandCard()
    {
        if (gameManager == null || !gameManager.CanHumanUseToolCard(gameManager.currentPlayerIndex, pendingHandCardIndex))
        {
            RefreshHandCardDialogUseState();
            return;
        }

        List<CardData> cards = gameManager.GetPlayerToolCards(gameManager.currentPlayerIndex);
        if (pendingHandCardIndex < 0 || pendingHandCardIndex >= cards.Count)
        {
            HideHandCardDialog();
            return;
        }

        int cardIndex = pendingHandCardIndex;
        HideHandCardDialog();
        selectedHandCardIndex = -1;
        handSignature = string.Empty;
        gameManager.RequestUseToolCard(cardIndex);
        UpdateHandPanel();
    }

    private void ApplyHandCardTextVisual(Text cardText, bool imageMode)
    {
        if (cardText == null)
        {
            return;
        }

        RectTransform rect = cardText.rectTransform;
        Outline outline = cardText.GetComponent<Outline>();
        if (imageMode && outline == null)
        {
            outline = cardText.gameObject.AddComponent<Outline>();
        }

        if (imageMode)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 5f);
            rect.sizeDelta = new Vector2(-18f, 28f);
            cardText.alignment = TextAnchor.MiddleCenter;
            cardText.color = Color.white;
            cardText.fontStyle = FontStyle.Bold;
            cardText.resizeTextForBestFit = true;
            cardText.resizeTextMinSize = 9;
            cardText.resizeTextMaxSize = 16;
            cardText.horizontalOverflow = HorizontalWrapMode.Wrap;
            cardText.verticalOverflow = VerticalWrapMode.Truncate;
            if (outline != null)
            {
                outline.enabled = true;
                outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
        }
        else
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 6f);
            rect.sizeDelta = new Vector2(-24f, -24f);
            cardText.alignment = TextAnchor.UpperLeft;
            cardText.color = new Color(0.27f, 0.18f, 0.12f, 1f);
            cardText.fontStyle = FontStyle.Bold;
            cardText.resizeTextForBestFit = true;
            cardText.resizeTextMinSize = 10;
            cardText.resizeTextMaxSize = 18;
            cardText.horizontalOverflow = HorizontalWrapMode.Wrap;
            cardText.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline != null)
            {
                outline.enabled = false;
            }
        }
    }

    private Sprite GetCardSprite(CardData card)
    {
        if (card == null)
        {
            return null;
        }

        string spriteName = !string.IsNullOrEmpty(card.spriteName) ? card.spriteName : GetFallbackCardSpriteName(card.id);
        if (string.IsNullOrEmpty(spriteName))
        {
            return null;
        }

        EnsureCardSpritesLoaded();
        return cardSpritesByName.TryGetValue(spriteName, out Sprite sprite) ? sprite : null;
    }

    private string GetFallbackCardSpriteName(string cardId)
    {
        if (string.IsNullOrEmpty(cardId) || cardId.Length < 2)
        {
            return string.Empty;
        }

        string numberPart = cardId.Substring(1);
        if (!int.TryParse(numberPart, out int number) || number <= 0)
        {
            return string.Empty;
        }

        char type = char.ToUpperInvariant(cardId[0]);
        if (type == 'T')
        {
            return $"Card_{number - 1}";
        }

        if (type == 'L')
        {
            return $"Card_{number + 5}";
        }

        return string.Empty;
    }

    private void EnsureCardSpritesLoaded()
    {
        if (cardSpritesLoaded)
        {
            return;
        }

        cardSpritesLoaded = true;
        cardSpritesByName.Clear();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Chance/Card");
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite != null && !string.IsNullOrEmpty(sprite.name))
            {
                cardSpritesByName[sprite.name] = sprite;
            }
        }
    }

    private void SetNoticeCardVisual(Sprite sprite)
    {
        if (sprite == null)
        {
            if (noticeCardImage != null)
            {
                noticeCardImage.sprite = null;
                noticeCardImage.gameObject.SetActive(false);
            }

            RestoreNoticeBodyLayout();
            return;
        }

        Image image = EnsureNoticeCardImage();
        if (image == null)
        {
            return;
        }

        CaptureNoticeBodyLayout();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.gameObject.SetActive(true);

        if (noticeBodyText != null)
        {
            SetCenteredRect(noticeBodyText.rectTransform, new Vector2(118f, 6f), new Vector2(260f, 210f));
            noticeBodyText.alignment = TextAnchor.UpperLeft;
        }
    }

    private Image EnsureNoticeCardImage()
    {
        if (noticeCardImage != null)
        {
            return noticeCardImage;
        }

        Transform parent = noticeBodyText != null ? noticeBodyText.transform.parent : null;
        if (parent == null && noticeOverlay != null)
        {
            parent = noticeOverlay;
        }

        if (parent == null)
        {
            return null;
        }

        GameObject imageObject = new GameObject("NoticeCardImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        imageObject.transform.SetSiblingIndex(1);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        SetCenteredRect(rect, new Vector2(-126f, 6f), new Vector2(190f, 190f));

        noticeCardImage = imageObject.GetComponent<Image>();
        noticeCardImage.raycastTarget = false;
        noticeCardImage.preserveAspect = true;
        noticeCardImage.gameObject.SetActive(false);
        return noticeCardImage;
    }

    private void CaptureNoticeBodyLayout()
    {
        if (noticeBodyLayoutCaptured || noticeBodyText == null)
        {
            return;
        }

        RectTransform bodyRect = noticeBodyText.rectTransform;
        noticeBodyAnchorMin = bodyRect.anchorMin;
        noticeBodyAnchorMax = bodyRect.anchorMax;
        noticeBodyAnchoredPosition = bodyRect.anchoredPosition;
        noticeBodySizeDelta = bodyRect.sizeDelta;
        noticeBodyPivot = bodyRect.pivot;
        noticeBodyLayoutCaptured = true;
    }

    private void RestoreNoticeBodyLayout()
    {
        if (!noticeBodyLayoutCaptured || noticeBodyText == null)
        {
            return;
        }

        RectTransform bodyRect = noticeBodyText.rectTransform;
        bodyRect.anchorMin = noticeBodyAnchorMin;
        bodyRect.anchorMax = noticeBodyAnchorMax;
        bodyRect.anchoredPosition = noticeBodyAnchoredPosition;
        bodyRect.sizeDelta = noticeBodySizeDelta;
        bodyRect.pivot = noticeBodyPivot;
    }

    private void ToggleHandCardSelection(int cardIndex)
    {
        selectedHandCardIndex = selectedHandCardIndex == cardIndex ? -1 : cardIndex;
        handSignature = string.Empty;
        UpdateHandPanel();
    }

    private void UseSelectedHandCard(int cardIndex)
    {
        if (!gameManager.CanHumanUseToolCard(gameManager.currentPlayerIndex, cardIndex))
        {
            return;
        }

        selectedHandCardIndex = -1;
        handSignature = string.Empty;
        gameManager.RequestUseToolCard(cardIndex);
        UpdateHandPanel();
    }

    private void BindSceneDialogs()
    {
        for (int i = 0; i < questionButtons.Count; i++)
        {
            Button button = questionButtons[i];
            if (button == null)
            {
                continue;
            }

            int optionIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnQuestionAnswered(optionIndex));
        }

        if (noticeConfirmButton != null)
        {
            noticeConfirmButton.onClick.RemoveAllListeners();
            noticeConfirmButton.onClick.AddListener(OnNoticeConfirmed);
        }
    }

    private void BindRuntimeCallbacks()
    {
        if (callbacksBound || gameManager == null)
        {
            return;
        }

        BindButton(MoveButton, gameManager.RequestRollDice);
        BindButton(BuyButton, gameManager.RequestBuyOrUpgrade);
        BindButton(SkipButton, gameManager.RequestEndTurn);
        callbacksBound = true;
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void UpdateButtonState(Button button, bool canUse)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(canUse);
        button.interactable = canUse;
    }

    private void OnQuestionAnswered(int selectedIndex)
    {
        Action<int> callback = questionCallback;
        HideQuestion();
        callback?.Invoke(selectedIndex);
    }

    private void OnNoticeConfirmed()
    {
        Action callback = noticeCallback;
        HideNotice();
        callback?.Invoke();
    }

    private void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        AddLogLine(condition);
    }

    private void HandleDebugLogToggle()
    {
        if (!runtimeReady || logPanel == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            SetDebugLogPanelVisible(!debugLogPanelVisible);
        }
    }

#if UNITY_EDITOR
    private void HandleEditorHandCardShortcut()
    {
        if (!Input.GetKeyDown(KeyCode.Q) || gameManager == null || !gameManager.ShouldShowHumanHand())
        {
            return;
        }

        CardData card = gameManager.EditorGiveRandomToolCardToCurrentPlayer();
        if (card == null)
        {
            return;
        }

        selectedHandCardIndex = -1;
        handSignature = string.Empty;
        CacheSceneCollections();
        UpdateHandPanel();
    }

    private void HandleEditorQuestionShortcut()
    {
        if (!Input.GetKeyDown(KeyCode.E) || gameManager == null)
        {
            return;
        }

        gameManager.EditorShowRandomQuestion();
    }

    private void HandleEditorMoneyShortcut()
    {
        if (!Input.GetKeyDown(KeyCode.M) || gameManager == null)
        {
            return;
        }

        gameManager.EditorGiveMoneyToCurrentPlayer(1000);
    }
#endif

    private void SetDebugLogPanelVisible(bool visible)
    {
        debugLogPanelVisible = visible;
        if (logPanel != null)
        {
            logPanel.gameObject.SetActive(visible);
            if (visible)
            {
                PlayPopupOpen(logPanel, null);
            }
        }
    }

    private int GetDisplayedMoneyValue(int playerIndex, int fallbackMoney)
    {
        return moneyDisplayOverrides.TryGetValue(playerIndex, out int displayMoney) ? displayMoney : fallbackMoney;
    }

    private IEnumerator ProcessMoneyChangeQueue(int playerIndex)
    {
        while (moneyFxQueues.TryGetValue(playerIndex, out Queue<MoneyChangeFxRequest> queue) && queue.Count > 0)
        {
            MoneyChangeFxRequest request = queue.Dequeue();
            moneyDisplayOverrides[playerIndex] = request.fromMoney;
            yield return PlayMoneyChangeAnimation(playerIndex, request);
            moneyDisplayOverrides[playerIndex] = request.toMoney;
            yield return new WaitForSeconds(0.05f);
        }

        moneyDisplayOverrides.Remove(playerIndex);
        moneyPendingTargets.Remove(playerIndex);
        moneyFxQueues.Remove(playerIndex);
        moneyFxCoroutines.Remove(playerIndex);
    }

    private IEnumerator PlayMoneyChangeAnimation(int playerIndex, MoneyChangeFxRequest request)
    {
        CacheCanvasRefs();
        if (rootCanvasRect == null)
        {
            yield break;
        }

        Vector2 targetPosition = GetMoneyTargetLocalPosition(playerIndex);
        Vector2 startPosition = WorldToCanvasLocalPoint(request.worldStart);

        Text travelText = CreateFxText("\u5609\u79be\u5e01", new Color(1f, 0.92f, 0.44f, 1f), 24, FontStyle.Bold);
        if (travelText != null)
        {
            yield return AnimateTextTransform(GetFxRootRect(travelText), startPosition, targetPosition, 0.45f, 0.72f, 1f, 0.92f, 0.18f);
            DestroyFxText(travelText);
        }

        Text amountText = CreateFxText(FormatSignedMoney(request.delta), request.delta >= 0 ? new Color(0.36f, 1f, 0.62f, 1f) : new Color(1f, 0.46f, 0.46f, 1f), 30, FontStyle.Bold);
        if (amountText == null)
        {
            moneyDisplayOverrides[playerIndex] = request.toMoney;
            yield break;
        }

        RectTransform amountRect = GetFxRootRect(amountText);
        Vector2 amountStart = targetPosition + new Vector2(0f, 18f);
        Vector2 amountEnd = targetPosition + new Vector2(0f, 58f);
        amountRect.anchoredPosition = amountStart;

        float duration = 0.38f;
        float elapsed = 0f;
        bool appliedMoney = false;
        Color originalColor = amountText.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            amountRect.anchoredPosition = Vector2.Lerp(amountStart, amountEnd, eased);
            amountRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.12f, eased);

            Color color = originalColor;
            color.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.2f) / 0.8f));
            amountText.color = color;

            if (!appliedMoney && t >= 0.8f)
            {
                moneyDisplayOverrides[playerIndex] = request.toMoney;
                appliedMoney = true;
            }

            yield return null;
        }

        if (!appliedMoney)
        {
            moneyDisplayOverrides[playerIndex] = request.toMoney;
        }

        DestroyFxText(amountText);
    }

    private IEnumerator AnimateTextTransform(RectTransform rectTransform, Vector2 start, Vector2 end, float duration, float startScale, float endScale, float startAlpha, float endAlpha)
    {
        if (rectTransform == null)
        {
            yield break;
        }

        Text text = rectTransform.GetComponent<Text>();
        if (text == null)
        {
            text = rectTransform.GetComponentInChildren<Text>(true);
        }

        Image[] images = rectTransform.GetComponentsInChildren<Image>(true);
        Color originalColor = text != null ? text.color : Color.white;
        Color[] originalImageColors = new Color[images.Length];
        for (int i = 0; i < images.Length; i++)
        {
            originalImageColors[i] = images[i] != null ? images[i].color : Color.white;
        }
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            rectTransform.anchoredPosition = Vector2.Lerp(start, end, eased);
            rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);

            if (text != null)
            {
                Color color = originalColor;
                color.a = Mathf.Lerp(startAlpha, endAlpha, eased);
                text.color = color;
            }

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null)
                {
                    continue;
                }

                Color color = originalImageColors[i];
                color.a *= Mathf.Lerp(startAlpha, endAlpha, eased);
                images[i].color = color;
            }

            yield return null;
        }
    }

    private Text CreateFxText(string content, Color color, int fontSize, FontStyle fontStyle)
    {
        CacheCanvasRefs();
        if (rootCanvasRect == null || moneyFxPrefab == null)
        {
            return null;
        }

        GameObject textObject = Instantiate(moneyFxPrefab, rootCanvasRect);
        textObject.name = "MoneyFxText";
        textObject.layer = rootCanvasRect.gameObject.layer;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        Text text = textObject.GetComponentInChildren<Text>(true);
        if (text == null)
        {
            Destroy(textObject);
            return null;
        }

        moneyFxRootLookup[text] = rectTransform;

        text.font = feedbackFont != null ? feedbackFont : text.font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = content;
        text.color = color;

        return text;
    }

    private RectTransform GetFxRootRect(Text text)
    {
        if (text != null && moneyFxRootLookup.TryGetValue(text, out RectTransform rectTransform) && rectTransform != null)
        {
            return rectTransform;
        }

        return text != null ? text.rectTransform : null;
    }

    private void DestroyFxText(Text text)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rootRect = GetFxRootRect(text);
        moneyFxRootLookup.Remove(text);
        if (rootRect != null)
        {
            Destroy(rootRect.gameObject);
            return;
        }

        Destroy(text.gameObject);
    }

    private Vector2 GetMoneyTargetLocalPosition(int playerIndex)
    {
        if (playerItems.Count == 0)
        {
            InitPlayerHud();
        }

        if (playerIndex >= 0 && playerIndex < playerItems.Count)
        {
            PlayerInfoItem item = playerItems[playerIndex];
            if (item != null && item.moneyText != null)
            {
                return GetRectCenterInCanvas(item.moneyText.rectTransform);
            }
        }

        return Vector2.zero;
    }

    private Vector2 WorldToCanvasLocalPoint(Vector3 worldPosition)
    {
        CacheCanvasRefs();
        if (rootCanvasRect == null)
        {
            return Vector2.zero;
        }

        Camera renderCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? (rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main)
            : null;

        Camera worldCamera = Camera.main != null ? Camera.main : renderCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootCanvasRect, screenPoint, renderCamera, out Vector2 localPoint);
        return localPoint;
    }

    private Vector2 GetRectCenterInCanvas(RectTransform target)
    {
        CacheCanvasRefs();
        if (rootCanvasRect == null || target == null)
        {
            return Vector2.zero;
        }

        Vector3 worldCenter = target.TransformPoint(target.rect.center);
        Camera renderCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(renderCamera, worldCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootCanvasRect, screenPoint, renderCamera, out Vector2 localPoint);
        return localPoint;
    }

    private string FormatSignedMoney(int delta)
    {
        return delta > 0 ? $"+{delta}" : delta.ToString();
    }

    private void AddLogLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        logLines.Add(message.Trim());
        while (logLines.Count > maxLogLines)
        {
            logLines.RemoveAt(0);
        }

        if (logText != null)
        {
            logText.text = string.Join("\n", logLines.ToArray());
        }
    }

    private string GetPlayerName(int playerIndex)
    {
        if (gameManager == null || gameManager.playerManager == null)
        {
            return $"\u73a9\u5bb6{playerIndex + 1}";
        }

        return gameManager.playerManager.GetPlayerDisplayName(playerIndex);
    }

    private string GetPlayerHudRoleName(int playerIndex)
    {
        if (gameManager == null || gameManager.playerManager == null)
        {
            return $"\u73a9\u5bb6{playerIndex + 1}";
        }

        string roleName = gameManager.playerManager.GetPlayerRoleDisplayName(playerIndex);
        return string.IsNullOrEmpty(roleName) ? GetPlayerName(playerIndex) : roleName;
    }

    private string GetPlayerControlTag(int playerIndex)
    {
        if (gameManager == null || gameManager.playerManager == null)
        {
            return $"P{playerIndex + 1}";
        }

        return gameManager.playerManager.GetPlayerLabel(playerIndex);
    }

    private Color GetPlayerAccent(int playerIndex)
    {
        if (gameManager == null || gameManager.playerManager == null)
        {
            return new Color(0.20f, 0.63f, 0.72f);
        }

        switch (gameManager.playerManager.GetPlayerRoleId(playerIndex))
        {
            case RoleId.Duck:
                return new Color(0.86f, 0.57f, 0.20f);
            case RoleId.Rabbit:
                return new Color(0.25f, 0.54f, 0.88f);
            case RoleId.Panda:
                return new Color(0.23f, 0.69f, 0.38f);
            case RoleId.Dog:
                return new Color(0.63f, 0.33f, 0.79f);
            default:
                return new Color(0.20f, 0.63f, 0.72f);
        }
    }

    private Sprite GetPlayerAvatar(int playerIndex)
    {
        if (gameManager == null || gameManager.playerManager == null)
        {
            return null;
        }

        RoleId roleId = gameManager.playerManager.GetPlayerRoleId(playerIndex);
        if (roleAvatarSprites.TryGetValue(roleId, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite sprite = LoadRoleAvatarSprite(roleId);
        roleAvatarSprites[roleId] = sprite;
        return sprite;
    }

    private Sprite LoadRoleAvatarSprite(RoleId roleId)
    {
        RoleDefinition role = GameRoleCatalog.Get(roleId);
        if (role != null && !string.IsNullOrEmpty(role.portraitResourceName))
        {
            Sprite sheetSprite = LoadRoleAvatarFromSheet(role.portraitResourceName);
            if (sheetSprite != null)
            {
                return sheetSprite;
            }
        }

        string textureName = GetRoleAvatarTextureName(roleId);
        Texture2D texture = string.IsNullOrEmpty(textureName)
            ? null
            : Resources.Load<Texture2D>($"RolePortraits/{textureName}");
        if (texture == null && role != null && !string.IsNullOrEmpty(role.portraitResourceName))
        {
            texture = Resources.Load<Texture2D>($"RolePortraits/{role.portraitResourceName}");
        }

        return texture != null
            ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f))
            : null;
    }

    private Sprite LoadRoleAvatarFromSheet(string spriteName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("RolePortraits/role");
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name == spriteName)
            {
                return sprites[i];
            }
        }

        return null;
    }

    private string GetRoleAvatarTextureName(RoleId roleId)
    {
        switch (roleId)
        {
            case RoleId.Duck:
                return "duck";
            case RoleId.Rabbit:
                return "rabbit";
            case RoleId.Panda:
                return "panda";
            case RoleId.Dog:
                return "dog";
            default:
                return string.Empty;
        }
    }

    private string GetStatusLabel(Status statusValue)
    {
        switch (statusValue)
        {
            case Status.PlayerMove:
                return "\u7b49\u5f85\u73a9\u5bb6\u63b7\u9ab0";
            case Status.PlayerAction:
                return "\u7b49\u5f85\u73a9\u5bb6\u64cd\u4f5c";
            case Status.AIMove:
                return "AI \u6b63\u5728\u79fb\u52a8";
            case Status.AIAction:
                return "AI \u6b63\u5728\u64cd\u4f5c";
            case Status.Resolving:
                return "\u6b63\u5728\u7ed3\u7b97";
            case Status.GameOver:
                return "\u6e38\u620f\u7ed3\u675f";
            default:
                return "\u8fdb\u884c\u4e2d";
        }
    }
}



