using System.Collections.Generic;
using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private Transform ownerRoot;

    private readonly HashSet<Health> damagedTargets = new HashSet<Health>();

    public void StartSwing()
    {
        damagedTargets.Clear();
        Debug.Log("Nuevo swing - lista reseteada");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter con: " + other.name);
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("OnTriggerExit con: " + other.name);
    }

    private void TryDamage(Collider other)
    {
        if (ownerRoot != null && other.transform.root == ownerRoot)
            return;

        Health health = other.GetComponentInParent<Health>();

        if (health == null)
        {
            Debug.Log("No tiene Health: " + other.name);
            return;
        }

        if (damagedTargets.Contains(health))
            return;

        Debug.Log("Daño aplicado a: " + health.gameObject.name);
        health.TakeDamage(damage);
        damagedTargets.Add(health);
    }
}