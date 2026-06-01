using System.Collections.Generic;
using UnityEngine;

public class AStarWalker : MonoBehaviour
{
    public AStarGrid grid;
    public float speed = 2.5f;
    public float turnSpeed = 8f;
    public bool resetOnArrival = true;
    public float resetDelay = 1.2f;

    List<AStarNode> followed;
    int idx;
    Vector3 spawnPosition;
    bool spawnCaptured;
    float arrivedTimer;

    void Update()
    {
        if (grid == null) return;

        if (!spawnCaptured)
        {
            spawnPosition = transform.position;
            spawnCaptured = true;
        }

        if (!ReferenceEquals(grid.path, followed))
        {
            var newPath = grid.path;
            arrivedTimer = 0f;

            if (newPath == null || newPath.Count == 0)
            {
                // No valid path — snap to spawn and wait
                followed = null;
                idx = 0;
                transform.position = spawnPosition;
            }
            else if (followed == null)
            {
                // First path assigned — snap to its start
                followed = newPath;
                Vector3 start = followed[0].worldPosition;
                start.y = transform.position.y;
                transform.position = start;
                idx = Mathf.Min(1, followed.Count - 1);
            }
            else
            {
                // Subsequent refresh — adopt new path, resume from nearest node
                followed = newPath;
                idx = ClosestNodeIndex(transform.position, followed);
            }
        }

        if (followed == null || followed.Count == 0) return;

        if (idx >= followed.Count)
        {
            if (!resetOnArrival) return;
            arrivedTimer += Time.deltaTime;
            if (arrivedTimer >= resetDelay)
            {
                transform.position = spawnPosition;
                followed = null;
                idx = 0;
                arrivedTimer = 0f;
            }
            return;
        }

        Vector3 target = followed[idx].worldPosition;
        target.y = transform.position.y;

        Vector3 delta = target - transform.position;
        float dist = delta.magnitude;
        if (dist < 0.08f) { idx++; return; }

        Vector3 step = delta.normalized * Mathf.Min(speed * Time.deltaTime, dist);
        transform.position += step;

        Vector3 flat = new Vector3(delta.x, 0f, delta.z);
        if (flat.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(flat), turnSpeed * Time.deltaTime);
    }

    static int ClosestNodeIndex(Vector3 pos, List<AStarNode> nodes)
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < nodes.Count; i++)
        {
            float d = (nodes[i].worldPosition - pos).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }
}
