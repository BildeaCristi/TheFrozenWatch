using UnityEngine;
using UnityEngine.InputSystem;

// ESC -> pause + settings overlay. Freezes time, exposes the debug-visualisation
// toggles and restart/quit. Also owns the global "show controls" flag the HUD reads.
public class PauseMenu : MonoBehaviour
{
    public static bool ShowControls = true;

    public Key pauseKey = Key.Escape;
    public Key controlsKey = Key.F1;

    bool paused;
    GUIStyle panel, header, item, btn;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[pauseKey].wasPressedThisFrame) TogglePause();
        if (kb[controlsKey].wasPressedThisFrame) ShowControls = !ShowControls;
    }

    void TogglePause()
    {
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    void OnGUI()
    {
        if (!paused) return;
        EnsureStyles();

        // dim the whole screen
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        float w = 440f, h = 440f;
        Rect r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        // solid border + panel (drawn explicitly so it never renders see-through)
        GUI.color = new Color(0.35f, 0.5f, 0.75f, 1f);
        GUI.DrawTexture(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), Texture2D.whiteTexture);
        GUI.color = new Color(0.07f, 0.09f, 0.14f, 1f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(r.x + 24, r.y + 20, w - 48, h - 40));
        GUILayout.Label("PAUSED  —  THE FROZEN WATCH", header);
        GUILayout.Space(10);

        if (GUILayout.Button("Resume  (Esc)", btn)) TogglePause();
        if (GUILayout.Button("Restart Siege  (R)", btn) && GameDirector.Instance != null)
        {
            GameDirector.Instance.Restart();
            TogglePause();
        }

        GUILayout.Space(8);
        GUILayout.Label("SETTINGS", header);
        bool v = GUILayout.Toggle(FieldOfView.drawRuntime, "  Show vision cones (V)", item);
        FieldOfView.drawRuntime = v;
        bool hh = GUILayout.Toggle(HearingSensor.drawRuntime, "  Show hearing radius (H)", item);
        HearingSensor.drawRuntime = hh;
        ShowControls = GUILayout.Toggle(ShowControls, "  Show controls panel (F1)", item);

        GUILayout.Space(8);
        GUILayout.Label("CONTROLS", header);
        GUILayout.Label("F  Possess / release guard    WASD  Move", item);
        GUILayout.Label("Shift  Run        Mouse  Look", item);
        GUILayout.Label("LMB / Space  Attack", item);
        GUILayout.Label("G  Main gate      Y / U  Seal tunnel doors", item);
        GUILayout.Label("Q  War Horn (rally)   T  Tunnel cam", item);

        GUILayout.Space(10);
        if (GUILayout.Button("Quit to Desktop", btn))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        GUILayout.EndArea();
    }

    void EnsureStyles()
    {
        if (panel != null) return;
        var bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.12f, 0.97f)); bg.Apply();
        panel = new GUIStyle(GUI.skin.box) { border = new RectOffset(2, 2, 2, 2) };
        panel.normal.background = bg;
        header = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.8f, 0.9f, 1f) } };
        item = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Color.white }, onNormal = { textColor = Color.white } };
        btn = new GUIStyle(GUI.skin.button) { fontSize = 14, fixedHeight = 34 };
    }
}
