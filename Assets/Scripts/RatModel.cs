using UnityEngine;

public enum RatWeapon { None, Crowbar, Pipe }

/// <summary>Colors and accessories for one rat.</summary>
public struct RatOutfit
{
    public Color fur;
    public Color skin;          // nose, ears, hands, feet, tail
    public Color shirt;
    public Color pants;
    public bool sleeves;        // hoodie sleeves instead of bare furry arms
    public bool hood;
    public bool cap;
    public Color capColor;
    public bool goldChain;
    public RatWeapon weapon;
}

/// <summary>
/// Builds a standing, street-tough rat out of primitives. Model units: feet at y = 0,
/// about 1.7 m tall with ears, facing +Z. Returns the RatRig holding its pivots.
/// </summary>
public static class RatModel
{
    static readonly Color Black = new Color(0.05f, 0.05f, 0.05f);
    static readonly Color Teeth = new Color(1f, 0.97f, 0.85f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.2f);
    static readonly Color CrowbarRed = new Color(0.65f, 0.08f, 0.06f);
    static readonly Color Steel = new Color(0.6f, 0.62f, 0.65f);
    static readonly Color PipeGrey = new Color(0.38f, 0.4f, 0.42f);

    public static RatRig Build(Transform parent, RatOutfit outfit, float scale)
    {
        Transform root = Shapes.Pivot("RatModel", parent, Vector3.zero);
        root.localScale = Vector3.one * scale;
        var rig = root.gameObject.AddComponent<RatRig>();

        Material fur = rig.Mat(outfit.fur);
        Material skin = rig.Mat(outfit.skin);
        Material shirt = rig.Mat(outfit.shirt);
        Material pants = rig.Mat(outfit.pants);
        Material black = rig.Mat(Black);

        // Legs (on the root so the feet stay planted while the upper body twists)
        rig.leftLeg = Leg(root, -1f, pants, skin);
        rig.rightLeg = Leg(root, 1f, pants, skin);

        // Upper body pivots at the hips
        Transform upper = Shapes.Pivot("Upper", root, new Vector3(0f, 0.62f, 0f));
        rig.upper = upper;
        Shapes.Create(PrimitiveType.Cube, "Pelvis", upper, new Vector3(0f, 0.06f, 0f), new Vector3(0.42f, 0.2f, 0.3f), pants);
        Shapes.Create(PrimitiveType.Capsule, "Torso", upper, new Vector3(0f, 0.35f, 0f), new Vector3(0.46f, 0.3f, 0.36f), shirt);
        if (outfit.goldChain)
            Shapes.Create(PrimitiveType.Cylinder, "GoldChain", upper, new Vector3(0f, 0.6f, 0.02f), new Vector3(0.28f, 0.012f, 0.28f), rig.Mat(Gold));

        // Head
        Transform head = Shapes.Pivot("Head", upper, new Vector3(0f, 0.65f, 0f));
        rig.head = head;
        Shapes.Create(PrimitiveType.Sphere, "Skull", head, new Vector3(0f, 0.13f, 0.02f), new Vector3(0.36f, 0.34f, 0.38f), fur);
        Shapes.Create(PrimitiveType.Sphere, "Snout", head, new Vector3(0f, 0.08f, 0.24f), new Vector3(0.2f, 0.17f, 0.3f), fur);
        Shapes.Create(PrimitiveType.Sphere, "Nose", head, new Vector3(0f, 0.1f, 0.39f), new Vector3(0.07f, 0.06f, 0.06f), skin);
        Shapes.Create(PrimitiveType.Cube, "Teeth", head, new Vector3(0f, 0.015f, 0.355f), new Vector3(0.05f, 0.045f, 0.02f), rig.Mat(Teeth));
        for (int s = -1; s <= 1; s += 2)
        {
            Shapes.Create(PrimitiveType.Sphere, "Eye", head, new Vector3(0.09f * s, 0.19f, 0.17f), new Vector3(0.06f, 0.06f, 0.06f), black);
            Shapes.Create(PrimitiveType.Sphere, "Ear", head, new Vector3(0.15f * s, 0.34f, -0.01f), new Vector3(0.2f, 0.2f, 0.05f), fur);
            Shapes.Create(PrimitiveType.Sphere, "EarInner", head, new Vector3(0.15f * s, 0.34f, 0.012f), new Vector3(0.13f, 0.13f, 0.03f), skin);
            for (int w = -1; w <= 1; w += 2)
            {
                Shapes.Create(PrimitiveType.Cube, "Whisker", head, new Vector3(0.12f * s, 0.08f + 0.02f * w, 0.33f),
                    Quaternion.Euler(0f, 0f, 12f * w * s), new Vector3(0.2f, 0.006f, 0.006f), black);
            }
        }

        if (outfit.cap)
        {
            Material cap = rig.Mat(outfit.capColor);
            Shapes.Create(PrimitiveType.Sphere, "Cap", head, new Vector3(0f, 0.25f, 0.01f), new Vector3(0.37f, 0.2f, 0.39f), cap);
            Shapes.Create(PrimitiveType.Cube, "CapBrim", head, new Vector3(0f, 0.2f, -0.2f), new Vector3(0.26f, 0.03f, 0.16f), cap); // worn backwards
        }
        if (outfit.hood)
            Shapes.Create(PrimitiveType.Sphere, "Hood", head, new Vector3(0f, 0.08f, -0.1f), new Vector3(0.42f, 0.36f, 0.34f), shirt);

        // Arms
        Material armMaterial = outfit.sleeves ? shirt : fur;
        rig.leftArm = Arm(upper, -1f, armMaterial, skin);
        rig.rightArm = Arm(upper, 1f, armMaterial, skin);

        // Weapon in the right hand; its local Y axis runs along the weapon
        if (outfit.weapon != RatWeapon.None)
        {
            Transform grip = Shapes.Pivot("Weapon", rig.rightArm, new Vector3(0f, -0.4f, 0f));
            grip.localRotation = Quaternion.Euler(70f, 0f, 0f); // points forward and up when the arm hangs down
            rig.weapon = grip;
            if (outfit.weapon == RatWeapon.Crowbar) Crowbar(grip, rig);
            else Pipe(grip, rig);
        }

        // Long pink tail, a chain of segments that the animator sways
        rig.tail = new Transform[7];
        Transform previous = upper;
        Vector3 position = new Vector3(0f, 0.08f, -0.17f);
        const float segmentLength = 0.16f;
        for (int i = 0; i < rig.tail.Length; i++)
        {
            Transform segment = Shapes.Pivot("Tail" + i, previous, position);
            segment.localRotation = Quaternion.Euler(i == 0 ? -40f : 10f, 0f, 0f); // droop down, then curl up
            float radius = Mathf.Lerp(0.07f, 0.025f, i / (float)(rig.tail.Length - 1));
            Shapes.Create(PrimitiveType.Cylinder, "TailPart", segment, new Vector3(0f, 0f, -segmentLength / 2f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(radius, segmentLength / 2f + 0.01f, radius), skin);
            rig.tail[i] = segment;
            previous = segment;
            position = new Vector3(0f, 0f, -segmentLength);
        }

        root.gameObject.AddComponent<RatAnimator>();
        return rig;
    }

    static Transform Leg(Transform root, float side, Material pants, Material skin)
    {
        Transform pivot = Shapes.Pivot(side < 0 ? "LegLeft" : "LegRight", root, new Vector3(0.12f * side, 0.62f, 0f));
        Shapes.Create(PrimitiveType.Cylinder, "Leg", pivot, new Vector3(0f, -0.27f, 0f), new Vector3(0.16f, 0.27f, 0.16f), pants);
        Shapes.Create(PrimitiveType.Sphere, "Foot", pivot, new Vector3(0f, -0.58f, 0.07f), new Vector3(0.13f, 0.08f, 0.28f), skin);
        return pivot;
    }

    static Transform Arm(Transform upper, float side, Material arm, Material skin)
    {
        Transform pivot = Shapes.Pivot(side < 0 ? "ArmLeft" : "ArmRight", upper, new Vector3(0.27f * side, 0.53f, 0f));
        pivot.localRotation = Quaternion.Euler(0f, 0f, 10f * side);
        Shapes.Create(PrimitiveType.Cylinder, "Arm", pivot, new Vector3(0f, -0.19f, 0f), new Vector3(0.1f, 0.19f, 0.1f), arm);
        Shapes.Create(PrimitiveType.Sphere, "Hand", pivot, new Vector3(0f, -0.4f, 0f), new Vector3(0.11f, 0.11f, 0.11f), skin);
        return pivot;
    }

    static void Crowbar(Transform grip, RatRig rig)
    {
        Material red = rig.Mat(CrowbarRed);
        Material steel = rig.Mat(Steel);
        Shapes.Create(PrimitiveType.Cube, "Shaft", grip, new Vector3(0f, 0.28f, 0f), new Vector3(0.04f, 0.8f, 0.04f), red);
        Shapes.Create(PrimitiveType.Cube, "Hook", grip, new Vector3(0.057f, 0.72f, 0f), Quaternion.Euler(0f, 0f, -55f), new Vector3(0.04f, 0.14f, 0.04f), red);
        Shapes.Create(PrimitiveType.Cube, "Claw", grip, new Vector3(0.14f, 0.735f, 0f), Quaternion.Euler(0f, 0f, -130f), new Vector3(0.035f, 0.1f, 0.035f), steel);
        Shapes.Create(PrimitiveType.Cube, "Chisel", grip, new Vector3(0f, -0.14f, 0.02f), Quaternion.Euler(25f, 0f, 0f), new Vector3(0.05f, 0.08f, 0.02f), steel);
    }

    static void Pipe(Transform grip, RatRig rig)
    {
        Material pipe = rig.Mat(PipeGrey);
        Shapes.Create(PrimitiveType.Cylinder, "Pipe", grip, new Vector3(0f, 0.3f, 0f), new Vector3(0.06f, 0.45f, 0.06f), pipe);
        Shapes.Create(PrimitiveType.Cylinder, "Fitting", grip, new Vector3(0f, -0.13f, 0f), new Vector3(0.085f, 0.04f, 0.085f), pipe);
        Shapes.Create(PrimitiveType.Cylinder, "Fitting", grip, new Vector3(0f, 0.73f, 0f), new Vector3(0.085f, 0.04f, 0.085f), pipe);
    }
}
