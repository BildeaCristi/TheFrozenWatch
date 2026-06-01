# The Frozen Watch

A 3D medieval defense game made in **Unity 6 (URP)** for the Interactive Systems course.
You play Aldric Frost, the last warden of the Frozen Watch, and your job is simple to say
and hard to do: keep the Watchfire burning while the dead try to put it out.

The point of the project is to take every AI topic from the course (navigation, perception,
decision making, group behavior, optimization) and put them together inside one game that
actually plays, instead of separate demos.

## Media

> Gameplay video: _add your link here_
>
> Screenshots:
>
> <!-- drop images in a /Media folder and reference them, e.g. -->
> <!-- ![Siege](Media/siege.png) -->
> <!-- ![Tunnel A*](Media/tunnel.png) -->

## Story

Beyond the Great Barrier, an ice wall that holds back the frozen north, the Winter Lord has
raised an army of the dead. They are called the Risen, and they march south with one goal: reach
Shadowhold and snuff out the Watchfire, the flame that keeps the keep and the lands behind it alive.

If that fire goes dark, the south freezes. So you hold the wall.

The Risen do not come straight on. A frontal column slams the main gate while a second group
slips through an old labyrinth carved under the barrier, trying to flank the fire from the side.
Your guards fight in two groups, one holding the gate and one ringed around the Watchfire, and
you can step into any of them yourself when the line starts to bend.

## Gameplay

The goal is to survive three waves of Risen with the Watchfire still lit.

- **Win** if the fire is still burning after the third wave is cleared.
- **Lose** the moment the Watchfire's health reaches zero.

You are not just a spectator. Doing nothing loses the game, because the enemy hits two routes:

1. **The main gate.** A direct charge on the NavMesh. Open it to let your guards sortie out and
   meet them, close it to stem the tide (but then your guards cannot push out).
2. **The tunnel.** A flanking route run by the A* pathfinder. While the doors are open, A* finds a
   way through the maze and Risen slip in toward the fire. Slam the doors (Y / U) and A* reports no
   path, so the breach stops.

Tools you use to turn the fight: possess a guard and fight directly, raise the War Horn to rally
the whole watch (faster movement and attacks for a few seconds), manage the gate, and seal the
tunnel doors. A larger Risen Warlord shows up on the final wave as a mini boss.

### Controls

| Key | Action |
|-----|--------|
| F | Possess a guard, press again to release |
| WASD | Move, Shift to sprint, Mouse to look |
| LMB or Space | Attack (melee swing) |
| G | Open or close the main gate |
| Y / U | Seal the two tunnel doors |
| Q | War Horn, rallies nearby guards |
| T | Tunnel picture-in-picture camera |
| V / H | Toggle vision cones / hearing radius |
| P | Recompute and show the A* path |
| Esc | Pause and settings menu |
| R | Restart the siege |
| F2 | Open the story and tutorial again |

## AI techniques

Everything below runs at the same time in the playable scene.

**Navigation**
- NavMesh agents follow waypoint patrols (AgentMover) on a baked NavMesh that covers the ground
  and the wall walkway.
- The gate and tunnel doors are dynamic obstacles: closing one carves the NavMesh at runtime so
  agents replan around it (GateController).
- A custom grid A* runs the underground labyrinth (AStarGrid, TunnelPathfinder, AStarWalker). It
  exposes the open set, closed set and final path as colored gizmos, uses a Manhattan heuristic,
  and a fixed spawn anchor so a closed door truly removes the path.

**Perception**
- Field of view with a cone angle and a line of sight check (FieldOfView).
- Hearing and a shared alarm bus: noise emitters, hearing sensors and an alert system that
  propagates a sighting to nearby guards even if they did not see the intruder (HearingSensor,
  NoiseEmitter, AlertSystem, SoundEvent).
- A proximity trigger volume at the gate chokepoint (ProximitySensor).

**Decision making**
- Finite state machine on the guards: Patrol, Chase, Attack, plus a Retreat state that pulls a
  wounded guard back to the fire to recover (GuardFSM). The Risen use an Advance / Retreat FSM
  (RisenFSM).
- Behaviour tree on the Frost Wolf: a selector that prioritizes fleeing at low health, then
  pursuing a visible target, then investigating a heard noise, then patrolling (WolfBT).
- Tactical retreat driven by a shared blackboard (AgentBlackboard).
- Utility AI on a Risen: Advance scores higher the closer it gets to the objective (linear curve),
  Retreat spikes when a guard is near (exponential curve), with a hysteresis band so it does not
  flip-flop (UtilityBrain, AdvanceAction, RetreatAction).

**Group behavior and optimization**
- Boids flocking for a raven flock above the wall: separation, alignment, cohesion, plus a scatter
  force when a loud sound goes off (Boid, BoidManager).
- Squad coordination: a manager assigns encirclement slots around the current threat so the guards
  surround it instead of stacking on one point (SquadManager).
- AI level of detail: a scheduler spreads sensor updates over time and slows them down for far
  away agents (AIUpdateScheduler).

**Tying it into a game**
- Wave manager with win and lose states, restart, and a final wave boss (GameDirector).
- Wave attackers seek the Watchfire and peel off to fight blocking guards (SiegeAttacker).
- The tunnel breach is a hybrid: the unit is driven by the A* result through the maze, then hands
  off to the NavMesh agent at the exit (TunnelRunner). Combat, health and the objective are handled
  by Combatant and Watchfire.

## Installation and running

1. Open the project with **Unity 6 (6000.x)** using the **Universal Render Pipeline**.
2. Re-import the third-party packages (see below). They are not committed to this repository.
3. Open `Assets/Scenes/FrozenWatch.unity` and press **Play**.

The scene is generated procedurally. If anything looks off after re-importing assets, run
`Tools` then `Rebuild Scene (Simplify)` from the Unity menu to regenerate it, then save.

All of the gameplay code is under `Assets/Scripts/` and is the part written for this project.

### Third-party packages (not included)

To respect Asset Store licensing this public repository ships only the project's own work, so the
art and effect packs are left out. Import these from the Unity Asset Store or Package Manager to
run the full scene:

- Polytope Studio: Lowpoly Characters, Weapons, Environments, Props
- Advance Studios Medieval Castle
- JustCreate Low Poly Medieval Characters Lite
- Lowpoly Forest Pack Winter
- Cartoon FX Remaster (JMO Assets)
- Fantasy Skybox FREE
- Stylize Snow Texture
- ASTROFISH Medieval Wells and Props
- MedievalCastlePackLite
- Wolf
