// ============================================================
// TopDownCameraFollow.cs  –  Week 3
// A simple MonoBehaviour that sits on the Main Camera and
// smoothly follows a target Transform from above.
// It is NOT a NetworkBehaviour because the camera is entirely
// local – each player has their own camera on their own machine.
// ============================================================

// UnityEngine for MonoBehaviour, Transform, Vector3, Lerp…
using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    // ---------------------------------------------------------
    // Inspector fields
    // ---------------------------------------------------------

    // How far above and behind the target the camera hovers.
    // (0, 10, -8) gives a nice 60-degree top-down angle when
    // combined with a (60, 0, 0) rotation on the camera.
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -8f);

    // How quickly the camera chases the target (units per second).
    // Higher = snappier, lower = more cinematic lag.
    [SerializeField] private float followSpeed = 10f;

    // ---------------------------------------------------------
    // Private state
    // ---------------------------------------------------------

    // The Transform we're following.  Set at runtime by
    // LocalPlayerCameraTarget when the local player spawns.
    private Transform target;

    // ---------------------------------------------------------
    // PUBLIC API: called by LocalPlayerCameraTarget
    // ---------------------------------------------------------

    // SetTarget is called once – when the local player spawns –
    // to tell the camera which object to follow from now on.
    public void SetTarget(Transform newTarget)
    {
        // Store the reference so LateUpdate can use it every frame.
        target = newTarget;
    }

    // ---------------------------------------------------------
    // LateUpdate runs AFTER all Update() calls each frame.
    // Following in LateUpdate means the player has already moved
    // this frame, so the camera never lags one frame behind.
    // ---------------------------------------------------------
    private void LateUpdate()
    {
        // If no target has been assigned yet, do nothing.
        // This happens during the first frames before a player spawns.
        if (target == null) return;

        // Calculate where the camera WANTS to be (target + offset).
        Vector3 desiredPosition = target.position + offset;

        // Vector3.Lerp smoothly interpolates from the current
        // camera position toward desiredPosition each frame.
        // followSpeed * Time.deltaTime controls how fast it catches up
        // while keeping the movement frame-rate independent.
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        // Apply the smooth position to the camera.
        transform.position = smoothedPosition;

        // Always rotate the camera to look directly at the target
        // (the player's position, not their feet offset).
        transform.LookAt(target.position);
    }
}
