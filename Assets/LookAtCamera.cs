using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{

    // Start is called before the first frame update
    public void Update()
    {
        // Look at main camera
        if (Camera.main != null)
        {
            transform.LookAt(new Vector3(1000, 1000, 1000));
        }
    }
}
