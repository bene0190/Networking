// ============================================================
// NetworkedButton.cs  –  Week 5
// A NetworkBehaviour on an interactable cylinder in the scene.
// Demonstrates a proximity-based interaction pattern:
//   1. Player walks into the trigger zone.
//   2. Player presses E.
//   3. Client sends a Server RPC.
//   4. Server validates and sends an Everyone RPC.
//   5. All clients see the button light turn green.
//
// This same pattern (proximity check + E key + Server RPC +
// Everyone RPC) is used for doors, pickups, objectives, etc.
// ============================================================

// Unity.Netcode for NetworkBehaviour, Rpc, SendTo…
using Unity.Netcode;

// UnityEngine for Collider, Input, Light, Debug…
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class NetworkedButton : NetworkBehaviour
{
    // ---------------------------------------------------------
    // Inspector fields
    // ---------------------------------------------------------

    // The Point Light child of the button cylinder.
    // Its color changes from Red → Green when activated.
    [SerializeField] public Light buttonLight;

    // ---------------------------------------------------------
    // Private state
    // ---------------------------------------------------------

    // Tracks whether the LOCAL player's character is inside the
    // trigger volume right now.  Only meaningful on the local machine.
    private bool isPlayerInTrigger = false;

    // ---------------------------------------------------------
    // OnTriggerEnter – fires when ANY collider enters the zone.
    // We check ownership to make sure only the LOCAL player's
    // character sets the flag (not remote players' characters).
    // ---------------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        // Does the entering object have the "Player" tag?
        // (Quick tag check before more expensive GetComponent calls.)
        if (!other.CompareTag("Player")) return;

        // Does it have a NetworkObject? (Is it a networked player?)
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null) return;

        // Is THIS the LOCAL player (not a remote player)?
        // We only set the flag for the owner so that only the
        // player standing on the button can press E, not everyone.
        if (netObj.IsOwner)
        {
            isPlayerInTrigger = true;
            Debug.Log("[NetworkedButton] Local player entered trigger zone.  Press E to interact.");
        }
    }

    // ---------------------------------------------------------
    // OnTriggerExit – fires when a collider leaves the zone.
    // Mirror of OnTriggerEnter: clear the flag for the local player.
    // ---------------------------------------------------------
    private void OnTriggerExit(Collider other)
    {
        // Same ownership check as OnTriggerEnter.
        if (!other.CompareTag("Player")) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null) return;

        if (netObj.IsOwner)
        {
            // Player left the zone – disable interaction until they return.
            isPlayerInTrigger = false;
            Debug.Log("[NetworkedButton] Local player left trigger zone.");
        }
    }

    // ---------------------------------------------------------
    // Update – check for the E key only when the local player
    // is standing in the trigger zone.
    // ---------------------------------------------------------
    private void Update()
    {
        if (!isPlayerInTrigger) return;

        bool ePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ePressed = true;
        }
        if (!ePressed && Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
        {
            ePressed = true;
        }
#endif

        if (!ePressed)
        {
            try
            {
                ePressed = Input.GetKeyDown(KeyCode.E);
            }
            catch { }
        }

        if (ePressed)
        {
            // Ask the server to validate and broadcast the interaction.
            RequestInteractRpc();
        }
    }

    // ---------------------------------------------------------
    // RequestInteractRpc – [Rpc(SendTo.Server)]
    // The server receives this request and decides whether to
    // allow the interaction.  (In this demo it's always allowed
    // once the server confirms the request arrived.)
    // ---------------------------------------------------------
    [Rpc(SendTo.Server)]
    private void RequestInteractRpc()
    {
        // TEACHER NOTE: In a production game you'd validate here
        // (e.g., "is the player close enough on the server?").
        // For this demo we trust that the client-side trigger
        // correctly filtered who can interact.

        Debug.Log("[NetworkedButton] Server received interaction request – broadcasting to all.");

        // Approved!  Tell every client to update the button visuals.
        BroadcastInteractRpc();
    }

    // ---------------------------------------------------------
    // BroadcastInteractRpc – [Rpc(SendTo.Everyone)]
    // Runs on every connected machine.  Changes the light color
    // so ALL players can see the button has been activated.
    // ---------------------------------------------------------
    [Rpc(SendTo.Everyone)]
    private void BroadcastInteractRpc()
    {
        // Guard: if the light reference wasn't assigned, warn and exit.
        if (buttonLight == null)
        {
            Debug.LogWarning("[NetworkedButton] buttonLight is not assigned!");
            return;
        }

        // Change the light to green to signal activation.
        // Running this on Everyone means the visual is consistent
        // on every client's screen at (approximately) the same time.
        buttonLight.color = Color.green;

        // Log which machine is processing this broadcast.
        Debug.Log($"[NetworkedButton] Button activated! " +
                  $"(Running on: {(IsServer ? "Server" : "Client")})");
    }
}
