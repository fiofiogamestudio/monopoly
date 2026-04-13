using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridConfig : MonoBehaviour
{
    public Vector3 gridOffset;

    public void Awake()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }
}
