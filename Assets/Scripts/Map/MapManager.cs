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
    public float autoSlotSpacing = 1f;
    public bool autoLayoutMapSlots = false;
    [Header("Tile Layering")]
    public float tileLayerYOffsetStep = 0.002f;
    [Range(2, 12)] public int tileLayerCycle = 7;

    public List<TileController> tileControllers = new List<TileController>();
    private readonly Dictionary<string, GameObject> _tilePrefabCache = new Dictionary<string, GameObject>();
    private bool _usingAutoLayout;
    private float _activeSlotSpacing = 1f;

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

        tileControllers.Clear();
        List<Transform> mapSlots = EnsureMapSlots(mapLoader.tileDatas.Length);

        for (int i = 0; i < mapRoot.childCount; i++)
        {
            Transform child = mapRoot.GetChild(i);
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

            if (tileController != null)
            {
                tileController.SetOffset(GetModelOffsetForSlot(slot, i, mapLoader.tileDatas.Length));
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

        PlayerManager playerManager = FindObjectOfType<PlayerManager>();
        if (playerManager != null)
        {
            playerManager.RefreshPlayerPositions();
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
        Vector3 layoutCenter = GetExistingSlotLayoutCenter();
        float slotSpacing = GetAutoSlotSpacing();
        _activeSlotSpacing = slotSpacing;

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
            if (!slot.gameObject.activeSelf)
            {
                slot.gameObject.SetActive(true);
            }

            slots.Add(slot);
        }

        for (int i = targetCount; i < mapRoot.childCount; i++)
        {
            Transform extraSlot = mapRoot.GetChild(i);
            if (extraSlot.gameObject.activeSelf)
            {
                extraSlot.gameObject.SetActive(false);
            }
        }

        _usingAutoLayout = autoLayoutMapSlots || createdSlots || mapRoot.childCount != targetCount;

        if (_usingAutoLayout)
        {
            if (createdSlots)
            {
                Debug.LogWarning($"[MapManager] mapRoot slot count was lower than map data count. Auto-layout {targetCount} slots.");
            }
            else if (mapRoot.childCount != targetCount)
            {
                Debug.Log($"[MapManager] mapRoot slot count ({mapRoot.childCount}) differs from map data count ({targetCount}). Auto-layout active slots.");
            }

            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].localPosition = GetAutoSlotPosition(i, targetCount, slotSpacing, layoutCenter);
                slots[i].localRotation = Quaternion.identity;
            }

        }

        return slots;
    }

    private Vector3 GetAutoSlotPosition(int index, int total, float spacing, Vector3 layoutCenter)
    {
        if (TryGetRectLoopGridPosition(index, total, out Vector2 gridPosition))
        {
            return new Vector3(
                layoutCenter.x + gridPosition.x * spacing,
                layoutCenter.y,
                layoutCenter.z + gridPosition.y * spacing);
        }

        Debug.LogWarning($"[MapManager] Auto-layout expects an even rectangular loop tile count, but got {total}. Falling back to a compact line.");
        float centeredX = index - (Mathf.Max(1, total) - 1) * 0.5f;
        return new Vector3(layoutCenter.x + centeredX * spacing, layoutCenter.y, layoutCenter.z);
    }

    private Vector3 GetModelOffsetForSlot(Transform slot, int index, int total)
    {
        if (!_usingAutoLayout && slot != null)
        {
            GridConfig gridConfig = slot.GetComponent<GridConfig>();
            if (gridConfig != null)
            {
                return gridConfig.gridOffset;
            }
        }

        if (TryGetRectLoopModelOffset(index, total, _activeSlotSpacing, out Vector3 autoOffset))
        {
            return autoOffset;
        }

        return Vector3.zero;
    }

    private bool TryGetRectLoopModelOffset(int index, int total, float spacing, out Vector3 offset)
    {
        offset = Vector3.zero;
        if (total < 8 || total % 2 != 0 || index < 0 || index >= total)
        {
            return false;
        }

        int edgeSum = total / 2 + 2;
        int width = Mathf.CeilToInt(edgeSum * 0.5f);
        int height = edgeSum - width;
        if (width < 2 || height < 2 || 2 * width + 2 * height - 4 != total)
        {
            return false;
        }

        float modelSpacing = Mathf.Max(0.001f, spacing);
        if (index < width)
        {
            offset = new Vector3(0f, 0f, -modelSpacing);
        }
        else if (index < width + height - 1)
        {
            offset = new Vector3(modelSpacing, 0f, 0f);
        }
        else if (index < width + height - 1 + width - 1)
        {
            offset = new Vector3(0f, 0f, modelSpacing);
        }
        else
        {
            offset = new Vector3(-modelSpacing, 0f, 0f);
        }

        return true;
    }

    private Vector3 GetExistingSlotLayoutCenter()
    {
        if (mapRoot == null || mapRoot.childCount == 0)
        {
            return Vector3.zero;
        }

        Vector3 min = mapRoot.GetChild(0).localPosition;
        Vector3 max = min;
        for (int i = 1; i < mapRoot.childCount; i++)
        {
            Vector3 position = mapRoot.GetChild(i).localPosition;
            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
        }

        return new Vector3(
            (min.x + max.x) * 0.5f,
            (min.y + max.y) * 0.5f,
            (min.z + max.z) * 0.5f);
    }

    private float GetAutoSlotSpacing()
    {
        const float minValidSpacing = 0.001f;
        if (mapRoot != null && mapRoot.childCount > 1)
        {
            float detectedSpacing = float.MaxValue;
            for (int i = 0; i < mapRoot.childCount; i++)
            {
                Vector3 a = mapRoot.GetChild(i).localPosition;
                for (int j = i + 1; j < mapRoot.childCount; j++)
                {
                    Vector3 b = mapRoot.GetChild(j).localPosition;
                    float dx = Mathf.Abs(a.x - b.x);
                    float dz = Mathf.Abs(a.z - b.z);

                    if (dx > minValidSpacing && dz <= minValidSpacing)
                    {
                        detectedSpacing = Mathf.Min(detectedSpacing, dx);
                    }

                    if (dz > minValidSpacing && dx <= minValidSpacing)
                    {
                        detectedSpacing = Mathf.Min(detectedSpacing, dz);
                    }
                }
            }

            if (detectedSpacing < float.MaxValue)
            {
                return detectedSpacing;
            }
        }

        return Mathf.Max(minValidSpacing, autoSlotSpacing);
    }

    private bool TryGetRectLoopGridPosition(int index, int total, out Vector2 gridPosition)
    {
        gridPosition = Vector2.zero;
        if (total < 8 || total % 2 != 0 || index < 0 || index >= total)
        {
            return false;
        }

        int edgeSum = total / 2 + 2;
        int width = Mathf.CeilToInt(edgeSum * 0.5f);
        int height = edgeSum - width;

        if (width < 2 || height < 2 || 2 * width + 2 * height - 4 != total)
        {
            return false;
        }

        int gridX;
        int gridY;

        if (index < width)
        {
            gridX = index;
            gridY = 0;
        }
        else if (index < width + height - 1)
        {
            gridX = width - 1;
            gridY = index - width + 1;
        }
        else if (index < width + height - 1 + width - 1)
        {
            gridX = width - 2 - (index - (width + height - 1));
            gridY = height - 1;
        }
        else
        {
            gridX = 0;
            gridY = height - 2 - (index - (width + height - 1 + width - 1));
        }

        float centeredX = gridX - (width - 1) * 0.5f;
        float centeredY = gridY - (height - 1) * 0.5f;
        gridPosition = new Vector2(centeredX, centeredY);
        return true;
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
