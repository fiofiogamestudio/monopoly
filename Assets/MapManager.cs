using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    private const string DefaultTilePrefabResourcePath = "Prefabs/Tile/tile_basic";
    private const string ToolTilePrefabResourcePath = "Prefabs/Tile/tile_chance";
    private const string QuestionTilePrefabResourcePath = "Prefabs/Tile/tile_question";
    private const string LuckTilePrefabResourcePath = "Prefabs/Tile/tile_why";

    public MapLoader mapLoader;
    public GameObject tilePrefab;
    public Transform mapRoot;
    public float autoSlotSpacing = 2.2f;
    public bool autoLayoutMapSlots = false;
    [Header("Tile Layering")]
    public float tileLayerYOffsetStep = 0.002f;
    [Range(2, 12)] public int tileLayerCycle = 7;

    public List<TileController> tileControllers = new List<TileController>();
    private readonly Dictionary<string, GameObject> _tilePrefabCache = new Dictionary<string, GameObject>();
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
        if (mapLoader == null || mapLoader.tileDatas == null || mapRoot == null)
        {
            Debug.LogError("[MapManager] Missing mapLoader, tileDatas, or mapRoot.");
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
            GameObject resolvedTilePrefab = ResolveTilePrefab(tileData);
            if (resolvedTilePrefab == null)
            {
                Debug.LogError($"[MapManager] Missing tile prefab at index={i}, tileName={tileData.tileName}.");
                return;
            }

            Transform slot = mapSlots[i];
            GameObject tileObj = Instantiate(resolvedTilePrefab, slot);
            tileObj.name = tileData.tileName;
            ApplyGeneratedTileTransform(tileObj, tileData, i);
            TileController tileController = tileObj.GetComponent<TileController>();
            if (tileController == null)
            {
                tileController = tileObj.AddComponent<TileController>();
            }

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

    private void ApplyGeneratedTileTransform(GameObject tileObj, TileData tileData, int tileIndex)
    {
        if (tileObj == null)
        {
            return;
        }

        Vector3 prefabScale = tileObj.transform.localScale;
        tileObj.transform.localRotation = Quaternion.identity;
        tileObj.transform.localPosition = new Vector3(0f, GetTileVisualYOffset(tileIndex), 0f);

        if (GetTilePrefabResourcePath(tileData) != DefaultTilePrefabResourcePath)
        {
            tileObj.transform.localScale = prefabScale;
        }
    }

    private GameObject ResolveTilePrefab(TileData tileData)
    {
        string resourcePath = GetTilePrefabResourcePath(tileData);
        GameObject resourcePrefab = LoadTilePrefab(resourcePath);
        if (resourcePrefab != null)
        {
            return resourcePrefab;
        }

        if (resourcePath != DefaultTilePrefabResourcePath)
        {
            GameObject defaultPrefab = LoadTilePrefab(DefaultTilePrefabResourcePath);
            if (defaultPrefab != null)
            {
                Debug.LogWarning($"[MapManager] Tile prefab not found at Resources/{resourcePath}. Falling back to tile_basic.");
                return defaultPrefab;
            }
        }

        if (tilePrefab != null)
        {
            Debug.LogWarning($"[MapManager] Tile prefab not found at Resources/{resourcePath}. Falling back to serialized tilePrefab.");
            return tilePrefab;
        }

        Debug.LogError($"[MapManager] Tile prefab not found at Resources/{resourcePath} or Resources/{DefaultTilePrefabResourcePath}.");
        return null;
    }

    private string GetTilePrefabResourcePath(TileData tileData)
    {
        if (tileData == null)
        {
            return DefaultTilePrefabResourcePath;
        }

        if (tileData.tileCost > 0)
        {
            return DefaultTilePrefabResourcePath;
        }

        switch (tileData.tileType)
        {
            case TileType.DaoJu:
                return ToolTilePrefabResourcePath;
            case TileType.DaTi:
                return QuestionTilePrefabResourcePath;
            case TileType.ShiJian:
            case TileType.JiHui:
                return LuckTilePrefabResourcePath;
            default:
                return DefaultTilePrefabResourcePath;
        }
    }

    private GameObject LoadTilePrefab(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        if (_tilePrefabCache.TryGetValue(resourcePath, out GameObject cachedPrefab))
        {
            return cachedPrefab;
        }

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        _tilePrefabCache[resourcePath] = prefab;
        return prefab;
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
