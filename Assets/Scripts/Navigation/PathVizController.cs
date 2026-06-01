using UnityEngine;
using UnityEngine.InputSystem;

public class PathVizController : MonoBehaviour
{
    public Key toggleKey = Key.L;
    bool visible = true;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb[toggleKey].wasPressedThisFrame)
        {
            visible = !visible;
            foreach (var v in FindObjectsByType<AgentPathViz>(FindObjectsSortMode.None))
                v.enabled = visible;
            foreach (var lr in FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
                if (lr.GetComponent<AgentPathViz>() != null) lr.enabled = visible;
            Debug.Log($"Path viz: {(visible ? "ON" : "OFF")}");
        }
    }
}
