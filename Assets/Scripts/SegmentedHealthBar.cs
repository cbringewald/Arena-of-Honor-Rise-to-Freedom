using UnityEngine;
using UnityEngine.UI;

public class SegmentedHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health targetHealth;
    [SerializeField] private Image[] segments;

    [Header("Colors")]
    [SerializeField] private Color fullColor = new Color(0.75f, 0f, 0f);
    [SerializeField] private Color mediumColor = new Color(1f, 0.45f, 0f);
    [SerializeField] private Color lowColor = new Color(0.45f, 0f, 0f);
    [SerializeField] private Color emptyColor = new Color(0.08f, 0.02f, 0.02f);

    [Header("Settings")]
    [SerializeField] private bool useGradientByTotalHealth = true;

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
            UpdateBar(targetHealth.CurrentHealth, targetHealth.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (targetHealth != null)
            targetHealth.OnHealthChanged -= UpdateBar;
    }

    private void UpdateBar(int current, int max)
    {
        if (segments == null || segments.Length == 0)
            return;

        float normalized = max > 0 ? (float)current / max : 0f;
        int activeSegments = Mathf.CeilToInt(normalized * segments.Length);

        Color activeColor = GetColorByHealth(normalized);

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == null)
                continue;

            bool isActive = i < activeSegments;

            if (isActive)
            {
                segments[i].enabled = true;
                segments[i].color = useGradientByTotalHealth ? activeColor : fullColor;
            }
            else
            {
                segments[i].enabled = true;
                segments[i].color = emptyColor;
            }
        }
    }

    private Color GetColorByHealth(float normalized)
    {
        if (normalized <= 0.3f)
            return lowColor;

        if (normalized <= 0.6f)
            return mediumColor;

        return fullColor;
    }
}
