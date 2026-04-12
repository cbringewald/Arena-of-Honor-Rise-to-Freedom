using UnityEngine;
using UnityEngine.InputSystem;

public class PortonArena : MonoBehaviour
{
    public float alturaSubida = 6f;
    public float velocidad = 3f;

    private Vector3 posicionCerrado;
    private Vector3 posicionAbierto;

    private bool abierto = false;

    void Start()
    {
        posicionCerrado = transform.position;
        posicionAbierto = posicionCerrado + Vector3.up * alturaSubida;
    }

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            abierto = !abierto; // cambia entre abrir y cerrar
        }

        Vector3 objetivo = abierto ? posicionAbierto : posicionCerrado;

        transform.position = Vector3.MoveTowards(
            transform.position,
            objetivo,
            velocidad * Time.deltaTime
        );
    }
}