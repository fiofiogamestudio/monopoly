using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuScene : MonoBehaviour
{
    private const string RoleSheetResourcesPath = "RolePortraits/role";
    private const string RoleSheetAssetPath = "Assets/Art/UI/role.png";
    private const float RoleCardWidth = 268f;
    private const float RoleCardHeight = 520f;
    private const float RoleCardPortraitWidth = 242f;
    private const float RoleCardPortraitHeight = 352f;
    private const float NavButtonWidth = 500f;
    private const float NavButtonHeight = 150f;
    private const float NavButtonHorizontalMargin = 64f;
    private const float NavButtonVerticalMargin = 48f;
    private const float NavButtonLabelYOffset = 17f;
    private const int LevelButtonLabelMaxFontSize = 34;
    private const int LevelButtonLabelMinFontSize = 22;
    private const string GameSceneName = "GameScene";
    private const string RoleTitle = "\u9009\u62e9\u4f60\u7684\u4e3b\u89d2";
    private const string RoleSubtitle = "";
    private static readonly string[] LevelDisplayNames =
    {
        "\u7b2c\u4e00\u5173\n\u670d\u9970",
        "\u7b2c\u4e8c\u5173\n\u7f8e\u98df",
        "\u7b2c\u4e09\u5173\n\u5730\u6807"
    };
    private static readonly Color SelectedBorderColor = new Color(0.99f, 0.95f, 0.74f, 1f);
    private static readonly Color SelectedPortraitFrameColor = new Color(0.98f, 0.96f, 0.86f, 1f);

    [Header("Level Selection")]
    public int selectedLevel = GameSessionConfig.DefaultLevelIndex;

    [Header("Player Count Selection")]
    public int minPlayers = 1;
    public int maxPlayers = 4;
    public int selectedPlayerCount = 1;

    public Button StartButton;

    private RectTransform _canvasRoot;
    private Font _defaultFont;
    private RoleId _selectedRole = RoleId.Duck;
    private GameObject _menuShellInstance;
    private GameObject _mainMenuPanel;
    private GameObject _levelSelectPanel;
    private GameObject _playerCountPanel;
    private GameObject _rolePanel;
    private GameObject _rulePanel;
    private RectTransform _cardsRoot;
    private Button _quitButton;
    private Button _roleBackButton;
    private Button _roleNextButton;
    private Button _ruleBackButton;
    private Button _enterGameButton;
    private Button _levelBackButton;
    private Button _levelNextButton;
    private Button _playerCountBackButton;
    private Button _playerCountNextButton;
    private Button[] _levelButtons;
    private Text[] _levelButtonTexts;
    private Button[] _playerCountButtons;
    private Text[] _playerCountButtonTexts;
    private Text _levelTitleText;
    private Text _levelSubtitleText;
    private Text _playerCountTitleText;
    private Text _playerCountSubtitleText;
    private Text _roleTitleText;
    private Text _roleSubtitleText;
    private GameObject _ruleSummaryFrame;
    private Text _ruleSummaryText;
    private Text _ruleBodyText;
    private string _ruleBodyTemplate;
    private Image _rulePortraitImage;
    private Image _rulePortraitFrameImage;
    private RectTransform _ruleCardAnchor;
    private RoleCardRuntime _rulePreviewCard;
    private GameObject _rulePreviewCardObject;

    private readonly List<RoleCardRuntime> _roleCards = new List<RoleCardRuntime>();
    private readonly Dictionary<string, Sprite> _portraitCache = new Dictionary<string, Sprite>();

    // Multi-player role selection
    private readonly List<RoleId> _playerRoleSelections = new List<RoleId>();
    private readonly List<bool> _playerRoleLocked = new List<bool>();
    private int _currentSlotIndex;
    private Button[] _playerSlotButtons;
    private Text[] _playerSlotLabels;
    private Image[] _playerSlotRoleBadges;
    private RectTransform _playerSlotsRoot;

    private sealed class RoleCardRuntime
    {
        public RoleId roleId;
        public RectTransform rectTransform;
        public Button button;
        public Image rootImage;
        public Outline outline;
        public Image portraitFrame;
        public Text nameText;
        public Text themeText;
        public Text skillTitleText;
        public Text skillText;
        public Image portraitImage;
        public GameObject selectedBadgeRoot;
        public Text selectedText;
        public Vector2 baseAnchoredPosition;
    }

    private struct RolePalette
    {
        public Color card;
        public Color cardSelected;
        public Color button;
        public Color portraitFrame;
    }

    private void Start()
    {
        AudioManager.Instance.PlayBgm(AudioIds.MenuBgm);
        Canvas canvas = FindSceneCanvas();
        _canvasRoot = canvas != null ? canvas.GetComponent<RectTransform>() : transform as RectTransform;
        _defaultFont = LoadMenuFont();

        if (_canvasRoot == null)
        {
            Debug.LogError("[MenuScene] Canvas root not found.");
            return;
        }

        ApplyFontToExistingTexts(_canvasRoot);
        GameSessionConfig.ResetLevelSelection();
        selectedLevel = GameSessionConfig.ResolveLevel(GameSessionConfig.DefaultLevelIndex);
        minPlayers = GameSessionConfig.MinPlayerCount;
        maxPlayers = GameSessionConfig.MaxPlayerCount;
        selectedPlayerCount = Mathf.Clamp(selectedPlayerCount, minPlayers, maxPlayers);
        EnsureMenuShell();
        CacheMenuShellRefs();
        CacheLevelSelectPanelRefs();
        CachePlayerCountPanelRefs();
        CachePlayerSlotStripRefs();
        InitializeRoleSelections();
        BuildRoleCards();
        BindButtons();
        ApplyNavigationButtonLayout();
        ShowMainMenu();
    }

    private void Update()
    {
        AnimateRoleCards();
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

    private void EnsureMenuShell()
    {
        Transform existing = _canvasRoot.Find("MenuShell");
        if (existing != null)
        {
            _menuShellInstance = existing.gameObject;
            return;
        }

        GameObject shellPrefab = Resources.Load<GameObject>("Prefabs/UI/MenuShell");
        if (shellPrefab == null)
        {
            Debug.LogError("[MenuScene] MenuShell prefab not found in Resources/Prefabs/UI.");
            return;
        }

        _menuShellInstance = Instantiate(shellPrefab, _canvasRoot);
        _menuShellInstance.name = shellPrefab.name;
    }

    private void CacheMenuShellRefs()
    {
        if (_menuShellInstance == null)
        {
            return;
        }

        _mainMenuPanel = _menuShellInstance.transform.Find("MainMenuPanel")?.gameObject;
        _levelSelectPanel = _menuShellInstance.transform.Find("LevelSelectPanel")?.gameObject;
        _playerCountPanel = _menuShellInstance.transform.Find("PlayerCountPanel")?.gameObject;
        _rolePanel = _menuShellInstance.transform.Find("RoleSelectPanel")?.gameObject;
        _rulePanel = _menuShellInstance.transform.Find("RulePanel")?.gameObject;
        _cardsRoot = _menuShellInstance.transform.Find("RoleSelectPanel/CardsRoot") as RectTransform;
        StartButton = _menuShellInstance.transform.Find("MainMenuPanel/StartButton")?.GetComponent<Button>();
        _quitButton = _menuShellInstance.transform.Find("MainMenuPanel/QuitButton")?.GetComponent<Button>();
        _roleBackButton = _menuShellInstance.transform.Find("RoleSelectPanel/BackButton")?.GetComponent<Button>();
        _roleNextButton = _menuShellInstance.transform.Find("RoleSelectPanel/NextButton")?.GetComponent<Button>();
        _ruleBackButton = _menuShellInstance.transform.Find("RulePanel/BackButton")?.GetComponent<Button>();
        _enterGameButton = _menuShellInstance.transform.Find("RulePanel/StartGameButton")?.GetComponent<Button>();
        _roleTitleText = _menuShellInstance.transform.Find("RoleSelectPanel/TitleText")?.GetComponent<Text>();
        _roleSubtitleText = _menuShellInstance.transform.Find("RoleSelectPanel/SubtitleText")?.GetComponent<Text>();
        _ruleSummaryFrame = _menuShellInstance.transform.Find("RulePanel/RuleSummaryFrame")?.gameObject;
        _ruleSummaryText = _menuShellInstance.transform.Find("RulePanel/RuleSummaryFrame/RuleSummaryText")?.GetComponent<Text>();
        _ruleBodyText = _menuShellInstance.transform.Find("RulePanel/RuleBodyFrame/RuleBodyText")?.GetComponent<Text>();
        _ruleBodyTemplate = _ruleBodyText != null ? _ruleBodyText.text : string.Empty;
        _ruleCardAnchor = _menuShellInstance.transform.Find("RulePanel/RulePortraitFrame") as RectTransform;
        _rulePortraitFrameImage = _menuShellInstance.transform.Find("RulePanel/RulePortraitFrame")?.GetComponent<Image>();
        _rulePortraitImage = _menuShellInstance.transform.Find("RulePanel/RulePortraitFrame/RulePortraitImage")?.GetComponent<Image>();

        ApplyFontToExistingTexts(_menuShellInstance.transform);
        ApplyButtonStyle(StartButton);
        ApplyButtonStyle(_quitButton);
        ApplyButtonStyle(_roleBackButton);
        ApplyButtonStyle(_roleNextButton);
        ApplyButtonStyle(_ruleBackButton);
        ApplyButtonStyle(_enterGameButton);
        ApplyNavigationButtonLayout();

        if (_roleTitleText != null)
        {
            _roleTitleText.text = RoleTitle;
        }

        if (_roleSubtitleText != null)
        {
            _roleSubtitleText.text = RoleSubtitle;
        }

    }

    private void CacheLevelSelectPanelRefs()
    {
        if (_menuShellInstance == null)
        {
            return;
        }

        Transform panelTf = _menuShellInstance.transform.Find("LevelSelectPanel");
        if (panelTf == null)
        {
            Debug.LogError("[MenuScene] MenuShell prefab is missing LevelSelectPanel. Please add it to the prefab instead of generating it in code.");
            return;
        }

        _levelSelectPanel = panelTf.gameObject;
        _levelTitleText = panelTf.Find("TitleText")?.GetComponent<Text>();
        _levelSubtitleText = panelTf.Find("SubtitleText")?.GetComponent<Text>();
        _levelBackButton = panelTf.Find("BackButton")?.GetComponent<Button>();
        _levelNextButton = panelTf.Find("NextButton")?.GetComponent<Button>();

        int buttonCount = GameSessionConfig.MaxLevelIndex - GameSessionConfig.MinLevelIndex + 1;
        _levelButtons = new Button[buttonCount];
        _levelButtonTexts = new Text[buttonCount];

        Transform buttonsRoot = panelTf.Find("ButtonsRoot");
        if (buttonsRoot == null)
        {
            Debug.LogError("[MenuScene] LevelSelectPanel is missing ButtonsRoot.");
            return;
        }

        for (int level = GameSessionConfig.MinLevelIndex; level <= GameSessionConfig.MaxLevelIndex; level++)
        {
            int index = level - GameSessionConfig.MinLevelIndex;
            Button btn = buttonsRoot.Find($"LevelButton{level}")?.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError($"[MenuScene] ButtonsRoot is missing LevelButton{level}.");
                continue;
            }

            _levelButtons[index] = btn;
            _levelButtonTexts[index] = btn.GetComponentInChildren<Text>();
            if (_levelButtonTexts[index] != null)
            {
                _levelButtonTexts[index].text = GetLevelDisplayName(level);
                ConfigureLevelButtonText(_levelButtonTexts[index]);
            }
        }

        ApplyButtonStyle(_levelBackButton);
        ApplyButtonStyle(_levelNextButton);

        if (_levelTitleText != null)
        {
            _levelTitleText.text = "\u9009\u62e9\u5173\u5361";
        }

        if (_levelSubtitleText != null)
        {
            _levelSubtitleText.text = "\u9009\u62e9\u5173\u5361";
        }

        _levelSelectPanel.SetActive(false);
    }

    private void InitializeRoleSelections()
    {
        _playerRoleSelections.Clear();
        _playerRoleLocked.Clear();
        int roleCount = GameRoleCatalog.AllRoles.Count;
        for (int i = 0; i < maxPlayers; i++)
        {
            _playerRoleSelections.Add(GameRoleCatalog.AllRoles[i % roleCount].roleId);
            _playerRoleLocked.Add(false);
        }
        _currentSlotIndex = 0;
        _selectedRole = _playerRoleSelections[0];
    }

    private void CachePlayerCountPanelRefs()
    {
        if (_menuShellInstance == null)
        {
            return;
        }

        Transform panelTf = _menuShellInstance.transform.Find("PlayerCountPanel");
        if (panelTf == null)
        {
            Debug.LogError("[MenuScene] MenuShell prefab is missing PlayerCountPanel. Please add it to the prefab instead of generating it in code.");
            return;
        }

        _playerCountPanel = panelTf.gameObject;
        _playerCountTitleText = panelTf.Find("TitleText")?.GetComponent<Text>();
        _playerCountSubtitleText = panelTf.Find("SubtitleText")?.GetComponent<Text>();
        _playerCountBackButton = panelTf.Find("BackButton")?.GetComponent<Button>();
        _playerCountNextButton = panelTf.Find("NextButton")?.GetComponent<Button>();

        int buttonCount = Mathf.Max(1, maxPlayers - minPlayers + 1);
        _playerCountButtons = new Button[buttonCount];
        _playerCountButtonTexts = new Text[buttonCount];

        Transform buttonsRoot = panelTf.Find("ButtonsRoot");
        if (buttonsRoot == null)
        {
            Debug.LogError("[MenuScene] PlayerCountPanel is missing ButtonsRoot.");
            return;
        }

        for (int i = minPlayers; i <= maxPlayers; i++)
        {
            int idx = i - minPlayers;
            Button btn = buttonsRoot.Find($"PlayerCountButton{i}")?.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError($"[MenuScene] ButtonsRoot is missing PlayerCountButton{i}.");
                continue;
            }

            _playerCountButtons[idx] = btn;
            _playerCountButtonTexts[idx] = btn.GetComponentInChildren<Text>();
        }

        ApplyButtonStyle(_playerCountBackButton);
        ApplyButtonStyle(_playerCountNextButton);

        if (_playerCountSubtitleText != null)
        {
            _playerCountSubtitleText.text = $"\u6bcf\u4f4d\u73a9\u5bb6\u90fd\u4f1a\u5728\u4e4b\u540e\u5355\u72ec\u9009\u62e9\u89d2\u8272\uff08{minPlayers}-{maxPlayers}\u4eba\uff09";
        }

        _playerCountPanel.SetActive(false);
    }

    private void CachePlayerSlotStripRefs()
    {
        if (_rolePanel == null)
        {
            return;
        }

        _playerSlotsRoot = _rolePanel.transform.Find("PlayerSlotsRoot") as RectTransform;
        if (_playerSlotsRoot == null)
        {
            Debug.LogError("[MenuScene] RoleSelectPanel is missing PlayerSlotsRoot. Please add it to MenuShell prefab.");
            return;
        }

        _playerSlotButtons = new Button[maxPlayers];
        _playerSlotLabels = new Text[maxPlayers];
        _playerSlotRoleBadges = new Image[maxPlayers];

        for (int i = 0; i < maxPlayers; i++)
        {
            Button slotBtn = _playerSlotsRoot.Find($"Slot{i + 1}")?.GetComponent<Button>();
            if (slotBtn == null)
            {
                Debug.LogError($"[MenuScene] PlayerSlotsRoot is missing Slot{i + 1}.");
                continue;
            }

            _playerSlotButtons[i] = slotBtn;
            _playerSlotLabels[i] = slotBtn.GetComponentInChildren<Text>();
            _playerSlotRoleBadges[i] = slotBtn.transform.Find("RoleBadge")?.GetComponent<Image>();
        }
    }
    private void BuildRoleCards()
    {
        _roleCards.Clear();

        if (_cardsRoot == null)
        {
            return;
        }

        int roleCount = GameRoleCatalog.AllRoles.Count;
        if (_cardsRoot.childCount < roleCount)
        {
            Debug.LogError($"[MenuScene] CardsRoot needs {roleCount} role card objects in the MenuShell prefab, but only {_cardsRoot.childCount} were found.");
            return;
        }

        for (int i = 0; i < roleCount; i++)
        {
            RoleDefinition role = GameRoleCatalog.AllRoles[i];
            GameObject cardObject = FindRoleCardObject(role, i);
            if (cardObject == null)
            {
                Debug.LogError($"[MenuScene] CardsRoot is missing card object for {role.roleId}.");
                continue;
            }

            cardObject.name = $"{role.roleId}Card";
            cardObject.SetActive(true);

            RoleCardRuntime runtime = CreateRoleCardRuntime(cardObject, true);
            PopulateRoleCard(runtime, role);

            RoleId roleId = role.roleId;
            if (runtime.button != null)
            {
                BindButtonClick(runtime.button, () => OnRoleCardClicked(roleId));
            }
            _roleCards.Add(runtime);
        }

        RefreshRoleSelection();
    }

    private GameObject FindRoleCardObject(RoleDefinition role, int fallbackIndex)
    {
        if (_cardsRoot == null || role == null)
        {
            return null;
        }

        Transform namedCard = _cardsRoot.Find($"{role.roleId}Card");
        if (namedCard != null)
        {
            return namedCard.gameObject;
        }

        return fallbackIndex >= 0 && fallbackIndex < _cardsRoot.childCount
            ? _cardsRoot.GetChild(fallbackIndex).gameObject
            : null;
    }

    private void BindButtons()
    {
        if (StartButton != null)
        {
            BindButtonClick(StartButton, ShowLevelSelection);
        }

        if (_quitButton != null)
        {
            BindButtonClick(_quitButton, QuitGame);
        }

        if (_roleBackButton != null)
        {
            BindButtonClick(_roleBackButton, ShowPlayerCountSelection);
        }

        if (_roleNextButton != null)
        {
            BindButtonClick(_roleNextButton, OpenRulePanel);
        }

        if (_ruleBackButton != null)
        {
            BindButtonClick(_ruleBackButton, OpenRoleSelect);
        }

        if (_enterGameButton != null)
        {
            BindButtonClick(_enterGameButton, StartGame);
        }

        if (_levelBackButton != null)
        {
            BindButtonClick(_levelBackButton, ShowMainMenu);
        }

        if (_levelNextButton != null)
        {
            BindButtonClick(_levelNextButton, ShowPlayerCountSelection);
        }

        if (_levelButtons != null)
        {
            for (int level = GameSessionConfig.MinLevelIndex; level <= GameSessionConfig.MaxLevelIndex; level++)
            {
                int selected = level;
                int index = level - GameSessionConfig.MinLevelIndex;
                if (index < 0 || index >= _levelButtons.Length || _levelButtons[index] == null)
                {
                    continue;
                }

                BindButtonClick(_levelButtons[index], () => SelectLevel(selected));
            }
        }

        if (_playerCountBackButton != null)
        {
            BindButtonClick(_playerCountBackButton, ShowLevelSelection);
        }

        if (_playerCountNextButton != null)
        {
            BindButtonClick(_playerCountNextButton, OpenRoleSelect);
        }

        if (_playerCountButtons != null)
        {
            for (int i = minPlayers; i <= maxPlayers; i++)
            {
                int count = i;
                int index = i - minPlayers;
                if (index < 0 || index >= _playerCountButtons.Length || _playerCountButtons[index] == null)
                {
                    continue;
                }

                BindButtonClick(_playerCountButtons[index], () => SelectPlayerCount(count));
            }
        }

        if (_playerSlotButtons != null)
        {
            for (int i = 0; i < _playerSlotButtons.Length; i++)
            {
                int slotIndex = i;
                if (_playerSlotButtons[i] == null)
                {
                    continue;
                }

                BindButtonClick(_playerSlotButtons[i], () => OnSlotButtonClicked(slotIndex));
            }
        }
    }

    private void BindButtonClick(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayUi(AudioIds.ButtonClick);
            action?.Invoke();
        });
    }

    private void ShowMainMenu()
    {
        SetActive(_mainMenuPanel, true);
        SetActive(_levelSelectPanel, false);
        SetActive(_playerCountPanel, false);
        SetActive(_rolePanel, false);
        SetActive(_rulePanel, false);
    }

    private void ShowLevelSelection()
    {
        SetActive(_mainMenuPanel, false);
        SetActive(_levelSelectPanel, true);
        SetActive(_playerCountPanel, false);
        SetActive(_rolePanel, false);
        SetActive(_rulePanel, false);
        RefreshLevelSelection();
    }

    private void ShowPlayerCountSelection()
    {
        InitializeRoleSelections();
        SetActive(_mainMenuPanel, false);
        SetActive(_levelSelectPanel, false);
        SetActive(_playerCountPanel, true);
        SetActive(_rolePanel, false);
        SetActive(_rulePanel, false);
        RefreshPlayerCountSelection();
    }

    private void OpenRoleSelect()
    {
        SetActive(_mainMenuPanel, false);
        SetActive(_levelSelectPanel, false);
        SetActive(_playerCountPanel, false);
        SetActive(_rolePanel, true);
        SetActive(_rulePanel, false);
        ApplyNavigationButtonLayout();
        EnsureActiveSlot();
        _selectedRole = _playerRoleSelections[_currentSlotIndex];
        EnsureCurrentSelectionAvailable();
        UpdateRoleSelectionForMultiplePlayers();
        RefreshRoleSelection();
        UpdatePlayerSlotIndicators();
    }

    private void OpenRulePanel()
    {
        SetActive(_mainMenuPanel, false);
        SetActive(_levelSelectPanel, false);
        SetActive(_playerCountPanel, false);
        SetActive(_rolePanel, false);
        SetActive(_rulePanel, true);
        ApplyNavigationButtonLayout();
        int previewSlot = Mathf.Clamp(_currentSlotIndex, 0, Mathf.Max(0, selectedPlayerCount - 1));
        _selectedRole = previewSlot < _playerRoleSelections.Count ? _playerRoleSelections[previewSlot] : RoleId.Duck;
        RefreshRulePanel();
    }

    private void SelectRole(RoleId roleId)
    {
        _selectedRole = roleId;
        RefreshRoleSelection();
    }

    private void OnSlotButtonClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= selectedPlayerCount)
        {
            return;
        }

        _currentSlotIndex = slotIndex;
        _selectedRole = _playerRoleSelections[slotIndex];
        RefreshRoleSelection();
        UpdatePlayerSlotIndicators();
        UpdateRoleSelectionForMultiplePlayers();
    }

    private void SelectLevel(int level)
    {
        selectedLevel = Mathf.Clamp(level, GameSessionConfig.MinLevelIndex, GameSessionConfig.MaxLevelIndex);
        RefreshLevelSelection();
        Debug.Log($"[MenuScene] Selected level: {selectedLevel}");
    }

    private void RefreshLevelSelection()
    {
        if (_levelButtons == null)
        {
            return;
        }

        for (int level = GameSessionConfig.MinLevelIndex; level <= GameSessionConfig.MaxLevelIndex; level++)
        {
            int index = level - GameSessionConfig.MinLevelIndex;
            if (index < _levelButtons.Length && _levelButtons[index] != null)
            {
                bool selected = level == selectedLevel;
                ApplyPlayerCountButtonStyle(_levelButtons[index], selected);
            }
        }
    }

    private static string GetLevelDisplayName(int level)
    {
        int index = level - GameSessionConfig.MinLevelIndex;
        if (index >= 0 && index < LevelDisplayNames.Length)
        {
            return LevelDisplayNames[index];
        }

        return $"\u7b2c{level}\u5173";
    }

    private void ConfigureLevelButtonText(Text text)
    {
        if (text == null)
        {
            return;
        }

        text.font = _defaultFont;
        text.fontSize = LevelButtonLabelMaxFontSize;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = LevelButtonLabelMinFontSize;
        text.resizeTextMaxSize = LevelButtonLabelMaxFontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 0.9f;
    }

    private void SelectPlayerCount(int count)
    {
        selectedPlayerCount = Mathf.Clamp(count, minPlayers, maxPlayers);
        EnsureActiveSlot();
        EnsureCurrentSelectionAvailable();
        RefreshPlayerCountSelection();
        Debug.Log($"[MenuScene] Selected player count: {selectedPlayerCount}");
    }

    private void RefreshPlayerCountSelection()
    {
        if (_playerCountButtons == null)
        {
            return;
        }

        for (int i = minPlayers; i <= maxPlayers; i++)
        {
            int index = i - minPlayers;
            if (index < _playerCountButtons.Length && _playerCountButtons[index] != null)
            {
                bool selected = i == selectedPlayerCount;
                ApplyPlayerCountButtonStyle(_playerCountButtons[index], selected);
            }
        }
    }

    private void UpdateRoleSelectionForMultiplePlayers()
    {
        if (_roleTitleText != null)
        {
            _roleTitleText.text = HasValidRoleSelection()
                ? "\u786e\u8ba4\u89d2\u8272\u5206\u914d"
                : selectedPlayerCount > 1
                ? $"\u4e3a P{_currentSlotIndex + 1} \u9009\u62e9\u89d2\u8272"
                : RoleTitle;
        }

        if (_roleSubtitleText != null)
        {
            _roleSubtitleText.text = string.Empty;
        }
    }

    private void UpdatePlayerSlotIndicators()
    {
        if (_playerSlotButtons == null) return;

        for (int i = 0; i < _playerSlotButtons.Length; i++)
        {
            Button slotButton = _playerSlotButtons[i];
            bool shouldShow = i < selectedPlayerCount;
            if (slotButton != null)
            {
                slotButton.gameObject.SetActive(shouldShow);
            }
            if (!shouldShow) continue;
            if (slotButton == null) continue;

            bool locked = IsSlotLocked(i);
            RoleId roleId = _playerRoleSelections[i];
            RoleDefinition role = GameRoleCatalog.Get(roleId);

            if (_playerSlotLabels[i] != null)
            {
                _playerSlotLabels[i].text = locked ? $"P{i + 1} {role.displayName}" : $"P{i + 1} \u5f85\u9009\u62e9";
            }

            if (_playerSlotRoleBadges[i] != null)
            {
                _playerSlotRoleBadges[i].color = GetRoleColor(roleId);
                _playerSlotRoleBadges[i].gameObject.SetActive(locked);
            }

            Image slotImage = slotButton.GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.color = i == _currentSlotIndex
                    ? new Color(0.99f, 0.85f, 0.35f, 1f)
                    : new Color(0.95f, 0.95f, 0.95f, 1f);
            }
        }

        // set NextButton interactable only when the selection is valid
        if (_roleNextButton != null)
        {
            _roleNextButton.interactable = HasValidRoleSelection();
        }
    }

    private bool HasValidRoleSelection()
    {
        if (_playerRoleSelections.Count < selectedPlayerCount) return false;
        if (_playerRoleLocked.Count < selectedPlayerCount) return false;

        HashSet<RoleId> seen = new HashSet<RoleId>();
        int limit = Mathf.Min(selectedPlayerCount, _playerRoleSelections.Count);
        for (int i = 0; i < limit; i++)
        {
            if (!IsSlotLocked(i)) return false;
            if (!seen.Add(_playerRoleSelections[i])) return false;
        }
        return true;
    }

    private Color GetRoleColor(RoleId roleId)
    {
        switch (roleId)
        {
            case RoleId.Duck: return new Color(1f, 0.8f, 0.2f, 1f); // 黄色
            case RoleId.Rabbit: return new Color(0.4f, 0.8f, 1f, 1f); // 蓝色
            case RoleId.Panda: return new Color(0.95f, 0.95f, 0.95f, 1f); // 白色
            case RoleId.Dog: return new Color(0.9f, 0.6f, 0.3f, 1f); // 橙色
            default: return Color.gray;
        }
    }

    protected void OnRoleCardClicked(RoleId roleId)
    {
        if (_currentSlotIndex < 0 || _currentSlotIndex >= selectedPlayerCount)
        {
            return;
        }

        int lockedSlot = FindLockedSlotWithRole(roleId);
        if (lockedSlot >= 0)
        {
            _currentSlotIndex = lockedSlot;
            _playerRoleLocked[lockedSlot] = false;
            _playerRoleSelections[lockedSlot] = roleId;
            _selectedRole = roleId;
            RefreshRoleSelection();
            UpdatePlayerSlotIndicators();
            UpdateRoleSelectionForMultiplePlayers();
            Debug.Log($"[MenuScene] Slot {lockedSlot + 1} role unlocked: {roleId}");
            return;
        }

        if (IsSlotLocked(_currentSlotIndex))
        {
            return;
        }

        int selectedSlotIndex = _currentSlotIndex;
        _playerRoleSelections[_currentSlotIndex] = roleId;
        _playerRoleLocked[_currentSlotIndex] = true;
        _selectedRole = roleId;
        RefreshRoleSelection();
        UpdatePlayerSlotIndicators();

        int nextSlot = FindNextUnlockedSlot(_currentSlotIndex + 1);
        if (nextSlot >= 0)
        {
            _currentSlotIndex = nextSlot;
            EnsureCurrentSelectionAvailable();
            _selectedRole = _playerRoleSelections[_currentSlotIndex];
            RefreshRoleSelection();
            UpdatePlayerSlotIndicators();
            UpdateRoleSelectionForMultiplePlayers();
        }

        Debug.Log($"[MenuScene] Slot {selectedSlotIndex + 1} role selected: {roleId}");
    }

    private int FindSlotWithRole(RoleId roleId, int excludeSlot)
    {
        int limit = Mathf.Min(selectedPlayerCount, _playerRoleSelections.Count);
        for (int i = 0; i < limit; i++)
        {
            if (i == excludeSlot) continue;
            if (IsSlotLocked(i) && _playerRoleSelections[i] == roleId) return i;
        }
        return -1;
    }

    private int FindLockedSlotWithRole(RoleId roleId)
    {
        return FindSlotWithRole(roleId, -1);
    }

    private bool IsSlotLocked(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < _playerRoleLocked.Count && _playerRoleLocked[slotIndex];
    }

    private bool IsRoleLocked(RoleId roleId)
    {
        int limit = Mathf.Min(selectedPlayerCount, _playerRoleSelections.Count);
        for (int i = 0; i < limit; i++)
        {
            if (IsSlotLocked(i) && _playerRoleSelections[i] == roleId)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsRoleLockedByOtherSlot(RoleId roleId, int excludeSlot)
    {
        return FindSlotWithRole(roleId, excludeSlot) >= 0;
    }

    private int FindNextUnlockedSlot(int startIndex)
    {
        for (int i = Mathf.Max(0, startIndex); i < selectedPlayerCount; i++)
        {
            if (!IsSlotLocked(i))
            {
                return i;
            }
        }

        return -1;
    }

    private void EnsureActiveSlot()
    {
        if (_currentSlotIndex < 0 || _currentSlotIndex >= selectedPlayerCount)
        {
            _currentSlotIndex = Mathf.Max(0, FindNextUnlockedSlot(0));
        }

        if (IsSlotLocked(_currentSlotIndex))
        {
            int unlockedSlot = FindNextUnlockedSlot(0);
            if (unlockedSlot >= 0)
            {
                _currentSlotIndex = unlockedSlot;
            }
        }

        if (_currentSlotIndex < 0 || _currentSlotIndex >= selectedPlayerCount)
        {
            _currentSlotIndex = 0;
        }
    }

    private void EnsureCurrentSelectionAvailable()
    {
        if (_currentSlotIndex < 0 || _currentSlotIndex >= _playerRoleSelections.Count)
        {
            return;
        }

        if (IsSlotLocked(_currentSlotIndex) || !IsRoleLocked(_playerRoleSelections[_currentSlotIndex]))
        {
            return;
        }

        _playerRoleSelections[_currentSlotIndex] = FindFirstUnlockedRole();
        _selectedRole = _playerRoleSelections[_currentSlotIndex];
    }

    private RoleId FindFirstUnlockedRole()
    {
        for (int i = 0; i < GameRoleCatalog.AllRoles.Count; i++)
        {
            RoleId roleId = GameRoleCatalog.AllRoles[i].roleId;
            if (!IsRoleLocked(roleId))
            {
                return roleId;
            }
        }

        return RoleId.Duck;
    }

    private void RefreshRoleSelection()
    {
        bool allLocalPlayersSelected = HasValidRoleSelection();
        for (int i = 0; i < _roleCards.Count; i++)
        {
            RoleCardRuntime card = _roleCards[i];
            RolePalette palette = GetRolePalette(card.roleId);
            bool selected = card.roleId == _selectedRole;
            ApplyRoleCardVisual(card, palette, selected, true);

            int lockedSlot = FindLockedSlotWithRole(card.roleId);
            bool locked = lockedSlot >= 0;
            if (card.rootImage != null)
            {
                Color color = card.rootImage.color;
                color.a = locked && !selected ? 0.55f : 1f;
                card.rootImage.color = color;
            }

            if (card.selectedBadgeRoot != null)
            {
                card.selectedBadgeRoot.SetActive(locked || allLocalPlayersSelected);
            }

            if (card.selectedText != null)
            {
                card.selectedText.text = locked ? $"P{lockedSlot + 1}" : "AI";
            }

            if (card.button != null)
            {
                card.button.interactable = locked || !IsSlotLocked(_currentSlotIndex);
            }
        }
    }

    private void RefreshRulePanel()
    {
        if (_ruleCardAnchor != null)
        {
            _ruleCardAnchor.gameObject.SetActive(false);
        }

        if (_rulePortraitImage != null)
        {
            _rulePortraitImage.gameObject.SetActive(false);
        }

        if (_ruleSummaryFrame != null)
        {
            _ruleSummaryFrame.SetActive(false);
        }

        if (_ruleSummaryText != null)
        {
            _ruleSummaryText.enabled = false;
            _ruleSummaryText.text = string.Empty;
        }

        if (_ruleBodyText != null)
        {
            _ruleBodyText.enabled = true;
            _ruleBodyText.text = BuildRuleBodyText();
        }
    }

    private string BuildRuleBodyText()
    {
        int targetMoney = GameSessionConfig.GetConfiguredTargetMoneyToWin(selectedLevel);
        if (string.IsNullOrEmpty(_ruleBodyTemplate))
        {
            return $"1. \u63b7\u9ab0\u540e\u6309\u70b9\u6570\u79fb\u52a8\uff0c\u843d\u5230\u683c\u5b50\u540e\u7ed3\u7b97\u6548\u679c\u3002\n" +
                   $"2. \u5730\u4ea7\u683c\u53ef\u8d2d\u4e70\uff0c\u5176\u4ed6\u89d2\u8272\u505c\u7559\u652f\u4ed8\u8d39\u7528\u3002\n" +
                   $"3. \u7b54\u9898\u683c\u7b54\u5bf9\u52a0\u94b1\uff0c\u7b54\u9519\u6263\u94b1\u3002\n" +
                   $"4. \u9053\u5177\u5361\u8fdb\u624b\u724c\uff0c\u8fd0\u6c14\u5361\u7acb\u5373\u751f\u6548\u3002\n" +
                   $"5. \u7387\u5148\u83b7\u5f97{targetMoney}\u5609\u79be\u5e01\u80dc\u5229\u3002";
        }

        string targetText = targetMoney.ToString();
        string bodyText = _ruleBodyTemplate
            .Replace("{TARGET_MONEY}", targetText)
            .Replace("\u672c\u5173\u76ee\u6807\u91d1\u989d", targetText);

        return bodyText != _ruleBodyTemplate
            ? bodyText
            : _ruleBodyTemplate.Replace("18000", targetText);
    }

    private void ApplyPlayerCountButtonStyle(Button button, bool selected)
    {
        if (button == null) return;
        
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected ? new Color(0.2f, 0.6f, 0.9f, 1f) : new Color(0.8f, 0.8f, 0.8f, 1f);
        }
        
        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.color = selected ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);
        }
    }

    private void StartGame()
    {
        List<RoleId> selectedRoles = new List<RoleId>();
        for (int i = 0; i < selectedPlayerCount; i++)
        {
            selectedRoles.Add(_playerRoleSelections[i]);
        }
        GameSessionConfig.SetLevel(selectedLevel);
        GameSessionConfig.SetLocalPlayers(selectedPlayerCount, selectedRoles);

        SceneTransitionManager.LoadScene(GameSceneName);
    }

    private void ApplyButtonStyle(Button button)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            button.targetGraphic = image;
        }

        Text labelText = button.GetComponentInChildren<Text>(true);
        if (labelText != null)
        {
            labelText.font = _defaultFont;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        button.colors = colors;
    }

    private void ApplyNavigationButtonLayout()
    {
        ApplyCornerButtonLayout(_levelBackButton, true);
        ApplyCornerButtonLayout(_levelNextButton, false);
        ApplyCornerButtonLayout(_playerCountBackButton, true);
        ApplyCornerButtonLayout(_playerCountNextButton, false);
        ApplyCornerButtonLayout(_roleBackButton, true);
        ApplyCornerButtonLayout(_roleNextButton, false);
        ApplyCornerButtonLayout(_ruleBackButton, true);
        ApplyCornerButtonLayout(_enterGameButton, false);
    }

    private void ApplyCornerButtonLayout(Button button, bool left)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rectTransform = button.transform as RectTransform;
        if (rectTransform != null)
        {
            float anchorX = left ? 0f : 1f;
            rectTransform.anchorMin = new Vector2(anchorX, 0f);
            rectTransform.anchorMax = new Vector2(anchorX, 0f);
            rectTransform.pivot = new Vector2(anchorX, 0f);
            rectTransform.anchoredPosition = new Vector2(left ? NavButtonHorizontalMargin : -NavButtonHorizontalMargin, NavButtonVerticalMargin);
            rectTransform.sizeDelta = new Vector2(NavButtonWidth, NavButtonHeight);
        }

        Text labelText = button.GetComponentInChildren<Text>(true);
        if (labelText != null)
        {
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, NavButtonLabelYOffset);
            labelRect.sizeDelta = new Vector2(-20f, -10f);
            labelText.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void SetActive(GameObject target, bool visible)
    {
        if (target != null)
        {
            target.SetActive(visible);
        }
    }

    private Font LoadMenuFont()
    {
        Font font = Resources.Load<Font>("Fonts/MenuFont");
        if (font != null)
        {
            return font;
        }

        Debug.LogWarning("[MenuScene] MenuFont not found in Resources/Fonts. Falling back to LegacyRuntime.ttf.");
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void ApplyFontToExistingTexts(Transform root)
    {
        if (root == null || _defaultFont == null)
        {
            return;
        }

        Text[] texts = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].font = _defaultFont;
        }
    }

    private RoleCardRuntime CreateRoleCardRuntime(GameObject cardObject, bool interactive)
    {
        RoleCardRuntime runtime = new RoleCardRuntime
        {
            rectTransform = cardObject != null ? cardObject.GetComponent<RectTransform>() : null,
            button = cardObject != null ? cardObject.GetComponent<Button>() : null,
            rootImage = cardObject != null ? cardObject.GetComponent<Image>() : null,
            portraitFrame = FindChildComponent<Image>(cardObject, "PortraitFrame"),
            portraitImage = FindChildComponent<Image>(cardObject, "PortraitImage"),
            nameText = FindChildComponent<Text>(cardObject, "NameText"),
            themeText = FindChildComponent<Text>(cardObject, "ThemeText"),
            skillTitleText = FindChildComponent<Text>(cardObject, "SkillTitleText"),
            skillText = FindChildComponent<Text>(cardObject, "SkillText"),
            selectedText = FindChildComponent<Text>(cardObject, "SelectedText")
        };

        if (cardObject != null)
        {
            CacheSelectedBadge(runtime, cardObject);
            ApplyFontToExistingTexts(cardObject.transform);
            runtime.outline = cardObject.GetComponent<Outline>();

            if (runtime.outline != null)
            {
                runtime.outline.useGraphicAlpha = false;
                runtime.outline.effectDistance = new Vector2(5f, -5f);
                runtime.outline.enabled = false;
            }
        }

        if (runtime.button != null)
        {
            runtime.button.interactable = interactive;
        }

        if (runtime.rectTransform != null)
        {
            runtime.baseAnchoredPosition = runtime.rectTransform.anchoredPosition;
        }

        ApplyRoleCardLayout(runtime);

        return runtime;
    }

    private void CacheSelectedBadge(RoleCardRuntime runtime, GameObject cardObject)
    {
        if (runtime == null || cardObject == null)
        {
            return;
        }

        if (runtime.selectedText != null)
        {
            runtime.selectedBadgeRoot = runtime.selectedText.transform.parent != null
                ? runtime.selectedText.transform.parent.gameObject
                : runtime.selectedText.gameObject;
            return;
        }

        Debug.LogError($"[MenuScene] {cardObject.name} is missing SelectedBadge/SelectedText. Please add it to RoleCard prefab.");
    }

    private T FindChildComponent<T>(GameObject root, string childName) where T : Component
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        Transform directChild = root.transform.Find(childName);
        if (directChild != null && directChild.TryGetComponent(out T directComponent))
        {
            return directComponent;
        }

        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].name == childName)
            {
                return components[i];
            }
        }

        return null;
    }

    private void PopulateRoleCard(RoleCardRuntime runtime, RoleDefinition role)
    {
        if (runtime == null || role == null)
        {
            return;
        }

        runtime.roleId = role.roleId;

        if (runtime.nameText != null)
        {
            runtime.nameText.text = role.displayName;
        }

        if (runtime.themeText != null)
        {
            runtime.themeText.text = role.cultureTheme;
        }

        if (runtime.skillTitleText != null)
        {
            runtime.skillTitleText.text = $"\u6280\u80fd\uff1a{role.skillName}";
        }

        if (runtime.skillText != null)
        {
            runtime.skillText.text = role.skillDescription;
        }

        if (runtime.portraitImage != null)
        {
            runtime.portraitImage.sprite = CreatePortrait(role);
            runtime.portraitImage.color = runtime.portraitImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.12f);
            runtime.portraitImage.preserveAspect = true;
        }
    }

    private void ApplyRoleCardLayout(RoleCardRuntime runtime)
    {
        if (runtime == null)
        {
            return;
        }

        if (runtime.rectTransform != null)
        {
            runtime.rectTransform.sizeDelta = new Vector2(RoleCardWidth, RoleCardHeight);
        }

        SetTextVisible(runtime.nameText, false);
        SetTextVisible(runtime.themeText, false);

        if (runtime.portraitFrame != null)
        {
            runtime.portraitFrame.gameObject.SetActive(true);
            SetTopCenterRect(runtime.portraitFrame.rectTransform, new Vector2(RoleCardPortraitWidth, RoleCardPortraitHeight), 12f);

            if (runtime.portraitImage != null)
            {
                SetStretchRect(runtime.portraitImage.rectTransform);
            }
        }
        else if (runtime.portraitImage != null)
        {
            runtime.portraitImage.gameObject.SetActive(true);
            SetTopCenterRect(runtime.portraitImage.rectTransform, new Vector2(RoleCardPortraitWidth, RoleCardPortraitHeight), 12f);
        }

        if (runtime.selectedBadgeRoot != null)
        {
            RectTransform badgeRect = runtime.selectedBadgeRoot.transform as RectTransform;
            SetTopCenterRect(badgeRect, new Vector2(132f, 42f), 28f);
            runtime.selectedBadgeRoot.SetActive(false);
        }

        if (runtime.selectedText != null)
        {
            if (runtime.selectedBadgeRoot != runtime.selectedText.gameObject)
            {
                SetStretchRect(runtime.selectedText.rectTransform);
            }

            runtime.selectedText.alignment = TextAnchor.MiddleCenter;
            runtime.selectedText.fontStyle = FontStyle.Bold;
            runtime.selectedText.fontSize = 24;
            runtime.selectedText.resizeTextForBestFit = true;
            runtime.selectedText.resizeTextMinSize = 12;
            runtime.selectedText.resizeTextMaxSize = 24;
            runtime.selectedText.horizontalOverflow = HorizontalWrapMode.Overflow;
            runtime.selectedText.verticalOverflow = VerticalWrapMode.Overflow;
            runtime.selectedText.color = Color.white;
        }

        if (runtime.skillTitleText != null)
        {
            runtime.skillTitleText.gameObject.SetActive(true);
            SetTopCenterRect(runtime.skillTitleText.rectTransform, new Vector2(232f, 30f), 376f);
            runtime.skillTitleText.alignment = TextAnchor.MiddleCenter;
            runtime.skillTitleText.resizeTextForBestFit = true;
            runtime.skillTitleText.resizeTextMinSize = 14;
            runtime.skillTitleText.resizeTextMaxSize = 20;
            runtime.skillTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            runtime.skillTitleText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        if (runtime.skillText != null)
        {
            runtime.skillText.gameObject.SetActive(true);
            SetTopCenterRect(runtime.skillText.rectTransform, new Vector2(232f, 88f), 410f);
            runtime.skillText.alignment = TextAnchor.UpperCenter;
            runtime.skillText.resizeTextForBestFit = true;
            runtime.skillText.resizeTextMinSize = 12;
            runtime.skillText.resizeTextMaxSize = 17;
            runtime.skillText.horizontalOverflow = HorizontalWrapMode.Wrap;
            runtime.skillText.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }

    private void SetTextVisible(Text text, bool visible)
    {
        if (text != null)
        {
            text.gameObject.SetActive(visible);
        }
    }

    private static void SetTopCenterRect(RectTransform rectTransform, Vector2 size, float top)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0f, -top);
        rectTransform.sizeDelta = size;
        rectTransform.localScale = Vector3.one;
    }

    private static void SetStretchRect(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private void ApplyRoleCardVisual(RoleCardRuntime card, RolePalette palette, bool selected, bool interactive)
    {
        if (card == null)
        {
            return;
        }

        Color rootColor = palette.card;
        Color titleColor = Color.white;
        Color bodyColor = new Color(0.95f, 0.97f, 1f, 1f);

        if (card.rootImage != null)
        {
            card.rootImage.color = rootColor;
        }

        if (card.outline != null)
        {
            card.outline.enabled = selected;
            card.outline.effectColor = selected
                ? new Color(0.99f, 0.96f, 0.78f, 1f)
                : new Color(0f, 0f, 0f, 0f);
        }

        if (card.portraitFrame != null)
        {
            card.portraitFrame.color = selected ? SelectedPortraitFrameColor : palette.portraitFrame;
        }

        if (card.nameText != null)
        {
            card.nameText.color = titleColor;
        }

        if (card.themeText != null)
        {
            card.themeText.color = bodyColor;
        }

        if (card.skillTitleText != null)
        {
            card.skillTitleText.color = titleColor;
        }

        if (card.skillText != null)
        {
            card.skillText.color = bodyColor;
        }

        if (card.button != null)
        {
            card.button.interactable = interactive;
            card.button.transform.localScale = Vector3.one;

            ColorBlock colors = card.button.colors;
            colors.normalColor = rootColor;
            colors.highlightedColor = Brighten(rootColor, 0.06f);
            colors.pressedColor = Darken(rootColor, 0.10f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = rootColor;
            card.button.colors = colors;
        }
    }

    private void AnimateRoleCards()
    {
        if (_roleCards.Count == 0)
        {
            return;
        }

        bool rolePanelVisible = _rolePanel != null && _rolePanel.activeSelf;
        if (!rolePanelVisible || HasValidRoleSelection())
        {
            for (int i = 0; i < _roleCards.Count; i++)
            {
                RoleCardRuntime card = _roleCards[i];
                if (card != null && card.rectTransform != null)
                {
                    card.rectTransform.anchoredPosition = card.baseAnchoredPosition;
                }
            }
            return;
        }

        float time = Time.unscaledTime;

        for (int i = 0; i < _roleCards.Count; i++)
        {
            RoleCardRuntime card = _roleCards[i];
            if (card == null || card.rectTransform == null)
            {
                continue;
            }

            bool selected = card.roleId == _selectedRole;
            float offsetY = selected ? Mathf.Sin((time * 2.6f) + (i * 0.2f)) * 7f : 0f;
            card.rectTransform.anchoredPosition = card.baseAnchoredPosition + new Vector2(0f, offsetY);
        }
    }

    private void CacheRuleCardPreview()
    {
        if (_rulePreviewCardObject != null || _ruleCardAnchor == null)
        {
            return;
        }

        Transform preview = _ruleCardAnchor.Find("RulePreviewCard");
        if (preview == null)
        {
            Debug.LogError("[MenuScene] RulePortraitFrame is missing RulePreviewCard. Please add a RoleCard object to MenuShell prefab.");
            return;
        }

        _rulePreviewCardObject = preview.gameObject;
        _rulePreviewCard = CreateRoleCardRuntime(_rulePreviewCardObject, false);
    }

    private RolePalette GetRolePalette(RoleId roleId)
    {
        switch (roleId)
        {
            case RoleId.Duck:
                return new RolePalette
                {
                    card = new Color(0.23f, 0.63f, 0.26f, 0.98f),
                    cardSelected = SelectedBorderColor,
                    button = new Color(0.38f, 0.82f, 0.24f, 1f),
                    portraitFrame = new Color(0.94f, 0.98f, 0.88f, 0.98f)
                };
            case RoleId.Rabbit:
                return new RolePalette
                {
                    card = new Color(0.21f, 0.55f, 0.82f, 0.98f),
                    cardSelected = SelectedBorderColor,
                    button = new Color(0.30f, 0.66f, 0.95f, 1f),
                    portraitFrame = new Color(0.88f, 0.95f, 1f, 0.98f)
                };
            case RoleId.Panda:
                return new RolePalette
                {
                    card = new Color(0.86f, 0.48f, 0.20f, 0.98f),
                    cardSelected = SelectedBorderColor,
                    button = new Color(0.98f, 0.76f, 0.26f, 1f),
                    portraitFrame = new Color(1f, 0.95f, 0.84f, 0.98f)
                };
            default:
                return new RolePalette
                {
                    card = new Color(0.56f, 0.35f, 0.83f, 0.98f),
                    cardSelected = SelectedBorderColor,
                    button = new Color(0.73f, 0.52f, 0.93f, 1f),
                    portraitFrame = new Color(0.94f, 0.88f, 1f, 0.98f)
                };
        }
    }

    private Color Brighten(Color color, float amount)
    {
        return new Color(
            Mathf.Clamp01(color.r + amount),
            Mathf.Clamp01(color.g + amount),
            Mathf.Clamp01(color.b + amount),
            color.a);
    }

    private Color Darken(Color color, float amount)
    {
        return new Color(
            Mathf.Clamp01(color.r - amount),
            Mathf.Clamp01(color.g - amount),
            Mathf.Clamp01(color.b - amount),
            color.a);
    }

    private Sprite CreatePortrait(RoleDefinition role)
    {
        if (role == null || string.IsNullOrEmpty(role.portraitResourceName))
        {
            return null;
        }

        if (_portraitCache.TryGetValue(role.portraitResourceName, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite sprite = LoadRoleSprite(role.portraitResourceName);
        if (sprite != null)
        {
            _portraitCache[role.portraitResourceName] = sprite;
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>($"RolePortraits/{role.portraitResourceName}");
        if (texture == null)
        {
            Debug.LogWarning($"[MenuScene] Missing portrait texture for {role.displayName}");
            return null;
        }

        sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        _portraitCache[role.portraitResourceName] = sprite;
        return sprite;
    }

    private Sprite LoadRoleSprite(string spriteName)
    {
        Sprite sprite = LoadRoleSpriteFromResources(spriteName);
        if (sprite != null)
        {
            return sprite;
        }

#if UNITY_EDITOR
        sprite = LoadRoleSpriteFromAssetDatabase(spriteName);
        if (sprite != null)
        {
            return sprite;
        }
#endif

        return null;
    }

    private Sprite LoadRoleSpriteFromResources(string spriteName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(RoleSheetResourcesPath);
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name == spriteName)
            {
                return sprites[i];
            }
        }

        return CreateRoleSpriteFromResourceSheet(spriteName);
    }

    private Sprite CreateRoleSpriteFromResourceSheet(string spriteName)
    {
        if (!TryGetRoleSpriteRect(spriteName, out Rect spriteRect))
        {
            return null;
        }

        Texture2D texture = Resources.Load<Texture2D>(RoleSheetResourcesPath);
        if (texture == null)
        {
            return null;
        }

        if (spriteRect.xMin < 0f || spriteRect.yMin < 0f || spriteRect.xMax > texture.width || spriteRect.yMax > texture.height)
        {
            Debug.LogWarning($"[MenuScene] Role sprite rect is outside texture bounds: {spriteName}");
            return null;
        }

        Sprite sprite = Sprite.Create(texture, spriteRect, new Vector2(0.5f, 0.5f), 100f);
        sprite.name = spriteName;
        return sprite;
    }

    private bool TryGetRoleSpriteRect(string spriteName, out Rect spriteRect)
    {
        switch (spriteName)
        {
            case "role_0":
                spriteRect = new Rect(14f, 962f, 242f, 352f);
                return true;
            case "role_1":
                spriteRect = new Rect(266f, 962f, 243f, 352f);
                return true;
            case "role_2":
                spriteRect = new Rect(523f, 962f, 233f, 351f);
                return true;
            case "role_3":
                spriteRect = new Rect(767f, 960f, 241f, 354f);
                return true;
            default:
                spriteRect = new Rect();
                return false;
        }
    }

#if UNITY_EDITOR
    private Sprite LoadRoleSpriteFromAssetDatabase(string spriteName)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(RoleSheetAssetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite sprite = assets[i] as Sprite;
            if (sprite != null && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        return null;
    }
#endif

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
