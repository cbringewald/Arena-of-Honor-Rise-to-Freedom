using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraOrbitCM : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform cameraRoot;
    public Transform cameraTarget;

    [Header("Position")]
    public float heightOffset = 1.6f;
    [SerializeField] private float defaultPitch = 10f;

    [Header("Mouse Sensitivity")]
    public float mouseSensitivityX = 0.12f;
    public float mouseSensitivityY = 0.12f;

    [Header("Gamepad Sensitivity")]
    public float gamepadSensitivityX = 120f;
    public float gamepadSensitivityY = 120f;

    [Header("Pitch Limits")]
    public float minPitch = -35f;
    public float maxPitch = 60f;

    [Header("Rotation Smoothing")]
    public float rotationSmoothTime = 0.05f;
    [SerializeField] private bool readInputOnlyWhenCursorLocked = true;

    [Header("Camera Collision")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private LayerMask cameraCollisionLayers = ~0;
    [SerializeField] private float cameraCollisionRadius = 0.18f;
    [SerializeField] private bool blockRotationAgainstObstacles = true;
    [SerializeField] private float cameraDistance = 4.5f;
    [SerializeField] private float minCameraDistance = 1.15f;
    [SerializeField] private float collisionProbePadding = 0.15f;
    [SerializeField] private float wallDistancePadding = 0.22f;
    [SerializeField] private float distanceAdjustSpeed = 18f;
    [SerializeField] private bool onlyBlockMeshColliders = true;
    [SerializeField] private float collisionDampingIn = 0f;
    [SerializeField] private float collisionDampingOut = 0.35f;
    [SerializeField] private bool ignorePlayerTag = true;

    private float targetYaw;
    private float targetPitch;

    private float currentYaw;
    private float currentPitch;
    private float currentCameraDistance;

    private float yawVelocity;
    private float pitchVelocity;

    private CinemachineThirdPersonFollow thirdPersonFollow;
    private readonly RaycastHit[] cameraCollisionHits = new RaycastHit[16];
    private readonly Collider[] cameraOverlapHits = new Collider[16];

    void Start()
    {
        ConfigureCameraCollision();

        float startYaw = player != null ? player.eulerAngles.y : transform.eulerAngles.y;

        targetYaw = startYaw;
        currentYaw = startYaw;

        targetPitch = defaultPitch;
        currentPitch = defaultPitch;
        currentCameraDistance = GetAllowedCameraDistance(currentYaw, currentPitch);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ConfigureCameraCollision()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        if (cinemachineCamera == null)
            return;

        thirdPersonFollow = cinemachineCamera.GetComponent<CinemachineThirdPersonFollow>();

#if CINEMACHINE_PHYSICS
        if (thirdPersonFollow != null && !blockRotationAgainstObstacles)
        {
            string ignoredTag = ignorePlayerTag && player != null && !player.CompareTag("Untagged")
                ? player.tag
                : string.Empty;

            thirdPersonFollow.AvoidObstacles = new CinemachineThirdPersonFollow.ObstacleSettings
            {
                Enabled = true,
                CollisionFilter = cameraCollisionLayers,
                IgnoreTag = ignoredTag,
                CameraRadius = cameraCollisionRadius,
                DampingIntoCollision = collisionDampingIn,
                DampingFromCollision = collisionDampingOut
            };
        }
#endif

        CinemachineDecollider decollider = cinemachineCamera.GetComponent<CinemachineDecollider>();

        if (decollider != null)
        {
            decollider.enabled = false;
            decollider.CameraRadius = cameraCollisionRadius;
            decollider.Decollision.Enabled = false;
            decollider.Decollision.ObstacleLayers = cameraCollisionLayers;
            decollider.Decollision.UseFollowTarget.Enabled = true;
        }

        CinemachineDeoccluder deoccluder = cinemachineCamera.GetComponent<CinemachineDeoccluder>();

        if (deoccluder != null)
        {
            deoccluder.enabled = false;
            deoccluder.AvoidObstacles.Enabled = false;
        }

        if (thirdPersonFollow != null)
        {
#if CINEMACHINE_PHYSICS
            thirdPersonFollow.AvoidObstacles.Enabled = false;
#endif
            thirdPersonFollow.CameraDistance = cameraDistance;
        }
    }

    void LateUpdate()
    {
        if (player == null || cameraRoot == null || cameraTarget == null)
            return;

        // Seguir al jugador
        cameraRoot.position = player.position + Vector3.up * heightOffset;

        bool canReadLookInput = !readInputOnlyWhenCursorLocked || Cursor.lockState == CursorLockMode.Locked;

        float yawInput = 0f;
        float pitchInput = 0f;

        if (canReadLookInput && Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            yawInput += mouseDelta.x * mouseSensitivityX;
            pitchInput -= mouseDelta.y * mouseSensitivityY;
        }

        if (canReadLookInput && Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.rightStick.ReadValue();
            yawInput += stick.x * gamepadSensitivityX * Time.deltaTime;
            pitchInput -= stick.y * gamepadSensitivityY * Time.deltaTime;
        }

        ApplyCameraInput(yawInput, pitchInput);

        // Suavizar la rotación final, no el input
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

        cameraRoot.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        cameraTarget.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);

        if (thirdPersonFollow != null)
        {
            float allowedDistance = GetAllowedCameraDistance(currentYaw, currentPitch);
            currentCameraDistance = Mathf.MoveTowards(
                currentCameraDistance,
                allowedDistance,
                distanceAdjustSpeed * Time.deltaTime);
            thirdPersonFollow.CameraDistance = currentCameraDistance;
        }
    }

    public void ResetViewToPlayer(bool instant = true)
    {
        if (player == null)
            return;

        targetYaw = player.eulerAngles.y;
        targetPitch = defaultPitch;

        yawVelocity = 0f;
        pitchVelocity = 0f;

        if (!instant)
            return;

        currentYaw = targetYaw;
        currentPitch = targetPitch;
        currentCameraDistance = cameraDistance;

        if (cameraRoot != null)
        {
            cameraRoot.position = player.position + Vector3.up * heightOffset;
            cameraRoot.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        }

        if (cameraTarget != null)
            cameraTarget.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);

        if (thirdPersonFollow != null)
            thirdPersonFollow.CameraDistance = currentCameraDistance;
    }

    private void ApplyCameraInput(float yawInput, float pitchInput)
    {
        float proposedYaw = targetYaw + yawInput;
        float proposedPitch = Mathf.Clamp(targetPitch + pitchInput, minPitch, maxPitch);

        if (!blockRotationAgainstObstacles || CanMoveCameraTo(proposedYaw, targetPitch))
            targetYaw = proposedYaw;

        if (!blockRotationAgainstObstacles || CanMoveCameraTo(targetYaw, proposedPitch))
            targetPitch = proposedPitch;
    }

    private bool CanMoveCameraTo(float yaw, float pitch)
    {
        float currentCollisionDistance = GetCameraCollisionDistance(targetYaw, targetPitch);
        float proposedCollisionDistance = GetCameraCollisionDistance(yaw, pitch);

        if (float.IsPositiveInfinity(proposedCollisionDistance))
            return true;

        if (float.IsPositiveInfinity(currentCollisionDistance))
            return false;

        return proposedCollisionDistance >= currentCollisionDistance - 0.02f;
    }

    private float GetCameraCollisionDistance(float yaw, float pitch)
    {
        if (cameraRoot == null)
            return float.PositiveInfinity;

        Vector3 origin = cameraRoot.position;
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
        Vector3 desiredDirection = rotation * Vector3.back;
        float probeDistance = Mathf.Max(0.1f, cameraDistance + collisionProbePadding);
        float nearestDistance = float.PositiveInfinity;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            cameraCollisionRadius,
            desiredDirection,
            cameraCollisionHits,
            probeDistance,
            cameraCollisionLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            if (!ShouldIgnoreCameraCollision(cameraCollisionHits[i].collider))
                nearestDistance = Mathf.Min(nearestDistance, cameraCollisionHits[i].distance);
        }

        Vector3 desiredPosition = origin + desiredDirection.normalized * cameraDistance;
        hitCount = Physics.OverlapSphereNonAlloc(
            desiredPosition,
            cameraCollisionRadius,
            cameraOverlapHits,
            cameraCollisionLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            if (!ShouldIgnoreCameraCollision(cameraOverlapHits[i]))
                nearestDistance = 0f;
        }

        return nearestDistance;
    }

    private float GetAllowedCameraDistance(float yaw, float pitch)
    {
        float collisionDistance = GetCameraCollisionDistance(yaw, pitch);

        if (float.IsPositiveInfinity(collisionDistance))
            return cameraDistance;

        return Mathf.Clamp(collisionDistance - wallDistancePadding, minCameraDistance, cameraDistance);
    }

    private bool ShouldIgnoreCameraCollision(Collider hitCollider)
    {
        if (hitCollider == null)
            return true;

        if (player != null && hitCollider.transform.IsChildOf(player))
            return true;

        if (cameraRoot != null && hitCollider.transform.IsChildOf(cameraRoot))
            return true;

        if (ignorePlayerTag && player != null && !player.CompareTag("Untagged") && hitCollider.CompareTag(player.tag))
            return true;

        if (onlyBlockMeshColliders && !(hitCollider is MeshCollider))
            return true;

        return false;
    }
}
