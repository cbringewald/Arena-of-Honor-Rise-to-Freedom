using UnityEngine;

public class HealthPrueba : MonoBehaviour
{
    [SerializeField] private int life = 3;

    private void Start()
    {
        Debug.Log(gameObject.name + " vida inicial: " + life);
    }

    public void TakeDamage(int damage)
    {
        life -= damage;
        Debug.Log(gameObject.name + " vida actual: " + life);

        if (life <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " ha muerto");
        Destroy(gameObject);
    }
}
