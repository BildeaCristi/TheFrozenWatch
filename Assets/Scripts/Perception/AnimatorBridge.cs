using UnityEngine;
using UnityEngine.AI;

// Drives the character Animator from gameplay state: locomotion Speed (from the agent),
// an Attack trigger when the Combatant strikes, and a Dead bool on death. Tolerant of
// missing parameters so it works with any controller.
[DisallowMultipleComponent]
public class AnimatorBridge : MonoBehaviour
{
    public string speedParam = "Speed";
    public string attackParam = "Attack";
    public string deadParam = "Dead";
    public float damping = 0.2f;

    Animator animator;
    NavMeshAgent agent;
    Combatant combat;
    bool hasSpeed, hasAttack, hasDead;
    bool deadSet;
    float prevFlash;
    Vector3 lastPos;
    float smoothed;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        combat = GetComponent<Combatant>();
        if (animator != null) animator.applyRootMotion = false;   // NavMesh moves us, not the clip
        hasSpeed = HasParam(animator, speedParam, AnimatorControllerParameterType.Float);
        hasAttack = HasParam(animator, attackParam, AnimatorControllerParameterType.Trigger);
        hasDead = HasParam(animator, deadParam, AnimatorControllerParameterType.Bool);
        lastPos = transform.position;
    }

    void Update()
    {
        if (animator == null) return;

        // Use actual world movement (works for NavMesh-driven AI AND player agent.Move,
        // where agent.velocity stays ~0 and would otherwise freeze the walk animation).
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float raw = (transform.position - lastPos).magnitude / dt;
        lastPos = transform.position;

        smoothed = Mathf.Lerp(smoothed, raw, Time.deltaTime / Mathf.Max(0.01f, damping));
        if (hasSpeed) animator.SetFloat(speedParam, smoothed);

        if (combat != null)
        {
            if (hasDead && combat.IsDead && !deadSet) { animator.SetBool(deadParam, true); deadSet = true; }
            if (hasAttack && combat.AttackFlash > prevFlash + 0.001f) animator.SetTrigger(attackParam);
            prevFlash = combat.AttackFlash;
        }
    }

    public float CurrentSpeed => smoothed;

    static bool HasParam(Animator a, string name, AnimatorControllerParameterType type)
    {
        if (a == null || a.runtimeAnimatorController == null) return false;
        foreach (var p in a.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }
}
