using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoItem : MonoBehaviour
{
    public Text nameText;
    public Text moneyText;
    public Image backgroundImage;
    public RectTransform avatarMaskRoot;
    public Image avatarImage;
    public RectTransform propertyCardRoot;
    public Image propertyCardBackground;
    public Text propertyCardText;
    public GameObject isPlayingRoot;

    private bool _alive = true;
    private bool _highlighted;
    private string _playerName = "";
    private string _controlTag = "";
    private string _ownedSummary = "\u623f\u4ea7\uff1a\u65e0";
    private int _money;

    private void Awake()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponentInChildren<Image>();
        }

        if (avatarImage == null)
        {
            Transform avatarTransform = transform.Find("AvatarMask/AvatarImage");
            avatarImage = avatarTransform != null ? avatarTransform.GetComponent<Image>() : null;
        }

        if (avatarMaskRoot == null && avatarImage != null && avatarImage.transform.parent != null)
        {
            avatarMaskRoot = avatarImage.transform.parent.GetComponent<RectTransform>();
        }

        if (isPlayingRoot == null)
        {
            Transform isPlayingTransform = transform.Find("IsPlaying");
            isPlayingRoot = isPlayingTransform != null ? isPlayingTransform.gameObject : null;
        }

        RefreshVisuals();
    }

    public void ApplyRuntimeStyle(Font font, Color accentColor)
    {
        RefreshTexts();
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

    public void SetAvatar(Sprite avatarSprite)
    {
        if (avatarImage == null)
        {
            return;
        }

        avatarImage.sprite = avatarSprite;
        avatarImage.enabled = avatarSprite != null;
        avatarImage.gameObject.SetActive(avatarSprite != null);
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
        if (isPlayingRoot != null)
        {
            isPlayingRoot.SetActive(_alive && _highlighted);
        }

        RefreshTexts();
    }

    private void RefreshTexts()
    {
        if (nameText != null)
        {
            string displayName = string.IsNullOrEmpty(_playerName) ? "\u89d2\u8272" : _playerName;
            nameText.text = string.IsNullOrEmpty(_controlTag)
                ? displayName
                : $"{displayName}\uff08{_controlTag}\uff09";
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
        moneyText.text = _money.ToString();

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
