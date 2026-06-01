using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Proper third-person control for the possessed guard: an orbit camera driven by the
// mouse, camera-relative WASD movement, Shift to run. Toggle with F. While the AI drives
// the guard the camera returns to the bird's-eye command view.
[RequireComponent(typeof(NavMeshAgent))]
public class ThirdPersonController : MonoBehaviour
{
    public string agentLabel = "GUARD_1";
    public Camera targetCamera;
    public Key toggleKey = Key.F;

    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float runMultiplier = 1.9f;
    public float turnSpeed = 720f;

    [Header("Camera orbit")]
    public float mouseSensitivity = 0.18f;
    public float cameraDistance = 6.5f;
    public float lookHeight = 1.5f;
    public float minPitch = -5f;
    public float maxPitch = 65f;
    public float camLerp = 16f;
    public LayerMask camCollision = 1;   // layer 0 (Default = walls/ground), keeps camera out of geometry

    public Vector3 birdsEyePos = new Vector3(0f, 28f, -32f);
    public Vector3 birdsEyeEuler = new Vector3(42f, 0f, 0f);

    NavMeshAgent agent;
    AgentMover mover;
    GuardFSM guardFsm;
    bool playerMode;
    bool snapCam;
    float yaw;
    float pitch = 18f;

    public bool IsPlayer => playerMode;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        mover = GetComponent<AgentMover>();
        guardFsm = GetComponent<GuardFSM>();
        if (targetCamera == null) targetCamera = Camera.main;
        yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame) SetPlayerMode(!playerMode);
        if (!playerMode || kb == null) return;

        // Release the cursor while paused so the menu is clickable; lock it while playing.
        bool paused = Time.timeScale <= 0f;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
        if (paused) return;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 d = mouse.delta.ReadValue();
            yaw += d.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - d.y * mouseSensitivity, minPitch, maxPitch);
        }

        Vector3 input = Vector3.zero;
        if (kb[Key.W].isPressed) input += Vector3.forward;
        if (kb[Key.S].isPressed) input += Vector3.back;
        if (kb[Key.D].isPressed) input += Vector3.right;
        if (kb[Key.A].isPressed) input += Vector3.left;

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 dir = (Quaternion.Euler(0f, yaw, 0f) * input).normalized;
            bool running = kb[Key.LeftShift].isPressed || kb[Key.RightShift].isPressed;
            float spd = moveSpeed * (running ? runMultiplier : 1f);
            agent.Move(dir * spd * Time.deltaTime);

            float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float ny = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetYaw, turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, ny, 0f);
        }
        else if (agent != null)
        {
            // No input: kill any residual NavMesh velocity/path so the guard stands still.
            agent.velocity = Vector3.zero;
            if (agent.hasPath) agent.ResetPath();
        }
    }

    void LateUpdate()
    {
        if (!playerMode || targetCamera == null) return;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = transform.position + Vector3.up * lookHeight;
        Vector3 dir = (rot * Vector3.forward);
        float dist = cameraDistance;

        // pull the camera in if a wall is between it and the guard
        if (Physics.Raycast(focus, -dir, out var hit, cameraDistance, camCollision, QueryTriggerInteraction.Ignore))
            dist = Mathf.Max(1.2f, hit.distance - 0.3f);

        Vector3 desired = focus - dir * dist;

        if (snapCam)
        {
            targetCamera.transform.position = desired;
            targetCamera.transform.rotation = Quaternion.LookRotation(focus - desired);
            snapCam = false;
        }
        else
        {
            targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, desired, camLerp * Time.deltaTime);
            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                Quaternion.LookRotation(focus - targetCamera.transform.position),
                camLerp * Time.deltaTime);
        }
    }

    void SetPlayerMode(bool on)
    {
        playerMode = on;
        if (mover != null) mover.enabled = !on;
        if (guardFsm != null) guardFsm.enabled = !on;   // hand full control to the player

        if (on)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.updateRotation = false;
            yaw = transform.eulerAngles.y;
            pitch = 18f;
            snapCam = true;   // jump straight to the over-the-shoulder view
        }
        else
        {
            agent.updateRotation = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (targetCamera != null)
            {
                targetCamera.transform.position = birdsEyePos;
                targetCamera.transform.rotation = Quaternion.Euler(birdsEyeEuler);
            }
        }
        Debug.Log($"{agentLabel}: {(on ? "PLAYER CONTROL (WASD + mouse-look, Shift run, LMB/Space attack)" : "AI (bird's-eye)")}");
    }
}
