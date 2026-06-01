using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(LineRenderer))]
public class AgentPathViz : MonoBehaviour
{
    public Color pathColor = Color.yellow;
    public float width = 0.25f;
    public float yOffset = 0.1f;

    NavMeshAgent agent;
    LineRenderer line;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        line = GetComponent<LineRenderer>();
        line.startWidth = line.endWidth = width;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = line.endColor = pathColor;
        line.useWorldSpace = true;
        line.numCornerVertices = 4;
    }

    void LateUpdate()
    {
        if (agent.hasPath)
        {
            var corners = agent.path.corners;
            line.positionCount = corners.Length;
            for (int i = 0; i < corners.Length; i++)
                line.SetPosition(i, corners[i] + Vector3.up * yOffset);
        }
        else
        {
            line.positionCount = 0;
        }
    }
}
