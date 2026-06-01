using UnityEngine;
using System.Collections.Generic;

public class BoidManager : MonoBehaviour
{
    [Header("Flock Settings")]
    public int flockSize = 20;
    public Transform goalTarget;
    public float spawnRadius = 8f;
    public float flockHeight = 14f;

    [Header("Scare Settings")]
    public float scareLoudnessThreshold = 30f;
    public float scareDuration = 5f;

    [HideInInspector] public List<Boid> boids = new List<Boid>();
    [HideInInspector] public bool IsScared;
    [HideInInspector] public Vector3 scarePoint;

    float scareTimer;

    void OnEnable()  => SoundEvent.OnNoise += OnSound;
    void OnDisable() => SoundEvent.OnNoise -= OnSound;

    void Start()
    {
        for (int i = 0; i < flockSize; i++)
        {
            Vector3 offset = Random.insideUnitSphere * spawnRadius;
            offset.y = 0f;
            Vector3 pos = transform.position + offset + Vector3.up * flockHeight;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.35f, 0.14f, 0.6f);  // flattened raven silhouette
            go.name = "Boid_" + i;
            go.transform.SetParent(transform);

            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                var dark = new Color(0.1f, 0.1f, 0.13f);
                m.color = dark;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", dark);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
                mr.sharedMaterial = m;
            }

            var b = go.AddComponent<Boid>();
            boids.Add(b);
        }
    }

    void Update()
    {
        if (IsScared)
        {
            scareTimer += Time.deltaTime;
            if (scareTimer >= scareDuration) IsScared = false;
        }
    }

    void OnSound(Vector3 pos, float loudness)
    {
        if (loudness >= scareLoudnessThreshold)
        {
            IsScared = true;
            scarePoint = pos;
            scareTimer = 0f;
        }
    }
}
