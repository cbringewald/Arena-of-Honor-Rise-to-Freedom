using System;
using UnityEngine;

public class Stamina : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenRate = 20f;
    [SerializeField] private float regenDelay = 1f;

    private float currentStamina;
    private float lastUseTime;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float NormalizedStamina => maxStamina > 0 ? currentStamina / maxStamina : 0f;

    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        if (Time.time >= lastUseTime + regenDelay)
        {
            Regenerate();
        }
    }

    public bool HasEnough(float amount)
    {
        return currentStamina >= amount;
    }

    public bool UseStamina(float amount)
    {
        if (amount <= 0f) return true;
        if (currentStamina < amount) return false;

        currentStamina -= amount;
        currentStamina = Mathf.Max(currentStamina, 0f);
        lastUseTime = Time.time;

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        return true;
    }

    public void Restore(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    private void Regenerate()
    {
        if (currentStamina >= maxStamina) return;

        currentStamina += regenRate * Time.deltaTime;
        currentStamina = Mathf.Min(currentStamina, maxStamina);

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}