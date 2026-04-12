using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbitCM : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform cameraRoot;
    public Transform cameraTarget;

    [Header("Position")]
    public float heightOffset = 1.6f;

    [Header("Mouse Sensitivity")]
    public float mouseSensitivityX = 0.0018f;
    public float mouseSensitivityY = 0.0018f;

    [Header("Gamepad Sensitivity")]
    public float gamepadSensitivityX = 120f;
    public float gamepadSensitivityY = 120f;

    [Header("Pitch Limits")]
    public float minPitch = -35f;
    public float maxPitch = 60f;

    [Header("Rotation Smoothing")]
    public float rotationSmoothTime = 0.05f;

    private float targetYaw;
    private float targetPitch;

    private float currentYaw;
    private float currentPitch;

    private float yawVelocity;
    private float pitchVelocity;

    void Start()
    {
        float startYaw = player != null ? player.eulerAngles.y : transform.eulerAngles.y;

        targetYaw = startYaw;
        currentYaw = startYaw;

        targetPitch = 10f;
        currentPitch = 10f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (player == null || cameraRoot == null || cameraTarget == null)
            return;

        // Seguir al jugador
        cameraRoot.position = player.position + Vector3.up * heightOffset;

        // Input ratón
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            targetYaw += mouseDelta.x * mouseSensitivityX;
            targetPitch -= mouseDelta.y * mouseSensitivityY;
        }

        // Input mando
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.rightStick.ReadValue();
            targetYaw += stick.x * gamepadSensitivityX * Time.deltaTime;
            targetPitch -= stick.y * gamepadSensitivityY * Time.deltaTime;
        }

        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        // Suavizar la rotación final, no el input
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

        cameraRoot.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        cameraTarget.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }
}