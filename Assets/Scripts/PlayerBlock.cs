using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBlock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Stamina stamina;

    [Header("Block Settings")]
    [SerializeField] private float staminaDrainPerSecond = 12f;

    [Header("Animator Parameters")]
    [SerializeField] private string blockBool = "Block";

    private PlayerCombat playerCombat;
    private PlayerDodge playerDodge;
    private int blockHash;

    public bool IsBlocking { get; private set; }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (stamina == null)
            stamina = GetComponent<Stamina>();

        playerCombat = GetComponent<PlayerCombat>();
        playerDodge = GetComponent<PlayerDodge>();

        blockHash = Animator.StringToHash(blockBool);
    }

    private void Update()
    {
        if (Mouse.current == null)
        {
            SetBlocking(false);
            return;
        }

        bool wantsBlock = Mouse.current.rightButton.isPressed;

        if (playerCombat != null && playerCombat.IsAttacking)
            wantsBlock = false;

        if (playerDodge != null && playerDodge.IsDodging)
            wantsBlock = false;

        if (wantsBlock)
        {
            if (stamina != null)
            {
                bool couldPay = stamina.UseStamina(staminaDrainPerSecond * Time.deltaTime);

                if (!couldPay)
                    wantsBlock = false;
            }
        }

        SetBlocking(wantsBlock);
    }

    private void SetBlocking(bool value)
    {
        IsBlocking = value;

        if (animator != null)
            animator.SetBool(blockHash, IsBlocking);
    }
}