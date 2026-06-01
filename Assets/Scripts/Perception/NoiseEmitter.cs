using UnityEngine;

[DisallowMultipleComponent]
public class NoiseEmitter : MonoBehaviour
{
    public float loudness = 8f;
    public float emitInterval = 0.5f;
    public float speedThreshold = 2.0f;

    Vector3 lastPos;
    float timer;

    void Start()
    {
        lastPos = transform.position;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < emitInterval) return;

        float dt = Mathf.Max(timer, 0.0001f);
        float speed = (transform.position - lastPos).magnitude / dt;
        lastPos = transform.position;
        timer = 0f;

        if (speed >= speedThreshold)
            SoundEvent.Emit(transform.position, loudness);
    }

    public void EmitBurst(float burstLoudness)
    {
        SoundEvent.Emit(transform.position, burstLoudness);
    }
}
