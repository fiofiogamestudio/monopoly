using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HandCardView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button rootButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image artBackgroundImage;
    [SerializeField] private Image effectBackgroundImage;
    [SerializeField] private Image cardImage;
    [SerializeField] private Text effectText;
    [SerializeField] private Button useButton;
    [SerializeField] private Image selectionFrame;

    public Button RootButton
    {
        get
        {
            EnsureReferences();
            return rootButton;
        }
    }

    public Image CardImage
    {
        get
        {
            EnsureReferences();
            return cardImage;
        }
    }

    public Text EffectText
    {
        get
        {
            EnsureReferences();
            return effectText;
        }
    }

    public Button UseButton
    {
        get
        {
            EnsureReferences();
            return useButton;
        }
    }

    public Image SelectionFrame
    {
        get
        {
            EnsureReferences();
            return selectionFrame;
        }
    }

    private void Awake()
    {
        EnsureReferences();
        ApplyButtonColors();
        ApplyBackground();
        ApplyTextDefaults();
    }

    private void OnValidate()
    {
        EnsureReferences();
        ApplyButtonColors();
        ApplyBackground();
        ApplyTextDefaults();
    }

    public void EnsureReferences()
    {
        if (rootButton == null)
        {
            rootButton = GetComponent<Button>();
        }

        if (backgroundImage == null)
        {
            Transform backgroundTransform = transform.Find("CardBase");
            if (backgroundTransform == null)
            {
                backgroundTransform = transform.Find("BackgroundImage");
            }

            if (backgroundTransform != null)
            {
                backgroundImage = backgroundTransform.GetComponent<Image>();
            }
        }

        if (cardImage == null)
        {
            Transform imageTransform = transform.Find("CardImage");
            if (imageTransform != null)
            {
                cardImage = imageTransform.GetComponent<Image>();
            }
        }

        if (artBackgroundImage == null)
        {
            Transform artBackTransform = transform.Find("CardArtBack");
            if (artBackTransform != null)
            {
                artBackgroundImage = artBackTransform.GetComponent<Image>();
            }
        }

        if (effectBackgroundImage == null)
        {
            Transform effectBackTransform = transform.Find("EffectBack");
            if (effectBackTransform != null)
            {
                effectBackgroundImage = effectBackTransform.GetComponent<Image>();
            }
        }

        if (effectText == null)
        {
            Transform textTransform = transform.Find("EffectText");
            if (textTransform == null)
            {
                textTransform = transform.Find("CardText");
            }

            if (textTransform != null)
            {
                effectText = textTransform.GetComponent<Text>();
            }
        }

        if (useButton == null)
        {
            Transform useTransform = transform.Find("UseButton");
            if (useTransform != null)
            {
                useButton = useTransform.GetComponent<Button>();
            }
        }

        if (selectionFrame == null)
        {
            Transform frameTransform = transform.Find("SelectionFrame");
            if (frameTransform != null)
            {
                selectionFrame = frameTransform.GetComponent<Image>();
            }
        }
    }

    public void SetCard(CardData card, Sprite sprite, bool canUse, bool selected, Action onSelect, Action onUse)
    {
        EnsureReferences();
        gameObject.SetActive(true);

        if (rootButton != null)
        {
            ApplyButtonColors(rootButton);
            rootButton.onClick.RemoveAllListeners();
            rootButton.interactable = onSelect != null;
            if (onSelect != null)
            {
                rootButton.onClick.AddListener(() => onSelect());
            }
        }

        ApplyBackground();

        bool hasSprite = sprite != null;
        if (cardImage != null)
        {
            cardImage.sprite = sprite;
            cardImage.preserveAspect = true;
            cardImage.enabled = hasSprite;
            cardImage.gameObject.SetActive(hasSprite);
        }

        if (effectText != null)
        {
            effectText.text = card != null ? card.cardName : string.Empty;
            SetTitleTextLayout(!hasSprite);
        }

        if (selectionFrame != null)
        {
            selectionFrame.gameObject.SetActive(selected);
        }

        if (useButton != null)
        {
            ApplyButtonColors(useButton);
            useButton.onClick.RemoveAllListeners();
            useButton.gameObject.SetActive(false);
            useButton.interactable = false;
        }
    }

    public void SetEmpty()
    {
        EnsureReferences();
        gameObject.SetActive(true);

        if (rootButton != null)
        {
            ApplyButtonColors(rootButton);
            rootButton.onClick.RemoveAllListeners();
            rootButton.interactable = false;
        }

        ApplyBackground();

        if (cardImage != null)
        {
            cardImage.sprite = null;
            cardImage.enabled = false;
            cardImage.gameObject.SetActive(false);
        }

        if (effectText != null)
        {
            effectText.text = "\u7a7a\u69fd";
            SetTitleTextLayout(true);
        }

        if (selectionFrame != null)
        {
            selectionFrame.gameObject.SetActive(false);
        }

        if (useButton != null)
        {
            ApplyButtonColors(useButton);
            useButton.onClick.RemoveAllListeners();
            useButton.gameObject.SetActive(false);
            useButton.interactable = false;
        }
    }

    private void ApplyButtonColors()
    {
        ApplyButtonColors(rootButton);
        ApplyButtonColors(useButton);
    }

    private void ApplyButtonColors(Button button)
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.white;
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private void ApplyBackground()
    {
        if (backgroundImage == null)
        {
            backgroundImage = CreateDecorationImage("CardBase");
        }

        if (artBackgroundImage == null)
        {
            artBackgroundImage = CreateDecorationImage("CardArtBack");
        }

        if (effectBackgroundImage == null)
        {
            effectBackgroundImage = CreateDecorationImage("EffectBack");
        }

        ConfigureDecorationImage(backgroundImage, new Color(0.13f, 0.18f, 0.10f, 1f));
        ConfigureDecorationImage(artBackgroundImage, new Color(0.04f, 0.07f, 0.04f, 1f));
        ConfigureDecorationImage(effectBackgroundImage, new Color(0.20f, 0.13f, 0.08f, 0.92f));

        SetRect(backgroundImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        SetRect(artBackgroundImage.rectTransform, new Vector2(0f, 0.30f), Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        SetRect(effectBackgroundImage.rectTransform, Vector2.zero, new Vector2(1f, 0.32f), new Vector2(8f, 6f), new Vector2(-8f, -4f));
        if (cardImage != null)
        {
            SetRect(cardImage.rectTransform, new Vector2(0.14f, 0.36f), new Vector2(0.86f, 0.96f), Vector2.zero, Vector2.zero);
        }

        backgroundImage.transform.SetSiblingIndex(0);
        artBackgroundImage.transform.SetSiblingIndex(1);
        if (cardImage != null)
        {
            cardImage.transform.SetSiblingIndex(2);
        }

        effectBackgroundImage.transform.SetSiblingIndex(3);
        if (effectText != null)
        {
            effectText.transform.SetSiblingIndex(4);
        }

        if (selectionFrame != null)
        {
            selectionFrame.transform.SetAsLastSibling();
        }

        if (useButton != null)
        {
            useButton.transform.SetAsLastSibling();
        }
    }

    private Image CreateDecorationImage(string objectName)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.layer = gameObject.layer;
        imageObject.transform.SetParent(transform, false);
        return imageObject.GetComponent<Image>();
    }

    private void ConfigureDecorationImage(Image image, Color color)
    {
        if (image == null)
        {
            return;
        }

        image.gameObject.SetActive(true);
        image.raycastTarget = false;
        image.color = color;
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.fillCenter = true;
    }

    private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }

    private void ApplyTextDefaults()
    {
        if (effectText == null)
        {
            return;
        }

        effectText.alignment = TextAnchor.MiddleCenter;
        effectText.fontStyle = FontStyle.Bold;
        effectText.resizeTextForBestFit = true;
        effectText.resizeTextMinSize = 10;
        effectText.resizeTextMaxSize = 17;
        effectText.horizontalOverflow = HorizontalWrapMode.Wrap;
        effectText.verticalOverflow = VerticalWrapMode.Truncate;
        effectText.color = Color.white;
    }

    private void SetTitleTextLayout(bool fullCard)
    {
        if (effectText == null)
        {
            return;
        }

        RectTransform rect = effectText.rectTransform;
        if (fullCard)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 10f);
            rect.offsetMax = new Vector2(-10f, -10f);
            effectText.resizeTextMaxSize = 17;
        }
        else
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0.32f);
            rect.offsetMin = new Vector2(9f, 5f);
            rect.offsetMax = new Vector2(-9f, -3f);
            effectText.resizeTextMaxSize = 17;
        }
    }
}
