using UnityEngine;

public class PlayerWeaponHandler : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private GameObject startingWeaponPrefab;

    private GameObject currentWeapon;

    private void Awake()
    {
        if (weaponSocket == null)
        {
            Transform found = transform.FindDeepChild("WeaponSocket");
            if (found != null)
            {
                weaponSocket = found;
            }
            else
            {
                Debug.LogError("PlayerWeaponHandler: Asigna weaponSocket (WeaponSocket en la mano).");
            }
        }
    }

    private void Start()
    {
        if (startingWeaponPrefab != null && weaponSocket != null)
        {
            EquipWeapon(startingWeaponPrefab);
        }
    }

    public void EquipWeapon(GameObject weaponPrefab)
    {
        if (weaponSocket == null || weaponPrefab == null)
            return;

        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
        }

        currentWeapon = Instantiate(weaponPrefab, weaponSocket);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;
        currentWeapon.transform.localScale = Vector3.one;
    }
}

public static class TransformSearchExtensions
{
    public static Transform FindDeepChild(this Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = child.FindDeepChild(name);
            if (result != null)
                return result;
        }

        return null;
    }
}