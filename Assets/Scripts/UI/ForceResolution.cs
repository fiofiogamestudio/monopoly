using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class ForceResolution : MonoBehaviour
{
    private const int TargetWidth = 1920;
    private const int TargetHeight = 1080;
    private const string ResolutionWidthPrefsKey = "display_width";
    private const string ResolutionHeightPrefsKey = "display_height";
    private const string FullscreenPrefsKey = "display_fullscreen";

    private void Awake()
    {
        int width = PlayerPrefs.GetInt(ResolutionWidthPrefsKey, TargetWidth);
        int height = PlayerPrefs.GetInt(ResolutionHeightPrefsKey, TargetHeight);
        bool fullscreen = PlayerPrefs.GetInt(FullscreenPrefsKey, 0) != 0;
        FullScreenMode mode = fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        Screen.SetResolution(width, height, mode);
    }
}
