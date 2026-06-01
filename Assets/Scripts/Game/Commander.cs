using UnityEngine;
using UnityEngine.InputSystem;

// Player command ability: the War Horn (Q). Rallies every defender within range —
// faster movement and a quicker attack cadence for a few seconds. A real lever the
// player pulls to turn a losing fight, on a cooldown so it is a decision, not spam.
public class Commander : MonoBehaviour
{
    public Key warCryKey = Key.Q;
    public float buffRadius = 32f;
    public float buffDuration = 5f;
    public float cooldown = 12f;
    public float speedMul = 1.45f;
    public float attackRateMul = 0.55f;   // attackInterval *= this (lower = faster)

    public float CooldownLeft { get; private set; }
    public float BuffLeft { get; private set; }

    void Update()
    {
        if (CooldownLeft > 0f) CooldownLeft -= Time.deltaTime;
        if (BuffLeft > 0f) BuffLeft -= Time.deltaTime;

        var kb = Keyboard.current;
        if (kb != null && kb[warCryKey].wasPressedThisFrame && CooldownLeft <= 0f)
            WarCry();
    }

    void WarCry()
    {
        CooldownLeft = cooldown;
        BuffLeft = buffDuration;

        Vector3 origin = Watchfire.Instance != null ? Watchfire.Instance.transform.position : transform.position;
        var all = Combatant.All;
        int n = 0;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c == null || c.IsDead || c.isStructure || c.team != Combatant.Team.Defender) continue;
            var buff = c.GetComponent<Buff>();
            if (buff == null) buff = c.gameObject.AddComponent<Buff>();
            buff.Apply(buffDuration, speedMul, attackRateMul);
            n++;
        }
        Debug.Log($"[WAR HORN] Rally! {n} defenders surge (+speed, +attack rate) for {buffDuration}s");
    }
}
