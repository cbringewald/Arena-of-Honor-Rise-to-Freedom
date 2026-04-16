using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Health targetHealth;
    [SerializeField] private Image fillImage;
    [SerializeField] private bool hideWhenDead = true;

    private void Awake()
    {
        if (targetHealth == null)
            targetHealth = GetComponentInParent<Health>();
    }

    private void OnEnable()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged += UpdateBar;
            targetHealth.OnDied += HandleDeath;
            UpdateBar(targetHealth.CurrentHealth, targetHealth.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= UpdateBar;
            targetHealth.OnDied -= HandleDeath;
        }
    }

    private void UpdateBar(int current, int max)
    {
        if (fillImage == null) return;

        float value = max > 0 ? (float)current / max : 0f;
        fillImage.fillAmount = value;
    }

    private void HandleDeath()
    {
        if (hideWhenDead)
            gameObject.SetActive(false);
    }
}
