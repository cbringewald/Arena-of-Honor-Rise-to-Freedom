using UnityEngine;
using UnityEngine.UI;

public class HealthBarWorld : MonoBehaviour
{
    public Health health;
    public Slider slider;
    public Vector3 offset = new Vector3(0f, 2f, 0f);

    void Start()
    {
        if (slider == null) slider = GetComponentInChildren<Slider>();

        if (health != null)
        {
            slider.value = health.Normalized();
            health.onHealthChanged.AddListener(OnHealthChanged);
        }
    }

    void LateUpdate()
    {
        if (health == null) return;

        transform.position = health.transform.position + offset;

        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward; // billboard
    }

    void OnHealthChanged(float current, float max)
    {
        slider.value = (max <= 0f) ? 0f : current / max;
    }
}
