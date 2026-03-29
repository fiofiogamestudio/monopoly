using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceResolution : MonoBehaviour
{
    public void Awake()
    {
        // force to 1920x1080, and windowed mode
        Screen.SetResolution(1920, 1080, false);
    }
}
