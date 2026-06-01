using UnityEngine;
using UnityEngine.AI;

// Goal-directed driver for wave attackers: march on the Watchfire, but peel off to
// engage any defender that gets in the way. Combatant handles the actual damage; this
// only steers the NavMeshAgent. Kept separate from the FSM/Utility demo agents so the
// playable loop is robust while Risen_1/Risen_3 keep showcasing the AI techniques.
[RequireComponent(typeof(NavMeshAgent))]
public class SiegeAttacker : MonoBehaviour
{
    public Transform objective;          // the Watchfire
    public float aggroRadius = 4.5f;     // only peel off for a defender right in the way; else push the objective
    public float repathInterval = 0.35f;
    public float fleeHealthFrac = 0.22f; // tactical retreat (Theme 6.3) when badly wounded

    NavMeshAgent agent;
    Combatant self;
    float timer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        self = GetComponent<Combatant>();
    }

    void Update()
    {
        if (self != null && self.IsDead) return;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = repathInterval;

        // Theme 6.3 tactical retreat: when badly wounded, flee from the nearest defender.
        if (self != null && self.HealthFraction < fleeHealthFrac)
        {
            Combatant threat = NearestDefender(18f);
            if (threat != null)
            {
                Vector3 away = (transform.position - threat.transform.position).normalized;
                agent.isStopped = false;
                agent.SetDestination(transform.position + away * 10f);
                return;
            }
        }

        Transform target = objective;

        Combatant def = NearestDefender(aggroRadius);
        if (def != null) target = def.transform;

        if (target != null)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
    }

    Combatant NearestDefender(float radius)
    {
        Combatant best = null;
        float bestSqr = radius * radius;
        var all = Combatant.All;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c == null || c.IsDead || c.isStructure) continue;
            if (c.team != Combatant.Team.Defender) continue;
            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d <= bestSqr) { bestSqr = d; best = c; }
        }
        return best;
    }
}
