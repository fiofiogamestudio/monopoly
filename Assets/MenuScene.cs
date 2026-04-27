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
    private const string RoleTitle = "\u9009\u62e9\u4f60\u7684\u4e3b\u89d2";
    private const string RoleSubtitle = "\u70b9\u51fb\u4e0b\u65b9\u89d2\u8272\u5361\u9009\u62e9\u4e3b\u89d2\uff0c\u518d\u8fdb\u5165\u89c4\u5219\u8bf4\u660e\u3002";
    private const string RuleTitle = "\u5f00\u59cb\u524d\u8bf4\u660e";
    private static readonly Color SelectedBorderColor = new Color(0.99f, 0.95f, 0.74f, 1f);
    private static readonly Color SelectedPortraitFrameColor = new Color(0.98f, 0.96f, 0.86f, 1f);

    public Button StartButton;

    private RectTransform _canvasRoot;
    private Font _defaultFont;
    private RoleId _selectedRole = RoleId.Duck;
    private GameObject _menuShellInstance;
    private GameObject _roleCardPrefab;
    private GameObject _mainMenuPanel;
    private GameObject _rolePanel;
    private GameObject _rulePanel;
    private RectTransform _cardsRoot;
    private Button _quitButton;
    private Button _roleBackButton;
    private Button _roleNextButton;
    private Button _ruleBackButton;
    private Button _enterGameButton;
    private Text _roleTitleText;
    private Text _roleSubtitleText;
    private Text _ruleTitleText;
    private Text _ruleSummaryText;
    private Text _ruleBodyText;
    private Image _rulePortraitImage;
    private Image _rulePortraitFrameImage;
    private RectTransform _ruleCardAnchor;
    private RoleCardRuntime _rulePreviewCard;
    private GameObject _rulePreviewCardObject;

    private readonly List<RoleCardRuntime> _roleCards = new List<RoleCardRuntime>();
    private readonly Dictionary<string, Sprite> _portraitCache = new Dictionary<string, Sprite>();

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
        Canvas canvas = FindObjectOfType<Canvas>();
        _canvasRoot = canvas != null ? canvas.GetComponent<RectTransform>() : transform as RectTransform;
        _defaultFont = LoadMenuFont();

        if (_canvasRoot == null)
        {
            Debug.LogError("[MenuScene] Canvas root not found.");
            return;
        }

        ApplyFontToExistingTexts(_canvasRoot);
        _roleCardPrefab = Resources.Load<GameObject>("Prefabs/UI/RoleCard");
        EnsureMenuShell();
        CacheMenuShellRefs();
        BuildRoleCards();
        BindButtons();
        ShowMainMenu();
    }

    private void Update()
    {
        AnimateRoleCards();
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
        _ruleTitleText = _menuShellInstance.transform.Find("RulePanel/TitleText")?.GetComponent<Text>();
        _ruleSummaryText = _menuShellInstance.transform.Find("RulePanel/RuleSummaryFrame/RuleSummaryText")?.GetComponent<Text>();
        _ruleBodyText = _menuShellInstance.transform.Find("RulePanel/RuleBodyFrame/RuleBodyText")?.GetComponent<Text>();
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

        if (_ruleTitleText != null)
        {
            _ruleTitleText.text = RuleTitle;
        }

        if (_ruleBodyText != null)
        {
            _ruleBodyText.text = BuildRuleDescription();
        }

        EnsureRuleCardPreview();
    }

    private void BuildRoleCards()
    {
        _roleCards.Clear();

        if (_cardsRoot == null || _roleCardPrefab == null)
        {
            return;
        }

        for (int i = _cardsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = _cardsRoot.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        for (int i = 0; i < GameRoleCatalog.AllRoles.Count; i++)
        {
            RoleDefinition role = GameRoleCatalog.AllRoles[i];
            GameObject cardObject = Instantiate(_roleCardPrefab, _cardsRoot);
            cardObject.name = $"{role.displayName}Card";

            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.sizeDelta = new Vector2(RoleCardWidth, RoleCardHeight);
                float cardWidth = RoleCardWidth;
                float gap = 28f;
                float totalWidth = (cardWidth * GameRoleCatalog.AllRoles.Count) + (gap * (GameRoleCatalog.AllRoles.Count - 1));
                float startX = (-totalWidth * 0.5f) + (cardWidth * 0.5f);

                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + gap), -6f);
            }

            RoleCardRuntime runtime = CreateRoleCardRuntime(cardObject, true);
            PopulateRoleCard(runtime, role);

            RoleId roleId = role.roleId;
            runtime.button.onClick.RemoveAllListeners();
            runtime.button.onClick.AddListener(() => SelectRole(roleId));
            _roleCards.Add(runtime);
        }

        RefreshRoleSelection();
    }

    private void BindButtons()
    {
        if (StartButton != null)
        {
            StartButton.onClick.RemoveAllListeners();
            StartButton.onClick.AddListener(OpenRoleSelect);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveAllListeners();
            _quitButton.onClick.AddListener(QuitGame);
        }

        if (_roleBackButton != null)
        {
            _roleBackButton.onClick.RemoveAllListeners();
            _roleBackButton.onClick.AddListener(ShowMainMenu);
        }

        if (_roleNextButton != null)
        {
            _roleNextButton.onClick.RemoveAllListeners();
            _roleNextButton.onClick.AddListener(OpenRulePanel);
        }

        if (_ruleBackButton != null)
        {
            _ruleBackButton.onClick.RemoveAllListeners();
            _ruleBackButton.onClick.AddListener(OpenRoleSelect);
        }

        if (_enterGameButton != null)
        {
            _enterGameButton.onClick.RemoveAllListeners();
            _enterGameButton.onClick.AddListener(StartGame);
        }
    }

    private void ShowMainMenu()
    {
        SetActive(_mainMenuPanel, true);
        SetActive(_rolePanel, false);
        SetActive(_rulePanel, false);
    }

    private void OpenRoleSelect()
    {
        SetActive(_mainMenuPanel, false);
        SetActive(_rolePanel, true);
        SetActive(_rulePanel, false);
        ApplyNavigationButtonLayout();
        RefreshRoleSelection();
    }

    private void OpenRulePanel()
    {
        SetActive(_mainMenuPanel, false);
        SetActive(_rolePanel, false);
        SetActive(_rulePanel, true);
        ApplyNavigationButtonLayout();
        RefreshRulePanel();
    }

    private void SelectRole(RoleId roleId)
    {
        _selectedRole = roleId;
        RefreshRoleSelection();
    }

    private void RefreshRoleSelection()
    {
        for (int i = 0; i < _roleCards.Count; i++)
        {
            RoleCardRuntime card = _roleCards[i];
            RolePalette palette = GetRolePalette(card.roleId);
            bool selected = card.roleId == _selectedRole;
            ApplyRoleCardVisual(card, palette, selected, true);
        }
    }

    private void RefreshRulePanel()
    {
        RoleDefinition role = GameRoleCatalog.Get(_selectedRole);
        RolePalette palette = GetRolePalette(role.roleId);

        EnsureRuleCardPreview();
        if (_rulePreviewCard != null)
        {
            PopulateRoleCard(_rulePreviewCard, role);
            ApplyRoleCardVisual(_rulePreviewCard, palette, true, false);
        }

        if (_rulePortraitImage != null)
        {
            _rulePortraitImage.gameObject.SetActive(false);
        }

        if (_rulePortraitFrameImage != null)
        {
            Color frameColor = palette.portraitFrame;
            frameColor.a = 0.14f;
            _rulePortraitFrameImage.color = frameColor;
        }

        if (_ruleSummaryText != null)
        {
            _ruleSummaryText.text =
                $"\u672c\u5c40\u4e3b\u89d2\uff1a{role.displayName}\n" +
                $"\u6587\u5316\u4e3b\u9898\uff1a{role.cultureTheme}\n" +
                $"\u88ab\u52a8\u6280\u80fd\uff1a{role.skillName}\n" +
                $"{role.skillDescription}\n" +
                "\u5176\u4f59 3 \u4f4d\u89d2\u8272\u7531 AI \u64cd\u63a7\u3002";
            _ruleSummaryText.color = new Color(0.98f, 0.97f, 0.92f, 1f);
        }
    }

    private string BuildRuleDescription()
    {
        return "\u89c4\u5219\u901f\u89c8\n" +
               "1. \u6bcf\u56de\u5408\uff1a\u63b7\u9ab0 -> \u79fb\u52a8 -> \u7ed3\u7b97\u683c\u5b50\u3002\n" +
               "2. \u5730\u4ea7\u683c\u53ef\u8d2d\u4e70\uff0c\u522b\u4eba\u505c\u4e0a\u8981\u4ed8\u8fc7\u8def\u8d39\u3002\n" +
               "3. \u7b54\u9898\u683c\u7b54\u5bf9\u52a0\u94b1\uff0c\u7b54\u9519\u6263\u94b1\uff1b\u9053\u5177\u5361\u6536\u5165\u624b\u724c\uff0c\u8fd0\u6c14\u5361\u7acb\u5373\u751f\u6548\u3002\n" +
               "4. \u8d44\u91d1\u5f52\u96f6\u4f1a\u88ab\u6dd8\u6c70\uff1b\u5148\u8fbe\u5230 30000 \u5609\u79be\u5e01\uff0c\u6216\u6210\u4e3a\u6700\u540e\u7559\u5728\u573a\u4e0a\u7684\u89d2\u8272\uff0c\u5373\u53ef\u83b7\u80dc\u3002";
    }

    private void StartGame()
    {
        GameSessionConfig.SetHumanRole(_selectedRole);
        SceneManager.LoadScene("GameScene");
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
            skillText = FindChildComponent<Text>(cardObject, "SkillText")
        };

        if (cardObject != null)
        {
            ApplyFontToExistingTexts(cardObject.transform);
            runtime.outline = cardObject.GetComponent<Outline>();
            if (runtime.outline == null)
            {
                runtime.outline = cardObject.AddComponent<Outline>();
            }

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
        float time = Time.unscaledTime;

        for (int i = 0; i < _roleCards.Count; i++)
        {
            RoleCardRuntime card = _roleCards[i];
            if (card == null || card.rectTransform == null)
            {
                continue;
            }

            bool selected = rolePanelVisible && card.roleId == _selectedRole;
            float offsetY = selected ? Mathf.Sin((time * 2.6f) + (i * 0.2f)) * 7f : 0f;
            card.rectTransform.anchoredPosition = card.baseAnchoredPosition + new Vector2(0f, offsetY);
        }
    }

    private void EnsureRuleCardPreview()
    {
        if (_rulePreviewCardObject != null || _ruleCardAnchor == null || _roleCardPrefab == null)
        {
            return;
        }

        _rulePreviewCardObject = Instantiate(_roleCardPrefab, _ruleCardAnchor);
        _rulePreviewCardObject.name = "RulePreviewCard";

        RectTransform rect = _rulePreviewCardObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = new Vector3(0.98f, 0.98f, 1f);
        }

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
