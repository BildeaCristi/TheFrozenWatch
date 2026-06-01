using UnityEngine;

public class FiniteStateMachine
{
    FSMState current;
    GameObject owner;

    public FSMState Current => current;
    public string StateName => current?.GetType().Name ?? "None";

    public void Init(GameObject owner, FSMState initialState)
    {
        this.owner = owner;
        current = initialState;
        current.OnEnter(owner);
    }

    public void ChangeState(FSMState next)
    {
        if (next == null || next == current) return;
        current?.OnExit(owner);
        current = next;
        current.OnEnter(owner);
    }

    public void Tick()
    {
        current?.OnUpdate(owner);
    }
}
