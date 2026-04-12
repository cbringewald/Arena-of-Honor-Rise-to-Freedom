using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public UnityEvent<float, float> onHealthChanged; // (current, max)
    public UnityEvent onDied;

    void Awake()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        currentHealth -= amount;
        if (currentHealth < 0f) currentHealth = 0f;

        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            onDied?.Invoke();
    }

    public float Normalized() => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
}
