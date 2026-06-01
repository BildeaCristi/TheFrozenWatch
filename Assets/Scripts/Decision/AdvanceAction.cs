using UnityEngine;
using UnityEngine.AI;

public class AdvanceAction : UtilityAction
{
    public Transform gateTarget;
    public float maxDistance = 35f;

    NavMeshAgent agent;
    AgentMover mover;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        mover = GetComponent<AgentMover>();
        if (string.IsNullOrEmpty(actionName)) actionName = "Advance";

        if (gateTarget == null)
        {
            var go = GameObject.Find("MainGate");
            if (go != null) gateTarget = go.transform;
        }
    }

    public override float Evaluate()
    {
        if (gateTarget == null) return 0f;
        float dist = Vector3.Distance(transform.position, gateTarget.position);
        // Linear: higher score the closer we are to the gate (motivation to finish the advance)
        float normalized = Mathf.Clamp01(dist / maxDistance);
        return 1f - normalized;
    }

    public override void Execute()
    {
        if (mover != null) mover.enabled = true;
        if (agent != null) { agent.isStopped = false; }
        if (gateTarget != null && agent != null)
            agent.SetDestination(gateTarget.position);
    }
}
