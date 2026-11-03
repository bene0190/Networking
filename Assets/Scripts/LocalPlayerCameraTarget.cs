// ============================================================
// LocalPlayerCameraTarget.cs  –  Week 3
// A NetworkBehaviour on the NetworkPlayer prefab that tells
// the Main Camera to follow THIS player – but ONLY if this
// machine owns the player (i.e., it's the LOCAL player).
//
// WHY: In a multiplayer session every player's prefab spawns on
// every machine.  Without an ownership check the camera would
// try to follow all of them at once!
// ============================================================

// Unity.Netcode for NetworkBehaviour and IsOwner.
using Unity.Netcode;

// UnityEngine for Camera, transform, etc.
using UnityEngine;

public class LocalPlayerCameraTarget : NetworkBehaviour
{
    // ---------------------------------------------------------
    // OnNetworkSpawn is NGO's reliable callback for when this
    // object has been fully spawned and ownership is known.
    // It's safer than Start() for anything that touches IsOwner.
    // ---------------------------------------------------------
    public override void OnNetworkSpawn()
    {
        // IsOwner is true ONLY on the machine that owns this player.
        // If we're NOT the owner (i.e., we're looking at someone
        // else's player) we do nothing – their camera is their business.
        if (!IsOwner) return;

        // Grab the main camera.  Camera.main searches by the
        // "MainCamera" tag, which Unity sets by default.
        Camera mainCamera = Camera.main;

        // Guard: if there's no main camera in the scene, warn us.
        if (mainCamera == null)
        {
            Debug.LogWarning("[LocalPlayerCameraTarget] No Main Camera found in the scene!");
            return;
        }

        // Look for the TopDownCameraFollow script we placed on
        // the camera in the Editor builder.
        TopDownCameraFollow cameraFollow = mainCamera.GetComponent<TopDownCameraFollow>();

        // Guard: if the follow script isn't attached, warn the developer.
        if (cameraFollow == null)
        {
            Debug.LogWarning("[LocalPlayerCameraTarget] TopDownCameraFollow not found on Main Camera!");
            return;
        }

        // Tell the camera to follow THIS player's transform.
        // From this point on, the camera smoothly tracks us.
        cameraFollow.SetTarget(transform);
    }
}
