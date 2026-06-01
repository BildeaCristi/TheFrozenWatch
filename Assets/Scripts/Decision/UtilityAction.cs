using UnityEngine;

public abstract class UtilityAction : MonoBehaviour
{
    public string actionName;
    public abstract float Evaluate();
    public abstract void Execute();
}
