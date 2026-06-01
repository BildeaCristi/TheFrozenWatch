using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class WolfBT : MonoBehaviour
{
    public float pursueStopDistance = 2f;
    public float noiseMemoryDuration = 4f;

    public string LastAction { get; private set; } = "Patrol";

    NavMeshAgent agent;
    AgentMover mover;
    FieldOfView fov;
    HearingSensor hearing;
    AgentBlackboard bb;
    Node root;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        mover = GetComponent<AgentMover>();
        fov = GetComponent<FieldOfView>();
        hearing = GetComponent<HearingSensor>();
        bb = GetComponent<AgentBlackboard>();
        root = BuildTree();
    }

    void Update()
    {
        root?.Evaluate();
    }

    Node BuildTree()
    {
        return new Selector(new List<Node>
        {
            // Branch 1: self-preservation — flee when HP is very low
            new Sequence(new List<Node>
            {
                new ConditionNode(() => bb != null && bb.IsLowHealth(0.3f)),
                new ActionNode(FleeFromThreat)
            }),

            // Branch 2: engage — pursue any visible target
            new Sequence(new List<Node>
            {
                new ConditionNode(() => fov != null && fov.visibleTargets.Count > 0),
                new ActionNode(PursueTarget)
            }),

            // Branch 3: investigate — heard something recently
            new Sequence(new List<Node>
            {
                new ConditionNode(() => hearing != null && hearing.HasRecentNoise(noiseMemoryDuration)),
                new ActionNode(InvestigateNoise)
            }),

            // Branch 4: fallback patrol
            new ActionNode(Patrol)
        });
    }

    NodeState FleeFromThreat()
    {
        if (mover != null) mover.enabled = false;
        if (agent == null) return NodeState.Failure;

        Vector3 fleeDir = transform.position;
        if (fov != null && fov.visibleTargets.Count > 0)
            fleeDir = transform.position + (transform.position - fov.visibleTargets[0].position).normalized * 15f;
        else if (bb != null && bb.lastThreatPos != null)
            fleeDir = transform.position + (transform.position - bb.lastThreatPos.position).normalized * 15f;
        else
            return NodeState.Failure;

        agent.isStopped = false;
        agent.SetDestination(fleeDir);
        LastAction = "Flee";
        return NodeState.Running;
    }

    NodeState PursueTarget()
    {
        if (mover != null) mover.enabled = false;
        if (fov == null || fov.visibleTargets.Count == 0) return NodeState.Failure;

        Transform t = fov.visibleTargets[0];
        if (bb != null) bb.lastThreatPos = t;

        float dist = Vector3.Distance(transform.position, t.position);
        if (dist <= pursueStopDistance)
        {
            agent.isStopped = true;
            transform.LookAt(new Vector3(t.position.x, transform.position.y, t.position.z));
            LastAction = "Attack";
            return NodeState.Success;
        }

        agent.isStopped = false;
        agent.SetDestination(t.position);
        LastAction = "Pursue";
        return NodeState.Running;
    }

    NodeState InvestigateNoise()
    {
        if (mover != null) mover.enabled = false;
        if (hearing == null || agent == null) return NodeState.Failure;

        Vector3 dest = hearing.LastHeardAt;
        agent.isStopped = false;
        agent.SetDestination(dest);

        float dist = Vector3.Distance(transform.position, dest);
        LastAction = "Investigate";
        return dist < 1.5f ? NodeState.Success : NodeState.Running;
    }

    NodeState Patrol()
    {
        if (mover != null) mover.enabled = true;
        if (agent != null) agent.isStopped = false;
        LastAction = "Patrol";
        return NodeState.Running;
    }
}
