// ============================================================
// PlayerColorManager.cs  –  Week 3
// Uses a NetworkVariable<int> to synchronise each player's
// color across ALL clients.  This is the classic Week 3
// NetworkVariable demo: the server sets the value once and
// every client automatically receives the update via the
// OnValueChanged callback.
// ============================================================

// Unity.Netcode for NetworkBehaviour, NetworkVariable, etc.
using Unity.Netcode;

// UnityEngine for Renderer, Material, Debug…
using UnityEngine;

public class PlayerColorManager : NetworkBehaviour
{
    // ---------------------------------------------------------
    // Inspector fields
    // ---------------------------------------------------------

    // The Renderer (MeshRenderer) on the player capsule.
    // We change its material to set the player's color.
    [SerializeField] private Renderer playerRenderer;

    // An array of materials – one per possible player color.
    // The server picks an index based on the player's ClientId.
    [SerializeField] private Material[] playerMaterials;

    // ---------------------------------------------------------
    // NetworkVariable
    // KEY CONCEPT: NetworkVariable<T> is automatically
    // synchronised by NGO. When the SERVER changes .Value,
    // all clients receive the new value and fire OnValueChanged.
    // Read permission: Everyone (all clients can read it).
    // Write permission: Server only (only server can set it).
    // ---------------------------------------------------------
    private NetworkVariable<int> colorIndex = new NetworkVariable<int>(
        0,                                          // default value
        NetworkVariableReadPermission.Everyone,     // clients can read
        NetworkVariableWritePermission.Server       // only server writes
    );

    // ---------------------------------------------------------
    // OnNetworkSpawn – runs after spawn on ALL machines.
    // ---------------------------------------------------------
    public override void OnNetworkSpawn()
    {
        // Subscribe to changes so that whenever colorIndex.Value
        // changes (because the server set it), HandleColorChanged
        // is called automatically on every client.
        colorIndex.OnValueChanged += HandleColorChanged;

        // SERVER SIDE: decide which color this player gets.
        if (IsServer && playerMaterials != null && playerMaterials.Length > 0)
        {
            // OwnerClientId is unique per connected client.
            // Modulo wraps the ID into our materials array length,
            // so with 4 materials: client 0=Red, 1=Blue, 2=Green, 3=Yellow,
            // 4=Red again, etc.
            colorIndex.Value = (int)(OwnerClientId % (ulong)playerMaterials.Length);
        }

        // ALL CLIENTS (including server/host): apply whatever color
        // the NetworkVariable currently holds.  This handles the case
        // where a late-joining client spawns and needs the current value.
        ApplyColor(colorIndex.Value);
    }

    // ---------------------------------------------------------
    // OnNetworkDespawn – runs when this object is despawned.
    // Always unsubscribe from events here to prevent memory leaks
    // and callbacks firing on destroyed objects.
    // ---------------------------------------------------------
    public override void OnNetworkDespawn()
    {
        // Unsubscribe from the NetworkVariable callback.
        colorIndex.OnValueChanged -= HandleColorChanged;
    }

    // ---------------------------------------------------------
    // HandleColorChanged – the NetworkVariable callback.
    // NGO calls this on every client whenever the server changes
    // colorIndex.Value.  previousValue is what it WAS,
    // newValue is what it IS NOW.
    // ---------------------------------------------------------
    private void HandleColorChanged(int previousValue, int newValue)
    {
        // Apply the new material to our renderer.
        ApplyColor(newValue);
    }

    // ---------------------------------------------------------
    // ApplyColor – the actual material-swap logic.
    // Kept separate so OnNetworkSpawn can also call it directly.
    // ---------------------------------------------------------
    private void ApplyColor(int index)
    {
        // Guard against bad state (renderer or materials not set).
        if (playerRenderer == null || playerMaterials == null || playerMaterials.Length == 0)
        {
            Debug.LogWarning("[PlayerColorManager] playerRenderer or playerMaterials not assigned!");
            return;
        }

        // Clamp in case of an out-of-range index (defensive coding).
        int safeIndex = Mathf.Clamp(index, 0, playerMaterials.Length - 1);

        // Apply the material to the renderer.
        // sharedMaterial would share across all instances;
        // we assign to .material to get an instance copy so
        // each player's color is independent.
        playerRenderer.material = playerMaterials[safeIndex];
    }
}
