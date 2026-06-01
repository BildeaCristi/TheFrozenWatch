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
        "Night falls, commander.\n\n" +
        "You are ALDRIC FROST, last warden of the Frozen Watch. " +
        "Past the Great Barrier, the dead are walking. The RISEN answer the Winter Lord, " +
        "and they want one thing: the WATCHFIRE.\n\n" +
        "Let that flame die and the whole south freezes with it.\n\n" +
        "Hold the wall. Keep the fire burning.",

        "Three waves are coming. Stand through all of them.\n\n" +
        "They hit you from two sides at once:\n" +
        "   •  the MAIN GATE, a straight charge into your spears\n" +
        "   •  the TUNNEL, a sneak run that worms in from the flank\n\n" +
        "WIN  if the Watchfire is still lit when the third wave breaks.\n" +
        "LOSE  the moment that fire goes dark.\n\n" +
        "Two squads hold the line for you, one at the gate, one at the fire. " +
        "Wherever it bends, that is where you go.",

        "F        jump into a guard, press again to step out\n" +
        "WASD   move      SHIFT  sprint      MOUSE  look\n" +
        "LMB / SPACE   swing\n" +
        "G        raise or drop the main gate\n" +
        "Y / U   slam the tunnel doors shut\n" +
        "Q        sound the war horn and rally the watch\n" +
        "ESC     menu        R   start over        F2  read this again\n\n" +
        "Press  BEGIN  and raise the watch.",
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
