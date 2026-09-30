using System.Collections.Generic;
using UnityEngine;

public enum AttackKind { Light, Heavy }

/// <summary>Arm/body pose used by the animator while swinging a weapon.</summary>
public struct AttackPose
{
    public float armPitch;   // right arm rotation around X (negative = raised forward/up)
    public float twist;      // upper body turn around Y
    public float lean;       // upper body lean around X (positive = forward)
    public float elbow;      // right elbow bend in degrees

    /// <summary>Fighting stance the swings start from and return to.</summary>
    public static readonly AttackPose Rest = new AttackPose(-30f, 0f, 0f, 50f);

    public AttackPose(float armPitch, float twist, float lean, float elbow)
    {
        this.armPitch = armPitch;
        this.twist = twist;
        this.lean = lean;
        this.elbow = elbow;
    }

    public static AttackPose Lerp(AttackPose a, AttackPose b, float t) => new AttackPose(
        Mathf.Lerp(a.armPitch, b.armPitch, t), Mathf.Lerp(a.twist, b.twist, t),
        Mathf.Lerp(a.lean, b.lean, t), Mathf.Lerp(a.elbow, b.elbow, t));
}

[System.Serializable]
public class AttackData
{
    public string name;
    public float windup;        // telegraph before the swing
    public float active;        // the swing itself; damage lands halfway through
    public float recovery;      // vulnerable afterwards
    public float damage;
    public float range = 1.9f;
    public float arc = 110f;    // degrees in front that get hit
    public float knockback = 4f;
    public float launch;        // upward pop on hit
    public float stun = 0.35f;
    public float lunge = 3f;    // forward step during the swing
    public float hitStop = 0.05f;
    public AttackPose windupPose;
    public AttackPose strikePose;
}

/// <summary>
/// Weapon attacks with windup / active / recovery phases, a light combo chain with input
/// buffering, a heavy attack, soft auto-aim, and hit detection against other teams.
/// </summary>
public class MeleeAttacker : MonoBehaviour
{
    public AttackData[] lightCombo = DefaultLightCombo();
    public AttackData heavy = DefaultHeavy();
    public float damageMultiplier = 1f;
    public float windupMultiplier = 1f;     // > 1 = slower, easier-to-read attacks
    public float comboWindow = 0.45f;
    public float autoAimRange = 4.5f;

    Combatant self;
    AttackData current;
    bool currentIsLight;
    float startTime;
    bool hitDone;
    int comboIndex = -1;
    float comboExpires;
    bool queuedLight;

    public bool IsAttacking => current != null;
    public AttackData Current => current;

    float Windup => current.windup * windupMultiplier;
    float Elapsed => Time.time - startTime;

    /// <summary>Forward movement during the swing, for the movement script to add.</summary>
    public Vector3 LungeVelocity =>
        current != null && Elapsed >= Windup && Elapsed < Windup + current.active
            ? transform.forward * current.lunge
            : Vector3.zero;

    void Awake() => self = GetComponent<Combatant>();

    /// <summary>
    /// Starts an attack, or buffers the next light hit if one is already in progress.
    /// Returns true if the attack started or was queued.
    /// </summary>
    public bool TryAttack(AttackKind kind, Vector3 preferredDirection)
    {
        if (self && (self.IsDead || self.IsStunned || self.IsDodging)) return false;

        if (current != null)
        {
            if (kind != AttackKind.Light || queuedLight) return false;
            queuedLight = true;
            return true;
        }

        if (kind == AttackKind.Heavy)
        {
            comboIndex = -1;
            Begin(heavy, false, preferredDirection);
        }
        else
        {
            comboIndex = Time.time <= comboExpires ? (comboIndex + 1) % lightCombo.Length : 0;
            Begin(lightCombo[comboIndex], true, preferredDirection);
        }
        return true;
    }

    public void Cancel()
    {
        current = null;
        queuedLight = false;
    }

    void Begin(AttackData attack, bool isLight, Vector3 preferredDirection)
    {
        current = attack;
        currentIsLight = isLight;
        startTime = Time.time;
        hitDone = false;
        queuedLight = false;
        FaceTarget(preferredDirection);
    }

    void FaceTarget(Vector3 preferredDirection)
    {
        Combatant nearest = null;
        float best = autoAimRange;
        foreach (Combatant other in Combatant.All)
        {
            if (!IsEnemy(other)) continue;
            float distance = Vector3.Distance(other.transform.position, transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = other;
            }
        }

        Vector3 direction = nearest ? nearest.transform.position - transform.position : preferredDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
    }

    bool IsEnemy(Combatant other) => other && other != self && !other.IsDead && (!self || other.team != self.team);

    void Update()
    {
        if (current == null) return;
        if (self && (self.IsDead || self.IsStunned))
        {
            Cancel();
            return;
        }

        if (!hitDone && Elapsed >= Windup + current.active * 0.5f)
        {
            hitDone = true;
            DealHits();
        }

        if (Elapsed >= Windup + current.active + current.recovery)
        {
            bool continueCombo = currentIsLight && queuedLight;
            if (currentIsLight) comboExpires = Time.time + comboWindow;
            current = null;
            queuedLight = false;
            if (continueCombo) TryAttack(AttackKind.Light, transform.forward);
        }
    }

    void DealHits()
    {
        bool hitSomething = false;
        foreach (Combatant other in new List<Combatant>(Combatant.All))
        {
            if (!IsEnemy(other)) continue;

            Vector3 toOther = other.transform.position - transform.position;
            if (Mathf.Abs(toOther.y) > 1.5f) continue;
            toOther.y = 0f;
            float distance = toOther.magnitude;
            if (distance > current.range + 0.4f) continue;
            if (distance > 0.6f && Vector3.Angle(transform.forward, toOther) > current.arc * 0.5f) continue;

            hitSomething |= other.TakeHit(current.damage * damageMultiplier, transform.position,
                current.knockback, current.launch, current.stun);
        }

        if (hitSomething && GameManager.Instance) GameManager.Instance.HitStop(current.hitStop);
    }

    /// <summary>Pose for the animator at this moment of the attack.</summary>
    public AttackPose GetPose()
    {
        if (current == null) return AttackPose.Rest;

        float t = Elapsed;
        if (t < Windup)
        {
            float k = t / Mathf.Max(Windup, 0.0001f);
            return AttackPose.Lerp(AttackPose.Rest, current.windupPose, 1f - (1f - k) * (1f - k));
        }
        t -= Windup;
        if (t < current.active)
        {
            float k = t / current.active;
            return AttackPose.Lerp(current.windupPose, current.strikePose, 1f - Mathf.Pow(1f - k, 3f));
        }
        t -= current.active;
        float r = Mathf.Clamp01(t / Mathf.Max(current.recovery, 0.0001f));
        return AttackPose.Lerp(current.strikePose, AttackPose.Rest, r * r);
    }

    public static AttackData[] DefaultLightCombo() => new[]
    {
        new AttackData
        {
            name = "Swing", windup = 0.16f, active = 0.1f, recovery = 0.22f, damage = 10f,
            knockback = 4f, stun = 0.35f, lunge = 3f,
            windupPose = new AttackPose(-150f, 40f, -5f, 70f), strikePose = new AttackPose(-20f, -30f, 15f, 5f),
        },
        new AttackData
        {
            name = "Backhand", windup = 0.14f, active = 0.1f, recovery = 0.22f, damage = 10f,
            knockback = 4f, stun = 0.35f, lunge = 3f,
            windupPose = new AttackPose(-100f, -50f, 0f, 90f), strikePose = new AttackPose(-70f, 60f, 10f, 10f),
        },
        new AttackData
        {
            name = "Slam", windup = 0.26f, active = 0.1f, recovery = 0.4f, damage = 18f,
            knockback = 9f, launch = 4f, stun = 0.6f, lunge = 5f, hitStop = 0.09f,
            windupPose = new AttackPose(-175f, 10f, -15f, 60f), strikePose = new AttackPose(5f, 0f, 30f, 0f),
        },
    };

    public static AttackData DefaultHeavy() => new AttackData
    {
        name = "Heavy", windup = 0.5f, active = 0.12f, recovery = 0.5f, damage = 28f, range = 2.2f,
        knockback = 12f, launch = 5f, stun = 0.8f, lunge = 6f, hitStop = 0.12f,
        windupPose = new AttackPose(-180f, 30f, -25f, 100f), strikePose = new AttackPose(15f, -10f, 35f, 0f),
    };
}
