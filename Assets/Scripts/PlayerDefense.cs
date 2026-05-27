using UnityEngine;

public class PlayerDefense : MonoBehaviour
{
    [SerializeField] private PlayerBlock playerBlock;
    [SerializeField] private Stamina stamina;

    [Header("Block Settings")]
    [SerializeField] private float frontalBlockAngle = 120f;
    [SerializeField] private float shieldFrontalBlockAngle = 170f;
    [SerializeField] private int blockedDamage = 0;
    [SerializeField] private int shieldBlockedDamage = 0;
    [SerializeField] private float blockImpactStaminaCost = 15f;
    [SerializeField] private float shieldBlockImpactStaminaCost = 8f;

    private void Awake()
    {
        if (playerBlock == null)
            playerBlock = GetComponent<PlayerBlock>();

        if (stamina == null)
            stamina = GetComponent<Stamina>();
    }

    public bool TryBlockHit(Transform attacker, ref int damage)
    {
        return TryBlockHit(attacker, null, ref damage);
    }

    public bool TryBlockHit(Transform attacker, Collider hitCollider, ref int damage)
    {
        if (playerBlock == null || !playerBlock.IsBlocking)
            return false;

        if (attacker == null)
            return false;

        bool hitShieldCollider = playerBlock.IsShieldCollider(hitCollider);
        bool hasShield = playerBlock.HasShieldEquipped;

        Vector3 toAttacker = (attacker.position - transform.position).normalized;
        toAttacker.y = 0f;

        float angle = Vector3.Angle(transform.forward, toAttacker);
        float allowedAngle = hasShield ? shieldFrontalBlockAngle : frontalBlockAngle;

        if (!hitShieldCollider && angle > allowedAngle * 0.5f)
            return false;

        float staminaCost = hasShield ? shieldBlockImpactStaminaCost : blockImpactStaminaCost;

        if (stamina != null && !stamina.UseStamina(staminaCost))
            return false;

        damage = hasShield ? shieldBlockedDamage : blockedDamage;
        return true;
    }
}
