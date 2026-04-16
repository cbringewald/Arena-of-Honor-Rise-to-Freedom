using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private Renderer rend;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float flashTime = 0.1f;

    [Header("Death")]
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTriggerName = "Die";

    private int currentHealth;
    private Color originalColor;
    private bool isDead;
    private DeathCinemachineController deathCam;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float NormalizedHealth => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    public bool IsDead => isDead;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;
    [SerializeField] private Collider shieldCollider;

    

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
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0 || isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} vida: {currentHealth}");

        if (rend != null)
            StartCoroutine(HitFlash());

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (currentHealth <= 0 || isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private IEnumerator HitFlash()
    {
        rend.material.color = hitColor;
        yield return new WaitForSeconds(flashTime);
        rend.material.color = originalColor;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        OnDied?.Invoke();

        if (animator != null)
        {
            animator.ResetTrigger(deathTriggerName);
            animator.SetTrigger(deathTriggerName);
        }

        if (deathCam != null)
        {
            deathCam.FocusOnDeadEnemy(transform);
        }

        // Desactivar IA
        BotAI ai = GetComponent<BotAI>();
        if (ai != null)
            ai.enabled = false;

        // Desactivar navegación
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
            agent.enabled = false;

        // Desactivar CharacterController si lo usa
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        // Mantener el cadáver físico para poder moverlo
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        if (shieldCollider != null)
            shieldCollider.enabled = false;
        }
}