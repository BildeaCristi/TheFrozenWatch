using UnityEngine;

// The objective the attackers want to extinguish. It is a stationary, neutral-attack
// Defender Combatant with a big health pool; attackers damage it via their auto-melee
// when adjacent. When it dies, the game is lost.
[RequireComponent(typeof(Combatant))]
public class Watchfire : MonoBehaviour
{
    public static Watchfire Instance { get; private set; }

    public Combatant Core { get; private set; }
    public Light flame;
    public float flickerSpeed = 6f;
    public float baseIntensity = 3.5f;

    float baseScale;
    Transform flameVisual;

    void Awake()
    {
        Instance = this;
        Core = GetComponent<Combatant>();
        Core.team = Combatant.Team.Defender;
        Core.isStructure = true;
        Core.autoAttack = false;       // the fire does not strike back
        Core.attackDamage = 0f;
    }

    void Start()
    {
        var t = transform.Find("Flame");
        if (t != null) { flameVisual = t; baseScale = t.localScale.y; }
    }

    void Update()
    {
        float f = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);

        // dim as the fire dies
        float hp = Core != null ? Core.HealthFraction : 1f;

        if (flame != null) flame.intensity = baseIntensity * f * Mathf.Lerp(0.2f, 1f, hp);
        if (flameVisual != null)
            flameVisual.localScale = new Vector3(
                flameVisual.localScale.x,
                baseScale * f * Mathf.Lerp(0.3f, 1f, hp),
                flameVisual.localScale.z);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
