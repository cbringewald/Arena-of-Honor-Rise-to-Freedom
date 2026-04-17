using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float blockMoveSpeed = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Gravedad")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedForce = -2f;

    [Header("Restricciones")]
    [SerializeField] private bool blockMovementWhileAttacking = true;

    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerBlock playerBlock;
    [SerializeField] private PlayerDodge playerDodge;

    [Header("Animator Parameters")]
    [SerializeField] private string walkParameter = "Walk";
    [SerializeField] private string runParameter = "Run";

    private CharacterController controller;
    private Vector3 verticalVelocity;

    private int walkHash;
    private int runHash;

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

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        walkHash = Animator.StringToHash(walkParameter);
        runHash = Animator.StringToHash(runParameter);
    }

    private void Update()
    {
        HandleMovement();
        ApplyGravity();
    }

    private void HandleMovement()
    {
        if (cameraTransform == null || animator == null)
            return;

        if (playerDodge != null && playerDodge.IsDodging)
        {
            SetMovementAnimator(false, false);
            return;
        }

        if (blockMovementWhileAttacking && playerCombat != null && playerCombat.IsAttacking)
        {
            SetMovementAnimator(false, false);
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

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (hasInput)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraForward * inputDirection.z + cameraRight * inputDirection.x).normalized;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            float speed;

            if (isBlocking)
            {
                speed = blockMoveSpeed;
            }
            else
            {
                speed = wantsRun ? runSpeed : walkSpeed;
            }

            controller.Move(moveDirection * speed * Time.deltaTime);
        }

        bool isRunning = hasInput && wantsRun && !isBlocking;
        bool isWalking = hasInput && !isRunning;

        SetMovementAnimator(isWalking, isRunning);
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = groundedForce;

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private void SetMovementAnimator(bool walk, bool run)
    {
        animator.SetBool(walkHash, walk);
        animator.SetBool(runHash, run);
    }
}