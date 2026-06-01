using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TunnelPathfinder : MonoBehaviour
{
    public AStarGrid grid;
    public Transform seeker;
    public Transform target;
    public Transform startAnchor;
    public Key triggerKey = Key.P;

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[triggerKey].wasPressedThisFrame)
        {
            var start = startAnchor != null ? startAnchor : seeker;
            if (start != null) FindPath(start.position, target.position);
        }
    }

    public void FindPath(Vector3 startPos, Vector3 targetPos)
    {
        grid.BuildGrid();
        grid.ResetCosts();

        AStarNode startNode = grid.NodeFromWorldPoint(startPos);
        AStarNode targetNode = grid.NodeFromWorldPoint(targetPos);

        if (!startNode.walkable || !targetNode.walkable)
        {
            Debug.LogWarning("Start or target node is not walkable.");
            grid.path = null;
            grid.closedSetDebug = null;
            return;
        }

        var openList = new List<AStarNode> { startNode };
        var closedSet = new HashSet<AStarNode>();

        startNode.gCost = 0;
        startNode.hCost = ManhattanDistance(startNode, targetNode);

        while (openList.Count > 0)
        {
            AStarNode current = openList[0];
            foreach (var n in openList)
                if (n.fCost < current.fCost || (n.fCost == current.fCost && n.hCost < current.hCost))
                    current = n;

            openList.Remove(current);
            closedSet.Add(current);

            if (current == targetNode)
            {
                RetracePath(startNode, targetNode);
                grid.closedSetDebug = closedSet;
                return;
            }

            foreach (var neighbour in grid.GetNeighbours(current))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;

                int tentativeG = current.gCost + 10;
                if (tentativeG < neighbour.gCost || !openList.Contains(neighbour))
                {
                    neighbour.gCost = tentativeG;
                    neighbour.hCost = ManhattanDistance(neighbour, targetNode);
                    neighbour.parent = current;
                    if (!openList.Contains(neighbour))
                        openList.Add(neighbour);
                }
            }
        }

        grid.path = null;
        grid.closedSetDebug = closedSet;
    }

    void RetracePath(AStarNode start, AStarNode end)
    {
        var p = new List<AStarNode>();
        var current = end;
        while (current != start)
        {
            p.Add(current);
            current = current.parent;
        }
        p.Reverse();
        grid.path = p;
    }

    int ManhattanDistance(AStarNode a, AStarNode b)
        => 10 * (Mathf.Abs(a.gridX - b.gridX) + Mathf.Abs(a.gridY - b.gridY));
}
