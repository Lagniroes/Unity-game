using UnityEngine;

/// <summary>
/// Looping office animation for a seated rat: types on the keyboard, reaches for the coffee
/// mug, lifts it to its snout, sips with eyes closed, lets out a happy "ahh", puts the mug
/// back and goes back to typing. Hands are placed with two-bone arm IK, so they really hold
/// the mug handle and land on the keys.
/// </summary>
public class CoffeeRatAnimator : MonoBehaviour
{
    public RatRig rig;
    public Transform mug;               // pivot at the bottom center, handle on its +X side
    public Transform mugRest;           // where the mug sits on the desk
    public Transform keyboard;          // top surface center of the keyboard
    public float loopLength = 12f;

    static readonly Vector3 HandleLocal = new Vector3(0.075f, 0.05f, 0f);
    static readonly Vector3 MugUnderSnout = new Vector3(0f, -0.14f, 0.31f); // head space: mug bottom while sipping
    const float MugHeight = 0.1f;

    public float LoopTime { get; private set; }
    public float Typing { get; private set; }   // 0..1, used by the monitor to scroll code
    public float Ahh { get; private set; }      // 0..1, used for the speech bubble

    Transform[] steam;
    Transform[] lids;
    Vector3[] lidPositions;
    Vector3[] lidScales;
    float[] tailBaseAngles;
    float nextBlink;
    float blinkStart = -10f;

    void Start()
    {
        tailBaseAngles = new float[rig.tail.Length];
        for (int i = 0; i < rig.tail.Length; i++) tailBaseAngles[i] = rig.tail[i].localEulerAngles.x;

        // Eyelids are the head's children named "Lid"; we slide them down to blink
        var found = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in rig.head)
            if (child.name == "Lid") found.Add(child);
        lids = found.ToArray();
        lidPositions = new Vector3[lids.Length];
        lidScales = new Vector3[lids.Length];
        for (int i = 0; i < lids.Length; i++)
        {
            lidPositions[i] = lids[i].localPosition;
            lidScales[i] = lids[i].localScale;
        }

        // Little puffs of steam rising from the mug
        Material steamMaterial = Shapes.Shared(new Color(0.92f, 0.92f, 0.95f));
        steam = new Transform[7];
        for (int i = 0; i < steam.Length; i++)
            steam[i] = Shapes.Create(PrimitiveType.Sphere, "Steam", transform.parent, Vector3.zero, Vector3.one * 0.03f, steamMaterial);

        nextBlink = Time.time + 2f;
    }

    void LateUpdate()
    {
        LoopTime = Mathf.Repeat(Time.time, loopLength);
        float t = LoopTime;

        // Story beats (all 0..1, eased)
        float rightTyping = 1f - Ease(3.0f, 3.6f, t) + Ease(9.0f, 9.6f, t);
        float leftTyping = 1f - Ease(4.2f, 4.8f, t) + Ease(7.8f, 8.6f, t);
        float reach = Ease(3.0f, 4.0f, t) - Ease(8.8f, 9.6f, t);      // right hand on the mug
        float lift = Ease(4.0f, 5.2f, t) - Ease(7.6f, 8.8f, t);       // mug up at the snout
        float sip = Ease(5.2f, 5.8f, t) - Ease(6.4f, 6.9f, t);        // tilted, drinking
        Ahh = Ease(6.9f, 7.2f, t) - Ease(7.8f, 8.4f, t);
        Typing = Mathf.Max(rightTyping, leftTyping) * (1f - lift);
        float lookAtMug = reach * (1f - lift);
        float breathe = Mathf.Sin(Time.time * 2f);

        PoseBody(lift, sip, lookAtMug, breathe, rightTyping);
        PlaceMug(lift, sip);
        PlaceHands(leftTyping, rightTyping, reach);
        AnimateSteam(sip);
        AnimateEyes(sip);
        AnimateTail();
    }

    // ---------------------------------------------------------------- Body

    void PoseBody(float lift, float sip, float lookAtMug, float breathe, float rightTyping)
    {
        // Seated: thighs forward on the chair, shins straight down, feet flat
        float footTap = Mathf.Max(0f, Mathf.Sin(Time.time * 9f)) * 8f * rightTyping;
        rig.leftLeg.upper.localRotation = Quaternion.Euler(-88f, 0f, -3f);
        rig.rightLeg.upper.localRotation = Quaternion.Euler(-88f, 0f, 3f);
        rig.leftLeg.middle.localRotation = Quaternion.Euler(92f, 0f, 0f);
        rig.rightLeg.middle.localRotation = Quaternion.Euler(92f, 0f, 0f);
        rig.leftLeg.end.localRotation = Quaternion.Euler(-4f, 0f, 0f);
        rig.rightLeg.end.localRotation = Quaternion.Euler(-4f - footTap, 0f, 0f);

        // Hunched toward the screen while typing, leaning in to sip, sitting back for the "ahh"
        float chestLean = 14f + breathe * 1.2f - 6f * lift - 12f * Ahh;
        float chestRoll = 4f * Ahh;
        rig.hips.localRotation = Quaternion.Euler(-4f - 3f * Ahh, 0f, 0f);
        rig.chest.localRotation = Quaternion.Euler(chestLean, 8f * lookAtMug, chestRoll);

        // Eyes on the monitor, then on the mug, head tipped back while drinking, a happy tilt after
        float pitch = 6f + 18f * lookAtMug - 22f * sip - 6f * Ahh;
        float yaw = 22f * lookAtMug;
        float roll = 10f * Ahh * Mathf.Sin(Time.time * 1.5f + 1f);
        rig.head.localRotation = Quaternion.Euler(pitch, yaw, roll);

        rig.roll.localRotation = Quaternion.identity;
        rig.roll.localPosition = new Vector3(0f, rig.rollPivotHeight + breathe * 0.003f, 0f);
    }

    void PlaceMug(float lift, float sip)
    {
        Vector3 rest = mugRest.position;
        Vector3 atSnout = rig.head.TransformPoint(MugUnderSnout);
        Vector3 position = Vector3.Lerp(rest, atSnout, lift) + Vector3.up * Mathf.Sin(lift * Mathf.PI) * 0.05f;

        // Upright on the desk, tipped toward the rat to drink
        Quaternion upright = Quaternion.LookRotation(transform.parent.forward, Vector3.up);
        Quaternion tilt = Quaternion.AngleAxis(-55f * sip - 8f * lift, transform.parent.right);
        mug.SetPositionAndRotation(position, tilt * upright);
    }

    void PlaceHands(float leftTyping, float rightTyping, float reach)
    {
        Transform body = transform.parent;
        float scale = transform.lossyScale.x;
        float upper = rig.leftArm.middle.localPosition.magnitude * scale;
        float lower = rig.leftArm.end.localPosition.magnitude * scale;

        // Fist knuckles sit about 9 cm below the wrist, so wrists float above the keys
        Vector3 keys = keyboard.position + Vector3.up * 0.09f;
        float tapL = Mathf.Max(0f, Mathf.Sin(Time.time * 17f)) * 0.015f;
        float tapR = Mathf.Max(0f, Mathf.Sin(Time.time * 17f + 2f)) * 0.015f;

        Vector3 leftKeys = keys - body.right * 0.08f + Vector3.up * tapL;
        Vector3 leftResting = keyboard.position - body.right * 0.26f - body.forward * 0.04f + Vector3.up * 0.08f;
        Vector3 leftTarget = Vector3.Lerp(leftResting, leftKeys, leftTyping);

        Vector3 rightKeys = keys + body.right * 0.08f + Vector3.up * tapR;
        Vector3 onHandle = mug.TransformPoint(HandleLocal) + Vector3.up * 0.06f + body.right * 0.02f;
        Vector3 rightTarget = Vector3.Lerp(rightKeys, onHandle, reach);
        // Arc up and over instead of sliding through the keyboard
        rightTarget += Vector3.up * Mathf.Sin(reach * Mathf.PI) * 0.06f * (1f - rightTyping);

        Vector3 down = Vector3.down;
        SolveArm(rig.leftArm, leftTarget, -body.right * 0.6f + down - body.forward * 0.4f, upper, lower);
        SolveArm(rig.rightArm, rightTarget, body.right * 0.6f + down - body.forward * 0.4f, upper, lower);

        rig.leftArm.end.localRotation = Quaternion.Euler(-20f, 0f, 0f);
        rig.rightArm.end.localRotation = Quaternion.Euler(-20f + 30f * reach, 0f, 0f);
    }

    /// <summary>
    /// Analytic two-bone IK: aims the upper arm and bends the elbow so the wrist reaches
    /// <paramref name="target"/>, with the elbow pointing toward <paramref name="pole"/>.
    /// The elbow is a hinge that bends the forearm toward the upper arm's local +Z.
    /// </summary>
    static void SolveArm(RatRig.Limb arm, Vector3 target, Vector3 pole, float upperLength, float lowerLength)
    {
        Vector3 shoulder = arm.upper.position;
        Vector3 toTarget = target - shoulder;
        float distance = toTarget.magnitude;
        if (distance < 0.0001f) return;
        Vector3 forward = toTarget / distance;
        distance = Mathf.Clamp(distance, Mathf.Abs(upperLength - lowerLength) + 0.01f, (upperLength + lowerLength) * 0.999f);

        float cosShoulder = (upperLength * upperLength + distance * distance - lowerLength * lowerLength) / (2f * upperLength * distance);
        float cosElbow = (upperLength * upperLength + lowerLength * lowerLength - distance * distance) / (2f * upperLength * lowerLength);
        float shoulderAngle = Mathf.Acos(Mathf.Clamp(cosShoulder, -1f, 1f));
        float elbowAngle = Mathf.Acos(Mathf.Clamp(cosElbow, -1f, 1f)) * Mathf.Rad2Deg;

        Vector3 poleDirection = Vector3.ProjectOnPlane(pole, forward);
        if (poleDirection.sqrMagnitude < 0.000001f) poleDirection = Vector3.ProjectOnPlane(Vector3.down, forward);
        poleDirection.Normalize();

        Vector3 upperDirection = forward * Mathf.Cos(shoulderAngle) + poleDirection * Mathf.Sin(shoulderAngle);
        Vector3 bendSide = Vector3.ProjectOnPlane(forward, upperDirection);
        if (bendSide.sqrMagnitude < 0.000001f) bendSide = -poleDirection;

        arm.upper.rotation = Quaternion.LookRotation(bendSide.normalized, -upperDirection);
        arm.middle.localRotation = Quaternion.Euler(-(180f - elbowAngle), 0f, 0f);
    }

    // ---------------------------------------------------------------- Details

    void AnimateSteam(float sip)
    {
        Vector3 top = mug.TransformPoint(new Vector3(0f, MugHeight, 0f));
        for (int i = 0; i < steam.Length; i++)
        {
            float p = Mathf.Repeat(Time.time * 0.45f + i / (float)steam.Length, 1f);
            float wiggle = Mathf.Sin(Time.time * 2.3f + i * 1.7f) * 0.03f * p;
            steam[i].position = top + new Vector3(wiggle, p * 0.3f, wiggle * 0.5f);
            float size = Mathf.Sin(p * Mathf.PI) * 0.06f * (1f - sip * 0.7f); // grow, then fade away
            steam[i].localScale = Vector3.one * Mathf.Max(size, 0.0001f);
        }
    }

    void AnimateEyes(float sip)
    {
        if (Time.time >= nextBlink)
        {
            blinkStart = Time.time;
            nextBlink = Time.time + Random.Range(2.5f, 5f);
        }
        float blink = Mathf.Clamp01(1f - Mathf.Abs((Time.time - blinkStart) / 0.08f - 1f));
        float closed = Mathf.Max(blink, sip, Ahh * 0.65f); // eyes shut while enjoying the coffee

        for (int i = 0; i < lids.Length; i++)
        {
            lids[i].localPosition = lidPositions[i] + Vector3.down * 0.022f * closed;
            lids[i].localScale = lidScales[i] + new Vector3(0f, 0.045f, 0.006f) * closed;
        }
    }

    void AnimateTail()
    {
        // Lazy sway off the back of the chair, with a happy flick during the "ahh"
        float flick = Ahh * Mathf.Sin(Time.time * 9f) * 18f;
        for (int i = 0; i < rig.tail.Length; i++)
        {
            float wave = Mathf.Sin(Time.time * 1.6f - i * 0.45f);
            rig.tail[i].localRotation = Quaternion.Euler(tailBaseAngles[i] + wave * 2f + Ahh * 4f, wave * 7f + flick * (i / (float)rig.tail.Length), 0f);
        }
    }

    static float Ease(float start, float end, float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, t));
}
