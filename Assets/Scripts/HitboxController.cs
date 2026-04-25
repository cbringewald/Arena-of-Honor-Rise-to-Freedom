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

    public void SetOwnerRoot(Transform ownerRoot)
    {
        if (weaponHitbox != null)
            weaponHitbox.SetOwnerRoot(ownerRoot);
    }

    public void EnableHitbox()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
            Physics.SyncTransforms();
            hitboxCollider.enabled = true;
        }

        if (weaponHitbox != null)
            weaponHitbox.StartSwing();
    }

    public void DisableHitbox()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;

        if (weaponHitbox != null)
            weaponHitbox.EndSwing();
    }
}