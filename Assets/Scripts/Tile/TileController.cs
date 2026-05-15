using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileController : MonoBehaviour
{
    private static readonly Color BorderColor = new Color(0.08f, 0.09f, 0.12f, 1f);
    private static readonly Color NameBadgeColor = new Color(1f, 0.985f, 0.94f, 0.92f);
    private static readonly Color SignPostColor = new Color(0.40f, 0.25f, 0.12f, 1f);
    private static readonly Color TileNamePaintColor = new Color(0.13f, 0.08f, 0.04f, 1f);
    private static readonly string[] TileNameFallbackFontNames =
    {
        "Microsoft YaHei UI",
        "Microsoft YaHei",
        "SimHei",
        "Noto Sans CJK SC",
        "PingFang SC",
        "Arial Unicode MS"
    };
    private const float OwnerSignEdgePadding = 0.045f;
    private const float OwnerSignPanelWidth = 0.56f;
    private const float OwnerSignPanelHeight = 0.24f;
    private const float OwnerSignPanelThickness = 0.035f;
    private const float OwnerSignTextBaseCharacterSize = 0.04f;
    private const int OwnerSignTextFontSize = 128;
    private const float TileNameSurfaceOffset = 0.035f;
    private const float TileNameTextBaseCharacterSize = 0.015f;
    private const int TileNameTextFontSize = 128;
    private const int SignSortingOrder = 260;
    private const int TileNameSortingOrder = -50;
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
    private static Shader depthTestedTextShader;
    private static Font labelFont;
    private static Font tileNameFont;
    private static Font tileNameFallbackFont;
    private static readonly Dictionary<Font, Material> tileNameTextMaterials = new Dictionary<Font, Material>();

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
    public RectTransform tileNameRoot;
    public Text tileNameText;
    [SerializeField] private bool useTileModels = true;

    public bool hasUpgraded;

    private readonly Dictionary<string, Renderer> tileRenderers = new Dictionary<string, Renderer>();
    private readonly Dictionary<Renderer, Material> runtimeMaterials = new Dictionary<Renderer, Material>();

    private GameObject currentModelInstance;
    private GameObject ownerSignObject;
    private Renderer ownerSignPanelRenderer;
    private TextMesh ownerSignFrontTextMesh;
    private TextMesh tileNameTextMesh;
    private Vector3 modelBaseInitialLocalPosition;
    private Vector3 customOutwardLocalDirection;
    private bool visualsCached;
    private bool hasCustomOutwardLocalDirection;

    private void Awake()
    {
        CacheVisualParts();
    }

    public void SetOffset(Vector3 offset)
    {
        gridOffset = offset;
    }

    public void SetCustomOutwardDirection(Vector3 localDirection)
    {
        localDirection.y = 0f;
        if (localDirection.sqrMagnitude < 0.0001f)
        {
            hasCustomOutwardLocalDirection = false;
            customOutwardLocalDirection = Vector3.zero;
            return;
        }

        customOutwardLocalDirection = SnapLocalDirectionToAxis(localDirection);
        hasCustomOutwardLocalDirection = true;
    }

    public void Init(TileData newTileData)
    {
        tileData = newTileData;

        CacheVisualParts();
        ApplyTileVisuals();
        ResetModelBasePosition();
        ClearCurrentModel();
        EnsureLabelFont();
        EnsureRuntimeLabels();
        ApplyLabelVisuals();
        SetOwnerSign(string.Empty, Color.white, false);

        if (!useTileModels)
        {
            FixMissingTileMaterials();
            return;
        }

        GameObject modelPrefab = LoadTileModelPrefab(tileData);
        if (modelPrefab == null)
        {
            FixMissingTileMaterials();
            return;
        }

        currentModelInstance = Instantiate(modelPrefab, transform);
        currentModelInstance.transform.SetParent(transform, false);
        currentModelInstance.transform.localPosition = gridOffset;
        currentModelInstance.transform.localRotation = GetTileModelRotation(tileData);
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
            modelText.text = tileData != null && tileData.tileType != TileType.KongDi
                ? FormatTileLabel(tileData.tileName)
                : string.Empty;
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
            gridText.text = GetGridLabel(tileData);
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

        if (tileNameTextMesh != null)
        {
            string tileLabel = GetTileSurfaceLabel(tileData);
            Font resolvedTileNameFont = ResolveTileNameFont(tileLabel);
            tileNameTextMesh.font = resolvedTileNameFont;
            tileNameTextMesh.text = tileLabel;
            tileNameTextMesh.color = TileNamePaintColor;
            tileNameTextMesh.fontStyle = FontStyle.Bold;
            tileNameTextMesh.fontSize = TileNameTextFontSize;
            tileNameTextMesh.characterSize = TileNameTextBaseCharacterSize;
            tileNameTextMesh.lineSpacing = 0.82f;
            tileNameTextMesh.anchor = TextAnchor.MiddleCenter;
            tileNameTextMesh.alignment = TextAlignment.Center;
            tileNameTextMesh.richText = false;

            if (resolvedTileNameFont != null && !string.IsNullOrEmpty(tileLabel))
            {
                resolvedTileNameFont.RequestCharactersInTexture(tileLabel, TileNameTextFontSize, FontStyle.Bold);
            }

            MeshRenderer textRenderer = tileNameTextMesh.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                Material textMaterial = GetTileNameTextMaterial(resolvedTileNameFont);
                if (textMaterial != null)
                {
                    textRenderer.sharedMaterial = textMaterial;
                }

                textRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                textRenderer.receiveShadows = false;
                textRenderer.sortingOrder = TileNameSortingOrder;
            }

            ApplyUniformTileNameTextMesh(tileNameTextMesh);
        }

        if (tileNameTextMesh != null)
        {
            bool hasName = ShouldShowTileSurfaceLabel(tileData);
            tileNameTextMesh.gameObject.SetActive(hasName);
        }
    }

    public void SetOwnerSign(string ownerName, Color ownerColor, bool visible)
    {
        if (ownerSignObject != null)
        {
            ownerSignObject.SetActive(visible);
        }

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
        SetRendererColor(ownerSignPanelRenderer, plaqueColor);

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

        string signText = FormatOwnerLabel(ownerName);
        Color signTextColor = GetReadableTextColor(ownerColor);
        if (labelFont != null && !string.IsNullOrEmpty(signText))
        {
            labelFont.RequestCharactersInTexture(signText, 128, FontStyle.Bold);
        }

        if (ownerSignFrontTextMesh != null)
        {
            ownerSignFrontTextMesh.text = signText;
            ownerSignFrontTextMesh.color = signTextColor;
            FitOwnerSignTextMesh(ownerSignFrontTextMesh);
        }
    }

    private static void EnsureLabelFont()
    {
        if (labelFont != null && tileNameFont != null)
        {
            return;
        }

        if (labelFont == null)
        {
            labelFont = Resources.Load<Font>("Fonts/MenuFont");
            if (labelFont == null)
            {
                labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        if (tileNameFont == null)
        {
            tileNameFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (tileNameFont == null)
            {
                tileNameFont = labelFont;
            }
        }
    }

    private static Font ResolveTileNameFont(string text)
    {
        Font defaultFont = tileNameFont != null ? tileNameFont : labelFont;
        if (SupportsText(defaultFont, text))
        {
            return defaultFont;
        }

        if (tileNameFallbackFont == null)
        {
            tileNameFallbackFont = Font.CreateDynamicFontFromOSFont(TileNameFallbackFontNames, 64);
        }

        if (SupportsText(tileNameFallbackFont, text))
        {
            return tileNameFallbackFont;
        }

        return defaultFont;
    }

    private static bool SupportsText(Font font, string text)
    {
        if (font == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
            {
                continue;
            }

            if (!font.HasCharacter(c))
            {
                return false;
            }
        }

        return true;
    }

    private GameObject LoadTileModelPrefab(TileData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.tileName))
        {
            return null;
        }

        string tileName = data.tileName;
        string[] searchFolders = GetTileModelSearchFolders(data);
        GameObject modelPrefab = LoadTileModelPrefabFromFolders(tileName, searchFolders);
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
            modelPrefab = LoadTileModelPrefabFromFolders(alias, searchFolders);
            if (modelPrefab != null)
            {
                return modelPrefab;
            }
        }

        return null;
    }

    private static string[] GetTileModelSearchFolders(TileData data)
    {
        List<string> folders = new List<string>();
        if (TryGetLevelNumber(data, out int levelNumber))
        {
            folders.Add($"Prefabs/TileModels/Level{levelNumber}");
            folders.Add($"TileModels/Level{levelNumber}");
        }

        folders.Add("Prefabs/TileModels");
        folders.Add("TileModels");
        return folders.ToArray();
    }

    private static GameObject LoadTileModelPrefabFromFolders(string tileName, string[] folders)
    {
        if (string.IsNullOrWhiteSpace(tileName) || folders == null)
        {
            return null;
        }

        for (int i = 0; i < folders.Length; i++)
        {
            string folder = folders[i];
            if (string.IsNullOrWhiteSpace(folder))
            {
                continue;
            }

            GameObject modelPrefab = Resources.Load<GameObject>($"{folder}/{tileName}");
            if (modelPrefab == null)
            {
                modelPrefab = Resources.Load<GameObject>($"{folder}/{tileName}/{tileName}");
            }
            if (modelPrefab != null)
            {
                return modelPrefab;
            }
        }

        return null;
    }

    private static bool TryGetLevelNumber(TileData data, out int levelNumber)
    {
        levelNumber = 0;
        if (data == null || string.IsNullOrWhiteSpace(data.tileID))
        {
            return false;
        }

        string tileId = data.tileID.Trim();
        if (tileId.Length < 2 || char.ToUpperInvariant(tileId[0]) != 'L')
        {
            return false;
        }

        int digitStart = 1;
        int digitEnd = digitStart;
        while (digitEnd < tileId.Length && char.IsDigit(tileId[digitEnd]))
        {
            digitEnd++;
        }

        return digitEnd > digitStart
            && int.TryParse(tileId.Substring(digitStart, digitEnd - digitStart), out levelNumber)
            && levelNumber > 0;
    }

    private static Quaternion GetTileModelRotation(TileData data)
    {
        if (TryGetLevelNumber(data, out int levelNumber) && levelNumber == 4)
        {
            return Quaternion.Euler(0f, 180f, 0f);
        }

        return Quaternion.identity;
    }

    private void EnsureRuntimeLabels()
    {
        DestroyGeneratedRuntimeLabels();
        Bounds tileBounds = GetTileVisualLocalBounds();

        if (ownerSignObject == null)
        {
            CreateOwnerEdgeSign(
                tileBounds,
                new Color(0.25f, 0.56f, 0.34f, 0.97f));
        }

        if (tileNameTextMesh == null)
        {
            tileNameTextMesh = CreateTileNameLabel(tileBounds);
        }
    }

    private TextMesh CreateTileNameLabel(Bounds tileBounds)
    {
        Transform labelParent = GetOrCreateRuntimeSignParent();

        GameObject textObject = new GameObject("TileNameLabel", typeof(TextMesh));
        textObject.transform.SetParent(labelParent, false);
        textObject.transform.localPosition = new Vector3(tileBounds.center.x, tileBounds.max.y + TileNameSurfaceOffset, tileBounds.center.z);
        textObject.transform.localRotation = GetTileNameLabelRotation();
        textObject.transform.localScale = Vector3.one;

        TextMesh label = textObject.GetComponent<TextMesh>();
        label.text = string.Empty;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.richText = false;
        return label;
    }

    private void CreateOwnerEdgeSign(Bounds tileBounds, Color badgeColor)
    {
        Transform signParent = GetOrCreateRuntimeSignParent();
        Vector3 outward = GetOutwardLocalDirection(tileBounds);
        Vector3 edgePosition = GetBoundsEdgePoint(tileBounds, outward) + outward * OwnerSignEdgePadding;
        float tileTopY = Mathf.Max(0.02f, tileBounds.max.y);
        float panelCenterY = tileTopY + OwnerSignPanelHeight * 0.72f;

        ownerSignObject = new GameObject("OwnerSign");
        ownerSignObject.transform.SetParent(signParent, false);
        ownerSignObject.transform.localPosition = new Vector3(edgePosition.x, 0f, edgePosition.z);
        ownerSignObject.transform.localRotation = Quaternion.LookRotation(-outward, Vector3.up);
        ownerSignObject.transform.localScale = Vector3.one;

        CreateColorCube(
            "LeftPost",
            ownerSignObject.transform,
            new Vector3(-OwnerSignPanelWidth * 0.32f, panelCenterY * 0.52f, 0f),
            new Vector3(0.035f, panelCenterY * 1.04f, 0.035f),
            SignPostColor);
        CreateColorCube(
            "RightPost",
            ownerSignObject.transform,
            new Vector3(OwnerSignPanelWidth * 0.32f, panelCenterY * 0.52f, 0f),
            new Vector3(0.035f, panelCenterY * 1.04f, 0.035f),
            SignPostColor);
        ownerSignPanelRenderer = CreateColorCube(
            "Panel",
            ownerSignObject.transform,
            new Vector3(0f, panelCenterY, 0f),
            new Vector3(OwnerSignPanelWidth, OwnerSignPanelHeight, OwnerSignPanelThickness),
            badgeColor);

        float textSide = GetCameraFacingLocalZSide(ownerSignObject.transform);
        Quaternion textRotation = textSide >= 0f ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        ownerSignFrontTextMesh = CreateOwnerSignTextMesh(
            ownerSignObject.transform,
            "Text",
            new Vector3(0f, panelCenterY, OwnerSignPanelThickness * 1.08f * textSide),
            textRotation);
    }

    private TextMesh CreateOwnerSignTextMesh(Transform parent, string objectName, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject textObject = new GameObject(objectName, typeof(TextMesh));
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = localRotation;
        textObject.transform.localScale = Vector3.one;

        TextMesh textMesh = textObject.GetComponent<TextMesh>();
        textMesh.font = labelFont;
        textMesh.text = string.Empty;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.fontSize = OwnerSignTextFontSize;
        textMesh.characterSize = OwnerSignTextBaseCharacterSize;
        textMesh.lineSpacing = 0.82f;
        textMesh.richText = false;

        MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
        if (textRenderer != null)
        {
            if (labelFont != null && labelFont.material != null)
            {
                textRenderer.sharedMaterial = labelFont.material;
            }

            textRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            textRenderer.receiveShadows = false;
            textRenderer.sortingOrder = SignSortingOrder;
        }

        return textMesh;
    }

    private void FitOwnerSignTextMesh(TextMesh textMesh)
    {
        if (textMesh == null || string.IsNullOrEmpty(textMesh.text))
        {
            return;
        }

        textMesh.characterSize = OwnerSignTextBaseCharacterSize;

        MeshRenderer textRenderer = textMesh.GetComponent<MeshRenderer>();
        if (textRenderer == null)
        {
            return;
        }

        Bounds bounds = textRenderer.localBounds;
        float width = bounds.size.x;
        float height = bounds.size.y;
        if (width <= 0.0001f || height <= 0.0001f)
        {
            return;
        }

        float maxWidth = OwnerSignPanelWidth * 0.78f;
        float maxHeight = OwnerSignPanelHeight * 0.58f;
        float scale = Mathf.Min(maxWidth / width, maxHeight / height, 1f);
        textMesh.characterSize *= Mathf.Clamp(scale, 0.01f, 1f);
    }

    private void ApplyUniformTileNameTextMesh(TextMesh textMesh)
    {
        if (textMesh == null)
        {
            return;
        }

        textMesh.characterSize = TileNameTextBaseCharacterSize;
    }

    private static Material GetTileNameTextMaterial(Font font)
    {
        if (font == null || font.material == null)
        {
            return null;
        }

        if (tileNameTextMaterials.TryGetValue(font, out Material cachedMaterial) && cachedMaterial != null)
        {
            SyncFontTexture(cachedMaterial, font);
            return cachedMaterial;
        }

        Shader shader = ResolveDepthTestedTextShader();
        if (shader == null)
        {
            return font.material;
        }

        Material material = new Material(shader)
        {
            name = $"TileNameText_{font.name}",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = 3000
        };

        SyncFontTexture(material, font);
        tileNameTextMaterials[font] = material;
        return material;
    }

    private static Shader ResolveDepthTestedTextShader()
    {
        if (depthTestedTextShader != null)
        {
            return depthTestedTextShader;
        }

        depthTestedTextShader = Resources.Load<Shader>("Shaders/DepthTestedText");
        if (depthTestedTextShader == null)
        {
            depthTestedTextShader = Shader.Find("Monopoly/DepthTestedText");
        }

        return depthTestedTextShader;
    }

    private static void SyncFontTexture(Material material, Font font)
    {
        if (material == null || font == null || font.material == null)
        {
            return;
        }

        Texture fontTexture = font.material.mainTexture;
        if (fontTexture != null && material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", fontTexture);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }
    }

    private void DestroyGeneratedRuntimeLabels()
    {
        Transform runtimeSigns = transform.Find("RuntimeSigns");
        if (runtimeSigns == null)
        {
            return;
        }

        DestroyRuntimeChild(runtimeSigns, "OwnerSign");
        DestroyRuntimeChild(runtimeSigns, "LandmarkSign");
        DestroyRuntimeChild(runtimeSigns, "TileNameLabel");

        ownerSignObject = null;
        ownerSignPanelRenderer = null;
        ownerSignFrontTextMesh = null;
        ownerSignRoot = null;
        ownerBadge = null;
        ownerText = null;
        tileNameRoot = null;
        tileNameText = null;
        tileNameTextMesh = null;

        if (modelText != null && modelText.transform.IsChildOf(runtimeSigns))
        {
            modelText = null;
        }
    }

    private void DestroyRuntimeChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(child.gameObject);
        }
        else
        {
            DestroyImmediate(child.gameObject);
        }
    }

    private Bounds GetTileVisualLocalBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsRuntimeGeneratedTransform(renderer.transform))
            {
                continue;
            }

            if (currentModelInstance != null && renderer.transform.IsChildOf(currentModelInstance.transform))
            {
                continue;
            }

            Bounds worldBounds = renderer.bounds;
            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 worldCorner = new Vector3(
                            x == 0 ? worldBounds.min.x : worldBounds.max.x,
                            y == 0 ? worldBounds.min.y : worldBounds.max.y,
                            z == 0 ? worldBounds.min.z : worldBounds.max.z);
                        Vector3 localCorner = transform.InverseTransformPoint(worldCorner);

                        if (!hasBounds)
                        {
                            localBounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        return hasBounds ? localBounds : new Bounds(Vector3.zero, new Vector3(1f, 0.2f, 1f));
    }

    private bool IsRuntimeGeneratedTransform(Transform target)
    {
        Transform runtimeSigns = transform.Find("RuntimeSigns");
        return runtimeSigns != null && target != null && target.IsChildOf(runtimeSigns);
    }

    private Quaternion GetTileNameLabelRotation()
    {
        Vector3 localForward = transform.InverseTransformDirection(Vector3.up);
        Vector3 localUp = transform.InverseTransformDirection(Vector3.forward);

        if (localUp.sqrMagnitude < 0.0001f)
        {
            localUp = Vector3.forward;
        }

        return Quaternion.LookRotation(localForward.normalized, -localUp.normalized) * Quaternion.Euler(0f, 180f, 180f);
    }

    private static float GetCameraFacingLocalZSide(Transform ownerSignTransform)
    {
        if (ownerSignTransform == null)
        {
            return -1f;
        }

        Camera cameraToUse = Camera.main;
        if (cameraToUse == null)
        {
            return -1f;
        }

        Vector3 localCameraPosition = ownerSignTransform.InverseTransformPoint(cameraToUse.transform.position);
        return localCameraPosition.z >= 0f ? 1f : -1f;
    }

    private Vector3 GetOutwardLocalDirection(Bounds tileBounds)
    {
        if (hasCustomOutwardLocalDirection)
        {
            return customOutwardLocalDirection;
        }

        Transform slot = transform.parent;
        Transform mapRoot = slot != null ? slot.parent : null;
        if (TryGetActiveRectLoopOutwardDirection(slot, mapRoot, out Vector3 rectWorldDirection))
        {
            return SnapWorldDirectionToLocalAxis(rectWorldDirection);
        }

        Vector3 mapCenter = GetMapCenterWorld(mapRoot);
        Vector3 worldTileCenter = slot != null ? slot.position : transform.TransformPoint(tileBounds.center);
        Vector3 worldDirection = worldTileCenter - mapCenter;
        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude < 0.0001f)
        {
            worldDirection = -transform.forward;
        }

        return SnapWorldDirectionToLocalAxis(worldDirection);
    }

    private Vector3 SnapWorldDirectionToLocalAxis(Vector3 worldDirection)
    {
        Vector3 localDirection = transform.InverseTransformDirection(worldDirection.normalized);
        return SnapLocalDirectionToAxis(localDirection);
    }

    private static Vector3 SnapLocalDirectionToAxis(Vector3 localDirection)
    {
        localDirection.y = 0f;

        if (localDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.back;
        }

        if (Mathf.Abs(localDirection.x) >= Mathf.Abs(localDirection.z))
        {
            return localDirection.x >= 0f ? Vector3.right : Vector3.left;
        }

        return localDirection.z >= 0f ? Vector3.forward : Vector3.back;
    }

    private static bool TryGetActiveRectLoopOutwardDirection(Transform slot, Transform mapRoot, out Vector3 worldDirection)
    {
        worldDirection = Vector3.zero;
        if (slot == null || mapRoot == null)
        {
            return false;
        }

        bool hasBounds = false;
        float minX = 0f;
        float maxX = 0f;
        float minZ = 0f;
        float maxZ = 0f;
        for (int i = 0; i < mapRoot.childCount; i++)
        {
            Transform child = mapRoot.GetChild(i);
            if (child == null || !child.gameObject.activeSelf)
            {
                continue;
            }

            Vector3 localPosition = mapRoot.InverseTransformPoint(child.position);
            if (!hasBounds)
            {
                minX = maxX = localPosition.x;
                minZ = maxZ = localPosition.z;
                hasBounds = true;
                continue;
            }

            minX = Mathf.Min(minX, localPosition.x);
            maxX = Mathf.Max(maxX, localPosition.x);
            minZ = Mathf.Min(minZ, localPosition.z);
            maxZ = Mathf.Max(maxZ, localPosition.z);
        }

        if (!hasBounds)
        {
            return false;
        }

        Vector3 slotLocalPosition = mapRoot.InverseTransformPoint(slot.position);
        float distanceToLeft = Mathf.Abs(slotLocalPosition.x - minX);
        float distanceToRight = Mathf.Abs(maxX - slotLocalPosition.x);
        float distanceToBottom = Mathf.Abs(slotLocalPosition.z - minZ);
        float distanceToTop = Mathf.Abs(maxZ - slotLocalPosition.z);
        float nearest = Mathf.Min(
            Mathf.Min(distanceToLeft, distanceToRight),
            Mathf.Min(distanceToBottom, distanceToTop));

        Vector3 localDirection;
        if (Mathf.Approximately(nearest, distanceToLeft) && Mathf.Approximately(nearest, distanceToBottom))
        {
            localDirection = Mathf.Abs(slotLocalPosition.x) >= Mathf.Abs(slotLocalPosition.z) ? Vector3.left : Vector3.back;
        }
        else if (Mathf.Approximately(nearest, distanceToRight) && Mathf.Approximately(nearest, distanceToBottom))
        {
            localDirection = Mathf.Abs(slotLocalPosition.x) >= Mathf.Abs(slotLocalPosition.z) ? Vector3.right : Vector3.back;
        }
        else if (Mathf.Approximately(nearest, distanceToLeft) && Mathf.Approximately(nearest, distanceToTop))
        {
            localDirection = Mathf.Abs(slotLocalPosition.x) >= Mathf.Abs(slotLocalPosition.z) ? Vector3.left : Vector3.forward;
        }
        else if (Mathf.Approximately(nearest, distanceToRight) && Mathf.Approximately(nearest, distanceToTop))
        {
            localDirection = Mathf.Abs(slotLocalPosition.x) >= Mathf.Abs(slotLocalPosition.z) ? Vector3.right : Vector3.forward;
        }
        else if (Mathf.Approximately(nearest, distanceToLeft))
        {
            localDirection = Vector3.left;
        }
        else if (Mathf.Approximately(nearest, distanceToRight))
        {
            localDirection = Vector3.right;
        }
        else if (Mathf.Approximately(nearest, distanceToBottom))
        {
            localDirection = Vector3.back;
        }
        else
        {
            localDirection = Vector3.forward;
        }

        worldDirection = mapRoot.TransformDirection(localDirection);
        worldDirection.y = 0f;
        return worldDirection.sqrMagnitude > 0.0001f;
    }

    private static Vector3 GetBoundsEdgePoint(Bounds bounds, Vector3 direction)
    {
        Vector3 center = bounds.center;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return new Vector3(center.x, 0f, bounds.min.z);
        }

        direction.Normalize();

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.z))
        {
            return new Vector3(direction.x >= 0f ? bounds.max.x : bounds.min.x, 0f, center.z);
        }

        return new Vector3(center.x, 0f, direction.z >= 0f ? bounds.max.z : bounds.min.z);
    }

    private static Vector3 GetMapCenterWorld(Transform mapRoot)
    {
        if (mapRoot == null || mapRoot.childCount == 0)
        {
            return mapRoot != null ? mapRoot.position : Vector3.zero;
        }

        Vector3 center = Vector3.zero;
        int count = 0;
        for (int i = 0; i < mapRoot.childCount; i++)
        {
            Transform child = mapRoot.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (!child.gameObject.activeSelf)
            {
                continue;
            }

            center += child.position;
            count++;
        }

        return count > 0 ? center / count : mapRoot.position;
    }

    private Renderer CreateColorCube(string objectName, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = localScale;

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        Renderer renderer = cube.GetComponent<Renderer>();
        ConfigureSignRenderer(renderer);
        SetRendererColor(renderer, color);
        return renderer;
    }

    private static void ConfigureSignRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private Transform GetOrCreateRuntimeSignParent()
    {
        Transform existing = transform.Find("RuntimeSigns");
        if (existing != null)
        {
            return existing;
        }

        GameObject parentObject = new GameObject("RuntimeSigns");
        parentObject.transform.SetParent(transform, false);
        parentObject.transform.localPosition = Vector3.zero;
        parentObject.transform.localRotation = Quaternion.identity;
        parentObject.transform.localScale = Vector3.one;
        return parentObject.transform;
    }

    private void CreateSignPost(RectTransform parent, Vector2 anchoredPosition)
    {
        GameObject postObject = new GameObject("Post", typeof(RectTransform), typeof(Image));
        postObject.transform.SetParent(parent, false);

        RectTransform postRect = postObject.GetComponent<RectTransform>();
        postRect.anchorMin = new Vector2(0.5f, 0.5f);
        postRect.anchorMax = new Vector2(0.5f, 0.5f);
        postRect.pivot = new Vector2(0.5f, 1f);
        postRect.anchoredPosition = anchoredPosition;
        postRect.sizeDelta = new Vector2(7f, 26f);

        Image postImage = postObject.GetComponent<Image>();
        postImage.color = SignPostColor;
        postImage.raycastTarget = false;
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
                if (path == "FrontPlaque")
                {
                    ConfigureSignRenderer(renderer);
                }
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

        SetRendererColor(renderer, color);
    }

    private void SetRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null)
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

    private static string FormatTileNameLabel(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        string compactName = rawName.Trim();
        if (compactName.Contains("\n") || compactName.Length <= 4)
        {
            return compactName;
        }

        int lineLength = compactName.Length <= 6 ? 3 : 4;
        List<string> lines = new List<string>();
        for (int i = 0; i < compactName.Length; i += lineLength)
        {
            int count = Mathf.Min(lineLength, compactName.Length - i);
            lines.Add(compactName.Substring(i, count));
        }

        return string.Join("\n", lines);
    }

    private static bool IsPropertyTile(TileData data)
    {
        return data != null && data.tileCost > 0;
    }

    private static bool IsStartTile(TileData data)
    {
        if (data == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(data.tileID))
        {
            string tileId = data.tileID.Trim().ToUpperInvariant();
            if (tileId.StartsWith("ST") || tileId.Contains("-ST"))
            {
                return true;
            }
        }

        return !string.IsNullOrWhiteSpace(data.tileName) && data.tileName.Contains("起点");
    }

    private static string GetGridLabel(TileData data)
    {
        if (data == null)
        {
            return string.Empty;
        }

        if (data.tileType == TileType.KongDi)
        {
            return string.Empty;
        }

        return IsStartTile(data) ? "起点" : data.tileType.ToRealName();
    }

    private static string GetTileSurfaceLabel(TileData data)
    {
        if (IsStartTile(data))
        {
            return "起点";
        }

        return IsPropertyTile(data) ? FormatTileNameLabel(data.tileName) : string.Empty;
    }

    private static bool ShouldShowTileSurfaceLabel(TileData data)
    {
        return IsStartTile(data) || (IsPropertyTile(data) && !string.IsNullOrWhiteSpace(data.tileName));
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
