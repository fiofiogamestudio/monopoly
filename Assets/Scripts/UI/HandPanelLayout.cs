using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HandPanelLayout : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private HandCardView cardPrefab;

    [Header("Layout")]
    [SerializeField, Range(1, 8)] private int slotCount = 5;
    [SerializeField] private Vector2 cardSize = new Vector2(100f, 100f);
    [SerializeField] private float cardSpacing = 8f;
    [SerializeField] private Vector2 cardOffset = new Vector2(0f, 0f);

    private readonly List<HandCardView> cardViews = new List<HandCardView>();
    private readonly List<Button> cardButtons = new List<Button>();

    public RectTransform BodyRoot
    {
        get
        {
            EnsureReferences();
            return bodyRoot;
        }
    }

    public List<HandCardView> CardViews
    {
        get
        {
            ApplyLayout();
            return cardViews;
        }
    }

    public List<Button> CardButtons
    {
        get
        {
            ApplyLayout();
            cardButtons.Clear();
            for (int i = 0; i < cardViews.Count; i++)
            {
                if (cardViews[i] != null && cardViews[i].RootButton != null)
                {
                    cardButtons.Add(cardViews[i].RootButton);
                }
            }

            return cardButtons;
        }
    }

    public int SlotCount => Mathf.Max(1, slotCount);

    private void Awake()
    {
        ApplyLayout();
    }

    private void OnEnable()
    {
        ApplyLayout();
    }

    private void OnValidate()
    {
        slotCount = Mathf.Max(1, slotCount);
        cardSize.x = Mathf.Max(1f, cardSize.x);
        cardSize.y = Mathf.Max(1f, cardSize.y);
        ApplyLayout();
    }

    public void ApplyLayout()
    {
        EnsureReferences();
        CollectCardViews();
        EnsureRuntimeSlots();
        LayoutCards();
    }

    public void SetSlotCount(int count)
    {
        slotCount = Mathf.Max(1, count);
        ApplyLayout();
    }

    private void EnsureReferences()
    {
        if (bodyRoot == null)
        {
            Transform bodyTransform = transform.Find("HandBody");
            if (bodyTransform != null)
            {
                bodyRoot = bodyTransform.GetComponent<RectTransform>();
            }
        }

        if (bodyRoot == null && CanCreateRuntimeChildren())
        {
            bodyRoot = CreateBodyRoot();
        }

        if (cardPrefab == null)
        {
            GameObject prefabObject = Resources.Load<GameObject>("Prefabs/UI/HandCard");
            if (prefabObject != null)
            {
                cardPrefab = prefabObject.GetComponent<HandCardView>();
            }
        }
    }

    private RectTransform CreateBodyRoot()
    {
        GameObject bodyObject = new GameObject("HandBody", typeof(RectTransform));
        bodyObject.layer = gameObject.layer;
        bodyObject.transform.SetParent(transform, false);

        RectTransform rect = bodyObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private bool CanCreateRuntimeChildren()
    {
        if (!Application.isPlaying || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
        {
            return false;
        }

#if UNITY_EDITOR
        if (UnityEditor.EditorUtility.IsPersistent(gameObject) || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            return false;
        }
#endif

        return true;
    }

    private void CollectCardViews()
    {
        cardViews.Clear();
        if (bodyRoot == null)
        {
            return;
        }

        for (int i = 0; i < bodyRoot.childCount; i++)
        {
            HandCardView view = bodyRoot.GetChild(i).GetComponent<HandCardView>();
            if (view != null)
            {
                cardViews.Add(view);
            }
        }
    }

    private void EnsureRuntimeSlots()
    {
        if (!CanCreateRuntimeChildren() || bodyRoot == null || cardPrefab == null)
        {
            return;
        }

        while (cardViews.Count < SlotCount)
        {
            HandCardView clone = Instantiate(cardPrefab, bodyRoot);
            clone.name = $"HandCard{cardViews.Count}";
            clone.SetEmpty();
            cardViews.Add(clone);
        }
    }

    private void LayoutCards()
    {
        if (cardViews.Count == 0)
        {
            return;
        }

        int layoutCount = Mathf.Max(SlotCount, cardViews.Count);
        float step = cardSize.x + cardSpacing;
        float firstX = -0.5f * step * (layoutCount - 1);

        for (int i = 0; i < cardViews.Count; i++)
        {
            HandCardView cardView = cardViews[i];
            if (cardView == null)
            {
                continue;
            }

            RectTransform rect = cardView.GetComponent<RectTransform>();
            if (rect == null)
            {
                continue;
            }

            bool activeSlot = i < SlotCount;
            if (!Application.isPlaying || cardView.gameObject.activeSelf != activeSlot)
            {
                cardView.gameObject.SetActive(activeSlot);
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = cardSize;
            rect.anchoredPosition = new Vector2(firstX + step * i + cardOffset.x, cardOffset.y);
        }
    }
}
