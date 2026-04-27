using UnityEngine;

public class CloseDoorTrigger : MonoBehaviour
{
    [SerializeField] private PortonArena arenaDoor;

    private void OnTriggerEnter(Collider other)
    {
        PlayerCombat player = other.GetComponentInParent<PlayerCombat>();
        if (player == null) return;

        if (arenaDoor != null)
            arenaDoor.CloseDoor();
    }
}