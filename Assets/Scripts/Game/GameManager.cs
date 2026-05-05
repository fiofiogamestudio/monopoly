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
    AIAction,
    Resolving,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public const int DefaultStartMoney = 8000;
    public const int DefaultTargetMoneyToWin = 18000;
    private const string DiceRollPanelPrefabPath = "Prefabs/UI/DiceRollPanel";
    private const string BuyablePropertySignText = "\u53ef\u8d2d\u4e70";
    private static readonly Color BuyablePropertySignColor = new Color(0.22f, 0.62f, 0.36f, 1f);

    [Header("Refs")]
    public PlayerManager playerManager;

    [Header("Game Setup")]
    [Range(1, 6)] public int totalPlayers = 4;
    public List<bool> isAIPlayer = new List<bool>();

    [Header("Dice")]
    public int diceMin = 1;
    public int diceMax = 6;
    public DiceRollAnimator diceRollAnimator;

    [Header("Money")]
    public int startMoney = DefaultStartMoney;
    public List<int> playerMoneyList = new List<int>();

    [Header("Victory")]
    public bool enableTargetMoneyVictory = true;
    public int targetMoneyToWin = DefaultTargetMoneyToWin;

    [Header("Property")]
    public List<int> tileOwnerList = new List<int>();
    public List<bool> tileUpgradedList = new List<bool>();
    public List<List<int>> playerOwnedTilesList = new List<List<int>>();

    [Header("Cards")]
    [Range(1, 5)] public int maxToolCardsPerPlayer = 5;

    [Header("AI")]
    [Range(0f, 1f)] public float aiQuestionCorrectChance = 0.65f;
    public int aiBuyReserveMoney = 1200;
    public float aiActionDelay = 0.35f;

    [Header("Runtime Status")]
    public Status status;
    public int turnNumber;
    public int currentPlayerIndex;
    public int lastDiceValue;

    public List<int> playerSkipTurnCountList = new List<int>();
    public List<bool> playerAliveList = new List<bool>();
    public List<int> playerNextRentDiscountList = new List<int>();
    public List<int> playerNextBuyRebateList = new List<int>();
    public List<int> playerSkipProtectionList = new List<int>();
    public List<List<CardData>> playerToolCardsList = new List<List<CardData>>();

    private bool _rollRequested;
    private bool _endTurnRequested;
    private bool _buyRequested;
    private int _requestedToolCardIndex = -1;
    private bool _allowHumanInput;
    private bool _gameEnded;
    private int _activeResolutionCount;

    private Coroutine _gameLoopCo;

    private int _fxPlayer;
    private int _fxTileIndex;
    private string _fxKey;
    private string[] _fxArgs;
    private bool _fxIsPass;

    private readonly Dictionary<string, MethodInfo> _fxMethodCache = new Dictionary<string, MethodInfo>();
    private QuestionData[] _questionBank;
    private CardData[] _toolCardBank;
    private CardData[] _luckCardBank;

    private void Awake()
    {
        LoadGameConfig();
        ApplySessionConfig();
        if (isAIPlayer == null) isAIPlayer = new List<bool>();
        NormalizeAIList();
        InitMoneyList();
        InitRuntimePlayerLists();
        InitSkipTurnList();
    }

    private void Start()
    {
        StartGame();
    }

    public bool IsPlayerAlive(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < playerAliveList.Count && playerAliveList[playerIndex];
    }

    public int GetMoney(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < playerMoneyList.Count ? playerMoneyList[playerIndex] : 0;
    }

    public int GetTileOwnerIndex(int tileIndex)
    {
        return tileIndex >= 0 && tileIndex < tileOwnerList.Count ? tileOwnerList[tileIndex] : -1;
    }

    public bool IsTileUpgraded(int tileIndex)
    {
        return tileIndex >= 0 && tileIndex < tileUpgradedList.Count && tileUpgradedList[tileIndex];
    }

    public int GetTileCurrentRent(int tileIndex, TileData tileData)
    {
        if (tileData == null) return 0;
        if (IsTileUpgraded(tileIndex) && tileData.tileIncomeUpgrade > 0) return tileData.tileIncomeUpgrade;
        return tileData.tileIncome;
    }

    public string GetOwnedPropertySummary(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerOwnedTilesList.Count)
        {
            return "\u623f\u4ea7\uff1a\u65e0";
        }

        List<int> ownedTiles = playerOwnedTilesList[playerIndex];
        if (ownedTiles == null || ownedTiles.Count == 0)
        {
            return "\u623f\u4ea7\uff1a\u65e0";
        }

        List<string> names = new List<string>();
        for (int i = 0; i < ownedTiles.Count; i++)
        {
            int tileIndex = ownedTiles[i];
            if (tileIndex < 0 || tileIndex >= tileOwnerList.Count || tileOwnerList[tileIndex] != playerIndex)
            {
                continue;
            }

            TileData tileData = GetTileDataByIndex(tileIndex);
            if (tileData == null || tileData.tileCost <= 0)
            {
                continue;
            }

            names.Add(tileData.tileName);
        }

        if (names.Count == 0)
        {
            return "\u623f\u4ea7\uff1a\u65e0";
        }

        if (names.Count == 1)
        {
            return $"\u623f\u4ea7\uff1a1\u5904\u00b7{names[0]}";
        }

        if (names.Count == 2)
        {
            return $"\u623f\u4ea7\uff1a2\u5904\u00b7{names[0]}/{names[1]}";
        }

        return $"\u623f\u4ea7\uff1a{names.Count}\u5904\u00b7{names[0]}/{names[1]}\u2026";
    }

    public bool CanHumanRoll()
    {
        return !_gameEnded && !_HasBlockingInteraction() && !_IsCurrentPlayerAI() && status == Status.PlayerMove && _allowHumanInput && IsPlayerAlive(currentPlayerIndex);
    }

    public bool CanHumanBuyCurrentTile()
    {
        return !_gameEnded && !_HasBlockingInteraction() && !_IsCurrentPlayerAI() && status == Status.PlayerAction && _allowHumanInput && CanBuyOrUpgradeCurrentTile(currentPlayerIndex);
    }

    public bool CanHumanEndTurn()
    {
        return !_gameEnded && !_HasBlockingInteraction() && !_IsCurrentPlayerAI() && status == Status.PlayerAction && _allowHumanInput && IsPlayerAlive(currentPlayerIndex);
    }

    public bool CanHumanUseToolCards(int playerIndex)
    {
        return HasHumanToolCardInput(playerIndex) && playerToolCardsList[playerIndex].Count > 0;
    }

    public bool CanHumanUseToolCard(int playerIndex, int cardIndex)
    {
        if (!HasHumanToolCardInput(playerIndex)) return false;
        if (cardIndex < 0 || cardIndex >= playerToolCardsList[playerIndex].Count) return false;

        return IsToolCardUsableInStatus(playerToolCardsList[playerIndex][cardIndex], status);
    }

    private bool HasHumanToolCardInput(int playerIndex)
    {
        bool activeHumanPhase = status == Status.PlayerMove || status == Status.PlayerAction;
        return !_gameEnded && !_HasBlockingInteraction() && !_IsCurrentPlayerAI() && _allowHumanInput && activeHumanPhase && playerIndex == currentPlayerIndex && IsPlayerAlive(playerIndex) && playerIndex >= 0 && playerIndex < playerToolCardsList.Count;
    }

    private bool IsToolCardUsableInStatus(CardData card, Status useStatus)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.effect)) return false;
        if (useStatus == Status.PlayerAction) return true;

        if (useStatus == Status.PlayerMove)
        {
            string effectKey = GetEffectKey(card.effect);
            return effectKey == "skip_protect"
                || effectKey == "move"
                || effectKey == "gain"
                || effectKey == "rent_discount"
                || effectKey == "buy_rebate";
        }

        return false;
    }

    private string GetEffectKey(string effect)
    {
        if (string.IsNullOrWhiteSpace(effect)) return string.Empty;

        int commaIndex = effect.IndexOf(',');
        string key = commaIndex >= 0 ? effect.Substring(0, commaIndex) : effect;
        return key.Trim().Replace(" ", string.Empty).ToLowerInvariant();
    }

    public bool ShouldShowHumanHand()
    {
        return !_gameEnded && !_IsCurrentPlayerAI() && IsPlayerAlive(currentPlayerIndex);
    }

    public List<CardData> GetPlayerToolCards(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < playerToolCardsList.Count ? playerToolCardsList[playerIndex] : new List<CardData>();
    }

#if UNITY_EDITOR
    public CardData EditorGiveRandomToolCardToCurrentPlayer()
    {
        int playerIndex = currentPlayerIndex;
        if (playerIndex < 0 || playerIndex >= playerToolCardsList.Count || !IsPlayerAlive(playerIndex))
        {
            Debug.LogWarning("[Editor] Q \u6d4b\u8bd5\u53d1\u724c\u5931\u8d25\uff1a\u5f53\u524d\u73a9\u5bb6\u65e0\u6548\u6216\u5df2\u51fa\u5c40\u3002");
            return null;
        }

        CardData card = PickToolCard();
        if (card == null)
        {
            Debug.LogWarning("[Editor] Q \u6d4b\u8bd5\u53d1\u724c\u5931\u8d25\uff1a\u9053\u5177\u5361\u6c60\u4e3a\u7a7a\u3002");
            return null;
        }

        AddToolCardToHand(playerIndex, card);
        Debug.Log($"[Editor] Q \u6d4b\u8bd5\u53d1\u724c\uff1a{GetPlayerDisplayName(playerIndex)} \u83b7\u5f97 {card.cardName}");
        return card;
    }

    public QuestionData EditorShowRandomQuestion()
    {
        QuestionData question = PickQuestion();
        if (question == null)
        {
            Debug.LogWarning("[Editor] E \u6d4b\u8bd5\u63d0\u95ee\u5931\u8d25\uff1a\u9898\u5e93\u4e3a\u7a7a\u3002");
            return null;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[Editor] E \u6d4b\u8bd5\u63d0\u95ee\u5931\u8d25\uff1aUIManager \u4e0d\u5b58\u5728\u3002");
            return question;
        }

        Debug.Log($"[Editor] E \u6d4b\u8bd5\u63d0\u95ee\uff1a[{question.category}] {question.text}");
        UIManager.Instance.ShowQuestion(question, selectedIndex =>
        {
            bool correct = selectedIndex == question.answerIndex;
            string selectedText = question.options != null && selectedIndex >= 0 && selectedIndex < question.options.Length
                ? question.options[selectedIndex]
                : "\u672a\u77e5\u9009\u9879";
            string title = correct ? "\u6d4b\u8bd5\uff1a\u56de\u7b54\u6b63\u786e" : "\u6d4b\u8bd5\uff1a\u56de\u7b54\u9519\u8bef";
            string body = $"\u4f60\u9009\u62e9\uff1a{selectedText}\n\u6b63\u786e\u7b54\u6848\uff1a{GetAnswerText(question)}\n{question.explain}";

            Debug.Log($"[Editor] E \u6d4b\u8bd5\u7b54\u9898\uff1a\u9009\u62e9 {selectedText}\uff0c\u6b63\u786e\u7b54\u6848 {GetAnswerText(question)}");
            UIManager.Instance.ShowNotice(title, body, "\u7ee7\u7eed");
        });

        return question;
    }

    public bool EditorGiveMoneyToCurrentPlayer(int amount)
    {
        int playerIndex = currentPlayerIndex;
        if (playerIndex < 0 || playerIndex >= playerMoneyList.Count || !IsPlayerAlive(playerIndex))
        {
            Debug.LogWarning("[Editor] M \u6d4b\u8bd5\u52a0\u94b1\u5931\u8d25\uff1a\u5f53\u524d\u73a9\u5bb6\u65e0\u6548\u6216\u5df2\u51fa\u5c40\u3002");
            return false;
        }

        AddMoney(playerIndex, amount, "\u7f16\u8f91\u5668\u6d4b\u8bd5\u52a0\u94b1");
        Debug.Log($"[Editor] M \u6d4b\u8bd5\u52a0\u94b1\uff1a{GetPlayerDisplayName(playerIndex)} +{amount} \u5609\u79be\u5e01");
        return true;
    }
#endif

    public void RequestRollDice()
    {
        if (CanHumanRoll()) _rollRequested = true;
    }

    public void RequestEndTurn()
    {
        if (CanHumanEndTurn()) _endTurnRequested = true;
    }

    public void RequestBuyOrUpgrade()
    {
        if (CanHumanBuyCurrentTile()) _buyRequested = true;
    }

    public void RequestUseToolCard(int cardIndex)
    {
        if (!CanHumanUseToolCard(currentPlayerIndex, cardIndex)) return;
        _requestedToolCardIndex = cardIndex;
    }

    public int GetPlayerCurrentTileIndexSafe(int playerIndex)
    {
        if (playerManager == null || playerManager.playerTileIndexList == null) return -1;
        return playerIndex >= 0 && playerIndex < playerManager.playerTileIndexList.Count ? playerManager.playerTileIndexList[playerIndex] : -1;
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

    public void StartGame()
    {
        if (_gameLoopCo != null) StopCoroutine(_gameLoopCo);
        _gameLoopCo = StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        if (playerManager == null)
        {
            Debug.LogError("[GameManager] playerManager is null.");
            yield break;
        }

        while (!IsMapGenerated()) yield return null;

        InitPropertyListsIfNeeded();
        RefreshAllTileOwnershipSigns();
        EnsureQuestionBankLoaded();
        EnsureCardBanksLoaded();

        currentPlayerIndex = FindFirstAlivePlayer();
        turnNumber = 0;
        lastDiceValue = 0;
        status = Status.PlayerMove;

        while (!_gameEnded)
        {
            int alivePlayerCount = GetAlivePlayerCount();
            if (alivePlayerCount <= 0)
            {
                FinishGame(-1);
                break;
            }

            // The "last survivor" victory only makes sense when there are at
            // least two players. For a solo session the player has to reach
            // the money target to win.
            if (totalPlayers >= 2 && alivePlayerCount <= 1)
            {
                FinishGame(FindFirstAlivePlayer());
                break;
            }

            if (TryFinishByMoneyTarget(currentPlayerIndex))
            {
                break;
            }

            if (!IsPlayerAlive(currentPlayerIndex))
            {
                AdvanceToNextAlivePlayer();
                yield return null;
                continue;
            }

            turnNumber += 1;
            lastDiceValue = 0;

            if (TryResolveSkipTurnAtTurnStart(currentPlayerIndex))
            {
                AdvanceToNextAlivePlayer();
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            if (_IsCurrentPlayerAI()) yield return RunAITurn(currentPlayerIndex);
            else yield return RunHumanTurn(currentPlayerIndex);

            if (_gameEnded) break;
            AdvanceToNextAlivePlayer();
            yield return null;
        }
    }

    private IEnumerator RunHumanTurn(int playerIndex)
    {
        _allowHumanInput = true;
        status = Status.PlayerMove;
        _rollRequested = false;
        _buyRequested = false;
        _endTurnRequested = false;
        _requestedToolCardIndex = -1;


        while (!_rollRequested && !_gameEnded && IsPlayerAlive(playerIndex))
        {
            yield return new WaitUntil(() => _rollRequested || _gameEnded || !IsPlayerAlive(playerIndex) || _requestedToolCardIndex >= 0);

            if (_requestedToolCardIndex >= 0)
            {
                int cardIndex = _requestedToolCardIndex;
                _requestedToolCardIndex = -1;
                yield return UseToolCard(playerIndex, cardIndex);
            }
        }

        if (_gameEnded || !IsPlayerAlive(playerIndex))
        {
            _allowHumanInput = false;
            yield break;
        }

        lastDiceValue = RollDiceForPlayer(playerIndex);
        yield return PlayDiceRollAnimation(lastDiceValue);
        Debug.Log($"[Turn] {GetPlayerDisplayName(playerIndex)} 闂備胶鎳撻幖顐﹀床閺屻儱鍚规い鎾卞灪閺?{lastDiceValue}");
        yield return MovePlayerAndResolve(playerIndex, lastDiceValue);

        if (_gameEnded || !IsPlayerAlive(playerIndex))
        {
            _allowHumanInput = false;
            yield break;
        }

        status = Status.PlayerAction;

        while (!_gameEnded && IsPlayerAlive(playerIndex))
        {
            yield return new WaitUntil(() => _gameEnded || !IsPlayerAlive(playerIndex) || _buyRequested || _endTurnRequested || _requestedToolCardIndex >= 0);

            if (_gameEnded || !IsPlayerAlive(playerIndex)) break;

            if (_requestedToolCardIndex >= 0)
            {
                int cardIndex = _requestedToolCardIndex;
                _requestedToolCardIndex = -1;
                yield return UseToolCard(playerIndex, cardIndex);
                continue;
            }

            if (_buyRequested)
            {
                _buyRequested = false;
                TryBuyOrUpgradeCurrentTile(playerIndex);
                continue;
            }

            if (_endTurnRequested) break;
        }

        _allowHumanInput = false;
        _buyRequested = false;
        _endTurnRequested = false;
        _requestedToolCardIndex = -1;
        Debug.Log($"[Turn] {GetPlayerDisplayName(playerIndex)} \u7ed3\u675f\u56de\u5408");
    }

    private IEnumerator RunAITurn(int playerIndex)
    {
        _allowHumanInput = false;
        status = Status.AIMove;


        yield return new WaitForSeconds(aiActionDelay);
        lastDiceValue = RollDiceForPlayer(playerIndex);
        yield return PlayDiceRollAnimation(lastDiceValue);


        yield return MovePlayerAndResolve(playerIndex, lastDiceValue);
        if (_gameEnded || !IsPlayerAlive(playerIndex)) yield break;

        status = Status.AIAction;
        yield return new WaitForSeconds(aiActionDelay);
        yield return RunAIActions(playerIndex);
        Debug.Log($"[Turn] {GetPlayerDisplayName(playerIndex)} \u7ed3\u675f\u56de\u5408");
    }

    private IEnumerator RunAIActions(int playerIndex)
    {
        int safety = 6;

        while (!_gameEnded && IsPlayerAlive(playerIndex) && safety-- > 0)
        {
            bool acted = false;

            int prepCardIndex = PickAIBuyPrepCard(playerIndex);
            if (prepCardIndex >= 0)
            {
                yield return UseToolCard(playerIndex, prepCardIndex);
                acted = true;
                if (_gameEnded || !IsPlayerAlive(playerIndex)) yield break;
            }

            if ((CanBuyCurrentTile(playerIndex) && ShouldAIBuyCurrentTile(playerIndex)) || ShouldAIUpgradeCurrentTile(playerIndex))
            {
                TryBuyOrUpgradeCurrentTile(playerIndex);
                acted = true;
                if (_gameEnded || !IsPlayerAlive(playerIndex)) yield break;
                yield return new WaitForSeconds(aiActionDelay);
            }

            int actionCardIndex = PickAIToolCardToUse(playerIndex);
            if (actionCardIndex >= 0)
            {
                yield return UseToolCard(playerIndex, actionCardIndex);
                acted = true;
                if (_gameEnded || !IsPlayerAlive(playerIndex)) yield break;
                yield return new WaitForSeconds(aiActionDelay);
                continue;
            }

            if (!acted) break;
        }
    }

    private IEnumerator MovePlayerAndResolve(int playerIndex, int stepCount)
    {
        if (!IsPlayerAlive(playerIndex)) yield break;

        int baseline = _activeResolutionCount;
        playerManager.StepPlayer(playerIndex, stepCount);
        yield return WaitPlayerMoveDone(playerIndex);
        yield return WaitForResolutionCount(baseline);
    }

    private IEnumerator UseToolCard(int playerIndex, int cardIndex)
    {
        if (!IsPlayerAlive(playerIndex)) yield break;
        if (playerIndex < 0 || playerIndex >= playerToolCardsList.Count) yield break;
        if (cardIndex < 0 || cardIndex >= playerToolCardsList[playerIndex].Count) yield break;

        Status resumeStatus = status;
        CardData card = playerToolCardsList[playerIndex][cardIndex];
        playerToolCardsList[playerIndex].RemoveAt(cardIndex);
        Debug.Log($"[Tool] {GetPlayerDisplayName(playerIndex)} \u4f7f\u7528\u9053\u5177\u5361\uff1a{card.cardName}");

        int baseline = _activeResolutionCount;
        ExecuteEffectByInvoke(card.effect, playerIndex, GetPlayerCurrentTileIndexSafe(playerIndex), false);
        yield return WaitForResolutionCount(baseline);

        if (!_gameEnded && IsPlayerAlive(playerIndex))
        {
            if (IsAI(playerIndex))
            {
                status = Status.AIAction;
            }
            else
            {
                status = resumeStatus == Status.PlayerMove ? Status.PlayerMove : Status.PlayerAction;
            }
        }
    }

    private IEnumerator WaitPlayerMoveDone(int playerIndex)
    {
        if (playerManager.playerIsMovingList == null || playerManager.playerIsMovingList.Count <= playerIndex)
        {
            yield return new WaitForSeconds(0.2f);
            yield break;
        }

        int safetyFrames = 30;
        while (!playerManager.playerIsMovingList[playerIndex] && safetyFrames-- > 0) yield return null;
        if (!playerManager.playerIsMovingList[playerIndex]) yield break;
        yield return new WaitUntil(() => playerManager.playerIsMovingList[playerIndex] == false);
        yield return new WaitUntil(() => !playerManager.IsAnyVisualMovementActive());
        yield return new WaitForSeconds(0.05f);
    }

    private IEnumerator WaitForResolutionCount(int maxCount)
    {
        yield return new WaitUntil(() => _activeResolutionCount <= maxCount);
    }

    public void onPass(int player, int tileIndex)
    {
        if (_gameEnded || !IsPlayerAlive(player)) return;

        InitPropertyListsIfNeeded();
        TileData td = GetTileDataByIndex(tileIndex);
        if (td == null) return;

        Debug.Log($"[PASS] P{player + 1} -> {td.tileName} ({td.passEffect})");
        ExecuteEffectByInvoke(td.passEffect, player, tileIndex, true);
    }

    public void onEnter(int player, int tileIndex)
    {
        if (_gameEnded || !IsPlayerAlive(player)) return;

        InitPropertyListsIfNeeded();
        TileController tc = GetTileControllerByIndex(tileIndex);
        TileData td = tc != null ? tc.tileData : null;
        if (td == null) return;

        Debug.Log($"[ENTER] P{player + 1} -> {td.tileName} ({td.enterEffect})");
        ExecuteEffectByInvoke(td.enterEffect, player, tileIndex, false);
        TryPayRentOnEnter(player, tileIndex, td);
    }

    private void NormalizeAIList()
    {
        if (GameSessionConfig.HasExplicitSelection)
        {
            isAIPlayer = GameSessionConfig.BuildSessionAIList(totalPlayers);
            return;
        }

        if (isAIPlayer.Count == 0)
        {
            for (int i = 0; i < totalPlayers; i++) isAIPlayer.Add(i != 0);
            return;
        }

        if (isAIPlayer.Count < totalPlayers)
        {
            while (isAIPlayer.Count < totalPlayers) isAIPlayer.Add(isAIPlayer.Count != 0);
        }
        else if (isAIPlayer.Count > totalPlayers)
        {
            isAIPlayer.RemoveRange(totalPlayers, isAIPlayer.Count - totalPlayers);
        }
    }

    private void InitMoneyList()
    {
        playerMoneyList.Clear();
        for (int i = 0; i < totalPlayers; i++) playerMoneyList.Add(startMoney);
    }

    private void InitRuntimePlayerLists()
    {
        playerAliveList.Clear();
        playerNextRentDiscountList.Clear();
        playerNextBuyRebateList.Clear();
        playerSkipProtectionList.Clear();
        playerToolCardsList.Clear();

        for (int i = 0; i < totalPlayers; i++)
        {
            playerAliveList.Add(true);
            playerNextRentDiscountList.Add(0);
            playerNextBuyRebateList.Add(0);
            playerSkipProtectionList.Add(0);
            playerToolCardsList.Add(new List<CardData>());
        }
    }

    private void InitSkipTurnList()
    {
        playerSkipTurnCountList.Clear();
        for (int i = 0; i < totalPlayers; i++) playerSkipTurnCountList.Add(0);
    }

    private void LoadGameConfig()
    {
        startMoney = DefaultStartMoney;
        enableTargetMoneyVictory = true;
        targetMoneyToWin = DefaultTargetMoneyToWin;

        try
        {
            GameConfigData config = DataLoader.LoadJson<GameConfigData>("game_config");
            if (config != null)
            {
                if (config.startMoney > 0)
                {
                    startMoney = config.startMoney;
                }

                enableTargetMoneyVictory = config.enableTargetMoneyVictory;

                if (config.targetMoneyToWin > 0)
                {
                    targetMoneyToWin = config.targetMoneyToWin;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Config] Failed to load game_config.json: {e.Message}");
        }

        startMoney = Mathf.Max(1000, startMoney);
        if (enableTargetMoneyVictory)
        {
            targetMoneyToWin = Mathf.Max(startMoney + 1000, targetMoneyToWin);
        }

    }

    private void ApplySessionConfig()
    {
        if (!GameSessionConfig.HasExplicitSelection)
        {
            totalPlayers = Mathf.Max(1, totalPlayers);
            return;
        }

        totalPlayers = GameRoleCatalog.AllRoles.Count;
    }

    private void InitPropertyListsIfNeeded()
    {
        int mapCount = GetGeneratedMapCount();
        if (mapCount <= 0) return;

        if (tileOwnerList.Count == mapCount && tileUpgradedList.Count == mapCount && playerOwnedTilesList.Count == totalPlayers) return;

        tileOwnerList.Clear();
        tileUpgradedList.Clear();
        for (int i = 0; i < mapCount; i++)
        {
            tileOwnerList.Add(-1);
            tileUpgradedList.Add(false);
        }

        playerOwnedTilesList.Clear();
        for (int p = 0; p < totalPlayers; p++) playerOwnedTilesList.Add(new List<int>());
        RefreshAllTileOwnershipSigns();
    }

    private int GetGeneratedMapCount()
    {
        if (playerManager != null && playerManager.mapManager != null && playerManager.mapManager.tileControllers != null && playerManager.mapManager.tileControllers.Count > 0)
        {
            return playerManager.mapManager.tileControllers.Count;
        }

        return playerManager != null && playerManager.mapRoot != null ? playerManager.mapRoot.childCount : 0;
    }

    private bool IsMapGenerated()
    {
        if (playerManager == null || playerManager.mapRoot == null) return false;
        MapManager mapManager = playerManager.mapManager;
        if (mapManager == null) return playerManager.mapRoot.childCount > 0;

        int expectedCount = 0;
        if (mapManager.mapLoader != null && mapManager.mapLoader.tileDatas != null) expectedCount = mapManager.mapLoader.tileDatas.Length;
        if (expectedCount <= 0) return mapManager.tileControllers != null && mapManager.tileControllers.Count > 0;
        return mapManager.tileControllers != null && mapManager.tileControllers.Count >= expectedCount;
    }

    public TileController GetCurrentTileController(int playerIndex)
    {
        return GetTileControllerByIndex(GetPlayerCurrentTileIndexSafe(playerIndex));
    }

    public TileController GetTileControllerByIndex(int tileIndex)
    {
        if (tileIndex < 0) return null;

        if (playerManager != null && playerManager.mapManager != null && playerManager.mapManager.tileControllers != null)
        {
            List<TileController> controllers = playerManager.mapManager.tileControllers;
            if (tileIndex < controllers.Count && controllers[tileIndex] != null) return controllers[tileIndex];
        }

        if (playerManager == null || playerManager.mapRoot == null || tileIndex >= playerManager.mapRoot.childCount) return null;

        Transform slot = playerManager.mapRoot.GetChild(tileIndex);
        TileController tileController = slot.GetComponent<TileController>();
        if (tileController != null) return tileController;
        return slot.GetComponentInChildren<TileController>();
    }

    public bool CanBuyCurrentTile(int playerIndex)
    {
        if (!IsPlayerAlive(playerIndex)) return false;

        int tileIndex = GetPlayerCurrentTileIndexSafe(playerIndex);
        if (tileIndex < 0) return false;
        if (tileOwnerList == null || tileOwnerList.Count <= tileIndex) return false;
        if (tileOwnerList[tileIndex] != -1) return false;

        TileData td = GetCurrentTileData(playerIndex);
        return td != null && td.tileCost > 0 && GetMoney(playerIndex) >= td.tileCost;
    }

    public bool CanUpgradeCurrentTile(int playerIndex)
    {
        int tileIndex = GetPlayerCurrentTileIndexSafe(playerIndex);
        if (tileIndex < 0) return false;
        if (tileOwnerList == null || tileOwnerList.Count <= tileIndex) return false;
        if (tileUpgradedList == null || tileUpgradedList.Count <= tileIndex) return false;
        if (tileOwnerList[tileIndex] != playerIndex || tileUpgradedList[tileIndex]) return false;

        TileController tc = GetCurrentTileController(playerIndex);
        TileData td = tc != null ? tc.tileData : null;
        if (td == null || td.upgradeCost <= 0) return false;
        return tc == null || !tc.hasUpgraded;
    }

    public bool CanBuyOrUpgradeCurrentTile(int playerIndex)
    {
        return CanBuyCurrentTile(playerIndex) || CanUpgradeCurrentTile(playerIndex);
    }

    public void AddMoney(int playerIndex, int delta, string reason = "")
    {
        if (!IsPlayerAlive(playerIndex) || playerIndex < 0 || playerIndex >= playerMoneyList.Count) return;

        if (delta >= 0)
        {
            int oldMoney = playerMoneyList[playerIndex];
            playerMoneyList[playerIndex] += delta;
            QueueMoneyChangeFeedback(playerIndex, oldMoney, playerMoneyList[playerIndex]);
            TryFinishByMoneyTarget(playerIndex);
            Debug.Log($"[Money] {GetPlayerDisplayName(playerIndex)} +{delta} {reason}闂備焦瀵х粙鎴︽嚐椤栨粎绀婇悗锝庡枟閺?{playerMoneyList[playerIndex]}");
            return;
        }

        SpendOrBankrupt(playerIndex, -delta, reason, -1, false);
    }

    public bool TrySpendMoney(int playerIndex, int cost, string reason = "")
    {
        if (!IsPlayerAlive(playerIndex) || playerIndex < 0 || playerIndex >= playerMoneyList.Count) return false;
        if (cost <= 0) return true;
        if (playerMoneyList[playerIndex] < cost)
        {
            Debug.Log($"[Money] {GetPlayerDisplayName(playerIndex)} 闂佽崵濮嶉崘顭戜純闂侀潻绲块崑鎾剁矙婢跺鍚嬮柛顐ｇ箓閺嬫瑩姊洪幐搴ｂ槈闁活厼鍊搁妴?{cost}闂備焦瀵х粙鎴︽儔婵傚摜宓侀柛銉墯閺?{playerMoneyList[playerIndex]}");
            return false;
        }

        int oldMoney = playerMoneyList[playerIndex];
        playerMoneyList[playerIndex] -= cost;
        QueueMoneyChangeFeedback(playerIndex, oldMoney, playerMoneyList[playerIndex]);
        Debug.Log($"[Money] {GetPlayerDisplayName(playerIndex)} -{cost} {reason}闂備焦瀵х粙鎴︽嚐椤栨粎绀婇悗锝庡枟閺?{playerMoneyList[playerIndex]}");
        return true;
    }

    private bool SpendOrBankrupt(int playerIndex, int amount, string reason, int receiverIndex, bool receiverGetsRentBonus)
    {
        if (!IsPlayerAlive(playerIndex) || amount <= 0) return true;

        if (playerMoneyList[playerIndex] >= amount)
        {
            int payerOldMoney = playerMoneyList[playerIndex];
            playerMoneyList[playerIndex] -= amount;
            QueueMoneyChangeFeedback(playerIndex, payerOldMoney, playerMoneyList[playerIndex]);

            if (receiverIndex >= 0 && IsPlayerAlive(receiverIndex))
            {
                int receiverOldMoney = playerMoneyList[receiverIndex];
                playerMoneyList[receiverIndex] += amount;
                QueueMoneyChangeFeedback(receiverIndex, receiverOldMoney, playerMoneyList[receiverIndex]);
                Debug.Log($"[Transfer] {GetPlayerDisplayName(playerIndex)} -> {GetPlayerDisplayName(receiverIndex)} {amount} ({reason})");
                if (receiverGetsRentBonus) ApplyRentCollectorBonus(receiverIndex);
                TryFinishByMoneyTarget(receiverIndex);
            }
            else
            {
                Debug.Log($"[Money] {GetPlayerDisplayName(playerIndex)} -{amount} {reason}闂備焦瀵х粙鎴︽嚐椤栨粎绀婇悗锝庡枟閺?{playerMoneyList[playerIndex]}");
            }

            return true;
        }


        BankruptPlayer(playerIndex, receiverIndex, reason, receiverGetsRentBonus);
        return false;
    }

    private void BankruptPlayer(int playerIndex, int receiverIndex, string reason, bool receiverGetsRentBonus)
    {
        if (!IsPlayerAlive(playerIndex)) return;

        int remainingMoney = playerMoneyList[playerIndex];
        playerMoneyList[playerIndex] = 0;
        QueueMoneyChangeFeedback(playerIndex, remainingMoney, 0);

        if (receiverIndex >= 0 && IsPlayerAlive(receiverIndex) && remainingMoney > 0)
        {
            int receiverOldMoney = playerMoneyList[receiverIndex];
            playerMoneyList[receiverIndex] += remainingMoney;
            QueueMoneyChangeFeedback(receiverIndex, receiverOldMoney, playerMoneyList[receiverIndex]);
            Debug.Log($"[Transfer] {GetPlayerDisplayName(playerIndex)} 闂備焦妞块崰姘辨崲濠靛浂娈介柛銉㈡櫇閻捇鎮规担鑺ョ彧闁?{remainingMoney} 闂?{GetPlayerDisplayName(receiverIndex)}");
            if (receiverGetsRentBonus) ApplyRentCollectorBonus(receiverIndex);
            TryFinishByMoneyTarget(receiverIndex);
        }

        ReleasePlayerProperties(playerIndex);
        playerToolCardsList[playerIndex].Clear();
        playerNextRentDiscountList[playerIndex] = 0;
        playerNextBuyRebateList[playerIndex] = 0;
        playerSkipProtectionList[playerIndex] = 0;
        playerSkipTurnCountList[playerIndex] = 0;
        playerAliveList[playerIndex] = false;

        if (playerManager != null)
        {
            playerManager.SetPlayerActive(playerIndex, false);
            playerManager.RefreshPlayerPositions();
        }

        Debug.Log($"[Bankrupt] {GetPlayerDisplayName(playerIndex)} \u5df2\u51fa\u5c40\uff0c\u539f\u56e0\uff1a{reason}");
        int alivePlayerCount = GetAlivePlayerCount();
        if (alivePlayerCount <= 0)
        {
            FinishGame(-1);
        }
        else if (totalPlayers >= 2 && alivePlayerCount <= 1)
        {
            FinishGame(FindFirstAlivePlayer());
        }
    }

    private void ReleasePlayerProperties(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerOwnedTilesList.Count) return;

        List<int> ownedTiles = playerOwnedTilesList[playerIndex];
        for (int i = 0; i < ownedTiles.Count; i++)
        {
            int tileIndex = ownedTiles[i];
            if (tileIndex >= 0 && tileIndex < tileOwnerList.Count) tileOwnerList[tileIndex] = -1;
            if (tileIndex >= 0 && tileIndex < tileUpgradedList.Count) tileUpgradedList[tileIndex] = false;

            TileController tileController = GetTileControllerByIndex(tileIndex);
            if (tileController != null) tileController.hasUpgraded = false;
            RefreshTileOwnershipVisual(tileIndex);
        }

        ownedTiles.Clear();
    }

    private void AdvanceToNextAlivePlayer()
    {
        if (GetAlivePlayerCount() <= 0) return;

        int nextPlayer = currentPlayerIndex;
        for (int i = 0; i < totalPlayers; i++)
        {
            nextPlayer = (nextPlayer + 1) % totalPlayers;
            if (IsPlayerAlive(nextPlayer))
            {
                currentPlayerIndex = nextPlayer;
                return;
            }
        }
    }

    private int FindFirstAlivePlayer()
    {
        for (int i = 0; i < totalPlayers; i++)
        {
            if (IsPlayerAlive(i)) return i;
        }

        return 0;
    }

    private int GetAlivePlayerCount()
    {
        int count = 0;
        for (int i = 0; i < playerAliveList.Count; i++)
        {
            if (playerAliveList[i]) count++;
        }

        return count;
    }

    private void FinishGame(int winnerIndex, string customMessage = null)
    {
        if (_gameEnded) return;

        _gameEnded = true;
        status = Status.GameOver;
        _allowHumanInput = false;
        string message = !string.IsNullOrEmpty(customMessage)
            ? customMessage
            : (winnerIndex >= 0 && IsPlayerAlive(winnerIndex)
                ? $"{GetPlayerDisplayName(winnerIndex)} \u83b7\u5f97\u4e86\u6700\u7ec8\u80dc\u5229\u3002"
                : "\u6240\u6709\u73a9\u5bb6\u90fd\u5df2\u51fa\u5c40\u3002");
        Debug.Log($"[GameOver] {message}");
        if (UIManager.Instance != null) UIManager.Instance.ShowNotice("\u6e38\u620f\u7ed3\u675f", message, "\u786e\u5b9a");
    }

    private bool TryFinishByMoneyTarget(int playerIndex)
    {
        if (_gameEnded || !enableTargetMoneyVictory || targetMoneyToWin <= 0)
        {
            return false;
        }

        if (!IsPlayerAlive(playerIndex) || playerIndex < 0 || playerIndex >= playerMoneyList.Count)
        {
            return false;
        }

        if (playerMoneyList[playerIndex] < targetMoneyToWin)
        {
            return false;
        }

        FinishGame(playerIndex, $"{GetPlayerDisplayName(playerIndex)} \u7387\u5148\u8fbe\u5230 {targetMoneyToWin} \u5609\u79be\u5e01\uff0c\u83b7\u5f97\u80dc\u5229\u3002");
        return true;
    }

    private bool TryResolveSkipTurnAtTurnStart(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerSkipTurnCountList.Count) return false;
        if (playerSkipTurnCountList[playerIndex] <= 0) return false;

        if (TryConsumeSkipProtection(playerIndex))
        {
            playerSkipTurnCountList[playerIndex] = Mathf.Max(0, playerSkipTurnCountList[playerIndex] - 1);

            return false;
        }

        playerSkipTurnCountList[playerIndex] -= 1;

        return true;
    }

    private bool TryConsumeSkipProtection(int playerIndex)
    {
        if (playerSkipProtectionList[playerIndex] > 0)
        {
            playerSkipProtectionList[playerIndex] -= 1;
            return true;
        }

        int cardIndex = FindToolCardIndexByEffect(playerIndex, "skip_protect");
        if (cardIndex < 0) return false;

        CardData card = playerToolCardsList[playerIndex][cardIndex];
        playerToolCardsList[playerIndex].RemoveAt(cardIndex);
        Debug.Log($"[Tool] {GetPlayerDisplayName(playerIndex)} \u81ea\u52a8\u4f7f\u7528\u9053\u5177\u5361\uff1a{card.cardName}");
        return true;
    }

    private int FindToolCardIndexByEffect(int playerIndex, string effectPrefix)
    {
        if (playerIndex < 0 || playerIndex >= playerToolCardsList.Count) return -1;

        List<CardData> cards = playerToolCardsList[playerIndex];
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null && !string.IsNullOrEmpty(cards[i].effect) && cards[i].effect.StartsWith(effectPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private bool TryBuyOrUpgradeCurrentTile(int playerIndex)
    {
        if (!IsPlayerAlive(playerIndex)) return false;

        int tileIndex = GetPlayerCurrentTileIndexSafe(playerIndex);
        if (tileIndex < 0) return false;

        TileController tc = GetCurrentTileController(playerIndex);
        if (tc == null || tc.tileData == null) return false;
        TileData td = tc.tileData;

        if (CanBuyCurrentTile(playerIndex))
        {
            if (!TrySpendMoney(playerIndex, td.tileCost, $"闂佽崵濮甸崝锕傚储濞差亜绠?{td.tileName}")) return false;

            tileOwnerList[tileIndex] = playerIndex;
            tileUpgradedList[tileIndex] = false;
            tc.hasUpgraded = false;
            if (!playerOwnedTilesList[playerIndex].Contains(tileIndex)) playerOwnedTilesList[playerIndex].Add(tileIndex);
            RefreshTileOwnershipVisual(tileIndex);

            Debug.Log($"[Property] {GetPlayerDisplayName(playerIndex)} 闂佽崵濮甸崝锕傚储濞差亜绠梺顒€绉甸弲?{td.tileName}闂備焦瀵х粙鎴︽儗娴ｈ鍙忛柟鎯板Г閺?{td.tileCost}");
            ApplyPropertyPurchaseBonuses(playerIndex);
            return true;
        }

        if (CanUpgradeCurrentTile(playerIndex))
        {
            if (!TrySpendMoney(playerIndex, td.upgradeCost, $"闂備礁鎲￠〃鍛洪妸锔绘?{td.tileName}")) return false;

            tileUpgradedList[tileIndex] = true;
            tc.hasUpgraded = true;
            Debug.Log($"[Property] {GetPlayerDisplayName(playerIndex)} 闂備礁鎲￠〃鍛洪妸锔绘闁搞儺鍓氶弲?{td.tileName}");
            return true;
        }

        return false;
    }

    private void ApplyPropertyPurchaseBonuses(int playerIndex)
    {
        int totalRebate = 0;
        if (HasRole(playerIndex, RoleId.Rabbit)) totalRebate += 200;

        if (playerIndex >= 0 && playerIndex < playerNextBuyRebateList.Count && playerNextBuyRebateList[playerIndex] > 0)
        {
            totalRebate += playerNextBuyRebateList[playerIndex];
            playerNextBuyRebateList[playerIndex] = 0;
        }

        if (totalRebate > 0) AddMoney(playerIndex, totalRebate, "\u4e70\u5730\u8fd4\u5229");
    }

    private void TryPayRentOnEnter(int player, int tileIndex, TileData td)
    {
        if (td == null || td.tileCost <= 0 || !IsPlayerAlive(player)) return;
        if (tileOwnerList == null || tileOwnerList.Count <= tileIndex) return;

        int owner = tileOwnerList[tileIndex];
        if (owner == -1 || owner == player || !IsPlayerAlive(owner)) return;

        int rent = CalcRent(tileIndex, owner, td);
        rent = ApplyRentDiscount(player, rent);
        if (rent <= 0)
        {
            Debug.Log($"[Rent] {GetPlayerDisplayName(player)} \u7684\u79df\u91d1\u5df2\u88ab\u62b5\u6d88\u3002");
            return;
        }
        SpendOrBankrupt(player, rent, $"\u652f\u4ed8 {td.tileName} \u8fc7\u8def\u8d39", owner, true);
    }

    private int CalcRent(int tileIndex, int owner, TileData td)
    {
        return GetTileCurrentRent(tileIndex, td);
    }

    private int ApplyRentDiscount(int playerIndex, int rent)
    {
        if (rent <= 0 || playerIndex < 0 || playerIndex >= playerNextRentDiscountList.Count) return rent;

        int discount = playerNextRentDiscountList[playerIndex];
        if (discount <= 0) return rent;

        playerNextRentDiscountList[playerIndex] = 0;
        int finalRent = Mathf.Max(0, rent - discount);
        Debug.Log($"[Tool] {GetPlayerDisplayName(playerIndex)} 闂備焦鐪归崝宀€鈧凹鍘介幈銊╁閵忋垻鐣堕梺鎸庢⒒閸嬫捇寮查幖浣圭厸濞达絽鎽滄晶宕囩磼鏉堛劎绠樼紒顔芥閹垽鎮℃惔銏╀淮 {rent} -> {finalRent}");
        return finalRent;
    }

    private void ApplyRentCollectorBonus(int receiverIndex)
    {
        if (!HasRole(receiverIndex, RoleId.Dog) || !IsPlayerAlive(receiverIndex)) return;
        AddMoney(receiverIndex, 100, "\u5e02\u96c6\u638c\u67dc");
    }

    private bool _HasBlockingInteraction()
    {
        return _activeResolutionCount > 0 || status == Status.Resolving || status == Status.GameOver;
    }

    private bool _IsCurrentPlayerAI()
    {
        return IsAI(currentPlayerIndex);
    }

    public bool IsAIPlayer(int index)
    {
        return IsAI(index);
    }

    private bool IsAI(int index)
    {
        return index >= 0 && index < isAIPlayer.Count && isAIPlayer[index];
    }

    private IEnumerator PlayDiceRollAnimation(int diceValue)
    {
        DiceRollAnimator animator = EnsureDiceRollAnimator();
        if (animator == null)
        {
            yield break;
        }

        yield return animator.PlayRoll(diceValue);
    }

    private DiceRollAnimator EnsureDiceRollAnimator()
    {
        if (diceRollAnimator != null)
        {
            return diceRollAnimator;
        }

        DiceRollAnimator[] existingAnimators = Resources.FindObjectsOfTypeAll<DiceRollAnimator>();
        for (int i = 0; i < existingAnimators.Length; i++)
        {
            DiceRollAnimator animator = existingAnimators[i];
            if (animator != null && animator.gameObject.scene.IsValid())
            {
                diceRollAnimator = animator;
                return diceRollAnimator;
            }
        }

        GameObject prefab = Resources.Load<GameObject>(DiceRollPanelPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[Dice] Missing dice roll prefab at Resources/{DiceRollPanelPrefabPath}.");
            return null;
        }

        Transform parent = GetDiceRollPanelParent();
        GameObject instance = Instantiate(prefab, parent, false);
        instance.name = "DiceRollPanel";

        RectTransform rectTransform = instance.GetComponent<RectTransform>();
        if (rectTransform != null && parent is RectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        diceRollAnimator = instance.GetComponent<DiceRollAnimator>();
        return diceRollAnimator;
    }

    private Transform GetDiceRollPanelParent()
    {
        if (UIManager.Instance != null)
        {
            Canvas uiCanvas = GetCanvasFromRect(UIManager.Instance.actionPanel);
            if (uiCanvas == null)
            {
                uiCanvas = GetCanvasFromRect(UIManager.Instance.topPanel);
            }
            if (uiCanvas == null)
            {
                uiCanvas = GetCanvasFromRect(UIManager.Instance.handPanel);
            }
            if (uiCanvas == null)
            {
                uiCanvas = GetCanvasFromRect(UIManager.Instance.questionOverlay);
            }
            if (uiCanvas == null)
            {
                uiCanvas = GetCanvasFromRect(UIManager.Instance.noticeOverlay);
            }

            if (uiCanvas != null)
            {
                return uiCanvas.transform;
            }
        }

        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.gameObject.scene.IsValid() && canvas.isRootCanvas && !SceneTransitionManager.IsTransitionCanvas(canvas))
            {
                return canvas.transform;
            }
        }

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.gameObject.scene.IsValid() && !SceneTransitionManager.IsTransitionCanvas(canvas))
            {
                return canvas.transform;
            }
        }

        return transform;
    }

    private Canvas GetCanvasFromRect(RectTransform rectTransform)
    {
        return rectTransform != null ? rectTransform.GetComponentInParent<Canvas>(true) : null;
    }

    private int RollDiceForPlayer(int playerIndex)
    {
        int diceValue = UnityEngine.Random.Range(diceMin, diceMax + 1);
        if (HasRole(playerIndex, RoleId.Duck) && diceValue == 1)
        {
            Debug.Log($"[Role] {GetPlayerDisplayName(playerIndex)} 闂佽崵鍠愰悷杈╃不閹达絻浜归柛宀€鍋涢悘铏節婵犲倻澧戦柍褜鍓欐鎼侊綖濠靛绾ч柟瀵稿仜閸撳姊洪悡搴☆棌濞存粍鐗犻崺鈧い鎺嶈兌缁犳壆绱掓潏銊х畺妞ゃ劊鍎甸獮宥夘敊闂傛潙瀵?1 -> 2");
            return 2;
        }

        return diceValue;
    }

    private string GetPlayerDisplayName(int playerIndex)
    {
        return playerManager != null ? playerManager.GetPlayerDisplayName(playerIndex) : $"\u73a9\u5bb6{playerIndex + 1}";
    }

    private Color GetPlayerAccentColor(int playerIndex)
    {
        switch (GetPlayerRoleId(playerIndex))
        {
            case RoleId.Duck:
                return new Color(0.86f, 0.57f, 0.20f, 1f);
            case RoleId.Rabbit:
                return new Color(0.25f, 0.54f, 0.88f, 1f);
            case RoleId.Panda:
                return new Color(0.23f, 0.69f, 0.38f, 1f);
            case RoleId.Dog:
                return new Color(0.63f, 0.33f, 0.79f, 1f);
            default:
                return new Color(0.20f, 0.63f, 0.72f, 1f);
        }
    }

    private void RefreshAllTileOwnershipSigns()
    {
        int mapCount = GetGeneratedMapCount();
        for (int i = 0; i < mapCount; i++)
        {
            RefreshTileOwnershipVisual(i);
        }
    }

    private void RefreshTileOwnershipVisual(int tileIndex)
    {
        TileController tileController = GetTileControllerByIndex(tileIndex);
        if (tileController == null)
        {
            return;
        }

        TileData tileData = tileController.tileData;
        if (tileData == null || tileData.tileCost <= 0 || tileOwnerList == null || tileIndex < 0 || tileIndex >= tileOwnerList.Count)
        {
            tileController.SetOwnerSign(string.Empty, Color.white, false);
            return;
        }

        int ownerIndex = tileOwnerList[tileIndex];
        if (ownerIndex < 0 || !IsPlayerAlive(ownerIndex))
        {
            tileController.SetOwnerSign(BuyablePropertySignText, BuyablePropertySignColor, true);
            return;
        }

        tileController.SetOwnerSign(GetPlayerDisplayName(ownerIndex), GetPlayerAccentColor(ownerIndex), true);
    }

    private RoleId GetPlayerRoleId(int playerIndex)
    {
        return playerManager != null ? playerManager.GetPlayerRoleId(playerIndex) : RoleId.Duck;
    }

    private bool HasRole(int playerIndex, RoleId roleId)
    {
        return GetPlayerRoleId(playerIndex) == roleId;
    }

    private bool ShouldAIBuyCurrentTile(int playerIndex)
    {
        TileData td = GetCurrentTileData(playerIndex);
        if (td == null || td.tileCost <= 0) return false;
        int moneyAfterBuy = GetMoney(playerIndex) - td.tileCost;
        int reserve = td.tileCost >= 2500 ? aiBuyReserveMoney + 800 : aiBuyReserveMoney;
        return moneyAfterBuy >= reserve;
    }

    private bool ShouldAIUpgradeCurrentTile(int playerIndex)
    {
        if (!CanUpgradeCurrentTile(playerIndex)) return false;

        TileData td = GetCurrentTileData(playerIndex);
        if (td == null || td.upgradeCost <= 0) return false;

        int moneyAfterUpgrade = GetMoney(playerIndex) - td.upgradeCost;
        return moneyAfterUpgrade >= aiBuyReserveMoney;
    }

    private int PickAIBuyPrepCard(int playerIndex)
    {
        return CanBuyCurrentTile(playerIndex) ? FindToolCardIndexByEffect(playerIndex, "buy_rebate") : -1;
    }

    private int PickAIToolCardToUse(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerToolCardsList.Count || playerToolCardsList[playerIndex].Count == 0) return -1;

        if (GetMoney(playerIndex) <= 1200)
        {
            int gainCardIndex = FindToolCardIndexByEffect(playerIndex, "gain, 500");
            if (gainCardIndex >= 0) return gainCardIndex;
        }

        if (playerNextRentDiscountList[playerIndex] <= 0)
        {
            int rentShieldIndex = FindToolCardIndexByEffect(playerIndex, "rent_discount");
            if (rentShieldIndex >= 0) return rentShieldIndex;
        }

        int moveCardIndex = FindToolCardIndexByEffect(playerIndex, "move, 2");
        if (moveCardIndex >= 0 && ScoreTileForAI(playerIndex, GetOffsetTileIndex(playerIndex, 2)) > ScoreTileForAI(playerIndex, GetPlayerCurrentTileIndexSafe(playerIndex)))
        {
            return moveCardIndex;
        }

        return FindToolCardIndexByEffect(playerIndex, "roll_again");
    }

    private int ScoreTileForAI(int playerIndex, int tileIndex)
    {
        TileData td = GetTileDataByIndex(tileIndex);
        if (td == null) return 0;

        if (td.tileCost > 0)
        {
            int owner = tileOwnerList != null && tileIndex >= 0 && tileIndex < tileOwnerList.Count ? tileOwnerList[tileIndex] : -1;
            if (owner == -1) return 8;
            if (owner == playerIndex) return -1;
            return -6;
        }

        if (!string.IsNullOrEmpty(td.enterEffect))
        {
            if (td.enterEffect.StartsWith("question", StringComparison.OrdinalIgnoreCase)) return 5;
            if (td.enterEffect.StartsWith("tool", StringComparison.OrdinalIgnoreCase)) return 4;
            if (td.enterEffect.StartsWith("destiny", StringComparison.OrdinalIgnoreCase)) return 3;
            if (td.enterEffect.StartsWith("skip_turn", StringComparison.OrdinalIgnoreCase)) return -7;
            if (td.enterEffect.StartsWith("gain, 300", StringComparison.OrdinalIgnoreCase)) return 2;
            if (td.enterEffect.StartsWith("gain, -300", StringComparison.OrdinalIgnoreCase)) return -2;
        }

        return 0;
    }

    private int GetOffsetTileIndex(int playerIndex, int offset)
    {
        int currentTileIndex = GetPlayerCurrentTileIndexSafe(playerIndex);
        int mapCount = GetGeneratedMapCount();
        if (currentTileIndex < 0 || mapCount <= 0) return currentTileIndex;

        int targetIndex = (currentTileIndex + offset) % mapCount;
        if (targetIndex < 0) targetIndex += mapCount;
        return targetIndex;
    }

    private void ExecuteEffectByInvoke(string effectStr, int player, int tileIndex, bool isPass)
    {
        if (string.IsNullOrWhiteSpace(effectStr)) return;

        string[] parts = effectStr.Split(',');
        if (parts.Length <= 0) return;

        string key = parts[0].Trim();
        if (string.IsNullOrEmpty(key)) return;

        List<string> args = new List<string>();
        for (int i = 1; i < parts.Length; i++)
        {
            string arg = parts[i].Trim();
            if (!string.IsNullOrEmpty(arg)) args.Add(arg);
        }

        _fxPlayer = player;
        _fxTileIndex = tileIndex;
        _fxKey = key;
        _fxArgs = args.ToArray();
        _fxIsPass = isPass;

        string methodName = "FX_" + key.Replace(" ", "");
        if (!_fxMethodCache.TryGetValue(methodName, out MethodInfo methodInfo))
        {
            methodInfo = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            _fxMethodCache[methodName] = methodInfo;
        }

        if (methodInfo == null)
        {
            Debug.LogWarning($"[Effect] Missing handler: {methodName}");
            return;
        }

        try
        {
            methodInfo.Invoke(this, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Effect] Handler failed: {methodName}\n{e}");
        }
    }

    private int FX_ArgInt(int idx, int defaultValue = 0)
    {
        if (_fxArgs == null || idx < 0 || idx >= _fxArgs.Length) return defaultValue;
        return int.TryParse(_fxArgs[idx], out int value) ? value : defaultValue;
    }

    private void EnsureQuestionBankLoaded()
    {
        if (_questionBank != null) return;

        try
        {
            QuestionWrapper wrapper = DataLoader.LoadJson<QuestionWrapper>("question");
            _questionBank = wrapper != null ? wrapper.questions : Array.Empty<QuestionData>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Question] Failed to load question.json: {e.Message}");
            _questionBank = Array.Empty<QuestionData>();
        }
    }

    private void EnsureCardBanksLoaded()
    {
        if (_toolCardBank != null && _luckCardBank != null) return;

        try
        {
            CardWrapper wrapper = DataLoader.LoadJson<CardWrapper>("card");
            _toolCardBank = wrapper != null && wrapper.toolCards != null ? wrapper.toolCards : Array.Empty<CardData>();
            _luckCardBank = wrapper != null && wrapper.luckCards != null ? wrapper.luckCards : Array.Empty<CardData>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Card] Failed to load card.json: {e.Message}");
            _toolCardBank = Array.Empty<CardData>();
            _luckCardBank = Array.Empty<CardData>();
        }
    }

    private QuestionData PickQuestion()
    {
        EnsureQuestionBankLoaded();
        if (_questionBank == null || _questionBank.Length == 0) return null;

        string levelCategory = GetCurrentLevelQuestionCategory();
        if (!string.IsNullOrEmpty(levelCategory))
        {
            List<QuestionData> themedQuestions = new List<QuestionData>();
            for (int i = 0; i < _questionBank.Length; i++)
            {
                QuestionData question = _questionBank[i];
                if (question != null && string.Equals(question.category, levelCategory, StringComparison.OrdinalIgnoreCase))
                {
                    themedQuestions.Add(question);
                }
            }

            if (themedQuestions.Count > 0)
            {
                return themedQuestions[UnityEngine.Random.Range(0, themedQuestions.Count)];
            }
        }

        return _questionBank[UnityEngine.Random.Range(0, _questionBank.Length)];
    }

    private string GetCurrentLevelQuestionCategory()
    {
        MapLoader mapLoader = playerManager != null && playerManager.mapManager != null
            ? playerManager.mapManager.mapLoader
            : null;

        if (mapLoader == null)
        {
            return string.Empty;
        }

        switch (mapLoader.CurrentLevel)
        {
            case 1:
                return "\u670d\u9970";
            case 2:
                return "\u5730\u6807";
            case 3:
                return "\u7f8e\u98df";
            default:
                return string.Empty;
        }
    }

    private CardData PickToolCard()
    {
        EnsureCardBanksLoaded();
        if (_toolCardBank == null || _toolCardBank.Length == 0) return null;
        return _toolCardBank[UnityEngine.Random.Range(0, _toolCardBank.Length)];
    }

    private CardData PickLuckCard()
    {
        EnsureCardBanksLoaded();
        if (_luckCardBank == null || _luckCardBank.Length == 0) return null;
        return _luckCardBank[UnityEngine.Random.Range(0, _luckCardBank.Length)];
    }

    private string GetAnswerText(QuestionData question)
    {
        if (question == null || question.options == null) return "";
        if (question.answerIndex < 0 || question.answerIndex >= question.options.Length) return "";
        return question.options[question.answerIndex];
    }

    private void QueueMoneyChangeFeedback(int playerIndex, int oldMoney, int newMoney)
    {
        if (oldMoney == newMoney || UIManager.Instance == null)
        {
            return;
        }

        UIManager.Instance.QueueMoneyChange(playerIndex, oldMoney, newMoney, newMoney - oldMoney, GetMoneyEffectWorldPosition(playerIndex));
    }

    private Vector3 GetMoneyEffectWorldPosition(int playerIndex)
    {
        if (playerManager != null && playerIndex >= 0 && playerIndex < playerManager.playerList.Count)
        {
            GameObject playerObject = playerManager.playerList[playerIndex];
            if (playerObject != null)
            {
                return playerObject.transform.position + Vector3.up * 1.8f;
            }
        }

        TileController tileController = GetCurrentTileController(playerIndex);
        if (tileController != null)
        {
            return tileController.transform.position + Vector3.up * 1.2f;
        }

        return Vector3.up * 1.5f;
    }

    private void StartManagedResolution(IEnumerator routine)
    {
        if (routine == null)
        {
            return;
        }

        _activeResolutionCount += 1;
        if (!_gameEnded) status = Status.Resolving;
        StartCoroutine(RunManagedResolution(routine));
    }

    private IEnumerator RunManagedResolution(IEnumerator routine)
    {
        yield return routine;
        _activeResolutionCount = Mathf.Max(0, _activeResolutionCount - 1);
    }

    private void AddToolCardToHand(int playerIndex, CardData card)
    {
        if (card == null || playerIndex < 0 || playerIndex >= playerToolCardsList.Count) return;

        List<CardData> hand = playerToolCardsList[playerIndex];
        hand.Add(card);
        Debug.Log($"[Tool] {GetPlayerDisplayName(playerIndex)} \u83b7\u5f97\u9053\u5177\u5361\uff1a{card.cardName}");

        if (hand.Count > maxToolCardsPerPlayer)
        {
            CardData removedCard = hand[0];
            hand.RemoveAt(0);
            Debug.Log($"[Tool] {GetPlayerDisplayName(playerIndex)} \u7684\u624b\u724c\u5df2\u6ee1\uff0c\u4e22\u5f03\u6700\u65e9\u83b7\u5f97\u7684\u9053\u5177\u5361\uff1a{removedCard.cardName}");
        }
    }

    private int PickRandomOtherPlayer(int self)
    {
        List<int> candidates = new List<int>();
        for (int i = 0; i < totalPlayers; i++)
        {
            if (i == self) continue;
            if (!IsPlayerAlive(i)) continue;
            candidates.Add(i);
        }

        if (candidates.Count == 0) return -1;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private int PickWrongAnswerIndex(QuestionData question)
    {
        if (question == null || question.options == null || question.options.Length <= 1) return 0;

        int wrongAnswerIndex = question.answerIndex;
        while (wrongAnswerIndex == question.answerIndex)
        {
            wrongAnswerIndex = UnityEngine.Random.Range(0, question.options.Length);
        }

        return wrongAnswerIndex;
    }

    private void FX_gain()
    {
        int amount = FX_ArgInt(0, 0);
        if (_fxIsPass && amount > 0 && IsStartTile(_fxTileIndex) && HasRole(_fxPlayer, RoleId.Panda))
        {
            amount += 300;
            Debug.Log($"[Role] {GetPlayerDisplayName(_fxPlayer)} \u89e6\u53d1\u201c\u7cbd\u9999\u8865\u7ed9\u201d\uff0c\u989d\u5916\u83b7\u5f97 300 \u5609\u79be\u5e01\u3002");
        }

        AddMoney(_fxPlayer, amount, $"\u89e6\u53d1\u6548\u679c\uff1a{_fxKey}");
    }

    private void FX_question()
    {
        StartManagedResolution(CoResolveQuestion(_fxPlayer, FX_ArgInt(0, 0), FX_ArgInt(1, 0)));
    }

    private IEnumerator CoResolveQuestion(int playerIndex, int reward, int penalty)
    {
        QuestionData question = PickQuestion();
        if (question == null)
        {
            Debug.LogWarning("[Question] \u9898\u5e93\u4e3a\u7a7a\u3002");
            yield break;
        }

        Debug.Log($"[Question:{question.category}] {question.text}");
        int answerIndex;

        if (IsAI(playerIndex))
        {
            yield return new WaitForSeconds(aiActionDelay);
            bool aiCorrect = UnityEngine.Random.value < aiQuestionCorrectChance;
            answerIndex = aiCorrect ? question.answerIndex : PickWrongAnswerIndex(question);
            Debug.Log($"[Question] {GetPlayerDisplayName(playerIndex)} \u9009\u62e9\u4e86\uff1a{question.options[answerIndex]}");
        }
        else
        {
            bool answered = false;
            answerIndex = -1;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowQuestion(question, selectedIndex =>
                {
                    answerIndex = selectedIndex;
                    answered = true;
                });
            }
            else
            {
                answerIndex = question.answerIndex;
                answered = true;
            }

            yield return new WaitUntil(() => answered || _gameEnded);
            if (UIManager.Instance != null) UIManager.Instance.HideQuestion();
            if (_gameEnded) yield break;
        }

        bool correct = answerIndex == question.answerIndex;
        if (!IsAI(playerIndex) && UIManager.Instance != null)
        {
            bool confirmed = false;
            int amount = correct ? reward : penalty;
            string title = correct ? "\u56de\u7b54\u6b63\u786e" : "\u56de\u7b54\u9519\u8bef";
            string body = $"{(correct ? "\u83b7\u5f97" : "\u6263\u9664")} {amount} \u5609\u79be\u5e01\n\u6b63\u786e\u7b54\u6848\uff1a{GetAnswerText(question)}\n{question.explain}";
            UIManager.Instance.ShowNotice(title, body, "\u7ee7\u7eed", () => confirmed = true);
            yield return new WaitUntil(() => confirmed || _gameEnded);
            if (_gameEnded) yield break;
        }

        if (correct)
        {
            AddMoney(playerIndex, reward, "\u7b54\u9898\u5956\u52b1");
            Debug.Log($"[Question] {GetPlayerDisplayName(playerIndex)} \u56de\u7b54\u6b63\u786e\uff0c\u83b7\u5f97 {reward} \u5609\u79be\u5e01\u3002");
        }
        else
        {
            AddMoney(playerIndex, -penalty, "\u7b54\u9898\u60e9\u7f5a");
            Debug.Log($"[Question] {GetPlayerDisplayName(playerIndex)} \u56de\u7b54\u9519\u8bef\uff0c\u6263\u9664 {penalty} \u5609\u79be\u5e01\u3002");
        }

        Debug.Log($"[Question] \u6b63\u786e\u7b54\u6848\uff1a{GetAnswerText(question)}\u3002{question.explain}");
    }

    private void FX_tool()
    {
        StartManagedResolution(CoResolveToolCard(_fxPlayer));
    }

    private IEnumerator CoResolveToolCard(int playerIndex)
    {
        CardData card = PickToolCard();
        if (card == null)
        {
            Debug.LogWarning("[Tool] \u9053\u5177\u5361\u6c60\u4e3a\u7a7a\u3002");
            yield break;
        }

        AddToolCardToHand(playerIndex, card);
        yield return ShowCardNoticeIfHuman(playerIndex, "\u62bd\u5230\u9053\u5177\u5361", card);
    }

    private void FX_destiny()
    {
        StartManagedResolution(CoResolveLuckCard(_fxPlayer, _fxTileIndex));
    }

    private IEnumerator CoResolveLuckCard(int playerIndex, int tileIndex)
    {
        CardData card = PickLuckCard();
        if (card == null)
        {
            Debug.LogWarning("[Luck] \u8fd0\u6c14\u5361\u6c60\u4e3a\u7a7a\u3002");
            yield break;
        }

        Debug.Log($"[Luck] {GetPlayerDisplayName(playerIndex)} \u62bd\u5230\u8fd0\u6c14\u5361\uff1a{card.cardName} - {card.description}");
        yield return ShowCardNoticeIfHuman(playerIndex, "\u62bd\u5230\u8fd0\u6c14\u5361", card);
        int baseline = _activeResolutionCount;
        ExecuteEffectByInvoke(card.effect, playerIndex, tileIndex, false);
        yield return WaitForResolutionCount(baseline);
    }

    private IEnumerator ShowCardNoticeIfHuman(int playerIndex, string title, CardData card)
    {
        if (IsAI(playerIndex) || UIManager.Instance == null || card == null)
        {
            yield break;
        }

        bool confirmed = false;
        UIManager.Instance.ShowCardNotice(title, card, "\u7ee7\u7eed", () => confirmed = true);
        yield return new WaitUntil(() => confirmed || _gameEnded);
    }

    private void FX_get_from_others()
    {
        int amount = FX_ArgInt(0, 0);
        int target = PickRandomOtherPlayer(_fxPlayer);
        if (target < 0)
        {
            Debug.Log("[Effect:get_from_others] \u6ca1\u6709\u53ef\u6536\u53d6\u7684\u5176\u4ed6\u73a9\u5bb6\u3002");
            return;
        }
        SpendOrBankrupt(target, amount, "\u6e38\u5ba2\u6253\u8d4f/\u644a\u4f4d\u8d39", _fxPlayer, false);

    }

    private void FX_skip_turn()
    {
        if (_fxPlayer < 0 || _fxPlayer >= playerSkipTurnCountList.Count) return;
        playerSkipTurnCountList[_fxPlayer] += 1;
        Debug.Log($"[Effect:skip_turn] {GetPlayerDisplayName(_fxPlayer)} \u7684\u4e0b\u56de\u5408\u5c06\u88ab\u8df3\u8fc7\u3002");
    }

    private void FX_move()
    {
        StartManagedResolution(CoResolveMoveEffect(_fxPlayer, FX_ArgInt(0, 0)));
    }

    private IEnumerator CoResolveMoveEffect(int playerIndex, int stepCount)
    {
        if (!IsPlayerAlive(playerIndex) || stepCount == 0) yield break;

        Debug.Log($"[Effect:move] {GetPlayerDisplayName(playerIndex)} \u79fb\u52a8 {stepCount} \u683c\u3002");
        int baseline = _activeResolutionCount;
        playerManager.StepPlayer(playerIndex, stepCount);
        yield return WaitPlayerMoveDone(playerIndex);
        yield return WaitForResolutionCount(baseline);
    }

    private void FX_rent_discount()
    {
        int discount = FX_ArgInt(0, 0);
        if (_fxPlayer < 0 || _fxPlayer >= playerNextRentDiscountList.Count) return;
        playerNextRentDiscountList[_fxPlayer] = Mathf.Max(playerNextRentDiscountList[_fxPlayer], discount);
        Debug.Log($"[Tool] {GetPlayerDisplayName(_fxPlayer)} \u83b7\u5f97\u4e00\u6b21\u8fc7\u8def\u8d39\u51cf\u514d {discount}\u3002");
    }

    private void FX_buy_rebate()
    {
        int rebate = FX_ArgInt(0, 0);
        if (_fxPlayer < 0 || _fxPlayer >= playerNextBuyRebateList.Count) return;
        playerNextBuyRebateList[_fxPlayer] = Mathf.Max(playerNextBuyRebateList[_fxPlayer], rebate);
        Debug.Log($"[Tool] {GetPlayerDisplayName(_fxPlayer)} \u7684\u4e0b\u6b21\u4e70\u5730\u8fd4\u5229\u63d0\u5347\u4e3a {rebate}\u3002");
    }

    private void FX_roll_again()
    {
        StartManagedResolution(CoResolveRollAgain(_fxPlayer));
    }

    private IEnumerator CoResolveRollAgain(int playerIndex)
    {
        if (!IsPlayerAlive(playerIndex)) yield break;

        int rerollValue = RollDiceForPlayer(playerIndex);
        lastDiceValue = rerollValue;
        yield return PlayDiceRollAnimation(rerollValue);
        Debug.Log($"[Tool] {GetPlayerDisplayName(playerIndex)} 濠电偠鎻紞鈧繛澶嬫礋瀵偊濡舵径濠勪紜濠电姴锕ら崰姘跺吹閵堝棛绠鹃柟楣冾杺閻掔偓绻涚€涙﹫鑰块柛銊﹀劤閳规垿骞橀崜渚囨Х濠碘槅鍋嗘晶妤冩崲閸岀倛鍥ㄧ節濮橆剛顔婇悗鐟板濠㈡ê袙?{rerollValue}");

        int baseline = _activeResolutionCount;
        playerManager.StepPlayer(playerIndex, rerollValue);
        yield return WaitPlayerMoveDone(playerIndex);
        yield return WaitForResolutionCount(baseline);
    }

    private void FX_skip_protect()
    {
        int count = Mathf.Max(1, FX_ArgInt(0, 1));
        if (_fxPlayer < 0 || _fxPlayer >= playerSkipProtectionList.Count) return;
        playerSkipProtectionList[_fxPlayer] += count;
        Debug.Log($"[Tool] {GetPlayerDisplayName(_fxPlayer)} \u83b7\u5f97 {count} \u6b21\u8df3\u8fc7\u56de\u5408\u4fdd\u62a4\u3002");
    }

    private void FX_E_FW08_ENTER_PROPERTY_STATION_MAIN()
    {
        Debug.Log("[Effect:E_FW08_ENTER_PROPERTY_STATION_MAIN] \u5f53\u524d\u7248\u672c\u672a\u542f\u7528\u7279\u6b8a\u7ad9\u70b9\u89c4\u5219\u3002");
    }

    private bool IsStartTile(int tileIndex)
    {
        TileData tileData = GetTileDataByIndex(tileIndex);
        if (tileData == null) return tileIndex == 0;
        return tileIndex == 0 || (!string.IsNullOrEmpty(tileData.tileID) && tileData.tileID.StartsWith("ST", StringComparison.OrdinalIgnoreCase));
    }
}
