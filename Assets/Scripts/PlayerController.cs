using UnityEngine;

/// <summary>
/// Player rat: walks by default, runs while holding Shift, jumps with Space, and does a
/// forward roll with Shift + Space (or Ctrl / Q). Also sends attack input to the MeleeAttacker.
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(Combatant), typeof(MeleeAttacker))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 2.3f;
    public float runSpeed = 6.5f;
    public float acceleration = 10f;         // how quickly speed changes (higher = snappier)
    public float attackMoveFactor = 0.2f;
    public float jumpHeight = 1.2f;
    public float gravity = -22f;
    public float turnSpeed = 10f;
    public float coyoteTime = 0.12f;

    [Header("Roll")]
    public float rollSpeed = 8.5f;
    public float rollDuration = 0.6f;
    public float rollInvulnerableTime = 0.4f;
    public float rollCooldown = 0.25f;       // after the roll ends

    public float fallResetY = -15f;
    public Transform cameraTransform;

    CharacterController controller;
    Combatant combatant;
    MeleeAttacker attacker;
    Vector3 spawnPoint;
    Vector3 planarVelocity;
    float verticalVelocity;
    float lastGroundedTime = float.NegativeInfinity;
    float rollEndTime = -10f;
    Vector3 rollDirection;

    public bool IsRunning { get; private set; }

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
        bool canAct = !gameOver && !combatant.IsDead && !combatant.IsStunned && !OfficeScene.Viewing;
        bool rolling = combatant.IsDodging;

        Vector2 input = canAct ? GameInput.Move : Vector2.zero;
        Vector3 forward = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform ? cameraTransform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        Vector3 move = forward.normalized * input.y + right.normalized * input.x;
        bool hasInput = move.sqrMagnitude > 0.01f;

        if (controller.isGrounded) lastGroundedTime = Time.time;
        bool grounded = Time.time - lastGroundedTime <= coyoteTime;

        IsRunning = canAct && GameInput.Sprint && hasInput && !attacker.IsAttacking;

        if (canAct && !rolling)
        {
            bool rollInput = GameInput.DodgePressed || (GameInput.Sprint && GameInput.JumpPressed);
            if (rollInput && grounded && Time.time - rollEndTime >= rollCooldown)
            {
                StartRoll(hasInput ? move.normalized : transform.forward);
                rolling = true;
            }
            else
            {
                if (GameInput.LightAttackPressed) attacker.TryAttack(AttackKind.Light, move);
                if (GameInput.HeavyAttackPressed) attacker.TryAttack(AttackKind.Heavy, move);

                if (GameInput.JumpPressed && grounded && !attacker.IsAttacking)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    lastGroundedTime = float.NegativeInfinity;
                }
            }
        }

        // Horizontal movement
        if (rolling)
        {
            // Fast at the start of the roll, slowing as the rat comes out of it
            float progress = Mathf.Clamp01(combatant.DodgeProgress);
            planarVelocity = rollDirection * rollSpeed * Mathf.Lerp(1f, 0.55f, progress);
            transform.rotation = Quaternion.LookRotation(rollDirection);
            rollEndTime = Time.time;
            // Keep running out of the roll if Shift is still held
            if (progress > 0.85f && GameInput.Sprint && hasInput) planarVelocity = Vector3.Lerp(planarVelocity, move * runSpeed, 0.5f);
        }
        else if (attacker.IsAttacking)
        {
            planarVelocity = move * walkSpeed * attackMoveFactor + attacker.LungeVelocity;
        }
        else
        {
            Vector3 target = move * (IsRunning ? runSpeed : walkSpeed);
            // Smooth acceleration keeps the walk steady instead of jerky
            planarVelocity = Vector3.MoveTowards(planarVelocity, target, acceleration * (grounded ? 1f : 0.4f) * Time.deltaTime);
            if (hasInput)
            {
                float turn = IsRunning ? turnSpeed : turnSpeed * 0.8f;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), turn * Time.deltaTime);
            }
        }

        // Vertical movement
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        float launch = combatant.ConsumeLaunch();
        if (launch > 0f) verticalVelocity = launch;
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((planarVelocity + combatant.Knockback + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (transform.position.y < fallResetY) Respawn();
    }

    void StartRoll(Vector3 direction)
    {
        rollDirection = direction;
        attacker.Cancel();
        combatant.StartDodge(rollDuration, rollInvulnerableTime);
        transform.rotation = Quaternion.LookRotation(direction);
    }

    public void Respawn()
    {
        // CharacterController overrides position changes while enabled.
        controller.enabled = false;
        transform.position = spawnPoint;
        controller.enabled = true;
        verticalVelocity = 0f;
        planarVelocity = Vector3.zero;
    }
}
