using UnityEngine;

[System.Serializable]
public enum TileType
{
    DiBiao,
    FengWu,
    ShiJian,
    JiHui,
    DaoJu,
    DaTi
}

public static class TileTypeExtension
{
    public struct TilePalette
    {
        public Color baseColor;
        public Color topColor;
        public Color accentColor;
        public Color pedestalColor;
        public Color labelColor;
    }

    public static string ToRealName(this TileType tileType)
    {
        switch (tileType)
        {
            case TileType.DiBiao:
                return "\u5730\u6807";
            case TileType.FengWu:
                return "\u98ce\u7269";
            case TileType.ShiJian:
                return "\u4e8b\u4ef6";
            case TileType.JiHui:
                return "\u8fd0\u6c14";
            case TileType.DaoJu:
                return "\u9053\u5177";
            case TileType.DaTi:
                return "\u7b54\u9898";
            default:
                return "\u672a\u77e5";
        }
    }

    public static Color ToRealColor(this TileType tileType)
    {
        return tileType.ToPalette().accentColor;
    }

    public static TilePalette ToPalette(this TileType tileType)
    {
        switch (tileType)
        {
            case TileType.DiBiao:
                return CreatePalette("9C4A2D", "F2A65A", "C85B32", "FFD5A4", "4A2012");
            case TileType.FengWu:
                return CreatePalette("2E7D54", "7ED49B", "1F9A64", "D8F8E3", "163526");
            case TileType.ShiJian:
                return CreatePalette("1E6E8A", "65C7DD", "1990B0", "D6F3F9", "103544");
            case TileType.JiHui:
                return CreatePalette("A86811", "F4D35E", "E49B17", "FFF0B1", "5A3604");
            case TileType.DaoJu:
                return CreatePalette("5A3A97", "9A78F0", "7A53D6", "E5D8FF", "28174C");
            case TileType.DaTi:
                return CreatePalette("A83E5E", "F28CAB", "D65479", "FFD9E6", "4B1527");
            default:
                return CreatePalette("9CA3AF", "F8FAFC", "6B7280", "E5E7EB", "1F2937");
        }
    }

    public static Color ToBaseColor(this TileType tileType)
    {
        return tileType.ToPalette().baseColor;
    }

    public static Color ToTopColor(this TileType tileType)
    {
        return tileType.ToPalette().topColor;
    }

    public static Color ToAccentColor(this TileType tileType)
    {
        return tileType.ToPalette().accentColor;
    }

    public static Color ToPedestalColor(this TileType tileType)
    {
        return tileType.ToPalette().pedestalColor;
    }

    public static Color ToLabelColor(this TileType tileType)
    {
        return tileType.ToPalette().labelColor;
    }

    private static TilePalette CreatePalette(string baseHex, string topHex, string accentHex, string pedestalHex, string labelHex)
    {
        return new TilePalette
        {
            baseColor = ParseHex(baseHex),
            topColor = ParseHex(topHex),
            accentColor = ParseHex(accentHex),
            pedestalColor = ParseHex(pedestalHex),
            labelColor = ParseHex(labelHex)
        };
    }

    private static Color ParseHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString($"#{hex}", out Color color))
        {
            return color;
        }

        return Color.white;
    }
}

[System.Serializable]
public class TileData
{
    public string tileID;
    public TileType tileType;
    public string tileName;
    public string tileDescription;
    public string enterEffect;
    public string passEffect;
    public int tileCost;
    public int tileIncome;
    public int upgradeCost;
    public int tileIncomeUpgrade;
}

[System.Serializable]
public class MapWrapper
{
    public TileData[] datas;
}

[System.Serializable]
public class QuestionData
{
    public string id;
    public string category;
    public string text;
    public string[] options;
    public int answerIndex;
    public string explain;
}

[System.Serializable]
public class QuestionWrapper
{
    public QuestionData[] questions;
}

[System.Serializable]
public class CardData
{
    public string id;
    public string cardName;
    public string description;
    public string spriteName;
    public string effect;
}

[System.Serializable]
public class CardWrapper
{
    public CardData[] toolCards;
    public CardData[] luckCards;
}

[System.Serializable]
public class GameConfigData
{
    public int startMoney = 8000;
    public bool enableTargetMoneyVictory = true;
    public int targetMoneyToWin = 18000;
}

