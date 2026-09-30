using UnityEngine;

/// <summary>
/// Rival rat brain: runs at the target, circles at fighting range, and mixes light
/// combos with telegraphed heavy attacks. Gets more aggressive when low on health.
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(Combatant), typeof(MeleeAttacker))]
public class RatAI : MonoBehaviour
{
    public Combatant target;
    public float runSpeed = 5.5f;
    public float strafeSpeed = 1.6f;
    public float detectRange = 30f;
    public float engageRange = 2.1f;
    public float tooClose = 1.2f;
    public float heavyChance = 0.3f;
    public Vector2 attackDelay = new Vector2(1.2f, 2.2f);
    public float gravity = -22f;
    public float turnSpeed = 8f;

    CharacterController controller;
    Combatant self;
    MeleeAttacker attacker;
    float verticalVelocity;
    float nextAttackTime;
    int comboHitsLeft;
    float strafeDirection = 1f;
    float nextStrafeSwitch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        self = GetComponent<Combatant>();
        attacker = GetComponent<MeleeAttacker>();
    }

    void Start() => nextAttackTime = Time.time + 1.5f; // a moment to size each other up

    void Update()
    {
        bool gameOver = GameManager.Instance && GameManager.Instance.IsGameOver;
        bool canAct = !gameOver && !self.IsDead && !self.IsStunned && target && !target.IsDead;
        Vector3 horizontal = attacker.IsAttacking ? attacker.LungeVelocity : Vector3.zero;

        if (canAct)
        {
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            Vector3 direction = distance > 0.01f ? toTarget / distance : transform.forward;

            if (attacker.IsAttacking)
            {
                if (comboHitsLeft > 0 && attacker.TryAttack(AttackKind.Light, direction)) comboHitsLeft--;
            }
            else if (distance <= detectRange)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);

                if (distance > engageRange)
                {
                    horizontal = direction * runSpeed;
                }
                else
                {
                    if (distance < tooClose)
                    {
                        horizontal = -direction * strafeSpeed;
                    }
                    else
                    {
                        if (Time.time >= nextStrafeSwitch)
                        {
                            strafeDirection = -strafeDirection;
                            nextStrafeSwitch = Time.time + Random.Range(1f, 2.5f);
                        }
                        horizontal = Vector3.Cross(Vector3.up, direction) * strafeDirection * strafeSpeed;
                    }

                    if (Time.time >= nextAttackTime) Attack(direction);
                }
            }
        }

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        float launch = self.ConsumeLaunch();
        if (launch > 0f) verticalVelocity = launch;
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((horizontal + self.Knockback + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    void Attack(Vector3 direction)
    {
        bool enraged = self.Health / self.maxHealth < 0.4f;
        if (Random.value < heavyChance)
        {
            attacker.TryAttack(AttackKind.Heavy, direction);
            comboHitsLeft = 0;
        }
        else if (attacker.TryAttack(AttackKind.Light, direction))
        {
            comboHitsLeft = Random.Range(0, enraged ? 3 : 2);
        }
        nextAttackTime = Time.time + Random.Range(attackDelay.x, attackDelay.y) * (enraged ? 0.6f : 1f);
    }
}
