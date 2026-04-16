using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class DeathCinemachineController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineCamera deathCamera;

    [Header("Priorities")]
    [SerializeField] private int normalPriority = 0;
    [SerializeField] private int deathPriority = 20;

    [Header("Death Camera Timing")]
    [SerializeField] private float approachTime = 0.45f;
    [SerializeField] private float holdTime = 1.2f;

    [Header("Death Camera Framing")]
    [SerializeField] private Vector3 deathOffset = new Vector3(1.2f, 1.4f, -2.2f);
    [SerializeField] private string deathTargetChildName = "DeathTarget";

    private Coroutine currentRoutine;

    private void Start()
    {
        if (deathCamera != null)
        {
            deathCamera.Priority = normalPriority;
            deathCamera.Target.TrackingTarget = null;
        }
    }

    public void FocusOnDeadEnemy(Transform enemyRoot)
    {
        if (enemyRoot == null || deathCamera == null || mainCamera == null)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(FocusRoutine(enemyRoot));
    }

    private IEnumerator FocusRoutine(Transform enemyRoot)
    {
        Transform lookTarget = enemyRoot;
        Transform child = enemyRoot.Find(deathTargetChildName);

        if (child != null)
            lookTarget = child;

        // La death cam empieza exactamente donde está la cámara actual
        deathCamera.transform.position = mainCamera.transform.position;
        deathCamera.transform.rotation = mainCamera.transform.rotation;

        // Activamos la death cam
        deathCamera.Priority = deathPriority;

        Vector3 startPos = deathCamera.transform.position;
        Quaternion startRot = deathCamera.transform.rotation;

        Vector3 desiredPos =
            enemyRoot.position
            + enemyRoot.right * deathOffset.x
            + Vector3.up * deathOffset.y
            + enemyRoot.forward * deathOffset.z;

        Quaternion desiredRot = Quaternion.LookRotation(
            (lookTarget.position - desiredPos).normalized,
            Vector3.up
        );

        float t = 0f;

        while (t < approachTime)
        {
            t += Time.deltaTime;
            float blend = t / approachTime;
            blend = Mathf.SmoothStep(0f, 1f, blend);

            deathCamera.transform.position = Vector3.Lerp(startPos, desiredPos, blend);
            deathCamera.transform.rotation = Quaternion.Slerp(startRot, desiredRot, blend);

            yield return null;
        }

        deathCamera.transform.position = desiredPos;
        deathCamera.transform.rotation = desiredRot;

        yield return new WaitForSeconds(holdTime);

        deathCamera.Priority = normalPriority;
        currentRoutine = null;
    }
}