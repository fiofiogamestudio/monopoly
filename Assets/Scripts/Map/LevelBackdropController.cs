using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Renderer))]
public class LevelBackdropController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private MapLoader mapLoader;

    [Header("Resources")]
    [SerializeField] private string level1TexturePath = "LevelBackgrounds/Level1";
    [SerializeField] private string level2TexturePath = "LevelBackgrounds/Level2";
    [SerializeField] private string level3TexturePath = "LevelBackgrounds/Level3";
    [SerializeField] private string level4TexturePath = "LevelBackgrounds/Level4";
    [SerializeField] private string level5TexturePath = "LevelBackgrounds/Level5";
    [SerializeField] private string level6TexturePath = "LevelBackgrounds/Level6";

    [Header("Look")]
    [SerializeField] private Color tint = Color.white;
    [SerializeField] private bool flipTextureVertically = true;
    [SerializeField] private bool cropSpriteToSquareArea = true;
    [SerializeField] private bool disableShadowCasting = true;
    [SerializeField] private bool disableShadowReceiving = true;

    private Material runtimeMaterial;
    private Vector3 _baseLocalScale = Vector3.one;
    private bool _hasCapturedBaseScale;

    private void Reset()
    {
        targetRenderer = GetComponent<Renderer>();
    }

    private void Awake()
    {
        EnsureRenderer();
        CaptureBaseScale();
        ApplyRendererFlags();
    }

    private void Start()
    {
        ApplyForCurrentLevel();
    }

    public void ApplyForCurrentLevel()
    {
        EnsureRenderer();
        if (targetRenderer == null)
        {
            Debug.LogWarning("[LevelBackdrop] Renderer is missing.");
            return;
        }

        int levelIndex = ResolveLevelIndex();
        Sprite sprite = LoadSpriteForLevel(levelIndex);
        if (sprite == null)
        {
            Debug.LogWarning($"[LevelBackdrop] Missing backdrop texture for level {levelIndex}.");
            return;
        }

        runtimeMaterial ??= targetRenderer.material;
        ApplySprite(runtimeMaterial, sprite);
    }

    private void EnsureRenderer()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }
    }

    private void CaptureBaseScale()
    {
        if (_hasCapturedBaseScale)
        {
            return;
        }

        _baseLocalScale = transform.localScale;
        _hasCapturedBaseScale = true;
    }

    private void ApplyRendererFlags()
    {
        if (targetRenderer == null)
        {
            return;
        }

        if (disableShadowCasting)
        {
            targetRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        if (disableShadowReceiving)
        {
            targetRenderer.receiveShadows = false;
        }
    }

    private int ResolveLevelIndex()
    {
        if (mapLoader == null)
        {
            mapLoader = FindFirstObjectByType<MapLoader>();
        }

        if (mapLoader != null && mapLoader.CurrentLevel >= GameSessionConfig.MinLevelIndex)
        {
            return mapLoader.CurrentLevel;
        }

        return GameSessionConfig.ResolveLevel(GameSessionConfig.DefaultLevelIndex);
    }

    private Sprite LoadSpriteForLevel(int levelIndex)
    {
        string resourcePath = levelIndex switch
        {
            1 => level1TexturePath,
            2 => level2TexturePath,
            3 => level3TexturePath,
            4 => level4TexturePath,
            5 => level5TexturePath,
            6 => level6TexturePath,
            _ => level1TexturePath,
        };

        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                sprite.name = texture.name;
            }
        }

        if (sprite != null && sprite.texture != null)
        {
            sprite.texture.wrapMode = TextureWrapMode.Clamp;
        }

        return sprite;
    }

    private void ApplySprite(Material material, Sprite sprite)
    {
        if (material == null || sprite == null || sprite.texture == null)
        {
            return;
        }

        Texture2D texture = sprite.texture;
        Vector2 appliedScale;
        Vector2 appliedOffset;
        CalculateSpriteUv(sprite, out appliedScale, out appliedOffset);

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_MainTex", appliedScale);
            material.SetTextureOffset("_MainTex", appliedOffset);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", appliedScale);
            material.SetTextureOffset("_BaseMap", appliedOffset);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", tint);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0f);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        transform.localScale = _baseLocalScale;
    }

    private void CalculateSpriteUv(Sprite sprite, out Vector2 appliedScale, out Vector2 appliedOffset)
    {
        Rect textureRect = sprite.textureRect;
        Texture2D texture = sprite.texture;

        float textureWidth = Mathf.Max(1f, texture.width);
        float textureHeight = Mathf.Max(1f, texture.height);

        float rectWidth = Mathf.Max(1f, textureRect.width);
        float rectHeight = Mathf.Max(1f, textureRect.height);

        float baseScaleX = rectWidth / textureWidth;
        float baseScaleY = rectHeight / textureHeight;
        float baseOffsetX = textureRect.x / textureWidth;
        float baseOffsetY = textureRect.y / textureHeight;

        float cropScaleX = 1f;
        float cropScaleY = 1f;
        float cropOffsetX = 0f;
        float cropOffsetY = 0f;

        if (cropSpriteToSquareArea)
        {
            CaptureBaseScale();
            float boardAspect = Mathf.Max(0.001f, _baseLocalScale.x) / Mathf.Max(0.001f, _baseLocalScale.z);
            float spriteAspect = rectWidth / rectHeight;

            if (spriteAspect > boardAspect)
            {
                cropScaleX = boardAspect / spriteAspect;
                cropOffsetX = (1f - cropScaleX) * 0.5f;
            }
            else if (spriteAspect < boardAspect)
            {
                cropScaleY = spriteAspect / boardAspect;
                cropOffsetY = (1f - cropScaleY) * 0.5f;
            }
        }

        float positiveScaleX = baseScaleX * cropScaleX;
        float positiveScaleY = baseScaleY * cropScaleY;
        float positiveOffsetX = baseOffsetX + baseScaleX * cropOffsetX;
        float positiveOffsetY = baseOffsetY + baseScaleY * cropOffsetY;

        appliedScale = new Vector2(positiveScaleX, positiveScaleY);
        appliedOffset = new Vector2(positiveOffsetX, positiveOffsetY);

        if (flipTextureVertically)
        {
            appliedScale.y *= -1f;
            appliedOffset.y = positiveOffsetY + positiveScaleY;
        }
    }
}
