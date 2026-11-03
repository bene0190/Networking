// ============================================================
// PlayerHealth.cs  –  Week 4
// Tracks player health using a NetworkVariable<int> so that
// every client can read the current health value and the UI
// stays in sync on all machines.  Only the SERVER can write
// the value, which prevents clients from hacking their HP.
// ============================================================

// Unity.Netcode for NetworkBehaviour, NetworkVariable, IsServer…
using Unity.Netcode;

// UnityEngine for Debug, Mathf, etc.
using UnityEngine;

// UnityEngine.UI for the Image component used as a health bar.
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    // ---------------------------------------------------------
    // Inspector fields
    // ---------------------------------------------------------

    // The foreground Image of the health bar (Fill type).
    // Its fillAmount (0–1) represents current / max health.
    [SerializeField] public Image healthBarFill;

    // ---------------------------------------------------------
    // NetworkVariable
    // currentHealth is readable by everyone (so any client can
    // display it) but only the server can write (change) it.
    // Starting value: 100 (full health).
    // ---------------------------------------------------------
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,                                        // starting value
        NetworkVariableReadPermission.Everyone,     // all clients can read
        NetworkVariableWritePermission.Server       // only server can write
    );

    // ---------------------------------------------------------
    // Private constant – never changes at runtime.
    // ---------------------------------------------------------

    // Maximum health; used to calculate the fill ratio (0–1).
    private int maxHealth = 100;

    // Cached camera reference for billboarding the health bar.
    private Camera mainCam;

    // ---------------------------------------------------------
    // LateUpdate – runs after movement each frame.
    // Keeps the overhead health bar canvas facing the camera at
    // all times so it never rotates edge-on when the player turns.
    // ---------------------------------------------------------
    private void LateUpdate()
    {
        if (healthBarFill != null)
        {
            if (mainCam == null || !mainCam.isActiveAndEnabled)
            {
                mainCam = Camera.main;
            }

            if (mainCam != null && healthBarFill.canvas != null)
            {
                healthBarFill.canvas.transform.rotation = mainCam.transform.rotation;
            }
        }
    }

    // ---------------------------------------------------------
    // OnNetworkSpawn – subscribe to health changes on ALL machines
    // so the health bar updates everywhere simultaneously.
    // ---------------------------------------------------------
    public override void OnNetworkSpawn()
    {
        // Register our callback.  Whenever the server changes
        // currentHealth.Value, HandleHealthChanged fires on every client.
        currentHealth.OnValueChanged += HandleHealthChanged;

        // Set the health bar to full immediately on spawn so it
        // displays correctly before any damage is taken.
        UpdateHealthBar(currentHealth.Value);
    }

    // ---------------------------------------------------------
    // OnNetworkDespawn – always clean up event subscriptions
    // to avoid callbacks firing on objects that no longer exist.
    // ---------------------------------------------------------
    public override void OnNetworkDespawn()
    {
        // Unsubscribe our callback when this object is removed.
        currentHealth.OnValueChanged -= HandleHealthChanged;
    }

    // ---------------------------------------------------------
    // TakeDamage – called by server-side objects (e.g., DamageZone).
    // The Server-only guard means a rogue client can't call this
    // directly via their own code to give themselves infinite HP.
    // ---------------------------------------------------------
    public void TakeDamage(int damageAmount)
    {
        // AUTHORITATIVE GUARD: only the server may reduce health.
        // If somehow a client calls this, we exit immediately.
        if (!IsServer) return;

        // Subtract damage, then clamp so HP never goes below 0
        // or above maxHealth.  Mathf.Clamp is perfect for this.
        int newHealth = Mathf.Clamp(currentHealth.Value - damageAmount, 0, maxHealth);

        // Assign the new value.  NGO automatically broadcasts this
        // change to every connected client, firing OnValueChanged.
        currentHealth.Value = newHealth;

        // Log for debugging – you can see this in the server console.
        Debug.Log($"[PlayerHealth] Client {OwnerClientId} took {damageAmount} damage. HP: {newHealth}/{maxHealth}");

        // Optional: add death handling here in a future week!
        if (newHealth <= 0)
        {
            Debug.Log($"[PlayerHealth] Player {OwnerClientId} has died!");
            // Week 6 hook: call a RespawnRpc() here.
        }
    }

    // ---------------------------------------------------------
    // HandleHealthChanged – NGO callback, fires on every client
    // when the server updates currentHealth.Value.
    // ---------------------------------------------------------
    private void HandleHealthChanged(int previousValue, int newValue)
    {
        // Delegate to UpdateHealthBar so the logic is in one place.
        UpdateHealthBar(newValue);
    }

    // ---------------------------------------------------------
    // UpdateHealthBar – refreshes the UI fill amount.
    // fillAmount = 0 means empty bar, 1 means full.
    // ---------------------------------------------------------
    private void UpdateHealthBar(int currentHealthValue)
    {
        // Guard: if the healthBarFill wasn't assigned, skip silently
        // so the lack of UI doesn't crash the multiplayer logic.
        if (healthBarFill == null) return;

        // Divide current HP by max HP to get a 0–1 ratio.
        // The cast to float is essential: int/int = int in C#
        // (e.g., 50/100 = 0, NOT 0.5).
        healthBarFill.fillAmount = (float)currentHealthValue / maxHealth;
    }
}
