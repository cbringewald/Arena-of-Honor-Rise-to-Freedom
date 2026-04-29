using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyAudioFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private AudioSource audioSource;

    [Header("Hit Sounds")]
    [SerializeField] private AudioClip[] hitSounds;

    [Header("Settings")]
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;
    [SerializeField] private bool use3DSound = true;
    [SerializeField] private bool playAtPosition = false;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = use3DSound ? 1f : 0f;
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged(int damage, string hitZone)
    {
        PlayRandomHitSound();
    }

    private void PlayRandomHitSound()
    {
        if (hitSounds == null || hitSounds.Length == 0)
            return;

        AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];

        if (clip == null)
            return;

        if (playAtPosition)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
            return;
        }

        if (audioSource == null)
            return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, volume);
    }
}
