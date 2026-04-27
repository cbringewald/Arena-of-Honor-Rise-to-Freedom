using UnityEngine;

public class EnemyAudioEvents : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swordSwingSound;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlaySwordSwing()
    {
        if (audioSource != null && swordSwingSound != null)
            audioSource.PlayOneShot(swordSwingSound);
    }
}
