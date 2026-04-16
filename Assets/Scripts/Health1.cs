using System.Collections;
using UnityEngine;

public class Health1 : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private Renderer rend;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float flashTime = 0.1f;

    private int currentHealth;
    private Color originalColor;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (rend == null)
            rend = GetComponentInChildren<Renderer>();

        if (rend != null)
            originalColor = rend.material.color;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} vida: {currentHealth}");

        if (rend != null)
            StartCoroutine(HitFlash());

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator HitFlash()
    {
        rend.material.color = hitColor;
        yield return new WaitForSeconds(flashTime);
        rend.material.color = originalColor;
    }
}