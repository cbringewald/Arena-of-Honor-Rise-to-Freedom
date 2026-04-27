using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    [Header("Footsteps Audio")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private float walkPitch = 1f;
    [SerializeField] private float runPitch = 1.5f;

private float nextStepTime;
    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float blockMoveSpeed = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Salto y gravedad")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedForce = -2f;

    [Header("Jump Control")]
    [SerializeField] private float jumpCooldown = 0.35f;
    [SerializeField] private float groundedGraceTime = 0.15f;

    [Header("Restricciones")]
    [SerializeField] private bool blockMovementWhileAttacking = true;
    [SerializeField] private bool allowJumpWhileBlocking = false;

    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerBlock playerBlock;
    [SerializeField] private PlayerDodge playerDodge;
    [SerializeField] private Health health;

    [Header("Animator Parameters")]
    [SerializeField] private string walkParameter = "Walk";
    [SerializeField] private string runParameter = "Run";
    [SerializeField] private string jumpParameter = "Jump";
    [SerializeField] private string groundedParameter = "IsGrounded";

    private CharacterController controller;
    private Vector3 verticalVelocity;

    private int walkHash;
    private int runHash;
    private int jumpHash;
    private int groundedHash;
    private bool isJumping;
    private float lastJumpTime;
    private float lastGroundedTime;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerCombat == null)
            playerCombat = GetComponent<PlayerCombat>();

        if (playerBlock == null)
            playerBlock = GetComponent<PlayerBlock>();

        if (playerDodge == null)
            playerDodge = GetComponent<PlayerDodge>();

        if (health == null)
            health = GetComponent<Health>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        walkHash = Animator.StringToHash(walkParameter);
        runHash = Animator.StringToHash(runParameter);
        jumpHash = Animator.StringToHash(jumpParameter);
        groundedHash = Animator.StringToHash(groundedParameter);
    }

    private void Update()
    {
        if (controller == null || !controller.enabled || !gameObject.activeInHierarchy)
            return;

        if (health != null && health.IsDead)
        {
            SetMovementAnimator(false, false);
            return;
        }

        if (controller.isGrounded)
        {
            lastGroundedTime = Time.time;

        if (Time.time > lastJumpTime + jumpCooldown && verticalVelocity.y <= 0f)
            isJumping = false;
        }

        HandleJump();
        HandleMovement();
        ApplyGravity();
        UpdateGroundedAnimator();
    }

    private void HandleMovement()
    {
        if (cameraTransform == null)
            return;

        if (playerDodge != null && playerDodge.IsDodging)
        {
            SetMovementAnimator(false, false);
            HandleFootsteps(false, false);
            return;
        }

        if (blockMovementWhileAttacking && playerCombat != null && playerCombat.IsAttacking)
        {
            SetMovementAnimator(false, false);
            HandleFootsteps(false, false);
            return;
        }

        Vector2 moveInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
        }

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        
        bool hasInput = moveInput.sqrMagnitude > 0.01f;
        bool wantsRun = hasInput && Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        bool isBlocking = playerBlock != null && playerBlock.IsBlocking;

        if (hasInput)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            float speed = walkSpeed;

            if (isBlocking)
                speed = blockMoveSpeed;
            else if (wantsRun)
                speed = runSpeed;

            controller.Move(moveDirection * speed * Time.deltaTime);
        }

        bool isRunning = hasInput && wantsRun && !isBlocking;
        bool isWalking = hasInput && !isRunning;
        bool movementKeyPressed =
            Keyboard.current != null &&
            (Keyboard.current.wKey.isPressed ||
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.dKey.isPressed);

        bool shouldPlayFootsteps =
            movementKeyPressed &&
            !isBlocking &&
            (playerCombat == null || !playerCombat.IsAttacking) &&
            (playerDodge == null || !playerDodge.IsDodging);

        HandleFootsteps(shouldPlayFootsteps, isRunning);
        SetMovementAnimator(isWalking, isRunning);
    }

    private void HandleJump()
    {
        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.spaceKey.wasPressedThisFrame)
            return;

        if (isJumping)
            return;

        if (Time.time < lastJumpTime + jumpCooldown)
            return;

        bool canJump = Time.time <= lastGroundedTime + groundedGraceTime;

        if (!canJump)
            return;

        if (playerDodge != null && playerDodge.IsDodging)
            return;

        if (playerCombat != null && playerCombat.IsAttacking)
            return;

        if (!allowJumpWhileBlocking && playerBlock != null && playerBlock.IsBlocking)
            return;

        isJumping = true;
        lastJumpTime = Time.time;

        verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        SetMovementAnimator(false, false);
        
        if (animator != null)
        {
            animator.ResetTrigger(jumpHash);
            animator.SetTrigger(jumpHash);
        }
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = groundedForce;

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private void UpdateGroundedAnimator()
    {
        if (animator == null || controller == null)
            return;

        animator.SetBool(groundedHash, controller.isGrounded);
    }

    private void SetMovementAnimator(bool walk, bool run)
    {
        if (animator == null)
            return;

        animator.SetBool(walkHash, walk);
        animator.SetBool(runHash, run);
    }

    private void HandleFootsteps(bool shouldPlay, bool isRunning)
    {
        if (footstepSource == null)
            return;

        footstepSource.loop = true;
        footstepSource.pitch = isRunning ? runPitch : walkPitch;

        if (shouldPlay)
        {
            if (!footstepSource.isPlaying)
            {
                if (footstepSource.time > 0f)
                    footstepSource.UnPause();
                else
                    footstepSource.Play();
            }
        }
        else
        {
            if (footstepSource.isPlaying)
                footstepSource.Pause();
        }
    }
}