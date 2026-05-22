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
    [SerializeField] private float celebrationDuration = 3f;

    private int victoryHash;
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

        StartCoroutine(VictoryRoutine());
    }

    private IEnumerator VictoryRoutine()
    {
        IsCelebrating = true;

        animator.ResetTrigger(victoryHash);
        animator.SetTrigger(victoryHash);

        Debug.Log("Celebración activada");

        yield return new WaitForSeconds(celebrationDuration);

        IsCelebrating = false;
    }
}
