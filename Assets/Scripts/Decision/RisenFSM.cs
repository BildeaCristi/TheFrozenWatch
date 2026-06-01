using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class RisenFSM : MonoBehaviour
{
    public enum RisenState { Advance, Retreat }

    [Header("Thresholds")]
    public float threatRadius = 10f;
    public float safeRadius = 15f;
    public float retreatDuration = 5f;
    public float retreatSpeedBoost = 1.5f;

    public RisenState CurrentState => currentState;
    public string StateName => currentState.ToString();

    RisenState currentState = RisenState.Advance;
    NavMeshAgent agent;
    AgentMover mover;
    AgentBlackboard bb;
    float retreatTimer;
    float baseSpeed;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        mover = GetComponent<AgentMover>();
        bb = GetComponent<AgentBlackboard>();
        if (agent != null) baseSpeed = agent.speed;
    }

    void Update()
    {
        switch (currentState)
        {
            case RisenState.Advance: UpdateAdvance(); break;
            case RisenState.Retreat: UpdateRetreat(); break;
        }
    }

    void UpdateAdvance()
    {
        if (mover != null) mover.enabled = true;
        if (agent != null) { agent.isStopped = false; agent.speed = baseSpeed; }

        float dist;
        Transform threat = FindNearestGuard(out dist);
        if (threat != null && dist < threatRadius)
        {
            if (bb != null) bb.lastThreatPos = threat;
            retreatTimer = 0f;
            ChangeState(RisenState.Retreat);
        }
    }

    void UpdateRetreat()
    {
        if (mover != null) mover.enabled = false;
        retreatTimer += Time.deltaTime;

        float dist;
        Transform threat = FindNearestGuard(out dist);

        if (retreatTimer >= retreatDuration || dist > safeRadius)
        {
            ChangeState(RisenState.Advance);
            return;
        }

        Vector3 threatPos = (threat != null) ? threat.position
            : (bb != null && bb.lastThreatPos != null ? bb.lastThreatPos.position : transform.position + Vector3.back);

        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = baseSpeed * retreatSpeedBoost;
            Vector3 awayDir = (transform.position - threatPos).normalized;
            agent.SetDestination(transform.position + awayDir * 12f);
        }
        if (bb != null) bb.isUnderFire = true;
    }

    void ChangeState(RisenState next)
    {
        if (next == currentState) return;
        currentState = next;
        if (bb != null) bb.isUnderFire = (next == RisenState.Retreat);
    }

    Transform FindNearestGuard(out float closestDist)
    {
        closestDist = Mathf.Infinity;
        Transform nearest = null;
        string[] names = { "Guard_1", "Guard_2", "Guard_3" };
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            if (go == null) continue;
            float d = Vector3.Distance(transform.position, go.transform.position);
            if (d < closestDist) { closestDist = d; nearest = go.transform; }
        }
        return nearest;
    }

    void OnDrawGizmos()
    {
        Color c = currentState == RisenState.Retreat
            ? new Color(1f, 0.5f, 0f, 0.7f)
            : new Color(0.8f, 0.1f, 0.1f, 0.35f);
        Gizmos.color = c;
        Gizmos.DrawWireSphere(transform.position, threatRadius);
    }
}
