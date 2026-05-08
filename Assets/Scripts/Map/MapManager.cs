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
    private bool _usingCustomLayout;
    private float _activeSlotSpacing = 1f;
    private LevelLayout _activeCustomLayout;

    private static readonly Dictionary<int, LevelLayout> CustomLevelLayouts = CreateCustomLevelLayouts();

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
                Vector3 modelOffset = GetModelOffsetForSlot(slot, tileData, i, mapLoader.tileDatas.Length);
                tileController.SetOffset(modelOffset);
                tileController.SetCustomOutwardDirection(modelOffset);
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
            playerManager.RefreshPlayerFacingsToNextTile();
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

        if (IsStartTile(tileData) || tileData.tileCost > 0)
        {
            return DefaultTilePrefabResourcePath;
        }

        switch (tileData.tileType)
        {
            case TileType.DaoJu:
                return ToolTilePrefabResourcePath;
            case TileType.DaTi:
                return QuestionTilePrefabResourcePath;
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
        _activeCustomLayout = null;
        _usingCustomLayout = TryGetCustomLevelLayout(targetCount, out _activeCustomLayout);

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

        _usingAutoLayout = _usingCustomLayout || autoLayoutMapSlots || createdSlots || mapRoot.childCount != targetCount;

        if (_usingAutoLayout)
        {
            if (_usingCustomLayout)
            {
                Debug.Log($"[MapManager] Level {mapLoader.CurrentLevel} uses custom layout {_activeCustomLayout.LayoutId}.");
            }
            else if (createdSlots)
            {
                Debug.LogWarning($"[MapManager] mapRoot slot count was lower than map data count. Auto-layout {targetCount} slots.");
            }
            else if (mapRoot.childCount != targetCount)
            {
                Debug.Log($"[MapManager] mapRoot slot count ({mapRoot.childCount}) differs from map data count ({targetCount}). Auto-layout active slots.");
            }

            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].localPosition = _usingCustomLayout
                    ? GetCustomSlotPosition(_activeCustomLayout, i, slotSpacing, layoutCenter)
                    : GetAutoSlotPosition(i, targetCount, slotSpacing, layoutCenter);
                slots[i].localRotation = Quaternion.identity;
            }

        }

        return slots;
    }

    private bool TryGetCustomLevelLayout(int targetCount, out LevelLayout layout)
    {
        layout = null;
        if (mapLoader == null)
        {
            return false;
        }

        if (!CustomLevelLayouts.TryGetValue(mapLoader.CurrentLevel, out layout))
        {
            return false;
        }

        if (layout.TileCount == targetCount)
        {
            return true;
        }

        Debug.LogWarning(
            $"[MapManager] Custom layout {layout.LayoutId} expects {layout.TileCount} tiles, but level {mapLoader.CurrentLevel} loaded {targetCount}. Falling back to auto layout.");
        layout = null;
        return false;
    }

    private Vector3 GetCustomSlotPosition(LevelLayout layout, int index, float spacing, Vector3 layoutCenter)
    {
        if (layout == null || index < 0 || index >= layout.TileCount)
        {
            return GetAutoSlotPosition(index, Mathf.Max(1, layout != null ? layout.TileCount : 1), spacing, layoutCenter);
        }

        Vector2 centeredPosition = layout.GetCenteredTilePosition(index);
        return new Vector3(
            layoutCenter.x + centeredPosition.x * spacing,
            layoutCenter.y,
            layoutCenter.z + centeredPosition.y * spacing);
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

    private Vector3 GetModelOffsetForSlot(Transform slot, TileData tileData, int index, int total)
    {
        if (TryGetCustomModelOffset(tileData, index, out Vector3 customOffset))
        {
            return customOffset;
        }

        if (_usingCustomLayout)
        {
            return Vector3.zero;
        }

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

    private bool TryGetCustomModelOffset(TileData tileData, int index, out Vector3 offset)
    {
        offset = Vector3.zero;
        if (tileData == null || tileData.tileCost <= 0)
        {
            return false;
        }

        if (!_usingCustomLayout || _activeCustomLayout == null)
        {
            return false;
        }

        if (!_activeCustomLayout.TryGetModelGridPosition(index, out Vector2Int modelGridPosition))
        {
            return false;
        }

        Vector2Int tileGridPosition = _activeCustomLayout.GetTileGridPosition(index);
        Vector2Int delta = modelGridPosition - tileGridPosition;
        offset = new Vector3(delta.x * _activeSlotSpacing, 0f, delta.y * _activeSlotSpacing);
        return offset.sqrMagnitude > 0.0001f;
    }

    private static bool IsStartTile(TileData tileData)
    {
        if (tileData == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(tileData.tileID))
        {
            string tileId = tileData.tileID.Trim().ToUpperInvariant();
            if (tileId.StartsWith("ST") || tileId.Contains("-ST"))
            {
                return true;
            }
        }

        return !string.IsNullOrWhiteSpace(tileData.tileName) && tileData.tileName.Contains("起点");
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

    private static Dictionary<int, LevelLayout> CreateCustomLevelLayouts()
    {
        return new Dictionary<int, LevelLayout>
        {
            {
                1,
                new LevelLayout(
                    "L1-08",
                    new[]
                    {
                        new Vector2Int(5, 4),
                        new Vector2Int(5, 3),
                        new Vector2Int(5, 2),
                        new Vector2Int(5, 1),
                        new Vector2Int(5, 0),
                        new Vector2Int(4, 0),
                        new Vector2Int(3, 0),
                        new Vector2Int(3, 1),
                        new Vector2Int(2, 1),
                        new Vector2Int(1, 1),
                        new Vector2Int(0, 1),
                        new Vector2Int(0, 2),
                        new Vector2Int(0, 3),
                        new Vector2Int(1, 3),
                        new Vector2Int(1, 4),
                        new Vector2Int(2, 4),
                        new Vector2Int(3, 4),
                        new Vector2Int(4, 4)
                    })
            },
            {
                2,
                new LevelLayout(
                    "L2-03",
                    new[]
                    {
                        new Vector2Int(6, 5),
                        new Vector2Int(5, 5),
                        new Vector2Int(4, 5),
                        new Vector2Int(3, 5),
                        new Vector2Int(2, 5),
                        new Vector2Int(1, 5),
                        new Vector2Int(0, 5),
                        new Vector2Int(0, 4),
                        new Vector2Int(0, 3),
                        new Vector2Int(1, 3),
                        new Vector2Int(1, 2),
                        new Vector2Int(1, 1),
                        new Vector2Int(1, 0),
                        new Vector2Int(2, 0),
                        new Vector2Int(3, 0),
                        new Vector2Int(4, 0),
                        new Vector2Int(4, 1),
                        new Vector2Int(4, 2),
                        new Vector2Int(5, 2),
                        new Vector2Int(5, 3),
                        new Vector2Int(6, 3),
                        new Vector2Int(6, 4)
                    })
            },
            {
                3,
                new LevelLayout(
                    "L3-05",
                    new[]
                    {
                        new Vector2Int(2, 7),
                        new Vector2Int(2, 6),
                        new Vector2Int(2, 5),
                        new Vector2Int(1, 5),
                        new Vector2Int(0, 5),
                        new Vector2Int(0, 4),
                        new Vector2Int(0, 3),
                        new Vector2Int(1, 3),
                        new Vector2Int(2, 3),
                        new Vector2Int(2, 2),
                        new Vector2Int(2, 1),
                        new Vector2Int(3, 1),
                        new Vector2Int(4, 1),
                        new Vector2Int(4, 0),
                        new Vector2Int(5, 0),
                        new Vector2Int(6, 0),
                        new Vector2Int(6, 1),
                        new Vector2Int(6, 2),
                        new Vector2Int(7, 2),
                        new Vector2Int(8, 2),
                        new Vector2Int(8, 3),
                        new Vector2Int(8, 4),
                        new Vector2Int(7, 4),
                        new Vector2Int(6, 4),
                        new Vector2Int(6, 5),
                        new Vector2Int(6, 6),
                        new Vector2Int(5, 6),
                        new Vector2Int(4, 6),
                        new Vector2Int(4, 7),
                        new Vector2Int(3, 7)
                    })
            }
        };
    }

    private sealed class LevelLayout
    {
        public readonly string LayoutId;
        private readonly Vector2Int[] tilePositions;
        private readonly Dictionary<int, Vector2Int> modelPositions;
        private readonly Vector2 center;

        public LevelLayout(string layoutId, Vector2Int[] tilePositions)
        {
            LayoutId = layoutId;
            this.tilePositions = ReorientPath(tilePositions, layoutId == "L2-03");
            this.modelPositions = CalculateModelPositions(this.tilePositions);
            center = CalculateCenter(this.tilePositions);
        }

        public int TileCount => tilePositions.Length;

        public Vector2Int GetTileGridPosition(int index)
        {
            if (index < 0 || index >= tilePositions.Length)
            {
                return Vector2Int.zero;
            }

            return tilePositions[index];
        }

        public Vector2 GetCenteredTilePosition(int index)
        {
            Vector2Int position = GetTileGridPosition(index);
            return new Vector2(position.x - center.x, position.y - center.y);
        }

        public bool TryGetModelGridPosition(int index, out Vector2Int position)
        {
            return modelPositions.TryGetValue(index, out position);
        }

        private static Vector2 CalculateCenter(Vector2Int[] positions)
        {
            if (positions == null || positions.Length == 0)
            {
                return Vector2.zero;
            }

            int minX = positions[0].x;
            int maxX = positions[0].x;
            int minY = positions[0].y;
            int maxY = positions[0].y;
            for (int i = 1; i < positions.Length; i++)
            {
                minX = Mathf.Min(minX, positions[i].x);
                maxX = Mathf.Max(maxX, positions[i].x);
                minY = Mathf.Min(minY, positions[i].y);
                maxY = Mathf.Max(maxY, positions[i].y);
            }

            return new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        }

        private static Vector2Int[] ReorientPath(Vector2Int[] positions, bool reverseAfterStart)
        {
            if (positions == null || positions.Length == 0)
            {
                return new Vector2Int[0];
            }

            Vector2Int[] ordered = new Vector2Int[positions.Length];
            ordered[0] = positions[0];
            for (int i = 1; i < positions.Length; i++)
            {
                ordered[i] = reverseAfterStart ? positions[positions.Length - i] : positions[i];
            }

            int minX = positions[0].x;
            int maxX = positions[0].x;
            int minY = positions[0].y;
            int maxY = positions[0].y;
            for (int i = 1; i < positions.Length; i++)
            {
                minX = Mathf.Min(minX, positions[i].x);
                maxX = Mathf.Max(maxX, positions[i].x);
                minY = Mathf.Min(minY, positions[i].y);
                maxY = Mathf.Max(maxY, positions[i].y);
            }

            for (int i = 0; i < ordered.Length; i++)
            {
                ordered[i] = new Vector2Int(minX + maxX - ordered[i].x, minY + maxY - ordered[i].y);
            }

            return ordered;
        }

        private static Dictionary<int, Vector2Int> CalculateModelPositions(Vector2Int[] positions)
        {
            Dictionary<int, Vector2Int> result = new Dictionary<int, Vector2Int>();
            if (positions == null || positions.Length == 0)
            {
                return result;
            }

            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>(positions);
            Vector2 center = CalculateCenter(positions);
            for (int i = 0; i < positions.Length; i++)
            {
                result[i] = FindModelPosition(positions[i], occupied, center);
            }

            return result;
        }

        private static Vector2Int FindModelPosition(Vector2Int tilePosition, HashSet<Vector2Int> occupied, Vector2 center)
        {
            Vector2Int[] directions =
            {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(-1, 0),
                new Vector2Int(0, -1)
            };

            Vector2 tileVector = new Vector2(tilePosition.x - center.x, tilePosition.y - center.y);
            Vector2Int bestPosition = tilePosition + directions[0];
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int direction = directions[i];
                Vector2Int candidate = tilePosition + direction;
                if (occupied.Contains(candidate))
                {
                    continue;
                }

                Vector2 candidateVector = new Vector2(candidate.x - center.x, candidate.y - center.y);
                Vector2 directionVector = new Vector2(direction.x, direction.y);
                int occupiedNeighborCount = CountOccupiedNeighbors(candidate, occupied);
                float score = candidateVector.sqrMagnitude
                    + Vector2.Dot(directionVector, tileVector) * 1.5f
                    - Mathf.Max(0, occupiedNeighborCount - 1) * 4f;

                if (!found || score > bestScore)
                {
                    found = true;
                    bestScore = score;
                    bestPosition = candidate;
                }
            }

            return bestPosition;
        }

        private static int CountOccupiedNeighbors(Vector2Int position, HashSet<Vector2Int> occupied)
        {
            int count = 0;
            if (occupied.Contains(position + new Vector2Int(1, 0))) count++;
            if (occupied.Contains(position + new Vector2Int(0, 1))) count++;
            if (occupied.Contains(position + new Vector2Int(-1, 0))) count++;
            if (occupied.Contains(position + new Vector2Int(0, -1))) count++;
            return count;
        }
    }
}
