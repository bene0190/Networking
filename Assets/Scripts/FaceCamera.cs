// ============================================================
// FaceCamera.cs
// Keeps a GameObject (e.g. overhead health bar canvas) facing
// the camera at all times, preventing it from rotating with
// the parent character or appearing edge-on.
// ============================================================

using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [Tooltip("Target camera to face. If null, automatically uses Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("If true, matches the full camera rotation (pitch, yaw, roll) so the UI remains perpendicular to the view axis.")]
    [SerializeField] private bool matchFullCameraRotation = true;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        if (matchFullCameraRotation)
        {
            // Fully aligns the canvas rotation to the camera view plane
            transform.rotation = targetCamera.transform.rotation;
        }
        else
        {
            // Yaw-only billboarding: faces camera azimuth while staying upright
            Vector3 lookDir = transform.position - targetCamera.transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }
}
