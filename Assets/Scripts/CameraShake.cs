using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [SerializeField] private Transform cameraTarget;
    private Vector3 originalLocalPos;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (cameraTarget == null)
            cameraTarget = transform;

        originalLocalPos = cameraTarget.localPosition;
    }

    public void Shake(float duration, float magnitude)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            Vector3 offset = Random.insideUnitSphere * magnitude;
            offset.z = 0f;

            cameraTarget.localPosition = originalLocalPos + offset;
            yield return null;
        }

        cameraTarget.localPosition = originalLocalPos;
        shakeRoutine = null;
    }
}
