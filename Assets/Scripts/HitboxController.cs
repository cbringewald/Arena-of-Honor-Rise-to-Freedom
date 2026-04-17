using UnityEngine;

public class HitboxController : MonoBehaviour
{
    [SerializeField] private Collider hitboxCollider;
    [SerializeField] private WeaponHitbox weaponHitbox;

    private void Awake()
    {
        if (hitboxCollider == null)
            hitboxCollider = GetComponent<Collider>();

        if (weaponHitbox == null)
            weaponHitbox = GetComponent<WeaponHitbox>();

        if (hitboxCollider != null)
            hitboxCollider.enabled = false;
    }

    public void EnableHitbox()
    {
        if (weaponHitbox != null)
            weaponHitbox.StartSwing();

        if (hitboxCollider == null)
            return;

        hitboxCollider.enabled = false;
        Physics.SyncTransforms();
        hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;

        if (weaponHitbox != null)
            weaponHitbox.EndSwing();
    }
}