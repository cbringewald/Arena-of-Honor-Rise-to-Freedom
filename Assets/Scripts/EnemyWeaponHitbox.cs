using System.Collections.Generic;
using UnityEngine;

public class EnemyWeaponHitbox : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private Transform ownerRoot;

    private readonly HashSet<Health> damagedTargets = new HashSet<Health>();
    private bool swingActive;

    public void StartSwing()
    {
        if (swingActive) return;

        swingActive = true;
        damagedTargets.Clear();
        Debug.Log("Enemy swing iniciado");
    }

    public void EndSwing()
    {
        swingActive = false;
        damagedTargets.Clear();
        Debug.Log("Enemy swing finalizado");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!swingActive) return;
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!swingActive) return;
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        if (ownerRoot != null && other.transform.root == ownerRoot)
            return;

        Health health = other.GetComponentInParent<Health>();
        if (health == null)
            return;

        if (damagedTargets.Contains(health))
            return;

        int finalDamage = damage;

        PlayerDefense defense = health.GetComponent<PlayerDefense>();
        if (defense != null)
        {
            bool blocked = defense.TryBlockHit(ownerRoot, ref finalDamage);

            if (blocked)
            {
                Debug.Log($"Golpe bloqueado por {health.gameObject.name}. Daño final: {finalDamage}");
            }
        }

        if (finalDamage > 0)
        {
            health.TakeDamage(finalDamage, "EnemyWeapon");
            Debug.Log($"EnemyWeaponHitbox dañó a {health.gameObject.name} por {finalDamage}");
        }

        damagedTargets.Add(health);
    }
}
