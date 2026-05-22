using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private Renderer rend;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float flashTime = 0.1f;

    [Header("Death")]
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTriggerName = "Die";
    [SerializeField] private string hitTriggerName = "Hit";
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private bool useDeathCamera = false;
    [Header("Combat")]
    [SerializeField] private bool isEnemy = false;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    private int currentHealth;
    private Color originalColor;
    private bool isDead;
    private DeathCinemachineController deathCam;
    private Coroutine hitFlashCoroutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float NormalizedHealth => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    public bool IsDead => isDead;

    public bool IsInvincible { get; set; }

    public event Action<int, int> OnHealthChanged;
    public event Action<int, string> OnDamaged;
    public event Action OnDied;
    

    private void Awake()
    {
        currentHealth = maxHealth;

        if (rend == null)
            rend = GetComponentInChildren<Renderer>();

        if (rend != null)
            originalColor = rend.material.color;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        deathCam = FindFirstObjectByType<DeathCinemachineController>();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (isEnemy && CombatManager.Instance != null)
            CombatManager.Instance.RegisterEnemy(this);
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, "Unknown");
    }

    public void TakeDamage(int damage, string hitZone)
    {
        if (currentHealth <= 0 || isDead) return;
        if (IsInvincible) return;
        if (damage <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} recibió {damage} de daño en {hitZone}. Vida: {currentHealth}");

        if (audioSource != null && hurtSound != null)
            audioSource.PlayOneShot(hurtSound);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Si el golpe mata, NO disparamos OnDamaged para evitar que la IA lance Hit
        // y compita con la animación de muerte.
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (rend != null)
        {
            if (hitFlashCoroutine != null)
                StopCoroutine(hitFlashCoroutine);

            hitFlashCoroutine = StartCoroutine(HitFlash());
        }

        if (animator != null && !string.IsNullOrEmpty(hitTriggerName))
        {
            animator.ResetTrigger(hitTriggerName);
            animator.SetTrigger(hitTriggerName);
        }

        OnDamaged?.Invoke(damage, hitZone);
    }

    public void SetMaxHealth(int newMaxHealth, bool refillHealth)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);

        if (refillHealth)
            currentHealth = maxHealth;
        else
            currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(int amount)
    {
        if (currentHealth <= 0 || isDead) return;
        if (amount <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetUseDeathCamera(bool value)
    {
        useDeathCamera = value;
    }

    public void RestoreToFull()
    {
        if (currentHealth <= 0 || isDead) return;

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private IEnumerator HitFlash()
    {
        rend.material.color = hitColor;
        yield return new WaitForSeconds(flashTime);

        if (rend != null)
            rend.material.color = originalColor;

        hitFlashCoroutine = null;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        Debug.Log(gameObject.name + " -> Die() llamada");

        // Parar cualquier flash activo para no pisar materiales al morir
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }

        if (rend != null)
            rend.material.color = originalColor;

        // Desactivar IA antes de lanzar la muerte
        BotAI botAI = GetComponent<BotAI>();
        if (botAI != null)
            botAI.enabled = false;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            if (agent.isOnNavMesh)
                agent.isStopped = true;

            agent.enabled = false;
        }

        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        PlayerMove playerMove = GetComponent<PlayerMove>();
        if (playerMove != null)
            playerMove.enabled = false;

        PlayerCombat playerCombat = GetComponent<PlayerCombat>();
        if (playerCombat != null)
            playerCombat.enabled = false;

        PlayerBlock playerBlock = GetComponent<PlayerBlock>();
        if (playerBlock != null)
            playerBlock.enabled = false;

        PlayerDodge playerDodge = GetComponent<PlayerDodge>();
        if (playerDodge != null)
            playerDodge.enabled = false;

        PlayerDefense playerDefense = GetComponent<PlayerDefense>();
        if (playerDefense != null)
            playerDefense.enabled = false;

        OnDied?.Invoke();

        if (animator != null)
        {
            Debug.Log("Trigger de muerte enviado al Animator: " + animator.name);

            // Limpiamos posibles triggers que estén compitiendo
            if (!string.IsNullOrEmpty(hitTriggerName))
                animator.ResetTrigger(hitTriggerName);

            if (!string.IsNullOrEmpty(attackTriggerName))
                animator.ResetTrigger(attackTriggerName);

            animator.ResetTrigger(deathTriggerName);
            animator.SetTrigger(deathTriggerName);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: no hay Animator asignado en Health.");
        }

        if (useDeathCamera && deathCam != null)
        {
            deathCam.FocusOnDeadEnemy(transform);
        }
    }
}
