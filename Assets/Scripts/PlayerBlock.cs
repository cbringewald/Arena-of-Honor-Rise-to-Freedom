using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBlock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Stamina stamina;
    [SerializeField] private Health health;

    [Header("Block Settings")]
    [SerializeField] private float staminaDrainPerSecond = 12f;
    [SerializeField] private float minimumStaminaToBlock = 5f;

    [Header("Animator Parameters")]
    [SerializeField] private string blockBool = "Block";

    private PlayerCombat playerCombat;
    private PlayerDodge playerDodge;

    private int blockHash;

    public bool IsBlocking { get; private set; }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (stamina == null)
            stamina = GetComponent<Stamina>();

        if (health == null)
            health = GetComponent<Health>();

        playerCombat = GetComponent<PlayerCombat>();
        playerDodge = GetComponent<PlayerDodge>();

        blockHash = Animator.StringToHash(blockBool);
    }

    private void Update()
    {
        bool shouldBlock = CanHoldBlock();

        SetBlocking(shouldBlock);
    }

    private bool CanHoldBlock()
    {
        if (Mouse.current == null)
            return false;

        if (!Mouse.current.rightButton.isPressed)
            return false;

        if (health != null && health.IsDead)
            return false;

        if (playerCombat != null && playerCombat.IsAttacking)
            return false;

        if (playerDodge != null && playerDodge.IsDodging)
            return false;

        if (stamina != null)
        {
            if (!stamina.HasEnough(minimumStaminaToBlock))
                return false;

            bool paid = stamina.UseStamina(staminaDrainPerSecond * Time.deltaTime);

            if (!paid)
                return false;
        }

        return true;
    }

    private void SetBlocking(bool value)
    {
        if (IsBlocking == value)
            return;

        IsBlocking = value;

        if (animator != null)
            animator.SetBool(blockHash, IsBlocking);
    }

    private void OnDisable()
    {
        SetBlocking(false);
    }
}