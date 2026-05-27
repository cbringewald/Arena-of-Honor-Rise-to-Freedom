using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBlock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Stamina stamina;
    [SerializeField] private Health health;
    [SerializeField] private GameObject shieldObject;

    [Header("Block Settings")]
    [SerializeField] private float staminaDrainPerSecond = 12f;
    [SerializeField] private float minimumStaminaToBlock = 5f;
    [SerializeField] private bool shieldEquipped;
    [SerializeField] private bool useShieldObjectActiveState = true;
    [SerializeField] private bool cancelBlockOnMovementInput = true;

    [Header("Animator Parameters")]
    [SerializeField] private string blockBool = "Block";
    [SerializeField] private string shieldBlockBool = "ShieldBlock";
    [SerializeField] private string blockStyleInt = "BlockStyle";

    [Header("Animator States")]
    [SerializeField] private bool forceAnimatorStateOnBlockChange = true;
    [SerializeField] private string locomotionStateName = "Locomotion";
    [SerializeField] private string blockNoShieldStateName = "Block_NoShield";
    [SerializeField] private string blockShieldStateName = "Block_Shield";
    [SerializeField, Min(0f)] private float blockCrossFadeDuration = 0.05f;

    private PlayerCombat playerCombat;
    private PlayerDodge playerDodge;

    private int blockHash;
    private int shieldBlockHash;
    private int blockStyleHash;
    private int locomotionStateHash;
    private int blockNoShieldStateHash;
    private int blockShieldStateHash;
    private bool hasBlockParameter;
    private bool hasShieldBlockParameter;
    private bool hasBlockStyleParameter;

    public bool IsBlocking { get; private set; }
    public bool HasShieldEquipped => useShieldObjectActiveState && shieldObject != null ? shieldObject.activeInHierarchy : shieldEquipped;

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
        shieldBlockHash = Animator.StringToHash(shieldBlockBool);
        blockStyleHash = Animator.StringToHash(blockStyleInt);
        locomotionStateHash = Animator.StringToHash(locomotionStateName);
        blockNoShieldStateHash = Animator.StringToHash(blockNoShieldStateName);
        blockShieldStateHash = Animator.StringToHash(blockShieldStateName);

        hasBlockParameter = HasAnimatorParameter(blockBool, AnimatorControllerParameterType.Bool);
        hasShieldBlockParameter = HasAnimatorParameter(shieldBlockBool, AnimatorControllerParameterType.Bool);
        hasBlockStyleParameter = HasAnimatorParameter(blockStyleInt, AnimatorControllerParameterType.Int);
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

        if (cancelBlockOnMovementInput && HasMovementInput())
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
        {
            if (IsBlocking)
                UpdateAnimatorBlockParameters();

            return;
        }

        IsBlocking = value;
        UpdateAnimatorBlockParameters();
        ForceAnimatorStateForBlock(value);
    }

    public void SetShieldEquipped(bool value)
    {
        shieldEquipped = value;

        if (shieldObject != null)
            shieldObject.SetActive(value);

        if (IsBlocking)
            UpdateAnimatorBlockParameters();
    }

    public void EquipShield(GameObject newShieldObject)
    {
        if (shieldObject != null && shieldObject != newShieldObject)
            shieldObject.SetActive(false);

        shieldObject = newShieldObject;
        SetShieldEquipped(true);
    }

    public void UnequipShield()
    {
        SetShieldEquipped(false);
    }

    public bool IsShieldCollider(Collider hitCollider)
    {
        if (!HasShieldEquipped || shieldObject == null || hitCollider == null)
            return false;

        return hitCollider.transform == shieldObject.transform || hitCollider.transform.IsChildOf(shieldObject.transform);
    }

    private bool HasMovementInput()
    {
        return Keyboard.current != null &&
            (Keyboard.current.wKey.isPressed ||
             Keyboard.current.aKey.isPressed ||
             Keyboard.current.sKey.isPressed ||
             Keyboard.current.dKey.isPressed);
    }

    private void UpdateAnimatorBlockParameters()
    {
        if (animator == null)
            return;

        bool hasShield = HasShieldEquipped;

        if (hasBlockParameter)
            animator.SetBool(blockHash, IsBlocking);

        if (hasShieldBlockParameter)
            animator.SetBool(shieldBlockHash, IsBlocking && hasShield);

        if (hasBlockStyleParameter)
            animator.SetInteger(blockStyleHash, IsBlocking && hasShield ? 1 : 0);
    }

    private void ForceAnimatorStateForBlock(bool blocking)
    {
        if (!forceAnimatorStateOnBlockChange || animator == null)
            return;

        int stateHash = locomotionStateHash;

        if (blocking)
            stateHash = HasShieldEquipped ? blockShieldStateHash : blockNoShieldStateHash;

        CrossFadeStateIfExists(stateHash);
    }

    private void CrossFadeStateIfExists(int stateHash)
    {
        if (stateHash == 0)
            return;

        for (int layer = 0; layer < animator.layerCount; layer++)
        {
            if (!animator.HasState(layer, stateHash))
                continue;

            animator.CrossFadeInFixedTime(stateHash, blockCrossFadeDuration, layer);
            return;
        }
    }

    private void OnDisable()
    {
        SetBlocking(false);
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
}
