using UnityEngine;
using UnityEngine.InputSystem;

public class ShieldPickup : MonoBehaviour
{
    [Header("Shield")]
    [SerializeField] private GameObject shieldInHandObject;
    [SerializeField] private GameObject shieldWorldObject;

    [Header("UI")]
    [SerializeField] private GameObject promptText;

    [Header("Door")]
    [SerializeField] private PortonArena arenaDoor;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip pickupSound;

    private PlayerBlock playerBlock;
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
        if (picked || !playerInside || playerBlock == null)
            return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            PickupShield();
    }

    private void PickupShield()
    {
        if (shieldInHandObject == null)
        {
            Debug.LogError("ShieldPickup: falta shieldInHandObject en " + gameObject.name);
            return;
        }

        playerBlock.EquipShield(shieldInHandObject);

        if (playerCombat != null)
            playerCombat.SuppressDropInputUntilKeyReleased();

        if (sfxSource != null && pickupSound != null)
            sfxSource.PlayOneShot(pickupSound);

        if (promptText != null)
            promptText.SetActive(false);

        if (arenaDoor != null)
            arenaDoor.OpenDoor();

        if (shieldWorldObject != null)
            shieldWorldObject.SetActive(false);

        picked = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerBlock block = other.GetComponentInParent<PlayerBlock>();

        if (block == null)
            return;

        playerBlock = block;
        playerCombat = other.GetComponentInParent<PlayerCombat>();
        if (playerCombat != null)
            playerCombat.BeginPickupInteraction();

        playerInside = true;

        if (promptText != null)
            promptText.SetActive(true);

        Debug.Log("Jugador cerca del escudo: " + gameObject.name);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerBlock block = other.GetComponentInParent<PlayerBlock>();

        if (block == null || block != playerBlock)
            return;

        playerInside = false;

        if (playerCombat != null)
            playerCombat.EndPickupInteraction();

        playerBlock = null;
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
        playerBlock = null;
        playerCombat = null;

        if (promptText != null)
            promptText.SetActive(false);

        if (shieldWorldObject != null)
            shieldWorldObject.SetActive(true);

        if (shieldInHandObject != null)
            shieldInHandObject.SetActive(false);
    }
}
