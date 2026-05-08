using UnityEngine;
using UnityEngine.SceneManagement;

public class MapLoader : MonoBehaviour
{
    [Header("Level Selection")]
    [SerializeField, Range(GameSessionConfig.MinLevelIndex, GameSessionConfig.MaxLevelIndex)]
    private int defaultLevel = GameSessionConfig.DefaultLevelIndex;
    [SerializeField]
    private string[] levelMapResources = { "map1", "map2", "map3" };
    [SerializeField]
    private string fallbackMapResource = "map3";

    [Header("Debug Shortcuts")]
    [SerializeField]
    private bool enableDebugLevelHotkeys = true;
    [SerializeField]
    private bool reloadSceneOnDebugLevelChange = true;

    public TileData[] tileDatas;
    public int CurrentLevel { get; private set; }
    public string CurrentMapResource { get; private set; }


    public void Awake()
    {
        LoadResolvedLevel();
    }

    private void Update()
    {
        if (!enableDebugLevelHotkeys)
        {
            return;
        }

        int requestedLevel = GetDebugRequestedLevel();
        if (requestedLevel == 0)
        {
            return;
        }

        int resolvedLevel = GameSessionConfig.SetDebugDefaultLevel(requestedLevel);
        Debug.Log($"[MapLoader] Debug level set to {resolvedLevel} for this session.");

        if (!reloadSceneOnDebugLevelChange || resolvedLevel == CurrentLevel)
        {
            return;
        }

        SceneTransitionManager.ReloadActiveScene();
    }

    private void LoadResolvedLevel()
    {
        defaultLevel = GameSessionConfig.DefaultLevelIndex;
        LoadLevel(GameSessionConfig.ResolveLevel(GameSessionConfig.DefaultLevelIndex));
    }

    private void LoadLevel(int levelIndex)
    {
        CurrentLevel = Mathf.Clamp(levelIndex, GameSessionConfig.MinLevelIndex, GameSessionConfig.MaxLevelIndex);
        CurrentMapResource = GetMapResourceName(CurrentLevel);

        MapWrapper wrapper = LoadMapWrapper(CurrentMapResource);
        if (wrapper == null && CurrentMapResource != fallbackMapResource)
        {
            Debug.LogWarning($"[MapLoader] Map resource '{CurrentMapResource}' is missing. Falling back to '{fallbackMapResource}'.");
            CurrentMapResource = fallbackMapResource;
            wrapper = LoadMapWrapper(CurrentMapResource);
        }

        tileDatas = wrapper != null && wrapper.datas != null ? wrapper.datas : new TileData[0];
        Debug.Log($"[MapLoader] Level {CurrentLevel} uses map resource '{CurrentMapResource}'.");
    }

    private int GetDebugRequestedLevel()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            return 1;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            return 2;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            return 3;
        }

        return 0;
    }

    private string GetMapResourceName(int levelIndex)
    {
        int arrayIndex = Mathf.Clamp(levelIndex, GameSessionConfig.MinLevelIndex, GameSessionConfig.MaxLevelIndex) - 1;
        if (levelMapResources != null &&
            arrayIndex >= 0 &&
            arrayIndex < levelMapResources.Length &&
            !string.IsNullOrEmpty(levelMapResources[arrayIndex]))
        {
            return levelMapResources[arrayIndex];
        }

        return fallbackMapResource;
    }

    private MapWrapper LoadMapWrapper(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
        {
            return null;
        }

        TextAsset asset = Resources.Load<TextAsset>(resourceName);
        if (asset == null)
        {
            return null;
        }

        return DataLoader.LoadJson<MapWrapper>(resourceName);
    }
}
