using UnityEngine;

public class AgentBlackboard : MonoBehaviour
{
    public float health = 100f;
    public float maxHealth = 100f;
    public bool isUnderFire = false;
    public Transform lastThreatPos;
    public Transform nearestCover;

    public float HealthFraction => health / Mathf.Max(maxHealth, 1f);
    public bool IsLowHealth(float threshold = 0.3f) => HealthFraction < threshold;

    public void TakeDamage(float amount)
    {
        health = Mathf.Max(0f, health - amount);
    }

    public void Heal(float amount)
    {
        health = Mathf.Min(maxHealth, health + amount);
    }
}
