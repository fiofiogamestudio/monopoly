using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileController : MonoBehaviour
{
    private static readonly Color BorderColor = new Color(0.08f, 0.09f, 0.12f, 1f);
    private static readonly Color NameBadgeColor = new Color(1f, 0.985f, 0.94f, 0.92f);
    private static readonly string[] StyledRendererPaths =
    {
        "BaseFrame",
        "TopPlate",
        "ShowcaseRoot/ShowcasePad",
        "TrimLeft",
        "TrimRight",
        "TrimFront",
        "TrimBack",
        "FrontPlaque"
    };

    private static readonly Dictionary<string, string[]> TileModelAliases = new Dictionary<string, string[]>
    {
        { "\u5609\u79be\u7801\u5934", new[] { "\u8d77\u70b9" } },
        { "\u4e94\u82b3\u658b\u7cbd\u5b50\u574a", new[] { "\u4e94\u82b3\u658b\u7cbd\u5b50\u5e97" } },
        { "\u5609\u5174\u5357\u7ad9", new[] { "\u5609\u5174\u7ad9" } },
        { "\u5b50\u57ce\u9057\u5740", new[] { "\u5b50\u57ce" } },
        { "\u6742\u8d27\u94fa", new[] { "\u540d\u521b\u4f18\u54c1", "\u5976\u8336\u5e97" } },
        { "\u8d85\u5e02", new[] { "\u540d\u521b\u4f18\u54c1", "\u5976\u8336\u5e97" } },
        { "\u5c0f\u5356\u90e8", new[] { "\u5976\u8336\u5e97", "\u540d\u521b\u4f18\u54c1" } }
    };

    private static Shader tileShader;
    private static Font labelFont;

    public TileData tileData;
    public Text gridText;
    public Image gridBadge;
    public GameObject modelBase;
    public Vector3 gridOffset;
    public Text modelText;
    public Image modelBadge;
    public RectTransform ownerSignRoot;
    public Image ownerBadge;
    public Text ownerText;
    [SerializeField] private bool useTileModels = false;

    public bool hasUpgraded;

    private readonly Dictionary<string, Renderer> tileRenderers = new Dictionary<string, Renderer>();
    private readonly Dictionary<Renderer, Material> runtimeMaterials = new Dictionary<Renderer, Material>();

    private GameObject currentModelInstance;
    private Vector3 modelBaseInitialLocalPosition;
    private bool visualsCached;

    private void Awake()
    {
        CacheVisualParts();
    }

    public void SetOffset(Vector3 offset)
    {
        gridOffset = offset;
    }

    public void Init(TileData newTileData)
    {
        tileData = newTileData;

        CacheVisualParts();
        ApplyTileVisuals();
        ResetModelBasePosition();
        ClearCurrentModel();
        EnsureLabelFont();
        ApplyLabelVisuals();
        SetOwnerSign(string.Empty, Color.white, false);

        if (!useTileModels)
        {
            FixMissingTileMaterials();
            return;
        }

        GameObject modelPrefab = LoadTileModelPrefab(tileData.tileName);
        if (modelPrefab == null)
        {
            FixMissingTileMaterials();
            return;
        }

        currentModelInstance = Instantiate(modelPrefab, transform);
        currentModelInstance.transform.SetParent(transform, false);
        currentModelInstance.transform.localPosition = gridOffset;
        currentModelInstance.transform.localRotation = Quaternion.identity;
        currentModelInstance.transform.localScale = Vector3.one;

        if (modelBase != null)
        {
            modelBase.transform.localPosition = modelBaseInitialLocalPosition + gridOffset;
        }

        FixMissingTileMaterials();
    }

    private void FixMissingTileMaterials()
    {
        if (tileData == null)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material shared = renderer.sharedMaterial;
            if (shared != null && shared.shader != null && shared.shader.name != "Hidden/InternalErrorShader")
            {
                continue;
            }

            Material material = new Material(ResolveTileShader())
            {
                name = $"TileFallback_{renderer.gameObject.name}"
            };

            Color color = tileData.tileType.ToBaseColor();
            string lowerName = renderer.gameObject.name.ToLowerInvariant();
            if (lowerName.Contains("trim") || lowerName.Contains("plaque"))
            {
                color = tileData.tileType.ToAccentColor();
            }
            else if (lowerName.Contains("top"))
            {
                color = tileData.tileType.ToTopColor();
            }
            else if (lowerName.Contains("pad") || lowerName.Contains("showcase"))
            {
                color = tileData.tileType.ToPedestalColor();
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.color = color;
            renderer.material = material;
        }
    }

    private void ApplyLabelVisuals()
    {
        if (modelText != null)
        {
            modelText.font = labelFont;
            modelText.text = tileData != null ? FormatTileLabel(tileData.tileName) : string.Empty;
            modelText.color = tileData != null ? tileData.tileType.ToLabelColor() : new Color(0.12f, 0.12f, 0.12f, 1f);
            modelText.fontStyle = FontStyle.Bold;
            modelText.resizeTextForBestFit = true;
            modelText.resizeTextMinSize = 18;
            modelText.resizeTextMaxSize = 38;
            modelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            modelText.verticalOverflow = VerticalWrapMode.Overflow;
            modelText.alignment = TextAnchor.MiddleCenter;
        }

        if (gridText != null)
        {
            gridText.font = labelFont;
            gridText.text = tileData != null ? tileData.tileType.ToRealName() : string.Empty;
            gridText.color = Color.white;
            gridText.fontStyle = FontStyle.Bold;
            gridText.resizeTextForBestFit = true;
            gridText.resizeTextMinSize = 16;
            gridText.resizeTextMaxSize = 28;
            gridText.horizontalOverflow = HorizontalWrapMode.Wrap;
            gridText.verticalOverflow = VerticalWrapMode.Overflow;
            gridText.alignment = TextAnchor.MiddleCenter;
        }

        if (gridBadge != null && tileData != null)
        {
            Color badgeColor = tileData.tileType.ToAccentColor();
            badgeColor.a = 0.96f;
            gridBadge.color = badgeColor;
        }

        if (modelBadge != null)
        {
            modelBadge.color = NameBadgeColor;
        }

        if (ownerText != null)
        {
            ownerText.font = labelFont;
            ownerText.fontStyle = FontStyle.Bold;
            ownerText.resizeTextForBestFit = true;
            ownerText.resizeTextMinSize = 14;
            ownerText.resizeTextMaxSize = 28;
            ownerText.horizontalOverflow = HorizontalWrapMode.Wrap;
            ownerText.verticalOverflow = VerticalWrapMode.Overflow;
            ownerText.alignment = TextAnchor.MiddleCenter;
        }
    }

    public void SetOwnerSign(string ownerName, Color ownerColor, bool visible)
    {
        if (ownerSignRoot != null)
        {
            ownerSignRoot.gameObject.SetActive(visible);
        }

        SetRendererVisible("FrontPlaque", visible);
        if (!visible)
        {
            return;
        }

        Color plaqueColor = Color.Lerp(ownerColor, BorderColor, 0.28f);
        plaqueColor.a = 1f;
        SetRendererColor("FrontPlaque", plaqueColor);

        if (ownerBadge != null)
        {
            Color badgeColor = ownerColor;
            badgeColor.a = 0.96f;
            ownerBadge.color = badgeColor;
        }

        if (ownerText != null)
        {
            ownerText.text = FormatOwnerLabel(ownerName);
            ownerText.color = GetReadableTextColor(ownerColor);
        }
    }

    private static void EnsureLabelFont()
    {
        if (labelFont != null)
        {
            return;
        }

        labelFont = Resources.Load<Font>("Fonts/MenuFont");
        if (labelFont == null)
        {
            labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    private GameObject LoadTileModelPrefab(string tileName)
    {
        GameObject modelPrefab = Resources.Load<GameObject>($"Prefabs/TileModels/{tileName}");
        if (modelPrefab == null)
        {
            modelPrefab = Resources.Load<GameObject>($"Prefabs/TileModels1/{tileName}");
        }

        if (modelPrefab != null)
        {
            return modelPrefab;
        }

        if (!TileModelAliases.TryGetValue(tileName, out string[] aliases))
        {
            return null;
        }

        for (int i = 0; i < aliases.Length; i++)
        {
            string alias = aliases[i];
            modelPrefab = Resources.Load<GameObject>($"Prefabs/TileModels/{alias}");
            if (modelPrefab == null)
            {
                modelPrefab = Resources.Load<GameObject>($"Prefabs/TileModels1/{alias}");
            }

            if (modelPrefab != null)
            {
                return modelPrefab;
            }
        }

        return null;
    }

    private void CacheVisualParts()
    {
        if (visualsCached)
        {
            return;
        }

        if (modelBase != null)
        {
            modelBaseInitialLocalPosition = modelBase.transform.localPosition;
        }

        for (int i = 0; i < StyledRendererPaths.Length; i++)
        {
            string path = StyledRendererPaths[i];
            Transform target = transform.Find(path);
            if (target == null)
            {
                continue;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                tileRenderers[path] = renderer;
            }
        }

        visualsCached = true;
    }

    private void ApplyTileVisuals()
    {
        if (tileData == null)
        {
            return;
        }

        SetRendererColor("BaseFrame", BorderColor);
        SetRendererColor("TopPlate", tileData.tileType.ToTopColor());
        SetRendererColor("ShowcaseRoot/ShowcasePad", tileData.tileType.ToPedestalColor());
        SetRendererColor("TrimLeft", BorderColor);
        SetRendererColor("TrimRight", BorderColor);
        SetRendererColor("TrimFront", BorderColor);
        SetRendererColor("TrimBack", BorderColor);
        SetRendererColor("FrontPlaque", BorderColor);

        SetRendererVisible("TrimLeft", false);
        SetRendererVisible("TrimRight", false);
        SetRendererVisible("TrimFront", false);
        SetRendererVisible("TrimBack", false);
        SetRendererVisible("FrontPlaque", false);
        SetRendererVisible("ShowcaseRoot/ShowcasePad", false);
    }

    private void SetRendererColor(string path, Color color)
    {
        if (!tileRenderers.TryGetValue(path, out Renderer renderer) || renderer == null)
        {
            return;
        }

        Material material = GetOrCreateRuntimeMaterial(renderer);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        material.color = color;
    }

    private Material GetOrCreateRuntimeMaterial(Renderer renderer)
    {
        if (runtimeMaterials.TryGetValue(renderer, out Material material) && material != null)
        {
            return material;
        }

        tileShader ??= ResolveTileShader();
        material = new Material(tileShader)
        {
            name = $"TileRuntime_{renderer.gameObject.name}"
        };

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.12f);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.12f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        renderer.material = material;
        runtimeMaterials[renderer] = material;
        return material;
    }

    private void SetRendererVisible(string path, bool visible)
    {
        if (!tileRenderers.TryGetValue(path, out Renderer renderer) || renderer == null)
        {
            return;
        }

        renderer.enabled = visible;
    }

    private static Shader ResolveTileShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Standard");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            return shader;
        }

        return Shader.Find("Unlit/Color");
    }

    private void ResetModelBasePosition()
    {
        if (modelBase != null)
        {
            modelBase.transform.localPosition = modelBaseInitialLocalPosition;
        }
    }

    private void ClearCurrentModel()
    {
        if (currentModelInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(currentModelInstance);
        }
        else
        {
            DestroyImmediate(currentModelInstance);
        }

        currentModelInstance = null;
    }

    private static string FormatTileLabel(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
        {
            return string.Empty;
        }

        if (rawName.Contains("\n") || rawName.Length <= 4)
        {
            return rawName;
        }

        int firstLineLength = Mathf.Min(4, rawName.Length - 1);
        return rawName.Substring(0, firstLineLength) + "\n" + rawName.Substring(firstLineLength);
    }

    private static string FormatOwnerLabel(string ownerName)
    {
        if (string.IsNullOrWhiteSpace(ownerName))
        {
            return string.Empty;
        }

        string compactName = ownerName.Trim();
        if (compactName.Length <= 4)
        {
            return compactName;
        }

        return FormatTileLabel(compactName);
    }

    private static Color GetReadableTextColor(Color backgroundColor)
    {
        float luminance = backgroundColor.r * 0.299f + backgroundColor.g * 0.587f + backgroundColor.b * 0.114f;
        return luminance >= 0.62f ? new Color(0.14f, 0.11f, 0.08f, 1f) : Color.white;
    }
}
