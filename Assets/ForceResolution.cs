using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class ForceResolution : MonoBehaviour
{
    private const int TargetWidth = 1920;
    private const int TargetHeight = 1080;

    private void Awake()
    {
        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.fullScreen = false;
        Screen.SetResolution(TargetWidth, TargetHeight, FullScreenMode.Windowed);
    }
}
