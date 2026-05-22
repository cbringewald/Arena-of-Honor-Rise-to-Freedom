using UnityEngine;

public class EnemyAttackAnimationEvents : MonoBehaviour
{
    [SerializeField] private BotAI botAI;
    [SerializeField] private EnemyHitboxController hitboxController;

    private void Awake()
    {
        if (botAI == null)
            botAI = GetComponentInParent<BotAI>();

        if (hitboxController == null)
            hitboxController = GetComponentInParent<EnemyHitboxController>();

        if (hitboxController == null)
            hitboxController = GetComponentInChildren<EnemyHitboxController>(true);
    }

    public void EnemyAnim_IsAttacking()
    {
        if (botAI != null)
            botAI.IsAttacking();
    }

    public void EnemyAnim_BeginAttackCommit()
    {
        if (botAI != null)
            botAI.BeginAttackCommit();
    }

    public void EnemyAnim_EndAttackCommit()
    {
        if (botAI != null)
            botAI.EndAttackCommit();
    }

    public void EnemyAnim_EndAttack()
    {
        if (botAI != null)
            botAI.EndAttack();
    }

    public void EnemyAnim_EnableHitbox()
    {
        if (hitboxController != null)
            hitboxController.EnableHitbox();
    }

    public void EnemyAnim_DisableHitbox()
    {
        if (hitboxController != null)
            hitboxController.DisableHitbox();
    }
}
