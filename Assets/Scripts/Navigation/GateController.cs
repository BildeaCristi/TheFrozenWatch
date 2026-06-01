using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshObstacle))]
public class GateController : MonoBehaviour
{
    public Key toggleKey = Key.G;
    public float openLiftHeight = 6f;
    public bool startOpen = false;

    private NavMeshObstacle obstacle;
    private bool isOpen = false;
    private Vector3 closedPosition;

    public bool IsOpen => isOpen;

    void Start()
    {
        obstacle = GetComponent<NavMeshObstacle>();
        obstacle.carving = true;
        closedPosition = transform.position;
        if (startOpen) ToggleGate();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
            ToggleGate();
    }

    public void ToggleGate()
    {
        isOpen = !isOpen;
        obstacle.carving = !isOpen;
        transform.position = isOpen
            ? closedPosition + Vector3.up * openLiftHeight
            : closedPosition;

        Debug.Log($"Gate [{name}]: {(isOpen ? "OPEN" : "CLOSED")}");
    }
}
