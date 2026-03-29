using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoItem : MonoBehaviour
{
    public Text nameText;
    public Text moneyText;

    public void SetName(string n)
    {
        if (nameText != null) nameText.text = n;
    }

    public void SetMoney(int m)
    {
        if (moneyText != null) moneyText.text = $"￥{m}";
    }

    public void SetHighlight(bool on)
    {
        // 最简单的高亮：改名字颜色（也可改背景图）
        if (nameText != null)
            nameText.color = on ? Color.yellow : Color.white;
    }
}
