using UnityEngine;
using UnityEngine.InputSystem;

// Paged story / tutorial overlay shown at the start of a siege. Freezes time until the
// player reads the lore, the objective (how to win) and the controls, then begins.
// Re-openable with F2.
public class StoryIntro : MonoBehaviour
{
    [TextArea] public string[] titles = {
        "THE FROZEN WATCH",
        "YOUR MISSION",
        "HOW TO PLAY",
    };

    [TextArea] public string[] bodies = {
        "You are ALDRIC FROST, commander of the Frozen Watch.\n\n" +
        "Beyond the Great Barrier, the undead RISEN gather under the Winter Lord. " +
        "They march to extinguish the WATCHFIRE — the heart of the keep — and overrun the south.\n\n" +
        "Hold the wall. Keep the fire alight.",

        "Defend the WATCHFIRE through 3 waves of attackers.\n\n" +
        "The enemy strikes on TWO routes:\n" +
        "   •  the MAIN GATE  — a frontal assault (NavMesh)\n" +
        "   •  the TUNNEL  — a flanking breach guided by A* pathfinding\n\n" +
        "WIN: survive all 3 waves with the Watchfire still burning.\n" +
        "LOSE: the Watchfire is extinguished.\n\n" +
        "Your guards defend in two squads (gate + watchfire). Help where the line is thin.",

        "F        possess / release a guard\n" +
        "WASD   move      SHIFT  run      MOUSE  look\n" +
        "LMB / SPACE   attack\n" +
        "G        open / close the main gate\n" +
        "Y / U   seal the tunnel doors (block the A* breach)\n" +
        "Q        war horn — rally your guards\n" +
        "ESC     menu        R   restart        F2  this screen\n\n" +
        "Press  BEGIN  to raise the watch.",
    };

    public Key advanceKey = Key.Enter;
    public Key reopenKey = Key.F2;

    int page;
    bool showing;
    float prevTimeScale = 1f;
    GUIStyle panel, title, body, btn, hint;

    void Start() { Open(); }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (!showing && kb[reopenKey].wasPressedThisFrame) Open();
        else if (showing && kb[advanceKey].wasPressedThisFrame) Advance();
    }

    void Open()
    {
        showing = true;
        page = 0;
        prevTimeScale = Time.timeScale == 0f ? 1f : Time.timeScale;
        Time.timeScale = 0f;
    }

    void Advance()
    {
        page++;
        if (page >= titles.Length) Close();
    }

    void Close()
    {
        showing = false;
        Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
    }

    void OnGUI()
    {
        if (!showing) return;
        EnsureStyles();

        // dim
        GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = 720f, h = 460f;
        Rect r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.color = new Color(0.55f, 0.7f, 0.95f, 1f);
        GUI.DrawTexture(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Texture2D.whiteTexture);
        GUI.color = new Color(0.06f, 0.09f, 0.15f, 1f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;

        int i = Mathf.Clamp(page, 0, titles.Length - 1);
        GUILayout.BeginArea(new Rect(r.x + 34, r.y + 28, w - 68, h - 56));
        GUILayout.Label(titles[i], title);
        GUILayout.Space(14);
        GUILayout.Label(bodies[i], body);
        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUILayout.Label("page " + (i + 1) + " / " + titles.Length, hint);
        GUILayout.FlexibleSpace();
        string label = (i >= titles.Length - 1) ? "BEGIN  (Enter)" : "NEXT  (Enter)";
        if (GUILayout.Button(label, btn, GUILayout.Width(200), GUILayout.Height(40))) Advance();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void EnsureStyles()
    {
        if (panel != null) return;
        panel = new GUIStyle();
        title = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.85f, 0.92f, 1f) } };
        body = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true, wordWrap = true,
            normal = { textColor = new Color(0.9f, 0.92f, 0.96f) } };
        btn = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        hint = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.6f, 0.65f, 0.75f) } };
    }
}
