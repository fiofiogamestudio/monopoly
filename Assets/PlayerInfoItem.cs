using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoItem : MonoBehaviour
{
    public Text nameText;
    public Text moneyText;
    public Image backgroundImage;
    public RectTransform propertyCardRoot;
    public Image propertyCardBackground;
    public Text propertyCardText;

    private bool _alive = true;
    private bool _highlighted;
    private string _playerName = "";
    private string _controlTag = "";
    private string _ownedSummary = "\u623f\u4ea7\uff1a\u65e0";
    private int _money;
    private Color _accentColor = new Color(0.20f, 0.63f, 0.72f, 1f);

    private void Awake()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponentInChildren<Image>();
        }

        if (nameText != null)
        {
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 14;
            nameText.resizeTextMaxSize = 24;
            nameText.fontStyle = FontStyle.Bold;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        if (moneyText != null)
        {
            moneyText.resizeTextForBestFit = true;
            moneyText.resizeTextMinSize = 10;
            moneyText.resizeTextMaxSize = 18;
            moneyText.fontStyle = FontStyle.Normal;
            moneyText.alignment = TextAnchor.UpperLeft;
            moneyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            moneyText.verticalOverflow = VerticalWrapMode.Overflow;
            moneyText.lineSpacing = 0.92f;
        }

        if (propertyCardText != null)
        {
            propertyCardText.resizeTextForBestFit = true;
            propertyCardText.resizeTextMinSize = 9;
            propertyCardText.resizeTextMaxSize = 15;
            propertyCardText.fontStyle = FontStyle.Normal;
            propertyCardText.alignment = TextAnchor.MiddleCenter;
            propertyCardText.horizontalOverflow = HorizontalWrapMode.Wrap;
            propertyCardText.verticalOverflow = VerticalWrapMode.Overflow;
            propertyCardText.lineSpacing = 0.92f;
        }
    }

    public void ApplyRuntimeStyle(Font font, Color accentColor)
    {
        _accentColor = accentColor;
        RefreshVisuals();
    }

    public void SetName(string playerName)
    {
        _playerName = playerName;
        RefreshTexts();
    }

    public void SetMoney(int money)
    {
        _money = money;
        RefreshTexts();
    }

    public void SetControlTag(string controlTag)
    {
        _controlTag = controlTag ?? "";
        RefreshTexts();
    }

    public void SetOwnedPropertySummary(string ownedSummary)
    {
        _ownedSummary = string.IsNullOrWhiteSpace(ownedSummary) ? "\u623f\u4ea7\uff1a\u65e0" : ownedSummary;
        RefreshTexts();
    }

    public void SetHighlight(bool highlighted)
    {
        _highlighted = highlighted;
        RefreshVisuals();
    }

    public void SetAlive(bool alive)
    {
        _alive = alive;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        Color highlightBackground = Color.Lerp(_accentColor, new Color(0.18f, 0.12f, 0.08f, 1f), 0.16f);
        Color highlightTextColor = new Color(0.14f, 0.10f, 0.07f, 1f);

        if (nameText != null)
        {
            nameText.color = !_alive
                ? new Color(1f, 0.72f, 0.72f)
                : (_highlighted ? highlightTextColor : Color.white);
        }

        if (moneyText != null)
        {
            moneyText.color = !_alive
                ? new Color(1f, 0.76f, 0.76f)
                : (_highlighted ? highlightTextColor : new Color(0.92f, 0.97f, 1f));
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = !_alive
                ? new Color(0.33f, 0.12f, 0.12f, 0.92f)
                : (_highlighted ? highlightBackground : new Color(0.12f, 0.19f, 0.25f, 0.92f));
        }

        if (propertyCardBackground != null)
        {
            propertyCardBackground.color = !_alive
                ? new Color(0.36f, 0.16f, 0.16f, 0.95f)
                : (_highlighted
                    ? Color.Lerp(_accentColor, Color.white, 0.16f)
                    : Color.Lerp(_accentColor, new Color(0.08f, 0.11f, 0.15f, 1f), 0.58f));
        }

        if (propertyCardText != null)
        {
            propertyCardText.color = !_alive
                ? new Color(1f, 0.84f, 0.84f)
                : (_highlighted ? highlightTextColor : new Color(0.97f, 0.98f, 1f));
        }

        RefreshTexts();
    }

    private void RefreshTexts()
    {
        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(_playerName) ? "\u89d2\u8272" : _playerName;
        }

        if (moneyText == null)
        {
            return;
        }

        if (!_alive)
        {
            moneyText.text = "\u5df2\u51fa\u5c40";
            if (propertyCardText != null)
            {
                propertyCardText.text = "\u623f\u4ea7 \u00b7 \u5df2\u6e05\u7a7a";
            }
            return;
        }

        string summary = NormalizePropertySummary(_ownedSummary);
        string header = string.IsNullOrEmpty(_controlTag)
            ? $"{_money}"
            : $"{_controlTag} \u00b7 {_money}";

        if (_highlighted)
        {
            moneyText.text = $"{header}\n\u5f53\u524d\u884c\u52a8";
        }
        else
        {
            moneyText.text = header;
        }

        if (propertyCardText != null)
        {
            propertyCardText.text = $"\u623f\u4ea7 \u00b7 {summary}";
        }
    }

    private static string NormalizePropertySummary(string ownedSummary)
    {
        if (string.IsNullOrWhiteSpace(ownedSummary))
        {
            return "\u65e0\u623f\u4ea7";
        }

        string summary = ownedSummary.Trim();
        if (summary.StartsWith("\u623f\u4ea7\uff1a"))
        {
            summary = summary.Substring(3).Trim();
        }

        return string.IsNullOrEmpty(summary) ? "\u65e0\u623f\u4ea7" : summary;
    }
}
