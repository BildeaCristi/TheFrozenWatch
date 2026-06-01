using UnityEngine;
using System.Collections.Generic;

public class SquadManager : MonoBehaviour
{
    [Header("Guards")]
    public GuardFSM[] guards;

    [Header("Slot config")]
    public float slotDistance = 8f;

    void Start()
    {
        if (guards == null || guards.Length == 0)
            AutoFindGuards();
    }

    void Update()
    {
        Transform threat = FindPrimaryThreat();
        if (threat == null)
        {
            foreach (var g in guards)
                if (g != null) g.SetTacticalSlot(null);
            return;
        }
        AssignSlots(threat);
    }

    void AutoFindGuards()
    {
        var list = new List<GuardFSM>();
        for (int i = 1; i <= 8; i++)
        {
            var go = GameObject.Find("Guard_" + i);
            if (go != null)
            {
                var g = go.GetComponent<GuardFSM>();
                if (g != null) list.Add(g);
            }
        }
        guards = list.ToArray();
    }

    void AssignSlots(Transform threat)
    {
        int slotIndex = 0;
        for (int i = 0; i < guards.Length; i++)
        {
            if (guards[i] == null) continue;
            var state = guards[i].CurrentState;
            if (state == GuardFSM.State.Chase || state == GuardFSM.State.Attack)
            {
                Vector3 slot = CalcSlotPos(threat, slotIndex++);
                guards[i].SetTacticalSlot(slot);
            }
            else
            {
                guards[i].SetTacticalSlot(null);
            }
        }
    }

    // Slots fan out around the threat's rear so any number of guards encircle it
    // (0 = centre, then alternating flanks): -80, -40, 0, 40, 80 degrees, wrapping.
    Vector3 CalcSlotPos(Transform threat, int index)
    {
        float angle = (index % 5) * 40f - 80f;

        Vector3 dir = Quaternion.Euler(0, angle, 0) * -threat.forward;
        Vector3 slot = threat.position + dir * slotDistance;

        // Keep slot on the wall walkway y-level of the guards
        if (guards.Length > 0 && guards[0] != null)
            slot.y = guards[0].transform.position.y;

        return slot;
    }

    Transform FindPrimaryThreat()
    {
        foreach (var g in guards)
        {
            if (g == null) continue;
            if (g.CurrentState == GuardFSM.State.Chase || g.CurrentState == GuardFSM.State.Attack)
            {
                var bb = g.GetComponent<AgentBlackboard>();
                if (bb != null && bb.lastThreatPos != null)
                    return bb.lastThreatPos;
            }
        }
        return null;
    }

    void OnDrawGizmos()
    {
        if (guards == null) return;
        Transform threat = FindPrimaryThreat();
        if (threat == null) return;
        for (int i = 0; i < Mathf.Min(guards.Length, 3); i++)
        {
            Vector3 slot = CalcSlotPos(threat, i);
            Gizmos.color = i == 0 ? Color.red : (i == 1 ? Color.cyan : Color.yellow);
            Gizmos.DrawWireSphere(slot, 0.6f);
            Gizmos.DrawLine(threat.position, slot);
        }
    }
}
