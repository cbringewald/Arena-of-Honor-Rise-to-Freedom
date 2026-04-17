using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Stamina stamina;
    [SerializeField] private WeaponHitbox weaponHitbox;
    [SerializeField] private HitboxController hitboxController;
    [SerializeField] private PlayerBlock playerBlock;
    [SerializeField] private PlayerDodge playerDodge;

    [Header("Attack Settings")]
    [SerializeField] private float weaponAttackCost = 20f;
    [SerializeField] private float unarmedAttackCost = 10f;
    [SerializeField] private bool isUnarmed = false;

    [Header("Animator Parameters")]
    [SerializeField] private string walkParameter = "Walk";
    [SerializeField] private string runParameter = "Run";
    [SerializeField] private string attackParameter = "Attack";
    [SerializeField] private string punchParameter = "Punch";
    [SerializeField] private string isUnarmedParameter = "IsUnarmed";

    private int walkHash;
    private int runHash;
    private int attackHash;
    private int punchHash;
    private int isUnarmedHash;

    private bool isAttacking;

    public bool IsAttacking => isAttacking;
    public bool IsUnarmed => isUnarmed;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (stamina == null)
            stamina = GetComponent<Stamina>();

        if (playerBlock == null)
            playerBlock = GetComponent<PlayerBlock>();

        if (playerDodge == null)
            playerDodge = GetComponent<PlayerDodge>();

        if (hitboxController == null)
            hitboxController = GetComponentInChildren<HitboxController>();

        if (animator == null)
            Debug.LogError("PlayerCombat: No se encontró Animator.");

        walkHash = Animator.StringToHash(walkParameter);
        runHash = Animator.StringToHash(runParameter);
        attackHash = Animator.StringToHash(attackParameter);
        punchHash = Animator.StringToHash(punchParameter);
        isUnarmedHash = Animator.StringToHash(isUnarmedParameter);
    }

    private void Start()
    {
        if (animator != null)
            animator.SetBool(isUnarmedHash, isUnarmed);
    }

    private void Update()
    {
        if (Mouse.current == null || animator == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryAttack();
        }
    }

    public void TryAttack()
    {
        if (isAttacking)
            return;

        if (playerBlock != null && playerBlock.IsBlocking)
            return;

        if (playerDodge != null && playerDodge.IsDodging)
            return;

        float staminaCost = isUnarmed ? unarmedAttackCost : weaponAttackCost;

        if (stamina != null && !stamina.UseStamina(staminaCost))
        {
            Debug.Log("No hay suficiente stamina para atacar.");
            return;
        }

        isAttacking = true;

        animator.SetBool(walkHash, false);
        animator.SetBool(runHash, false);

        if (isUnarmed)
        {
            animator.ResetTrigger(punchHash);
            animator.SetTrigger(punchHash);
        }
        else
        {
            animator.ResetTrigger(attackHash);
            animator.SetTrigger(attackHash);
        }
    }

    public void SetUnarmed(bool value)
    {
        isUnarmed = value;

        if (animator != null)
            animator.SetBool(isUnarmedHash, isUnarmed);
    }

    public void SetWeaponHitbox(WeaponHitbox newHitbox)
    {
        weaponHitbox = newHitbox;
    }

    public void EnableHitbox()
    {
        if (hitboxController != null)
            hitboxController.EnableHitbox();
    }

    public void DisableHitbox()
    {
        if (hitboxController != null)
            hitboxController.DisableHitbox();
    }

    public void EndAttack()
    {
        isAttacking = false;

        if (weaponHitbox != null)
            weaponHitbox.EndSwing();
    }
}