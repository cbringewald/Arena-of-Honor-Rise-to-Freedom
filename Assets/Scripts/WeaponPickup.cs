using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private WeaponStyle weaponStyle = WeaponStyle.Sword;
    [SerializeField] private GameObject weaponInHandObject;
    [SerializeField] private GameObject weaponWorldObject;

    [Header("UI")]
    [SerializeField] private GameObject promptText;

    [Header("Door")]
    [SerializeField] private PortonArena arenaDoor;

    private PlayerCombat playerCombat;
    private bool playerInside;
    private bool picked;

    private void Awake()
    {
        if (promptText != null)
            promptText.SetActive(false);
    }

    private void Update()
    {
        if (picked) return;
        if (!playerInside || playerCombat == null) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("Recogiendo arma: " + weaponStyle);

            playerCombat.EquipWeapon(weaponStyle, weaponInHandObject);

            if (promptText != null)
                promptText.SetActive(false);

            if (arenaDoor != null)
                arenaDoor.OpenDoor();

            if (weaponWorldObject != null)
                weaponWorldObject.SetActive(false);

            picked = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCombat combat = other.GetComponentInParent<PlayerCombat>();

        if (combat == null)
            return;

        playerCombat = combat;
        playerInside = true;

        if (promptText != null)
            promptText.SetActive(true);

        Debug.Log("Jugador cerca del arma: " + gameObject.name);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCombat combat = other.GetComponentInParent<PlayerCombat>();

        if (combat == null)
            return;

        playerInside = false;
        playerCombat = null;

        if (promptText != null)
            promptText.SetActive(false);
    }
}