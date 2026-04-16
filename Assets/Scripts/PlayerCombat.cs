using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
    [SerializeField] private string walkParameter = "Walk";
    [SerializeField] private string runParameter = "Run";
    [SerializeField] private string attackParameter = "Attack";

    private int walkHash;
    private int runHash;
    private int attackHash;

    private bool isAttacking;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("PlayerCombat: No se encontró Animator.");

        walkHash = Animator.StringToHash(walkParameter);
        runHash = Animator.StringToHash(runParameter);
        attackHash = Animator.StringToHash(attackParameter);
    }

    private void Update()
    {
        if (Mouse.current == null || animator == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame && !isAttacking)
        {
            isAttacking = true;

            animator.SetBool(walkHash, false);
            animator.SetBool(runHash, false);
            animator.ResetTrigger(attackHash);
            animator.SetTrigger(attackHash);  
        }
    }

    public void EndAttack()
    {
        isAttacking = false;
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }
}