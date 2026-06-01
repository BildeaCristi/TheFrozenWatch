using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class Boid : MonoBehaviour
{
    [HideInInspector] public Vector3 velocity;

    public float minSpeed = 3f;
    public float maxSpeed = 8f;
    public float perceptionRadius = 5f;

    public float separationWeight = 1.5f;
    public float alignmentWeight  = 1.0f;
    public float cohesionWeight   = 1.0f;
    public float goalWeight       = 0.8f;
    public float avoidWeight      = 4.0f;

    BoidManager manager;

    void Start()
    {
        manager = GetComponentInParent<BoidManager>() ?? FindFirstObjectByType<BoidManager>();
        velocity = Random.insideUnitSphere * ((minSpeed + maxSpeed) * 0.5f);
        velocity.y = 0f;
        if (velocity.sqrMagnitude < 0.01f) velocity = Vector3.forward * minSpeed;
    }

    void Update()
    {
        List<Boid> neighbors = GetNeighbors();
        Vector3 accel = Vector3.zero;

        if (neighbors.Count > 0)
        {
            accel += CalcSeparation(neighbors) * separationWeight;
            accel += CalcAlignment(neighbors)  * alignmentWeight;
            accel += CalcCohesion(neighbors)   * cohesionWeight;
        }

        if (manager != null && manager.goalTarget != null)
            accel += (manager.goalTarget.position - transform.position).normalized * goalWeight;

        if (manager != null && manager.IsScared)
        {
            Vector3 away = transform.position - manager.scarePoint;
            if (away.sqrMagnitude > 0.01f)
                accel += away.normalized * avoidWeight;
        }

        velocity += accel * Time.deltaTime;
        velocity.y = 0f;

        float speed = velocity.magnitude;
        if (speed < 0.001f) velocity = transform.forward * minSpeed;
        else velocity = velocity.normalized * Mathf.Clamp(speed, minSpeed, maxSpeed);

        transform.position += velocity * Time.deltaTime;
        transform.forward = velocity.normalized;
    }

    List<Boid> GetNeighbors()
    {
        if (manager == null) return new List<Boid>();
        var result = new List<Boid>();
        float r2 = perceptionRadius * perceptionRadius;
        foreach (var b in manager.boids)
        {
            if (b == this) continue;
            if ((b.transform.position - transform.position).sqrMagnitude < r2)
                result.Add(b);
        }
        return result;
    }

    Vector3 CalcSeparation(List<Boid> neighbors)
    {
        Vector3 force = Vector3.zero;
        foreach (var n in neighbors)
        {
            Vector3 diff = transform.position - n.transform.position;
            float dist = diff.magnitude;
            if (dist > 0f) force += diff.normalized / dist;
        }
        return force;
    }

    Vector3 CalcAlignment(List<Boid> neighbors)
    {
        Vector3 avg = Vector3.zero;
        foreach (var n in neighbors) avg += n.velocity;
        avg /= neighbors.Count;
        return (avg - velocity).normalized;
    }

    Vector3 CalcCohesion(List<Boid> neighbors)
    {
        Vector3 center = Vector3.zero;
        foreach (var n in neighbors) center += n.transform.position;
        center /= neighbors.Count;
        return (center - transform.position).normalized;
    }
}
