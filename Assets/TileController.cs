using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileController : MonoBehaviour
{
    public TileData tileData;
    public Text gridText;
    public GameObject modelBase;
    public Vector3 gridOffset;
    public Text modelText;

    public bool hasUpgraded = false;
    public void SetOffset(Vector3 offset)
    {
        this.gridOffset = offset;
    }
    public void Init(TileData tileData)
    {
        this.tileData = tileData;

        // attach models, find same prefab name under Resources/Prefabs/TileModels
        GameObject modelPrefab = Resources.Load<GameObject>($"Prefabs/TileModels/{tileData.tileName}");
        if (modelPrefab != null)
        {
            GameObject modelInstance = Instantiate(modelPrefab, this.transform);
            // set parent to this
            modelInstance.transform.SetParent(this.transform, false);

            modelInstance.transform.localPosition = Vector3.zero + gridOffset;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            modelBase.transform.localPosition += gridOffset;

            modelText.text = tileData.tileName;
        }
        else
        {
            modelText.text = ""; // hide
        }


        // set gridText to tileName
        if (gridText != null)
        {
            gridText.text = tileData.tileType.ToRealName();
            gridText.color = tileData.tileType.ToRealColor();
        }
    }

}
