using UnityEngine;

public class TunnelPathfinderAuto : MonoBehaviour
{
    public TunnelPathfinder pathfinder;
    public float interval = 0.3f;

    float t;

    void Update()
    {
        if (pathfinder == null || pathfinder.grid == null || pathfinder.target == null) return;
        var start = pathfinder.startAnchor != null ? pathfinder.startAnchor : pathfinder.seeker;
        if (start == null) return;
        t += Time.deltaTime;
        if (t < interval) return;
        t = 0f;
        pathfinder.FindPath(start.position, pathfinder.target.position);
    }
}
