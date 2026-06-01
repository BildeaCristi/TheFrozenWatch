using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Combat core for The Frozen Watch.
// Every fighter (guards, player, risen, wolf) and the Watchfire carries a Combatant.
// It tracks HP + team, runs an autonomous melee strike on the nearest hostile in range,
// and resolves death (despawn or, for the player, a revivable "down" state).
[DisallowMultipleComponent]
public class Combatant : MonoBehaviour
{
    public enum Team { Defender, Attacker, Neutral }

    [Header("Identity")]
    public Team team = Team.Defender;
    public bool isStructure = false;     // Watchfire: never moves, never chases

    [Header("Stats")]
    public float maxHealth = 100f;
    public float health = 100f;
    public float attackDamage = 12f;
    public float attackRange = 2.2f;
    public float attackInterval = 1.0f;

    [Header("Behaviour")]
    public bool autoAttack = true;       // player turns this off (manual swings)
    public bool destroyOnDeath = true;   // player uses revive instead

    [Header("Death")]
    public float despawnDelay = 4f;

    public bool IsDead { get; private set; }
    public event Action<Combatant> OnDeath;
    public event Action<Combatant> OnDamaged;
    public float HealthFraction => Mathf.Clamp01(health / Mathf.Max(1f, maxHealth));

    float atkTimer;
    AgentBlackboard bb;
    NavMeshAgent agent;

    // visual feedback
    Transform vis;
    Vector3 visBaseScale = Vector3.one;
    Vector3 visBasePos = Vector3.zero;
    float hitPunch;
    bool dying;
    float dyingT;
    public float AttackFlash { get; private set; }   // >0 right after this unit strikes

    public void TriggerAttackFlash() { AttackFlash = 0.18f; }

    // Global registry so anyone can scan teams without FindObjectsOfType every frame.
    static readonly List<Combatant> all = new List<Combatant>();
    public static IReadOnlyList<Combatant> All => all;

    void OnEnable() { if (!all.Contains(this)) all.Add(this); }
    void OnDisable() { all.Remove(this); }

    void Awake()
    {
        bb = GetComponent<AgentBlackboard>();
        agent = GetComponent<NavMeshAgent>();
        if (health <= 0f) health = maxHealth;
        vis = transform.Find("Visual");
        if (vis != null) { visBaseScale = vis.localScale; visBasePos = vis.localPosition; }
    }

    void Update()
    {
        UpdateFeedback();
        if (IsDead) return;
        if (bb != null) bb.health = health;     // keep BT low-health checks honest

        if (AttackFlash > 0f) AttackFlash -= Time.deltaTime;
        if (!autoAttack || attackDamage <= 0f) return;

        atkTimer -= Time.deltaTime;
        if (atkTimer > 0f) return;

        var victim = FindHostileInRange();
        if (victim != null)
        {
            victim.ApplyDamage(attackDamage, this);
            atkTimer = attackInterval;
            AttackFlash = 0.18f;
            if (!isStructure) FaceTarget(victim.transform);
        }
    }

    // Hit punch (scale pop on damage) and death topple (fall + sink before despawn).
    void UpdateFeedback()
    {
        if (vis == null) return;

        if (dying)
        {
            dyingT += Time.deltaTime;
            float k = Mathf.Clamp01(dyingT / Mathf.Max(0.01f, despawnDelay));
            vis.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 90f, k), 0f, 0f);
            vis.localPosition = visBasePos + Vector3.down * Mathf.Lerp(0f, 0.6f, k);
            return;
        }

        float scale = 1f;
        if (hitPunch > 0f) { hitPunch -= Time.deltaTime; scale = 1f + 0.22f * Mathf.Clamp01(hitPunch / 0.12f); }
        vis.localScale = visBaseScale * scale;

        // attack lunge — a quick forward jab so a strike is readable without a skeletal anim
        float lunge = (AttackFlash > 0f) ? 0.4f * Mathf.Clamp01(AttackFlash / 0.18f) : 0f;
        vis.localPosition = visBasePos + Vector3.forward * lunge;
    }

    public Combatant FindHostileInRange()
    {
        Combatant best = null;
        float bestSqr = attackRange * attackRange;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c == null || c.IsDead || c == this || !IsHostileTo(c)) continue;
            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d <= bestSqr) { bestSqr = d; best = c; }
        }
        return best;
    }

    public bool IsHostileTo(Combatant o)
    {
        if (o == null) return false;
        if (team == Team.Neutral || o.team == Team.Neutral) return false;
        return team != o.team;
    }

    public void ApplyDamage(float amount, Combatant source = null)
    {
        if (IsDead || amount <= 0f) return;
        health = Mathf.Max(0f, health - amount);
        if (bb != null) { bb.health = health; bb.isUnderFire = true; }
        hitPunch = 0.12f;
        OnDamaged?.Invoke(this);
        if (health <= 0f) Die();
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDeath?.Invoke(this);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        DisableBehaviours();
        if (destroyOnDeath)
        {
            dying = true;              // UpdateFeedback topples the body over despawnDelay
            Destroy(gameObject, despawnDelay);
        }
        else
        {
            gameObject.SetActive(false);   // player goes "down", GameDirector revives it
        }
    }

    void DisableBehaviours()
    {
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var mb in GetComponents<MonoBehaviour>())
            if (mb != this) mb.enabled = false;
    }

    // Used by GameDirector to bring the player (or a reset round) back to life.
    public void Revive(Vector3 at)
    {
        IsDead = false;
        health = maxHealth;
        atkTimer = 0f;
        transform.position = at;
        gameObject.SetActive(true);

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = true;
        foreach (var mb in GetComponents<MonoBehaviour>())
            mb.enabled = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(at);
            agent.isStopped = false;
        }
        if (bb != null) bb.health = health;
    }

    void FaceTarget(Transform t)
    {
        Vector3 d = t.position - transform.position; d.y = 0f;
        if (d.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 0.4f);
    }
}
