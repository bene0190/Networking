// ============================================================
// MultiplayerMenu.cs  –  Weeks 1 & 3
// Manages the Host / Client / Server connection buttons and
// displays join-code feedback to the player.
// ============================================================

// We need Unity's core engine namespace for MonoBehaviour, etc.
using UnityEngine;

// TMPro gives us TextMeshPro components for crisp UI text.
using TMPro;

// Unity.Netcode is the Netcode for GameObjects (NGO) namespace –
// everything multiplayer lives here.
using Unity.Netcode;

public class MultiplayerMenu : MonoBehaviour
{
    // ---------------------------------------------------------
    // Inspector fields
    // ---------------------------------------------------------

    // The panel GameObject that contains all menu buttons.
    // We keep it visible so players can still read the join code
    // while connected, but you could hide it if you prefer.
    [SerializeField] private GameObject menuUI;

    // An input field where a client types the join code they
    // received from the host (used in Relay mode, Week 3+).
    [SerializeField] private TMP_InputField joinCodeInput;

    // A text label that the HOST fills in so players know what
    // join code to share with friends.
    [SerializeField] private TMP_Text joinCodeText;

    // A general-purpose status label ("Host started!", errors…).
    [SerializeField] private TMP_Text statusText;

    // --------------------------------------------------------
    // Toggle: set to FALSE for local LAN / Editor testing
    // (default).  Set to TRUE when Unity Relay is integrated
    // later in the course (Week 3 extension activity).
    // --------------------------------------------------------
    [SerializeField] private bool useRelayJoinCodes = false;

    // Generated or current session code displayed to the player
    private string activeSessionCode = "";

    // ---------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------

    // Convenience wrapper so we always safely read NetworkManager.
    private NetworkManager Net => NetworkManager.Singleton;

    private void Start()
    {
        if (joinCodeText != null)
            joinCodeText.text = "Join Code: (start a host)";
    }

    // =========================================================
    // PUBLIC METHODS — called by the UI buttons
    // =========================================================

    // StartHost() makes THIS machine the server AND a client.
    // The host runs the authoritative game simulation while also
    // playing.  This is the most common mode for small sessions.
    public void StartHost()
    {
        // Guard: if NetworkManager isn't in the scene, bail out.
        if (Net == null)
        {
            SetStatus("ERROR: NetworkManager not found in scene!");
            return;
        }

        // Actually start hosting.  StartHost() returns true on
        // success, so we branch on that to give useful feedback.
        if (Net.StartHost())
        {
            // Generate a readable 6-character room code so the host
            // always has an actual code to share with clients
            if (string.IsNullOrEmpty(activeSessionCode))
            {
                activeSessionCode = "ROOM-" + UnityEngine.Random.Range(1000, 9999);
            }

            ShowJoinCode(activeSessionCode);
            SetStatus($"Host started! Share code '{activeSessionCode}' with clients.");
        }
        else
        {
            // StartHost() returned false – something went wrong.
            SetStatus("Host failed to start.  Check the Console.");
        }
    }

    // StartClient() connects THIS machine to an existing host.
    // The client sends input to the server and receives state
    // updates; it does NOT run the authoritative simulation.
    public void StartClient()
    {
        if (Net == null)
        {
            SetStatus("ERROR: NetworkManager not found in scene!");
            return;
        }

        string code = joinCodeInput != null ? joinCodeInput.text.Trim() : "";
        if (!string.IsNullOrEmpty(code))
        {
            SetStatus($"Joining session '{code}'…");
        }
        else
        {
            SetStatus("Client connecting to host…");
        }

        // Kick off the client connection attempt.
        Net.StartClient();
    }

    // StartServer() starts a DEDICATED server – no local player.
    // Useful for headless builds or teaching about authoritative
    // server architecture without a playing host.
    public void StartServer()
    {
        if (Net == null)
        {
            SetStatus("ERROR: NetworkManager not found in scene!");
            return;
        }

        if (Net.StartServer())
        {
            activeSessionCode = "SRV-" + UnityEngine.Random.Range(1000, 9999);
            ShowJoinCode(activeSessionCode);
            SetStatus("Dedicated server running.");
        }
        else
        {
            SetStatus("Server failed to start.  Check the Console.");
        }
    }

    // =========================================================
    // PRIVATE HELPERS
    // =========================================================

    // Shows a message on the statusText label.
    // Safe to call even if statusText hasn't been assigned.
    private void SetStatus(string message)
    {
        Debug.Log($"[MultiplayerMenu] {message}");
        if (statusText != null)
            statusText.text = message;
    }

    // Writes the join code into the joinCodeText label so the
    // host can read it out to their students / friends.
    private void ShowJoinCode(string code)
    {
        if (joinCodeText != null)
            joinCodeText.text = $"Join Code: {code}";
    }
}
