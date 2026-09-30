using UnityEngine;

/// <summary>
/// Code-driven animation for a RatRig. Builds a full-body pose every frame from:
/// fighting stance (idle), a calm walk cycle and a pumping run cycle (blended by speed),
/// jumping, weapon swings, hit-stun, a tucked forward roll, getting knocked out, and the tail.
/// </summary>
[RequireComponent(typeof(RatRig))]
public class RatAnimator : MonoBehaviour
{
    /// <summary>Every joint angle (degrees) plus body height offset.</summary>
    struct Pose
    {
        public float hipL, hipR, kneeL, kneeR, ankleL, ankleR;
        public float shoulderL, shoulderR, shoulderRollL, shoulderRollR, elbowL, elbowR;
        public float hipsLean, hipsTwist, hipsRoll, chestLean, chestTwist, headPitch;
        public float bodyY;

        public static Pose Lerp(Pose a, Pose b, float t)
        {
            return new Pose
            {
                hipL = Mathf.Lerp(a.hipL, b.hipL, t), hipR = Mathf.Lerp(a.hipR, b.hipR, t),
                kneeL = Mathf.Lerp(a.kneeL, b.kneeL, t), kneeR = Mathf.Lerp(a.kneeR, b.kneeR, t),
                ankleL = Mathf.Lerp(a.ankleL, b.ankleL, t), ankleR = Mathf.Lerp(a.ankleR, b.ankleR, t),
                shoulderL = Mathf.Lerp(a.shoulderL, b.shoulderL, t), shoulderR = Mathf.Lerp(a.shoulderR, b.shoulderR, t),
                shoulderRollL = Mathf.Lerp(a.shoulderRollL, b.shoulderRollL, t), shoulderRollR = Mathf.Lerp(a.shoulderRollR, b.shoulderRollR, t),
                elbowL = Mathf.Lerp(a.elbowL, b.elbowL, t), elbowR = Mathf.Lerp(a.elbowR, b.elbowR, t),
                hipsLean = Mathf.Lerp(a.hipsLean, b.hipsLean, t), hipsTwist = Mathf.Lerp(a.hipsTwist, b.hipsTwist, t),
                hipsRoll = Mathf.Lerp(a.hipsRoll, b.hipsRoll, t), chestLean = Mathf.Lerp(a.chestLean, b.chestLean, t),
                chestTwist = Mathf.Lerp(a.chestTwist, b.chestTwist, t), headPitch = Mathf.Lerp(a.headPitch, b.headPitch, t),
                bodyY = Mathf.Lerp(a.bodyY, b.bodyY, t),
            };
        }
    }

    /// <summary>Numbers that shape a gait (walk or run).</summary>
    struct Gait
    {
        public float legSwing, kneeSwing, kneeStance, bob, lean, armSwing, elbow, twist, hipRoll;

        public static Gait Lerp(Gait a, Gait b, float t) => new Gait
        {
            legSwing = Mathf.Lerp(a.legSwing, b.legSwing, t), kneeSwing = Mathf.Lerp(a.kneeSwing, b.kneeSwing, t),
            kneeStance = Mathf.Lerp(a.kneeStance, b.kneeStance, t), bob = Mathf.Lerp(a.bob, b.bob, t),
            lean = Mathf.Lerp(a.lean, b.lean, t), armSwing = Mathf.Lerp(a.armSwing, b.armSwing, t),
            elbow = Mathf.Lerp(a.elbow, b.elbow, t), twist = Mathf.Lerp(a.twist, b.twist, t),
            hipRoll = Mathf.Lerp(a.hipRoll, b.hipRoll, t),
        };
    }

    static readonly Gait Walk = new Gait
    {
        legSwing = 24f, kneeSwing = 40f, kneeStance = 6f, bob = 0.025f, lean = 4f,
        armSwing = 16f, elbow = 18f, twist = 4f, hipRoll = 2f,
    };

    static readonly Gait Run = new Gait
    {
        legSwing = 50f, kneeSwing = 100f, kneeStance = 22f, bob = 0.07f, lean = 16f,
        armSwing = 50f, elbow = 85f, twist = 9f, hipRoll = 1f,
    };

    [Header("Gait")]
    public float walkStride = 1.3f;       // meters per full walk cycle (two steps)
    public float runStride = 2.8f;
    public float runBlendStart = 2.8f;    // speed where the walk starts turning into a run
    public float runBlendEnd = 5.5f;
    public float blendSpeed = 15f;

    RatRig rig;
    CharacterController controller;
    MeleeAttacker attacker;
    Combatant combatant;
    float phase;
    float smoothedSpeed;
    float runWeight;
    float airTime;
    float[] tailBaseAngles;

    void Awake()
    {
        rig = GetComponent<RatRig>();
        controller = GetComponentInParent<CharacterController>();
        attacker = GetComponentInParent<MeleeAttacker>();
        combatant = GetComponentInParent<Combatant>();
    }

    void Start()
    {
        tailBaseAngles = new float[rig.tail.Length];
        for (int i = 0; i < rig.tail.Length; i++) tailBaseAngles[i] = rig.tail[i].localEulerAngles.x;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Smooth the raw speed and ground contact so small bumps don't make the animation twitch
        Vector3 velocity = controller ? controller.velocity : Vector3.zero;
        float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 1f - Mathf.Exp(-8f * dt));
        airTime = controller && !controller.isGrounded ? airTime + dt : 0f;
        bool airborne = airTime > 0.15f;

        bool dead = combatant && combatant.IsDead;
        bool stunned = combatant && combatant.IsStunned;
        float rollProgress = combatant ? combatant.DodgeProgress : -1f;
        bool attacking = attacker && attacker.IsAttacking;

        runWeight = Mathf.MoveTowards(runWeight, Mathf.InverseLerp(runBlendStart, runBlendEnd, smoothedSpeed), 3f * dt);
        float stride = Mathf.Lerp(walkStride, runStride, runWeight);
        phase = Mathf.Repeat(phase + smoothedSpeed / stride * Mathf.PI * 2f * dt, Mathf.PI * 2f);
        float moving = Mathf.Clamp01(smoothedSpeed / 0.8f);

        // Build the target pose
        Pose pose = Pose.Lerp(Idle(), Locomotion(Gait.Lerp(Walk, Run, runWeight), runWeight), moving);
        if (airborne) pose = Jump(pose);
        if (stunned) pose = Stunned(pose);
        if (attacking) pose = Attacking(pose, attacker.GetPose());
        if (dead) pose = KnockedOut();
        pose.headPitch -= (pose.hipsLean + pose.chestLean) * 0.7f; // keep the eyes level

        float blend = 1f - Mathf.Exp(-blendSpeed * dt);
        Apply(pose, blend, attacking);

        // Forward roll: tuck into a ball and spin a full circle around the belly
        if (rollProgress >= 0f && !dead)
        {
            float tuck = Mathf.Clamp01(Mathf.Sin(rollProgress * Mathf.PI) * 1.8f);
            Apply(Pose.Lerp(pose, Tucked(), tuck), 1f, true);
            float spin = Mathf.SmoothStep(0f, 1f, rollProgress) * 360f;
            rig.roll.localRotation = Quaternion.Euler(spin, 0f, 0f);
            rig.roll.localPosition = new Vector3(0f, rig.rollPivotHeight - 0.12f * tuck, 0f);
        }
        else
        {
            rig.roll.localRotation = Quaternion.identity;
        }

        AnimateTail(dt, speed);

        // Knocked out: fall over backwards
        Quaternion bodyPose = dead ? Quaternion.Euler(-85f, 0f, 0f) : Quaternion.identity;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, bodyPose, 1f - Mathf.Exp(-6f * dt));
    }

    // ---------------------------------------------------------------- Poses

    Pose Idle()
    {
        // Boxer stance: knees bent, left foot forward, crowbar up, left fist guarding
        float breathe = Mathf.Sin(Time.time * 2.2f);
        return new Pose
        {
            hipL = -20f, kneeL = 20f, ankleL = 0f,
            hipR = 6f, kneeR = 22f, ankleR = -28f,
            shoulderL = -25f, shoulderRollL = -15f, elbowL = 85f,
            shoulderR = AttackPose.Rest.armPitch, shoulderRollR = 12f, elbowR = AttackPose.Rest.elbow,
            hipsLean = 4f, hipsTwist = 12f, chestLean = 5f + breathe * 1.5f, chestTwist = -6f,
            bodyY = -0.035f + breathe * 0.006f,
        };
    }

    Pose Locomotion(Gait g, float run)
    {
        float s = Mathf.Sin(phase);
        float c = Mathf.Cos(phase);
        float kneeShift = Mathf.Lerp(0.1f, 0.5f, run); // runners fold the knee right after toe-off

        var p = new Pose
        {
            hipL = -g.legSwing * s,
            hipR = g.legSwing * s,
            kneeL = g.kneeStance + g.kneeSwing * Mathf.Max(0f, Mathf.Cos(phase - kneeShift)),
            kneeR = g.kneeStance + g.kneeSwing * Mathf.Max(0f, -Mathf.Cos(phase - kneeShift)),

            // Arms swing opposite to the legs; the weapon arm swings less and stays forward
            shoulderL = g.armSwing * s,
            shoulderR = -10f - g.armSwing * 0.6f * s,
            shoulderRollL = -6f, shoulderRollR = 8f,
            elbowL = g.elbow + (run > 0.5f ? 10f * s : 0f),
            elbowR = g.elbow + 20f,

            hipsLean = g.lean * 0.4f,
            chestLean = g.lean * 0.6f,
            hipsTwist = g.twist * s,
            chestTwist = -g.twist * 1.5f * s,
            hipsRoll = g.hipRoll * c,

            // Walk: highest when a leg passes under the body. Run: highest in the air between steps.
            bodyY = Mathf.Lerp(g.bob * (Mathf.Abs(c) - 0.5f), g.bob * (Mathf.Abs(s) - 0.5f), run),
        };
        // Keep the feet roughly flat to the ground
        p.ankleL = -(p.hipL + p.kneeL) * 0.7f;
        p.ankleR = -(p.hipR + p.kneeR) * 0.7f;
        return p;
    }

    static Pose Jump(Pose p)
    {
        p.hipL = -55f; p.kneeL = 75f; p.ankleL = -10f;
        p.hipR = -10f; p.kneeR = 45f; p.ankleR = -20f;
        p.shoulderL = -70f; p.elbowL = 40f; p.shoulderRollL = -30f;
        p.bodyY = 0f;
        return p;
    }

    static Pose Stunned(Pose p)
    {
        p.hipsLean = -8f; p.chestLean = -22f; p.headPitch = -20f;
        p.shoulderL = -35f; p.shoulderRollL = -50f; p.elbowL = 25f;
        p.shoulderR = -25f; p.shoulderRollR = 50f; p.elbowR = 30f;
        p.hipL = -15f; p.kneeL = 30f; p.hipR = 10f; p.kneeR = 25f;
        return p;
    }

    static Pose Attacking(Pose p, AttackPose swing)
    {
        p.shoulderR = swing.armPitch;
        p.elbowR = swing.elbow;
        p.shoulderRollR = 12f;
        p.hipsTwist = swing.twist * 0.4f;
        p.chestTwist = swing.twist * 0.6f;
        p.hipsLean = swing.lean * 0.4f;
        p.chestLean = swing.lean * 0.6f;
        p.shoulderL = -50f; p.shoulderRollL = -25f; p.elbowL = 80f;   // guard up
        p.hipL = -30f; p.kneeL = 30f; p.ankleL = 0f;                  // lunging stance
        p.hipR = 20f; p.kneeR = 15f; p.ankleR = -35f;
        return p;
    }

    static Pose Tucked()
    {
        return new Pose
        {
            hipL = -115f, hipR = -115f, kneeL = 135f, kneeR = 135f, ankleL = 30f, ankleR = 30f,
            shoulderL = -70f, shoulderR = -70f, shoulderRollL = -10f, shoulderRollR = 10f, elbowL = 110f, elbowR = 110f,
            hipsLean = 25f, chestLean = 45f, headPitch = 40f,
        };
    }

    static Pose KnockedOut()
    {
        return new Pose
        {
            hipL = -10f, hipR = 15f, kneeL = 20f, kneeR = 5f,
            shoulderL = -20f, shoulderR = -10f, shoulderRollL = -70f, shoulderRollR = 70f, elbowL = 20f, elbowR = 30f,
            headPitch = -15f,
        };
    }

    // ---------------------------------------------------------------- Applying

    void Apply(Pose p, float t, bool snapUpperBody)
    {
        float upperT = snapUpperBody ? 1f : t; // swings and rolls snap so they read fast and clearly

        Set(rig.leftLeg.upper, Quaternion.Euler(p.hipL, 0f, 0f), t);
        Set(rig.rightLeg.upper, Quaternion.Euler(p.hipR, 0f, 0f), t);
        Set(rig.leftLeg.middle, Quaternion.Euler(p.kneeL, 0f, 0f), t);
        Set(rig.rightLeg.middle, Quaternion.Euler(p.kneeR, 0f, 0f), t);
        Set(rig.leftLeg.end, Quaternion.Euler(p.ankleL, 0f, 0f), t);
        Set(rig.rightLeg.end, Quaternion.Euler(p.ankleR, 0f, 0f), t);

        Set(rig.leftArm.upper, Quaternion.Euler(p.shoulderL, 0f, p.shoulderRollL), upperT);
        Set(rig.rightArm.upper, Quaternion.Euler(p.shoulderR, 0f, p.shoulderRollR), upperT);
        Set(rig.leftArm.middle, Quaternion.Euler(-p.elbowL, 0f, 0f), upperT);
        Set(rig.rightArm.middle, Quaternion.Euler(-p.elbowR, 0f, 0f), upperT);

        Set(rig.hips, Quaternion.Euler(p.hipsLean, p.hipsTwist, p.hipsRoll), upperT);
        Set(rig.chest, Quaternion.Euler(p.chestLean, p.chestTwist, 0f), upperT);
        Set(rig.head, Quaternion.Euler(p.headPitch, 0f, 0f), t);

        Vector3 rollPosition = new Vector3(0f, rig.rollPivotHeight + p.bodyY, 0f);
        rig.roll.localPosition = Vector3.Lerp(rig.roll.localPosition, rollPosition, t);
    }

    static void Set(Transform joint, Quaternion target, float t)
    {
        joint.localRotation = t >= 1f ? target : Quaternion.Slerp(joint.localRotation, target, t);
    }

    void AnimateTail(float dt, float speed)
    {
        // Lazy S-curve sway that speeds up and flattens out when running
        float tailSpeed = 2.5f + speed * 0.8f;
        float sway = Mathf.Lerp(10f, 6f, runWeight);
        float lift = runWeight * 12f; // held out straighter behind when running
        for (int i = 0; i < rig.tail.Length; i++)
        {
            float wave = Mathf.Sin(Time.time * tailSpeed - i * 0.5f);
            float baseAngle = tailBaseAngles[i] + (i == 0 ? lift : -lift * 0.3f);
            rig.tail[i].localRotation = Quaternion.Euler(baseAngle + wave * 2f, wave * sway, 0f);
        }
    }
}
