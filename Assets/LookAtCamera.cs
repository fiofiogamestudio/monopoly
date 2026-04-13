using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public bool useMainCamera = true;
    public Camera targetCamera;
    public bool invertForward = false;

    private void LateUpdate()
    {
        FaceCamera();
    }

    private void FaceCamera()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : (useMainCamera ? Camera.main : null);
        if (cameraToUse == null)
        {
            return;
        }

        Quaternion cameraRotation = cameraToUse.transform.rotation;
        Quaternion facingRotation = invertForward
            ? cameraRotation * Quaternion.Euler(0f, 180f, 0f)
            : cameraRotation;

        transform.rotation = facingRotation;
    }
}
