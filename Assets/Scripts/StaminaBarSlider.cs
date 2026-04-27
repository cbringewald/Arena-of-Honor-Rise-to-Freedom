using UnityEngine;
using UnityEngine.UI;

public class StaminaBarUI : MonoBehaviour
{
    [SerializeField] private Stamina stamina;
    [SerializeField] private Slider slider;

    private void OnEnable()
    {
        if (stamina != null)
        {
            stamina.OnStaminaChanged += UpdateStamina;
            UpdateStamina(stamina.CurrentStamina, stamina.MaxStamina);
        }
    }

    private void OnDisable()
    {
        if (stamina != null)
            stamina.OnStaminaChanged -= UpdateStamina;
    }

    private void UpdateStamina(float current, float max)
    {
        slider.value = max > 0 ? current / max : 0f;
    }
}