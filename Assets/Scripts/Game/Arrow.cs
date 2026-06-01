using UnityEngine;

// Simple homing arrow: flies toward the target's chest, applies damage on arrival,
// despawns on hit or after its lifetime. Kept minimal and robust (no physics needed).
public class Arrow : MonoBehaviour
{
    Combatant target;
    Combatant source;
    float damage;
    float speed;
    float life = 4f;

    public void Init(Combatant t, float dmg, float spd, Combatant src)
    {
        target = t; damage = dmg; speed = spd; source = src;
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f || target == null || target.IsDead) { Destroy(gameObject); return; }

        Vector3 aim = target.transform.position + Vector3.up * 1.0f;
        Vector3 to = aim - transform.position;
        float dist = to.magnitude;

        if (dist < 0.6f)
        {
            target.ApplyDamage(damage, source);
            Destroy(gameObject);
            return;
        }

        Vector3 dir = to / dist;
        transform.position += dir * speed * Time.deltaTime;
        // cylinder's long axis is Y, so align Y to the flight direction
        transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);
    }
}
