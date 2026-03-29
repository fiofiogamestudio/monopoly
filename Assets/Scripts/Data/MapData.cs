using System.Drawing;

[System.Serializable]
public enum TileType
{
    DiBiao, // 地标
    FengWu, // 风物
    ShiJian, // 事件
    JiHui, // 机会
    DaoJu, // 道具
    DaTi // 答题
}

public static class TileTypeExtension
{
    public static string ToRealName(this TileType tileType)
    {
        switch (tileType)
        {
            case TileType.DiBiao:
                return "地标";
            case TileType.FengWu:
                return "风物";
            case TileType.ShiJian:
                return "事件";
            case TileType.JiHui:
                return "机会";
            case TileType.DaoJu:
                return "道具";
            case TileType.DaTi:
                return "答题";
            default:
                return "未知";
        }
    }

    public static UnityEngine.Color ToRealColor(this TileType tileType)
    {
        switch (tileType)
        {
            case TileType.DiBiao:
                return UnityEngine.Color.red;
            case TileType.FengWu:
                return UnityEngine.Color.green;
            case TileType.ShiJian:
                return UnityEngine.Color.blue;
            case TileType.JiHui:
                return UnityEngine.Color.yellow;
            case TileType.DaoJu:
                return UnityEngine.Color.cyan;
            case TileType.DaTi:
                return UnityEngine.Color.magenta;
            default:
                return UnityEngine.Color.black;
        }
    }
}

[System.Serializable]
public class TileData
{
    public string tileID;
    public TileType tileType;
    public string tileName;
    public string tileDescription;
    public string enterEffect; // 进入触发
    public string passEffect; // 经过触发
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