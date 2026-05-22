using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDodge : MonoBehaviour
{
    private enum DodgeDirection
    {
        Forward = 0,
        Backward = 1,
        Left = 2,
        Right = 3
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Stamina stamina;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Health health;

    [Header("Dodge Settings")]
    [SerializeField] private float dodgeCost = 18f;
    [SerializeField] private float dodgeDuration = 0.45f;
    [SerializeField] private Key dodgeKey = Key.Q;

    [Header("I-Frames")]
    [SerializeField] private bool useInvincibilityFrames = true;

    [Header("Animator Parameters")]
    [SerializeField] private string dodgeTrigger = "Dodge";
    [SerializeField] private string dodgingBool = "IsDodging";
    [SerializeField] private string dodgeDirectionParameter = "DodgeDirection";

    private PlayerCombat playerCombat;
    private PlayerBlock playerBlock;

    private int dodgeTriggerHash;
    private int dodgingBoolHash;
    private int dodgeDirectionHash;

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
        dodgeDirectionHash = Animator.StringToHash(dodgeDirectionParameter);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[dodgeKey].wasPressedThisFrame)
            TryDodge();
    }

    public void TryDodge()
    {
        if (health != null && health.IsDead)
            return;

        if (IsDodging)
            return;

        if (playerCombat != null && playerCombat.IsAttacking)
            return;

        if (playerBlock != null && playerBlock.IsBlocking)
            return;

        if (characterController != null && !characterController.isGrounded)
            return;

        if (stamina != null && !stamina.UseStamina(dodgeCost))
        {
            Debug.Log("Sin stamina para esquivar");
            return;
        }

        DodgeDirection animationDirection = GetDodgeAnimationDirection();

        StartCoroutine(DodgeRoutine(animationDirection));
    }

    private DodgeDirection GetDodgeAnimationDirection()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
        }

        if (input.sqrMagnitude < 0.01f)
            return DodgeDirection.Forward;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return input.x > 0f ? DodgeDirection.Right : DodgeDirection.Left;

        return input.y > 0f ? DodgeDirection.Forward : DodgeDirection.Backward;
    }

    private IEnumerator DodgeRoutine(DodgeDirection animationDirection)
    {
        IsDodging = true;

        if (useInvincibilityFrames && health != null)
            health.IsInvincible = true;

        if (animator != null)
        {
            animator.SetBool(dodgingBoolHash, true);
            animator.SetInteger(dodgeDirectionHash, (int)animationDirection);

            animator.ResetTrigger(dodgeTriggerHash);
            animator.SetTrigger(dodgeTriggerHash);
        }

        yield return new WaitForSeconds(dodgeDuration);

        IsDodging = false;

        if (useInvincibilityFrames && health != null)
            health.IsInvincible = false;

        if (animator != null)
            animator.SetBool(dodgingBoolHash, false);
    }
}