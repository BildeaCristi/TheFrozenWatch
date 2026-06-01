using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class AIUpdateScheduler : MonoBehaviour
{
    [Header("LOD Thresholds")]
    public float nearRadius = 20f;
    public float nearRate   = 0.1f;
    public float farRate    = 1.0f;

    Transform reference;
    FieldOfView fov;

    void Start()
    {
        fov = GetComponent<FieldOfView>();
        if (Camera.main != null) reference = Camera.main.transform;
        StartCoroutine(ScheduleLoop());
    }

    IEnumerator ScheduleLoop()
    {
        // Stagger initial offset so agents don't all scan the same frame
        yield return new WaitForSeconds(Random.Range(0f, 0.5f));
        while (true)
        {
            if (fov != null && reference != null)
            {
                float dist = Vector3.Distance(transform.position, reference.position);
                fov.checkInterval = dist > nearRadius ? farRate : nearRate;
            }
            yield return new WaitForSeconds(0.25f);
        }
    }
}
