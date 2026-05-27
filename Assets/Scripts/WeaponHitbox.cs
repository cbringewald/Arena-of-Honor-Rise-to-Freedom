using System.Collections.Generic;
using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private Transform ownerRoot;

    [Header("Impact Feedback")]
    [SerializeField] private bool useHitstop = true;
    [SerializeField] private float hitstopDuration = 0.04f;
    [SerializeField] private float hitstopTimeScale = 0.08f;

    [SerializeField] private bool useCameraShake = true;
    [SerializeField] private float cameraShakeDuration = 0.08f;
    [SerializeField] private float cameraShakeMagnitude = 0.08f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;

    private readonly HashSet<Health> damagedTargets = new HashSet<Health>();
    private bool swingActive;
    private int baseDamage;
    private static float lastHitstopTime;
    private const float hitstopCooldown = 0.15f;

    public int Damage => damage;
    public int BaseDamage => baseDamage;

    private void Awake()
    {
        baseDamage = Mathf.Max(1, damage);
        damage = baseDamage;
    }

    public void SetDamage(int newDamage)
    {
        damage = Mathf.Max(1, newDamage);
    }

    public void StartSwing()
    {
        if (swingActive) return;

        swingActive = true;
        damagedTargets.Clear();

        Debug.Log("StartSwing en " + gameObject.name);
    }

    public void EndSwing()
    {
        swingActive = false;
        damagedTargets.Clear();

        Debug.Log("Swing terminado en " + gameObject.name);
    }

    public void SetOwnerRoot(Transform newOwnerRoot)
    {
        ownerRoot = newOwnerRoot;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Arma ha tocado: " + other.name + " con " + gameObject.name);

        if (!swingActive)
        {
            Debug.Log("Toca, pero swingActive está en false");
            return;
        }

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
        {
            Debug.Log("Ignorado porque pertenece al dueño del arma: " + other.name);
            return;
        }

        DamageZone zone = other.GetComponent<DamageZone>();

        if (zone != null && zone.Health != null)
        {
            Health targetHealth = zone.Health;

            if (damagedTargets.Contains(targetHealth))
                return;

            int finalDamage = zone.GetFinalDamage(damage);

            Debug.Log($"Daño aplicado a: {targetHealth.gameObject.name} | Zona: {zone.ZoneName} | Daño final: {finalDamage}");

            ApplyImpactFeedback();

            zone.ApplyDamage(damage, ownerRoot);
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

        ApplyImpactFeedback();

        health.TakeDamage(damage, "Default");
        damagedTargets.Add(health);
    }


    private void ApplyImpactFeedback()
    {
        if (useHitstop && HitstopManager.Instance != null)
        {
            if (Time.unscaledTime >= lastHitstopTime + hitstopCooldown)
            {
                lastHitstopTime = Time.unscaledTime;
                HitstopManager.Instance.DoHitstop(hitstopDuration, hitstopTimeScale);
            }
        }

        if (useCameraShake && CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(cameraShakeDuration, cameraShakeMagnitude);
        }

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);
    }
}
