using System;
using UnityEngine;

public static class SoundEvent
{
    public static event Action<Vector3, float> OnNoise;

    public static void Emit(Vector3 position, float loudness)
    {
        OnNoise?.Invoke(position, loudness);
    }

    public static void ClearSubscribers()
    {
        OnNoise = null;
    }
}
