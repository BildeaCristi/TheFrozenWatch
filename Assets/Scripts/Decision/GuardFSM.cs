using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class GuardFSM : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack, Retreat }

    [Header("Thresholds")]
    public float attackRange = 2.5f;
    public float chaseRecoverRange = 3.5f;
    public float lostTargetTimeout = 5f;

    [Header("Offence")]
    public bool canSortie = true;        // ground guards charge attackers; wall lookout does not
    public float engageRadius = 24f;

    [Header("Zone defence (leash)")]
    public bool useCustomHome = false;   // set by the scene builder for the Watchfire squad
    public Vector3 homePoint;            // centre of this guard's defended zone
    public float leashRadius = 40f;      // only engages attackers inside this radius of home

    [Header("Self-preservation (Theme 6.3)")]
    public float retreatHealthFrac = 0.3f;   // fall back below this
    public float recoverHealthFrac = 0.7f;   // rejoin the fight above this
    public float healRate = 10f;             // HP/s regained while regrouping

    public State CurrentState => currentState;
    public string StateName => currentState.ToString();
    public bool InSquadMode => tacSlot.HasValue;

    Vector3? tacSlot;

    State currentState = State.Patrol;
    NavMeshAgent agent;
    AgentMover mover;
    FieldOfView fov;
    AlertSystem alert;
    AgentBlackboard bb;
    Combatant combat;
    Transform target;
    float lostTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        mover = GetComponent<AgentMover>();
        fov = GetComponent<FieldOfView>();
        alert = AlertSystem.Instance;
        bb = GetComponent<AgentBlackboard>();
        combat = GetComponent<Combatant>();
        if (!useCustomHome) homePoint = transform.position;
        currentState = State.Patrol;
    }

    void Update()
    {
        // Theme 6.3: when badly wounded, break off and regroup at the Watchfire.
        if (currentState != State.Retreat && combat != null && !combat.IsDead
            && combat.HealthFraction < retreatHealthFrac)
        {
            ChangeState(State.Retreat);
        }

        switch (currentState)
        {
            case State.Patrol:  UpdatePatrol();  break;
            case State.Chase:   UpdateChase();   break;
            case State.Attack:  UpdateAttack();  break;
            case State.Retreat: UpdateRetreat(); break;
        }
    }

    void UpdatePatrol()
    {
        if (mover != null) mover.enabled = true;
        if (agent != null) agent.isStopped = false;

        if (fov != null && fov.visibleTargets.Count > 0)
        {
            target = fov.visibleTargets[0];
            if (bb != null) bb.lastThreatPos = target;
            ChangeState(State.Chase);
            return;
        }

        // Sortie: actively hunt the highest-priority attacker within engage range (Theme 6.4).
        if (canSortie)
        {
            Transform atk = FindBestAttacker(engageRadius);
            if (atk != null)
            {
                target = atk;
                if (bb != null) bb.lastThreatPos = atk;
                ChangeState(State.Chase);
                return;
            }
        }

        if (alert != null && alert.IsAlerted(gameObject))
            ChangeState(State.Chase);
    }

    // Theme 6.4 (applied to guards): utility score favours attackers closest to the
    // Watchfire (most urgent) and closest to this guard (easiest to reach).
    Transform FindBestAttacker(float radius)
    {
        Transform best = null;
        float bestScore = -1f;
        Vector3 wf = Watchfire.Instance != null ? Watchfire.Instance.transform.position : transform.position;
        var all = Combatant.All;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c == null || c.IsDead || c.team != Combatant.Team.Attacker) continue;
            // Only defend our own zone: ignore attackers outside the leash radius of home.
            if (Vector3.Distance(c.transform.position, homePoint) > leashRadius) continue;
            float dg = Vector3.Distance(c.transform.position, transform.position);
            float dw = Vector3.Distance(c.transform.position, wf);
            float score = 0.6f * (1f - Mathf.Clamp01(dw / 60f))
                        + 0.4f * (1f - Mathf.Clamp01(dg / Mathf.Max(1f, leashRadius)));
            if (score > bestScore) { bestScore = score; best = c.transform; }
        }
        return best;
    }

    void UpdateChase()
    {
        if (mover != null) mover.enabled = false;

        if (fov != null && fov.visibleTargets.Count > 0)
        {
            target = fov.visibleTargets[0];
            if (bb != null) bb.lastThreatPos = target;
            lostTimer = 0f;
        }
        else
        {
            Transform atk = canSortie ? FindBestAttacker(engageRadius) : null;
            if (atk != null)
            {
                target = atk;
                if (bb != null) bb.lastThreatPos = atk;
                lostTimer = 0f;
            }
            else
            {
                lostTimer += Time.deltaTime;
                if (lostTimer >= lostTargetTimeout)
                {
                    target = null;
                    ChangeState(State.Patrol);
                    return;
                }
            }
        }

        // Don't get dragged out of our zone — drop a target that runs too far from home.
        if (target != null && Vector3.Distance(target.position, homePoint) > leashRadius * 1.4f)
        {
            target = null;
            ChangeState(State.Patrol);
            return;
        }

        if (target != null && agent != null)
        {
            agent.isStopped = false;
            Vector3 dest = tacSlot.HasValue ? tacSlot.Value : target.position;
            agent.SetDestination(dest);
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist < attackRange)
                ChangeState(State.Attack);
        }
        else if (alert != null && alert.IsAlerted(gameObject) && agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(alert.GetSource(gameObject));
        }
    }

    void UpdateAttack()
    {
        if (agent != null) agent.isStopped = true;

        if (target != null)
        {
            transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
            if (bb != null) bb.isUnderFire = true;

            SoundEvent.Emit(transform.position, 40f);

            float dist = Vector3.Distance(transform.position, target.position);
            if (dist > chaseRecoverRange)
                ChangeState(State.Chase);
        }
        else
        {
            ChangeState(State.Patrol);
        }
    }

    // Theme 6.3: fall back to the Watchfire and recover, then rejoin the fight.
    void UpdateRetreat()
    {
        if (mover != null) mover.enabled = false;

        Vector3 rally = Watchfire.Instance != null
            ? Watchfire.Instance.transform.position
            : transform.position;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(rally);
        }

        if (combat != null)
        {
            combat.health = Mathf.Min(combat.maxHealth, combat.health + healRate * Time.deltaTime);
            if (combat.HealthFraction >= recoverHealthFrac)
            {
                target = null;
                ChangeState(State.Patrol);
            }
        }
    }

    void ChangeState(State next)
    {
        if (next == currentState) return;
        currentState = next;
        lostTimer = 0f;
        if (bb != null) bb.isUnderFire = (next == State.Attack);
    }

    public void SetTacticalSlot(Vector3? slot) { tacSlot = slot; }

    void OnDrawGizmos()
    {
        Color c;
        if      (currentState == State.Patrol)  c = new Color(0.2f, 1f,    0.2f,  0.85f);
        else if (currentState == State.Chase)   c = new Color(1f,   0.9f,  0.1f,  0.85f);
        else if (currentState == State.Attack)  c = new Color(1f,   0.15f, 0.15f, 0.85f);
        else if (currentState == State.Retreat) c = new Color(0.2f, 0.6f,  1f,    0.85f);
        else                                    c = Color.white;
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position + Vector3.up * 2.8f, 0.3f);
    }
}
