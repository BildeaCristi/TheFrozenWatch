using UnityEngine;

public abstract class FSMState
{
    public abstract void OnEnter(GameObject owner);
    public abstract void OnUpdate(GameObject owner);
    public abstract void OnExit(GameObject owner);
}
