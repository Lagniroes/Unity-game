using UnityEngine;

/// <summary>
/// Code-driven animation for a RatRig: fighting stance, running, jumping, weapon swings,
/// hit reactions, dodge rolls, getting knocked out, and a swishing tail.
/// </summary>
[RequireComponent(typeof(RatRig))]
public class RatAnimator : MonoBehaviour
{
    public float strideFrequency = 1.8f;
    public float blendSpeed = 14f;

    RatRig rig;
    CharacterController controller;
    MeleeAttacker attacker;
    Combatant combatant;
    float phase;
    float[] tailBaseAngles;
    Vector3 upperRestPosition;

    void Awake()
    {
        rig = GetComponent<RatRig>();
        controller = GetComponentInParent<CharacterController>();
        attacker = GetComponentInParent<MeleeAttacker>();
        combatant = GetComponentInParent<Combatant>();
    }

    void Start()
    {
        upperRestPosition = rig.upper.localPosition;
        tailBaseAngles = new float[rig.tail.Length];
        for (int i = 0; i < rig.tail.Length; i++) tailBaseAngles[i] = rig.tail[i].localEulerAngles.x;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        Vector3 velocity = controller ? controller.velocity : Vector3.zero;
        float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        bool grounded = !controller || controller.isGrounded;
        bool dead = combatant && combatant.IsDead;
        bool stunned = combatant && combatant.IsStunned;
        bool dodging = combatant && combatant.IsDodging;
        bool attacking = attacker && attacker.IsAttacking;

        phase += speed * strideFrequency * dt;
        float swing = Mathf.Sin(phase * Mathf.PI);
        float amount = Mathf.Clamp01(speed / 5f);
        float breathe = Mathf.Sin(Time.time * 3f);

        // Default: fighting stance / running
        float leftLeg = swing * 45f * amount;
        float rightLeg = -leftLeg;
        float leftArm = -swing * 40f * amount - 15f;
        float rightArm = -35f - swing * 15f * amount;           // crowbar held up and ready
        float leftArmRoll = -10f, rightArmRoll = 10f;
        float lean = amount * 12f;
        float twist = 0f;
        float headPitch = 0f;
        Vector3 upperOffset = Vector3.up * (Mathf.Abs(swing) * 0.05f * amount + breathe * 0.008f);

        if (!grounded && !dodging)
        {
            leftLeg = -45f;
            rightLeg = 20f;
            leftArm = -70f;
        }
        if (dodging)
        {
            leftLeg = rightLeg = -70f;
            leftArm = rightArm = -60f;
            lean = 60f;
            upperOffset = Vector3.down * 0.25f;
        }
        if (stunned)
        {
            lean = -25f;
            headPitch = -20f;
            leftArm = -40f;
            rightArm = -20f;
            leftArmRoll = -50f;
            rightArmRoll = 50f;
        }

        float t = 1f - Mathf.Exp(-blendSpeed * dt);
        Blend(rig.leftLeg, Quaternion.Euler(leftLeg, 0f, 0f), t);
        Blend(rig.rightLeg, Quaternion.Euler(rightLeg, 0f, 0f), t);
        Blend(rig.leftArm, Quaternion.Euler(leftArm, 0f, leftArmRoll), t);
        Blend(rig.head, Quaternion.Euler(headPitch, 0f, 0f), t);
        rig.upper.localPosition = Vector3.Lerp(rig.upper.localPosition, upperRestPosition + upperOffset, t);

        if (attacking)
        {
            // Swings snap straight to the pose so they feel fast and readable
            AttackPose pose = attacker.GetPose();
            rig.rightArm.localRotation = Quaternion.Euler(pose.armPitch, 0f, rightArmRoll);
            rig.upper.localRotation = Quaternion.Euler(pose.lean, pose.twist, 0f);
            rig.leftArm.localRotation = Quaternion.Euler(-50f, 0f, -25f); // guard up
        }
        else
        {
            Blend(rig.rightArm, Quaternion.Euler(rightArm, 0f, rightArmRoll), t);
            Blend(rig.upper, Quaternion.Euler(lean, twist, 0f), t);
        }

        // Tail swish, faster when running
        float tailSpeed = 3f + speed;
        float tailAmount = 12f + amount * 10f;
        for (int i = 0; i < rig.tail.Length; i++)
        {
            float wave = Mathf.Sin(Time.time * tailSpeed - i * 0.6f);
            rig.tail[i].localRotation = Quaternion.Euler(tailBaseAngles[i] + wave * 3f, wave * tailAmount, 0f);
        }

        // Knocked out: fall over backwards
        Quaternion bodyPose = dead ? Quaternion.Euler(-85f, 0f, 0f) : Quaternion.identity;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, bodyPose, 1f - Mathf.Exp(-6f * dt));
    }

    static void Blend(Transform part, Quaternion target, float t)
    {
        part.localRotation = Quaternion.Slerp(part.localRotation, target, t);
    }
}
