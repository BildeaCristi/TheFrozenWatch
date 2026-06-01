using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FieldOfView : MonoBehaviour
{
    public float viewRadius = 14f;
    [Range(0, 360)] public float viewAngle = 90f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public float eyeHeight = 1.6f;
    public float checkInterval = 0.1f;
    public static bool drawRuntime = true;

    public event Action<Transform> OnTargetSpotted;

    [HideInInspector] public List<Transform> visibleTargets = new List<Transform>();

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            ScanForTargets();
        }
    }

    void ScanForTargets()
    {
        visibleTargets.Clear();
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Collider[] inRange = Physics.OverlapSphere(origin, viewRadius, targetMask);

        for (int i = 0; i < inRange.Length; i++)
        {
            Transform t = inRange[i].transform;
            Vector3 dirToTarget = (t.position + Vector3.up * eyeHeight - origin).normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);
            if (angle > viewAngle * 0.5f) continue;

            float dist = Vector3.Distance(origin, t.position + Vector3.up * eyeHeight);
            if (Physics.Raycast(origin, dirToTarget, dist, obstacleMask)) continue;

            visibleTargets.Add(t);
            OnTargetSpotted?.Invoke(t);

            if (AlertSystem.Instance != null)
                AlertSystem.Instance.Alert(gameObject, t.position);
        }
    }

    void OnDrawGizmos()
    {
        if (!drawRuntime && Application.isPlaying) return;
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        bool seeing = visibleTargets.Count > 0;
        Gizmos.color = seeing ? new Color(1f, 0.2f, 0.2f, 0.9f) : new Color(1f, 0.95f, 0.2f, 0.5f);

        Gizmos.DrawWireSphere(origin, viewRadius);
        Vector3 left = DirFromAngle(-viewAngle * 0.5f);
        Vector3 right = DirFromAngle(viewAngle * 0.5f);
        Gizmos.DrawLine(origin, origin + left * viewRadius);
        Gizmos.DrawLine(origin, origin + right * viewRadius);
    }

    Vector3 DirFromAngle(float degrees)
    {
        float rad = (degrees + transform.eulerAngles.y) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
}
