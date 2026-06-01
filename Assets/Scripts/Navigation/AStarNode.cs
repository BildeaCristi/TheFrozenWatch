using UnityEngine;

public class AStarNode
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX, gridY;
    public int gCost, hCost;
    public AStarNode parent;

    public int fCost => gCost + hCost;

    public AStarNode(bool walkable, Vector3 worldPosition, int gridX, int gridY)
    {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;
    }
}
