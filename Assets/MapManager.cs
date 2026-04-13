using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public MapLoader mapLoader;
    public GameObject tilePrefab;
    public Transform mapRoot;
    public float autoSlotSpacing = 2.2f;
    public bool autoLayoutMapSlots = false;
    [Header("Tile Layering")]
    public float tileLayerYOffsetStep = 0.002f;
    [Range(2, 12)] public int tileLayerCycle = 7;

    public List<TileController> tileControllers = new List<TileController>();
    private bool _usingAutoLayout;

    private static readonly Vector2[] JiaxingRouteLayout24 =
    {
        new Vector2(-4.0f, -3.0f),
        new Vector2(-2.6f, -3.2f),
        new Vector2(-1.3f, -3.0f),
        new Vector2(0.0f, -3.2f),
        new Vector2(1.3f, -3.0f),
        new Vector2(2.6f, -3.2f),
        new Vector2(4.0f, -3.0f),
        new Vector2(4.6f, -2.0f),
        new Vector2(4.4f, -1.0f),
        new Vector2(4.7f, 0.0f),
        new Vector2(4.4f, 1.0f),
        new Vector2(4.6f, 2.0f),
        new Vector2(4.0f, 3.0f),
        new Vector2(2.6f, 3.2f),
        new Vector2(1.3f, 3.0f),
        new Vector2(0.0f, 3.2f),
        new Vector2(-1.3f, 3.0f),
        new Vector2(-2.6f, 3.2f),
        new Vector2(-4.0f, 3.0f),
        new Vector2(-4.6f, 2.0f),
        new Vector2(-4.4f, 1.0f),
        new Vector2(-4.7f, 0.0f),
        new Vector2(-4.4f, -1.0f),
        new Vector2(-4.6f, -2.0f)
    };

    public void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        if (mapLoader == null || mapLoader.tileDatas == null || tilePrefab == null || mapRoot == null)
        {
            Debug.LogError("[MapManager] Missing mapLoader, tileDatas, tilePrefab, or mapRoot.");
            return;
        }

        List<Transform> mapSlots = EnsureMapSlots(mapLoader.tileDatas.Length);
        tileControllers.Clear();

        foreach (Transform child in mapSlots)
        {
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
            Transform slot = mapSlots[i];
            GameObject tileObj = Instantiate(tilePrefab, slot);
            tileObj.name = tileData.tileName;
            Vector3 tileLocalPosition = tileObj.transform.localPosition;
            tileLocalPosition.y = GetTileVisualYOffset(i);
            tileObj.transform.localPosition = tileLocalPosition;
            TileController tileController = tileObj.GetComponent<TileController>();

            GridConfig gridConfig = slot.GetComponent<GridConfig>();
            if (!_usingAutoLayout && gridConfig != null && tileController != null)
            {
                tileController.SetOffset(gridConfig.gridOffset);
            }

            tileControllers.Add(tileController);

            if (tileController != null)
            {
                tileController.Init(tileData);
            }
            else
            {
                Debug.LogError($"[MapManager] Tile prefab missing TileController at index={i}, tileName={tileData.tileName}.");
            }
        }
    }

    private List<Transform> EnsureMapSlots(int targetCount)
    {
        List<Transform> slots = new List<Transform>();

        bool createdSlots = false;
        while (mapRoot.childCount < targetCount)
        {
            GameObject slotObj = new GameObject($"AutoSlot ({mapRoot.childCount})");
            slotObj.transform.SetParent(mapRoot, false);
            createdSlots = true;
        }

        for (int i = 0; i < targetCount; i++)
        {
            Transform slot = mapRoot.GetChild(i);
            slots.Add(slot);
        }

        _usingAutoLayout = autoLayoutMapSlots || createdSlots;

        if (_usingAutoLayout)
        {
            if (createdSlots)
            {
                Debug.LogWarning($"[MapManager] mapRoot slot count was lower than map data count. Auto-layout {targetCount} slots.");
            }

            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].localPosition = GetAutoSlotPosition(i, targetCount);
                slots[i].localRotation = Quaternion.identity;
            }

            PlayerManager playerManager = FindObjectOfType<PlayerManager>();
            if (playerManager != null)
            {
                playerManager.RefreshPlayerPositions();
            }
        }

        return slots;
    }

    private Vector3 GetAutoSlotPosition(int index, int total)
    {
        if (total == JiaxingRouteLayout24.Length)
        {
            Vector2 point = JiaxingRouteLayout24[index];
            float routeScale = autoSlotSpacing * 0.8f;
            return new Vector3(point.x * routeScale, 0f, point.y * routeScale);
        }

        float angle = (Mathf.PI * 2f * index) / Mathf.Max(1, total);
        float radius = Mathf.Max(4f, total * autoSlotSpacing / (Mathf.PI * 2f));
        return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }

    private float GetTileVisualYOffset(int tileIndex)
    {
        if (tileLayerYOffsetStep <= 0f)
        {
            return 0f;
        }

        int cycle = Mathf.Max(2, tileLayerCycle);
        int patternIndex = (tileIndex * 3) % cycle;
        float centered = patternIndex - (cycle - 1) * 0.5f;
        return centered * tileLayerYOffsetStep;
    }
}
