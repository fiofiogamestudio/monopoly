using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public GameManager gameManager;

    [Header("Top Info")]
    public Text turnText;
    public Text diceText;
    public Text statusText;

    [Header("Buttons")]
    public Button BuyButton;
    public Button MoveButton;
    public Button SkipButton;

    [Header("Player HUD (Top Center)")]
    public RectTransform playerHudRoot;
    public PlayerInfoItem playerInfoPrefab;
    public float itemWidth = 160f;
    public float itemHeight = 40f;
    public float itemSpacing = 12f;
    public float topOffsetY = -20f;

    [Header("Log UI")]
    public Text logText;                 // ✅ 日志输出文本
    public int maxLogLines = 10;         // ✅ 只显示最近10条
    public bool hookUnityLogs = true;    // ✅ 自动接管 Debug.Log

    private readonly List<PlayerInfoItem> _playerItems = new List<PlayerInfoItem>();
    private bool _inited;

    // 缓存日志（index=0 最新）
    private readonly List<string> _logLines = new List<string>();

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        BuyButton.onClick.AddListener(() => gameManager.RequestBuyOrUpgrade());
        MoveButton.onClick.AddListener(() => gameManager.RequestRollDice());
        SkipButton.onClick.AddListener(() => gameManager.RequestEndTurn());

        if (hookUnityLogs)
        {
            Application.logMessageReceived += OnUnityLog;
        }
    }

    private void OnDestroy()
    {
        if (hookUnityLogs)
        {
            Application.logMessageReceived -= OnUnityLog;
        }
    }

    private void Start()
    {
        InitPlayerHud();
        Log("UI Ready");
    }

    private void InitPlayerHud()
    {
        if (_inited) return;
        _inited = true;

        for (int i = 0; i < gameManager.totalPlayers; i++)
        {
            PlayerInfoItem item = Instantiate(playerInfoPrefab, playerHudRoot);

            item.SetName($"玩家{i + 1}");
            item.SetMoney(gameManager.GetMoney(i));
            item.SetHighlight(i == gameManager.currentPlayerIndex);

            _playerItems.Add(item);
        }
    }

    public void FixedUpdate()
    {
        if (gameManager == null) return;

        // 顶部文字
        turnText.text = $"玩家 {gameManager.currentPlayerIndex + 1} 的回合";
        diceText.text = $"上次骰点: {gameManager.lastDiceValue}";
        statusText.text = $"当前状态: {gameManager.status}";

        // 根据状态显示/隐藏按钮
        if (gameManager.status == Status.PlayerMove)
        {
            BuyButton.gameObject.SetActive(false);
            MoveButton.gameObject.SetActive(true);
            SkipButton.gameObject.SetActive(false);
        }
        else if (gameManager.status == Status.PlayerAction)
        {
            // ✅ 用你提取出来的判断（买或升级都显示）
            bool showBuy = gameManager.CanBuyOrUpgradeCurrentTile(gameManager.currentPlayerIndex);
            BuyButton.gameObject.SetActive(showBuy);

            MoveButton.gameObject.SetActive(false);
            SkipButton.gameObject.SetActive(true);
        }
        else
        {
            BuyButton.gameObject.SetActive(false);
            MoveButton.gameObject.SetActive(false);
            SkipButton.gameObject.SetActive(false);
        }

        if (!_inited) InitPlayerHud();

        // 刷新玩家金钱 + 高亮当前玩家
        for (int i = 0; i < _playerItems.Count; i++)
        {
            _playerItems[i].SetMoney(gameManager.GetMoney(i));
            _playerItems[i].SetHighlight(i == gameManager.currentPlayerIndex);
        }
    }

    // =========================
    // Log System
    // =========================

    // 你可以在任何地方调用：FindObjectOfType<UIManager>().Log("xxx");
    public void Log(string msg)
    {
        AddLogLine(msg);
    }

    private void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        // 你可以按需过滤：比如只收 Warning/Error
        // if (type == LogType.Log) return;

        // 只保留一句（不要把整段堆栈塞满UI）
        string prefix = type == LogType.Log ? "" : $"[{type}] ";
        AddLogLine(prefix + condition);
    }

    private void AddLogLine(string line)
    {
        // 最新的插在最前面
        _logLines.Insert(0, line);

        // 只保留最近 maxLogLines 条
        if (_logLines.Count > maxLogLines)
        {
            _logLines.RemoveRange(maxLogLines, _logLines.Count - maxLogLines);
        }

        RefreshLogText();
    }

    private void RefreshLogText()
    {
        if (logText == null) return;

        // 最新在最上：list 本身就是 0 最新，所以直接拼
        // 为了避免频繁GC，你以后可以换 StringBuilder，这里先简单实现
        string s = "";
        for (int i = 0; i < _logLines.Count; i++)
        {
            s += _logLines[i];
            if (i != _logLines.Count - 1) s += "\n";
        }

        logText.text = s;
    }
}
