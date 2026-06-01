using UnityEngine;

// Ties the A* labyrinth into the live game. On an interval, if the tunnel's A* path is
// open (the player has NOT sealed it with the Y/U doors), a Risen "slips through" and
// spawns at the tunnel exit, then paths to the Watchfire. Sealing the doors makes A*
// report "no path" -> no breach. Closing the main gate diverts more enemies underground,
// so breaches come faster. This is the strategic triangle: gate (G) vs tunnel doors (Y/U).
public class TunnelBreach : MonoBehaviour
{
    public AStarGrid grid;          // tunnel grid; grid.path is kept fresh by TunnelPathfinderAuto
    public Transform exitPoint;     // Tunnel_Exit in the world
    public GateController gate;     // main gate, to ramp breach rate

    public float firstDelay = 14f;
    public float openGateInterval = 16f;    // trickle while the front is open
    public float closedGateInterval = 7f;   // ramps up when the player seals the gate

    float timer;

    void Start() { timer = firstDelay; }

    void Update()
    {
        var gd = GameDirector.Instance;
        if (gd == null) return;
        if (gd.CurrentPhase != GameDirector.Phase.Fighting &&
            gd.CurrentPhase != GameDirector.Phase.BetweenWaves) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        bool gateOpen = gate == null || gate.IsOpen;
        timer = gateOpen ? openGateInterval : closedGateInterval;

        // Doors Y/U sealed -> A* finds no path -> the tunnel is safe.
        if (grid == null || grid.path == null || grid.path.Count == 0) return;

        Vector3 at = exitPoint != null ? exitPoint.position : new Vector3(50f, 0f, -10f);
        var c = gd.SpawnAttackerAt(at, false);
        if (c != null)
        {
            SoundEvent.Emit(at, 30f);
            Debug.Log("[TUNNEL BREACH] A Risen slipped through the labyrinth!");
        }
    }
}
