using UnityEngine;

/// <summary>
/// Player rat: camera-relative movement, sprint, jump, dodge roll, and attack input.
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(Combatant), typeof(MeleeAttacker))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float sprintSpeed = 8.5f;
    public float attackMoveFactor = 0.2f;
    public float jumpHeight = 1.3f;
    public float gravity = -22f;
    public float turnSpeed = 14f;
    public float coyoteTime = 0.12f;
    public float dodgeSpeed = 13f;
    public float dodgeDuration = 0.28f;
    public float dodgeCooldown = 0.6f;
    public float fallResetY = -15f;
    public Transform cameraTransform;

    CharacterController controller;
    Combatant combatant;
    MeleeAttacker attacker;
    Vector3 spawnPoint;
    float verticalVelocity;
    float lastGroundedTime = float.NegativeInfinity;
    float dodgeStart = -10f;
    Vector3 dodgeDirection;

    bool IsDodging => Time.time - dodgeStart < dodgeDuration;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        combatant = GetComponent<Combatant>();
        attacker = GetComponent<MeleeAttacker>();
        spawnPoint = transform.position;
    }

    void Update()
    {
        bool gameOver = GameManager.Instance && GameManager.Instance.IsGameOver;
        bool canAct = !gameOver && !combatant.IsDead && !combatant.IsStunned;

        Vector2 input = canAct ? GameInput.Move : Vector2.zero;
        Vector3 forward = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform ? cameraTransform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        Vector3 move = forward.normalized * input.y + right.normalized * input.x;

        if (canAct)
        {
            if (GameInput.DodgePressed && !IsDodging && Time.time - dodgeStart >= dodgeCooldown)
            {
                dodgeStart = Time.time;
                dodgeDirection = move.sqrMagnitude > 0.01f ? move.normalized : -transform.forward; // no input: hop back
                attacker.Cancel();
                combatant.StartDodge(dodgeDuration);
            }
            else if (!IsDodging)
            {
                if (GameInput.LightAttackPressed) attacker.TryAttack(AttackKind.Light, move);
                if (GameInput.HeavyAttackPressed) attacker.TryAttack(AttackKind.Heavy, move);
            }
        }

        Vector3 horizontal;
        if (IsDodging)
        {
            horizontal = dodgeDirection * dodgeSpeed;
        }
        else if (attacker.IsAttacking)
        {
            horizontal = move * moveSpeed * attackMoveFactor + attacker.LungeVelocity;
        }
        else
        {
            float speed = GameInput.Sprint ? sprintSpeed : moveSpeed;
            horizontal = move * speed;
            if (move.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), turnSpeed * Time.deltaTime);
        }

        if (controller.isGrounded)
        {
            lastGroundedTime = Time.time;
            if (verticalVelocity < 0f) verticalVelocity = -2f;
        }

        bool canJump = canAct && !IsDodging && !attacker.IsAttacking;
        if (canJump && GameInput.JumpPressed && Time.time - lastGroundedTime <= coyoteTime)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastGroundedTime = float.NegativeInfinity;
        }

        float launch = combatant.ConsumeLaunch();
        if (launch > 0f) verticalVelocity = launch;
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((horizontal + combatant.Knockback + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (transform.position.y < fallResetY) Respawn();
    }

    public void Respawn()
    {
        // CharacterController overrides position changes while enabled.
        controller.enabled = false;
        transform.position = spawnPoint;
        controller.enabled = true;
        verticalVelocity = 0f;
    }
}
