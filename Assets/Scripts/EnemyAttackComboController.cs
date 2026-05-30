using System;
using UnityEngine;

[Serializable]
public class EnemyAttackOption
{
    public string label = "Attack";
    public int attackIndex = 0;
    [Min(1)] public int damage = 1;
    [Min(0f)] public float minDistance = 0f;
    [Min(0.1f)] public float maxDistance = 3f;
    [Min(0.01f)] public float weight = 1f;
    [Min(0.1f)] public float cooldownMultiplier = 1f;
}

public class EnemyAttackComboController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyWeaponHitbox[] weaponHitboxes;

    [Header("Animator")]
    [SerializeField] private string attackIndexParameter = "AttackIndex";
    [SerializeField] private bool avoidImmediateRepeat = true;

    [Header("Attacks")]
    [SerializeField] private bool overrideWeaponDamageFromAttackOptions = false;
    [SerializeField] private EnemyAttackOption[] attacks =
    {
        new EnemyAttackOption { label = "Attack 0", attackIndex = 0, damage = 1, minDistance = 0f, maxDistance = 2.2f, weight = 1f },
        new EnemyAttackOption { label = "Attack 1", attackIndex = 1, damage = 1, minDistance = 0f, maxDistance = 2.4f, weight = 1f },
        new EnemyAttackOption { label = "Attack 2", attackIndex = 2, damage = 2, minDistance = 0.8f, maxDistance = 2.8f, weight = 0.7f }
    };

    private int attackIndexHash;
    private int lastAttackIndex = int.MinValue;
    private bool hasAttackIndexParameter;
    private float damageMultiplier = 1f;

    public int CurrentAttackIndex { get; private set; }
    public float CurrentCooldownMultiplier { get; private set; } = 1f;
    public bool HasAttacks => attacks != null && attacks.Length > 0;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (weaponHitboxes == null || weaponHitboxes.Length == 0)
            weaponHitboxes = GetComponentsInChildren<EnemyWeaponHitbox>(true);

        attackIndexHash = Animator.StringToHash(attackIndexParameter);
        hasAttackIndexParameter = HasAnimatorParameter(animator, attackIndexParameter, AnimatorControllerParameterType.Int);
    }

    public float SelectAttack(float distanceToPlayer)
    {
        EnemyAttackOption attack = PickAttack(distanceToPlayer);

        if (attack == null)
            return 1f;

        CurrentAttackIndex = attack.attackIndex;
        CurrentCooldownMultiplier = Mathf.Max(0.1f, attack.cooldownMultiplier);
        lastAttackIndex = attack.attackIndex;

        if (hasAttackIndexParameter)
            animator.SetInteger(attackIndexHash, attack.attackIndex);

        if (overrideWeaponDamageFromAttackOptions)
            SetWeaponDamage(Mathf.RoundToInt(attack.damage * damageMultiplier));

        return CurrentCooldownMultiplier;
    }

    public void ApplyDamageMultiplier(float multiplier)
    {
        damageMultiplier *= Mathf.Max(0.1f, multiplier);
    }

    public bool CanAttackAtDistance(float distanceToPlayer)
    {
        if (!HasAttacks)
            return true;

        return distanceToPlayer <= GetMaxAttackDistance();
    }

    public float GetMaxAttackDistance()
    {
        if (!HasAttacks)
            return 0f;

        float maxDistance = 0f;

        foreach (EnemyAttackOption attack in attacks)
        {
            if (attack != null)
                maxDistance = Mathf.Max(maxDistance, attack.maxDistance);
        }

        return maxDistance;
    }

    private EnemyAttackOption PickAttack(float distanceToPlayer)
    {
        if (attacks == null || attacks.Length == 0)
            return null;

        EnemyAttackOption attack = PickWeightedAttack(distanceToPlayer, true);

        if (attack != null)
            return attack;

        attack = PickWeightedAttack(distanceToPlayer, false);

        if (attack != null)
            return attack;

        return GetClosestDistanceAttack(distanceToPlayer);
    }

    private EnemyAttackOption PickWeightedAttack(float distanceToPlayer, bool respectRepeatRule)
    {
        float totalWeight = 0f;

        foreach (EnemyAttackOption attack in attacks)
        {
            if (!IsAttackValid(attack, distanceToPlayer, respectRepeatRule))
                continue;

            totalWeight += Mathf.Max(0.01f, attack.weight);
        }

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.value * totalWeight;

        foreach (EnemyAttackOption attack in attacks)
        {
            if (!IsAttackValid(attack, distanceToPlayer, respectRepeatRule))
                continue;

            roll -= Mathf.Max(0.01f, attack.weight);

            if (roll <= 0f)
                return attack;
        }

        return null;
    }

    private bool IsAttackValid(EnemyAttackOption attack, float distanceToPlayer, bool respectRepeatRule)
    {
        if (attack == null)
            return false;

        if (respectRepeatRule && avoidImmediateRepeat && attacks.Length > 1 && attack.attackIndex == lastAttackIndex)
            return false;

        return distanceToPlayer >= attack.minDistance && distanceToPlayer <= attack.maxDistance;
    }

    private EnemyAttackOption GetClosestDistanceAttack(float distanceToPlayer)
    {
        EnemyAttackOption closestAttack = null;
        float closestDistance = float.PositiveInfinity;

        foreach (EnemyAttackOption attack in attacks)
        {
            if (attack == null)
                continue;

            float attackDistance = Mathf.Abs(distanceToPlayer - Mathf.Clamp(distanceToPlayer, attack.minDistance, attack.maxDistance));

            if (attackDistance < closestDistance)
            {
                closestDistance = attackDistance;
                closestAttack = attack;
            }
        }

        return closestAttack;
    }

    private void SetWeaponDamage(int damage)
    {
        if (weaponHitboxes == null)
            return;

        foreach (EnemyWeaponHitbox hitbox in weaponHitboxes)
        {
            if (hitbox != null)
                hitbox.SetDamage(damage);
        }
    }

    private static bool HasAnimatorParameter(Animator targetAnimator, string parameterName, AnimatorControllerParameterType type)
    {
        if (targetAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.type == type && parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
