using UnityEngine;
using UnityEngine.UI;

public class HealthBarSlider : MonoBehaviour
{
    [SerializeField] private Health targetHealth;
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;

    [Header("Color")]
    [SerializeField] private Gradient healthGradient;

    private void Start()
    {
        if (targetHealth == null)
            targetHealth = GetComponentInParent<Health>();

        if (slider == null)
            slider = GetComponent<Slider>();

        slider.maxValue = targetHealth.MaxHealth;
        slider.value = targetHealth.CurrentHealth;

        UpdateColor();
    }

    private void OnEnable()
    {
        if (targetHealth != null)
            targetHealth.OnHealthChanged += UpdateBar;
    }

    private void OnDisable()
    {
        if (targetHealth != null)
            targetHealth.OnHealthChanged -= UpdateBar;
    }

    private void UpdateBar(int current, int max)
    {
        slider.value = current;
        UpdateColor();
    }

    private void UpdateColor()
    {
        float normalized = slider.value / slider.maxValue;
        fillImage.color = healthGradient.Evaluate(normalized);
    }
}