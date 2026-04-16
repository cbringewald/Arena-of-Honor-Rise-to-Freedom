using UnityEngine;

public class HealthBarFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private float horizontalOffset = 0.5f;
    [SerializeField] private float verticalOffset = 0.2f;
    [SerializeField] private bool placeLeft = false;

    private Camera mainCamera;

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Vector3 basePos = target.position + worldOffset;

        Vector3 camRight = mainCamera.transform.right;
        Vector3 camUp = mainCamera.transform.up;

        float side = placeLeft ? -1f : 1f;

        transform.position = basePos + camRight * horizontalOffset * side + camUp * verticalOffset;
        transform.forward = mainCamera.transform.forward;
    }
}