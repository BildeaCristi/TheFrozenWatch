using UnityEngine;
using UnityEngine.AI;

public class AgentMover : MonoBehaviour
{
    public Transform[] waypoints;
    private NavMeshAgent agent;
    private int currentIndex = 0;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (waypoints != null && waypoints.Length > 0)
            agent.SetDestination(waypoints[0].position);
    }

    void Update()
    {
        if (agent == null || waypoints == null || waypoints.Length == 0) return;
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentIndex = (currentIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentIndex].position);
        }
    }
}
