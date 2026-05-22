using System.Collections.Generic;
using UnityEngine;

public class EnemyWeaponHitbox : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private Transform ownerRoot;

    [Header("Audio On Successful Hit")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hitSounds;
    [Range(0f, 1f)] [SerializeField] private float hitVolume = 1f;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    private readonly HashSet<Health> damagedTargets = new HashSet<Health>();
    private bool swingActive;

    public int Damage => damage;

    private void Awake()
    {
        if (ownerRoot == null)
            ownerRoot = transform.root;

        if (audioSource == null)
            audioSource = GetComponentInParent<AudioSource>();
    }

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

    public void SetDamage(int newDamage)
    {
        damage = Mathf.Max(1, newDamage);
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
                Debug.Log($"Golpe bloqueado por {health.gameObject.name}. Dano final: {finalDamage}");
        }

        if (finalDamage > 0)
        {
            health.TakeDamage(finalDamage, "EnemyWeapon");
            PlayHitSound();
            Debug.Log($"EnemyWeaponHitbox dano a {health.gameObject.name} por {finalDamage}");
        }

        damagedTargets.Add(health);
    }

    private void PlayHitSound()
    {
        if (audioSource == null || hitSounds == null || hitSounds.Length == 0)
            return;

        AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];

        if (clip == null)
            return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, hitVolume);
    }
}
