using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum RoleId
{
    Duck,
    Rabbit,
    Panda,
    Dog
}

public class RoleDefinition
{
    public RoleId roleId;
    public string displayName;
    public string modelName;
    public string portraitResourceName;
    public string cultureTheme;
    public string backgroundSummary;
    public string skillName;
    public string skillDescription;
}

public static class GameRoleCatalog
{
    private static readonly RoleDefinition[] Roles =
    {
        new RoleDefinition
        {
            roleId = RoleId.Duck,
            displayName = "\u5357\u6e56\u5c0f\u9e2d",
            modelName = "Duck",
            portraitResourceName = "role_2",
            cultureTheme = "\u5357\u6e56\u7ea2\u8239 / \u6c34\u4e61\u6cb3\u9053",
            backgroundSummary = "\u4ece\u5c0f\u5728\u5357\u6e56\u8fb9\u957f\u5927\u7684\u5c0f\u9e2d\uff0c\u719f\u6089\u6c34\u8def\uff0c\u4e5f\u542c\u8fc7\u5f88\u591a\u7ea2\u8239\u6545\u4e8b\u3002",
            skillName = "\u987a\u6c34\u542f\u822a",
            skillDescription = "\u63b7\u9ab0\u7ed3\u679c\u4e3a 1 \u65f6\uff0c\u81ea\u52a8\u6539\u6210 2\u3002"
        },
        new RoleDefinition
        {
            roleId = RoleId.Rabbit,
            displayName = "\u84dd\u5370\u5c0f\u5154",
            modelName = "Rabbit",
            portraitResourceName = "role_0",
            cultureTheme = "\u84dd\u5370\u82b1\u5e03 / \u4f20\u7edf\u67d3\u7ec7",
            backgroundSummary = "\u5728\u84dd\u5370\u82b1\u5e03\u9986\u5e2e\u5fd9\u7684\u5c0f\u5154\u5de5\u5320\uff0c\u8033\u6735\u4e0a\u7cfb\u7740\u84dd\u767d\u82b1\u5e03\u3002",
            skillName = "\u5de7\u624b\u8bae\u4ef7",
            skillDescription = "\u6bcf\u6b21\u4e70\u5730\u540e\u8fd4\u8fd8 200 \u5609\u79be\u5e01\u3002"
        },
        new RoleDefinition
        {
            roleId = RoleId.Panda,
            displayName = "\u7cbd\u9999\u718a\u732b",
            modelName = "Panda",
            portraitResourceName = "role_1",
            cultureTheme = "\u4e94\u82b3\u658b\u7cbd\u5b50 / \u5609\u5174\u98ce\u7269",
            backgroundSummary = "\u80cc\u7740\u7cbd\u5b50\u7bee\u7684\u5c0f\u718a\u732b\u53a8\u5e08\uff0c\u662f\u961f\u4f0d\u91cc\u7684\u8865\u7ed9\u62c5\u5f53\u3002",
            skillName = "\u7cbd\u9999\u8865\u7ed9",
            skillDescription = "\u6bcf\u6b21\u7ecf\u8fc7\u8d77\u70b9\u65f6\uff0c\u989d\u5916\u83b7\u5f97 300 \u5609\u79be\u5e01\u3002"
        },
        new RoleDefinition
        {
            roleId = RoleId.Dog,
            displayName = "\u6708\u6cb3\u5c0f\u72d7",
            modelName = "Dog",
            portraitResourceName = "role_3",
            cultureTheme = "\u6708\u6cb3\u5386\u53f2\u8857\u533a / \u8001\u8857\u5e02\u96c6",
            backgroundSummary = "\u4f4f\u5728\u6708\u6cb3\u8001\u8857\u7684\u5c0f\u72d7\u638c\u67dc\uff0c\u5f88\u4f1a\u62db\u547c\u6e38\u5ba2\u548c\u505a\u5c0f\u751f\u610f\u3002",
            skillName = "\u5e02\u96c6\u638c\u67dc",
            skillDescription = "\u6536\u5230\u5730\u4ea7\u8fc7\u8def\u8d39\u65f6\uff0c\u989d\u5916\u83b7\u5f97 100 \u5609\u79be\u5e01\u3002"
        }
    };

    public static IReadOnlyList<RoleDefinition> AllRoles => Roles;

    public static RoleDefinition Get(RoleId roleId)
    {
        for (int i = 0; i < Roles.Length; i++)
        {
            if (Roles[i].roleId == roleId)
            {
                return Roles[i];
            }
        }

        return Roles[0];
    }

    public static List<RoleId> BuildRoleOrder(RoleId humanRole, int totalPlayers)
    {
        List<RoleId> order = new List<RoleId> { humanRole };

        for (int i = 0; i < Roles.Length; i++)
        {
            if (Roles[i].roleId != humanRole)
            {
                order.Add(Roles[i].roleId);
            }
        }

        int index = 0;
        while (order.Count < totalPlayers)
        {
            order.Add(Roles[index % Roles.Length].roleId);
            index++;
        }

        if (order.Count > totalPlayers)
        {
            order.RemoveRange(totalPlayers, order.Count - totalPlayers);
        }

        return order;
    }
}

public static class GameSessionConfig
{
    public const int MinLevelIndex = 1;
    public const int MaxLevelIndex = 3;
    public const int DefaultLevelIndex = 3;
    private const string DebugDefaultLevelPrefsKey = "Monopoly.DebugDefaultLevel";
    public const int MinPlayerCount = 1;
    public const int MaxPlayerCount = 4;

    public static int SelectedLevel { get; private set; } = DefaultLevelIndex;
    public static bool HasExplicitLevelSelection { get; private set; }
    public static RoleId HumanRole { get; private set; } = RoleId.Duck;
    public static int PlayerCount { get; private set; } = GameRoleCatalog.AllRoles.Count;
    public static List<RoleId> SelectedRoles { get; private set; } = new List<RoleId>();
    public static bool HasExplicitSelection { get; private set; }

    public static void SetLevel(int levelIndex)
    {
        SelectedLevel = Mathf.Clamp(levelIndex, MinLevelIndex, MaxLevelIndex);
        HasExplicitLevelSelection = true;
    }

    public static int SetDebugDefaultLevel(int levelIndex)
    {
        int clampedLevel = Mathf.Clamp(levelIndex, MinLevelIndex, MaxLevelIndex);
        PlayerPrefs.SetInt(DebugDefaultLevelPrefsKey, clampedLevel);
        PlayerPrefs.Save();

        SelectedLevel = clampedLevel;
        HasExplicitLevelSelection = true;
        return clampedLevel;
    }

    public static int ResolveLevel(int fallbackLevel)
    {
        return HasExplicitLevelSelection
            ? SelectedLevel
            : ResolveDefaultLevel(fallbackLevel);
    }

    public static int ResolveDefaultLevel(int fallbackLevel)
    {
        int clampedFallback = Mathf.Clamp(fallbackLevel, MinLevelIndex, MaxLevelIndex);
        int storedLevel = PlayerPrefs.GetInt(DebugDefaultLevelPrefsKey, clampedFallback);
        return Mathf.Clamp(storedLevel, MinLevelIndex, MaxLevelIndex);
    }

    public static void ResetLevelSelection()
    {
        SelectedLevel = ResolveDefaultLevel(DefaultLevelIndex);
        HasExplicitLevelSelection = false;
    }

    public static void SetHumanRole(RoleId roleId)
    {
        HumanRole = roleId;
        SelectedRoles = new List<RoleId>();
        HasExplicitSelection = false;
    }

    public static void SetPlayerCount(int count)
    {
        PlayerCount = Mathf.Clamp(count, MinPlayerCount, MaxPlayerCount);
    }

    public static void SetSelectedRoles(List<RoleId> roles)
    {
        SelectedRoles = SanitizeRoleList(roles);
        HasExplicitSelection = SelectedRoles.Count > 0;
        if (HasExplicitSelection)
        {
            PlayerCount = SelectedRoles.Count;
            HumanRole = SelectedRoles[0];
        }
    }

    public static void SetLocalPlayers(int playerCount, List<RoleId> roles)
    {
        PlayerCount = Mathf.Clamp(playerCount, MinPlayerCount, MaxPlayerCount);
        SelectedRoles = SanitizeRoleList(roles);

        while (SelectedRoles.Count < PlayerCount)
        {
            SelectedRoles.Add(GetFirstUnusedRole(SelectedRoles));
        }

        if (SelectedRoles.Count > PlayerCount)
        {
            SelectedRoles.RemoveRange(PlayerCount, SelectedRoles.Count - PlayerCount);
        }

        HasExplicitSelection = SelectedRoles.Count > 0;
        if (HasExplicitSelection)
        {
            HumanRole = SelectedRoles[0];
        }
    }

    public static void ResetSelection()
    {
        SelectedRoles = new List<RoleId>();
        HasExplicitSelection = false;
        PlayerCount = GameRoleCatalog.AllRoles.Count;
    }

    public static List<RoleId> BuildSessionRoleOrder(int totalPlayers)
    {
        if (HasExplicitSelection && SelectedRoles.Count > 0)
        {
            List<RoleId> order = new List<RoleId>(SelectedRoles);

            while (order.Count < totalPlayers)
            {
                order.Add(GetFirstUnusedRole(order));
            }

            if (order.Count > totalPlayers)
            {
                order.RemoveRange(totalPlayers, order.Count - totalPlayers);
            }

            return order;
        }

        return GameRoleCatalog.BuildRoleOrder(HumanRole, totalPlayers);
    }

    public static List<bool> BuildSessionAIList(int totalPlayers)
    {
        List<bool> aiList = new List<bool>();
        int localPlayerCount = HasExplicitSelection ? Mathf.Min(PlayerCount, totalPlayers) : 1;

        for (int i = 0; i < totalPlayers; i++)
        {
            aiList.Add(i >= localPlayerCount);
        }

        return aiList;
    }

    private static List<RoleId> SanitizeRoleList(List<RoleId> roles)
    {
        List<RoleId> sanitized = new List<RoleId>();
        if (roles == null)
        {
            return sanitized;
        }

        for (int i = 0; i < roles.Count && sanitized.Count < MaxPlayerCount; i++)
        {
            RoleId roleId = roles[i];
            if (!sanitized.Contains(roleId))
            {
                sanitized.Add(roleId);
            }
        }

        return sanitized;
    }

    private static RoleId GetFirstUnusedRole(List<RoleId> usedRoles)
    {
        for (int i = 0; i < GameRoleCatalog.AllRoles.Count; i++)
        {
            RoleId roleId = GameRoleCatalog.AllRoles[i].roleId;
            if (usedRoles == null || !usedRoles.Contains(roleId))
            {
                return roleId;
            }
        }

        int fallbackIndex = usedRoles != null ? usedRoles.Count % GameRoleCatalog.AllRoles.Count : 0;
        return GameRoleCatalog.AllRoles[fallbackIndex].roleId;
    }
}

public class PlayerManager : MonoBehaviour
{
    private sealed class PlayerTurnMarker
    {
        public Transform root;
        public Text arrowText;
        public Text identityText;
        public bool lastIsAI;
        public float baseHeight;
    }

    private sealed class PlayerActiveHighlight
    {
        public Transform root;
        public LineRenderer ring;
        public float baseYOffset;
        public float baseRadius;
    }

    public Transform mapRoot;
    public Transform playerRoot;
    public List<GameObject> playerList = new List<GameObject>();
    public List<bool> playerIsMovingList = new List<bool>();
    public List<int> playerTileIndexList = new List<int>();
    public GameObject playerPrefab;

    public MapManager mapManager;
    public GameManager gameManager;
    public float playerStepDuration = 0.28f;
    public float playerLandingSettleDuration = 0.14f;
    public float playerPackingHalfSize = 0.4f;
    public float playerPackingTransitionDuration = 0.2f;
    public float playerFacingYawOffset;
    public float playerModelScale = 0.5f;
    public bool enableDebugSpaceMove;

    [Header("Role Y Offsets")]
    public float duckYOffset;
    public float rabbitYOffset;
    public float pandaYOffset;
    public float dogYOffset;

    [Header("Turn Marker")]
    public bool showActiveTurnMarker = true;
    public float turnMarkerMinHeight = 1.3f;
    public float turnMarkerVerticalPadding = 0.16f;
    public float turnMarkerBounceAmplitude = 0.08f;
    public float turnMarkerBounceSpeed = 4.6f;
    public float turnMarkerMovePulse = 0.12f;
    public Color humanMarkerColor = new Color(0.98f, 0.80f, 0.25f, 1f);
    public Color humanBadgeColor = new Color(0.92f, 0.40f, 0.20f, 1f);
    public Color aiMarkerColor = new Color(0.34f, 0.73f, 0.98f, 1f);
    public Color aiBadgeColor = new Color(0.42f, 0.39f, 0.90f, 1f);

    [Header("Active Highlight")]
    public bool showActivePlayerHighlight = true;
    public float activeHighlightMinRadius = 0.2f;
    public float activeHighlightMaxRadius = 0.3f;
    public float activeHighlightPadding = 0.02f;
    public float activeHighlightVerticalOffset = 0.09f;
    public float activeHighlightWidth = 0.06f;
    public float activeHighlightPulse = 0.08f;
    public float activeHighlightPulseSpeed = 4.4f;
    public int activeHighlightSegments = 40;
    public Color humanHighlightColor = new Color(1f, 0.84f, 0.24f, 0.98f);
    public Color aiHighlightColor = new Color(0.36f, 0.78f, 1f, 0.96f);

    private readonly Dictionary<int, Coroutine> _packingMoveCoroutines = new Dictionary<int, Coroutine>();
    private readonly List<RoleId> _playerRoles = new List<RoleId>();
    private readonly List<PlayerTurnMarker> _playerTurnMarkers = new List<PlayerTurnMarker>();
    private readonly List<PlayerActiveHighlight> _playerHighlights = new List<PlayerActiveHighlight>();
    private Transform _turnMarkerRoot;
    private Transform _activeHighlightRoot;
    private GameObject _turnMarkerPrefab;
    private Material _activeHighlightMaterial;
    private readonly List<float> _lastAppliedPlayerYOffsets = new List<float>();

    public int mapCount
    {
        get
        {
            if (mapManager != null && mapManager.tileControllers != null && mapManager.tileControllers.Count > 0)
            {
                return mapManager.tileControllers.Count;
            }

            return mapRoot != null ? mapRoot.childCount : 0;
        }
    }

    public void Awake()
    {
        GeneratePlayers();
    }

    public RoleId GetPlayerRoleId(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < _playerRoles.Count)
        {
            return _playerRoles[playerIndex];
        }

        return RoleId.Duck;
    }

    public string GetPlayerDisplayName(int playerIndex)
    {
        return GetPlayerLabel(playerIndex);
    }

    public string GetPlayerRoleDisplayName(int playerIndex)
    {
        return GameRoleCatalog.Get(GetPlayerRoleId(playerIndex)).displayName;
    }

    public string GetPlayerLabel(int playerIndex)
    {
        return IsAIControlled(playerIndex) ? "AI" : $"P{playerIndex + 1}";
    }

    private string GetGeneratedPlayerLabel(int playerIndex)
    {
        if (GameSessionConfig.HasExplicitSelection)
        {
            return playerIndex >= GameSessionConfig.PlayerCount ? "AI" : $"P{playerIndex + 1}";
        }

        if (gameManager != null)
        {
            return gameManager.IsAIPlayer(playerIndex) ? "AI" : $"P{playerIndex + 1}";
        }

        return playerIndex == 0 ? "P1" : "AI";
    }

    public Transform GetPlayerTransform(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerList.Count)
        {
            return null;
        }

        GameObject player = playerList[playerIndex];
        return player != null ? player.transform : null;
    }

    public Vector3 GetPlayerWorldPosition(int playerIndex)
    {
        Transform playerTransform = GetPlayerTransform(playerIndex);
        return playerTransform != null ? playerTransform.position : Vector3.zero;
    }

    public bool IsPlayerMoving(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < playerIsMovingList.Count && playerIsMovingList[playerIndex];
    }

    public void GeneratePlayers()
    {
        ClearTurnMarkers();
        ClearActiveHighlights();

        foreach (GameObject player in playerList)
        {
            if (player != null)
            {
                Destroy(player);
            }
        }

        _packingMoveCoroutines.Clear();
        _playerRoles.Clear();
        playerList.Clear();
        playerIsMovingList.Clear();
        playerTileIndexList.Clear();

        int playerCount = GameSessionConfig.HasExplicitSelection
            ? GameRoleCatalog.AllRoles.Count
            : (gameManager != null ? gameManager.totalPlayers : GameRoleCatalog.AllRoles.Count);
        List<RoleId> roleOrder = GameSessionConfig.BuildSessionRoleOrder(playerCount);

        for (int i = 0; i < playerCount; i++)
        {
            RoleId roleId = roleOrder[i];
            RoleDefinition role = GameRoleCatalog.Get(roleId);
            GameObject playerModel = Resources.Load<GameObject>($"Prefabs/PlayerModels/{role.modelName}");

            GameObject playerInstance = Instantiate(playerPrefab, playerRoot);
            playerInstance.name = $"{GetGeneratedPlayerLabel(i)}: {role.displayName}";

            if (mapRoot != null && mapCount > 0)
            {
                playerInstance.transform.position = GetTileCenterPosition(0) + Vector3.up * GetRoleYOffset(roleId);
            }

            if (playerModel != null)
            {
                GameObject modelInstance = Instantiate(playerModel, playerInstance.transform);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one * playerModelScale;
            }
            else
            {
                Debug.LogWarning($"Player model prefab for {role.modelName} not found. Player {role.displayName} will use the base player prefab only.");
            }

            playerList.Add(playerInstance);
            playerIsMovingList.Add(false);
            playerTileIndexList.Add(0);
            _playerRoles.Add(roleId);
            CreateTurnMarkerForPlayer(i, playerInstance);
            CreateActiveHighlightForPlayer(i, playerInstance);
            ApplyInitialFacing(i);
        }

        RefreshPlayerPositions();
        CapturePlayerYOffsets();
        UpdateTurnMarkers();
        UpdateActivePlayerHighlights();
    }

    public void RefreshPlayerPositions()
    {
        if (mapRoot == null)
        {
            return;
        }

        int generatedMapCount = mapCount;
        for (int tileIndex = 0; tileIndex < generatedMapCount; tileIndex++)
        {
            ArrangePlayersOnTile(tileIndex);
        }
    }

    public void SetPlayerActive(int playerIndex, bool active)
    {
        if (playerIndex < 0 || playerIndex >= playerList.Count)
        {
            return;
        }

        GameObject player = playerList[playerIndex];
        if (player != null)
        {
            player.SetActive(active);
        }
    }

    public void StepPlayer(int playerIndex, int stepCount)
    {
        if (playerIndex < 0 || playerIndex >= playerIsMovingList.Count)
        {
            Debug.LogWarning($"Player {playerIndex} does not exist.");
            return;
        }

        if (playerIsMovingList[playerIndex])
        {
            Debug.LogWarning($"Player {playerIndex} is already moving.");
            return;
        }

        if (stepCount == 0)
        {
            if (gameManager != null)
            {
                gameManager.onEnter(playerIndex, playerTileIndexList[playerIndex]);
            }

            return;
        }

        StopPackingMove(playerIndex);
        StartCoroutine(IEStepPlayer(playerIndex, stepCount));
    }

    public IEnumerator IEStepPlayer(int playerIndex, int stepCount)
    {
        if (playerIndex < 0 || playerIndex >= playerList.Count)
        {
            yield break;
        }

        if (playerList[playerIndex] == null)
        {
            yield break;
        }

        int direction = stepCount >= 0 ? 1 : -1;
        int stepTotal = Mathf.Abs(stepCount);
        playerIsMovingList[playerIndex] = true;

        float stepDuration = Mathf.Max(0.08f, playerStepDuration);
        float settleDuration = Mathf.Max(0.06f, playerLandingSettleDuration);
        float jumpHeight = 0.22f;
        float wobbleAngle = 10f;
        float wobbleFreq = 15f;
        float yawJitter = 1.5f;

        for (int i = 0; i < stepTotal; i++)
        {
            GameObject player = playerList[playerIndex];
            int currentPosIndex = playerTileIndexList[playerIndex];
            int nextPosIndex = WrapTileIndex(currentPosIndex + direction);

            playerTileIndexList[playerIndex] = nextPosIndex;
            ArrangePlayersOnTile(currentPosIndex, playerIndex, smooth: true);
            ArrangePlayersOnTile(nextPosIndex, playerIndex, smooth: true);

            Vector3 nextPos = GetPackedTilePosition(playerIndex, nextPosIndex);
            Vector3 tileDirection = (GetTileCenterPosition(nextPosIndex) - GetTileCenterPosition(currentPosIndex)).normalized;
            ApplyFacingDirection(player.transform, tileDirection);

            Quaternion settleRot = player.transform.rotation;
            float elapsedTime = 0f;
            Vector3 startingPos = player.transform.position;

            while (elapsedTime < stepDuration)
            {
                float t = Mathf.Clamp01(elapsedTime / stepDuration);
                float easedT = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                Vector3 basePos = Vector3.Lerp(startingPos, nextPos, easedT);

                float arc = 4f * t * (1f - t);
                basePos.y += arc * jumpHeight;

                player.transform.position = basePos;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            player.transform.position = nextPos;

            float wobbleTime = 0f;
            float phaseOffset = Random.Range(0f, 6.28f);

            while (wobbleTime < settleDuration)
            {
                float t = Mathf.Clamp01(wobbleTime / settleDuration);
                float damper = 1f - t;
                float phase = (wobbleTime * wobbleFreq) + phaseOffset;

                float pitch = Mathf.Sin(phase) * wobbleAngle * damper;
                float roll = Mathf.Cos(phase * 0.85f) * wobbleAngle * damper;
                float yaw = Mathf.Sin(phase * 0.6f) * yawJitter * damper;

                player.transform.rotation = settleRot * Quaternion.Euler(pitch, yaw, roll);
                wobbleTime += Time.deltaTime;
                yield return null;
            }

            player.transform.rotation = settleRot;
            ArrangePlayersOnTile(nextPosIndex, playerIndex, smooth: true);

            if (direction > 0 && gameManager != null)
            {
                gameManager.onPass(playerIndex, nextPosIndex);
            }
        }

        if (gameManager != null)
        {
            gameManager.onEnter(playerIndex, playerTileIndexList[playerIndex]);
        }

        playerIsMovingList[playerIndex] = false;
    }

    public bool IsAnyVisualMovementActive()
    {
        for (int i = 0; i < playerIsMovingList.Count; i++)
        {
            if (playerIsMovingList[i])
            {
                return true;
            }
        }

        return _packingMoveCoroutines.Count > 0;
    }

    public void Update()
    {
        ApplyPlayerYOffsetChange();
        UpdateTurnMarkers();
        UpdateActivePlayerHighlights();

        if (enableDebugSpaceMove && Input.GetKeyDown(KeyCode.Space))
        {
            if (playerList.Count == 0)
            {
                return;
            }

            StepPlayer(Random.Range(0, playerList.Count), Random.Range(1, 7));
        }
    }

    private int WrapTileIndex(int tileIndex)
    {
        if (mapCount <= 0)
        {
            return 0;
        }

        int wrapped = tileIndex % mapCount;
        if (wrapped < 0)
        {
            wrapped += mapCount;
        }

        return wrapped;
    }

    private Vector3 GetTileCenterPosition(int tileIndex)
    {
        if (mapRoot == null || tileIndex < 0 || tileIndex >= mapCount || tileIndex >= mapRoot.childCount)
        {
            return Vector3.zero;
        }

        return mapRoot.GetChild(tileIndex).position;
    }

    private Vector3 GetPlayerTileCenterPosition(int playerIndex, int tileIndex)
    {
        return GetTileCenterPosition(tileIndex) + Vector3.up * GetPlayerYOffset(playerIndex);
    }

    private List<int> GetPlayersOnTile(int tileIndex)
    {
        List<int> players = new List<int>();

        for (int i = 0; i < playerTileIndexList.Count; i++)
        {
            if (playerTileIndexList[i] == tileIndex && i < playerList.Count && playerList[i] != null && playerList[i].activeSelf)
            {
                players.Add(i);
            }
        }

        players.Sort();
        return players;
    }

    private Vector3 GetPackedOffset(int slotIndex, int playerCount)
    {
        float h = playerPackingHalfSize;

        switch (playerCount)
        {
            case 1:
                return Vector3.zero;
            case 2:
                float diagonalOffset = h / (1f + Mathf.Sqrt(2f));
                return slotIndex == 0
                    ? new Vector3(-diagonalOffset, 0f, -diagonalOffset)
                    : new Vector3(diagonalOffset, 0f, diagonalOffset);
            case 3:
                float triangleOffset = h * 0.5f;
                if (slotIndex == 0) return new Vector3(-triangleOffset, 0f, -triangleOffset);
                if (slotIndex == 1) return new Vector3(triangleOffset, 0f, -triangleOffset);
                return new Vector3(0f, 0f, triangleOffset);
            case 4:
                float gridOffset = h * 0.5f;
                if (slotIndex == 0) return new Vector3(-gridOffset, 0f, -gridOffset);
                if (slotIndex == 1) return new Vector3(gridOffset, 0f, -gridOffset);
                if (slotIndex == 2) return new Vector3(-gridOffset, 0f, gridOffset);
                return new Vector3(gridOffset, 0f, gridOffset);
            default:
                float angle = Mathf.PI * 2f * slotIndex / Mathf.Max(1, playerCount);
                return new Vector3(Mathf.Cos(angle) * h, 0f, Mathf.Sin(angle) * h);
        }
    }

    private Vector3 GetPackedTilePosition(int playerIndex, int tileIndex)
    {
        Vector3 center = GetPlayerTileCenterPosition(playerIndex, tileIndex);
        List<int> players = GetPlayersOnTile(tileIndex);

        int slotIndex = players.IndexOf(playerIndex);
        if (slotIndex < 0)
        {
            players.Add(playerIndex);
            players.Sort();
            slotIndex = players.IndexOf(playerIndex);
        }

        return center + GetPackedOffset(slotIndex, players.Count);
    }

    private void ArrangePlayersOnTile(int tileIndex, int skipPlayerIndex = -1, bool smooth = false)
    {
        List<int> players = GetPlayersOnTile(tileIndex);

        for (int i = 0; i < players.Count; i++)
        {
            int playerIndex = players[i];
            if (playerIndex == skipPlayerIndex) continue;
            if (playerIndex < 0 || playerIndex >= playerList.Count) continue;
            if (playerList[playerIndex] == null) continue;

            MovePlayerToPackedPosition(playerIndex, GetPlayerTileCenterPosition(playerIndex, tileIndex) + GetPackedOffset(i, players.Count), smooth);
        }
    }

    private void ApplyPlayerYOffsetChange()
    {
        while (_lastAppliedPlayerYOffsets.Count < playerList.Count)
        {
            _lastAppliedPlayerYOffsets.Add(GetPlayerYOffset(_lastAppliedPlayerYOffsets.Count));
        }

        if (_lastAppliedPlayerYOffsets.Count > playerList.Count)
        {
            _lastAppliedPlayerYOffsets.RemoveRange(playerList.Count, _lastAppliedPlayerYOffsets.Count - playerList.Count);
        }

        for (int i = 0; i < playerList.Count; i++)
        {
            float currentYOffset = GetPlayerYOffset(i);
            float previousYOffset = _lastAppliedPlayerYOffsets[i];
            if (Mathf.Approximately(previousYOffset, currentYOffset))
            {
                continue;
            }

            GameObject player = playerList[i];
            if (player != null)
            {
                player.transform.position += Vector3.up * (currentYOffset - previousYOffset);
            }

            _lastAppliedPlayerYOffsets[i] = currentYOffset;
        }
    }

    private void CapturePlayerYOffsets()
    {
        _lastAppliedPlayerYOffsets.Clear();
        for (int i = 0; i < playerList.Count; i++)
        {
            _lastAppliedPlayerYOffsets.Add(GetPlayerYOffset(i));
        }
    }

    private float GetPlayerYOffset(int playerIndex)
    {
        return GetRoleYOffset(GetPlayerRoleId(playerIndex));
    }

    private float GetRoleYOffset(RoleId roleId)
    {
        switch (roleId)
        {
            case RoleId.Duck:
                return duckYOffset;
            case RoleId.Rabbit:
                return rabbitYOffset;
            case RoleId.Panda:
                return pandaYOffset;
            case RoleId.Dog:
                return dogYOffset;
            default:
                return 0f;
        }
    }

    private void MovePlayerToPackedPosition(int playerIndex, Vector3 targetPosition, bool smooth)
    {
        if (playerIndex < 0 || playerIndex >= playerList.Count) return;
        if (playerList[playerIndex] == null) return;

        StopPackingMove(playerIndex);

        if (!smooth || playerPackingTransitionDuration <= 0f || !Application.isPlaying)
        {
            playerList[playerIndex].transform.position = targetPosition;
            return;
        }

        if ((playerList[playerIndex].transform.position - targetPosition).sqrMagnitude < 0.0001f)
        {
            playerList[playerIndex].transform.position = targetPosition;
            return;
        }

        _packingMoveCoroutines[playerIndex] = StartCoroutine(IEMovePlayerToPackedPosition(playerIndex, targetPosition));
    }

    private void StopPackingMove(int playerIndex)
    {
        if (_packingMoveCoroutines.TryGetValue(playerIndex, out Coroutine coroutine) && coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        _packingMoveCoroutines.Remove(playerIndex);
    }

    private IEnumerator IEMovePlayerToPackedPosition(int playerIndex, Vector3 targetPosition)
    {
        if (playerIndex < 0 || playerIndex >= playerList.Count) yield break;
        GameObject player = playerList[playerIndex];
        if (player == null) yield break;

        Vector3 startPosition = player.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < playerPackingTransitionDuration)
        {
            if (player == null) yield break;

            float t = Mathf.Clamp01(elapsedTime / playerPackingTransitionDuration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            player.transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (player != null)
        {
            player.transform.position = targetPosition;
        }

        _packingMoveCoroutines.Remove(playerIndex);
    }

    private void ApplyInitialFacing(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerList.Count)
        {
            return;
        }

        GameObject player = playerList[playerIndex];
        if (player == null || mapCount <= 1)
        {
            return;
        }

        Vector3 initialDirection = (GetTileCenterPosition(1) - GetTileCenterPosition(0)).normalized;
        ApplyFacingDirection(player.transform, initialDirection);
    }

    private void ApplyFacingDirection(Transform playerTransform, Vector3 moveDirection)
    {
        if (playerTransform == null)
        {
            return;
        }

        moveDirection.y = 0f;
        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion facingRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up) * Quaternion.Euler(0f, playerFacingYawOffset, 0f);
        playerTransform.rotation = facingRotation;
    }

    private void UpdateTurnMarkers()
    {
        int activePlayerIndex = GetActiveTurnMarkerPlayerIndex();

        for (int i = 0; i < _playerTurnMarkers.Count; i++)
        {
            PlayerTurnMarker marker = _playerTurnMarkers[i];
            if (marker == null || marker.root == null)
            {
                continue;
            }

            bool shouldShow = i == activePlayerIndex
                && i >= 0
                && i < playerList.Count
                && playerList[i] != null
                && playerList[i].activeSelf;

            if (!shouldShow)
            {
                if (marker.root.gameObject.activeSelf)
                {
                    marker.root.gameObject.SetActive(false);
                }

                continue;
            }

            if (!marker.root.gameObject.activeSelf)
            {
                marker.root.gameObject.SetActive(true);
            }

            RefreshTurnMarkerIdentity(i, marker);

            bool isMoving = i < playerIsMovingList.Count && playerIsMovingList[i];
            float bounceSpeed = isMoving ? turnMarkerBounceSpeed * 1.45f : turnMarkerBounceSpeed;
            float bounceAmplitude = isMoving ? turnMarkerBounceAmplitude * 1.4f : turnMarkerBounceAmplitude;
            float bounceOffset = Mathf.Sin((Time.unscaledTime + (i * 0.17f)) * bounceSpeed) * bounceAmplitude;
            float pulseScale = isMoving
                ? 1f + (Mathf.Sin(Time.unscaledTime * 9f) * 0.5f + 0.5f) * turnMarkerMovePulse
                : 1f;

            marker.root.position = playerList[i].transform.position + Vector3.up * (marker.baseHeight + bounceOffset);
            marker.root.rotation = Quaternion.identity;
            marker.root.localScale = Vector3.one * pulseScale;
        }
    }

    private void UpdateActivePlayerHighlights()
    {
        int activePlayerIndex = GetActiveTurnMarkerPlayerIndex();

        for (int i = 0; i < _playerHighlights.Count; i++)
        {
            PlayerActiveHighlight highlight = _playerHighlights[i];
            if (highlight == null || highlight.root == null || highlight.ring == null)
            {
                continue;
            }

            bool shouldShow = showActivePlayerHighlight
                && i == activePlayerIndex
                && i >= 0
                && i < playerList.Count
                && playerList[i] != null
                && playerList[i].activeSelf;

            if (!shouldShow)
            {
                if (highlight.root.gameObject.activeSelf)
                {
                    highlight.root.gameObject.SetActive(false);
                }

                continue;
            }

            if (!highlight.root.gameObject.activeSelf)
            {
                highlight.root.gameObject.SetActive(true);
            }

            bool isAI = IsAIControlled(i);
            Color color = isAI ? aiHighlightColor : humanHighlightColor;
            float pulseScale = 1f + (Mathf.Sin((Time.unscaledTime + i * 0.11f) * activeHighlightPulseSpeed) * 0.5f + 0.5f) * activeHighlightPulse;

            highlight.root.position = playerList[i].transform.position + Vector3.up * highlight.baseYOffset;
            highlight.root.rotation = Quaternion.identity;
            highlight.root.localScale = new Vector3(pulseScale, 1f, pulseScale);

            highlight.ring.startColor = color;
            highlight.ring.endColor = color;
            highlight.ring.startWidth = activeHighlightWidth;
            highlight.ring.endWidth = activeHighlightWidth;
        }
    }

    private int GetActiveTurnMarkerPlayerIndex()
    {
        if (!showActiveTurnMarker || gameManager == null || gameManager.status == Status.GameOver)
        {
            return -1;
        }

        int playerIndex = gameManager.currentPlayerIndex;
        if (playerIndex < 0 || playerIndex >= playerList.Count)
        {
            return -1;
        }

        if (!gameManager.IsPlayerAlive(playerIndex))
        {
            return -1;
        }

        return playerIndex;
    }

    private void EnsureTurnMarkerRoot()
    {
        if (_turnMarkerRoot != null)
        {
            return;
        }

        GameObject rootObject = new GameObject("PlayerTurnMarkers");
        Transform parent = playerRoot != null ? playerRoot : transform;
        _turnMarkerRoot = rootObject.transform;
        _turnMarkerRoot.SetParent(parent, false);
        _turnMarkerRoot.localPosition = Vector3.zero;
        _turnMarkerRoot.localRotation = Quaternion.identity;
        _turnMarkerRoot.localScale = Vector3.one;
    }

    private void EnsureActiveHighlightRoot()
    {
        if (_activeHighlightRoot != null)
        {
            return;
        }

        GameObject rootObject = new GameObject("PlayerActiveHighlights");
        Transform parent = playerRoot != null ? playerRoot : transform;
        _activeHighlightRoot = rootObject.transform;
        _activeHighlightRoot.SetParent(parent, false);
        _activeHighlightRoot.localPosition = Vector3.zero;
        _activeHighlightRoot.localRotation = Quaternion.identity;
        _activeHighlightRoot.localScale = Vector3.one;
    }

    private void EnsureTurnMarkerPrefab()
    {
        if (_turnMarkerPrefab != null)
        {
            return;
        }

        _turnMarkerPrefab = Resources.Load<GameObject>("Prefabs/UI/PlayerTurnMarker");
    }

    private void ClearTurnMarkers()
    {
        _playerTurnMarkers.Clear();

        if (_turnMarkerRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_turnMarkerRoot.gameObject);
        }
        else
        {
            DestroyImmediate(_turnMarkerRoot.gameObject);
        }

        _turnMarkerRoot = null;
    }

    private void ClearActiveHighlights()
    {
        _playerHighlights.Clear();

        if (_activeHighlightRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_activeHighlightRoot.gameObject);
            }
            else
            {
                DestroyImmediate(_activeHighlightRoot.gameObject);
            }

            _activeHighlightRoot = null;
        }

        if (_activeHighlightMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_activeHighlightMaterial);
            }
            else
            {
                DestroyImmediate(_activeHighlightMaterial);
            }

            _activeHighlightMaterial = null;
        }
    }

    private void CreateTurnMarkerForPlayer(int playerIndex, GameObject playerObject)
    {
        if (playerObject == null)
        {
            return;
        }

        EnsureTurnMarkerRoot();
        EnsureTurnMarkerPrefab();

        if (_turnMarkerPrefab == null)
        {
            return;
        }

        while (_playerTurnMarkers.Count <= playerIndex)
        {
            _playerTurnMarkers.Add(null);
        }

        GameObject markerObject = Instantiate(_turnMarkerPrefab, _turnMarkerRoot);
        markerObject.name = playerObject.name + "_TurnMarker";

        Transform markerTransform = markerObject.transform;
        markerTransform.SetParent(_turnMarkerRoot, false);
        markerTransform.gameObject.SetActive(false);

        Text arrowText = markerObject.transform.Find("Pivot/Canvas/ArrowText")?.GetComponent<Text>();
        Text identityText = markerObject.transform.Find("Pivot/Canvas/IdentityText")?.GetComponent<Text>();

        _playerTurnMarkers[playerIndex] = new PlayerTurnMarker
        {
            root = markerTransform,
            arrowText = arrowText,
            identityText = identityText,
            lastIsAI = false,
            baseHeight = CalculateTurnMarkerBaseHeight(playerObject)
        };

        RefreshTurnMarkerIdentity(playerIndex, _playerTurnMarkers[playerIndex]);
    }

    private void CreateActiveHighlightForPlayer(int playerIndex, GameObject playerObject)
    {
        if (playerObject == null)
        {
            return;
        }

        EnsureActiveHighlightRoot();
        while (_playerHighlights.Count <= playerIndex)
        {
            _playerHighlights.Add(null);
        }

        GameObject highlightObject = new GameObject(playerObject.name + "_ActiveHighlight");
        Transform highlightTransform = highlightObject.transform;
        highlightTransform.SetParent(_activeHighlightRoot, false);
        highlightTransform.gameObject.SetActive(false);

        LineRenderer lineRenderer = highlightObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCapVertices = 6;
        lineRenderer.numCornerVertices = 6;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.allowOcclusionWhenDynamic = false;
        lineRenderer.material = GetActiveHighlightMaterial();
        lineRenderer.startWidth = activeHighlightWidth;
        lineRenderer.endWidth = activeHighlightWidth;
        lineRenderer.startColor = humanHighlightColor;
        lineRenderer.endColor = humanHighlightColor;
        lineRenderer.sortingOrder = 500;

        float radius = CalculateHighlightRadius(playerObject);
        ConfigureHighlightRing(lineRenderer, radius);

        _playerHighlights[playerIndex] = new PlayerActiveHighlight
        {
            root = highlightTransform,
            ring = lineRenderer,
            baseYOffset = CalculateHighlightYOffset(playerObject),
            baseRadius = radius
        };
    }

    private float CalculateTurnMarkerBaseHeight(GameObject playerObject)
    {
        if (playerObject == null)
        {
            return turnMarkerMinHeight;
        }

        float highestPoint = playerObject.transform.position.y + turnMarkerMinHeight;
        Renderer[] renderers = playerObject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            highestPoint = Mathf.Max(highestPoint, renderer.bounds.max.y);
        }

        return Mathf.Max(turnMarkerMinHeight, (highestPoint - playerObject.transform.position.y) + turnMarkerVerticalPadding);
    }

    private float CalculateHighlightYOffset(GameObject playerObject)
    {
        if (playerObject == null)
        {
            return activeHighlightVerticalOffset;
        }

        float lowestPoint = playerObject.transform.position.y;
        Renderer[] renderers = playerObject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            lowestPoint = Mathf.Min(lowestPoint, renderer.bounds.min.y);
        }

        return (lowestPoint - playerObject.transform.position.y) + activeHighlightVerticalOffset;
    }

    private float CalculateHighlightRadius(GameObject playerObject)
    {
        if (playerObject == null)
        {
            return activeHighlightMinRadius;
        }

        float radius = activeHighlightMinRadius;
        Renderer[] renderers = playerObject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Vector3 extents = renderer.bounds.extents;
            radius = Mathf.Max(radius, Mathf.Max(extents.x, extents.z) + activeHighlightPadding);
        }

        return Mathf.Clamp(radius, activeHighlightMinRadius, activeHighlightMaxRadius);
    }

    private void ConfigureHighlightRing(LineRenderer lineRenderer, float radius)
    {
        if (lineRenderer == null)
        {
            return;
        }

        int segments = Mathf.Max(12, activeHighlightSegments);
        lineRenderer.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private Material GetActiveHighlightMaterial()
    {
        if (_activeHighlightMaterial != null)
        {
            return _activeHighlightMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            return null;
        }

        _activeHighlightMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        _activeHighlightMaterial.renderQueue = 4000;
        return _activeHighlightMaterial;
    }

    private void RefreshTurnMarkerIdentity(int playerIndex, PlayerTurnMarker marker)
    {
        if (marker == null)
        {
            return;
        }

        bool isAI = IsAIControlled(playerIndex);
        Color markerColor = isAI ? aiMarkerColor : humanMarkerColor;
        Color identityColor = isAI ? aiBadgeColor : humanBadgeColor;

        if (marker.arrowText != null)
        {
            marker.arrowText.color = markerColor;
        }

        if (marker.identityText != null)
        {
            marker.identityText.text = isAI ? "AI" : $"P{playerIndex + 1}";
            marker.identityText.color = identityColor;
        }

        marker.lastIsAI = isAI;
    }

    private bool IsAIControlled(int playerIndex)
    {
        if (gameManager != null)
        {
            return gameManager.IsAIPlayer(playerIndex);
        }

        if (GameSessionConfig.HasExplicitSelection)
        {
            return playerIndex >= GameSessionConfig.PlayerCount;
        }

        return playerIndex != 0;
    }

}
