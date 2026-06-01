using UnityEngine;

// Ranged attacker: instead of Combatant's instant melee, it looses a visible arrow
// projectile at the nearest hostile within range. Used by the wall guard so the tower
// actually does something you can watch.
[RequireComponent(typeof(Combatant))]
public class Archer : MonoBehaviour
{
    public float range = 18f;
    public float damage = 12f;
    public float cooldown = 1.2f;
    public float arrowSpeed = 34f;
    public float bowHeight = 1.6f;

    Combatant self;
    float timer;

    void Awake()
    {
        self = GetComponent<Combatant>();
        if (self != null) self.autoAttack = false;   // the arrow deals the damage, not auto-melee
    }

    void Update()
    {
        if (self == null || self.IsDead) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        var target = NearestHostile();
        if (target == null) return;

        timer = cooldown;
        Fire(target);
        self.TriggerAttackFlash();
    }

    Combatant NearestHostile()
    {
        Combatant best = null;
        float bestSqr = range * range;
        var all = Combatant.All;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c == null || c.IsDead || !self.IsHostileTo(c)) continue;
            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d <= bestSqr) { bestSqr = d; best = c; }
        }
        return best;
    }

    void Fire(Combatant target)
    {
        Vector3 origin = transform.position + Vector3.up * bowHeight;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Arrow";
        var col = go.GetComponent<Collider>(); if (col) Destroy(col);
        go.transform.position = origin;
        go.transform.localScale = new Vector3(0.05f, 0.45f, 0.05f);
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var wood = new Color(0.32f, 0.22f, 0.1f);
        m.color = wood; if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", wood);
        go.GetComponent<MeshRenderer>().sharedMaterial = m;

        go.AddComponent<Arrow>().Init(target, damage, arrowSpeed, self);

        // face the shot
        Vector3 flat = target.transform.position - transform.position; flat.y = 0f;
        if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(flat);
    }
}
