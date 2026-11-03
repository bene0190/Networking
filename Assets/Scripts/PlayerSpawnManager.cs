// ============================================================
// PlayerSpawnManager.cs  –  Week 2
// Runs on the SERVER only and places each player at a unique
// spawn point when they join.  Server-authoritative spawn
// prevents clients from cheating their starting position.
// ============================================================

// Unity.Netcode for NetworkBehaviour, IsServer, etc.
using Unity.Netcode;

// UnityEngine for Transform, Debug, GameObject…
using UnityEngine;

// NetworkBehaviour extends MonoBehaviour with networking hooks.
public class PlayerSpawnManager : NetworkBehaviour
{
    // ---------------------------------------------------------
    // static field: shared across ALL instances so each new
    // player gets the NEXT spawn point, not the same one.
    // ---------------------------------------------------------

    // nextSpawnIndex increments every time a player spawns,
    // cycling through all available spawn points in order.
    private static int nextSpawnIndex;

    // ---------------------------------------------------------
    // OnNetworkSpawn is called by NGO right after this
    // NetworkObject is spawned across the network.
    // We do our spawn-point logic here rather than in Start()
    // because we need IsServer to be valid, which it isn't yet
    // during Start().
    // ---------------------------------------------------------
    public override void OnNetworkSpawn()
    {
        // Only the SERVER should move players – if we're a client
        // we return immediately.  Clients receive the corrected
        // position via NetworkTransform automatically.
        if (!IsServer) return;

        // Find every GameObject tagged "SpawnPoint" in the scene.
        // Tags are fast to search and keep the code decoupled from
        // specific GameObject names.
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");

        // Safety check: warn the developer if no spawn points
        // were placed in the scene (easy mistake to forget!).
        if (spawnPointObjects.Length == 0)
        {
            Debug.LogWarning("[PlayerSpawnManager] No objects with the 'SpawnPoint' tag were found.  " +
                             "Please add empty GameObjects tagged 'SpawnPoint' to your scene.");
            return;
        }

        // Use modulo (%) so the index wraps around when we have
        // more players than spawn points.
        Transform selectedSpawnPoint = spawnPointObjects[nextSpawnIndex % spawnPointObjects.Length].transform;

        // CharacterController fights against manual position
        // changes – we must disable it, move, then re-enable.
        CharacterController characterController = GetComponent<CharacterController>();

        // Disable the CharacterController so we can warp the
        // player without physics fighting us.
        if (characterController != null) characterController.enabled = false;

        // Teleport the player to the chosen spawn point position
        // and copy its rotation (so players face the right way).
        transform.position = selectedSpawnPoint.position;
        transform.rotation = selectedSpawnPoint.rotation;

        // Re-enable movement after repositioning is complete.
        if (characterController != null) characterController.enabled = true;

        // Advance the index so the NEXT player gets a different
        // spawn point.  Wrapping with modulo keeps it in bounds.
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPointObjects.Length;
    }
}
