using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public class ProximitySensor : MonoBehaviour
{
    public LayerMask targetMask;
    public string label = "Chokepoint";

    public event Action<Transform> OnIntrusion;
    public int IntrusionCount { get; private set; }

    void Reset()
    {
        var box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetMask) == 0) return;
        IntrusionCount++;
        OnIntrusion?.Invoke(other.transform);
        Debug.Log($"[{label}] Intrusion detected: {other.name}");
        if (AlertSystem.Instance != null)
            AlertSystem.Instance.Alert(gameObject, other.transform.position);
    }

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.9f);
        Gizmos.DrawWireCube(box.center, box.size);
        Gizmos.matrix = prev;
    }
}
