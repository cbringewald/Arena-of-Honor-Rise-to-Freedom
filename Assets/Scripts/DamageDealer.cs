using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    public float damage = 10f;
    public string targetTag = "Enemy"; // para el player
    public bool active = false;

    void OnTriggerEnter(Collider other)
    {
        if (!active) return;
        if (!other.CompareTag(targetTag)) return;

        var health = other.GetComponentInParent<Health>();
        if (health != null)
            health.TakeDamage(damage);
    }

    // Llamar desde Animation Events
    public void EnableDamage() => active = true;
    public void DisableDamage() => active = false;
}
