using UnityEngine;
using UnityEngine.InputSystem;

// Manual melee for the possessed guard. Left mouse button (or Space) swings a frontal
// arc; every attacker inside the arc takes damage. Only active while the player is
// actually driving this guard (ThirdPersonController in player mode).
[RequireComponent(typeof(Combatant))]
public class PlayerCombat : MonoBehaviour
{
    public float swingRange = 3.0f;
    public float swingAngle = 120f;     // full arc width in degrees
    public float swingDamage = 34f;
    public float swingCooldown = 0.45f;

    public float SwingFlash { get; private set; }   // >0 right after a swing (HUD feedback)

    Combatant self;
    ThirdPersonController tpc;
    float cooldown;

    void Awake()
    {
        self = GetComponent<Combatant>();
        tpc = GetComponent<ThirdPersonController>();
        self.autoAttack = false;        // the player chooses when to strike
    }

    void Update()
    {
        if (SwingFlash > 0f) SwingFlash -= Time.deltaTime;
        if (cooldown > 0f) cooldown -= Time.deltaTime;
        if (self == null || self.IsDead) return;

        // While the AI drives this guard it fights on its own (auto-melee). When the
        // player possesses it, auto-melee is off and the player swings manually.
        bool possessed = tpc != null && tpc.IsPlayer;
        self.autoAttack = !possessed;
        if (!possessed) return;

        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool pressed =
            (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
            (kb != null && kb[Key.Space].wasPressedThisFrame);

        if (pressed && cooldown <= 0f)
            Swing();
    }

    void Swing()
    {
        cooldown = swingCooldown;
        SwingFlash = 0.15f;

        float halfArc = swingAngle * 0.5f;
        float sqrRange = swingRange * swingRange;
        Vector3 fwd = transform.forward;

        var all = Combatant.All;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c == null || c.IsDead || !self.IsHostileTo(c)) continue;
            Vector3 to = c.transform.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude > sqrRange) continue;
            if (Vector3.Angle(fwd, to) > halfArc) continue;
            c.ApplyDamage(swingDamage, self);
        }

        self.TriggerAttackFlash();                   // visible forward jab
        SoundEvent.Emit(transform.position, 35f);   // the clash draws attention (perception)
    }
}
