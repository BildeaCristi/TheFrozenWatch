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
    float stuckT;

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

        bool routeOpen = grid != null && grid.path != null && grid.path.Count > 0;

        // Adopt the A* node list ONCE. The auto-pathfinder reassigns an equivalent list
        // every tick; re-adopting it each time reset our index and froze the unit in place.
        if (path == null && routeOpen) { path = grid.path; idx = ClosestNodeIndex(); }
        if (path == null) return;        // never got a route
        if (!routeOpen) return;          // player sealed the doors -> trapped, wait

        // Reached the south mouth (or the end of the path) -> hand off to NavMesh.
        if (idx >= path.Count || (exit != null && Vector3.Distance(transform.position, exit.position) < 1.8f))
        { HandOff(); return; }

        Vector3 target = path[idx].worldPosition; target.y = transform.position.y;
        Vector3 to = target - transform.position;
        float dist = to.magnitude;
        if (dist < arriveDist) { idx++; stuckT = 0f; return; }

        Vector3 dir = to / dist;
        transform.position += dir * speed * Time.deltaTime;
        Vector3 flat = new Vector3(dir.x, 0f, dir.z);
        if (flat.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), turnSpeed * Time.deltaTime);

        // Anti-stuck: if a node stays unreachable too long, skip it.
        stuckT += Time.deltaTime;
        if (stuckT > 2.5f) { idx++; stuckT = 0f; }
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
        if (siege != null) siege.enabled = true;
        if (agent == null) return;

        agent.enabled = true;

        // Snap onto the NavMesh. If the exact spot isn't covered, fall back to the open
        // field just south of the tunnel mouth so the unit never gets stranded.
        NavMeshHit h;
        bool placed = NavMesh.SamplePosition(transform.position, out h, 10f, NavMesh.AllAreas);
        if (!placed)
            placed = NavMesh.SamplePosition(new Vector3(transform.position.x, 0f, -16f), out h, 25f, NavMesh.AllAreas);
        if (placed) agent.Warp(h.position);

        agent.isStopped = false;
        if (siege != null && siege.objective != null && agent.isOnNavMesh)
            agent.SetDestination(siege.objective.position);   // head straight for the Watchfire
    }
}
