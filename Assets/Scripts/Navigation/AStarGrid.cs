using System.Collections.Generic;
using UnityEngine;

public class AStarGrid : MonoBehaviour
{
    [Header("Tunnel Grid Settings")]
    public Vector2 gridWorldSize = new Vector2(8, 12);
    public float nodeRadius = 0.5f;
    public LayerMask obstacleLayer;

    [HideInInspector] public List<AStarNode> path;
    [HideInInspector] public HashSet<AStarNode> closedSetDebug;

    AStarNode[,] grid;
    float nodeDiameter;
    int gridSizeX, gridSizeY;

    void Awake()
    {
        BuildGrid();
    }

    public void BuildGrid()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        grid = new AStarNode[gridSizeX, gridSizeY];

        Vector3 bottomLeft = transform.position
            - Vector3.right * gridWorldSize.x / 2f
            - Vector3.forward * gridWorldSize.y / 2f;

        for (int x = 0; x < gridSizeX; x++)
        for (int y = 0; y < gridSizeY; y++)
        {
            Vector3 worldPoint = bottomLeft
                + Vector3.right * (x * nodeDiameter + nodeRadius)
                + Vector3.forward * (y * nodeDiameter + nodeRadius);
            bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius - 0.05f, obstacleLayer);
            grid[x, y] = new AStarNode(walkable, worldPoint, x, y);
        }
    }

    public AStarNode NodeFromWorldPoint(Vector3 worldPos)
    {
        if (grid == null) BuildGrid();
        float pctX = Mathf.Clamp01((worldPos.x - (transform.position.x - gridWorldSize.x / 2f)) / gridWorldSize.x);
        float pctY = Mathf.Clamp01((worldPos.z - (transform.position.z - gridWorldSize.y / 2f)) / gridWorldSize.y);
        int x = Mathf.RoundToInt((gridSizeX - 1) * pctX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * pctY);
        return grid[x, y];
    }

    public List<AStarNode> GetNeighbours(AStarNode node)
    {
        var result = new List<AStarNode>();
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = node.gridX + dx[i];
            int ny = node.gridY + dy[i];
            if (nx >= 0 && nx < gridSizeX && ny >= 0 && ny < gridSizeY)
                result.Add(grid[nx, ny]);
        }
        return result;
    }

    public void ResetCosts()
    {
        if (grid == null) return;
        foreach (var n in grid) { n.gCost = 0; n.hCost = 0; n.parent = null; }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if (grid == null) return;
        float d = nodeRadius * 2;

        foreach (var node in grid)
        {
            if (path != null && path.Contains(node))
                Gizmos.color = Color.green;
            else if (closedSetDebug != null && closedSetDebug.Contains(node))
                Gizmos.color = Color.red;
            else if (!node.walkable)
                Gizmos.color = Color.black;
            else
                Gizmos.color = new Color(1, 1, 1, 0.4f);

            Gizmos.DrawCube(node.worldPosition, Vector3.one * (d - 0.05f));
        }
    }
}
