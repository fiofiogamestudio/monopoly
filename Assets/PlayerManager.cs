using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public Transform mapRoot;
    public Transform playerRoot;
    public List<GameObject> playerList = new List<GameObject>();
    public List<bool> playerIsMovingList = new List<bool>();
    public List<int> playerTileIndexList = new List<int>();
    public GameObject playerPrefab;

    public MapManager mapManager;
    public GameManager gameManager;

    public int mapCount => mapRoot.childCount;

    private Vector3 GetRandomOnePosition(float scale = 1.0f)
    {
        return Vector3.left * scale * Random.Range(-1.0f, 1.0f) + Vector3.forward * scale * Random.Range(-1.0f, 1.0f);
    }
    public void GeneratePlayers()
    {
        List<string> playerNames = new List<string>() { "Rabbit", "Panda" };

        for (int i = 0; i < playerNames.Count; i++)
        {
            string playerName = playerNames[i];
            GameObject playerModel = Resources.Load<GameObject>($"Prefabs/PlayerModels/{playerName}");
            if (playerModel != null)
            {
                // generate player prefab, set parent to playerRoot, init position to mapRoot.GetChild(0)
                // generate player model, set parent to player prefab
                GameObject playerInstance = Instantiate(playerPrefab, playerRoot);
                playerInstance.name = playerName;
                playerInstance.transform.position = mapRoot.GetChild(0).position + GetRandomOnePosition(0.2f);

                GameObject modelInstance = Instantiate(playerModel, playerInstance.transform);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;
                playerList.Add(playerInstance);
                playerIsMovingList.Add(false);
                playerTileIndexList.Add(0);
            }
            else
            {
                Debug.LogWarning($"Player prefab for {playerName} not found!");
            }
        }
    }

    public void StepPlayer(int playerIndex, int stepCount)
    {
        if (playerIsMovingList[playerIndex])
        {
            Debug.LogWarning($"Player {playerIndex} is already moving!");
            return;
        }
        StartCoroutine(IEStepPlayer(playerIndex, stepCount));
    }

    public IEnumerator IEStepPlayer(int playerIndex, int stepCount, float stepDuration = 0.5f)
    {
        playerIsMovingList[playerIndex] = true;

        // 可调：跳跃高度、落地“不倒翁”晃动强度/频率
        float jumpHeight = 0.25f;          // 跳跃高度（世界坐标 Y 方向）
        float wobbleAngle = 12f;           // 不倒翁最大倾斜角度（度）
        float wobbleFreq = 14f;            // 晃动频率（越大越快）
        float yawJitter = 2f;              // 轻微左右偏航（度，可设为0）

        for (int i = 0; i < stepCount; i++)
        {
            if (playerIndex < 0 || playerIndex >= playerList.Count)
                yield break;

            GameObject player = playerList[playerIndex];

            int currentPosIndex = playerTileIndexList[playerIndex];
            if (currentPosIndex == -1)
                yield break;

            int nextPosIndex = (currentPosIndex + 1) % mapRoot.childCount;
            Vector3 nextPos = mapRoot.GetChild(nextPosIndex).position;

            // 朝向：按你原来的“右转90度”逻辑
            Vector3 direction = (nextPos - player.transform.position).normalized;
            direction = new Vector3(-direction.z, direction.y, direction.x);
            player.transform.forward = new Vector3(direction.x, 0, direction.z).normalized;

            // 保存“落地后应该回到的基准旋转”（面向正确方向）
            Quaternion settleRot = player.transform.rotation;

            // ===== 1) 跳跃移动（抛物线 + ease）=====
            float elapsedTime = 0f;
            Vector3 startingPos = player.transform.position;

            while (elapsedTime < stepDuration)
            {
                float t = Mathf.Clamp01(elapsedTime / stepDuration);

                // ease in/out cubic
                float easedT = (t < 0.5f) ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

                Vector3 basePos = Vector3.Lerp(startingPos, nextPos, easedT);

                // 抛物线跳跃：0->1->0
                float arc = 4f * t * (1f - t);
                basePos.y += arc * jumpHeight;

                player.transform.position = basePos;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            player.transform.position = nextPos;
            playerTileIndexList[playerIndex] = nextPosIndex;

            // ===== 2) 落地“不倒翁”晃动 stepDuration 秒 =====
            // 思路：保持位置不动，只做旋转摇摆（绕 X/Z 轴倾斜），并用阻尼衰减回到 settleRot
            float wobbleTime = 0f;

            // 为了让每次落地晃动方向不同一点（但不随机太夸张），用一个相位偏移
            float phaseOffset = Random.Range(0f, 6.28f);

            while (wobbleTime < stepDuration)
            {
                float t = Mathf.Clamp01(wobbleTime / stepDuration);
                float damper = 1f - t; // 线性衰减：1 -> 0

                float phase = (wobbleTime * wobbleFreq) + phaseOffset;

                // 绕 X/Z 轴摆动：像不倒翁一样“歪一下又回弹”
                float pitch = Mathf.Sin(phase) * wobbleAngle * damper;          // 前后倾
                float roll = Mathf.Cos(phase * 0.85f) * wobbleAngle * damper;  // 左右倾

                // 可选：落地时轻微左右摇头，增加“Q弹感”
                float yaw = Mathf.Sin(phase * 0.6f) * yawJitter * damper;

                // 只改变旋转，不改变位置（不倒翁效果）
                Quaternion wobbleRot = settleRot * Quaternion.Euler(pitch, yaw, roll);
                player.transform.rotation = wobbleRot;

                wobbleTime += Time.deltaTime;
                yield return null;
            }

            // 最终回到基准旋转
            player.transform.rotation = settleRot;

            onPass(playerIndex, nextPosIndex);
        }

        onEnter(playerIndex, playerTileIndexList[playerIndex]);

        playerIsMovingList[playerIndex] = false;
    }


    private void onEnter(int player, int tileIndex)
    {
        Debug.Log($"Player {player} entered tile {tileIndex}");

        gameManager.onEnter(player, tileIndex);
    }

    private void onPass(int player, int tileIndex)
    {
        Debug.Log($"Player {player} passed tile {tileIndex}");

        gameManager.onPass(player, tileIndex);
    }


    public void Awake()
    {
        GeneratePlayers();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Move Step");
            StepPlayer(Random.Range(0, 2), Random.Range(1, 7)); // Move first player by 1 step on space key press
        }
    }
}
