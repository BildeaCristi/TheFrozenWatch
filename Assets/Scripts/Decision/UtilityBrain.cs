using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UtilityBrain : MonoBehaviour
{
    public List<UtilityAction> availableActions = new List<UtilityAction>();
    public float hysteresis = 0.1f;

    public UtilityAction CurrentAction => currentAction;
    public string ActionName => currentAction != null ? currentAction.actionName : "None";

    UtilityAction currentAction;

    void Update()
    {
        ChooseBestAction();
    }

    void ChooseBestAction()
    {
        if (availableActions == null || availableActions.Count == 0) return;

        UtilityAction best = null;
        float bestScore = -1f;

        foreach (var action in availableActions)
        {
            if (action == null) continue;
            float score = action.Evaluate();
            if (score > bestScore) { bestScore = score; best = action; }
        }

        if (best == null) return;

        float currentScore = currentAction != null ? currentAction.Evaluate() : -1f;
        if (best != currentAction && bestScore > currentScore + hysteresis)
        {
            currentAction = best;
            currentAction.Execute();
            Debug.Log($"[UtilityBrain:{name}] -> {currentAction.actionName} (score {bestScore:F2})");
        }
    }
}
