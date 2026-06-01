using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AStarGrid))]
public class TunnelGridRenderer : MonoBehaviour
{
    public int tunnelLayer = 9;
    public float cellAlpha = 0.55f;
    public float yOffset = 0.05f;

    public Color walkableColor = new Color(0.85f, 0.9f, 1f, 1f);
    public Color obstacleColor = new Color(0.05f, 0.05f, 0.07f, 1f);
    public Color pathColor = new Color(0.2f, 1f, 0.35f, 1f);
    public Color closedColor = new Color(1f, 0.35f, 0.35f, 1f);
    public Color startColor = new Color(1f, 0.95f, 0.2f, 1f);
    public Color endColor = new Color(1f, 0.55f, 0.1f, 1f);

    AStarGrid grid;
    GameObject[,] tiles;
    MaterialPropertyBlock mpb;
    Material sharedMat;
    int gridX, gridY;

    void Start()
    {
        grid = GetComponent<AStarGrid>();
        if (grid == null) return;
        grid.BuildGrid();
        Rebuild();
    }

    void Rebuild()
    {
        if (tiles != null)
        {
            foreach (var t in tiles) if (t != null) Destroy(t);
        }

        float nd = grid.nodeRadius * 2f;
        gridX = Mathf.RoundToInt(grid.gridWorldSize.x / nd);
        gridY = Mathf.RoundToInt(grid.gridWorldSize.y / nd);
        tiles = new GameObject[gridX, gridY];
        mpb = new MaterialPropertyBlock();

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        sharedMat = new Material(shader);

        var root = new GameObject("Tiles_Root");
        root.transform.SetParent(transform, worldPositionStays: false);
        root.layer = tunnelLayer;

        for (int x = 0; x < gridX; x++)
        for (int y = 0; y < gridY; y++)
        {
            var node = GetNode(x, y);
            if (node == null) continue;
            var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.name = $"Tile_{x}_{y}";
            tile.transform.SetParent(root.transform, worldPositionStays: false);
            var col = tile.GetComponent<Collider>(); if (col != null) Destroy(col);
            var pos = node.worldPosition;
            pos.y = transform.position.y + yOffset;
            tile.transform.position = pos;
            tile.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            tile.transform.localScale = new Vector3(nd * 0.92f, nd * 0.92f, 1f);
            tile.layer = tunnelLayer;
            var rend = tile.GetComponent<Renderer>();
            rend.sharedMaterial = sharedMat;
            tiles[x, y] = tile;
        }
    }

    void LateUpdate()
    {
        if (tiles == null || grid == null) return;

        var pathSet = grid.path != null ? new HashSet<AStarNode>(grid.path) : null;
        AStarNode start = null, end = null;
        if (grid.path != null && grid.path.Count > 0)
        {
            end = grid.path[grid.path.Count - 1];
            // Start is the closest node to seeker — not in path, so find via closedSet: lowest gCost. Too expensive; leave start null and highlight end only.
        }
        var closed = grid.closedSetDebug;

        for (int x = 0; x < gridX; x++)
        for (int y = 0; y < gridY; y++)
        {
            var tile = tiles[x, y]; if (tile == null) continue;
            var node = GetNode(x, y); if (node == null) continue;
            Color c;
            if (!node.walkable) c = obstacleColor;
            else if (pathSet != null && pathSet.Contains(node)) c = (node == end) ? endColor : pathColor;
            else if (closed != null && closed.Contains(node)) c = closedColor;
            else c = walkableColor;
            c.a = cellAlpha;
            var rend = tile.GetComponent<Renderer>();
            rend.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);
            rend.SetPropertyBlock(mpb);
        }
    }

    AStarNode GetNode(int x, int y)
    {
        var f = typeof(AStarGrid).GetField("grid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f == null) return null;
        var arr = f.GetValue(grid) as AStarNode[,];
        if (arr == null) return null;
        if (x < 0 || y < 0 || x >= arr.GetLength(0) || y >= arr.GetLength(1)) return null;
        return arr[x, y];
    }
}
