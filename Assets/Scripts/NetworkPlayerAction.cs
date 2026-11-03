// ============================================================
// NetworkPlayerAction.cs  –  Week 5
// Demonstrates the full RPC validation pattern:
//   1. OWNER presses Space.
//   2. Client sends [Rpc(SendTo.Server)] to request the action.
//   3. SERVER validates the cooldown (anti-cheat!).
//   4. If valid, server sends [Rpc(SendTo.Everyone)] to play
//      the particle effect on ALL machines simultaneously.
//
// This is the gold standard for any action that can be abused
// (shooting, dashing, abilities, etc.) — Week 6 extends this!
// ============================================================

// Unity.Netcode for NetworkBehaviour, Rpc, SendTo, OwnerClientId…
using Unity.Netcode;

// UnityEngine for Input, ParticleSystem, Time, Debug…
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class NetworkPlayerAction : NetworkBehaviour
{
    // ---------------------------------------------------------
    // Inspector fields
    // ---------------------------------------------------------

    // The ParticleSystem child object on the player prefab.
    // Plays a burst of particles when the action is triggered.
    [SerializeField] public ParticleSystem actionParticleSystem;

    // ---------------------------------------------------------
    // Private state
    // ---------------------------------------------------------

    // Minimum seconds between allowed actions.
    // The SERVER enforces this – clients cannot bypass it.
    private float actionCooldown = 1.0f;

    // The server-side timestamp of the last successful action.
    // Comparing Time.time against this enforces the cooldown.
    private float lastActionTime = -999f; // Start negative so first action is always allowed.

    // ---------------------------------------------------------
    // Update – runs every frame on every machine.
    // The IsOwner guard ensures only the local player reads input.
    // ---------------------------------------------------------
    private void Update()
    {
        // Only the player who OWNS this object should send actions.
        // Without this, every player's Space bar would trigger
        // an action on every player simultaneously!
        if (!IsOwner) return;

        bool spacePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            spacePressed = true;
        }
        if (!spacePressed && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            spacePressed = true;
        }
#endif

        if (!spacePressed)
        {
            try
            {
                spacePressed = Input.GetKeyDown(KeyCode.Space);
            }
            catch { }
        }

        if (spacePressed)
        {
            // Ask the server if this action is allowed.
            RequestActionRpc();
        }
    }

    // ---------------------------------------------------------
    // RequestActionRpc – [Rpc(SendTo.Server)]
    // This runs on the SERVER.  The client is simply making a
    // request; the server decides whether to approve it.
    // ---------------------------------------------------------
    [Rpc(SendTo.Server)]
    private void RequestActionRpc()
    {
        // SERVER COOLDOWN VALIDATION:
        // Compare the current server time with when the action was
        // last successfully performed.  If not enough time has
        // passed, REJECT the request.  The client cannot override
        // this – it runs entirely on the authoritative server!
        if (Time.time - lastActionTime < actionCooldown)
        {
            // Blocked!  Log a warning so the teacher can demonstrate
            // server-side anti-cheat working in the Console.
            Debug.Log($"[NetworkPlayerAction] Action BLOCKED by Server: " +
                      $"Cooldown active for Player {OwnerClientId}. " +
                      $"Wait {actionCooldown - (Time.time - lastActionTime):F2}s.");
            return; // Early return – do NOT broadcast the action.
        }

        // Action is APPROVED – record the current time so the
        // NEXT request will be measured against this moment.
        lastActionTime = Time.time;

        // Tell EVERYONE to play the visual effect.
        // We call an Everyone RPC rather than doing it here so the
        // effect looks synchronised on all clients at once.
        BroadcastActionRpc();
    }

    // ---------------------------------------------------------
    // BroadcastActionRpc – [Rpc(SendTo.Everyone)]
    // This runs on EVERY connected machine (including server).
    // It's safe to do visuals here because it's already approved.
    // ---------------------------------------------------------
    [Rpc(SendTo.Everyone)]
    private void BroadcastActionRpc()
    {
        // Play the particle burst on every client's local instance
        // of this player.  ParticleSystem.Play() works even on
        // non-owned objects, which is exactly what we want.
        if (actionParticleSystem != null)
        {
            // Stop first in case it's still playing from a previous
            // action, then restart.
            actionParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            actionParticleSystem.Play();
        }

        // Log to the Console so students can see the RPC call
        // arriving on all clients simultaneously.
        Debug.Log($"[NetworkPlayerAction] Player {OwnerClientId} performed an action! " +
                  $"(Running on: {(IsServer ? "Server" : "Client")})");
    }
}
