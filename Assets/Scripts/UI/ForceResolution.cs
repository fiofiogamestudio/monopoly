using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class ForceResolution : MonoBehaviour
{
    private const int TargetWidth = 1920;
    private const int TargetHeight = 1080;
    private const int MinWidth = 1920;
    private const int MinHeight = 1080;
    private const string ResolutionWidthPrefsKey = "display_width";
    private const string ResolutionHeightPrefsKey = "display_height";
    private const string FullscreenPrefsKey = "display_fullscreen";

    private void Awake()
    {
        int width = PlayerPrefs.GetInt(ResolutionWidthPrefsKey, TargetWidth);
        int height = PlayerPrefs.GetInt(ResolutionHeightPrefsKey, TargetHeight);
        bool clamped = false;

        if (width < MinWidth || height < MinHeight)
        {
            width = TargetWidth;
            height = TargetHeight;
            clamped = true;
        }

        bool fullscreen = PlayerPrefs.GetInt(FullscreenPrefsKey, 0) != 0;
        FullScreenMode mode = fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        Screen.SetResolution(width, height, mode);

        if (clamped)
        {
            PlayerPrefs.SetInt(ResolutionWidthPrefsKey, width);
            PlayerPrefs.SetInt(ResolutionHeightPrefsKey, height);
            PlayerPrefs.Save();
        }
    }
}
