using UnityEngine;
using UnityEngine.InputSystem;

public class TunnelMapCameraToggle : MonoBehaviour
{
    public Camera mainCamera;
    public Camera tunnelMapCamera;
    public Key toggleKey = Key.M;

    public static bool MapActive { get; private set; }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (tunnelMapCamera != null) tunnelMapCamera.enabled = false;
        MapActive = false;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || tunnelMapCamera == null) return;
        if (kb[toggleKey].wasPressedThisFrame)
        {
            bool showMap = !tunnelMapCamera.enabled;
            tunnelMapCamera.enabled = showMap;
            if (mainCamera != null) mainCamera.enabled = !showMap;
            MapActive = showMap;
            Debug.Log(showMap ? "TUNNEL TACTICAL MAP: ON (press M to exit)" : "TUNNEL TACTICAL MAP: OFF");
        }
    }
}
