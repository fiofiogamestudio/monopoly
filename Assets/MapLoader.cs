using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    public TileData[] tileDatas;


    public void Awake()
    {
        tileDatas = DataLoader.LoadJson<MapWrapper>("map").datas;
    }
}
