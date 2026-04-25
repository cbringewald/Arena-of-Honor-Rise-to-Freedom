using UnityEngine;

public class CloseDoorTrigger : MonoBehaviour
{
    [SerializeField] private PortonArena arenaDoor;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        PlayerCombat player = other.GetComponentInParent<PlayerCombat>();
        if (player == null) return;

        Debug.Log("Jugador ha entrado en la arena → cerrando puerta");

        if (arenaDoor != null)
            arenaDoor.CloseDoor();

        triggered = true;
    }
}