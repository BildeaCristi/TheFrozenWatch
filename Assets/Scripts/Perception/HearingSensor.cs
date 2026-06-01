using UnityEngine;

[DisallowMultipleComponent]
public class HearingSensor : MonoBehaviour
{
    public float earReach = 12f;
    public static bool drawRuntime = false;

    Vector3 lastHeardAt;
    float lastHeardAge = Mathf.Infinity;

    void OnEnable()
    {
        SoundEvent.OnNoise += HandleNoise;
    }

    void OnDisable()
    {
        SoundEvent.OnNoise -= HandleNoise;
    }

    void Update()
    {
        lastHeardAge += Time.deltaTime;
    }

    void HandleNoise(Vector3 pos, float loudness)
    {
        float d = Vector3.Distance(transform.position, pos);
        float audible = Mathf.Min(loudness, earReach);
        if (d <= audible)
        {
            lastHeardAt = pos;
            lastHeardAge = 0f;
            if (AlertSystem.Instance != null)
                AlertSystem.Instance.Alert(gameObject, pos);
        }
    }

    public bool HasRecentNoise(float within) => lastHeardAge <= within;
    public Vector3 LastHeardAt => lastHeardAt;

    void OnDrawGizmos()
    {
        if (!drawRuntime && Application.isPlaying) return;
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, earReach);
    }
}
