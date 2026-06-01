using UnityEngine;
using UnityEngine.AI;

public class RetreatAction : UtilityAction
{
    public float dangerRadius = 14f;
    public float retreatDistance = 12f;

    NavMeshAgent agent;
    AgentMover mover;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        mover = GetComponent<AgentMover>();
        if (string.IsNullOrEmpty(actionName)) actionName = "Retreat";
    }

    public override float Evaluate()
    {
        float minDist = MinGuardDistance();
        if (minDist >= dangerRadius) return 0f;

        // Exponential curve: score spikes sharply as guard closes in
        float t = 1f - Mathf.Clamp01(minDist / dangerRadius);
        return Mathf.Pow(t, 2f);
    }

    public override void Execute()
    {
        if (mover != null) mover.enabled = false;
        if (agent == null) return;

        Vector3 fleeFrom = NearestActiveGuardPos();
        if (fleeFrom == Vector3.zero) return;

        Vector3 awayDir = (transform.position - fleeFrom).normalized;
        agent.isStopped = false;
        agent.SetDestination(transform.position + awayDir * retreatDistance);
    }

    float MinGuardDistance()
    {
        float min = Mathf.Infinity;
        foreach (var n in new[] { "Guard_1", "Guard_2", "Guard_3" })
        {
            var go = GameObject.Find(n);
            if (go == null) continue;
            float d = Vector3.Distance(transform.position, go.transform.position);
            if (d < min) min = d;
        }
        return min;
    }

    Vector3 NearestActiveGuardPos()
    {
        float min = Mathf.Infinity;
        Vector3 pos = Vector3.zero;
        foreach (var n in new[] { "Guard_1", "Guard_2", "Guard_3" })
        {
            var go = GameObject.Find(n);
            if (go == null) continue;
            float d = Vector3.Distance(transform.position, go.transform.position);
            if (d < min) { min = d; pos = go.transform.position; }
        }
        return pos;
    }
}
