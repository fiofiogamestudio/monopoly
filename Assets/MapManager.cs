using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public MapLoader mapLoader;
    public GameObject tilePrefab;
    public Transform mapRoot;

    public List<TileController> tileControllers = new List<TileController>();

    public void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        if (mapRoot.childCount < mapLoader.tileDatas.Length)
        {
            Debug.LogError("Not enough tile prefabs under mapRoot to generate the map.");
        }

        // Clear
        foreach (Transform child in mapRoot)
        {
            // if child has children, destroy
            if (child.childCount > 0)
            {
                foreach (Transform grandChild in child)
                {
                    Destroy(grandChild.gameObject);
                }
            }
        }

        // Generate
        for (int i = 0; i < mapLoader.tileDatas.Length; i++)
        {
            TileData tileData = mapLoader.tileDatas[i];
            GameObject tileObj = Instantiate(tilePrefab, mapRoot);
            tileObj.name = tileData.tileName;
            TileController tileController = tileObj.GetComponent<TileController>();

            // get GridConfig from mapRoot.GetChild(i)
            GridConfig gridConfig = mapRoot.GetChild(i).GetComponent<GridConfig>();
            if (gridConfig != null)
            {
                tileController.SetOffset(gridConfig.gridOffset);
            }

            // set parent to mapRoot.GetChild(i)
            tileObj.transform.SetParent(mapRoot.GetChild(i), false);

            // register tileController
            tileControllers.Add(tileController);

            // init data
            tileController.Init(tileData);
        }
    }
}
