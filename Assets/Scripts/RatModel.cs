using UnityEngine;

public enum RatWeapon { None, Crowbar, Pipe }

/// <summary>Colors and accessories for one rat.</summary>
public struct RatOutfit
{
    public Color fur;
    public Color furLight;      // muzzle and chest
    public Color skin;          // nose, ears, hands, feet, tail
    public Color shirt;
    public Color pants;
    public bool tankTop;        // bare furry shoulders and arms
    public bool hood;           // hoodie: sleeves, hood, drawstrings, front pocket
    public bool sleeves;        // long shirt sleeves (office shirt)
    public bool tie;            // shirt collar and necktie
    public Color tieColor;
    public bool glasses;
    public bool cap;
    public Color capColor;
    public bool goldChain;
    public bool earring;
    public bool sneakers;       // otherwise bare rat feet with claws
    public Color sneakerColor;
    public bool handWraps;
    public bool scar;
    public bool goldTooth;
    public RatWeapon weapon;
}

/// <summary>
/// Builds a detailed, standing street rat out of primitives. Model units: feet at y = 0,
/// about 1.75 m tall with ears, facing +Z. Arms and legs have elbows and knees, and the
/// whole body hangs from a belly pivot so it can do forward rolls.
/// </summary>
public static class RatModel
{
    static readonly Color Black = new Color(0.04f, 0.04f, 0.04f);
    static readonly Color EyeWhite = new Color(0.95f, 0.93f, 0.88f);
    static readonly Color Teeth = new Color(1f, 0.96f, 0.82f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.2f);
    static readonly Color Claw = new Color(0.9f, 0.85f, 0.75f);
    static readonly Color Leather = new Color(0.25f, 0.15f, 0.08f);
    static readonly Color Rubber = new Color(0.95f, 0.95f, 0.93f);
    static readonly Color CrowbarRed = new Color(0.65f, 0.08f, 0.06f);
    static readonly Color Steel = new Color(0.6f, 0.62f, 0.65f);
    static readonly Color PipeGrey = new Color(0.38f, 0.4f, 0.42f);
    static readonly Color Rust = new Color(0.45f, 0.25f, 0.12f);
    static readonly Color Scar = new Color(0.75f, 0.45f, 0.45f);
    static readonly Color Tape = new Color(0.92f, 0.9f, 0.85f);

    const float HipHeight = 0.62f;
    const float RollHeight = 0.55f;

    /// <param name="animate">Adds the gameplay RatAnimator. Pass false when another script poses the rat.</param>
    public static RatRig Build(Transform parent, RatOutfit outfit, float scale, bool animate = true)
    {
        Transform root = Shapes.Pivot("RatModel", parent, Vector3.zero);
        root.localScale = Vector3.one * scale;
        var rig = root.gameObject.AddComponent<RatRig>();

        // Roll pivot at belly height, with an offset child so everything below uses feet-at-zero coordinates
        rig.roll = Shapes.Pivot("RollPivot", root, new Vector3(0f, RollHeight, 0f));
        rig.rollPivotHeight = RollHeight;
        Transform body = Shapes.Pivot("Body", rig.roll, new Vector3(0f, -RollHeight, 0f));

        rig.leftLeg = Leg(body, -1f, outfit, rig);
        rig.rightLeg = Leg(body, 1f, outfit, rig);

        rig.hips = Shapes.Pivot("Hips", body, new Vector3(0f, HipHeight, 0f));
        Pelvis(rig.hips, outfit, rig);
        Tail(rig.hips, outfit, rig);

        rig.chest = Shapes.Pivot("Chest", rig.hips, new Vector3(0f, 0.2f, 0f));
        Torso(rig.chest, outfit, rig);

        rig.head = Shapes.Pivot("Head", rig.chest, new Vector3(0f, 0.46f, 0f));
        Head(rig.head, outfit, rig);

        rig.leftArm = Arm(rig.chest, -1f, outfit, rig);
        rig.rightArm = Arm(rig.chest, 1f, outfit, rig);

        if (outfit.weapon != RatWeapon.None)
        {
            // Held in the right fist; the weapon's local Y axis runs along it
            Transform grip = Shapes.Pivot("Weapon", rig.rightArm.end, new Vector3(0f, -0.07f, 0.02f));
            grip.localRotation = Quaternion.Euler(70f, 0f, 0f);
            rig.weapon = grip;
            if (outfit.weapon == RatWeapon.Crowbar) Crowbar(grip, rig);
            else Pipe(grip, rig);
        }

        if (animate) root.gameObject.AddComponent<RatAnimator>();
        return rig;
    }

    // ---------------------------------------------------------------- Legs

    static RatRig.Limb Leg(Transform body, float side, RatOutfit o, RatRig rig)
    {
        Material pants = rig.Mat(o.pants);
        Material pantsDark = rig.Mat(o.pants * 0.75f + Color.black * 0.25f);
        var limb = new RatRig.Limb();

        // Thigh
        limb.upper = Shapes.Pivot(side < 0 ? "HipLeft" : "HipRight", body, new Vector3(0.12f * side, HipHeight, 0f));
        Shapes.Create(PrimitiveType.Capsule, "Thigh", limb.upper, new Vector3(0f, -0.15f, 0f), new Vector3(0.18f, 0.17f, 0.18f), pants);
        Shapes.Create(PrimitiveType.Cube, "Seam", limb.upper, new Vector3(0.088f * side, -0.15f, 0f), new Vector3(0.01f, 0.26f, 0.02f), pantsDark);

        // Shin
        limb.middle = Shapes.Pivot("Knee", limb.upper, new Vector3(0f, -0.3f, 0f));
        Shapes.Create(PrimitiveType.Sphere, "KneeCap", limb.middle, Vector3.zero, new Vector3(0.165f, 0.165f, 0.165f), pants);
        Shapes.Create(PrimitiveType.Capsule, "Shin", limb.middle, new Vector3(0f, -0.12f, 0f), new Vector3(0.155f, 0.14f, 0.155f), pants);
        Shapes.Create(PrimitiveType.Cylinder, "Cuff", limb.middle, new Vector3(0f, -0.22f, 0f), new Vector3(0.175f, 0.03f, 0.175f), pantsDark);

        // Foot
        limb.end = Shapes.Pivot("Ankle", limb.middle, new Vector3(0f, -0.26f, 0f));
        if (o.sneakers) Sneaker(limb.end, o, rig);
        else RatFoot(limb.end, rig.Mat(o.skin), rig);
        return limb;
    }

    static void Sneaker(Transform ankle, RatOutfit o, RatRig rig)
    {
        Material upper = rig.Mat(o.sneakerColor);
        Material white = rig.Mat(Rubber);
        Shapes.Create(PrimitiveType.Cube, "Sole", ankle, new Vector3(0f, -0.04f, 0.06f), new Vector3(0.15f, 0.04f, 0.3f), white);
        Shapes.Create(PrimitiveType.Sphere, "Upper", ankle, new Vector3(0f, 0f, 0.05f), new Vector3(0.145f, 0.11f, 0.27f), upper);
        Shapes.Create(PrimitiveType.Sphere, "ToeCap", ankle, new Vector3(0f, -0.015f, 0.16f), new Vector3(0.13f, 0.07f, 0.11f), white);
        Shapes.Create(PrimitiveType.Cylinder, "Collar", ankle, new Vector3(0f, 0.04f, -0.01f), new Vector3(0.15f, 0.025f, 0.15f), upper);
        Shapes.Create(PrimitiveType.Cube, "Tongue", ankle, new Vector3(0f, 0.055f, 0.06f), Quaternion.Euler(-30f, 0f, 0f), new Vector3(0.07f, 0.06f, 0.02f), white);
        for (int i = 0; i < 3; i++)
            Shapes.Create(PrimitiveType.Cube, "Lace", ankle, new Vector3(0f, 0.045f - i * 0.012f, 0.09f + i * 0.03f), new Vector3(0.08f, 0.01f, 0.012f), white);
        Shapes.Create(PrimitiveType.Cube, "Stripe", ankle, new Vector3(0.073f, 0f, 0.05f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.005f, 0.03f, 0.14f), white);
        Shapes.Create(PrimitiveType.Cube, "Stripe", ankle, new Vector3(-0.073f, 0f, 0.05f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.005f, 0.03f, 0.14f), white);
    }

    static void RatFoot(Transform ankle, Material skin, RatRig rig)
    {
        Material claw = rig.Mat(Claw);
        Shapes.Create(PrimitiveType.Sphere, "Heel", ankle, new Vector3(0f, -0.025f, 0f), new Vector3(0.11f, 0.07f, 0.13f), skin);
        Shapes.Create(PrimitiveType.Sphere, "Sole", ankle, new Vector3(0f, -0.03f, 0.09f), new Vector3(0.13f, 0.06f, 0.2f), skin);
        for (int i = 0; i < 4; i++)
        {
            float x = -0.045f + i * 0.03f;
            Shapes.Create(PrimitiveType.Capsule, "Toe", ankle, new Vector3(x, -0.035f, 0.2f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.028f, 0.035f, 0.028f), skin);
            Shapes.Create(PrimitiveType.Cube, "Claw", ankle, new Vector3(x, -0.045f, 0.245f), Quaternion.Euler(30f, 0f, 0f), new Vector3(0.012f, 0.012f, 0.03f), claw);
        }
    }

    // ---------------------------------------------------------------- Body

    static void Pelvis(Transform hips, RatOutfit o, RatRig rig)
    {
        Material pants = rig.Mat(o.pants);
        Material pantsDark = rig.Mat(o.pants * 0.75f + Color.black * 0.25f);
        Shapes.Create(PrimitiveType.Capsule, "Pelvis", hips, new Vector3(0f, 0.05f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.28f, 0.23f, 0.3f), pants);
        Shapes.Create(PrimitiveType.Cylinder, "Belt", hips, new Vector3(0f, 0.15f, 0f), new Vector3(0.43f, 0.03f, 0.32f), rig.Mat(Leather));
        Shapes.Create(PrimitiveType.Cube, "Buckle", hips, new Vector3(0f, 0.15f, 0.16f), new Vector3(0.08f, 0.055f, 0.02f), rig.Mat(Gold));
        for (int s = -1; s <= 1; s += 2)
        {
            Shapes.Create(PrimitiveType.Cube, "BackPocket", hips, new Vector3(0.1f * s, 0.04f, -0.15f), new Vector3(0.12f, 0.12f, 0.012f), pantsDark);
            Shapes.Create(PrimitiveType.Cube, "BeltLoop", hips, new Vector3(0.16f * s, 0.15f, 0.1f), new Vector3(0.02f, 0.05f, 0.02f), pantsDark);
        }
    }

    static void Tail(Transform hips, RatOutfit o, RatRig rig)
    {
        // Ringed rat tail: alternating shades, tapering to a thin tip
        Material skin = rig.Mat(o.skin);
        Material ring = rig.Mat(o.skin * 0.85f + Color.black * 0.15f);
        const int segments = 12;
        const float length = 0.1f;
        rig.tail = new Transform[segments];

        Transform previous = hips;
        Vector3 position = new Vector3(0f, 0.02f, -0.16f);
        for (int i = 0; i < segments; i++)
        {
            Transform segment = Shapes.Pivot("Tail" + i, previous, position);
            segment.localRotation = Quaternion.Euler(i == 0 ? -45f : 7f, 0f, 0f); // droop down, then curl up
            float radius = Mathf.Lerp(0.07f, 0.018f, i / (float)(segments - 1));
            Shapes.Create(PrimitiveType.Capsule, "TailPart", segment, new Vector3(0f, 0f, -length / 2f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(radius, length / 2f + radius * 0.5f, radius), i % 2 == 0 ? skin : ring);
            rig.tail[i] = segment;
            previous = segment;
            position = new Vector3(0f, 0f, -length);
        }
    }

    static void Torso(Transform chest, RatOutfit o, RatRig rig)
    {
        Material fur = rig.Mat(o.fur);
        Material furLight = rig.Mat(o.furLight);
        Material shirt = rig.Mat(o.shirt);

        // Pear-shaped rat body: round belly, narrower shoulders
        Shapes.Create(PrimitiveType.Sphere, "Belly", chest, new Vector3(0f, 0.05f, 0.02f), new Vector3(0.46f, 0.42f, 0.38f), shirt);
        Shapes.Create(PrimitiveType.Capsule, "Ribs", chest, new Vector3(0f, 0.22f, 0f), new Vector3(0.42f, 0.2f, 0.32f), shirt);

        if (o.tankTop)
        {
            // Furry shoulders and chest showing above a white tank top
            Shapes.Create(PrimitiveType.Capsule, "Shoulders", chest, new Vector3(0f, 0.33f, -0.01f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.22f, 0.25f, 0.28f), fur);
            Shapes.Create(PrimitiveType.Sphere, "ChestFur", chest, new Vector3(0f, 0.3f, 0.1f), new Vector3(0.24f, 0.16f, 0.14f), furLight);
            for (int s = -1; s <= 1; s += 2)
                Shapes.Create(PrimitiveType.Cube, "Strap", chest, new Vector3(0.11f * s, 0.35f, 0f), new Vector3(0.07f, 0.2f, 0.33f), shirt);
            Shapes.Create(PrimitiveType.Cube, "Hem", chest, new Vector3(0f, -0.13f, 0.02f), new Vector3(0.42f, 0.03f, 0.34f), rig.Mat(o.shirt * 0.9f + Color.black * 0.1f));
        }

        if (o.hood)
        {
            Material shirtDark = rig.Mat(o.shirt * 0.8f + Color.black * 0.2f);
            Shapes.Create(PrimitiveType.Capsule, "Shoulders", chest, new Vector3(0f, 0.33f, -0.01f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.24f, 0.27f, 0.3f), shirt);
            Shapes.Create(PrimitiveType.Cube, "Pocket", chest, new Vector3(0f, -0.02f, 0.2f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(0.26f, 0.13f, 0.02f), shirtDark);
            Shapes.Create(PrimitiveType.Cylinder, "Waistband", chest, new Vector3(0f, -0.15f, 0.01f), new Vector3(0.44f, 0.03f, 0.36f), shirtDark);
            for (int s = -1; s <= 1; s += 2)
            {
                Shapes.Create(PrimitiveType.Cylinder, "Drawstring", chest, new Vector3(0.05f * s, 0.26f, 0.175f), new Vector3(0.012f, 0.07f, 0.012f), rig.Mat(Rubber));
                Shapes.Create(PrimitiveType.Sphere, "Aglet", chest, new Vector3(0.05f * s, 0.19f, 0.178f), new Vector3(0.018f, 0.025f, 0.018f), rig.Mat(Rubber));
            }
            // Hood bunched up behind the neck
            Shapes.Create(PrimitiveType.Sphere, "HoodBack", chest, new Vector3(0f, 0.47f, -0.12f), new Vector3(0.36f, 0.26f, 0.2f), shirt);
        }

        Shapes.Create(PrimitiveType.Cylinder, "Neck", chest, new Vector3(0f, 0.43f, 0.01f), new Vector3(0.17f, 0.06f, 0.17f), fur);

        if (o.tie)
        {
            Material collar = rig.Mat(Rubber);
            Material tie = rig.Mat(o.tieColor);
            for (int s = -1; s <= 1; s += 2)
                Shapes.Create(PrimitiveType.Cube, "Collar", chest, new Vector3(0.05f * s, 0.41f, 0.12f), Quaternion.Euler(-20f, 0f, 35f * s), new Vector3(0.09f, 0.045f, 0.02f), collar);
            Shapes.Create(PrimitiveType.Cube, "TieKnot", chest, new Vector3(0f, 0.385f, 0.15f), Quaternion.Euler(-15f, 0f, 0f), new Vector3(0.05f, 0.045f, 0.03f), tie);
            Shapes.Create(PrimitiveType.Cube, "Tie", chest, new Vector3(0f, 0.24f, 0.2f), Quaternion.Euler(-14f, 0f, 0f), new Vector3(0.065f, 0.26f, 0.015f), tie);
            Shapes.Create(PrimitiveType.Cube, "TieTip", chest, new Vector3(0f, 0.105f, 0.235f), Quaternion.Euler(-14f, 0f, 45f), new Vector3(0.046f, 0.046f, 0.015f), tie);
            for (int i = 0; i < 3; i++)
                Shapes.Create(PrimitiveType.Sphere, "Button", chest, new Vector3(0.05f, 0.3f - i * 0.1f, 0.185f + i * 0.012f), new Vector3(0.015f, 0.015f, 0.008f), collar);
        }

        if (o.goldChain)
        {
            Material gold = rig.Mat(Gold);
            const int links = 14;
            for (int i = 0; i < links; i++)
            {
                float angle = i * Mathf.PI * 2f / links;
                // Chain hangs lower at the front
                float drop = (Mathf.Cos(angle) + 1f) * 0.035f;
                var position = new Vector3(Mathf.Sin(angle) * 0.13f, 0.42f - drop, Mathf.Cos(angle) * 0.11f + 0.02f);
                Shapes.Create(PrimitiveType.Cube, "Link", chest, position, Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 45f), new Vector3(0.03f, 0.03f, 0.015f), gold);
            }
            Shapes.Create(PrimitiveType.Cylinder, "Pendant", chest, new Vector3(0f, 0.3f, 0.2f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.07f, 0.008f, 0.07f), gold);
        }
    }

    // ---------------------------------------------------------------- Head

    static void Head(Transform head, RatOutfit o, RatRig rig)
    {
        Material fur = rig.Mat(o.fur);
        Material furLight = rig.Mat(o.furLight);
        Material furDark = rig.Mat(o.fur * 0.6f + Color.black * 0.4f);
        Material skin = rig.Mat(o.skin);
        Material black = rig.Mat(Black);
        Material white = rig.Mat(EyeWhite);

        Shapes.Create(PrimitiveType.Sphere, "Skull", head, new Vector3(0f, 0.13f, 0.02f), new Vector3(0.36f, 0.34f, 0.38f), fur);
        Shapes.Create(PrimitiveType.Sphere, "Snout", head, new Vector3(0f, 0.08f, 0.24f), new Vector3(0.2f, 0.17f, 0.3f), fur);
        Shapes.Create(PrimitiveType.Sphere, "Muzzle", head, new Vector3(0f, 0.035f, 0.24f), new Vector3(0.17f, 0.1f, 0.25f), furLight);
        Shapes.Create(PrimitiveType.Sphere, "Chin", head, new Vector3(0f, 0.0f, 0.17f), new Vector3(0.14f, 0.09f, 0.14f), furLight);

        // Nose, nostrils, mouth and buck teeth
        Shapes.Create(PrimitiveType.Sphere, "Nose", head, new Vector3(0f, 0.1f, 0.39f), new Vector3(0.075f, 0.06f, 0.06f), skin);
        for (int s = -1; s <= 1; s += 2)
            Shapes.Create(PrimitiveType.Sphere, "Nostril", head, new Vector3(0.016f * s, 0.094f, 0.418f), new Vector3(0.018f, 0.012f, 0.01f), black);
        Shapes.Create(PrimitiveType.Cube, "Mouth", head, new Vector3(0f, 0.036f, 0.366f), new Vector3(0.09f, 0.008f, 0.01f), black);
        Shapes.Create(PrimitiveType.Cube, "ToothLeft", head, new Vector3(-0.012f, 0.012f, 0.357f), new Vector3(0.022f, 0.045f, 0.014f), rig.Mat(Teeth));
        Shapes.Create(PrimitiveType.Cube, "ToothRight", head, new Vector3(0.012f, 0.012f, 0.357f), new Vector3(0.022f, 0.045f, 0.014f), rig.Mat(o.goldTooth ? Gold : Teeth));

        for (int s = -1; s <= 1; s += 2)
        {
            // Puffy cheeks
            Shapes.Create(PrimitiveType.Sphere, "Cheek", head, new Vector3(0.1f * s, 0.06f, 0.13f), new Vector3(0.16f, 0.13f, 0.16f), fur);

            // Eyes: white, pupil, shine, heavy lid, and an angry brow
            Shapes.Create(PrimitiveType.Sphere, "EyeWhite", head, new Vector3(0.09f * s, 0.19f, 0.165f), new Vector3(0.07f, 0.075f, 0.05f), white);
            Shapes.Create(PrimitiveType.Sphere, "Pupil", head, new Vector3(0.092f * s, 0.188f, 0.188f), new Vector3(0.045f, 0.055f, 0.02f), black);
            Shapes.Create(PrimitiveType.Sphere, "Shine", head, new Vector3(0.082f * s, 0.2f, 0.197f), new Vector3(0.014f, 0.014f, 0.008f), white);
            Shapes.Create(PrimitiveType.Sphere, "Lid", head, new Vector3(0.09f * s, 0.212f, 0.163f), new Vector3(0.08f, 0.045f, 0.056f), fur);
            Shapes.Create(PrimitiveType.Cube, "Brow", head, new Vector3(0.09f * s, 0.243f, 0.18f), Quaternion.Euler(0f, 0f, 15f * s), new Vector3(0.09f, 0.02f, 0.03f), furDark);

            // Big round ears, tilted out
            Shapes.Create(PrimitiveType.Sphere, "Ear", head, new Vector3(0.155f * s, 0.34f, -0.01f), Quaternion.Euler(0f, -10f * s, -20f * s), new Vector3(0.22f, 0.22f, 0.05f), fur);
            Shapes.Create(PrimitiveType.Sphere, "EarInner", head, new Vector3(0.155f * s, 0.34f, 0.012f), Quaternion.Euler(0f, -10f * s, -20f * s), new Vector3(0.15f, 0.15f, 0.03f), skin);

            // Three whiskers per side
            for (int w = -1; w <= 1; w++)
            {
                Shapes.Create(PrimitiveType.Cube, "Whisker", head, new Vector3(0.14f * s, 0.075f + 0.018f * w, 0.33f),
                    Quaternion.Euler(0f, -12f * s, 10f * w * s), new Vector3(0.24f, 0.005f, 0.005f), black);
            }
        }

        if (o.glasses)
        {
            Material frame = rig.Mat(Black);
            for (int s = -1; s <= 1; s += 2)
            {
                var eye = new Vector3(0.09f * s, 0.19f, 0.207f);
                Shapes.Create(PrimitiveType.Cube, "FrameTop", head, eye + new Vector3(0f, 0.042f, 0f), new Vector3(0.11f, 0.012f, 0.012f), frame);
                Shapes.Create(PrimitiveType.Cube, "FrameBottom", head, eye + new Vector3(0f, -0.04f, 0f), new Vector3(0.11f, 0.01f, 0.01f), frame);
                Shapes.Create(PrimitiveType.Cube, "FrameSide", head, eye + new Vector3(-0.055f, 0f, 0f), new Vector3(0.01f, 0.09f, 0.01f), frame);
                Shapes.Create(PrimitiveType.Cube, "FrameSide", head, eye + new Vector3(0.055f, 0f, 0f), new Vector3(0.01f, 0.09f, 0.01f), frame);
                Shapes.Create(PrimitiveType.Cube, "Temple", head, new Vector3(0.165f * s, 0.205f, 0.1f), Quaternion.Euler(0f, -12f * s, 0f), new Vector3(0.01f, 0.01f, 0.2f), frame);
            }
            Shapes.Create(PrimitiveType.Cube, "Bridge", head, new Vector3(0f, 0.2f, 0.214f), new Vector3(0.04f, 0.01f, 0.01f), frame);
        }

        if (o.earring)
            Shapes.Create(PrimitiveType.Cylinder, "Earring", head, new Vector3(-0.23f, 0.27f, 0.0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.05f, 0.006f, 0.05f), rig.Mat(Gold));

        if (o.scar)
            Shapes.Create(PrimitiveType.Cube, "Scar", head, new Vector3(0.095f, 0.2f, 0.195f), Quaternion.Euler(0f, 0f, -60f), new Vector3(0.14f, 0.012f, 0.012f), rig.Mat(Scar));

        if (o.cap)
        {
            Material cap = rig.Mat(o.capColor);
            Material capDark = rig.Mat(o.capColor * 0.7f + Color.black * 0.3f);
            Shapes.Create(PrimitiveType.Sphere, "Cap", head, new Vector3(0f, 0.25f, 0.01f), new Vector3(0.37f, 0.2f, 0.39f), cap);
            Shapes.Create(PrimitiveType.Sphere, "CapButton", head, new Vector3(0f, 0.35f, 0.01f), new Vector3(0.04f, 0.02f, 0.04f), capDark);
            Shapes.Create(PrimitiveType.Cube, "CapBrim", head, new Vector3(0f, 0.2f, -0.21f), Quaternion.Euler(-8f, 0f, 0f), new Vector3(0.27f, 0.025f, 0.17f), capDark); // worn backwards
            Shapes.Create(PrimitiveType.Cube, "CapStrap", head, new Vector3(0f, 0.19f, 0.2f), new Vector3(0.09f, 0.03f, 0.01f), capDark);
        }
        else
        {
            // Scruffy tuft of fur on top
            for (int i = -1; i <= 1; i++)
                Shapes.Create(PrimitiveType.Cube, "Tuft", head, new Vector3(0.03f * i, 0.3f, 0.08f), Quaternion.Euler(-30f, 0f, 20f * i), new Vector3(0.03f, 0.08f, 0.03f), fur);
        }
    }

    // ---------------------------------------------------------------- Arms

    static RatRig.Limb Arm(Transform chest, float side, RatOutfit o, RatRig rig)
    {
        Material sleeve = rig.Mat(o.hood || o.sleeves ? o.shirt : o.fur);
        Material skin = rig.Mat(o.skin);
        var limb = new RatRig.Limb();

        limb.upper = Shapes.Pivot(side < 0 ? "ShoulderLeft" : "ShoulderRight", chest, new Vector3(0.26f * side, 0.33f, 0f));
        limb.upper.localRotation = Quaternion.Euler(0f, 0f, 8f * side);
        Shapes.Create(PrimitiveType.Sphere, "Shoulder", limb.upper, Vector3.zero, new Vector3(0.15f, 0.15f, 0.15f), sleeve);
        Shapes.Create(PrimitiveType.Capsule, "UpperArm", limb.upper, new Vector3(0f, -0.12f, 0f), new Vector3(0.11f, 0.13f, 0.11f), sleeve);

        limb.middle = Shapes.Pivot("Elbow", limb.upper, new Vector3(0f, -0.24f, 0f));
        Shapes.Create(PrimitiveType.Sphere, "ElbowJoint", limb.middle, Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f), sleeve);
        Shapes.Create(PrimitiveType.Capsule, "Forearm", limb.middle, new Vector3(0f, -0.1f, 0f), new Vector3(0.095f, 0.11f, 0.095f), sleeve);
        if (o.hood || o.sleeves)
            Shapes.Create(PrimitiveType.Cylinder, "Cuff", limb.middle, new Vector3(0f, -0.19f, 0f), new Vector3(0.105f, 0.02f, 0.105f), rig.Mat(o.shirt * 0.8f + Color.black * 0.2f));

        limb.end = Shapes.Pivot("Wrist", limb.middle, new Vector3(0f, -0.2f, 0f));
        Hand(limb.end, side, o, skin, rig);
        return limb;
    }

    static void Hand(Transform wrist, float side, RatOutfit o, Material skin, RatRig rig)
    {
        Material claw = rig.Mat(Claw);
        if (o.handWraps)
            Shapes.Create(PrimitiveType.Cylinder, "Wrap", wrist, new Vector3(0f, -0.01f, 0f), new Vector3(0.09f, 0.03f, 0.09f), rig.Mat(Tape));

        // Clenched fist: palm, four knuckles with little claws, and a thumb
        Shapes.Create(PrimitiveType.Sphere, "Palm", wrist, new Vector3(0f, -0.05f, 0.01f), new Vector3(0.1f, 0.09f, 0.1f), skin);
        for (int i = 0; i < 4; i++)
        {
            float x = (-0.033f + i * 0.022f) * side;
            Shapes.Create(PrimitiveType.Sphere, "Knuckle", wrist, new Vector3(x, -0.09f, 0.035f), new Vector3(0.028f, 0.03f, 0.03f), skin);
            Shapes.Create(PrimitiveType.Cube, "Claw", wrist, new Vector3(x, -0.075f, 0.055f), Quaternion.Euler(-40f, 0f, 0f), new Vector3(0.01f, 0.01f, 0.02f), claw);
        }
        Shapes.Create(PrimitiveType.Capsule, "Thumb", wrist, new Vector3(-0.045f * side, -0.06f, 0.045f), Quaternion.Euler(60f, 0f, 30f * side), new Vector3(0.03f, 0.03f, 0.03f), skin);
    }

    // ---------------------------------------------------------------- Weapons

    static void Crowbar(Transform grip, RatRig rig)
    {
        Material red = rig.Mat(CrowbarRed);
        Material steel = rig.Mat(Steel);
        Shapes.Create(PrimitiveType.Cylinder, "Shaft", grip, new Vector3(0f, 0.28f, 0f), new Vector3(0.04f, 0.4f, 0.04f), red);
        Shapes.Create(PrimitiveType.Cylinder, "Grip", grip, new Vector3(0f, 0f, 0f), new Vector3(0.048f, 0.07f, 0.048f), rig.Mat(Black));
        // Curved hook at the top, ending in a split claw
        Vector3 point = new Vector3(0f, 0.68f, 0f);
        float angle = 0f;
        for (int i = 0; i < 4; i++)
        {
            angle -= 40f;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            Vector3 step = rotation * Vector3.up * 0.06f;
            Shapes.Create(PrimitiveType.Cube, "Hook", grip, point + step * 0.5f, rotation, new Vector3(0.04f, 0.065f, 0.04f), red);
            point += step;
        }
        Shapes.Create(PrimitiveType.Cube, "Claw", grip, point + new Vector3(0f, -0.025f, 0.01f), Quaternion.Euler(0f, 0f, angle), new Vector3(0.04f, 0.05f, 0.012f), steel);
        Shapes.Create(PrimitiveType.Cube, "Claw", grip, point + new Vector3(0f, -0.025f, -0.01f), Quaternion.Euler(0f, 0f, angle), new Vector3(0.04f, 0.05f, 0.012f), steel);
        // Flat chisel end at the bottom
        Shapes.Create(PrimitiveType.Cube, "Chisel", grip, new Vector3(0f, -0.14f, 0.015f), Quaternion.Euler(20f, 0f, 0f), new Vector3(0.05f, 0.09f, 0.018f), steel);
    }

    static void Pipe(Transform grip, RatRig rig)
    {
        Material pipe = rig.Mat(PipeGrey);
        Material rust = rig.Mat(Rust);
        Shapes.Create(PrimitiveType.Cylinder, "Pipe", grip, new Vector3(0f, 0.3f, 0f), new Vector3(0.06f, 0.45f, 0.06f), pipe);
        Shapes.Create(PrimitiveType.Cylinder, "Fitting", grip, new Vector3(0f, -0.13f, 0f), new Vector3(0.085f, 0.04f, 0.085f), pipe);
        Shapes.Create(PrimitiveType.Cylinder, "Elbow", grip, new Vector3(0f, 0.73f, 0f), new Vector3(0.09f, 0.05f, 0.09f), pipe);
        Shapes.Create(PrimitiveType.Cylinder, "RustBand", grip, new Vector3(0f, 0.45f, 0f), new Vector3(0.063f, 0.05f, 0.063f), rust);
        Shapes.Create(PrimitiveType.Cylinder, "RustBand", grip, new Vector3(0f, 0.6f, 0f), new Vector3(0.063f, 0.02f, 0.063f), rust);
        Shapes.Create(PrimitiveType.Cylinder, "Tape", grip, new Vector3(0f, 0f, 0f), new Vector3(0.068f, 0.08f, 0.068f), rig.Mat(Black));
    }
}
