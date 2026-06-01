using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Orchestrates the playable loop: prep -> waves of attackers -> win, or lose if the
// Watchfire is extinguished. Attackers are cloned from an inactive template so the
// scene stays procedural (SceneRebuilder is the source of truth).
public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance { get; private set; }

    public enum Phase { PreGame, Fighting, BetweenWaves, Won, Lost }

    [Header("Wave design (simplified)")]
    public int[] waveSizes = { 3, 4, 6 };
    public float spawnStagger = 0.7f;
    public float betweenWaveDelay = 5f;
    public float prepDelay = 4f;
    [Range(0f, 1f)] public float enemyArcherChance = 0.3f;   // some Risen carry bows

    [Header("Scene references (auto-found if empty)")]
    public GameObject attackerTemplate;     // inactive Risen prototype
    public Transform[] spawnPoints;
    public Combatant watchfire;
    public Combatant player;
    public Transform playerSpawn;

    [Header("Tunnel (A*) route")]
    public AStarGrid tunnelGrid;     // attackers spawned past x>40 cross the tunnel via A*
    public Transform tunnelExit;
    public float tunnelSpawnX = 40f;

    [Header("Player")]
    public float playerRespawnDelay = 4f;

    // --- public read-only state for the HUD ---
    public Phase CurrentPhase { get; private set; } = Phase.PreGame;
    public int WaveNumber { get; private set; }          // 1-based
    public int TotalWaves => waveSizes != null ? waveSizes.Length : 0;
    public int AttackersAlive => alive.Count;
    public float Countdown { get; private set; }         // prep / between-wave timer
    public bool PlayerDown { get; private set; }

    readonly List<Combatant> alive = new List<Combatant>();
    readonly Queue<int> spawnQueue = new Queue<int>();
    float spawnTimer;
    float playerReviveTimer;

    // start poses of every standing agent, so Restart can reset to the beginning
    class RosterEntry { public Combatant c; public Vector3 pos; public Quaternion rot; }
    readonly List<RosterEntry> roster = new List<RosterEntry>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        AutoWire();
        if (watchfire != null) watchfire.OnDeath += _ => Lose();
        if (player != null)
        {
            player.destroyOnDeath = false;
            player.OnDeath += OnPlayerDown;
        }
        CacheRoster();
        EnterPreGame();
    }

    // Snapshot every standing (non-structure) agent's start pose and make it revivable.
    void CacheRoster()
    {
        roster.Clear();
        foreach (var c in Combatant.All)
        {
            if (c == null || c.isStructure) continue;
            c.destroyOnDeath = false;   // a fallen unit deactivates instead of being destroyed -> revivable
            roster.Add(new RosterEntry { c = c, pos = c.transform.position, rot = c.transform.rotation });
        }
    }

    void AutoWire()
    {
        if (watchfire == null && Watchfire.Instance != null) watchfire = Watchfire.Instance.Core;
        if (attackerTemplate == null)
        {
            var t = GameObject.Find("Risen_Template");
            if (t != null) attackerTemplate = t;
        }
        if (player == null)
        {
            var g1 = GameObject.Find("Guard_1");
            if (g1 != null) player = g1.GetComponent<Combatant>();
        }
        if ((spawnPoints == null || spawnPoints.Length == 0))
        {
            var root = GameObject.Find("AttackerSpawns");
            if (root != null)
            {
                var list = new List<Transform>();
                foreach (Transform c in root.transform) list.Add(c);
                spawnPoints = list.ToArray();
            }
        }
        if (playerSpawn == null)
        {
            var ps = GameObject.Find("PlayerSpawn");
            if (ps != null) playerSpawn = ps.transform;
        }
        if (tunnelGrid == null)
        {
            var g = GameObject.Find("BarrierTunnel_Grid");
            if (g != null) tunnelGrid = g.GetComponent<AStarGrid>();
        }
        if (tunnelExit == null)
        {
            var e = GameObject.Find("Tunnel_Exit");
            if (e != null) tunnelExit = e.transform;
        }
    }

    void Update()
    {
        var kb = Keyboard.current;

        // R restarts the whole siege from any phase.
        if (kb != null && kb[Key.R].wasPressedThisFrame) { Restart(); return; }

        switch (CurrentPhase)
        {
            case Phase.PreGame:
                Countdown -= Time.deltaTime;
                bool begin = kb != null && (kb[Key.Enter].wasPressedThisFrame || kb[Key.B].wasPressedThisFrame);
                if (begin || Countdown <= 0f) StartWave(0);
                break;

            case Phase.Fighting:
                PumpSpawns();
                PrunePlayerRespawn();
                if (spawnQueue.Count == 0 && alive.Count == 0)
                {
                    if (WaveNumber >= TotalWaves) Win();
                    else EnterBetweenWaves();
                }
                break;

            case Phase.BetweenWaves:
                PrunePlayerRespawn();
                Countdown -= Time.deltaTime;
                if (Countdown <= 0f) StartWave(WaveNumber);   // WaveNumber is 1-based = next index
                break;

            case Phase.Won:
            case Phase.Lost:
                break;   // R (handled above) restarts
        }
    }

    // ------------------------------------------------------------------ phases
    void EnterPreGame()
    {
        CurrentPhase = Phase.PreGame;
        WaveNumber = 0;
        Countdown = prepDelay;
    }

    void EnterBetweenWaves()
    {
        CurrentPhase = Phase.BetweenWaves;
        Countdown = betweenWaveDelay;
    }

    void StartWave(int index)
    {
        if (waveSizes == null || index >= waveSizes.Length) { Win(); return; }
        WaveNumber = index + 1;
        CurrentPhase = Phase.Fighting;
        spawnQueue.Clear();
        int count = waveSizes[index];
        for (int i = 0; i < count; i++) spawnQueue.Enqueue(i);
        spawnTimer = 0f;

        if (index == waveSizes.Length - 1) SpawnBoss();   // final wave climax
    }

    void SpawnBoss()
    {
        // The boss is large -> spawn it at the GATE (frontal), never the tight A* tunnel.
        var c = SpawnAttackerAt(new Vector3(Random.Range(-3f, 3f), 0f, 34f), true);
        if (c == null) return;
        c.name = "Risen_Boss";
        c.maxHealth = 420f; c.health = 420f;
        c.attackDamage = 32f; c.attackInterval = 1.2f; c.attackRange = 2.8f;
        c.transform.localScale *= 1.7f;
        var ag = c.GetComponent<NavMeshAgent>();
        if (ag != null) { ag.speed *= 0.7f; ag.radius = 0.7f; }   // wider so it doesn't clip the gate
        Debug.Log("[BOSS] A Risen Warlord enters the field!");
    }

    void Win()
    {
        CurrentPhase = Phase.Won;
        Time.timeScale = 1f;
    }

    void Lose()
    {
        if (CurrentPhase == Phase.Lost) return;
        CurrentPhase = Phase.Lost;
    }

    // ------------------------------------------------------------------ spawning
    void PumpSpawns()
    {
        if (spawnQueue.Count == 0) return;
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;
        spawnTimer = spawnStagger;
        spawnQueue.Dequeue();
        SpawnAttacker();
    }

    void SpawnAttacker()
    {
        SpawnAttackerAt(PickSpawnPos(), true);
    }

    // Shared spawn used by waves (countAsWave=true, blocks wave completion until dead)
    // and by tunnel breaches (countAsWave=false, an independent secondary threat).
    public Combatant SpawnAttackerAt(Vector3 at, bool countAsWave)
    {
        if (attackerTemplate == null) return null;

        var clone = Instantiate(attackerTemplate, at, Quaternion.identity);
        clone.name = countAsWave ? "Risen_Wave" : "Risen_Tunnel";
        clone.SetActive(true);

        var agent = clone.GetComponent<NavMeshAgent>();
        if (agent != null && NavMesh.SamplePosition(at, out var hit, 6f, NavMesh.AllAreas))
            agent.Warp(hit.position);

        var siege = clone.GetComponent<SiegeAttacker>();
        if (siege == null) siege = clone.AddComponent<SiegeAttacker>();
        siege.objective = watchfire != null ? watchfire.transform : null;

        // Spawned past the tunnel line -> cross the labyrinth driven by A*, then hand off to NavMesh.
        if (at.x > tunnelSpawnX && tunnelGrid != null)
        {
            var tr = clone.AddComponent<TunnelRunner>();
            tr.grid = tunnelGrid;
            tr.exit = tunnelExit;
        }

        var cb = clone.GetComponent<Combatant>();
        if (cb != null)
        {
            cb.team = Combatant.Team.Attacker;
            if (Random.value < enemyArcherChance)
            {
                var a = clone.AddComponent<Archer>();   // enemy bowman: fires from range
                a.range = 15f; a.damage = 9f; a.cooldown = 1.6f;
                cb.autoAttack = false;
            }
            if (countAsWave)
            {
                cb.OnDeath += OnAttackerDeath;
                alive.Add(cb);
            }
        }
        return cb;
    }

    Vector3 PickSpawnPos()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var p = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (p != null) return p.position + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
        }
        return new Vector3(Random.Range(-6f, 6f), 0f, 32f);
    }

    void OnAttackerDeath(Combatant c)
    {
        alive.Remove(c);
    }

    // ------------------------------------------------------------------ player
    void OnPlayerDown(Combatant c)
    {
        PlayerDown = true;
        playerReviveTimer = playerRespawnDelay;
    }

    void PrunePlayerRespawn()
    {
        if (!PlayerDown || player == null) return;
        playerReviveTimer -= Time.deltaTime;
        if (playerReviveTimer <= 0f)
        {
            Vector3 at = playerSpawn != null ? playerSpawn.position : transform.position;
            player.Revive(at);
            PlayerDown = false;
        }
    }

    // ------------------------------------------------------------------ restart
    public void Restart()
    {
        Time.timeScale = 1f;

        // remove every spawned attacker (waves, tunnel breachers, boss)
        foreach (var c in new List<Combatant>(alive))
            if (c != null) Destroy(c.gameObject);
        alive.Clear();
        spawnQueue.Clear();
        foreach (var c in new List<Combatant>(Combatant.All))
            if (c != null && c.team == Combatant.Team.Attacker &&
                (c.name.Contains("Wave") || c.name.Contains("Tunnel") || c.name.Contains("Boss")))
                Destroy(c.gameObject);

        // restore the objective
        if (watchfire != null) watchfire.Revive(watchfire.transform.position);

        // revive + reposition every standing agent to its start pose
        foreach (var e in roster)
        {
            if (e == null || e.c == null) continue;
            e.c.Revive(e.pos);
            e.c.transform.rotation = e.rot;
        }

        ReopenGates();
        PlayerDown = false;
        EnterPreGame();
        Debug.Log("[RESTART] Siege reset to the beginning.");
    }

    void ReopenGates()
    {
        foreach (var n in new[] { "MainGate", "TunnelDoor_Y", "TunnelDoor_U" })
        {
            var go = GameObject.Find(n);
            if (go == null) continue;
            var gc = go.GetComponent<GateController>();
            if (gc != null && !gc.IsOpen) gc.ToggleGate();
        }
    }

    public string PhaseLabel()
    {
        switch (CurrentPhase)
        {
            case Phase.PreGame:      return "PREPARE  —  Enter/B to begin";
            case Phase.Fighting:     return $"WAVE {WaveNumber}/{TotalWaves}  —  enemies left: {AttackersAlive}";
            case Phase.BetweenWaves: return $"WAVE {WaveNumber} CLEARED  —  next in {Mathf.CeilToInt(Countdown)}s";
            case Phase.Won:          return "THE WATCH HELD — VICTORY!  (R to restart)";
            case Phase.Lost:         return "THE WATCHFIRE IS OUT — DEFEAT  (R to restart)";
        }
        return "";
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
