using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text;

public class SceneRebuilder : EditorWindow
{
    const string MAT = "Assets/Materials/";
    const string P_WALL    = "Assets/Advance Studios/Medieval Castle/Prefabs/Wall.prefab";
    const string P_TOWER   = "Assets/Advance Studios/Medieval Castle/Prefabs/Tower A.prefab";
    const string P_GATE    = "Assets/Advance Studios/Medieval Castle/Prefabs/Double Door Frame.prefab";
    const string P_HUMAN   = "Assets/JC_LP_MedievalCharacters_LITE/Prefabs/SM_MedievalMaleLite_01.prefab";  // URP-native + humanoid (no shader wash-out)
    const string P_RISEN   = "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_NPC/Skeleton/PT_Skeleton_Male_Modular.prefab";  // undead attackers
    const string P_WOLF    = "Assets/Wolf/URP/Wolf/Prefab/Wolf_URP.prefab";
    const string HUMAN_CTRL = "Assets/Animations/FrozenWatchHumanoid.controller";
    const string WOLF_CTRL  = "Assets/Wolf/Animations/Wolf_Controller/WolfAnimations.controller";

    // Perception layer convention — layer IDs reused when named layers are absent.
    // TunnelObstacle = 8 (existing); Enemy = 9; Guard = 10.
    const int LAYER_ENEMY = 9;
    const int LAYER_GUARD = 10;

    // Set true (e.g. from script-execute) to run the rebuild without the modal dialog.
    public static bool suppressDialog = false;

    [MenuItem("Tools/Rebuild Scene (Simplify)")]
    public static void Run()
    {
        var sb = new StringBuilder();

        Nuke("CameraManager", sb);
        Nuke("TunnelMapCamera", sb);
        Nuke("Background_Mountains", sb);
        Nuke("GreatBarrier", sb);
        Nuke("BarrierTunnel", sb);
        Nuke("BarrierWall_Clean", sb);
        Nuke("CastleWall", sb);
        Nuke("AlertSystem", sb);
        // Nuke extra troops so rebuild is idempotent
        Nuke("Guard_1", sb); Nuke("Risen_1", sb); Nuke("FrostWolf_1", sb);
        Nuke("Guard_2", sb); Nuke("Guard_3", sb); Nuke("Risen_3", sb);
        Nuke("Guard_4", sb); Nuke("Guard_5", sb);
        Nuke("Guard_6", sb); Nuke("Guard_7", sb);
        Nuke("BoidFlock", sb); Nuke("SquadManagerGO", sb);
        Nuke("NavMeshSurface", sb);
        Nuke("GroundPlane", sb);
        // Stage VI game-loop objects
        Nuke("Watchfire", sb); Nuke("GameDirector", sb);
        Nuke("AttackerSpawns", sb); Nuke("PlayerSpawn", sb);
        Nuke("Risen_Template", sb); Nuke("Risen_Wave", sb);
        Nuke("Decor", sb);

        var mainCam = Camera.main;
        if (mainCam)
        {
            mainCam.transform.position = new Vector3(0f, 28f, -32f);
            mainCam.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
            mainCam.fieldOfView = 78f;
            mainCam.cullingMask = ~0;
            mainCam.farClipPlane = 600f;
            mainCam.clearFlags = CameraClearFlags.Skybox;
            var sky = LoadMat("Assets/Fantasy Skybox FREE/Panoramics/FS013/FS013_Snowy.mat");
            if (sky != null) { RenderSettings.skybox = sky; }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.55f;     // was blowing everything out at 1.0
            RenderSettings.reflectionIntensity = 0.45f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.85f, 0.92f);
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 320f;
            DynamicGI.UpdateEnvironment();
            var hud = mainCam.gameObject.GetComponent<SceneHUD>();
            if (hud == null) mainCam.gameObject.AddComponent<SceneHUD>();
            sb.AppendLine("MainCam repositioned + SceneHUD attached");
        }

        // Key light: angled, warm, soft shadows for contrast against the snow.
        var dlGO = GameObject.Find("Directional Light");
        if (dlGO != null)
        {
            dlGO.transform.rotation = Quaternion.Euler(48f, 40f, 0f);
            var L = dlGO.GetComponent<Light>();
            if (L != null) { L.intensity = 1.3f; L.color = new Color(1f, 0.96f, 0.86f); L.shadows = LightShadows.Soft; }
        }

        // Ice ground plane
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "GroundPlane";
        ground.transform.position = new Vector3(0f, 0f, 0f);
        ground.transform.localScale = new Vector3(50f, 1f, 50f);
        var snowSrc = LoadMat("Assets/Stylize Snow Texture/Materials/Stylize Snow.mat");
        if (snowSrc != null)
        {
            var snow = new Material(snowSrc);            // instance so tiling change is local
            snow.mainTextureScale = new Vector2(45f, 45f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = snow;
        }
        else
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var ice = new Color(0.82f, 0.9f, 1f);
            mat.color = ice; if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", ice);
            ground.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
        sb.AppendLine("Snow ground plane created (500x500, Stylize Snow)");

        // AlertSystem singleton lives at the scene root (perception broadcast hub).
        var alertGO = new GameObject("AlertSystem");
        var alertSys = alertGO.AddComponent<AlertSystem>();
        alertSys.alertDuration = 4f;
        alertSys.propagationRadius = 30f;
        sb.AppendLine("AlertSystem singleton created (duration 4s, propagation 30m)");

        BuildCastleWall(sb);

        // Guard_1 (player): ground level just inside the gate so it can sortie out
        EnsureAgentGO("Guard_1");
        ConfigureAgent("Guard_1", new Vector3(-3f, 0f, -8f), P_HUMAN, new Color(0.4f, 0.7f, 1f),
            new Vector3[] { new Vector3(-5f, 0f, -9f), new Vector3(-2f, 0f, -5f) }, sb);
        // Risen: approaches main gate from north, goes through to defended zone
        EnsureAgentGO("Risen_1");
        ConfigureAgent("Risen_1", new Vector3(0f, 0f, 28f), P_RISEN, new Color(0.8f, 0.15f, 0.15f),
            new Vector3[] { new Vector3(0f, 0f, 25f), new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, -15f) }, sb);
        // FrostWolf: scout patrols north of wall
        EnsureAgentGO("FrostWolf_1");
        ConfigureAgent("FrostWolf_1", new Vector3(8f, 0f, 15f), P_WOLF, Color.white,
            new Vector3[] { new Vector3(-10f, 0f, 15f), new Vector3(10f, 0f, 15f), new Vector3(10f, 0f, 22f), new Vector3(-10f, 0f, 22f) }, sb);

        // ---- Extra troops (Stage IV game improvement) ----
        { var g = new GameObject("Guard_2"); g.AddComponent<NavMeshAgent>(); g.AddComponent<AgentMover>(); }
        ConfigureAgent("Guard_2", new Vector3(4f, 0f, -8f), P_HUMAN, new Color(0.4f, 0.7f, 1f),
            new Vector3[] { new Vector3(5f, 0f, -9f), new Vector3(2f, 0f, -5f) }, sb);
        { var g = new GameObject("Guard_3"); g.AddComponent<NavMeshAgent>(); g.AddComponent<AgentMover>(); }
        ConfigureAgent("Guard_3", new Vector3(12f, 5.4f, 0f), P_HUMAN, new Color(0.35f, 0.65f, 1f),
            new Vector3[] { new Vector3(6f, 5.4f, 0f), new Vector3(20f, 5.4f, 0f) }, sb);
        // Guard_4: extra ground melee defender near the gate
        { var g = new GameObject("Guard_4"); g.AddComponent<NavMeshAgent>(); g.AddComponent<AgentMover>(); }
        ConfigureAgent("Guard_4", new Vector3(-8f, 0f, -6f), P_HUMAN, new Color(0.45f, 0.7f, 1f),
            new Vector3[] { new Vector3(-9f, 0f, -7f), new Vector3(-6f, 0f, -4f) }, sb);
        // Guard_5: second wall archer on the left of the gate
        { var g = new GameObject("Guard_5"); g.AddComponent<NavMeshAgent>(); g.AddComponent<AgentMover>(); }
        ConfigureAgent("Guard_5", new Vector3(-14f, 5.4f, 0f), P_HUMAN, new Color(0.35f, 0.65f, 1f),
            new Vector3[] { new Vector3(-20f, 5.4f, 0f), new Vector3(-6f, 5.4f, 0f) }, sb);
        // Guard_6 + Guard_7: a second squad holding the line around the Watchfire
        { var g = new GameObject("Guard_6"); g.AddComponent<NavMeshAgent>(); g.AddComponent<AgentMover>(); }
        ConfigureAgent("Guard_6", new Vector3(-4f, 0f, -16f), P_HUMAN, new Color(0.45f, 0.7f, 1f),
            new Vector3[] { new Vector3(-5f, 0f, -17f), new Vector3(-3f, 0f, -14f) }, sb);
        { var g = new GameObject("Guard_7"); g.AddComponent<NavMeshAgent>(); g.AddComponent<AgentMover>(); }
        ConfigureAgent("Guard_7", new Vector3(4f, 0f, -16f), P_HUMAN, new Color(0.45f, 0.7f, 1f),
            new Vector3[] { new Vector3(5f, 0f, -17f), new Vector3(3f, 0f, -14f) }, sb);
        { var g = new GameObject("Risen_3"); g.AddComponent<NavMeshAgent>(); g.AddComponent<AgentMover>(); }
        ConfigureAgent("Risen_3", new Vector3(4f, 0f, 30f), P_RISEN, new Color(0.85f, 0.2f, 0.1f),
            new Vector3[] { new Vector3(4f, 0f, 25f), new Vector3(3f, 0f, 10f), new Vector3(2f, 0f, -12f) }, sb);

        // ThirdPersonController on Guard_1 — F key toggles player control
        var g1 = GameObject.Find("Guard_1");
        if (g1 != null && mainCam != null)
        {
            var tpc = g1.GetComponent<ThirdPersonController>();
            if (tpc == null) tpc = g1.AddComponent<ThirdPersonController>();
            tpc.targetCamera = mainCam;
            tpc.agentLabel = "GUARD_1";
            tpc.birdsEyePos = new Vector3(0f, 28f, -32f);
            tpc.birdsEyeEuler = new Vector3(42f, 0f, 0f);
            sb.AppendLine("ThirdPersonController on Guard_1 (F = toggle player/AI)");
        }

        AttachGroup(sb);
        BuildMazeTunnel(sb);
        BuildGameLoop(sb);
        DressScene(sb);

        // Create NavMeshSurface and bake — covers ground (y=0) + wall walkway (y=5.2)
        var navSurfGO = new GameObject("NavMeshSurface");
        var surf = navSurfGO.AddComponent<NavMeshSurface>();
        surf.collectObjects = CollectObjects.Volume;
        surf.center = new Vector3(0f, 3f, 5f);
        surf.size = new Vector3(404f, 8f, 64f);
        surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surf.BuildNavMesh();
        sb.AppendLine("NavMeshSurface baked (volume 404x8x64, covers ground y=0 + walkway y=5.2)");

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        sb.AppendLine("Ctrl+S to save.");

        Debug.Log("=== REBUILD COMPLETE ===\n" + sb.ToString());
        if (!suppressDialog)
            EditorUtility.DisplayDialog("Rebuild Complete", sb.ToString(), "OK");
    }

    // -----------------------------------------------------------------------
    // Long primitive wall — 400m total (x=-200 to 200), gate gap at center
    // -----------------------------------------------------------------------
    static void BuildCastleWall(StringBuilder sb)
    {
        var root = new GameObject("CastleWall");
        root.transform.position = Vector3.zero;

        var matStone = LoadMat(MAT + "Mat_Stone.mat");
        var matTop   = LoadMat(MAT + "Mat_WallTop.mat");

        // Wall is 10m THICK (z=-5..5), 400m long. Two gaps: main gate x=-2..2, labyrinth x=45..55.
        const float thickness = 10f;
        const float height = 5f;
        const float yCenter = 2.5f;

        // Left segment: x=-200 to -2
        var wallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallL.name = "Wall_L";
        wallL.transform.SetParent(root.transform, false);
        wallL.transform.localPosition = new Vector3(-101f, yCenter, 0f);
        wallL.transform.localScale = new Vector3(198f, height, thickness);
        if (matStone != null) wallL.GetComponent<MeshRenderer>().sharedMaterial = matStone;

        // Middle segment: x=2 to 45
        var wallM = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallM.name = "Wall_M";
        wallM.transform.SetParent(root.transform, false);
        wallM.transform.localPosition = new Vector3(23.5f, yCenter, 0f);
        wallM.transform.localScale = new Vector3(43f, height, thickness);
        if (matStone != null) wallM.GetComponent<MeshRenderer>().sharedMaterial = matStone;

        // Right segment: x=55 to 200
        var wallR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallR.name = "Wall_R";
        wallR.transform.SetParent(root.transform, false);
        wallR.transform.localPosition = new Vector3(127.5f, yCenter, 0f);
        wallR.transform.localScale = new Vector3(145f, height, thickness);
        if (matStone != null) wallR.GetComponent<MeshRenderer>().sharedMaterial = matStone;

        // Walkway on top of wall — 3 segments matching wall segments (gaps at x=-2..2 and x=45..55)
        MakeWalkway("Walkway_L", root.transform, -101f, height + 0.1f, 198f, thickness, matTop);
        MakeWalkway("Walkway_M", root.transform, 23.5f, height + 0.1f, 43f, thickness, matTop);
        MakeWalkway("Walkway_R", root.transform, 127.5f, height + 0.1f, 145f, thickness, matTop);

        // NavBarrier across the labyrinth gap: NavMesh agents use ONLY the main gate.
        // The tunnel is an A*-driven route (TunnelRunner) — A* doesn't care about this barrier.
        var navBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        navBar.name = "NavBarrier_LabyrinthGap";
        navBar.transform.SetParent(root.transform, false);
        navBar.transform.localPosition = new Vector3(50f, yCenter, 0f);
        navBar.transform.localScale = new Vector3(10f, height, thickness);
        HideVisual(navBar);

        // MAIN GATE — fills the 4m-wide, 10m-thick gap at x=-2..2
        var gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = "MainGate";
        gate.transform.SetParent(root.transform, false);
        gate.transform.localPosition = new Vector3(0f, yCenter, 0f);
        gate.transform.localScale = new Vector3(4f, height, thickness);
        var matGate = LoadMat(MAT + "Mat_IceGate.mat");
        if (matGate != null) gate.GetComponent<MeshRenderer>().sharedMaterial = matGate;
        var obst = gate.AddComponent<NavMeshObstacle>();
        obst.shape = NavMeshObstacleShape.Box;
        obst.size = Vector3.one;
        obst.carving = true;
        var gc = gate.AddComponent<GateController>();
        gc.toggleKey = UnityEngine.InputSystem.Key.G;
        gc.openLiftHeight = 7f;
        gc.startOpen = true;   // game begins with the gate open so the assault can flow

        // ProximitySensor volume covering the Main Gate chokepoint (5.4 Object detection).
        var probe = new GameObject("GateProximitySensor");
        probe.transform.SetParent(root.transform, false);
        probe.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        var box = probe.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(4f, 3f, 10f);
        var prox = probe.AddComponent<ProximitySensor>();
        prox.targetMask = 1 << LAYER_ENEMY;
        prox.label = "MainGate";

        sb.AppendLine("Wall built: 400m x 10m thick, main gate (G) at x=0, labyrinth gap at x=45..55");
        sb.AppendLine("ProximitySensor armed at Main Gate (box 4x3x10, targets layer " + LAYER_ENEMY + ")");
    }

    static void HideVisual(GameObject g)
    {
        var mr = g.GetComponent<MeshRenderer>();
        if (mr) mr.enabled = false;
    }

    static void MakeWalkway(string name, Transform parent, float centerX, float y, float lengthX, float thicknessZ, Material mat)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        w.transform.SetParent(parent, false);
        w.transform.localPosition = new Vector3(centerX, y, 0f);
        w.transform.localScale = new Vector3(lengthX, 0.2f, thicknessZ);
        if (mat != null) w.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 scale, Transform parent, string name)
    {
        if (prefab == null) return null;
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.name = name;
        inst.transform.localPosition = pos;
        inst.transform.localRotation = rot;
        inst.transform.localScale = scale;
        return inst;
    }

    // -----------------------------------------------------------------------
    static void ConfigureAgent(string agentName, Vector3 pos, string prefabPath, Color tint, Vector3[] waypoints, StringBuilder sb)
    {
        var go = GameObject.Find(agentName);
        if (go == null) { sb.AppendLine("  [!] " + agentName + " NOT FOUND"); return; }

        go.transform.position = pos;
        go.transform.localScale = Vector3.one;
        go.transform.rotation = Quaternion.identity;

        var pmr = go.GetComponent<MeshRenderer>(); if (pmr) DestroyImmediate(pmr);
        var pmf = go.GetComponent<MeshFilter>(); if (pmf) DestroyImmediate(pmf);
        for (int i = go.transform.childCount - 1; i >= 0; i--)
        {
            var c = go.transform.GetChild(i);
            if (c.name.StartsWith("Visual") || c.GetComponent<MeshRenderer>() != null || c.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                DestroyImmediate(c.gameObject);
        }

        var prefab = LoadPrefab(prefabPath);
        if (prefab != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
            inst.name = "Visual";
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            AssignController(inst, prefabPath);   // out of T-pose into an idle/locomotion controller
            if (!prefabPath.Contains("Wolf")) MakeRenderersURP(inst);  // fix magenta (built-in -> URP)
            AddTeamRing(go, tint);                // colored ground ring keeps faction readable w/o flat-tinting the mesh
        }
        else
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "Visual";
            var vc = fallback.GetComponent<Collider>(); if (vc) DestroyImmediate(vc);
            fallback.transform.SetParent(go.transform, false);
            fallback.transform.localPosition = new Vector3(0f, 1f, 0f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = tint; mat.SetColor("_BaseColor", tint);
            fallback.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        var na = go.GetComponent<NavMeshAgent>();
        if (na) { na.stoppingDistance = 0.3f; if (na.speed < 0.1f) na.speed = 2.5f; na.height = 2f; na.radius = 0.4f; na.baseOffset = 0f; na.obstacleAvoidanceType = ObstacleAvoidanceType.GoodQualityObstacleAvoidance; }

        var wpRoot = GameObject.Find("Waypoints");
        if (wpRoot == null) wpRoot = new GameObject("Waypoints");
        var oldWp = wpRoot.transform.Find("WP_" + agentName);
        if (oldWp) DestroyImmediate(oldWp.gameObject);
        var wpParent = new GameObject("WP_" + agentName);
        wpParent.transform.SetParent(wpRoot.transform, false);
        var xforms = new Transform[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            var w = new GameObject("WP_" + agentName + "_" + i);
            w.transform.SetParent(wpParent.transform, false);
            w.transform.position = waypoints[i];
            xforms[i] = w.transform;
        }
        var mover = go.GetComponent<AgentMover>();
        if (mover != null)
        {
            var field = typeof(AgentMover).GetField("waypoints");
            if (field != null) field.SetValue(mover, xforms);
        }

        AttachPerception(go, agentName, sb);
        AttachDecision(go, agentName, sb);

        sb.AppendLine("Agent " + agentName + " @ " + pos + " using " + System.IO.Path.GetFileName(prefabPath));
    }

    // -----------------------------------------------------------------------
    // Perception wiring (5.1-5.4): AnimatorBridge on every agent, FOV+hearing
    // on guards, noise emitter on enemies. Layers partition enemies and guards.
    static void AttachPerception(GameObject go, string agentName, StringBuilder sb)
    {
        if (go.GetComponent<AnimatorBridge>() == null)
            go.AddComponent<AnimatorBridge>();

        bool isGuard = agentName.StartsWith("Guard_") || agentName == "FrostWolf_1";
        bool isEnemy = agentName.StartsWith("Risen_");

        if (isGuard)
        {
            SetLayerRecursively(go, LAYER_GUARD);

            var fov = go.GetComponent<FieldOfView>();
            if (fov == null) fov = go.AddComponent<FieldOfView>();
            fov.viewRadius = agentName == "FrostWolf_1" ? 18f : 14f;
            fov.viewAngle = agentName == "FrostWolf_1" ? 120f : 90f;
            fov.eyeHeight = agentName == "FrostWolf_1" ? 0.9f : 1.7f;
            fov.targetMask = 1 << LAYER_ENEMY;
            fov.obstacleMask = 1 << 0;

            var hear = go.GetComponent<HearingSensor>();
            if (hear == null) hear = go.AddComponent<HearingSensor>();
            hear.earReach = agentName == "FrostWolf_1" ? 18f : 12f;

            sb.AppendLine("  + Perception on " + agentName + ": FOV(" + fov.viewRadius + "m/" + fov.viewAngle + "deg) + Hearing(" + hear.earReach + "m)");
        }

        if (isEnemy)
        {
            SetLayerRecursively(go, LAYER_ENEMY);

            if (go.GetComponent<SphereCollider>() == null &&
                go.GetComponent<CapsuleCollider>() == null &&
                go.GetComponent<BoxCollider>() == null)
            {
                var sc = go.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 0.6f;
                sc.center = new Vector3(0f, 1f, 0f);
            }

            var ne = go.GetComponent<NoiseEmitter>();
            if (ne == null) ne = go.AddComponent<NoiseEmitter>();
            ne.loudness = 10f;
            ne.emitInterval = 0.5f;
            ne.speedThreshold = 1.5f;

            sb.AppendLine("  + NoiseEmitter on " + agentName + " (loudness " + ne.loudness + ")");
        }
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    static void EnsureAgentGO(string name)
    {
        if (GameObject.Find(name) != null) return;
        var go = new GameObject(name);
        go.AddComponent<NavMeshAgent>();
        go.AddComponent<AgentMover>();
    }

    // -----------------------------------------------------------------------
    // Decision wiring (6.1-6.4): GuardFSM on guards, WolfBT on FrostWolf,
    // RisenFSM + UtilityBrain on Risen agents. AgentBlackboard on all.
    static void AttachDecision(GameObject go, string agentName, StringBuilder sb)
    {
        if (go.GetComponent<AgentBlackboard>() == null)
            go.AddComponent<AgentBlackboard>();

        bool isGuard  = agentName.StartsWith("Guard_");
        bool isWolf   = agentName == "FrostWolf_1";
        bool isEnemy  = agentName.StartsWith("Risen_") && agentName != "Risen_2";

        if (isGuard)
        {
            if (go.GetComponent<GuardFSM>() == null)
                go.AddComponent<GuardFSM>();
            sb.AppendLine("  + GuardFSM on " + agentName);
        }

        if (isWolf)
        {
            if (go.GetComponent<WolfBT>() == null)
                go.AddComponent<WolfBT>();
            sb.AppendLine("  + WolfBT on " + agentName);
        }

        if (isEnemy)
        {
            if (go.GetComponent<RisenFSM>() == null)
                go.AddComponent<RisenFSM>();

            var brain = go.GetComponent<UtilityBrain>();
            if (brain == null) brain = go.AddComponent<UtilityBrain>();

            var adv = go.GetComponent<AdvanceAction>();
            if (adv == null) adv = go.AddComponent<AdvanceAction>();
            adv.actionName = "Advance";

            var ret = go.GetComponent<RetreatAction>();
            if (ret == null) ret = go.AddComponent<RetreatAction>();
            ret.actionName = "Retreat";

            brain.availableActions.Clear();
            brain.availableActions.Add(adv);
            brain.availableActions.Add(ret);

            sb.AppendLine("  + RisenFSM + UtilityBrain on " + agentName);
        }
    }

    // -----------------------------------------------------------------------
    // Group wiring (7.1-7.3): BoidFlock, SquadManager, AIUpdateScheduler on guards.
    static void AttachGroup(StringBuilder sb)
    {
        // BoidFlock -- raven flock that circles above the wall and scatters on sound events
        var flockGO = new GameObject("BoidFlock");
        flockGO.transform.position = new Vector3(0f, 0f, 0f);
        var bm = flockGO.AddComponent<BoidManager>();
        bm.flockSize = 18;
        bm.flockHeight = 24f;
        bm.spawnRadius = 16f;
        bm.scareLoudnessThreshold = 30f;
        bm.scareDuration = 5f;
        sb.AppendLine("BoidFlock spawned: 20 boids at y=14, scatter on SoundEvent >= 30");

        // SquadManager -- distributes guards into frontal/flank slots when any guard is in Chase/Attack
        var sqGO = new GameObject("SquadManagerGO");
        sqGO.AddComponent<SquadManager>();
        sb.AppendLine("SquadManager created (auto-finds Guard_1/2/3 at Start)");

        // AIUpdateScheduler -- LOD time-slicing on every guard's FOV sensor
        string[] guardNames = { "Guard_1", "Guard_2", "Guard_3" };
        foreach (string gn in guardNames)
        {
            var go = GameObject.Find(gn);
            if (go == null) continue;
            if (go.GetComponent<AIUpdateScheduler>() == null)
                go.AddComponent<AIUpdateScheduler>();
            sb.AppendLine("  + AIUpdateScheduler on " + gn + " (near 0.1s / far 1.0s)");
        }
    }

    // -----------------------------------------------------------------------
    // Stage VI: the playable siege loop — Watchfire objective, combat stats on
    // every fighter, wave-spawn template, the GameDirector + pause menu.
    static void BuildGameLoop(StringBuilder sb)
    {
        var fireC = BuildWatchfire(sb);

        // Combat stats on the standing roster
        AttachCombat("Guard_1", Combatant.Team.Defender, 160f, 20f, 2.6f, sb);
        AttachCombat("Guard_2", Combatant.Team.Defender, 150f, 20f, 2.6f, sb);
        AttachCombat("Guard_4", Combatant.Team.Defender, 150f, 20f, 2.6f, sb);
        AttachCombat("Guard_6", Combatant.Team.Defender, 150f, 20f, 2.6f, sb);
        AttachCombat("Guard_7", Combatant.Team.Defender, 150f, 20f, 2.6f, sb);
        AttachCombat("Guard_3", Combatant.Team.Defender, 130f, 13f, 18f, sb);  // wall archer
        AttachCombat("Guard_5", Combatant.Team.Defender, 130f, 13f, 18f, sb);  // wall archer
        AttachCombat("Risen_1", Combatant.Team.Attacker,  85f, 13f, 2.2f, sb);
        AttachCombat("Risen_3", Combatant.Team.Attacker,  85f, 13f, 2.2f, sb);

        // Wall archers — never sortie, loose arrows at the gate approach.
        foreach (var an in new[] { "Guard_3", "Guard_5" })
        {
            var ga = GameObject.Find(an);
            if (ga == null) continue;
            var fsm = ga.GetComponent<GuardFSM>(); if (fsm != null) fsm.canSortie = false;
            var gac = ga.GetComponent<Combatant>(); if (gac != null) gac.autoAttack = false;
            var arch = ga.GetComponent<Archer>(); if (arch == null) arch = ga.AddComponent<Archer>();
            arch.range = 20f; arch.damage = 13f; arch.cooldown = 1.1f;
            sb.AppendLine("  + Archer on " + an + " (fires arrows, range 20)");
        }

        // Defence roles: G6/G7 are the dedicated Watchfire squad (useCustomHome -> tight
        // leash, never abandon the objective). G1/G2/G4 are mobile guards: they roam the
        // whole field and converge on the real threat, whether it comes over the gate or
        // out of the A* tunnel. The War Horn (Q) lifts every leash so all guards rally.
        Vector3 wfPos = fireC != null ? fireC.transform.position : new Vector3(0f, 0f, -20f);
        foreach (var gn in new[] { "Guard_6", "Guard_7" })
        {
            var g = GameObject.Find(gn);
            var fsm = g != null ? g.GetComponent<GuardFSM>() : null;
            if (fsm != null) { fsm.useCustomHome = true; fsm.homePoint = wfPos; fsm.leashRadius = 18f; }
        }
        foreach (var gn in new[] { "Guard_1", "Guard_2", "Guard_4" })
        {
            var g = GameObject.Find(gn);
            var fsm = g != null ? g.GetComponent<GuardFSM>() : null;
            if (fsm != null) fsm.useCustomHome = false;   // mobile: roam to the threat
        }
        sb.AppendLine("  + Defence roles: G6/G7 hold the Watchfire (leash 18); G1/G2/G4 roam to the threat; Q rallies all");

        // Guard_1 is player-driven: manual swings, revivable instead of destroyed.
        var g1 = GameObject.Find("Guard_1");
        Combatant playerC = null;
        if (g1 != null)
        {
            playerC = g1.GetComponent<Combatant>();
            // revivable; PlayerCombat toggles auto-melee (AI) vs manual swings (player)
            if (playerC != null) { playerC.destroyOnDeath = false; }
            if (g1.GetComponent<PlayerCombat>() == null) g1.AddComponent<PlayerCombat>();
            sb.AppendLine("  + PlayerCombat on Guard_1 (LMB/Space = swing)");
        }

        // Two dedicated demo invaders, ONE movement driver each (no conflict -> no stuck pathing):
        //   Risen_1 = tactical-retreat FSM (Theme 6.3); Risen_3 = utility AI (Theme 6.4).
        {
            var r = GameObject.Find("Risen_1");
            if (r != null)
            {
                var ub = r.GetComponent<UtilityBrain>();  if (ub) DestroyImmediate(ub);
                var aa = r.GetComponent<AdvanceAction>(); if (aa) DestroyImmediate(aa);
                var ra = r.GetComponent<RetreatAction>(); if (ra) DestroyImmediate(ra);
                var sa = r.GetComponent<SiegeAttacker>(); if (sa) DestroyImmediate(sa);
                if (r.GetComponent<AgentMover>() == null) r.AddComponent<AgentMover>();
                if (r.GetComponent<RisenFSM>() == null) r.AddComponent<RisenFSM>();
                sb.AppendLine("  + Risen_1 = RisenFSM tactical-retreat demo (Theme 6.3)");
            }
        }
        {
            var r = GameObject.Find("Risen_3");
            if (r != null)
            {
                var mv = r.GetComponent<AgentMover>(); if (mv) DestroyImmediate(mv);
                var rf = r.GetComponent<RisenFSM>();   if (rf) DestroyImmediate(rf);
                var sa = r.GetComponent<SiegeAttacker>(); if (sa) DestroyImmediate(sa);
                var brain = r.GetComponent<UtilityBrain>(); if (brain == null) brain = r.AddComponent<UtilityBrain>();
                var adv = r.GetComponent<AdvanceAction>(); if (adv == null) adv = r.AddComponent<AdvanceAction>();
                var ret = r.GetComponent<RetreatAction>(); if (ret == null) ret = r.AddComponent<RetreatAction>();
                adv.actionName = "Advance"; ret.actionName = "Retreat";
                adv.gateTarget = fireC != null ? fireC.transform : null;   // advance toward the Watchfire
                adv.maxDistance = 60f;
                brain.availableActions.Clear();
                brain.availableActions.Add(adv);
                brain.availableActions.Add(ret);
                sb.AppendLine("  + Risen_3 = UtilityBrain Advance/Retreat demo (Theme 6.4)");
            }
        }

        // FrostWolf becomes a horde predator (BT demo intact, now hunts guards).
        FixWolfAsAttacker(sb);

        // Spawn anchors (north of the wall)
        var spawns = new GameObject("AttackerSpawns");
        var spawnPositions = new Vector3[] {
            new Vector3(-7f, 0f, 33f), new Vector3(0f, 0f, 34f), new Vector3(7f, 0f, 33f),
            new Vector3(50f, 0f, 10f), new Vector3(48f, 0f, 10f)   // inside the tunnel's north end (A* path start)
        };
        var spawnXforms = new Transform[spawnPositions.Length];
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            var s = new GameObject("Spawn_" + i);
            s.transform.SetParent(spawns.transform, false);
            s.transform.position = spawnPositions[i];
            spawnXforms[i] = s.transform;
        }

        var pSpawn = new GameObject("PlayerSpawn");
        pSpawn.transform.position = new Vector3(-3f, 0f, -8f);

        var template = BuildAttackerTemplate(sb);

        var gdGO = new GameObject("GameDirector");
        var dir = gdGO.AddComponent<GameDirector>();
        dir.watchfire = fireC;
        dir.attackerTemplate = template;
        dir.spawnPoints = spawnXforms;
        dir.player = playerC;
        dir.playerSpawn = pSpawn.transform;
        var tgGO = GameObject.Find("BarrierTunnel_Grid"); if (tgGO != null) dir.tunnelGrid = tgGO.GetComponent<AStarGrid>();
        var teGO = GameObject.Find("Tunnel_Exit"); if (teGO != null) dir.tunnelExit = teGO.transform;
        dir.waveSizes = new int[] { 6, 8, 10 };
        dir.spawnStagger = 1.0f;       // trickle in so defenders aren't instantly swarmed
        dir.betweenWaveDelay = 6f;
        gdGO.AddComponent<PauseMenu>();
        gdGO.AddComponent<Commander>();   // War Horn (Q) rally buff
        gdGO.AddComponent<StoryIntro>();  // story + tutorial overlay at start (F2 to reopen)
        sb.AppendLine("GameDirector + PauseMenu + Commander created (waves 6/8/10)");

        // Tunnel is now a real NavMesh route: attackers spawn at its north mouth and walk
        // the maze to the south exit. Sealing doors Y/U (NavMeshObstacles) blocks them.
        sb.AppendLine("Tunnel is a live NavMesh route (north mouth spawns; Y/U doors seal it)");
    }

    static Combatant BuildWatchfire(StringBuilder sb)
    {
        var go = new GameObject("Watchfire");
        go.transform.position = new Vector3(0f, 0f, -20f);

        var basePillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        basePillar.name = "Base";
        basePillar.transform.SetParent(go.transform, false);
        basePillar.transform.localPosition = new Vector3(0f, 1f, 0f);
        basePillar.transform.localScale = new Vector3(1.8f, 1f, 1.8f);
        var bm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var stone = new Color(0.22f, 0.22f, 0.26f);
        bm.color = stone; if (bm.HasProperty("_BaseColor")) bm.SetColor("_BaseColor", stone);
        basePillar.GetComponent<MeshRenderer>().sharedMaterial = bm;

        var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flame.name = "Flame";
        flame.transform.SetParent(go.transform, false);
        flame.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        flame.transform.localScale = new Vector3(1.4f, 2.0f, 1.4f);
        var fcol = flame.GetComponent<Collider>(); if (fcol) DestroyImmediate(fcol);
        var fm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var orange = new Color(1f, 0.5f, 0.12f);
        fm.color = orange; if (fm.HasProperty("_BaseColor")) fm.SetColor("_BaseColor", orange);
        if (fm.HasProperty("_EmissionColor")) { fm.EnableKeyword("_EMISSION"); fm.SetColor("_EmissionColor", orange * 3f); }
        flame.GetComponent<MeshRenderer>().sharedMaterial = fm;

        var lightGO = new GameObject("FireLight");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 2.8f, 0f);
        var lt = lightGO.AddComponent<Light>();
        lt.type = LightType.Point; lt.color = orange; lt.range = 24f; lt.intensity = 3.5f;

        // A short, thin marker flame above the brazier — a subtle landmark, not a wall.
        var spire = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        spire.name = "Spire";
        var scol = spire.GetComponent<Collider>(); if (scol) DestroyImmediate(scol);
        spire.transform.SetParent(go.transform, false);
        spire.transform.localPosition = new Vector3(0f, 3.6f, 0f);
        spire.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
        var spireMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        spireMat.color = orange;
        if (spireMat.HasProperty("_BaseColor")) spireMat.SetColor("_BaseColor", orange);
        if (spireMat.HasProperty("_EmissionColor")) { spireMat.EnableKeyword("_EMISSION"); spireMat.SetColor("_EmissionColor", orange * 3f); }
        spire.GetComponent<MeshRenderer>().sharedMaterial = spireMat;

        // Real fire VFX (Cartoon FX) burning in the brazier.
        var firePf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR Fire.prefab");
        if (firePf != null)
        {
            var fire = (GameObject)PrefabUtility.InstantiatePrefab(firePf, go.transform);
            fire.name = "FireVFX";
            fire.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            fire.transform.localScale = Vector3.one * 2.4f;
        }

        var obst = basePillar.AddComponent<NavMeshObstacle>();
        obst.shape = NavMeshObstacleShape.Capsule;
        obst.center = new Vector3(0f, 1f, 0f);
        obst.radius = 0.7f; obst.height = 2.4f; obst.carving = true;   // smaller so attackers reach melee range

        var c = go.AddComponent<Combatant>();
        c.team = Combatant.Team.Defender; c.isStructure = true;
        c.autoAttack = false; c.attackDamage = 0f;
        c.maxHealth = 400f; c.health = 400f;
        c.destroyOnDeath = false;   // the objective must persist (HUD + restart); dying => Lose, not Destroy
        var wf = go.AddComponent<Watchfire>();
        wf.flame = lt;

        sb.AppendLine("Watchfire built at (0,0,-20), HP 400 (lose if extinguished)");
        return c;
    }

    static void AttachCombat(string name, Combatant.Team team, float hp, float dmg, float range, StringBuilder sb)
    {
        var go = GameObject.Find(name);
        if (go == null) { sb.AppendLine("  [!] AttachCombat: " + name + " NOT FOUND"); return; }
        var c = go.GetComponent<Combatant>();
        if (c == null) c = go.AddComponent<Combatant>();
        c.team = team; c.maxHealth = hp; c.health = hp;
        c.attackDamage = dmg; c.attackRange = range; c.attackInterval = 1f;
        sb.AppendLine("  + Combatant on " + name + " (" + team + ", hp " + hp + ", dmg " + dmg + ")");
    }

    static void FixWolfAsAttacker(StringBuilder sb)
    {
        var wolf = GameObject.Find("FrostWolf_1");
        if (wolf == null) return;

        SetLayerRecursively(wolf, LAYER_ENEMY);   // grouped with the horde

        var fov = wolf.GetComponent<FieldOfView>();
        if (fov != null) fov.targetMask = 1 << LAYER_GUARD;   // wolf hunts defenders

        if (wolf.GetComponent<SphereCollider>() == null)
        {
            var sc = wolf.AddComponent<SphereCollider>();
            sc.isTrigger = true; sc.radius = 0.7f; sc.center = new Vector3(0f, 0.6f, 0f);
        }

        var c = wolf.GetComponent<Combatant>();
        if (c == null) c = wolf.AddComponent<Combatant>();
        c.team = Combatant.Team.Attacker; c.maxHealth = 80f; c.health = 80f;
        c.attackDamage = 18f; c.attackRange = 2.2f; c.attackInterval = 0.85f;
        sb.AppendLine("  + FrostWolf re-tasked as horde predator (Attacker, hunts guards)");
    }

    static GameObject BuildAttackerTemplate(StringBuilder sb)
    {
        var go = new GameObject("Risen_Template");
        go.transform.position = new Vector3(0f, 0f, 44f);

        var na = go.AddComponent<NavMeshAgent>();
        na.speed = 3.3f; na.angularSpeed = 400f; na.acceleration = 24f;
        na.stoppingDistance = 1.3f; na.radius = 0.4f; na.height = 2f; na.baseOffset = 0f;
        na.obstacleAvoidanceType = ObstacleAvoidanceType.GoodQualityObstacleAvoidance;

        var prefab = LoadPrefab(P_RISEN);
        if (prefab != null)
        {
            var vis = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
            vis.name = "Visual";
            vis.transform.localPosition = Vector3.zero;
            AssignController(vis, P_RISEN);
            MakeRenderersURP(vis);
            AddTeamRing(go, new Color(0.9f, 0.2f, 0.15f));
        }
        else
        {
            var fb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fb.name = "Visual";
            var col = fb.GetComponent<Collider>(); if (col) DestroyImmediate(col);
            fb.transform.SetParent(go.transform, false);
            fb.transform.localPosition = new Vector3(0f, 1f, 0f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var red = new Color(0.6f, 0.08f, 0.1f);
            mat.color = red; mat.SetColor("_BaseColor", red);
            fb.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        go.AddComponent<AnimatorBridge>();
        go.AddComponent<AgentBlackboard>();

        var sc2 = go.AddComponent<SphereCollider>();
        sc2.isTrigger = true; sc2.radius = 0.6f; sc2.center = new Vector3(0f, 1f, 0f);

        var ne = go.AddComponent<NoiseEmitter>();
        ne.loudness = 10f; ne.emitInterval = 0.5f; ne.speedThreshold = 1.5f;

        var c = go.AddComponent<Combatant>();
        c.team = Combatant.Team.Attacker; c.maxHealth = 85f; c.health = 85f;
        c.attackDamage = 13f; c.attackRange = 2.2f; c.attackInterval = 1f;

        go.AddComponent<SiegeAttacker>();

        SetLayerRecursively(go, LAYER_ENEMY);
        go.SetActive(false);

        sb.AppendLine("Risen_Template built (inactive) — cloned per wave by GameDirector");
        return go;
    }

    // -----------------------------------------------------------------------
    // Scene dressing: real castle towers/gate-frame + trees/rocks/crosses/props +
    // weapons in hand. EVERYTHING here is collider-stripped (pure visual) so the
    // baked NavMesh and gameplay geometry are untouched.
    static void DressScene(StringBuilder sb)
    {
        const string WF = "Assets/Lowpoly Forest Pack Winter/Prefabs/";
        const string PROP = "Assets/Polytope Studio/Lowpoly_Props/Prefabs/";
        const string WPN = "Assets/Polytope Studio/Lowpoly_Weapons/Prefabs/";

        var root = new GameObject("Decor");
        var t = root.transform;

        const string CAS = "Assets/Advance Studios/Medieval Castle/Prefabs/";

        // Towers flanking the gate and stepping along the wall (Tower A is thin/short at 1x -> scale up).
        foreach (var x in new[] { -9f, 9f, -38f, 38f, -85f, 85f })
            Decor(P_TOWER, new Vector3(x, 0f, 0f), Quaternion.identity, 2.6f, t, true);

        // Shadowhold: the KEEP you defend, pushed well back so it doesn't block the battlefield.
        Decor(CAS + "Castle.prefab", new Vector3(0f, 0f, -64f), Quaternion.identity, 6f, t, true);
        Decor(CAS + "House A.prefab", new Vector3(-30f, 0f, -52f), Quaternion.Euler(0, 90, 0), 3.5f, t, true);
        Decor(CAS + "House B.prefab", new Vector3(30f, 0f, -52f), Quaternion.Euler(0, -90, 0), 3.5f, t, true);
        Decor(CAS + "House A.prefab", new Vector3(-42f, 0f, -60f), Quaternion.Euler(0, 55, 0), 3f, t, true);
        Decor(CAS + "House B.prefab", new Vector3(42f, 0f, -60f), Quaternion.Euler(0, -55, 0), 3f, t, true);

        // Distant mountain ring = snowy horizon backdrop (far enough not to crowd the arena).
        for (int i = 0; i < 12; i++)
        {
            float a = i * (360f / 12f) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 230f;
            Decor(WF + "Mountain.prefab", p, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                Random.Range(1.5f, 3f), t, true);
        }

        // Frozen Wastes (north): snowy pines, dead trees, rocks, shrubs + battlefield crosses.
        Scatter(WF + "Winter Pine Tree 1.prefab", 16, -75f, 75f, 9f, 55f, 1.2f, 2.2f, t);
        Scatter(WF + "Winter Pine Tree 3.prefab", 12, -75f, 75f, 9f, 55f, 1.2f, 2.2f, t);
        Scatter(WF + "Winter Dead Standing Tree 1.prefab", 8, -72f, 72f, 12f, 52f, 1.2f, 2.0f, t);
        Scatter(WF + "Winter Rock 1.prefab", 10, -74f, 74f, 9f, 54f, 0.8f, 1.6f, t);
        Scatter(WF + "Winter Shrub 1.prefab", 12, -66f, 66f, 10f, 50f, 0.8f, 1.4f, t);
        Scatter(PROP + "PT_Wooden_Cross_01.prefab", 9, -50f, 50f, 13f, 40f, 1.0f, 1.5f, t);

        // Shadowhold (south courtyard): pines, rocks, a well + chests near the keep.
        Scatter(WF + "Winter Pine Tree 2.prefab", 10, -48f, 48f, -50f, -26f, 1.2f, 2.2f, t);
        Scatter(WF + "Winter Rock 2.prefab", 7, -44f, 44f, -50f, -25f, 0.8f, 1.5f, t);
        Decor(WF + "Water Well.prefab", new Vector3(-10f, 0f, -23f), Quaternion.identity, 1f, t, true);
        Decor(PROP + "PT_Chest_01.prefab", new Vector3(6f, 0f, -24f), Quaternion.Euler(0, -20, 0), 1f, t, true);

        // Weapons in hand.
        foreach (var n in new[] { "Guard_1", "Guard_2", "Guard_4", "Guard_6", "Guard_7" })
            AttachWeapon(n, WPN + "PT_Sword_01_a.prefab");
        foreach (var n in new[] { "Guard_3", "Guard_5" })
            AttachWeapon(n, WPN + "PT_Bow_01_a.prefab");

        sb.AppendLine("Decor dressed: towers + gate + mountain horizon + winter forest + crosses/well/chests + weapons");
    }

    static GameObject Decor(string path, Vector3 pos, Quaternion rot, float scale, Transform parent, bool stripColliders)
    {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (pf == null) { Debug.LogWarning("Decor missing: " + path); return null; }
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
        inst.transform.position = pos;
        inst.transform.rotation = rot;
        // MULTIPLY the prefab's native scale (some packs model tiny meshes + a big root
        // scale; overriding to an absolute value would shrink them to nothing).
        inst.transform.localScale = Vector3.Scale(inst.transform.localScale, new Vector3(scale, scale, scale));
        if (stripColliders)
            foreach (var c in inst.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
        MakeRenderersURP(inst);   // Polytope/castle shaders -> URP so they aren't magenta

        // Sit the base on the ground — many prefabs have a centred pivot and would sink at y=0.
        var rends = inst.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            inst.transform.position += new Vector3(0f, pos.y - b.min.y, 0f);
        }
        return inst;
    }

    static void Scatter(string path, int n, float xMin, float xMax, float zMin, float zMax, float sMin, float sMax, Transform parent)
    {
        for (int i = 0; i < n; i++)
        {
            float x = Random.Range(xMin, xMax);
            float z = Random.Range(zMin, zMax);
            if (Mathf.Abs(x) < 7f && z > -3f && z < 7f) continue;   // keep the gate corridor clear
            if (x > 44f && x < 56f) continue;                        // keep the tunnel clear
            if (Mathf.Abs(x) < 4f && z > -23f && z < -17f) continue; // keep the Watchfire clear
            Decor(path, new Vector3(x, 0f, z), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                Random.Range(sMin, sMax), parent, true);
        }
    }

    static void AttachWeapon(string agentName, string weaponPath)
    {
        var go = GameObject.Find(agentName);
        if (go == null) return;
        var anim = go.GetComponentInChildren<Animator>();
        if (anim == null || anim.avatar == null || !anim.avatar.isHuman) return;
        var hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
        if (hand == null) return;
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath);
        if (pf == null) return;
        var w = (GameObject)PrefabUtility.InstantiatePrefab(pf, hand);
        w.name = "Weapon";
        w.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        w.transform.localScale = Vector3.one;
        foreach (var c in w.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
        MakeRenderersURP(w);
    }

    // Polytope materials ship as built-in-pipeline shaders -> magenta under URP.
    // Rebuild each renderer's materials on URP/Lit, preserving the original texture.
    static void MakeRenderersURP(GameObject root)
    {
        var urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null) return;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            var outMats = new Material[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                var old = mats[i];
                // Already a URP material (e.g. winter forest, snow) -> leave it untouched.
                if (old != null && old.shader != null && old.shader.name.Contains("Universal Render Pipeline"))
                {
                    outMats[i] = old;
                    continue;
                }
                var m = new Material(urp);
                if (old != null)
                {
                    Texture tex = PickAlbedo(old);
                    if (tex != null) { m.mainTexture = tex; if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex); }
                }
                outMats[i] = m;
            }
            r.sharedMaterials = outMats;
        }
    }

    // Choose the albedo/base texture from a material, avoiding mask/normal/emblem maps.
    static Texture PickAlbedo(Material mat)
    {
        Texture fallback = null;
        // pass 1: name clearly says base/albedo/diffuse/color/main
        foreach (var pn in mat.GetTexturePropertyNames())
        {
            var t = mat.GetTexture(pn); if (t == null) continue;
            string s = (pn + " " + t.name).ToLower();
            if (s.Contains("base") || s.Contains("albedo") || s.Contains("diffuse") || s.Contains("color") || s.Contains("maintex"))
                return t;
        }
        // pass 2: anything that is NOT obviously a non-color map
        foreach (var pn in mat.GetTexturePropertyNames())
        {
            var t = mat.GetTexture(pn); if (t == null) continue;
            string s = (pn + " " + t.name).ToLower();
            if (s.Contains("mask") || s.Contains("normal") || s.Contains("bump") || s.Contains("coat") ||
                s.Contains("metal") || s.Contains("occl") || s.Contains("height") || s.Contains("rough") ||
                s.Contains("specular") || s.Contains("emiss") || s.Contains("_ao")) continue;
            if (fallback == null) fallback = t;
        }
        if (fallback != null) return fallback;
        // pass 3: anything at all
        foreach (var pn in mat.GetTexturePropertyNames())
        {
            var t = mat.GetTexture(pn); if (t != null) return t;
        }
        return null;
    }

    // Assign an idle/locomotion controller so the character is not stuck in T-pose.
    static void AssignController(GameObject inst, string prefabPath)
    {
        var anim = inst.GetComponentInChildren<Animator>();
        if (anim == null) return;
        string cp = prefabPath.Contains("Wolf") ? WOLF_CTRL : HUMAN_CTRL;
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(cp);
        if (ctrl != null) anim.runtimeAnimatorController = ctrl;
    }

    // Flat emissive disc under a unit's feet — faction at a glance without recoloring the mesh.
    static void AddTeamRing(GameObject parent, Color c)
    {
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "TeamRing";
        var col = ring.GetComponent<Collider>(); if (col) DestroyImmediate(col);
        ring.transform.SetParent(parent.transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        ring.transform.localScale = new Vector3(1.4f, 0.03f, 1.4f);
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = c; if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.6f); }
        ring.GetComponent<MeshRenderer>().sharedMaterial = m;
    }

    static void TintRenderers(GameObject root, Color tint)
    {
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
        {
            if (mr.sharedMaterial == null) continue;
            var mat = new Material(mr.sharedMaterial);
            mat.color = tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            mr.sharedMaterial = mat;
        }
        foreach (var sr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (sr.sharedMaterial == null) continue;
            var mat = new Material(sr.sharedMaterial);
            mat.color = tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            sr.sharedMaterial = mat;
        }
    }

    // -----------------------------------------------------------------------
    static void BuildMazeTunnel(StringBuilder sb)
    {
        var matFloor    = MakeColorMat(new Color(0.70f, 0.83f, 0.95f), 0.45f);  // icy floor
        var matTWall    = MakeColorMat(new Color(0.30f, 0.34f, 0.42f), 0.15f);  // stone walls/obstacles
        var matTDoor    = LoadMat(MAT + "Mat_TunnelDoor.mat");
        var matExit     = LoadMat(MAT + "Mat_TunnelExit.mat");
        var matSeeker   = LoadMat(MAT + "Mat_Viz_Seeker.mat");

        var tunnelRoot = new GameObject("BarrierTunnel");
        tunnelRoot.transform.position = new Vector3(50f, 0.05f, 0f);

        var gridGO = new GameObject("BarrierTunnel_Grid");
        gridGO.transform.SetParent(tunnelRoot.transform, false);

        var grid = gridGO.AddComponent<AStarGrid>();
        grid.gridWorldSize = new Vector2(10f, 24f);
        grid.nodeRadius = 0.5f;
        grid.obstacleLayer = 1 << 8;

        MakePrimMat(PrimitiveType.Quad, "TunnelFloor", tunnelRoot.transform,
            Vector3.zero, Quaternion.Euler(90f, 0f, 0f), new Vector3(10f, 24f, 1f), matFloor, 0, true);

        // E/W walls now have COLLIDERS so they contain the NavMesh corridor (sides).
        // N/S ends are left open as the north entrance / south exit of the route.
        MakePrimMat(PrimitiveType.Cube, "TunnelWall_W", tunnelRoot.transform, new Vector3(-5.3f, 1f, 0f), Quaternion.identity, new Vector3(0.4f, 2f, 24f), matTWall, 0, false);
        MakePrimMat(PrimitiveType.Cube, "TunnelWall_E", tunnelRoot.transform, new Vector3(5.3f, 1f, 0f), Quaternion.identity, new Vector3(0.4f, 2f, 24f), matTWall, 0, false);

        // Row 1 (top choke z=7) — obstacles meet door Y edges exactly, full seal when Y closed
        MakeObstacle("Maze_R1_A", tunnelRoot.transform, new Vector3(-3f, 0.5f, 7f), new Vector3(4f, 1f, 1.2f), matTWall);
        MakeObstacle("Maze_R1_B", tunnelRoot.transform, new Vector3(3f, 0.5f, 7f), new Vector3(4f, 1f, 1.2f), matTWall);

        // Row 2 (zig-zag z=2) — left/right gaps
        MakeObstacle("Maze_R2_A", tunnelRoot.transform, new Vector3(-4f, 0.5f, 2f), new Vector3(2f, 1f, 1.2f), matTWall);
        MakeObstacle("Maze_R2_B", tunnelRoot.transform, new Vector3(1f, 0.5f, 2f), new Vector3(4f, 1f, 1.2f), matTWall);

        // Row 3 (zig-zag z=-3) — opposite-offset gaps force zig-zag
        MakeObstacle("Maze_R3_A", tunnelRoot.transform, new Vector3(-1.5f, 0.5f, -3f), new Vector3(3f, 1f, 1.2f), matTWall);
        MakeObstacle("Maze_R3_B", tunnelRoot.transform, new Vector3(4f, 0.5f, -3f), new Vector3(2f, 1f, 1.2f), matTWall);

        // Row 4 (bottom choke z=-8) — obstacles meet door U edges exactly, full seal when U closed
        MakeObstacle("Maze_R4_A", tunnelRoot.transform, new Vector3(-3f, 0.5f, -8f), new Vector3(4f, 1f, 1.2f), matTWall);
        MakeObstacle("Maze_R4_B", tunnelRoot.transform, new Vector3(3f, 0.5f, -8f), new Vector3(4f, 1f, 1.2f), matTWall);

        // Door Y on row 1 (top, z=7) — opens middle gap
        var doorY = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorY.name = "TunnelDoor_Y"; doorY.layer = 8;
        doorY.transform.SetParent(tunnelRoot.transform, false);
        doorY.transform.localPosition = new Vector3(0f, 0.5f, 7f);
        doorY.transform.localScale = new Vector3(2f, 1f, 1.2f);
        ApplyMat(doorY, matTDoor);
        var gcY = doorY.AddComponent<GateController>();
        gcY.toggleKey = UnityEngine.InputSystem.Key.Y;
        gcY.openLiftHeight = 3f;
        gcY.startOpen = true;   // tunnel starts breachable; player seals it with Y

        // Door U on row 4 (bottom, z=-8) — opens middle gap
        var doorU = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorU.name = "TunnelDoor_U"; doorU.layer = 8;
        doorU.transform.SetParent(tunnelRoot.transform, false);
        doorU.transform.localPosition = new Vector3(0f, 0.5f, -8f);
        doorU.transform.localScale = new Vector3(2f, 1f, 1.2f);
        ApplyMat(doorU, matTDoor);
        var gcU = doorU.AddComponent<GateController>();
        gcU.toggleKey = UnityEngine.InputSystem.Key.U;
        gcU.openLiftHeight = 3f;
        gcU.startOpen = true;   // tunnel starts breachable; player seals it with U

        // Risen_2: dedicated A* agent for the labyrinth (Theme 3)
        var seeker = new GameObject("Risen_2");
        seeker.transform.SetParent(tunnelRoot.transform, false);
        seeker.transform.localPosition = new Vector3(0f, 0f, 10f);
        var humanPrefab = LoadPrefab(P_RISEN);
        if (humanPrefab != null)
        {
            var vis = (GameObject)PrefabUtility.InstantiatePrefab(humanPrefab, seeker.transform);
            vis.name = "Visual";
            vis.transform.localPosition = Vector3.zero;
            AssignController(vis, P_RISEN);
            MakeRenderersURP(vis);
            AddTeamRing(seeker, new Color(0.9f, 0.2f, 0.15f));
        }
        else
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "Visual";
            var col = fallback.GetComponent<Collider>(); if (col) DestroyImmediate(col);
            fallback.transform.SetParent(seeker.transform, false);
            fallback.transform.localPosition = new Vector3(0f, 1f, 0f);
            if (matSeeker != null) fallback.GetComponent<MeshRenderer>().sharedMaterial = matSeeker;
        }
        var walker = seeker.AddComponent<AStarWalker>();
        walker.grid = grid;
        walker.speed = 3f;
        walker.resetOnArrival = true;
        walker.resetDelay = 1.2f;

        AttachPerception(seeker, "Risen_2", sb);

        // Fixed spawn anchor: FindPath ALWAYS evaluates from this point, not walker's current pos.
        // This makes Y/U closures actually block path (no path exists if route impossible).
        var spawnAnchor = new GameObject("SeekerSpawn");
        spawnAnchor.transform.SetParent(tunnelRoot.transform, false);
        spawnAnchor.transform.localPosition = new Vector3(0f, 0.6f, 10f);

        var exit = MakePrimMat(PrimitiveType.Cylinder, "Tunnel_Exit", tunnelRoot.transform,
            new Vector3(0f, 0.1f, -10f), Quaternion.identity, new Vector3(1.4f, 0.1f, 1.4f), matExit, 0, true);

        // Picture-in-picture camera — toggled with T key
        var tunnelCamGO = GameObject.Find("TunnelPiPCamera");
        if (tunnelCamGO) DestroyImmediate(tunnelCamGO);
        tunnelCamGO = new GameObject("TunnelPiPCamera");
        tunnelCamGO.transform.SetParent(tunnelRoot.transform, false);
        tunnelCamGO.transform.localPosition = new Vector3(0f, 22f, 0f);
        tunnelCamGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var pipCam = tunnelCamGO.AddComponent<Camera>();
        pipCam.orthographic = true;
        pipCam.orthographicSize = 13f;
        pipCam.depth = 2f;
        pipCam.rect = new Rect(0.79f, 0.40f, 0.20f, 0.26f);   // mid-right, clear of the AGENT STATES panel
        pipCam.clearFlags = CameraClearFlags.SolidColor;
        pipCam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
        var toggle = tunnelCamGO.AddComponent<PiPCameraToggle>();
        toggle.toggleKey = UnityEngine.InputSystem.Key.T;
        toggle.startEnabled = false;

        var pf = gridGO.AddComponent<TunnelPathfinder>();
        pf.grid = grid;
        pf.seeker = seeker.transform;
        pf.target = exit.transform;
        pf.startAnchor = spawnAnchor.transform;
        pf.triggerKey = UnityEngine.InputSystem.Key.P;

        var autoPf = gridGO.AddComponent<TunnelPathfinderAuto>();
        autoPf.pathfinder = pf;
        autoPf.interval = 0.3f;

        var rend = gridGO.AddComponent<TunnelGridRenderer>();
        rend.tunnelLayer = 0;
        rend.yOffset = 0.08f;

        grid.BuildGrid();
        sb.AppendLine("Labyrinth at (50,0,0) — 10x24 grid, 4 sealed rows, spawn-anchored FindPath, auto-reset");
    }

    static void MakeObstacle(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name; g.layer = 8;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        ApplyMat(g, mat);
    }

    // -----------------------------------------------------------------------
    static GameObject LoadPrefab(string path)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (p == null) Debug.LogWarning("Prefab missing: " + path);
        return p;
    }

    static Material MakeColorMat(Color c, float smoothness)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    static Material LoadMat(string path)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) Debug.LogWarning("Material missing: " + path);
        return m;
    }

    static void ApplyMat(GameObject g, Material m)
    {
        var mr = g.GetComponent<MeshRenderer>();
        if (!mr || m == null) return;
        mr.sharedMaterial = m;
    }

    static void Nuke(string name, StringBuilder sb)
    {
        var go = GameObject.Find(name);
        if (go) { DestroyImmediate(go); sb.AppendLine("Removed " + name); }
    }

    static GameObject MakePrimMat(PrimitiveType t, string name, Transform parent,
        Vector3 pos, Quaternion rot, Vector3 scale, Material mat, int layer, bool removeCol)
    {
        var g = GameObject.CreatePrimitive(t);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = rot;
        g.transform.localScale = scale;
        if (removeCol) { var c = g.GetComponent<Collider>(); if (c) DestroyImmediate(c); }
        g.layer = layer;
        ApplyMat(g, mat);
        return g;
    }
}
