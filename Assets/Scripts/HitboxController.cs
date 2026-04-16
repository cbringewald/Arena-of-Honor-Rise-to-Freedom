using UnityEngine;

public class HitboxController : MonoBehaviour
{
    [SerializeField] private Collider hitboxCollider;
    [SerializeField] private WeaponHitbox weaponHitbox;

    private void Start()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;
    }

    public void EnableHitbox()
    {
        if (weaponHitbox != null)
            weaponHitbox.StartSwing();

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
            hitboxCollider.enabled = true;
        }

        Debug.Log("Hitbox ENABLE");
    }

    public void DisableHitbox()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;

        Debug.Log("Hitbox DISABLE");
    }
}