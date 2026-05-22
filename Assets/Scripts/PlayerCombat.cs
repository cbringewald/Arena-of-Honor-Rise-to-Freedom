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
    [SerializeField] private Health health;

    [Header("Attack Settings")]
    [SerializeField] private float weaponAttackCost = 14f;
    [SerializeField] private float unarmedAttackCost = 8f;
    [SerializeField] private WeaponStyle currentWeaponStyle = WeaponStyle.Unarmed;

    [Header("Weapon Visuals")]
    [SerializeField] private GameObject currentWeaponObject;

    [Header("Drop Weapon")]
    [SerializeField] private bool canDropWeapon = true;
    [SerializeField] private Key dropWeaponKey = Key.E;

    [Header("Animator Parameters")]
    [SerializeField] private string walkParameter = "Walk";
    [SerializeField] private string runParameter = "Run";
    [SerializeField] private string attackParameter = "Attack";
    [SerializeField] private string weaponStyleParameter = "WeaponStyle";

    private int walkHash;
    private int runHash;
    private int attackHash;
    private int weaponStyleHash;

    private bool isAttacking;
    private WeaponPickup currentWeaponPickup;
    private bool suppressDropUntilKeyReleased;

    public bool IsAttacking => isAttacking;
    public WeaponStyle CurrentWeaponStyle => currentWeaponStyle;
    public bool IsUnarmed => currentWeaponStyle == WeaponStyle.Unarmed;

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

        if (health == null)
            health = GetComponent<Health>();

        if (hitboxController == null)
            hitboxController = GetComponentInChildren<HitboxController>();

        walkHash = Animator.StringToHash(walkParameter);
        runHash = Animator.StringToHash(runParameter);
        attackHash = Animator.StringToHash(attackParameter);
        weaponStyleHash = Animator.StringToHash(weaponStyleParameter);
    }

    private void Start()
    {
        if (hitboxController != null)
            hitboxController.SetOwnerRoot(transform.root);

        if (currentWeaponObject != null)
        {
            currentWeaponObject.SetActive(true);
            weaponHitbox = currentWeaponObject.GetComponentInChildren<WeaponHitbox>(true);

            if (weaponHitbox != null)
                weaponHitbox.SetOwnerRoot(transform.root);
        }
        else
        {
            weaponHitbox = null;
            currentWeaponStyle = WeaponStyle.Unarmed;
        }

    UpdateAnimatorWeaponStyle();
}

    private void Update()
    {
        if (Mouse.current == null || animator == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryAttack();

        HandleDropInput();
    }

    public void TryAttack()
    {
        if (health != null && health.IsDead)
            return;

        if (isAttacking)
            return;

        if (playerBlock != null && playerBlock.IsBlocking)
            return;

        if (playerDodge != null && playerDodge.IsDodging)
            return;

        float staminaCost = IsUnarmed ? unarmedAttackCost : weaponAttackCost;

        if (stamina != null && !stamina.UseStamina(staminaCost))
        {
            Debug.Log("No hay suficiente stamina para atacar.");
            return;
        }

        isAttacking = true;

        animator.SetBool(walkHash, false);
        animator.SetBool(runHash, false);

        UpdateAnimatorWeaponStyle();

        animator.ResetTrigger(attackHash);
        animator.SetTrigger(attackHash);
    }

    public void EquipWeapon(WeaponStyle newStyle, GameObject newWeaponObject)
    {
        EquipWeapon(newStyle, newWeaponObject, null);
    }

    public void EquipWeapon(WeaponStyle newStyle, GameObject newWeaponObject, WeaponPickup sourcePickup)
    {
        if (newWeaponObject == null)
        {
            Debug.LogError("EquipWeapon: newWeaponObject está vacío.");
            return;
        }

        if (newWeaponObject == gameObject || newWeaponObject.transform == transform)
        {
            Debug.LogError("EquipWeapon: has asignado el Player como arma.");
            return;
        }

        if (!newWeaponObject.transform.IsChildOf(transform))
        {
            Debug.LogError("EquipWeapon: el arma equipada debe ser hija del Player. Objeto recibido: " + newWeaponObject.name);
            return;
        }

        if (currentWeaponObject != null)
        {
            if (currentWeaponObject == gameObject || currentWeaponObject.transform == transform)
            {
                Debug.LogError("currentWeaponObject apunta al Player. Lo limpio para no desactivar el personaje.");
                currentWeaponObject = null;
            }
            else
            {
                currentWeaponObject.SetActive(false);
            }
        }

        currentWeaponStyle = newStyle;
        currentWeaponObject = newWeaponObject;
        currentWeaponPickup = sourcePickup;
        suppressDropUntilKeyReleased = Keyboard.current != null && Keyboard.current[dropWeaponKey].isPressed;
        currentWeaponObject.SetActive(true);

        weaponHitbox = currentWeaponObject.GetComponentInChildren<WeaponHitbox>(true);

        UpdateAnimatorWeaponStyle();

        if (weaponHitbox != null)
        {
            weaponHitbox.SetOwnerRoot(transform.root);
            Debug.Log("WeaponHitbox asignado: " + weaponHitbox.name);
        }
        else
        {
            Debug.LogError("No se encontró WeaponHitbox en " + currentWeaponObject.name);
        }

        Debug.Log("Arma equipada: " + currentWeaponStyle + " / " + currentWeaponObject.name);
    }

    public void UnequipWeapon()
    {
        if (currentWeaponObject != null)
            currentWeaponObject.SetActive(false);

        currentWeaponObject = null;
        weaponHitbox = null;
        currentWeaponPickup = null;
        currentWeaponStyle = WeaponStyle.Unarmed;

        UpdateAnimatorWeaponStyle();

        Debug.Log("Sin arma equipada.");
    }

    public void DropCurrentWeapon()
    {
        if (!canDropWeapon)
            return;

        if (IsUnarmed || currentWeaponObject == null)
            return;

        if (isAttacking)
            return;

        if (currentWeaponPickup != null)
            currentWeaponPickup.DropFromPlayer(transform);
        else
            currentWeaponObject.SetActive(false);

        currentWeaponObject = null;
        weaponHitbox = null;
        currentWeaponPickup = null;
        currentWeaponStyle = WeaponStyle.Unarmed;

        UpdateAnimatorWeaponStyle();

        Debug.Log("Arma soltada.");
    }

    private void UpdateAnimatorWeaponStyle()
    {
        if (animator != null)
            animator.SetInteger(weaponStyleHash, (int)currentWeaponStyle);
    }

    public void SetWeaponHitbox(WeaponHitbox newHitbox)
    {
        weaponHitbox = newHitbox;
    }

    public void EnableHitbox()
    {
        if (IsUnarmed)
        {
            if (hitboxController != null)
                hitboxController.EnableHitbox();

            return;
        }

        if (weaponHitbox != null)
            weaponHitbox.StartSwing();
    }

    public void DisableHitbox()
    {
        if (IsUnarmed)
        {
            if (hitboxController != null)
                hitboxController.DisableHitbox();

            return;
        }

        if (weaponHitbox != null)
            weaponHitbox.EndSwing();
    }

    public void EndAttack()
    {
        isAttacking = false;

        if (hitboxController != null)
            hitboxController.DisableHitbox();

        if (weaponHitbox != null)
            weaponHitbox.EndSwing();
    }

    private void HandleDropInput()
    {
        if (!canDropWeapon || Keyboard.current == null)
            return;

        if (suppressDropUntilKeyReleased)
        {
            if (!Keyboard.current[dropWeaponKey].isPressed)
                suppressDropUntilKeyReleased = false;

            return;
        }

        if (Keyboard.current[dropWeaponKey].wasPressedThisFrame)
            DropCurrentWeapon();
    }
}
