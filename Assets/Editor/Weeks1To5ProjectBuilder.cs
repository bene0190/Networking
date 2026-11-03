// ============================================================
// Weeks1To5ProjectBuilder.cs  –  Editor Automation
// Menu: Tools > Netcode Course > Build Weeks 1-5 Complete Project
//
// ONE CLICK builds the entire Weeks 1–5 scene from scratch:
// materials, prefab, scene objects, UI, tags, and all Inspector
// wiring.  Designed to be re-run safely after PC issues.
// ============================================================

// Standard C# namespaces
using System;
using System.Collections.Generic;
using System.IO;

// UnityEditor for AssetDatabase, PrefabUtility, EditorUtility, Undo…
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

// UnityEngine for all runtime components
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

// NGO namespaces
using Unity.Netcode;
using Unity.Netcode.Components;

// TMPro and Input namespaces
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Alias so "Component" refers to UnityEngine.Component,
// not System.ComponentModel.Component.
using Component = UnityEngine.Component;

// ============================================================
// NOTE: This script uses #if UNITY_EDITOR so it is completely
//       stripped from any player build.
// ============================================================
#if UNITY_EDITOR

public static class Weeks1To5ProjectBuilder
{
    // Automatically ensures UI and Player prefab are fixed whenever scripts recompile
    [InitializeOnLoadMethod]
    private static void AutoFixOnCompile()
    {
        EditorApplication.delayCall += () =>
        {
            FixOpenSceneUI(false);
            FixNetworkPlayerPrefab(false);
        };
    }

    // --------------------------------------------------------
    // MENU ENTRY POINTS
    // --------------------------------------------------------
    [MenuItem("Tools/Netcode Course/Fix Multiplayer UI")]
    public static void FixOpenSceneUIMenu()
    {
        FixOpenSceneUI(true);
    }

    [MenuItem("Tools/Netcode Course/Fix NetworkPlayer Prefab")]
    public static void FixNetworkPlayerPrefabMenu()
    {
        FixNetworkPlayerPrefab(true);
    }

    [MenuItem("Tools/Netcode Course/Build Weeks 1-5 Complete Project")]
    public static void BuildAll()
    {
        Debug.Log("========================================");
        Debug.Log(" Weeks 1-5 Builder: Starting…");
        Debug.Log("========================================");

        // Save any unsaved work first so nothing is lost.
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // --- Step 1: Folders ---
        EnsureFolders();

        // --- Step 2: Tags ---
        EnsureTag("SpawnPoint");
        // "Player" is a Unity built-in tag, no need to add it.

        // --- Step 3: Materials ---
        var mats = CreateMaterials();

        // --- Step 4: NetworkPlayer prefab ---
        GameObject networkPlayerPrefab = BuildNetworkPlayerPrefab(mats);

        // --- Step 5: Scene ---
        BuildScene(networkPlayerPrefab, mats);

        // --- Save everything ---
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "✅ Weeks 1–5 Build Complete!",
            "All scene objects, prefabs, materials, and Inspector references have been created.\n\n" +
            "Press Play → Start Host to test.\n\n" +
            "Read Assets/WEEK6_INSTRUCTOR_READY_GUIDE.md for next steps.",
            "Great, thanks!"
        );

        Debug.Log("========================================");
        Debug.Log(" Weeks 1-5 Builder: Done!");
        Debug.Log("========================================");
    }

    // ========================================================
    // STEP 1 — FOLDERS
    // ========================================================
    private static void EnsureFolders()
    {
        string[] folders = {
            "Assets/Scripts",
            "Assets/Materials",
            "Assets/Prefabs",
            "Assets/Scenes",
            "Assets/Editor"
        };

        foreach (string folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                string child  = Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, child);
                Debug.Log($"[Builder] Created folder: {folder}");
            }
        }
    }

    // ========================================================
    // STEP 2 — TAGS
    // ========================================================
    private static void EnsureTag(string tagName)
    {
        // Open the TagManager asset directly.
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset")
        );
        SerializedProperty tagsProperty = tagManager.FindProperty("tags");

        // Check if tag already exists to avoid duplicates.
        for (int i = 0; i < tagsProperty.arraySize; i++)
        {
            if (tagsProperty.GetArrayElementAtIndex(i).stringValue == tagName)
            {
                Debug.Log($"[Builder] Tag '{tagName}' already exists – skipping.");
                return;
            }
        }

        // Add the tag at the next available index.
        tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
        tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"[Builder] Added tag: '{tagName}'");
    }

    // ========================================================
    // STEP 3 — MATERIALS
    // ========================================================
    private static Dictionary<string, Material> CreateMaterials()
    {
        // Prefer URP Lit shader; fall back to Standard for Built-in RP.
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Standard");

        if (litShader == null)
        {
            Debug.LogError("[Builder] Could not find a Lit or Standard shader!");
        }

        var matDefs = new Dictionary<string, Color>
        {
            { "Player_Red",     new Color(0.90f, 0.15f, 0.10f) },
            { "Player_Blue",    new Color(0.10f, 0.40f, 0.90f) },
            { "Player_Green",   new Color(0.10f, 0.75f, 0.20f) },
            { "Player_Yellow",  new Color(0.95f, 0.85f, 0.10f) },
            { "DamageZone_Red", new Color(0.85f, 0.10f, 0.10f, 0.80f) },
            { "Button_Grey",    new Color(0.45f, 0.45f, 0.45f) },
            { "Ground_Dark",    new Color(0.18f, 0.20f, 0.22f) },
        };

        var results = new Dictionary<string, Material>();

        foreach (var kv in matDefs)
        {
            string path = $"Assets/Materials/{kv.Key}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null)
            {
                mat = new Material(litShader ?? Shader.Find("Standard"));
                // Set the base color using both the standard property name and
                // the URP property name so it works with both render pipelines.
                mat.color = kv.Value;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", kv.Value);

                AssetDatabase.CreateAsset(mat, path);
                Debug.Log($"[Builder] Created material: {kv.Key}");
            }
            else
            {
                Debug.Log($"[Builder] Material already exists: {kv.Key}");
            }

            results[kv.Key] = mat;
        }

        return results;
    }

    // ========================================================
    // STEP 4 — NETWORKPLAYER PREFAB
    // ========================================================
    private static GameObject BuildNetworkPlayerPrefab(Dictionary<string, Material> mats)
    {
        string prefabPath = "Assets/Prefabs/NetworkPlayer.prefab";

        // If the prefab already exists, load and update it.
        // We delete and recreate to ensure a clean state.
        if (File.Exists(Application.dataPath.Replace("Assets", "") + prefabPath))
        {
            AssetDatabase.DeleteAsset(prefabPath);
            Debug.Log("[Builder] Deleted old NetworkPlayer.prefab for clean rebuild.");
        }

        // --- ROOT: Capsule ---
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = "NetworkPlayer";

        // Tag the root so DamageZone and NetworkedButton can find it.
        root.tag = "Player";

        // Remove any Collider Unity auto-added to the Capsule primitive
        // (CharacterController has its own collision).
        UnityEngine.Object.DestroyImmediate(root.GetComponent<CapsuleCollider>());

        // Remove any Rigidbody if present (CharacterController does not
        // need one and it causes conflicts).
        Rigidbody rb = root.GetComponent<Rigidbody>();
        if (rb != null) UnityEngine.Object.DestroyImmediate(rb);

        // -- NetworkObject --
        // Every networked entity needs a NetworkObject component.
        root.AddComponent<NetworkObject>();

        // -- NetworkTransform --
        // Automatically syncs position & rotation across the network.
        root.AddComponent<NetworkTransform>();

        // -- CharacterController --
        CharacterController cc = root.AddComponent<CharacterController>();
        cc.center     = Vector3.zero;   // Mesh primitive is centered at (0, 0, 0), so CC center must be 0 to avoid being underground
        cc.radius     = 0.5f;
        cc.height     = 2f;
        cc.skinWidth  = 0.08f;

        // -- PlayerSpawnManager --
        root.AddComponent<PlayerSpawnManager>();

        // -- NetworkPlayerController --
        root.AddComponent<NetworkPlayerController>();

        // -- LocalPlayerCameraTarget --
        root.AddComponent<LocalPlayerCameraTarget>();

        // -- PlayerColorManager --
        PlayerColorManager colorManager = root.AddComponent<PlayerColorManager>();

        // Wire the Renderer reference via SerializedObject.
        MeshRenderer meshRenderer = root.GetComponent<MeshRenderer>();
        {
            SerializedObject so = new SerializedObject(colorManager);
            so.FindProperty("playerRenderer").objectReferenceValue = meshRenderer;

            // Build the playerMaterials array [Red, Blue, Green, Yellow].
            SerializedProperty matsArr = so.FindProperty("playerMaterials");
            matsArr.arraySize = 4;
            matsArr.GetArrayElementAtIndex(0).objectReferenceValue = mats["Player_Red"];
            matsArr.GetArrayElementAtIndex(1).objectReferenceValue = mats["Player_Blue"];
            matsArr.GetArrayElementAtIndex(2).objectReferenceValue = mats["Player_Green"];
            matsArr.GetArrayElementAtIndex(3).objectReferenceValue = mats["Player_Yellow"];
            so.ApplyModifiedProperties();
        }

        // -- PlayerHealth --
        PlayerHealth playerHealth = root.AddComponent<PlayerHealth>();

        // -- NetworkPlayerAction --
        NetworkPlayerAction playerAction = root.AddComponent<NetworkPlayerAction>();

        // -------------------------------------------------------
        // CHILD 1: Health Bar Canvas
        // -------------------------------------------------------
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(root.transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.localPosition = new Vector3(0f, 1.35f, 0f);
        canvasRT.localRotation = Quaternion.identity;
        canvasRT.localScale    = Vector3.one * 0.01f;   // World space canvas: 0.01 scale = 2cm per unit

        // Fix the canvas size to 200 × 50 (in canvas units, = 2m × 0.5m at scale 0.01)
        canvasRT.sizeDelta = new Vector2(200f, 50f);

        // Always face camera so the HP bar never rotates edge-on
        canvasGO.AddComponent<FaceCamera>();

        // Background image (dark red)
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.45f, 0.05f, 0.05f, 1f);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Foreground fill image (bright green)
        GameObject fgGO = new GameObject("Foreground");
        fgGO.transform.SetParent(canvasGO.transform, false);
        Image fgImage = fgGO.AddComponent<Image>();
        fgImage.color = new Color(0.10f, 0.85f, 0.20f, 1f);

        // Create a trivial white sprite so Image.Type.Filled works correctly.
        // Without a sprite, Unity ignores the fillAmount.
        Texture2D whiteTex = new Texture2D(4, 4);
        Color[]   pixels   = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        whiteTex.SetPixels(pixels);
        whiteTex.Apply();
        Sprite whiteSprite = Sprite.Create(
            whiteTex,
            new Rect(0, 0, 4, 4),
            new Vector2(0.5f, 0.5f)
        );
        fgImage.sprite     = whiteSprite;
        fgImage.type       = Image.Type.Filled;
        fgImage.fillMethod = Image.FillMethod.Horizontal;
        fgImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fgImage.fillAmount = 1f;  // Start at full health.

        RectTransform fgRT = fgGO.GetComponent<RectTransform>();
        fgRT.anchorMin = Vector2.zero;
        fgRT.anchorMax = Vector2.one;
        fgRT.offsetMin = Vector2.zero;
        fgRT.offsetMax = Vector2.zero;

        // Wire the Foreground image into PlayerHealth.healthBarFill
        {
            SerializedObject so = new SerializedObject(playerHealth);
            so.FindProperty("healthBarFill").objectReferenceValue = fgImage;
            so.ApplyModifiedProperties();
        }

        // -------------------------------------------------------
        // CHILD 2: Action Particles
        // -------------------------------------------------------
        GameObject particlesGO = new GameObject("ActionParticles");
        particlesGO.transform.SetParent(root.transform, false);
        particlesGO.transform.localPosition = Vector3.zero;

        ParticleSystem ps = particlesGO.AddComponent<ParticleSystem>();

        // Configure the particle system for a satisfying burst.
        var main        = ps.main;
        main.playOnAwake = false;
        main.loop        = false;
        main.duration    = 1.0f;
        main.startLifetime  = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize      = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
        // Random orange/yellow color for each particle
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1.0f, 0.85f, 0.0f),  // bright yellow
            new Color(1.0f, 0.40f, 0.0f)   // orange
        );

        // Emit 30 particles at once in a burst (not continuous).
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 30)
        });

        // Sphere shape so particles scatter in all directions.
        var shape  = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.3f;

        // Wire the ParticleSystem into NetworkPlayerAction.
        {
            SerializedObject so = new SerializedObject(playerAction);
            so.FindProperty("actionParticleSystem").objectReferenceValue = ps;
            so.ApplyModifiedProperties();
        }

        // -------------------------------------------------------
        // Save as a prefab asset
        // -------------------------------------------------------
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);  // Clean up the scene temp object

        Debug.Log("[Builder] NetworkPlayer.prefab created successfully.");
        return savedPrefab;
    }

    // ========================================================
    // STEP 5 — SCENE
    // ========================================================
    private static void BuildScene(GameObject networkPlayerPrefab, Dictionary<string, Material> mats)
    {
        // Remove stale objects with the same names to allow clean rebuilds.
        string[] managedNames = {
            "Ground", "SpawnPoints", "NetworkManager", "MultiplayerMenuCanvas",
            "DamageZone", "NetworkedButton", "EventSystem"
        };
        foreach (string n in managedNames)
        {
            GameObject existing = GameObject.Find(n);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
                Debug.Log($"[Builder] Removed existing scene object: {n}");
            }
        }

        // --- 5a. GROUND PLANE ---
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position   = Vector3.zero;
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        ground.GetComponent<Renderer>().sharedMaterial = mats["Ground_Dark"];

        // --- 5b. MAIN CAMERA ---
        // Find or reuse existing main camera.
        Camera mainCam = Camera.main;
        GameObject camGO;
        if (mainCam != null)
        {
            camGO = mainCam.gameObject;
        }
        else
        {
            camGO = new GameObject("Main Camera");
            camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 10f, -8f);
        camGO.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

        // Remove any existing TopDownCameraFollow to get a clean one.
        TopDownCameraFollow existingFollow = camGO.GetComponent<TopDownCameraFollow>();
        if (existingFollow != null) UnityEngine.Object.DestroyImmediate(existingFollow);
        camGO.AddComponent<TopDownCameraFollow>();

        // --- 5c. SPAWN POINTS ---
        GameObject spawnParent = new GameObject("SpawnPoints");

        Vector3[] spawnPositions = {
            new Vector3(-3f, 1f, 0f),
            new Vector3( 3f, 1f, 0f),
            new Vector3( 0f, 1f,-3f),
            new Vector3( 0f, 1f, 3f)
        };

        for (int i = 0; i < spawnPositions.Length; i++)
        {
            GameObject sp = new GameObject($"SpawnPoint_0{i + 1}");
            sp.transform.SetParent(spawnParent.transform, false);
            sp.transform.position = spawnPositions[i];

            // Apply the "SpawnPoint" tag we registered earlier.
            sp.tag = "SpawnPoint";
        }

        // --- 5d. NETWORK MANAGER ---
        GameObject nmGO = new GameObject("NetworkManager");
        NetworkManager nm = nmGO.AddComponent<NetworkManager>();

        // Add Unity Transport.
        Unity.Netcode.Transports.UTP.UnityTransport transport =
            nmGO.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

        // Enable WebSockets so the project is web-build ready.
        {
            SerializedObject so = new SerializedObject(transport);
            SerializedProperty useWS = so.FindProperty("m_UseWebSockets");
            if (useWS != null) { useWS.boolValue = true; so.ApplyModifiedProperties(); }
        }

        // Wire transport + player prefab into NetworkManager via SerializedObject.
        {
            SerializedObject so = new SerializedObject(nm);

            // Network Transport
            so.FindProperty("NetworkConfig.NetworkTransport").objectReferenceValue = transport;

            // Player Prefab
            so.FindProperty("NetworkConfig.PlayerPrefab").objectReferenceValue = networkPlayerPrefab;

            so.ApplyModifiedProperties();
        }

        // Register the player prefab in NetworkPrefabs list.
        {
            SerializedObject so = new SerializedObject(nm);
            SerializedProperty prefabsList = so.FindProperty("NetworkConfig.Prefabs.NetworkPrefabsLists");

            // The modern NGO prefab registration uses NetworkPrefabsList assets OR
            // the runtime API. We'll add via the NetworkConfig.Prefabs.Prefabs array.
            SerializedProperty prefabsArray = so.FindProperty("NetworkConfig.Prefabs.Prefabs");
            if (prefabsArray != null)
            {
                // Check if already registered.
                bool alreadyAdded = false;
                for (int i = 0; i < prefabsArray.arraySize; i++)
                {
                    var el = prefabsArray.GetArrayElementAtIndex(i);
                    var pref = el.FindPropertyRelative("Prefab");
                    if (pref != null && pref.objectReferenceValue == networkPlayerPrefab)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    prefabsArray.InsertArrayElementAtIndex(prefabsArray.arraySize);
                    var newEl = prefabsArray.GetArrayElementAtIndex(prefabsArray.arraySize - 1);
                    newEl.FindPropertyRelative("Prefab").objectReferenceValue = networkPlayerPrefab;
                }

                so.ApplyModifiedProperties();
            }
        }

        // --- 5e. MULTIPLAYER MENU UI ---
        BuildMultiplayerMenuUI();

        // --- 5f. DAMAGE ZONE (Week 4) ---
        BuildDamageZone(mats);

        // --- 5g. NETWORKED BUTTON (Week 5) ---
        BuildNetworkedButton(mats);

        // Mark the active scene as dirty so Unity knows to save it.
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[Builder] Scene built and saved.");
    }

    // --------------------------------------------------------
    // 5e  MULTIPLAYER MENU UI
    // --------------------------------------------------------
    private static void BuildMultiplayerMenuUI()
    {
        // Remove stale EventSystem if present.
        var oldES = GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (oldES != null) UnityEngine.Object.DestroyImmediate(oldES.gameObject);

        // --- Screen-space Canvas ---
        GameObject canvasGO = new GameObject("MultiplayerMenuCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // MultiplayerMenu script on the canvas root.
        MultiplayerMenu menuScript = canvasGO.AddComponent<MultiplayerMenu>();

        // --- Main Menu Panel (top-left corner, pivot 0,1 ensures it stays 100% on screen) ---
        GameObject panelGO = CreateUIPanel("MainMenuPanel", canvasGO.transform,
            new Vector2(0f, 1f),       // anchor min top-left
            new Vector2(0f, 1f),       // anchor max top-left
            new Vector2(16f, -16f),    // pivot offset: 16px padding from top-left
            new Vector2(340f, 380f),   // width × height
            new Vector2(0f, 1f));      // pivot: top-left

        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0.10f, 0.12f, 0.16f, 0.88f);

        // --- Start Host Button ---
        GameObject hostBtnGO = CreateUIButton("StartHostButton", panelGO.transform,
            new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.96f), "Start Host");

        // --- Start Client Button ---
        GameObject clientBtnGO = CreateUIButton("StartClientButton", panelGO.transform,
            new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.83f), "Start Client");

        // --- Start Server Button ---
        GameObject serverBtnGO = CreateUIButton("StartServerButton", panelGO.transform,
            new Vector2(0.06f, 0.59f), new Vector2(0.94f, 0.70f), "Start Server");

        // --- Join Code Text ---
        GameObject joinCodeTextGO = CreateUIText("JoinCodeText", panelGO.transform,
            new Vector2(0.06f, 0.49f), new Vector2(0.94f, 0.56f),
            "Join Code: (start a host)", 13, new Color(0.35f, 0.85f, 1.0f));

        // --- Join Code Input Field ---
        GameObject joinCodeInputGO = CreateUIInputField("JoinCodeInputField", panelGO.transform,
            new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.46f),
            "Enter join code…");

        // --- Status Text (generous space with word wrapping for multi-line status / errors) ---
        GameObject statusTextGO = CreateUIText("StatusText", panelGO.transform,
            new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.32f),
            "Not connected", 12, new Color(0.85f, 0.90f, 0.95f),
            TMPro.TextAlignmentOptions.TopLeft, true);

        // --- Wire references into MultiplayerMenu via SerializedObject ---
        {
            SerializedObject so = new SerializedObject(menuScript);
            so.FindProperty("menuUI").objectReferenceValue                = panelGO;
            so.FindProperty("joinCodeText").objectReferenceValue          = joinCodeTextGO.GetComponentInChildren<TMPro.TMP_Text>();
            so.FindProperty("statusText").objectReferenceValue            = statusTextGO.GetComponentInChildren<TMPro.TMP_Text>();
            so.FindProperty("joinCodeInput").objectReferenceValue         = joinCodeInputGO.GetComponentInChildren<TMPro.TMP_InputField>();
            so.ApplyModifiedProperties();
        }

        // --- Wire button onClick events using UnityEventTools ---
        WireButtonToMethod(hostBtnGO,   menuScript, "StartHost");
        WireButtonToMethod(clientBtnGO, menuScript, "StartClient");
        WireButtonToMethod(serverBtnGO, menuScript, "StartServer");

        // --- Event System ---
        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif

        Debug.Log("[Builder] Multiplayer Menu UI built.");
    }

    // --------------------------------------------------------
    // 5f  DAMAGE ZONE (Week 4)
    // --------------------------------------------------------
    private static void BuildDamageZone(Dictionary<string, Material> mats)
    {
        GameObject dz = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dz.name = "DamageZone";
        dz.transform.position   = new Vector3(-4f, 0.1f, 4f);
        dz.transform.localScale = new Vector3(3f, 0.2f, 3f);

        // Apply red transparent material.
        dz.GetComponent<Renderer>().sharedMaterial = mats["DamageZone_Red"];

        // Make the collider a trigger (no physical blocking).
        BoxCollider col = dz.GetComponent<BoxCollider>();
        col.isTrigger = true;

        // Attach the DamageZone script.
        dz.AddComponent<DamageZone>();

        Debug.Log("[Builder] DamageZone created at (-4, 0.1, 4).");
    }

    // --------------------------------------------------------
    // 5g  NETWORKED BUTTON (Week 5)
    // --------------------------------------------------------
    private static void BuildNetworkedButton(Dictionary<string, Material> mats)
    {
        // --- Cylinder base ---
        GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        btn.name = "NetworkedButton";
        btn.transform.position   = new Vector3(4f, 0.1f, 4f);
        btn.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);
        btn.GetComponent<Renderer>().sharedMaterial = mats["Button_Grey"];

        // Make the cylinder collider a trigger.
        CapsuleCollider cc = btn.GetComponent<CapsuleCollider>();
        cc.isTrigger = true;
        // Scale the trigger to match the flat cylinder shape.
        cc.height = 10f;  // tall enough to catch standing players
        cc.radius = 1.0f;

        // NetworkObject required because NetworkedButton sends RPCs.
        btn.AddComponent<NetworkObject>();

        // Attach the NetworkedButton script.
        NetworkedButton networkedButton = btn.AddComponent<NetworkedButton>();

        // --- Child: Point Light ---
        GameObject lightGO = new GameObject("ButtonLight");
        lightGO.transform.SetParent(btn.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 15f, 0f); // up 1.5m (cylinder is scaled 0.1 on Y)

        Light pointLight = lightGO.AddComponent<Light>();
        pointLight.type      = LightType.Point;
        pointLight.color     = Color.red;
        pointLight.intensity = 5f;
        pointLight.range     = 8f;

        // Wire the light into NetworkedButton.
        {
            SerializedObject so = new SerializedObject(networkedButton);
            so.FindProperty("buttonLight").objectReferenceValue = pointLight;
            so.ApplyModifiedProperties();
        }

        Debug.Log("[Builder] NetworkedButton created at (4, 0.1, 4).");
    }

    // ========================================================
    // UI HELPER METHODS & RUNTIME UI FIX
    // ========================================================

    private static TMP_FontAsset GetDefaultFont()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"
            );
        }
        return font;
    }

    private static GameObject CreateUIPanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Vector2 pivot)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.pivot            = pivot;
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        return go;
    }

    private static GameObject CreateUIButton(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, string label)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.48f, 0.88f);

        UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.28f, 0.58f, 0.98f);
        cb.pressedColor     = new Color(0.12f, 0.35f, 0.70f);
        btn.colors = cb;

        // Label child
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = GetDefaultFont();
        if (font != null)
        {
            tmp.font = font;
            tmp.fontSharedMaterial = font.material;
        }
        tmp.text      = label;
        tmp.fontSize  = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return go;
    }

    private static GameObject CreateUIText(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, string defaultText, int fontSize, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft,
        bool enableWordWrapping = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = GetDefaultFont();
        if (font != null)
        {
            tmp.font = font;
            tmp.fontSharedMaterial = font.material;
        }
        tmp.text               = defaultText;
        tmp.fontSize           = fontSize;
        tmp.color              = color;
        tmp.alignment          = alignment;
        tmp.enableWordWrapping = enableWordWrapping;

        return go;
    }

    private static GameObject CreateUIInputField(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, string placeholder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.08f, 0.09f, 0.12f, 1f);

        TMP_InputField inputField = go.AddComponent<TMP_InputField>();
        TMP_FontAsset font = GetDefaultFont();

        // Text Area child
        GameObject textAreaGO = new GameObject("Text Area");
        textAreaGO.transform.SetParent(go.transform, false);
        RectTransform taRT = textAreaGO.AddComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(8f, 2f);
        taRT.offsetMax = new Vector2(-8f, -2f);
        textAreaGO.AddComponent<RectMask2D>();

        // Placeholder child
        GameObject phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(textAreaGO.transform, false);
        RectTransform phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero;
        phRT.offsetMax = Vector2.zero;
        TextMeshProUGUI phTMP = phGO.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            phTMP.font = font;
            phTMP.fontSharedMaterial = font.material;
        }
        phTMP.text      = placeholder;
        phTMP.fontSize  = 13;
        phTMP.color     = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        phTMP.alignment = TextAlignmentOptions.MidlineLeft;
        phTMP.fontStyle = FontStyles.Italic;

        // Text child
        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(textAreaGO.transform, false);
        RectTransform txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI txtTMP = txtGO.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            txtTMP.font = font;
            txtTMP.fontSharedMaterial = font.material;
        }
        txtTMP.fontSize  = 13;
        txtTMP.color     = Color.white;
        txtTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Wire TMP_InputField references.
        inputField.textViewport  = taRT;
        inputField.textComponent = txtTMP;
        inputField.placeholder   = phTMP;

        return go;
    }

    public static void FixOpenSceneUI(bool showDialog)
    {
        GameObject canvasGO = GameObject.Find("MultiplayerMenuCanvas");
        if (canvasGO == null) return;

        // Fix CanvasScaler
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;
        }

        // Fix MainMenuPanel
        Transform panelT = canvasGO.transform.Find("MainMenuPanel");
        if (panelT != null)
        {
            RectTransform panelRT = panelT.GetComponent<RectTransform>();
            if (panelRT != null)
            {
                panelRT.pivot            = new Vector2(0f, 1f);
                panelRT.anchorMin        = new Vector2(0f, 1f);
                panelRT.anchorMax        = new Vector2(0f, 1f);
                panelRT.anchoredPosition = new Vector2(16f, -16f);
                panelRT.sizeDelta        = new Vector2(340f, 380f);
            }

            Image panelImage = panelT.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.10f, 0.12f, 0.16f, 0.88f);
            }

            TMP_FontAsset font = GetDefaultFont();

            // Fix Buttons
            FixButton(panelT.Find("StartHostButton"),   new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.96f), "Start Host", font);
            FixButton(panelT.Find("StartClientButton"), new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.83f), "Start Client", font);
            FixButton(panelT.Find("StartServerButton"), new Vector2(0.06f, 0.59f), new Vector2(0.94f, 0.70f), "Start Server", font);

            // Fix JoinCodeText
            Transform joinCodeT = panelT.Find("JoinCodeText");
            if (joinCodeT != null)
            {
                RectTransform rt = joinCodeT.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.06f, 0.49f);
                    rt.anchorMax = new Vector2(0.94f, 0.56f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
                TextMeshProUGUI tmp = joinCodeT.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    if (font != null)
                    {
                        tmp.font = font;
                        tmp.fontSharedMaterial = font.material;
                    }
                    tmp.fontSize  = 13;
                    tmp.color     = new Color(0.35f, 0.85f, 1.0f);
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;
                }
            }

            // Fix JoinCodeInputField
            Transform inputT = panelT.Find("JoinCodeInputField");
            if (inputT != null)
            {
                RectTransform rt = inputT.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.06f, 0.36f);
                    rt.anchorMax = new Vector2(0.94f, 0.46f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
                Image img = inputT.GetComponent<Image>();
                if (img != null) img.color = new Color(0.08f, 0.09f, 0.12f, 1f);

                Transform phT = inputT.Find("Text Area/Placeholder");
                if (phT != null)
                {
                    TextMeshProUGUI phTMP = phT.GetComponent<TextMeshProUGUI>();
                    if (phTMP != null && font != null)
                    {
                        phTMP.font = font;
                        phTMP.fontSharedMaterial = font.material;
                        phTMP.fontSize = 13;
                    }
                }
                Transform txtT = inputT.Find("Text Area/Text");
                if (txtT != null)
                {
                    TextMeshProUGUI txtTMP = txtT.GetComponent<TextMeshProUGUI>();
                    if (txtTMP != null && font != null)
                    {
                        txtTMP.font = font;
                        txtTMP.fontSharedMaterial = font.material;
                        txtTMP.fontSize = 13;
                    }
                }
            }

            // Fix StatusText
            Transform statusT = panelT.Find("StatusText");
            if (statusT != null)
            {
                RectTransform rt = statusT.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.06f, 0.05f);
                    rt.anchorMax = new Vector2(0.94f, 0.32f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
                TextMeshProUGUI tmp = statusT.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    if (font != null)
                    {
                        tmp.font = font;
                        tmp.fontSharedMaterial = font.material;
                    }
                    tmp.fontSize           = 12;
                    tmp.enableWordWrapping = true;
                    tmp.alignment          = TextAlignmentOptions.TopLeft;
                }
            }
        }

        // Fix EventSystem
        var es = GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es != null)
        {
#if ENABLE_INPUT_SYSTEM
            var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null)
            {
                UnityEngine.Object.DestroyImmediate(standalone);
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#endif
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Builder] Multiplayer Menu UI layout and fonts refreshed.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("UI Fixed", "Multiplayer Menu UI has been fixed!\n- Top-left anchoring aligned\n- TextMeshPro fonts connected\n- Layout spacing updated", "OK");
        }
    }

    private static void FixButton(Transform btnT, Vector2 anchorMin, Vector2 anchorMax, string label, TMP_FontAsset font)
    {
        if (btnT == null) return;
        RectTransform rt = btnT.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        Transform labelT = btnT.Find("Label");
        if (labelT != null)
        {
            RectTransform lrt = labelT.GetComponent<RectTransform>();
            if (lrt != null)
            {
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }
            TextMeshProUGUI tmp = labelT.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                if (font != null)
                {
                    tmp.font = font;
                    tmp.fontSharedMaterial = font.material;
                }
                tmp.text = label;
                tmp.fontSize = 15;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }
        }
    }

    // Wire a Button's onClick to a method on a MonoBehaviour using
    // UnityEventTools so the listener persists in the Inspector.
    private static void WireButtonToMethod(GameObject buttonGO,
        UnityEngine.Object target, string methodName)
    {
        UnityEngine.UI.Button btn = buttonGO.GetComponent<UnityEngine.UI.Button>();
        if (btn == null) return;

        // Remove any stale listeners first to prevent duplicates on rebuild.
        btn.onClick.RemoveAllListeners();

        // Use reflection to find the correct MethodInfo then add via UnityEventTools.
        var method = target.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance
        );

        if (method == null)
        {
            Debug.LogWarning($"[Builder] Could not find method '{methodName}' on {target.GetType().Name}");
            return;
        }

        UnityAction action = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), target, method);
        UnityEventTools.AddPersistentListener(btn.onClick, action);

        Debug.Log($"[Builder] Wired {buttonGO.name}.onClick → {target.GetType().Name}.{methodName}()");
    }

    // Fixes the NetworkPlayer prefab so the capsule stands properly on the ground
    // (CC center at 0) and the overhead health bar has FaceCamera and proper height.
    public static void FixNetworkPlayerPrefab(bool showDialog)
    {
        string prefabPath = "Assets/Prefabs/NetworkPlayer.prefab";
        if (!File.Exists(prefabPath))
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Fix NetworkPlayer", "Could not find " + prefabPath, "OK");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Fix NetworkPlayer", "Could not load " + prefabPath, "OK");
            return;
        }

        bool modified = false;

        // 1. CharacterController center = Vector3.zero
        CharacterController cc = root.GetComponent<CharacterController>();
        if (cc != null && cc.center != Vector3.zero)
        {
            cc.center = Vector3.zero;
            modified = true;
        }

        // 2. HealthBarCanvas localPosition = (0, 1.35f, 0) and add FaceCamera
        Transform canvasTransform = root.transform.Find("HealthBarCanvas");
        if (canvasTransform != null)
        {
            if (Vector3.Distance(canvasTransform.localPosition, new Vector3(0f, 1.35f, 0f)) > 0.01f)
            {
                canvasTransform.localPosition = new Vector3(0f, 1.35f, 0f);
                modified = true;
            }

            if (canvasTransform.GetComponent<FaceCamera>() == null)
            {
                canvasTransform.gameObject.AddComponent<FaceCamera>();
                modified = true;
            }
        }

        // 3. ActionParticles localPosition = Vector3.zero
        Transform particlesTransform = root.transform.Find("ActionParticles");
        if (particlesTransform != null)
        {
            if (particlesTransform.localPosition != Vector3.zero)
            {
                particlesTransform.localPosition = Vector3.zero;
                modified = true;
            }
        }

        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("[Builder] Updated NetworkPlayer.prefab: CC center = 0, HP bar position = 1.35, added FaceCamera.");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Fix NetworkPlayer", "NetworkPlayer prefab successfully updated!\n- CharacterController center reset to (0,0,0) (no underground sinking)\n- HealthBarCanvas moved to (0, 1.35, 0)\n- FaceCamera component attached.", "OK");
            }
        }
        else
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Fix NetworkPlayer", "NetworkPlayer prefab is already up to date.", "OK");
            }
        }

        PrefabUtility.UnloadPrefabContents(root);
    }
}

#endif
