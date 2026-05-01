using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSceneDecorationPrefabBuilder
{
    private const string PrefabPath = "Assets/Resources/Prefabs/Environment/GameSceneDecorationRoot.prefab";
    private const string RootName = "GameSceneDecorationRoot";
    private const float CellSize = 2f;
    private const int GroundHalfCells = 6;
    private const float RoadRingCoordinate = (GroundHalfCells + 1) * CellSize;
    private const float BuildingRingCoordinate = (GroundHalfCells + 3) * CellSize;
    private const float TreeRingCoordinate = (GroundHalfCells + 4) * CellSize;
    private const float GroundY = -0.08f;
    private const float RoadY = -0.045f;
    private const float PropY = -0.02f;

    private static readonly string[] GroundPrefabs =
    {
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Floor/Grass/Floor_Grass.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Floor/Grass/Floor_Grass_Bright.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Floor/Grass/Floor_SimpleGreen.prefab"
    };

    private static readonly string[] BuildingPrefabs =
    {
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Buildings/Shops/Coffee_Shop.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Buildings/Shops/Candy_Shop.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Buildings/Shops/Restaurant_Shop.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Buildings/Shops/Hypermarket_Shop.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Buildings/Shops/Magic_Shop.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Buildings/Shops/Kiosk_Shop.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Buildings/Locations/TownHall.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Buildings/Locations/TrainStation.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Buildings/Locations/Hotel.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Buildings/Locations/Windmill_Red.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Buildings/Houses/House_2Rooms_Green.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Buildings/Houses/House_3Rooms_Yellow.prefab"
    };

    private static readonly string[] TreePrefabs =
    {
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Trees/Tree_Simple_Green.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Trees/Tree_Big.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Trees/Tree_Green_x3Crowns.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Trees/Tree_Fruits_Apples.prefab"
    };

    private static readonly string[] FlowerPrefabs =
    {
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Flowers/Flowers_Mixed.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Flowers/Flowers_Yellow.prefab",
        "Assets/CartoonTown-LowPolyAssets/Prefabs/Flowers/Flower_1x_Pink.prefab"
    };

    [MenuItem("Tools/Monopoly/Environment/Rebuild Decoration Prefab")]
    public static void RebuildDecorationPrefab()
    {
        GameObject root = BuildDecorationRoot();
        EnsureFolder(Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/'));

        bool success;
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out success);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (success)
        {
            Debug.Log($"[Environment] Built decoration prefab: {PrefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        }
        else
        {
            Debug.LogError($"[Environment] Failed to build decoration prefab: {PrefabPath}");
        }
    }

    [MenuItem("Tools/Monopoly/Environment/Place Decoration Prefab In Current Scene")]
    public static void PlaceDecorationPrefabInCurrentScene()
    {
        PlaceDecorationPrefabInCurrentScene(false);
    }

    [MenuItem("Tools/Monopoly/Environment/Rebuild And Replace Decoration In Current Scene")]
    public static void RebuildAndReplaceDecorationInCurrentScene()
    {
        RebuildDecorationPrefab();
        PlaceDecorationPrefabInCurrentScene(true);
    }

    private static void PlaceDecorationPrefabInCurrentScene(bool replaceExisting)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            RebuildDecorationPrefab();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        if (prefab == null)
        {
            Debug.LogError("[Environment] Decoration prefab is missing.");
            return;
        }

        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            if (!replaceExisting)
            {
                Debug.Log("[Environment] Decoration root already exists in the current scene.");
                Selection.activeGameObject = existing;
                return;
            }

            Undo.DestroyObjectImmediate(existing);
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            Debug.LogError("[Environment] Failed to instantiate decoration prefab.");
            return;
        }

        instance.name = RootName;
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = instance;
        Debug.Log("[Environment] Placed decoration prefab in the current scene.");
    }

    private static GameObject BuildDecorationRoot()
    {
        GameObject root = new GameObject(RootName);
        Transform rootTransform = root.transform;
        rootTransform.position = Vector3.zero;
        rootTransform.rotation = Quaternion.identity;
        rootTransform.localScale = Vector3.one;

        Transform ground = CreateGroup(rootTransform, "Ground");
        Transform roadRing = CreateGroup(rootTransform, "RoadRing");
        Transform buildings = CreateGroup(rootTransform, "Buildings");
        Transform trees = CreateGroup(rootTransform, "Trees");
        Transform props = CreateGroup(rootTransform, "Props");

        BuildGround(ground);
        BuildRoadRing(roadRing);
        BuildBuildings(buildings);
        BuildTrees(trees);
        BuildProps(props);
        return root;
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static void BuildGround(Transform parent)
    {
        for (int z = -GroundHalfCells; z <= GroundHalfCells; z++)
        {
            for (int x = -GroundHalfCells; x <= GroundHalfCells; x++)
            {
                string path = GroundPrefabs[Mathf.Abs(x + z * 3) % GroundPrefabs.Length];
                GameObject tile = InstantiateAsset(path, parent, $"Grass_{x + GroundHalfCells:00}_{z + GroundHalfCells:00}");
                if (tile == null)
                {
                    continue;
                }

                tile.transform.localPosition = GridPosition(x, z, GroundY);
                tile.transform.localRotation = Quaternion.Euler(0f, ((x + z) & 1) == 0 ? 0f : 90f, 0f);
                tile.transform.localScale = Vector3.one * 1.05f;
            }
        }
    }

    private static void BuildRoadRing(Transform parent)
    {
        GameObject straightPrefab = LoadPrefab("Assets/CartoonTown-LowPolyAssets/Prefabs/Roads/StonePath/StonePath_Straight.prefab");
        GameObject turnPrefab = LoadPrefab("Assets/CartoonTown-LowPolyAssets/Prefabs/Roads/StonePath/StonePath_Turn.prefab");
        if (straightPrefab == null)
        {
            return;
        }

        int roadInnerHalfCells = GroundHalfCells;

        for (int x = -roadInnerHalfCells; x <= roadInnerHalfCells; x++)
        {
            PlacePrefab(straightPrefab, parent, $"Road_N_{x + roadInnerHalfCells:00}", new Vector3(x * CellSize, RoadY, RoadRingCoordinate), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            PlacePrefab(straightPrefab, parent, $"Road_S_{x + roadInnerHalfCells:00}", new Vector3(x * CellSize, RoadY, -RoadRingCoordinate), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        }

        for (int z = -roadInnerHalfCells; z <= roadInnerHalfCells; z++)
        {
            PlacePrefab(straightPrefab, parent, $"Road_E_{z + roadInnerHalfCells:00}", new Vector3(RoadRingCoordinate, RoadY, z * CellSize), Quaternion.identity, Vector3.one);
            PlacePrefab(straightPrefab, parent, $"Road_W_{z + roadInnerHalfCells:00}", new Vector3(-RoadRingCoordinate, RoadY, z * CellSize), Quaternion.identity, Vector3.one);
        }

        if (turnPrefab != null)
        {
            PlacePrefab(turnPrefab, parent, "RoadCorner_NE", new Vector3(RoadRingCoordinate, RoadY, RoadRingCoordinate), Quaternion.Euler(0f, 0f, 0f), Vector3.one);
            PlacePrefab(turnPrefab, parent, "RoadCorner_NW", new Vector3(-RoadRingCoordinate, RoadY, RoadRingCoordinate), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            PlacePrefab(turnPrefab, parent, "RoadCorner_SW", new Vector3(-RoadRingCoordinate, RoadY, -RoadRingCoordinate), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            PlacePrefab(turnPrefab, parent, "RoadCorner_SE", new Vector3(RoadRingCoordinate, RoadY, -RoadRingCoordinate), Quaternion.Euler(0f, 270f, 0f), Vector3.one);
        }
    }

    private static void BuildBuildings(Transform parent)
    {
        Vector3[] positions =
        {
            new Vector3(-10f, PropY, BuildingRingCoordinate),
            new Vector3(-6f, PropY, BuildingRingCoordinate),
            new Vector3(-2f, PropY, BuildingRingCoordinate),
            new Vector3(2f, PropY, BuildingRingCoordinate),
            new Vector3(6f, PropY, BuildingRingCoordinate),
            new Vector3(10f, PropY, BuildingRingCoordinate),
            new Vector3(BuildingRingCoordinate, PropY, 10f),
            new Vector3(BuildingRingCoordinate, PropY, 6f),
            new Vector3(BuildingRingCoordinate, PropY, 2f),
            new Vector3(BuildingRingCoordinate, PropY, -2f),
            new Vector3(BuildingRingCoordinate, PropY, -6f),
            new Vector3(BuildingRingCoordinate, PropY, -10f),
            new Vector3(10f, PropY, -BuildingRingCoordinate),
            new Vector3(6f, PropY, -BuildingRingCoordinate),
            new Vector3(2f, PropY, -BuildingRingCoordinate),
            new Vector3(-2f, PropY, -BuildingRingCoordinate),
            new Vector3(-6f, PropY, -BuildingRingCoordinate),
            new Vector3(-10f, PropY, -BuildingRingCoordinate),
            new Vector3(-BuildingRingCoordinate, PropY, -10f),
            new Vector3(-BuildingRingCoordinate, PropY, -6f),
            new Vector3(-BuildingRingCoordinate, PropY, -2f),
            new Vector3(-BuildingRingCoordinate, PropY, 2f),
            new Vector3(-BuildingRingCoordinate, PropY, 6f),
            new Vector3(-BuildingRingCoordinate, PropY, 10f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject prefab = LoadPrefab(BuildingPrefabs[i % BuildingPrefabs.Length]);
            if (prefab == null)
            {
                continue;
            }

            float scale = i % 5 == 0 ? 0.62f : 0.54f;
            PlacePrefab(prefab, parent, $"Building_{i:00}_{prefab.name}", positions[i], CardinalFacingCenter(positions[i]), Vector3.one * scale);
        }
    }

    private static void BuildTrees(Transform parent)
    {
        Vector3[] positions =
        {
            new Vector3(-TreeRingCoordinate, PropY, TreeRingCoordinate),
            new Vector3(-14f, PropY, TreeRingCoordinate),
            new Vector3(14f, PropY, TreeRingCoordinate),
            new Vector3(TreeRingCoordinate, PropY, TreeRingCoordinate),
            new Vector3(TreeRingCoordinate, PropY, 14f),
            new Vector3(TreeRingCoordinate, PropY, -14f),
            new Vector3(TreeRingCoordinate, PropY, -TreeRingCoordinate),
            new Vector3(14f, PropY, -TreeRingCoordinate),
            new Vector3(-14f, PropY, -TreeRingCoordinate),
            new Vector3(-TreeRingCoordinate, PropY, -TreeRingCoordinate),
            new Vector3(-TreeRingCoordinate, PropY, -14f),
            new Vector3(-TreeRingCoordinate, PropY, 14f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject prefab = LoadPrefab(TreePrefabs[i % TreePrefabs.Length]);
            if (prefab == null)
            {
                continue;
            }

            PlacePrefab(prefab, parent, $"Tree_{i:00}_{prefab.name}", positions[i], Quaternion.Euler(0f, (i % 4) * 90f, 0f), Vector3.one * 0.62f);
        }
    }

    private static void BuildProps(Transform parent)
    {
        string lanternPath = "Assets/CartoonTown-LowPolyAssets/Prefabs/Laterns/Latern_Path.prefab";
        for (int x = -GroundHalfCells; x <= GroundHalfCells; x += 2)
        {
            Vector3 north = GridPosition(x, GroundHalfCells, PropY);
            Vector3 south = GridPosition(x, -GroundHalfCells, PropY);
            PlacePrefab(LoadPrefab(lanternPath), parent, $"Latern_N_{x + GroundHalfCells:00}", north, Quaternion.Euler(0f, 180f, 0f), Vector3.one * 0.7f);
            PlacePrefab(LoadPrefab(lanternPath), parent, $"Latern_S_{x + GroundHalfCells:00}", south, Quaternion.identity, Vector3.one * 0.7f);
        }

        Vector3[] flowerPositions =
        {
            GridPosition(-5, 5, PropY),
            GridPosition(-3, 5, PropY),
            GridPosition(3, 5, PropY),
            GridPosition(5, 5, PropY),
            GridPosition(-5, -5, PropY),
            GridPosition(-3, -5, PropY),
            GridPosition(3, -5, PropY),
            GridPosition(5, -5, PropY),
            GridPosition(-5, 3, PropY),
            GridPosition(5, 3, PropY),
            GridPosition(-5, -3, PropY),
            GridPosition(5, -3, PropY)
        };

        for (int i = 0; i < flowerPositions.Length; i++)
        {
            string path = FlowerPrefabs[i % FlowerPrefabs.Length];
            PlacePrefab(LoadPrefab(path), parent, $"Flower_{i:00}", flowerPositions[i], Quaternion.Euler(0f, (i % 4) * 90f, 0f), Vector3.one * 0.75f);
        }

        PlacePrefab(LoadPrefab("Assets/CartoonTown-LowPolyAssets/Prefabs/Props/Boat.prefab"), parent, "Boat_Decoration", GridPosition(-6, -5, PropY), Quaternion.identity, Vector3.one * 0.55f);
        PlacePrefab(LoadPrefab("Assets/CartoonTown-LowPolyAssets/Prefabs/Props/ColorFlags.prefab"), parent, "ColorFlags_Decoration", GridPosition(0, 5, PropY), Quaternion.identity, Vector3.one * 0.8f);
    }

    private static GameObject InstantiateAsset(string assetPath, Transform parent, string name)
    {
        return PlacePrefab(LoadPrefab(assetPath), parent, name, Vector3.zero, Quaternion.identity, Vector3.one);
    }

    private static GameObject PlacePrefab(GameObject prefab, Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        if (prefab == null || parent == null)
        {
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
        {
            return null;
        }

        instance.name = name;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = localScale;
        return instance;
    }

    private static GameObject LoadPrefab(string assetPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[Environment] Missing asset: {assetPath}");
        }

        return prefab;
    }

    private static Vector3 GridPosition(int x, int z, float y)
    {
        return new Vector3(x * CellSize, y, z * CellSize);
    }

    private static Quaternion CardinalFacingCenter(Vector3 position)
    {
        if (Mathf.Abs(position.x) > Mathf.Abs(position.z))
        {
            return position.x > 0f ? Quaternion.Euler(0f, 270f, 0f) : Quaternion.Euler(0f, 90f, 0f);
        }

        return position.z > 0f ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string name = Path.GetFileName(folder);
        EnsureFolder(parent);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name) && !AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
