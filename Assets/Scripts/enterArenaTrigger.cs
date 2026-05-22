using UnityEngine;
using UnityEngine.InputSystem;

public class EnterArenaTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PortonArena arenaDoor;
    [SerializeField] private GameObject promptText;

    private PlayerCombat playerCombat;
    private bool playerInside;

    private void Awake()
    {
        if (promptText != null)
            promptText.SetActive(false);
    }

    private void Update()
    {
        if (!playerInside || playerCombat == null)
            return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            playerCombat.UnequipWeapon();

            if (promptText != null)
                promptText.SetActive(false);

            if (arenaDoor != null)
                arenaDoor.OpenDoor();

            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCombat combat = other.GetComponentInParent<PlayerCombat>();
        if (combat == null) return;

        playerCombat = combat;
        playerInside = true;

        if (promptText != null)
            promptText.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCombat combat = other.GetComponentInParent<PlayerCombat>();
        if (combat == null) return;

        playerInside = false;
        playerCombat = null;

        if (promptText != null)
            promptText.SetActive(false);
    }

    public void ResetTrigger()
    {
        playerCombat = null;
        playerInside = false;

        if (promptText != null)
            promptText.SetActive(false);

        gameObject.SetActive(true);
    }
}
