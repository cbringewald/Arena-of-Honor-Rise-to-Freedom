using System.Collections;
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

    [Header("Attack Variants")]
    [SerializeField, Min(1)] private int unarmedAttackVariants = 3;
    [SerializeField, Min(1)] private int swordAttackVariants = 2;
    [SerializeField, Min(1)] private int axeAttackVariants = 3;
    [SerializeField, Min(1)] private int maceAttackVariants = 3;
    [SerializeField] private bool cycleAttackVariants = true;
    [SerializeField] private bool avoidImmediateAttackRepeat = true;
    [SerializeField, Min(0.1f)] private float attackFailsafeDuration = 1.5f;
    [SerializeField] private bool maceUsesAxeAnimations = true;

    [Header("Attack Damage")]
    [SerializeField] private int[] unarmedAttackDamages = { 8, 10, 14 };
    [SerializeField] private int[] swordAttackBonusDamage = { 0, 2, 5 };
    [SerializeField] private int[] axeAttackBonusDamage = { 0, 4, 8 };
    [SerializeField] private int[] maceAttackBonusDamage = { 0, 3, 7 };

    [Header("Combo Input")]
    [SerializeField] private bool bufferAttackInput = true;
    [SerializeField, Min(0.05f)] private float attackInputBufferTime = 0.45f;
    [SerializeField, Min(0f)] private float queuedAttackDelay = 0.02f;

    [Header("Weapon Visuals")]
    [SerializeField] private GameObject currentWeaponObject;

    [Header("Drop Weapon")]
    [SerializeField] private bool canDropWeapon = true;
    [SerializeField] private Key dropWeaponKey = Key.E;

    [Header("Animator Parameters")]
    [SerializeField] private string walkParameter = "Walk";
    [SerializeField] private string runParameter = "Run";
    [SerializeField] private string attackParameter = "Attack";
    [SerializeField] private string attackIndexParameter = "AttackIndex";
    [SerializeField] private string weaponStyleParameter = "WeaponStyle";

    private int walkHash;
    private int runHash;
    private int attackHash;
    private int attackIndexHash;
    private int weaponStyleHash;
    private bool hasAttackIndexParameter;

    private bool isAttacking;
    private WeaponPickup currentWeaponPickup;
    private bool suppressDropUntilKeyReleased;
    private bool queuedAttackInput;
    private float queuedAttackExpireTime;
    private Coroutine queuedAttackRoutine;
    private int pickupInteractionLocks;
    private float attackStartTime;
    private int lastAttackIndex = -1;
    private int currentAttackIndex;
    private int nextUnarmedAttackIndex;
    private int nextSwordAttackIndex;
    private int nextAxeAttackIndex;
    private int nextMaceAttackIndex;

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
        attackIndexHash = Animator.StringToHash(attackIndexParameter);
        weaponStyleHash = Animator.StringToHash(weaponStyleParameter);
        hasAttackIndexParameter = HasAnimatorParameter(attackIndexParameter, AnimatorControllerParameterType.Int);
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

        if (isAttacking && Time.time >= attackStartTime + attackFailsafeDuration)
            EndAttack();

        HandleDropInput();
    }

    public void TryAttack()
    {
        if (health != null && health.IsDead)
            return;

        if (isAttacking)
        {
            QueueAttackInput();
            return;
        }

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
        queuedAttackInput = false;
        attackStartTime = Time.time;

        animator.SetBool(walkHash, false);
        animator.SetBool(runHash, false);

        UpdateAnimatorWeaponStyle();
        UpdateAttackIndexAndDamage();

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

    public void SuppressDropInputUntilKeyReleased()
    {
        suppressDropUntilKeyReleased = Keyboard.current != null && Keyboard.current[dropWeaponKey].isPressed;
    }

    public void BeginPickupInteraction()
    {
        pickupInteractionLocks++;
    }

    public void EndPickupInteraction()
    {
        pickupInteractionLocks = Mathf.Max(0, pickupInteractionLocks - 1);
    }

    private void UpdateAnimatorWeaponStyle()
    {
        if (animator != null)
            animator.SetInteger(weaponStyleHash, (int)GetAnimatorWeaponStyle());
    }

    private void UpdateAttackIndexAndDamage()
    {
        currentAttackIndex = ChooseAttackIndex();
        ApplyAttackDamage(currentAttackIndex);

        if (animator != null && hasAttackIndexParameter)
            animator.SetInteger(attackIndexHash, currentAttackIndex);
    }

    private int ChooseAttackIndex()
    {
        int attackCount = Mathf.Max(1, GetAttackVariantCount(currentWeaponStyle));

        if (!cycleAttackVariants)
        {
            int randomIndex = Random.Range(0, attackCount);

            if (avoidImmediateAttackRepeat && attackCount > 1 && randomIndex == lastAttackIndex)
                randomIndex = (randomIndex + 1) % attackCount;

            lastAttackIndex = randomIndex;
            return randomIndex;
        }

        int attackIndex = GetNextAttackIndex(currentWeaponStyle);

        if (avoidImmediateAttackRepeat && attackCount > 1 && attackIndex == lastAttackIndex)
            attackIndex = (attackIndex + 1) % attackCount;

        SetNextAttackIndex(currentWeaponStyle, (attackIndex + 1) % attackCount);
        lastAttackIndex = attackIndex;
        return attackIndex;
    }

    private int GetAttackVariantCount(WeaponStyle weaponStyle)
    {
        switch (weaponStyle)
        {
            case WeaponStyle.Sword:
                return swordAttackVariants;
            case WeaponStyle.Axe:
                return axeAttackVariants;
            case WeaponStyle.Mace:
                return maceAttackVariants;
            default:
                return GetSafeAttackVariantCount(unarmedAttackVariants, unarmedAttackDamages);
        }
    }

    private static int GetSafeAttackVariantCount(int configuredCount, int[] damageValues)
    {
        int count = Mathf.Max(1, configuredCount);

        if (damageValues != null && damageValues.Length > 0)
            count = Mathf.Min(count, damageValues.Length);

        return count;
    }

    private WeaponStyle GetAnimatorWeaponStyle()
    {
        if (currentWeaponStyle == WeaponStyle.Mace && maceUsesAxeAnimations)
            return WeaponStyle.Axe;

        return currentWeaponStyle;
    }

    private void ApplyAttackDamage(int attackIndex)
    {
        if (IsUnarmed)
        {
            WeaponHitbox unarmedHitbox = GetUnarmedWeaponHitbox();

            if (unarmedHitbox != null)
                unarmedHitbox.SetDamage(GetDamageFromArray(unarmedAttackDamages, attackIndex, unarmedHitbox.Damage));

            return;
        }

        if (weaponHitbox == null)
            return;

        int bonusDamage = 0;

        switch (currentWeaponStyle)
        {
            case WeaponStyle.Sword:
                bonusDamage = GetDamageFromArray(swordAttackBonusDamage, attackIndex, 0);
                break;
            case WeaponStyle.Axe:
                bonusDamage = GetDamageFromArray(axeAttackBonusDamage, attackIndex, 0);
                break;
            case WeaponStyle.Mace:
                bonusDamage = GetDamageFromArray(maceAttackBonusDamage, attackIndex, 0);
                break;
        }

        weaponHitbox.SetDamage(weaponHitbox.BaseDamage + bonusDamage);
    }

    private WeaponHitbox GetUnarmedWeaponHitbox()
    {
        if (hitboxController == null)
            return null;

        WeaponHitbox unarmedHitbox = hitboxController.GetComponent<WeaponHitbox>();

        if (unarmedHitbox == null)
            unarmedHitbox = hitboxController.GetComponentInChildren<WeaponHitbox>(true);

        return unarmedHitbox;
    }

    private static int GetDamageFromArray(int[] damages, int attackIndex, int fallback)
    {
        if (damages == null || damages.Length == 0)
            return fallback;

        int clampedIndex = Mathf.Clamp(attackIndex, 0, damages.Length - 1);
        return Mathf.Max(0, damages[clampedIndex]);
    }

    private int GetNextAttackIndex(WeaponStyle weaponStyle)
    {
        switch (weaponStyle)
        {
            case WeaponStyle.Sword:
                return nextSwordAttackIndex;
            case WeaponStyle.Axe:
                return nextAxeAttackIndex;
            case WeaponStyle.Mace:
                return nextMaceAttackIndex;
            default:
                return nextUnarmedAttackIndex;
        }
    }

    private void SetNextAttackIndex(WeaponStyle weaponStyle, int nextIndex)
    {
        switch (weaponStyle)
        {
            case WeaponStyle.Sword:
                nextSwordAttackIndex = nextIndex;
                break;
            case WeaponStyle.Axe:
                nextAxeAttackIndex = nextIndex;
                break;
            case WeaponStyle.Mace:
                nextMaceAttackIndex = nextIndex;
                break;
            default:
                nextUnarmedAttackIndex = nextIndex;
                break;
        }
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == type && parameter.name == parameterName)
                return true;
        }

        return false;
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

        TryConsumeQueuedAttack();
    }

    private void QueueAttackInput()
    {
        if (!bufferAttackInput)
            return;

        queuedAttackInput = true;
        queuedAttackExpireTime = Time.time + attackInputBufferTime;
    }

    private void TryConsumeQueuedAttack()
    {
        if (!queuedAttackInput)
            return;

        if (Time.time > queuedAttackExpireTime)
        {
            queuedAttackInput = false;
            return;
        }

        if (queuedAttackRoutine != null)
            StopCoroutine(queuedAttackRoutine);

        queuedAttackRoutine = StartCoroutine(QueuedAttackRoutine());
    }

    private IEnumerator QueuedAttackRoutine()
    {
        queuedAttackInput = false;

        if (queuedAttackDelay > 0f)
            yield return new WaitForSeconds(queuedAttackDelay);
        else
            yield return null;

        queuedAttackRoutine = null;

        if (!isAttacking)
            TryAttack();
    }

    private void HandleDropInput()
    {
        if (!canDropWeapon || Keyboard.current == null)
            return;

        if (pickupInteractionLocks > 0)
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
