using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridConfig : MonoBehaviour
{
    public Vector3 gridOffset;

    public void Awake()
    {
        this.GetComponent<MeshRenderer>().enabled = false;
    }
}
