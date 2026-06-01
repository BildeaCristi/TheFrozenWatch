using UnityEngine;
using UnityEngine.InputSystem;

public class PiPCameraToggle : MonoBehaviour
{
    public Key toggleKey = Key.T;
    public bool startEnabled = false;

    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null) cam.enabled = startEnabled;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || cam == null) return;
        if (kb[toggleKey].wasPressedThisFrame) cam.enabled = !cam.enabled;
    }
}
