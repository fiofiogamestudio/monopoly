using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiceRollAnimator : MonoBehaviour
{
    [Header("Refs")]
    public Image diceImage;

    [Header("Resources")]
    public string rollFrameResourcePath = "Dice/T003_12/Roll";
    public string resultFrameResourcePath = "Dice/T003_12/Result";
    public int rollFrameCount = 19;
    public int resultFrameCount = 3;

    [Header("Timing")]
    [Min(0.01f)] public float rollFrameSeconds = 0.035f;
    [Min(0.01f)] public float resultFrameSeconds = 0.075f;
    [Min(0f)] public float holdSeconds = 0.25f;

    private readonly List<Sprite> rollFrames = new List<Sprite>();
    private readonly Dictionary<int, List<Sprite>> resultFramesByValue = new Dictionary<int, List<Sprite>>();
    private bool framesLoaded;

    private void Awake()
    {
        EnsureReferences();
        SetVisible(false);
    }

    public IEnumerator PlayRoll(int diceValue)
    {
        EnsureReferences();
        EnsureFramesLoaded();

        if (diceImage == null || rollFrames.Count == 0)
        {
            yield break;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        SetVisible(true);

        yield return PlayFrames(rollFrames, rollFrameSeconds);

        int clampedDiceValue = Mathf.Clamp(diceValue, 1, 6);
        if (resultFramesByValue.TryGetValue(clampedDiceValue, out List<Sprite> resultFrames) && resultFrames.Count > 0)
        {
            yield return PlayFrames(resultFrames, resultFrameSeconds);
        }

        if (holdSeconds > 0f)
        {
            yield return new WaitForSeconds(holdSeconds);
        }

        SetVisible(false);
    }

    private void EnsureReferences()
    {
        if (diceImage == null)
        {
            diceImage = GetComponentInChildren<Image>(true);
        }

        if (diceImage != null)
        {
            diceImage.preserveAspect = true;
            diceImage.raycastTarget = false;
        }
    }

    private void EnsureFramesLoaded()
    {
        if (framesLoaded)
        {
            return;
        }

        framesLoaded = true;
        rollFrames.Clear();
        resultFramesByValue.Clear();

        for (int i = 1; i <= rollFrameCount; i++)
        {
            Sprite sprite = Resources.Load<Sprite>($"{rollFrameResourcePath}/roll_{i:000}");
            if (sprite != null)
            {
                rollFrames.Add(sprite);
            }
        }

        for (int diceValue = 1; diceValue <= 6; diceValue++)
        {
            List<Sprite> frames = new List<Sprite>();
            for (int frame = 1; frame <= resultFrameCount; frame++)
            {
                Sprite sprite = Resources.Load<Sprite>($"{resultFrameResourcePath}/dice_{diceValue}_{frame}");
                if (sprite != null)
                {
                    frames.Add(sprite);
                }
            }

            resultFramesByValue[diceValue] = frames;
        }
    }

    private IEnumerator PlayFrames(List<Sprite> frames, float frameSeconds)
    {
        for (int i = 0; i < frames.Count; i++)
        {
            Sprite sprite = frames[i];
            if (sprite == null)
            {
                continue;
            }

            diceImage.sprite = sprite;
            diceImage.enabled = true;

            if (frameSeconds > 0f)
            {
                yield return new WaitForSeconds(frameSeconds);
            }
            else
            {
                yield return null;
            }
        }
    }

    private void SetVisible(bool visible)
    {
        if (diceImage != null)
        {
            diceImage.enabled = visible;
            if (!visible)
            {
                diceImage.sprite = null;
            }
        }

        if (!visible && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }
}
