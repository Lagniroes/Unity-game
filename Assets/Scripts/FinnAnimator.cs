using UnityEngine;

/// <summary>
/// Simple code-driven animation for FinnModel: swinging arms and legs while running,
/// a jump pose in the air, and a little breathing bob while standing still.
/// </summary>
public class FinnAnimator : MonoBehaviour
{
    public Transform body;
    public Transform leftArm;
    public Transform rightArm;
    public Transform leftLeg;
    public Transform rightLeg;

    public float strideFrequency = 1.6f;   // swings per meter travelled
    public float legSwing = 40f;
    public float armSwing = 45f;
    public float blendSpeed = 12f;

    CharacterController controller;
    float phase;
    Vector3 bodyRestPosition;

    void Awake()
    {
        controller = GetComponentInParent<CharacterController>();
    }

    void Start()
    {
        bodyRestPosition = body.localPosition;
    }

    void LateUpdate()
    {
        Vector3 velocity = controller ? controller.velocity : Vector3.zero;
        float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        bool grounded = !controller || controller.isGrounded;

        phase += speed * strideFrequency * Time.deltaTime;
        float swing = Mathf.Sin(phase * Mathf.PI);
        float amount = Mathf.Clamp01(speed / 6f);

        float leftLegAngle, rightLegAngle, leftArmAngle, rightArmAngle, armSpread;
        Vector3 bodyOffset;

        if (!grounded)
        {
            // Jump pose: one knee up, arms thrown up and out
            leftLegAngle = -35f;
            rightLegAngle = 15f;
            leftArmAngle = -150f;
            rightArmAngle = -150f;
            armSpread = 30f;
            bodyOffset = Vector3.zero;
        }
        else if (amount > 0.05f)
        {
            leftLegAngle = swing * legSwing * amount;
            rightLegAngle = -leftLegAngle;
            leftArmAngle = -swing * armSwing * amount;
            rightArmAngle = -leftArmAngle;
            armSpread = 8f;
            bodyOffset = Vector3.up * Mathf.Abs(swing) * 0.06f * amount;
        }
        else
        {
            float breathe = Mathf.Sin(Time.time * 2f);
            leftLegAngle = rightLegAngle = 0f;
            leftArmAngle = rightArmAngle = breathe * 3f;
            armSpread = 8f;
            bodyOffset = Vector3.up * breathe * 0.01f;
        }

        float t = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
        Blend(leftLeg, Quaternion.Euler(leftLegAngle, 0f, 0f), t);
        Blend(rightLeg, Quaternion.Euler(rightLegAngle, 0f, 0f), t);
        Blend(leftArm, Quaternion.Euler(leftArmAngle, 0f, -armSpread), t);
        Blend(rightArm, Quaternion.Euler(rightArmAngle, 0f, armSpread), t);
        body.localPosition = Vector3.Lerp(body.localPosition, bodyRestPosition + bodyOffset, t);
    }

    static void Blend(Transform limb, Quaternion target, float t)
    {
        limb.localRotation = Quaternion.Slerp(limb.localRotation, target, t);
    }
}
