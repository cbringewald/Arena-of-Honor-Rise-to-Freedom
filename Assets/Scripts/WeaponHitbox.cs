using System.Collections.Generic;
using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private Transform ownerRoot;

    private readonly HashSet<Health> damagedTargets = new HashSet<Health>();
    private bool swingActive;

    public void StartSwing()
    {
        if (swingActive) return;

        swingActive = true;
        damagedTargets.Clear();
        Debug.Log("Nuevo swing - lista reseteada");
    }

    public void EndSwing()
    {
        swingActive = false;
        damagedTargets.Clear();
        Debug.Log("Swing terminado");
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

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("OnTriggerExit con: " + other.name);
    }

    private void TryDamage(Collider other)
    {
        if (ownerRoot != null && other.transform.root == ownerRoot)
            return;

        DamageZone zone = other.GetComponent<DamageZone>();

        if (zone != null && zone.Health != null)
        {
            Health targetHealth = zone.Health;

            if (damagedTargets.Contains(targetHealth))
                return;

            int finalDamage = zone.GetFinalDamage(damage);

            Debug.Log($"Daño aplicado a: {targetHealth.gameObject.name} | Zona: {zone.ZoneName} | Daño final: {finalDamage}");

            targetHealth.TakeDamage(finalDamage, zone.ZoneName);
            damagedTargets.Add(targetHealth);
            return;
        }

        Health health = other.GetComponentInParent<Health>();

        if (health == null)
        {
            Debug.Log("No tiene Health: " + other.name);
            return;
        }

        if (damagedTargets.Contains(health))
            return;

        Debug.Log($"Daño aplicado a: {health.gameObject.name} | Zona: Default | Daño: {damage}");
        health.TakeDamage(damage, "Default");
        damagedTargets.Add(health);
    }
}