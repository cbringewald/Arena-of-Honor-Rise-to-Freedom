using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCelebration : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Health health;
    [SerializeField] private Key celebrationKey = Key.C;
    [SerializeField] private bool allowCelebrationAnytime = true;
    [SerializeField] private string victoryTrigger = "Victory";
    [SerializeField] private string locomotionStateName = "Locomotion";
    [SerializeField] private float celebrationDuration = 3f;

    private int victoryHash;
    private int locomotionHash;
    private Coroutine victoryRoutine;
    private bool celebrationLocked;

    public bool IsCelebrating { get; private set; }
    public bool IsCelebrationLocked => celebrationLocked;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (health == null)
            health = GetComponent<Health>();

        victoryHash = Animator.StringToHash(victoryTrigger);
        locomotionHash = Animator.StringToHash(locomotionStateName);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[celebrationKey].wasPressedThisFrame)
            PlayVictory();
    }

    public void LockCelebration()
    {
        if (!allowCelebrationAnytime)
            celebrationLocked = true;
    }

    public void UnlockCelebration()
    {
        celebrationLocked = false;
    }

    public void PlayVictory()
    {
        if (celebrationLocked) return;
        if (IsCelebrating) return;
        if (animator == null) return;
        if (health != null && health.IsDead) return;

        victoryRoutine = StartCoroutine(VictoryRoutine());
    }

    public void CancelCelebration()
    {
        if (!IsCelebrating)
            return;

        if (victoryRoutine != null)
        {
            StopCoroutine(victoryRoutine);
            victoryRoutine = null;
        }

        IsCelebrating = false;

        if (animator == null)
            return;

        animator.ResetTrigger(victoryHash);

        if (HasAnimatorState(locomotionHash))
            animator.CrossFadeInFixedTime(locomotionHash, 0.08f);
    }

    private IEnumerator VictoryRoutine()
    {
        IsCelebrating = true;

        animator.ResetTrigger(victoryHash);
        animator.SetTrigger(victoryHash);

        Debug.Log("Celebración activada");

        yield return new WaitForSeconds(celebrationDuration);

        IsCelebrating = false;
        victoryRoutine = null;
    }

    private bool HasAnimatorState(int stateHash)
    {
        if (animator == null)
            return false;

        for (int layer = 0; layer < animator.layerCount; layer++)
        {
            if (animator.HasState(layer, stateHash))
                return true;
        }

        return false;
    }
}
