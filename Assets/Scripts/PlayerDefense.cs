using UnityEngine;

public class PlayerDefense : MonoBehaviour
{
    [SerializeField] private PlayerBlock playerBlock;
    [SerializeField] private Stamina stamina;

    [Header("Block Settings")]
    [SerializeField] private float frontalBlockAngle = 120f;
    [SerializeField] private int blockedDamage = 0;
    [SerializeField] private float blockImpactStaminaCost = 15f;

    private void Awake()
    {
        if (playerBlock == null)
            playerBlock = GetComponent<PlayerBlock>();

        if (stamina == null)
            stamina = GetComponent<Stamina>();
    }

    public bool TryBlockHit(Transform attacker, ref int damage)
    {
        if (playerBlock == null || !playerBlock.IsBlocking)
            return false;

        if (attacker == null)
            return false;

        Vector3 toAttacker = (attacker.position - transform.position).normalized;
        toAttacker.y = 0f;

        float angle = Vector3.Angle(transform.forward, toAttacker);

        if (angle > frontalBlockAngle * 0.5f)
            return false;

        if (stamina != null && !stamina.UseStamina(blockImpactStaminaCost))
            return false;

        damage = blockedDamage;
        return true;
    }
}