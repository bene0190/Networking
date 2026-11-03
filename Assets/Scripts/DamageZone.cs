// ============================================================
// DamageZone.cs  –  Week 4
// A plain MonoBehaviour (not NetworkBehaviour) that sits on a
// trigger collider.  When a player walks into it, the SERVER
// calls TakeDamage on that player's PlayerHealth component.
//
// WHY MonoBehaviour and not NetworkBehaviour?
// The DamageZone itself doesn't need a NetworkObject – it's a
// static world object.  We only need to check IsServer before
// applying damage, which we can do via NetworkManager.Singleton.
// ============================================================

// Unity.Netcode for NetworkManager (to check if we're the server).
using Unity.Netcode;

// UnityEngine for MonoBehaviour, Collider, SerializeField…
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    // ---------------------------------------------------------
    // Inspector fields
    // ---------------------------------------------------------

    // How many hit points to subtract each time a player enters.
    // With 100 HP and 20 damage, players survive 5 touches.
    [SerializeField] private int damageAmount = 20;

    // ---------------------------------------------------------
    // OnTriggerEnter is called by Unity's physics engine when
    // ANOTHER collider enters the trigger volume of this object.
    // The 'other' parameter is the collider that entered.
    // ---------------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        // CRITICAL: only the server should apply damage.
        // Without this guard, every client's DamageZone script
        // would independently try to reduce health, causing
        // damage to be applied multiple times (one per connected client)!
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        // Try to get the PlayerHealth component from the object
        // that walked into us.  TryGetComponent returns false
        // if the component doesn't exist (e.g., a projectile enters).
        if (other.TryGetComponent(out PlayerHealth playerHealth))
        {
            // The collider belongs to a player with a PlayerHealth
            // component – apply the damage now!
            playerHealth.TakeDamage(damageAmount);

            // Log who was hit for debugging in the Editor.
            Debug.Log($"[DamageZone] Hit player {playerHealth.OwnerClientId} for {damageAmount} damage.");
        }
        // If the collider is NOT a player (e.g., a prop), we simply
        // ignore it – nothing happens.
    }
}
