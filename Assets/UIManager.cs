using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    public List<PlayerInfoItem> playerInfoItems = new List<PlayerInfoItem>();
    public Text logText;
    public RectTransform logPanel;
    public RectTransform handPanel;
    public RectTransform handBodyRoot;
    public Text handTitleText;
    public List<Button> handCardButtons = new List<Button>();
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
    private readonly List<Button> handCardUseButtons = new List<Button>();
    private readonly List<Image> handCardSelectionFrames = new List<Image>();
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

        if (!runtimeReady || gameManager == null)
        {
            return;
        }

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

        noticeOverlay.gameObject.SetActive(true);
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

        CacheSceneCollections();
        CacheCanvasRefs();
        BindSceneDialogs();
        BindRuntimeCallbacks();
        InitPlayerHud();

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
            rootCanvas = FindObjectOfType<Canvas>();
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

        if ((handCardButtons == null || handCardButtons.Count == 0) && handBodyRoot != null)
        {
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

        if (actionPanel != null)
        {
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
    }

    private void CacheHandCardVisuals()
    {
        handCardTexts.Clear();
        handCardUseButtons.Clear();
        handCardSelectionFrames.Clear();

        if (handCardButtons == null)
        {
            return;
        }

        for (int i = 0; i < handCardButtons.Count; i++)
        {
            Button cardButton = handCardButtons[i];
            Transform cardRoot = cardButton != null ? cardButton.transform : null;

            Text cardText = null;
            Button useButton = null;
            Image selectionFrame = null;

            if (cardRoot != null)
            {
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
            handCardUseButtons.Add(useButton);
            handCardSelectionFrames.Add(selectionFrame);
        }
    }

    private void InitPlayerHud()
    {
        playerItems.Clear();

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
        string currentName = gameManager.currentPlayerIndex >= 0
            ? GetPlayerName(gameManager.currentPlayerIndex)
            : "\u7b49\u5f85\u5f00\u59cb";
        if (turnText != null)
        {
            turnText.text = $"\u5f53\u524d\u89d2\u8272\uff1a{currentName}";
        }
        if (diceText != null)
        {
            diceText.text = gameManager.lastDiceValue > 0
                ? $"\u4e0a\u6b21\u9ab0\u70b9\uff1a{gameManager.lastDiceValue}"
                : "\u4e0a\u6b21\u9ab0\u70b9\uff1a\u7b49\u5f85\u63b7\u9ab0";
        }
        if (statusText != null)
        {
            statusText.text = $"\u5f53\u524d\u9636\u6bb5\uff1a{GetStatusLabel(gameManager.status)}";
        }
    }

    private void UpdatePropertyInfoPanel()
    {
        propertyInfoVisible = false;

        if (propertyInfoTitleText == null && propertyInfoBodyText == null)
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
            stateText = $"\u505c\u7559\u9700\u4ed8 {tileData.tileIncome}";
        }

        if (propertyInfoTitleText != null)
        {
            propertyInfoTitleText.text = tileData.tileName;
        }

        if (propertyInfoBodyText != null)
        {
            propertyInfoBodyText.text =
                $"\u552e\u4ef7 {tileData.tileCost} / \u8def\u8d39 {tileData.tileIncome}\n" +
                $"\u5f52\u5c5e\uff1a{ownerText}\n" +
                stateText;
        }

        SetPropertyInfoVisible(true);
    }
    private void SetPropertyInfoVisible(bool visible)
    {
        propertyInfoVisible = visible;

        if (propertyInfoTitleText != null)
        {
            propertyInfoTitleText.gameObject.SetActive(visible);
        }

        if (propertyInfoBodyText != null)
        {
            propertyInfoBodyText.gameObject.SetActive(visible);
        }
    }

    private void UpdateButtons()
    {
        bool canRoll = gameManager.CanHumanRoll();
        bool canBuy = gameManager.CanHumanBuyCurrentTile();
        bool canEndTurn = gameManager.CanHumanEndTurn();

        UpdateButtonState(MoveButton, canRoll);
        UpdateButtonState(BuyButton, canBuy);
        UpdateButtonState(SkipButton, canEndTurn);

        if (actionPanel != null)
        {
            actionPanel.gameObject.SetActive(canRoll || canBuy || canEndTurn || propertyInfoVisible);
        }

        if (handPanel != null)
        {
            bool visible = gameManager.ShouldShowHumanHand();
            handPanel.gameObject.SetActive(visible);
            if (!visible)
            {
                handSignature = string.Empty;
                selectedHandCardIndex = -1;
            }
        }
    }

    private void UpdatePlayerHud()
    {
        if (playerItems.Count == 0)
        {
            InitPlayerHud();
        }
        int playerCount = Mathf.Min(playerItems.Count, gameManager.totalPlayers);
        for (int i = 0; i < playerItems.Count; i++)
        {
            if (playerItems[i] != null)
            {
                playerItems[i].gameObject.SetActive(i < playerCount);
            }
        }
        for (int i = 0; i < playerCount; i++)
        {
            PlayerInfoItem item = playerItems[i];
            if (item == null)
            {
                continue;
            }
            item.ApplyRuntimeStyle(null, GetPlayerAccent(i));
            item.SetName(GetPlayerName(i));
            item.SetControlTag(gameManager.IsAIPlayer(i) ? "AI" : "\u73a9\u5bb6");
            item.SetMoney(GetDisplayedMoneyValue(i, gameManager.GetMoney(i)));
            item.SetOwnedPropertySummary(gameManager.GetOwnedPropertySummary(i));
            item.SetAlive(gameManager.IsPlayerAlive(i));
            item.SetHighlight(i == gameManager.currentPlayerIndex && gameManager.status != Status.GameOver);
        }
    }

    private void UpdateHandPanel()
    {
        if (handPanel == null || handTitleText == null || !gameManager.ShouldShowHumanHand())
        {
            return;
        }

        List<CardData> cards = gameManager.GetPlayerToolCards(gameManager.currentPlayerIndex);
        bool canUseCards = gameManager.CanHumanUseToolCards(gameManager.currentPlayerIndex);
        if (selectedHandCardIndex >= cards.Count || !canUseCards)
        {
            selectedHandCardIndex = -1;
        }

        string nextSignature = gameManager.currentPlayerIndex + ":" + cards.Count + ":" + canUseCards + ":" + selectedHandCardIndex;
        for (int i = 0; i < cards.Count; i++)
        {
            nextSignature += "|" + cards[i].id;
        }

        if (handSignature == nextSignature)
        {
            return;
        }

        handSignature = nextSignature;
        handTitleText.text = cards.Count > 0 ? $"\u9053\u5177\u624b\u724c \u00b7 {cards.Count}/3" : "\u9053\u5177\u624b\u724c \u00b7 \u6682\u65e0\u9053\u5177";

        for (int i = 0; i < handCardButtons.Count; i++)
        {
            Button cardButton = handCardButtons[i];
            if (cardButton == null)
            {
                continue;
            }

            Text cardText = i < handCardTexts.Count ? handCardTexts[i] : null;
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
                cardButton.interactable = canUseCards;
                if (cardText != null)
                {
                    cardText.text = $"{cards[i].cardName}\n{cards[i].description}";
                }

                bool isSelected = canUseCards && selectedHandCardIndex == i;
                if (selectionFrame != null)
                {
                    selectionFrame.gameObject.SetActive(isSelected);
                }

                if (useButton != null)
                {
                    useButton.gameObject.SetActive(isSelected);
                    useButton.interactable = isSelected;
                }

                if (canUseCards)
                {
                    cardButton.onClick.AddListener(() => ToggleHandCardSelection(cardIndex));
                    if (useButton != null)
                    {
                        useButton.onClick.AddListener(() => UseSelectedHandCard(cardIndex));
                    }
                }
            }
            else
            {
                cardButton.gameObject.SetActive(true);
                cardButton.interactable = false;
                if (selectionFrame != null)
                {
                    selectionFrame.gameObject.SetActive(false);
                }

                if (useButton != null)
                {
                    useButton.gameObject.SetActive(false);
                    useButton.interactable = false;
                }

                if (cardText != null)
                {
                    buttonText.text = "\u7a7a\u69fd";
                }
            }
        }
    }
    private void ToggleHandCardSelection(int cardIndex)
    {
        selectedHandCardIndex = selectedHandCardIndex == cardIndex ? -1 : cardIndex;
        handSignature = string.Empty;
        UpdateHandPanel();
    }

    private void UseSelectedHandCard(int cardIndex)
    {
        if (!gameManager.CanHumanUseToolCards(gameManager.currentPlayerIndex))
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

    private void SetDebugLogPanelVisible(bool visible)
    {
        debugLogPanelVisible = visible;
        if (logPanel != null)
        {
            logPanel.gameObject.SetActive(visible);
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



