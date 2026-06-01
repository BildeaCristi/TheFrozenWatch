using UnityEngine;
using UnityEngine.AI;

// Temporary stat boost applied by the War Horn. Caches the base values once, refreshes
// the timer if re-applied, and restores cleanly when it expires.
public class Buff : MonoBehaviour
{
    Combatant c;
    NavMeshAgent agent;
    float baseInterval, baseSpeed, timer;
    bool active;

    void Awake()
    {
        c = GetComponent<Combatant>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void Apply(float duration, float speedMul, float intervalMul)
    {
        if (!active)
        {
            if (c != null) baseInterval = c.attackInterval;
            if (agent != null) baseSpeed = agent.speed;
            active = true;
        }
        if (c != null) c.attackInterval = baseInterval * intervalMul;
        if (agent != null) agent.speed = baseSpeed * speedMul;
        timer = duration;
    }

    void Update()
    {
        if (!active) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) Restore();
    }

    void Restore()
    {
        if (c != null) c.attackInterval = baseInterval;
        if (agent != null) agent.speed = baseSpeed;
        active = false;
        Destroy(this);
    }
}
