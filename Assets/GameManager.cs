using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum Status
{
    PlayerMove,
    PlayerAction,
    AIMove,
    AIAction
}

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    public PlayerManager playerManager;

    [Header("Game Setup")]
    [Range(2, 6)] public int totalPlayers = 2;
    public List<bool> isAIPlayer = new List<bool>(); // true=AI，false=真人

    [Header("Dice")]
    public int diceMin = 1;
    public int diceMax = 6;

    [Header("Money")]
    public int startMoney = 15000;
    public List<int> playerMoneyList = new List<int>(); // 每个玩家的钱，索引=playerIndex

    [Header("Property")]
    public List<int> tileOwnerList = new List<int>();              // 每格归属：-1=无主，否则=playerIndex
    public List<bool> tileUpgradedList = new List<bool>();         // 每格是否升级过
    public List<List<int>> playerOwnedTilesList = new List<List<int>>(); // 每个玩家拥有的格子索引

    [Header("Runtime Status")]
    public Status status;
    public int currentPlayerIndex = 0;
    public int lastDiceValue = 0;

    private bool _rollRequested;
    private bool _endTurnRequested;
    private bool _buyRequested;
    private bool allowHumanInput = false;

    private Coroutine _gameLoopCo;

    // =========================
    // Effect Runtime (Invoke 用)
    // =========================
    private int _fxPlayer;
    private int _fxTileIndex;
    private string _fxKey;
    private string[] _fxArgs;
    private bool _fxIsPass;

    private readonly Dictionary<string, MethodInfo> _fxMethodCache = new Dictionary<string, MethodInfo>();

    // 可选：跳过回合（先存起来，后续你可以在 GameLoop 开头判断）
    public List<int> playerSkipTurnCountList = new List<int>();

    private void Awake()
    {
        if (isAIPlayer == null) isAIPlayer = new List<bool>();
        NormalizeAIList();

        InitMoneyList();
        InitSkipTurnList();
    }

    private void Start()
    {
        StartGame();
    }

    // =========================
    // Init
    // =========================
    private void NormalizeAIList()
    {
        if (isAIPlayer.Count < totalPlayers)
        {
            while (isAIPlayer.Count < totalPlayers) isAIPlayer.Add(false);
        }
        else if (isAIPlayer.Count > totalPlayers)
        {
            isAIPlayer.RemoveRange(totalPlayers, isAIPlayer.Count - totalPlayers);
        }
    }

    private void InitMoneyList()
    {
        if (playerMoneyList == null) playerMoneyList = new List<int>();
        playerMoneyList.Clear();
        for (int i = 0; i < totalPlayers; i++)
            playerMoneyList.Add(startMoney);

        Debug.Log($"[Money] Init: totalPlayers={totalPlayers}, startMoney={startMoney}");
    }

    private void InitSkipTurnList()
    {
        if (playerSkipTurnCountList == null) playerSkipTurnCountList = new List<int>();
        playerSkipTurnCountList.Clear();
        for (int i = 0; i < totalPlayers; i++) playerSkipTurnCountList.Add(0);
    }

    // 初始化地产数据（需等地图生成后）
    private void InitPropertyListsIfNeeded()
    {
        if (playerManager == null || playerManager.mapRoot == null) return;

        int mapCount = playerManager.mapRoot.childCount;
        if (mapCount <= 0) return;

        if (tileOwnerList.Count == mapCount && tileUpgradedList.Count == mapCount && playerOwnedTilesList.Count == totalPlayers)
            return;

        tileOwnerList.Clear();
        tileUpgradedList.Clear();
        for (int i = 0; i < mapCount; i++)
        {
            tileOwnerList.Add(-1);
            tileUpgradedList.Add(false);
        }

        playerOwnedTilesList.Clear();
        for (int p = 0; p < totalPlayers; p++)
            playerOwnedTilesList.Add(new List<int>());

        Debug.Log($"[Property] Init: mapCount={mapCount}, totalPlayers={totalPlayers}");
    }

    // =========================
    // Money APIs
    // =========================
    public int GetMoney(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerMoneyList.Count) return 0;
        return playerMoneyList[playerIndex];
    }

    public void AddMoney(int playerIndex, int delta)
    {
        if (playerIndex < 0 || playerIndex >= playerMoneyList.Count) return;
        playerMoneyList[playerIndex] += delta;
        Debug.Log($"[Money] Player {playerIndex + 1} {(delta >= 0 ? "+" : "")}{delta}, now={playerMoneyList[playerIndex]}");
    }

    public bool TrySpendMoney(int playerIndex, int cost)
    {
        if (playerIndex < 0 || playerIndex >= playerMoneyList.Count) return false;
        if (cost <= 0) return true;

        if (playerMoneyList[playerIndex] < cost)
        {
            Debug.Log($"[Money] Player {playerIndex + 1} not enough. need={cost}, have={playerMoneyList[playerIndex]}");
            return false;
        }

        playerMoneyList[playerIndex] -= cost;
        Debug.Log($"[Money] Player {playerIndex + 1} -{cost}, now={playerMoneyList[playerIndex]}");
        return true;
    }

    private void ForceTransferMoney(int fromPlayer, int toPlayer, int amount, string reason)
    {
        if (amount <= 0) return;
        if (fromPlayer < 0 || fromPlayer >= playerMoneyList.Count) return;
        if (toPlayer < 0 || toPlayer >= playerMoneyList.Count) return;

        int pay = Mathf.Clamp(amount, 0, playerMoneyList[fromPlayer]);
        playerMoneyList[fromPlayer] -= pay;
        playerMoneyList[toPlayer] += pay;

        Debug.Log($"[Transfer] P{fromPlayer + 1} -> P{toPlayer + 1} {pay}/{amount} ({reason})");

        if (pay < amount)
        {
            Debug.Log($"[Transfer] P{fromPlayer + 1} 资金不足，仍欠 {amount - pay}（TODO：破产/抵押）");
        }
    }

    // =========================
    // UI Buttons
    // =========================
    public void RequestRollDice()
    {
        if (!allowHumanInput) return;
        if (IsAI(currentPlayerIndex)) return;
        if (status != Status.PlayerMove) return;
        _rollRequested = true;
    }

    public void RequestEndTurn()
    {
        if (!allowHumanInput) return;
        if (IsAI(currentPlayerIndex)) return;
        if (status != Status.PlayerAction) return;
        _endTurnRequested = true;
    }

    public void RequestBuyOrUpgrade()
    {
        if (!allowHumanInput) return;
        if (IsAI(currentPlayerIndex)) return;
        if (status != Status.PlayerAction) return;
        _buyRequested = true;
    }

    // =========================
    // Queries for UI (buy/upgrade)
    // =========================
    public int GetPlayerCurrentTileIndexSafe(int playerIndex)
    {
        if (playerManager == null) return -1;
        if (playerManager.playerTileIndexList == null) return -1;
        if (playerIndex < 0 || playerIndex >= playerManager.playerTileIndexList.Count) return -1;
        return playerManager.playerTileIndexList[playerIndex];
    }

    public TileController GetCurrentTileController(int playerIndex)
    {
        int tileIndex = GetPlayerCurrentTileIndexSafe(playerIndex);
        if (tileIndex < 0) return null;

        if (playerManager == null || playerManager.mapRoot == null) return null;
        if (tileIndex >= playerManager.mapRoot.childCount) return null;

        return playerManager.mapRoot.GetChild(tileIndex).GetComponent<TileController>();
    }

    public TileController GetTileControllerByIndex(int tileIndex)
    {
        if (playerManager == null || playerManager.mapRoot == null) return null;
        if (tileIndex < 0 || tileIndex >= playerManager.mapRoot.childCount) return null;
        return playerManager.mapRoot.GetChild(tileIndex).GetComponent<TileController>();
    }

    public TileData GetCurrentTileData(int playerIndex)
    {
        TileController tc = GetCurrentTileController(playerIndex);
        return tc != null ? tc.tileData : null;
    }

    public TileData GetTileDataByIndex(int tileIndex)
    {
        TileController tc = GetTileControllerByIndex(tileIndex);
        return tc != null ? tc.tileData : null;
    }

    public bool CanBuyCurrentTile(int playerIndex)
    {
        int tileIndex = GetPlayerCurrentTileIndexSafe(playerIndex);
        if (tileIndex < 0) return false;

        if (tileOwnerList == null || tileOwnerList.Count <= tileIndex) return false;
        if (tileOwnerList[tileIndex] != -1) return false;

        TileData td = GetCurrentTileData(playerIndex);
        if (td == null) return false;

        return td.tileCost != 0;
    }

    public bool CanUpgradeCurrentTile(int playerIndex)
    {
        int tileIndex = GetPlayerCurrentTileIndexSafe(playerIndex);
        if (tileIndex < 0) return false;

        if (tileOwnerList == null || tileOwnerList.Count <= tileIndex) return false;
        if (tileUpgradedList == null || tileUpgradedList.Count <= tileIndex) return false;

        if (tileOwnerList[tileIndex] != playerIndex) return false;
        if (tileUpgradedList[tileIndex]) return false;

        TileController tc = GetCurrentTileController(playerIndex);
        TileData td = tc != null ? tc.tileData : null;
        if (td == null) return false;

        if (td.upgradeCost == 0) return false;
        if (tc != null && tc.hasUpgraded) return false;

        return true;
    }

    public bool CanBuyOrUpgradeCurrentTile(int playerIndex)
    {
        return CanBuyCurrentTile(playerIndex) || CanUpgradeCurrentTile(playerIndex);
    }

    // =========================
    // Game Loop
    // =========================
    public void StartGame()
    {
        if (_gameLoopCo != null) StopCoroutine(_gameLoopCo);
        _gameLoopCo = StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        if (playerManager == null)
        {
            Debug.LogError("[GameManager] playerManager is null!");
            yield break;
        }

        while (playerManager.mapRoot == null || playerManager.mapRoot.childCount == 0)
            yield return null;

        InitPropertyListsIfNeeded();

        Debug.Log($"[GameManager] Game start. totalPlayers={totalPlayers}");
        currentPlayerIndex = 0;

        while (true)
        {
            bool ai = IsAI(currentPlayerIndex);

            if (!ai)
            {
                allowHumanInput = true;

                status = Status.PlayerMove;
                _rollRequested = false;
                Debug.Log($"[Turn] Player {currentPlayerIndex + 1} (Human) - Waiting Roll");
                yield return new WaitUntil(() => _rollRequested);

                lastDiceValue = RollDice();
                Debug.Log($"[Turn] Player {currentPlayerIndex + 1} rolled {lastDiceValue}");

                playerManager.StepPlayer(currentPlayerIndex, lastDiceValue);
                yield return WaitPlayerMoveDone(currentPlayerIndex);

                status = Status.PlayerAction;

                _endTurnRequested = false;
                _buyRequested = false;

                while (true)
                {
                    yield return new WaitUntil(() => _buyRequested || _endTurnRequested);

                    if (_endTurnRequested) break;

                    if (_buyRequested)
                    {
                        _buyRequested = false;
                        TryBuyOrUpgradeCurrentTile(currentPlayerIndex);
                    }
                }

                Debug.Log($"[Turn] Player {currentPlayerIndex + 1} End Turn");
                allowHumanInput = false;
            }
            else
            {
                allowHumanInput = false;

                status = Status.AIMove;
                Debug.Log($"[Turn] Player {currentPlayerIndex + 1} (AI) - Roll & Move");

                yield return new WaitForSeconds(0.25f);

                lastDiceValue = RollDice();
                Debug.Log($"[Turn] AI {currentPlayerIndex + 1} rolled {lastDiceValue}");

                playerManager.StepPlayer(currentPlayerIndex, lastDiceValue);
                yield return WaitPlayerMoveDone(currentPlayerIndex);

                status = Status.AIAction;
                // TODO: AI行动（买地/升级等）先略过
                yield return new WaitForSeconds(0.15f);

                Debug.Log($"[Turn] AI {currentPlayerIndex + 1} End Turn");
            }

            currentPlayerIndex = (currentPlayerIndex + 1) % totalPlayers;
            yield return null;
        }
    }

    // =========================
    // Buy / Upgrade Core
    // =========================
    private void TryBuyOrUpgradeCurrentTile(int playerIndex)
    {
        int tileIndex = GetPlayerCurrentTileIndexSafe(playerIndex);
        if (tileIndex < 0) return;

        TileController tc = GetCurrentTileController(playerIndex);
        if (tc == null || tc.tileData == null)
        {
            Debug.LogWarning($"[Property] TileController or TileData missing at tileIndex={tileIndex}");
            return;
        }

        TileData td = tc.tileData;

        if (CanBuyCurrentTile(playerIndex))
        {
            if (!TrySpendMoney(playerIndex, td.tileCost))
            {
                Debug.Log($"[Property] Player {playerIndex + 1} cannot afford buy {td.tileName}, cost={td.tileCost}");
                return;
            }

            tileOwnerList[tileIndex] = playerIndex;
            tileUpgradedList[tileIndex] = false;
            tc.hasUpgraded = false;

            playerOwnedTilesList[playerIndex].Add(tileIndex);
            Debug.Log($"[Property] Player {playerIndex + 1} bought tile {tileIndex} ({td.tileName}) cost={td.tileCost}");
            return;
        }

        if (CanUpgradeCurrentTile(playerIndex))
        {
            if (!TrySpendMoney(playerIndex, td.upgradeCost))
            {
                Debug.Log($"[Property] Player {playerIndex + 1} cannot afford upgrade {td.tileName}, cost={td.upgradeCost}");
                return;
            }

            tileUpgradedList[tileIndex] = true;
            tc.hasUpgraded = true;

            Debug.Log($"[Property] Player {playerIndex + 1} upgraded tile {tileIndex} ({td.tileName}) upgradeCost={td.upgradeCost}");
            return;
        }

        int owner = (tileOwnerList != null && tileOwnerList.Count > tileIndex) ? tileOwnerList[tileIndex] : -1;
        if (owner != -1 && owner != playerIndex)
        {
            Debug.Log($"[Property] Tile {tileIndex} ({td.tileName}) owned by Player {owner + 1}. TODO: rent logic.");
        }
        else
        {
            Debug.Log($"[Property] Tile {tileIndex} ({td.tileName}) cannot buy/upgrade now.");
        }
    }

    // =========================
    // Move Wait
    // =========================
    private IEnumerator WaitPlayerMoveDone(int playerIndex)
    {
        if (playerManager.playerIsMovingList == null || playerManager.playerIsMovingList.Count <= playerIndex)
        {
            yield return new WaitForSeconds(0.6f);
            yield break;
        }

        int safetyFrames = 30;
        while (!playerManager.playerIsMovingList[playerIndex] && safetyFrames-- > 0)
            yield return null;

        if (!playerManager.playerIsMovingList[playerIndex])
            yield break;

        yield return new WaitUntil(() => playerManager.playerIsMovingList[playerIndex] == false);
    }

    private bool IsAI(int index)
    {
        if (index < 0 || index >= isAIPlayer.Count) return false;
        return isAIPlayer[index];
    }

    private int RollDice()
    {
        return UnityEngine.Random.Range(diceMin, diceMax + 1);
    }

    // ==========================================================
    // ✅ 你要的：onEnter / onPass
    // ==========================================================
    public void onPass(int player, int tileIndex)
    {
        InitPropertyListsIfNeeded();

        TileData td = GetTileDataByIndex(tileIndex);
        if (td == null) return;

        Debug.Log($"[PASS] P{player + 1} -> Tile {tileIndex} {td.tileName} (passEffect='{td.passEffect}')");

        // 1) 执行 passEffect（通过 Invoke 调用 FX_XXX）
        ExecuteEffectByInvoke(td.passEffect, player, tileIndex, isPass: true);

        // 2) 经过格子一般不结算租金（如果你未来想“过路费=经过也收”，可以在这里加）
        // TODO: pass rent logic if needed
    }

    public void onEnter(int player, int tileIndex)
    {
        InitPropertyListsIfNeeded();

        TileController tc = GetTileControllerByIndex(tileIndex);
        TileData td = tc != null ? tc.tileData : null;
        if (td == null) return;

        Debug.Log($"[ENTER] P{player + 1} -> Tile {tileIndex} {td.tileName} (enterEffect='{td.enterEffect}')");

        // 1) 执行 enterEffect（通过 Invoke 调用 FX_XXX）
        ExecuteEffectByInvoke(td.enterEffect, player, tileIndex, isPass: false);

        // 2) 如果是可买地（tileCost>0）且落到别人的地：收租
        //    注：买地/升级由行动阶段按钮处理，这里只做“落地结算租金”
        TryPayRentOnEnter(player, tileIndex, td);
    }

    // =========================
    // Rent Logic
    // =========================
    private void TryPayRentOnEnter(int player, int tileIndex, TileData td)
    {
        if (td == null) return;

        // 只有 tileCost>0 的格子才认为是“资产地”
        if (td.tileCost <= 0) return;

        if (tileOwnerList == null || tileOwnerList.Count <= tileIndex) return;
        int owner = tileOwnerList[tileIndex];

        // 无主 or 自己地 -> 不收租
        if (owner == -1 || owner == player) return;

        int rent = CalcRent(tileIndex, owner, td);
        if (rent <= 0) return;

        ForceTransferMoney(player, owner, rent, $"租金 {td.tileName}");
    }

    private int CalcRent(int tileIndex, int owner, TileData td)
    {
        bool upgraded = (tileUpgradedList != null && tileUpgradedList.Count > tileIndex && tileUpgradedList[tileIndex]);

        // 车站联动：FW03(嘉兴南站) + FW08(嘉兴站) 同时拥有 -> 用 tileIncomeUpgrade
        if (td.tileID == "FW03" || td.tileID == "FW08")
        {
            bool hasFW03 = OwnerHasTileId(owner, "FW03");
            bool hasFW08 = OwnerHasTileId(owner, "FW08");
            if (hasFW03 && hasFW08 && td.tileIncomeUpgrade > 0)
                return td.tileIncomeUpgrade;

            return td.tileIncome;
        }

        // 特例：三塔 / 南湖天地 的 tileIncomeUpgrade 表示“增量”
        if (td.tileID == "FW04" || td.tileID == "FW07")
        {
            if (!upgraded) return td.tileIncome;
            return td.tileIncome + Mathf.Max(0, td.tileIncomeUpgrade);
        }

        // 默认：升级后用 tileIncomeUpgrade，否则 tileIncome
        if (!upgraded) return td.tileIncome;
        if (td.tileIncomeUpgrade > 0) return td.tileIncomeUpgrade;
        return td.tileIncome;
    }

    private bool OwnerHasTileId(int owner, string tileId)
    {
        if (owner < 0 || owner >= playerOwnedTilesList.Count) return false;
        var list = playerOwnedTilesList[owner];
        for (int i = 0; i < list.Count; i++)
        {
            TileData td = GetTileDataByIndex(list[i]);
            if (td != null && td.tileID == tileId) return true;
        }
        return false;
    }

    // =========================
    // Effect Invoke System
    // =========================
    private void ExecuteEffectByInvoke(string effectStr, int player, int tileIndex, bool isPass)
    {
        if (string.IsNullOrWhiteSpace(effectStr)) return;

        // 解析：token, arg0, arg1...
        string[] parts = effectStr.Split(',');
        if (parts.Length <= 0) return;

        string key = parts[0].Trim();
        if (string.IsNullOrEmpty(key)) return;

        var args = new List<string>();
        for (int i = 1; i < parts.Length; i++)
        {
            string a = parts[i].Trim();
            if (!string.IsNullOrEmpty(a)) args.Add(a);
        }

        _fxPlayer = player;
        _fxTileIndex = tileIndex;
        _fxKey = key;
        _fxArgs = args.ToArray();
        _fxIsPass = isPass;

        string methodName = "FX_" + key.Replace(" ", "");

        // 缓存一下，避免每次反射
        if (!_fxMethodCache.TryGetValue(methodName, out var mi))
        {
            mi = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            _fxMethodCache[methodName] = mi; // 允许 mi==null 缓存，避免重复查找
        }

        if (mi == null)
        {
            Debug.LogWarning($"[Effect] Missing method: {methodName} (from '{effectStr}')");
            return;
        }

        // 通过 Invoke 调用（符合你要求）
        Invoke(methodName, 0f);
    }

    private int FX_ArgInt(int idx, int defaultValue = 0)
    {
        if (_fxArgs == null || idx < 0 || idx >= _fxArgs.Length) return defaultValue;
        if (int.TryParse(_fxArgs[idx], out int v)) return v;
        return defaultValue;
    }

    // =========================
    // FX Handlers (先定义函数再说)
    // =========================

    // passEffect: "gain, 1000"
    private void FX_gain()
    {
        int amount = FX_ArgInt(0, 0);
        AddMoney(_fxPlayer, amount);
        UIManager.Instance.Log($"[Effect:gain] P{_fxPlayer + 1} +{amount} (tile={_fxTileIndex})");
    }

    // enterEffect: "question, reward, penalty"
    private void FX_question()
    {
        int reward = FX_ArgInt(0, 0);
        int penalty = FX_ArgInt(1, 0);

        // TODO: 接入 question.json 真正抽题+判题
        bool correct = UnityEngine.Random.value < 0.5f; // 临时：随机对错

        if (correct)
        {
            AddMoney(_fxPlayer, reward);
            UIManager.Instance.Log($"[Effect:question] P{_fxPlayer + 1} 答对 +{reward} (tile={_fxTileIndex})");
        }
        else
        {
            AddMoney(_fxPlayer, -penalty);
            UIManager.Instance.Log($"[Effect:question] P{_fxPlayer + 1} 答错 -{penalty} (tile={_fxTileIndex})");
        }
    }

    // enterEffect: "tool"
    private void FX_tool()
    {
        // TODO: 从道具库抽卡
        UIManager.Instance.Log($"[Effect:tool] P{_fxPlayer + 1} 抽取道具卡 (tile={_fxTileIndex}) TODO");
    }

    // enterEffect: "destiny"
    private void FX_destiny()
    {
        // TODO: 从机会/命运卡池抽卡
        UIManager.Instance.Log($"[Effect:destiny] P{_fxPlayer + 1} 抽取机会/命运卡 (tile={_fxTileIndex}) TODO");
    }

    // enterEffect: "get_from_others, 100"
    private void FX_get_from_others()
    {
        int amount = FX_ArgInt(0, 0);
        int target = PickRandomOtherPlayer(_fxPlayer);
        if (target < 0)
        {
            UIManager.Instance.Log($"[Effect:get_from_others] 没有其他玩家可收取 (P{_fxPlayer + 1})");
            return;
        }

        ForceTransferMoney(target, _fxPlayer, amount, "市集摊位费");
        UIManager.Instance.Log($"[Effect:get_from_others] P{_fxPlayer + 1} 向 P{target + 1} 收取 {amount} (tile={_fxTileIndex})");
    }

    // enterEffect: "skip_turn"
    private void FX_skip_turn()
    {
        if (playerSkipTurnCountList == null || playerSkipTurnCountList.Count != totalPlayers)
            InitSkipTurnList();

        playerSkipTurnCountList[_fxPlayer] += 1;

        // TODO：在 GameLoop 每个玩家回合开始时检查 playerSkipTurnCountList[player]>0 -> 直接跳过
        UIManager.Instance.Log($"[Effect:skip_turn] P{_fxPlayer + 1} 下一回合跳过 (tile={_fxTileIndex}) TODO: enforce in loop");
    }

    // 自定义：enterEffect="E_FW08_ENTER_PROPERTY_STATION_MAIN"
    private void FX_E_FW08_ENTER_PROPERTY_STATION_MAIN()
    {
        // TODO：如果你想做“嘉兴站特殊进站效果”在这里写
        UIManager.Instance.Log($"[Effect:E_FW08_ENTER_PROPERTY_STATION_MAIN] P{_fxPlayer + 1} 进入嘉兴站 (tile={_fxTileIndex}) TODO");
    }

    private int PickRandomOtherPlayer(int self)
    {
        if (totalPlayers <= 1) return -1;

        var candidates = new List<int>();
        for (int i = 0; i < totalPlayers; i++)
        {
            if (i == self) continue;
            candidates.Add(i);
        }

        if (candidates.Count == 0) return -1;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }
}
