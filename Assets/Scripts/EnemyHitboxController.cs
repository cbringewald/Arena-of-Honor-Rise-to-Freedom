using UnityEngine;

public class EnemyHitboxController : MonoBehaviour
{
    [Header("Legacy Single Hitbox")]
    [SerializeField] private Collider hitboxCollider;
    [SerializeField] private EnemyWeaponHitbox weaponHitbox;

    [Header("Multiple Hitboxes")]
    [SerializeField] private Collider[] hitboxColliders;
    [SerializeField] private EnemyWeaponHitbox[] weaponHitboxes;
    [SerializeField] private bool autoFindChildHitboxes = true;

    private void Awake()
    {
        if (hitboxCollider == null && hitboxColliders == null)
            hitboxCollider = GetComponent<Collider>();

        if (weaponHitbox == null)
            weaponHitbox = GetComponent<EnemyWeaponHitbox>();

        CacheHitboxes();
        SetCollidersEnabled(false);
    }

    public void EnableHitbox()
    {
        if (weaponHitboxes != null)
        {
            foreach (EnemyWeaponHitbox hitbox in weaponHitboxes)
            {
                if (hitbox != null)
                    hitbox.StartSwing();
            }
        }

        SetCollidersEnabled(false);
        Physics.SyncTransforms();
        SetCollidersEnabled(true);
    }

    public void DisableHitbox()
    {
        SetCollidersEnabled(false);

        if (weaponHitboxes == null)
            return;

        foreach (EnemyWeaponHitbox hitbox in weaponHitboxes)
        {
            if (hitbox != null)
                hitbox.EndSwing();
        }
    }

    private void CacheHitboxes()
    {
        if ((weaponHitboxes == null || weaponHitboxes.Length == 0) && autoFindChildHitboxes)
            weaponHitboxes = GetComponentsInChildren<EnemyWeaponHitbox>(true);

        if ((hitboxColliders == null || hitboxColliders.Length == 0) && autoFindChildHitboxes)
        {
            EnemyWeaponHitbox[] hitboxes = weaponHitboxes != null && weaponHitboxes.Length > 0
                ? weaponHitboxes
                : GetComponentsInChildren<EnemyWeaponHitbox>(true);

            hitboxColliders = new Collider[hitboxes.Length];

            for (int i = 0; i < hitboxes.Length; i++)
            {
                if (hitboxes[i] != null)
                    hitboxColliders[i] = hitboxes[i].GetComponent<Collider>();
            }
        }

        if ((weaponHitboxes == null || weaponHitboxes.Length == 0) && weaponHitbox != null)
            weaponHitboxes = new[] { weaponHitbox };

        if ((hitboxColliders == null || hitboxColliders.Length == 0) && hitboxCollider != null)
            hitboxColliders = new[] { hitboxCollider };
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (hitboxColliders == null)
            return;

        foreach (Collider colliderToToggle in hitboxColliders)
        {
            if (colliderToToggle != null)
                colliderToToggle.enabled = enabled;
        }
    }
}
