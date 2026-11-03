// ============================================================
// NetworkPlayerController.cs  –  Week 2
// Handles player movement in a server-authoritative way.
// KEY CONCEPT: Only the OWNER reads input; movement is
// executed on the SERVER so the server always has the
// correct position (prevents teleport / speed cheats).
// ============================================================

// Unity.Netcode for NetworkBehaviour, IsOwner, IsServer, Rpc…
using Unity.Netcode;

// UnityEngine for Input, CharacterController, Vector3…
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class NetworkPlayerController : NetworkBehaviour
{
    // ---------------------------------------------------------
    // Inspector-exposed settings
    // ---------------------------------------------------------

    // How many units per second the player moves.
    // Serialized so you can tweak it in the Inspector per-prefab.
    [SerializeField] private float moveSpeed = 5f;

    // Gravity applied when the player is airborne.
    // Negative = downward.  -20 feels snappy for a top-down game.
    [SerializeField] private float gravity = -20f;

    // A tiny downward force applied when grounded so
    // CharacterController registers isGrounded reliably next frame.
    [SerializeField] private float groundedGravity = -2f;

    // ---------------------------------------------------------
    // Private runtime state
    // ---------------------------------------------------------

    // CharacterController handles collision-aware movement for us.
    // We cache it in Awake() to avoid repeated GetComponent calls.
    private CharacterController characterController;

    // Tracks vertical speed (falling / rising).  Gravity
    // accumulates into this each frame when not grounded.
    private float verticalVelocity;

    // ---------------------------------------------------------
    // Awake is called before the first frame, before networking
    // initialises.  Safe place to cache component references.
    // ---------------------------------------------------------
    private void Awake()
    {
        // Cache the CharacterController attached to this GameObject.
        characterController = GetComponent<CharacterController>();
    }

    // ---------------------------------------------------------
    // Update runs every frame on EVERY client that has this
    // player in their scene.  The IsOwner guard means only the
    // local player's machine reads input – everyone else skips.
    // ---------------------------------------------------------
    private void Update()
    {
        // IsOwner is true ONLY on the machine that "owns" this
        // NetworkObject (i.e., the player who connected with it).
        // Without this guard, all clients would move ALL players!
        if (!IsOwner) return;

        float horizontalInput = 0f;
        float verticalInput   = 0f;

#if ENABLE_INPUT_SYSTEM
        // 1. Read New Input System Keyboard (WASD & Arrow keys)
        if (Keyboard.current != null)
        {
            var kb = Keyboard.current;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  horizontalInput -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontalInput += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  verticalInput   -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    verticalInput   += 1f;
        }

        // 2. Read New Input System Gamepad (Left Stick & D-Pad)
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.04f)
            {
                horizontalInput += stick.x;
                verticalInput   += stick.y;
            }
            if (Gamepad.current.dpad.left.isPressed)  horizontalInput -= 1f;
            if (Gamepad.current.dpad.right.isPressed) horizontalInput += 1f;
            if (Gamepad.current.dpad.down.isPressed)   verticalInput   -= 1f;
            if (Gamepad.current.dpad.up.isPressed)     verticalInput   += 1f;
        }
#endif

        // 3. Fall back to Legacy Input Manager if no input detected yet
        if (Mathf.Approximately(horizontalInput, 0f) && Mathf.Approximately(verticalInput, 0f))
        {
            try
            {
                horizontalInput = Input.GetAxisRaw("Horizontal");
                verticalInput   = Input.GetAxisRaw("Vertical");
            }
            catch
            {
                // Legacy input disabled in PlayerSettings
            }
        }

        horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);
        verticalInput   = Mathf.Clamp(verticalInput, -1f, 1f);

        // Pack both axes into a Vector2 to send across the network
        // (cheaper than sending two separate floats).
        Vector2 movementInput = new Vector2(horizontalInput, verticalInput);

        // KEY NETWORKING DECISION:
        // If we ARE the server (Host mode), we can call MovePlayer
        // directly because we're already on the server.
        // If we are a pure CLIENT, we must send an RPC to the
        // server asking it to move us – clients never self-move!
        if (IsServer)
        {
            // Host path: we're owner AND server, move directly.
            MovePlayer(movementInput);
        }
        else
        {
            // Client path: ask the server to process our movement.
            MovePlayerRpc(movementInput);
        }
    }

    // ---------------------------------------------------------
    // MovePlayerRpc – [Rpc(SendTo.Server)] means this method
    // runs on the SERVER regardless of who called it.
    // Modern NGO syntax replaces the legacy [ServerRpc].
    // ---------------------------------------------------------
    [Rpc(SendTo.Server)]
    private void MovePlayerRpc(Vector2 movementInput)
    {
        // The server receives our input and runs MovePlayer here.
        // The result is then broadcast to all clients via
        // NetworkTransform (added to the prefab separately).
        MovePlayer(movementInput);
    }

    // ---------------------------------------------------------
    // MovePlayer – the SHARED movement logic that runs on the
    // server (called directly by Host, or via RPC by clients).
    // ---------------------------------------------------------
    private void MovePlayer(Vector2 movementInput)
    {
        // --- Gravity simulation ---
        // If the player is touching the ground, reset vertical
        // speed to a small negative value so isGrounded stays true
        // reliably.  Otherwise, accelerate downward each frame.
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            // groundedGravity is a small negative "stick to ground"
            // force, not zero, so the controller keeps detecting ground.
            verticalVelocity = groundedGravity;
        }
        else
        {
            // Accumulate gravity over time (deltaTime = seconds
            // since last frame) so speed increases while falling.
            verticalVelocity += gravity * Time.deltaTime;
        }

        // --- Horizontal movement ---
        // Convert the 2D input (X = left/right, Y = forward/back)
        // into 3D world space.  Note: input.y maps to world Z.
        Vector3 moveDirection = new Vector3(movementInput.x, 0f, movementInput.y);

        // Normalize so diagonal movement isn't faster than cardinal
        // (without normalize diagonal = sqrt(2) × speed).
        moveDirection = moveDirection.normalized;

        // Scale direction by speed to get velocity (units/second).
        Vector3 horizontalMovement = moveDirection * moveSpeed;

        // --- Combine horizontal + vertical ---
        Vector3 verticalMovement = Vector3.up * verticalVelocity;
        Vector3 finalMovement    = horizontalMovement + verticalMovement;

        // CharacterController.Move expects units-per-frame, so
        // multiply by deltaTime to get frame-rate-independent motion.
        characterController.Move(finalMovement * Time.deltaTime);

        // --- Face the direction of movement ---
        // Rotate the player capsule to face where it's going.
        // This sets up the forward vector needed for Week 6 shooting!
        if (moveDirection != Vector3.zero)
        {
            // Setting transform.forward directly is instant and
            // server-authoritative; NetworkTransform syncs the rotation.
            transform.forward = moveDirection;
        }
    }
}
