using UnityEngine;

public class CloseDoorTrigger : MonoBehaviour
{
    [SerializeField] private PortonArena arenaDoor;
    [SerializeField] private RoundManager roundManager;

    private void Awake()
    {
        if (roundManager == null)
            roundManager = FindFirstObjectByType<RoundManager>(FindObjectsInactive.Include);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCombat player = other.GetComponentInParent<PlayerCombat>();
        if (player == null) return;

        if (arenaDoor != null)
            arenaDoor.CloseDoor();

        if (roundManager != null)
            roundManager.ActivateCurrentRoundEnemies();
    }
}
