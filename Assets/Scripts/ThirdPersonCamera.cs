using UnityEngine;

/// <summary>
/// Mouse-orbit camera that follows a target, pulls in when something is in the way,
/// and can shake on big hits.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    public static ThirdPersonCamera Instance { get; private set; }

    public Transform target;
    public float distance = 6f;
    public float minDistance = 2.5f;
    public float maxDistance = 12f;
    public float focusHeight = 1.4f;
    public float mouseSensitivity = 3f;
    public float minPitch = -20f;
    public float maxPitch = 75f;

    float yaw;
    float pitch = 15f;
    float shake;

    void Awake() => Instance = this;

    void Start()
    {
        if (target) yaw = target.eulerAngles.y;
        LockCursor(true);
    }

    public void Shake(float amount) => shake = Mathf.Max(shake, amount);

    void LateUpdate()
    {
        if (!target) return;

        if (GameInput.UnlockCursorPressed) LockCursor(false);
        else if (GameInput.LockCursorPressed) LockCursor(true);

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 look = GameInput.Look;
            yaw += look.x * mouseSensitivity;
            pitch -= look.y * mouseSensitivity;
        }
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        distance = Mathf.Clamp(distance - GameInput.Zoom * 10f, minDistance, maxDistance);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * focusHeight;
        Vector3 direction = rotation * Vector3.back;

        float actualDistance = distance;
        if (Physics.SphereCast(focus, 0.3f, direction, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
            actualDistance = Mathf.Max(hit.distance, 0.5f);

        shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 1.5f);
        Vector3 shakeOffset = Random.insideUnitSphere * shake * shake * 0.5f;

        transform.position = focus + direction * actualDistance + shakeOffset;
        transform.rotation = rotation;
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
