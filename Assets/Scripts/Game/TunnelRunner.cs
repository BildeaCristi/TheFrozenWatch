using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Hybrid driver for attackers that breach via the labyrinth: while inside the tunnel
// the unit is moved by the project's own A* result (grid.path, computed by TunnelPathfinder),
// node by node. When it reaches the south exit it hands control to the NavMeshAgent +
// SiegeAttacker to finish the run to the Watchfire. This makes "enemies cross the tunnel
// using A*" literally true. If the player seals the doors, A* yields no path and the
// breachers are stuck inside — exactly what sealing should do.
public class TunnelRunner : MonoBehaviour
{
    public AStarGrid grid;
    public Transform exit;
    public float speed = 3f;
    public float turnSpeed = 9f;
    public float arriveDist = 0.25f;

    NavMeshAgent agent;
    SiegeAttacker siege;
    Combatant self;
    List<AStarNode> path;
    int idx;
    bool handed;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        siege = GetComponent<SiegeAttacker>();
        self = GetComponent<Combatant>();
        if (agent != null) agent.enabled = false;   // A* drives us inside the tunnel
        if (siege != null) siege.enabled = false;
    }

    void Update()
    {
        if (handed) return;
        if (self != null && self.IsDead) return;

        // Reached the south mouth -> switch to NavMesh for the field run to the Watchfire.
        if (exit != null && Vector3.Distance(transform.position, exit.position) < 1.6f) { HandOff(); return; }

        if (grid == null || grid.path == null || grid.path.Count == 0) return;  // doors sealed => no path => wait

        if (!ReferenceEquals(path, grid.path)) { path = grid.path; idx = ClosestNodeIndex(); }
        if (idx >= path.Count) { HandOff(); return; }

        Vector3 target = path[idx].worldPosition; target.y = transform.position.y;
        Vector3 to = target - transform.position;
        float dist = to.magnitude;
        if (dist < arriveDist) { idx++; return; }

        Vector3 dir = to / dist;
        transform.position += dir * speed * Time.deltaTime;
        Vector3 flat = new Vector3(dir.x, 0f, dir.z);
        if (flat.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), turnSpeed * Time.deltaTime);
    }

    int ClosestNodeIndex()
    {
        int best = 0; float bestSqr = float.MaxValue;
        for (int i = 0; i < path.Count; i++)
        {
            float d = (path[i].worldPosition - transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = i; }
        }
        return best;
    }

    void HandOff()
    {
        handed = true;
        if (agent != null)
        {
            agent.enabled = true;
            if (NavMesh.SamplePosition(transform.position, out var h, 4f, NavMesh.AllAreas)) agent.Warp(h.position);
        }
        if (siege != null) siege.enabled = true;
    }
}
