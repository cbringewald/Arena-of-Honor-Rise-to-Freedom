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

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip pickupSound;

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

            if (weaponInHandObject == null)
            {
                Debug.LogError("WeaponPickup: falta weaponInHandObject en " + gameObject.name);
                return;
            }
            
            playerCombat.EquipWeapon(weaponStyle, weaponInHandObject, this);

            if (sfxSource != null && pickupSound != null)
                sfxSource.PlayOneShot(pickupSound);

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
        playerCombat.BeginPickupInteraction();
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
        if (playerCombat != null)
            playerCombat.EndPickupInteraction();

        playerCombat = null;

        if (promptText != null)
            promptText.SetActive(false);
    }

    public void ResetPickup()
    {
        if (playerInside && playerCombat != null)
            playerCombat.EndPickupInteraction();

        picked = false;
        playerInside = false;
        playerCombat = null;

        if (promptText != null)
            promptText.SetActive(false);

        if (weaponWorldObject != null)
            weaponWorldObject.SetActive(true);

    }

    public void DropFromPlayer(Transform playerTransform)
    {
        picked = false;
        if (playerInside && playerCombat != null)
            playerCombat.EndPickupInteraction();

        playerInside = false;
        playerCombat = null;

        if (weaponInHandObject != null)
            weaponInHandObject.SetActive(false);

        if (weaponWorldObject != null)
        {
            weaponWorldObject.SetActive(true);

            if (playerTransform != null)
            {
                Vector3 dropPosition = playerTransform.position + playerTransform.forward * 1.2f;
                dropPosition.y += 0.2f;
                transform.position = dropPosition;
                transform.rotation = Quaternion.LookRotation(playerTransform.forward, Vector3.up);
            }
        }

        if (promptText != null)
            promptText.SetActive(false);
    }
}
