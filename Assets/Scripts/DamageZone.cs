using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private string zoneName = "Body";
    [SerializeField] private float damageMultiplier = 1f;

    private Health health;

    public Health Health => health;
    public float DamageMultiplier => damageMultiplier;
    public string ZoneName => zoneName;

    private void Awake()
    {
        health = GetComponentInParent<Health>();

        if (health == null)
        {
            Debug.LogWarning($"DamageZone en {gameObject.name} no encontró un Health en el padre.");
        }
    }

    public int GetFinalDamage(int baseDamage)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * damageMultiplier));
    }
}