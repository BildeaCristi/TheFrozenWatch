using System.Collections.Generic;
using UnityEngine;

public class AlertSystem : MonoBehaviour
{
    public static AlertSystem Instance { get; private set; }

    public float alertDuration = 4f;
    public float propagationRadius = 30f;

    class State
    {
        public float timer;
        public Vector3 source;
    }

    readonly Dictionary<GameObject, State> states = new Dictionary<GameObject, State>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Alert(GameObject owner, Vector3 source)
    {
        if (owner == null) return;
        if (!states.TryGetValue(owner, out var st))
        {
            st = new State();
            states[owner] = st;
        }
        st.timer = alertDuration;
        st.source = source;
        PropagateFrom(owner, source);
    }

    void PropagateFrom(GameObject origin, Vector3 source)
    {
        var sensors = FindObjectsByType<HearingSensor>(FindObjectsSortMode.None);
        foreach (var s in sensors)
        {
            if (s == null || s.gameObject == origin) continue;
            float d = Vector3.Distance(s.transform.position, origin.transform.position);
            if (d <= propagationRadius)
            {
                if (!states.TryGetValue(s.gameObject, out var st))
                {
                    st = new State();
                    states[s.gameObject] = st;
                }
                if (st.timer < alertDuration * 0.5f)
                {
                    st.timer = alertDuration * 0.75f;
                    st.source = source;
                }
            }
        }
    }

    void Update()
    {
        if (states.Count == 0) return;
        var keys = new List<GameObject>(states.Keys);
        foreach (var k in keys)
        {
            var st = states[k];
            st.timer -= Time.deltaTime;
            if (st.timer <= 0f) states.Remove(k);
        }
    }

    public bool IsAlerted(GameObject owner)
    {
        return owner != null && states.TryGetValue(owner, out var st) && st.timer > 0f;
    }

    public Vector3 GetSource(GameObject owner)
    {
        return states.TryGetValue(owner, out var st) ? st.source : Vector3.zero;
    }

    public IEnumerable<GameObject> AlertedOwners()
    {
        foreach (var kv in states)
            if (kv.Value.timer > 0f) yield return kv.Key;
    }
}
