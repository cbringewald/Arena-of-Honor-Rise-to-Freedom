using UnityEngine;

public class BotForward : MonoBehaviour
{
    public float speed = 2.5f;
    public float rotationSpeed = 0f; // si quieres que no gire, deja 0
    public Animator animator;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Mover siempre hacia delante
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Activar animación de caminar (si usas un BlendTree con VelY)
        animator.SetFloat("VelY", 1f);
        animator.SetFloat("VelX", 0f);
    }
}

