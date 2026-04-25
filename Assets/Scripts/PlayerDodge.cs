using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDodge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Stamina stamina;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Health health;

    [Header("Dodge Settings")]
    [SerializeField] private float dodgeCost = 25f;
    [SerializeField] private float dodgeDistance = 3f;
    [SerializeField] private float dodgeDuration = 0.2f;
    [SerializeField] private Key dodgeKey = Key.Q;

    [Header("I-Frames")]
    [SerializeField] private bool useInvincibilityFrames = true;

    [Header("Animator Parameters")]
    [SerializeField] private string dodgeTrigger = "Dodge";
    [SerializeField] private string dodgingBool = "IsDodging";

    private PlayerCombat playerCombat;
    private PlayerBlock playerBlock;

    private int dodgeTriggerHash;
    private int dodgingBoolHash;

    public bool IsDodging { get; private set; }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (stamina == null)
            stamina = GetComponent<Stamina>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (health == null)
            health = GetComponent<Health>();

        playerCombat = GetComponent<PlayerCombat>();
        playerBlock = GetComponent<PlayerBlock>();

        dodgeTriggerHash = Animator.StringToHash(dodgeTrigger);
        dodgingBoolHash = Animator.StringToHash(dodgingBool);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[dodgeKey].wasPressedThisFrame)
        {
            TryDodge();
        }
    }

    public void TryDodge()
    {
        if (IsDodging) return;
        if (playerCombat != null && playerCombat.IsAttacking) return;
        if (playerBlock != null && playerBlock.IsBlocking) return;
        if (characterController != null && !characterController.isGrounded) return;

        if (stamina != null && !stamina.UseStamina(dodgeCost))
        {
            Debug.Log("Sin stamina para esquivar");
            return;
        }

        StartCoroutine(DodgeRoutine());
    }

    private IEnumerator DodgeRoutine()
    {
        IsDodging = true;

        if (useInvincibilityFrames && health != null)
            health.IsInvincible = true;

        if (animator != null)
        {
            animator.SetBool(dodgingBoolHash, true);
            animator.SetTrigger(dodgeTriggerHash);
        }

        Vector3 direction = transform.forward;
        float elapsed = 0f;
        float speed = dodgeDistance / dodgeDuration;

        while (elapsed < dodgeDuration)
        {
            elapsed += Time.deltaTime;

            if (characterController != null && characterController.enabled)
                characterController.Move(direction * speed * Time.deltaTime);
            else
                transform.position += direction * speed * Time.deltaTime;

            yield return null;
        }

        IsDodging = false;

        if (useInvincibilityFrames && health != null)
            health.IsInvincible = false;

        if (animator != null)
            animator.SetBool(dodgingBoolHash, false);
    }
}
