using UnityEngine;

/// <summary>
/// Third-person character movement: walk/sprint relative to the camera, jump, gravity.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float sprintSpeed = 10f;
    public float jumpHeight = 2f;
    public float gravity = -20f;
    public float turnSpeed = 12f;
    public float coyoteTime = 0.12f;   // grace period to still jump right after walking off a ledge
    public float fallResetY = -15f;
    public Transform cameraTransform;

    CharacterController controller;
    Vector3 spawnPoint;
    float verticalVelocity;
    float lastGroundedTime = float.NegativeInfinity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        spawnPoint = transform.position;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasWon) return;

        Vector2 input = GameInput.Move;

        Vector3 forward = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform ? cameraTransform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        Vector3 move = forward.normalized * input.y + right.normalized * input.x;

        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (controller.isGrounded)
        {
            lastGroundedTime = Time.time;
            if (verticalVelocity < 0f) verticalVelocity = -2f; // keep snapped to the ground
        }

        if (GameInput.JumpPressed && Time.time - lastGroundedTime <= coyoteTime)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastGroundedTime = float.NegativeInfinity;
        }

        verticalVelocity += gravity * Time.deltaTime;

        float speed = GameInput.Sprint ? sprintSpeed : moveSpeed;
        Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        if (transform.position.y < fallResetY) Respawn();
    }

    public void Respawn()
    {
        // CharacterController overrides position changes while enabled.
        controller.enabled = false;
        transform.SetPositionAndRotation(spawnPoint, Quaternion.identity);
        controller.enabled = true;
        verticalVelocity = 0f;
    }
}
