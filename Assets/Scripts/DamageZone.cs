using System.Collections;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private string zoneName = "Body";
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private Health health;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float flashTime = 0.1f;

    private MaterialPropertyBlock mpb;
    private Coroutine flashRoutine;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    public Health Health => health;
    public string ZoneName => zoneName;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();

        if ((targetRenderers == null || targetRenderers.Length == 0))
        {
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null)
                targetRenderers = new Renderer[] { r };
        }

        mpb = new MaterialPropertyBlock();
    }

    public int GetFinalDamage(int baseDamage)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * damageMultiplier));
    }

    public void ApplyDamage(int baseDamage, Transform attacker)
    {
        if (health == null || health.IsDead)
            return;

        int finalDamage = GetFinalDamage(baseDamage);

        Debug.Log($"Zona: {zoneName} | Daño final: {finalDamage}");

        health.TakeDamage(finalDamage, zoneName);

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(HitFlash());
    }

    private IEnumerator HitFlash()
    {
        SetColor(hitColor);
        yield return new WaitForSeconds(flashTime);
        ResetColor();
        flashRoutine = null;
    }

    private void SetColor(Color color)
    {
        if (targetRenderers == null) return;

        foreach (var r in targetRenderers)
        {
            if (r == null) continue;

            r.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorID, color);
            mpb.SetColor(ColorID, color);
            r.SetPropertyBlock(mpb);
        }
    }

    private void ResetColor()
    {
        if (targetRenderers == null) return;

        foreach (var r in targetRenderers)
        {
            if (r == null) continue;

            mpb.Clear();
            r.SetPropertyBlock(mpb);
        }
    }
}