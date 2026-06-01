using UnityEngine;
using UnityEngine.InputSystem;

public class SceneHUD : MonoBehaviour
{
    public Transform guard;
    public Transform risen;
    public Transform frostWolf;
    public Camera hudCamera;

    public float hornLoudness = 60f;

    Transform guard2, guard3, risen3;

    GUIStyle box, label, title, blue, red, white, green, alertStyle, stateStyle, banner, barText;

    void Start()
    {
        if (guard == null)     { var go = GameObject.Find("Guard_1");     if (go != null) guard = go.transform; }
        if (risen == null)     { var go = GameObject.Find("Risen_1");     if (go != null) risen = go.transform; }
        if (frostWolf == null) { var go = GameObject.Find("FrostWolf_1"); if (go != null) frostWolf = go.transform; }
        if (hudCamera == null) hudCamera = Camera.main;

        var g2 = GameObject.Find("Guard_2"); if (g2 != null) guard2 = g2.transform;
        var g3 = GameObject.Find("Guard_3"); if (g3 != null) guard3 = g3.transform;
        var r3 = GameObject.Find("Risen_3"); if (r3 != null) risen3 = r3.transform;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[Key.Q].wasPressedThisFrame)
        {
            Vector3 pos = guard != null ? guard.position : transform.position;
            SoundEvent.Emit(pos, hornLoudness);
            Debug.Log("[HORN] Alarm raised near " + pos);
        }
        if (kb[Key.V].wasPressedThisFrame)
            FieldOfView.drawRuntime = !FieldOfView.drawRuntime;
        if (kb[Key.H].wasPressedThisFrame)
            HearingSensor.drawRuntime = !HearingSensor.drawRuntime;
    }

    void OnGUI()
    {
        EnsureStyles();

        // Top-center: objective / wave status + Watchfire health
        DrawObjective();

        // Floating health bars over every fighter (makes combat readable)
        DrawUnitBars();

        // Top-left: controls (toggle with F1)
        if (PauseMenu.ShowControls)
        {
            GUI.Box(new Rect(10, 10, 340, 360), "", box);
            GUILayout.BeginArea(new Rect(20, 18, 320, 350));
            GUILayout.Label("THE FROZEN WATCH", title);
            GUILayout.Space(4);
            GUILayout.Label("COMMAND", title);
            GUILayout.Label("F        Possess / release guard", label);
            GUILayout.Label("WASD     Move    Shift  Run", label);
            GUILayout.Label("Mouse    Look    LMB/Space  Attack", label);
            GUILayout.Label("G        Open/Close Main Gate", label);
            GUILayout.Label("Q        War Horn (rally buff)", label);
            GUILayout.Space(4);
            GUILayout.Label("TUNNEL (A*)", title);
            GUILayout.Label("T  Tunnel cam   Y/U  Doors   P  Path", label);
            GUILayout.Space(4);
            GUILayout.Label("DEBUG", title);
            GUILayout.Label("V  Vision cones   H  Hearing radius", label);
            GUILayout.Label("F1  Hide this panel   Esc  Menu", label);
            GUILayout.EndArea();
        }
        else
        {
            GUI.Label(new Rect(12, 10, 260, 20), "F1 Controls   Esc Menu", label);
        }

        // Bottom-left: legend
        GUI.Box(new Rect(10, Screen.height - 110, 320, 100), "", box);
        GUILayout.BeginArea(new Rect(20, Screen.height - 102, 300, 90));
        GUILayout.Label("AGENTS", title);
        GUILayout.Label("BLUE = Guards (FSM: Patrol/Chase/Attack)", blue);
        GUILayout.Label("RED = Risen (FSM + Utility AI)", red);
        GUILayout.Label("WHITE = FrostWolf (Behaviour Tree)", white);
        GUILayout.EndArea();

        // Bottom-right: tunnel legend
        GUI.Box(new Rect(Screen.width - 230, Screen.height - 110, 220, 100), "", box);
        GUILayout.BeginArea(new Rect(Screen.width - 220, Screen.height - 102, 200, 90));
        GUILayout.Label("A* TUNNEL", title);
        GUILayout.Label("Green = path", green);
        GUILayout.Label("Red = explored (closed set)", red);
        GUILayout.Label("Doors: Y / U", red);
        GUILayout.EndArea();

        // Top-right: perception + decision status
        GUI.Box(new Rect(Screen.width - 250, 10, 240, 210), "", box);
        GUILayout.BeginArea(new Rect(Screen.width - 242, 18, 228, 200));
        GUILayout.Label("AGENT STATES", title);
        GUILayout.Label("Guard_1:  " + FSMStateName(guard)   + "  (" + SpeedOf(guard).ToString("F1") + ")", label);
        GUILayout.Label("Guard_2:  " + FSMStateName(guard2)  + "  (" + SpeedOf(guard2).ToString("F1") + ")", label);
        GUILayout.Label("Guard_3:  " + FSMStateName(guard3)  + "  (" + SpeedOf(guard3).ToString("F1") + ")", label);
        GUILayout.Label("Wolf:     " + WolfBTAction(frostWolf) + " (" + SpeedOf(frostWolf).ToString("F1") + ")", label);
        GUILayout.Label("Risen_1:  " + RisenState(risen)  + " | " + UtilityActionName(risen),  label);
        GUILayout.Label("Risen_3:  " + RisenState(risen3) + " | " + UtilityActionName(risen3), label);
        GUILayout.Space(4);
        GUILayout.Label("Vision:  " + (FieldOfView.drawRuntime  ? "ON" : "OFF"), label);
        GUILayout.Label("Hearing: " + (HearingSensor.drawRuntime ? "ON" : "OFF"), label);
        GUILayout.EndArea();

        // Floating labels + ALERT badges
        DrawWorldLabel(guard,     "G1",  blue);
        DrawWorldLabel(guard2,    "G2",  blue);
        DrawWorldLabel(guard3,    "G3",  blue);
        DrawWorldLabel(risen,     "R1",  red);
        DrawWorldLabel(risen3,    "R3",  red);
        DrawWorldLabel(frostWolf, "WOLF", white);

        DrawAlertBadge(guard);
        DrawAlertBadge(guard2);
        DrawAlertBadge(guard3);
        DrawAlertBadge(frostWolf);
    }

    string FSMStateName(Transform t)
    {
        if (t == null) return "N/A";
        var fsm = t.GetComponent<GuardFSM>();
        if (fsm == null) return "---";
        return fsm.InSquadMode ? fsm.StateName + "[S]" : fsm.StateName;
    }

    string WolfBTAction(Transform t)
    {
        if (t == null) return "N/A";
        var bt = t.GetComponent<WolfBT>();
        return bt != null ? bt.LastAction : "---";
    }

    string RisenState(Transform t)
    {
        if (t == null) return "N/A";
        var fsm = t.GetComponent<RisenFSM>();
        return fsm != null ? fsm.StateName : "---";
    }

    string UtilityActionName(Transform t)
    {
        if (t == null) return "---";
        var brain = t.GetComponent<UtilityBrain>();
        return brain != null ? brain.ActionName : "---";
    }

    float SpeedOf(Transform t)
    {
        if (t == null) return 0f;
        var ab = t.GetComponent<AnimatorBridge>();
        return ab != null ? ab.CurrentSpeed : 0f;
    }

    void DrawAlertBadge(Transform t)
    {
        if (t == null || AlertSystem.Instance == null) return;
        if (!AlertSystem.Instance.IsAlerted(t.gameObject)) return;
        var cam = hudCamera != null ? hudCamera : Camera.main;
        if (cam == null) return;
        Vector3 sp = cam.WorldToScreenPoint(t.position + Vector3.up * 3.0f);
        if (sp.z <= 0f) return;
        GUI.Label(new Rect(sp.x - 40f, Screen.height - sp.y - 12f, 80f, 22f), "ALERT", alertStyle);
    }

    void DrawWorldLabel(Transform t, string text, GUIStyle style)
    {
        if (t == null) return;
        var cam = hudCamera != null ? hudCamera : Camera.main;
        if (cam == null) return;
        Vector3 sp = cam.WorldToScreenPoint(t.position + Vector3.up * 2.2f);
        if (sp.z <= 0f) return;
        GUI.Label(new Rect(sp.x - 50f, Screen.height - sp.y - 12f, 100f, 20f), text, style);
    }

    void DrawObjective()
    {
        var gd = GameDirector.Instance;
        float cx = Screen.width * 0.5f;
        float w = 540f;

        GUI.Box(new Rect(cx - w * 0.5f, 8, w, 60), "", box);
        string status = gd != null ? gd.PhaseLabel() : "THE FROZEN WATCH";
        GUI.Label(new Rect(cx - w * 0.5f + 12, 12, w - 24, 22), status, title);

        float hp = (Watchfire.Instance != null && Watchfire.Instance.Core != null)
            ? Watchfire.Instance.Core.HealthFraction : 1f;
        DrawBar(new Rect(cx - w * 0.5f + 12, 38, w - 24, 18), hp, new Color(1f, 0.55f, 0.1f), "WATCHFIRE");

        // Win / lose banner
        if (gd != null && (gd.CurrentPhase == GameDirector.Phase.Won || gd.CurrentPhase == GameDirector.Phase.Lost))
        {
            bool won = gd.CurrentPhase == GameDirector.Phase.Won;
            float bw = 680f, bh = 130f, by = Screen.height * 0.30f;
            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.DrawTexture(new Rect(cx - bw * 0.5f, by, bw, bh), Texture2D.whiteTexture);
            GUI.color = Color.white;
            banner.normal.textColor = won ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f);
            GUI.Label(new Rect(cx - bw * 0.5f, by + 16, bw, 60), won ? "VICTORY" : "DEFEAT", banner);
            GUI.Label(new Rect(cx - bw * 0.5f, by + 86, bw, 30), "press  R  to restart the siege", title);
        }

        // Player health, bottom-center
        if (gd != null && gd.player != null)
        {
            float php = gd.PlayerDown ? 0f : gd.player.HealthFraction;
            DrawBar(new Rect(cx - 130, Screen.height - 34, 260, 18), php,
                new Color(0.4f, 0.8f, 1f), gd.PlayerDown ? "GUARD DOWN" : "YOUR GUARD");
        }
    }

    void DrawUnitBars()
    {
        var cam = hudCamera != null ? hudCamera : Camera.main;
        if (cam == null) return;
        var all = Combatant.All;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c == null || c.IsDead || c.isStructure) continue;
            Vector3 sp = cam.WorldToScreenPoint(c.transform.position + Vector3.up * 2.4f);
            if (sp.z <= 0f) continue;

            float w = 46f, h = 6f;
            float x = sp.x - w * 0.5f, y = Screen.height - sp.y;
            Color fill = c.team == Combatant.Team.Attacker
                ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.35f, 0.8f, 1f);

            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, h + 2), Texture2D.whiteTexture);
            GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(x, y, w * c.HealthFraction, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x - 20, y - 15, w + 40, 14),
                Mathf.CeilToInt(c.health) + " / " + Mathf.CeilToInt(c.maxHealth), barText);

            if (c.AttackFlash > 0f)   // yellow strip the instant this unit lands a hit
            {
                GUI.color = new Color(1f, 0.95f, 0.4f, Mathf.Clamp01(c.AttackFlash * 5f));
                GUI.DrawTexture(new Rect(x - 2, y - 4, w + 4, 2f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }
    }

    void DrawBar(Rect r, float frac, Color fill, string lbl)
    {
        GUI.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(frac), r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(r, "  " + lbl + "   " + Mathf.RoundToInt(Mathf.Clamp01(frac) * 100f) + "%", label);
    }

    void EnsureStyles()
    {
        if (box != null) return;
        box = new GUIStyle(GUI.skin.box);
        var bg = new Texture2D(1, 1); bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.72f)); bg.Apply();
        box.normal.background = bg;
        label = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = Color.white } };
        title = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.85f, 0.9f, 1f) } };
        blue  = new GUIStyle(label) { normal = { textColor = new Color(0.55f, 0.75f, 1f) } };
        red   = new GUIStyle(label) { normal = { textColor = new Color(1f, 0.45f, 0.45f) } };
        white = new GUIStyle(label) { normal = { textColor = Color.white } };
        green = new GUIStyle(label) { normal = { textColor = new Color(0.3f, 1f, 0.45f) } };
        alertStyle = new GUIStyle(label) {
            fontSize = 14, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.25f, 0.25f) }
        };
        stateStyle = new GUIStyle(label) { normal = { textColor = new Color(1f, 0.85f, 0.2f) } };
        banner = new GUIStyle(GUI.skin.label) {
            fontSize = 46, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        barText = new GUIStyle(GUI.skin.label) {
            fontSize = 10, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
    }
}
