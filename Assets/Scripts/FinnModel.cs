using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a low-poly Finn out of Unity primitives. Model units: feet at y = 0, about 2 m tall,
/// facing +Z. Arms and legs hang from pivot objects so FinnAnimator can swing them.
/// </summary>
public static class FinnModel
{
    public static readonly Color Skin = new Color(1f, 0.84f, 0.72f);
    public static readonly Color HatWhite = new Color(0.97f, 0.97f, 0.97f);
    public static readonly Color ShirtBlue = new Color(0.33f, 0.72f, 0.95f);
    public static readonly Color ShortsBlue = new Color(0.12f, 0.22f, 0.6f);
    public static readonly Color BackpackGreen = new Color(0.33f, 0.68f, 0.25f);
    public static readonly Color BackpackDark = new Color(0.22f, 0.5f, 0.17f);
    public static readonly Color Black = new Color(0.05f, 0.05f, 0.05f);
    public static readonly Color Mouth = new Color(0.35f, 0.08f, 0.08f);

    static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
    static Material baseMaterial;

    /// <summary>Creates Finn under <paramref name="parent"/> and returns the FinnAnimator driving him.</summary>
    public static FinnAnimator Build(Transform parent, Vector3 localOffset, Material material)
    {
        baseMaterial = material;

        var root = new GameObject("Finn").transform;
        root.SetParent(parent, false);
        root.localPosition = localOffset;

        // Body (everything above the hips bobs together while walking)
        Transform body = Pivot("Body", root, Vector3.zero);

        // Shorts and shirt
        Part("Shorts", PrimitiveType.Cube, body, new Vector3(0f, 0.72f, 0f), new Vector3(0.44f, 0.26f, 0.3f), ShortsBlue);
        Part("Shirt", PrimitiveType.Capsule, body, new Vector3(0f, 1.0f, 0f), new Vector3(0.46f, 0.25f, 0.32f), ShirtBlue);

        // Head: big round white bear hat with the face peeking out the front
        Part("Hat", PrimitiveType.Sphere, body, new Vector3(0f, 1.6f, 0f), new Vector3(0.7f, 0.66f, 0.62f), HatWhite);
        Part("EarLeft", PrimitiveType.Sphere, body, new Vector3(-0.2f, 1.9f, 0f), new Vector3(0.15f, 0.15f, 0.13f), HatWhite);
        Part("EarRight", PrimitiveType.Sphere, body, new Vector3(0.2f, 1.9f, 0f), new Vector3(0.15f, 0.15f, 0.13f), HatWhite);
        Part("Face", PrimitiveType.Sphere, body, new Vector3(0f, 1.55f, 0.27f), new Vector3(0.45f, 0.38f, 0.12f), Skin);
        Part("EyeLeft", PrimitiveType.Sphere, body, new Vector3(-0.09f, 1.59f, 0.325f), new Vector3(0.055f, 0.07f, 0.03f), Black);
        Part("EyeRight", PrimitiveType.Sphere, body, new Vector3(0.09f, 1.59f, 0.325f), new Vector3(0.055f, 0.07f, 0.03f), Black);
        Part("Mouth", PrimitiveType.Sphere, body, new Vector3(0f, 1.48f, 0.322f), new Vector3(0.13f, 0.045f, 0.025f), Mouth);

        // Green backpack with straps over the shoulders
        Part("Backpack", PrimitiveType.Cube, body, new Vector3(0f, 1.0f, -0.24f), new Vector3(0.38f, 0.42f, 0.18f), BackpackGreen);
        Part("BackpackFlap", PrimitiveType.Cube, body, new Vector3(0f, 1.15f, -0.25f), new Vector3(0.4f, 0.14f, 0.2f), BackpackDark);
        Part("StrapLeft", PrimitiveType.Cube, body, new Vector3(-0.13f, 1.03f, 0.155f), new Vector3(0.05f, 0.34f, 0.03f), BackpackDark);
        Part("StrapRight", PrimitiveType.Cube, body, new Vector3(0.13f, 1.03f, 0.155f), new Vector3(0.05f, 0.34f, 0.03f), BackpackDark);

        // Arms hang from the shoulders
        Transform leftArm = Arm("ArmLeft", body, -1f);
        Transform rightArm = Arm("ArmRight", body, 1f);

        // Legs hang from the hips (not parented to the body so the feet stay planted when it bobs)
        Transform leftLeg = Leg("LegLeft", root, -1f);
        Transform rightLeg = Leg("LegRight", root, 1f);

        var animator = root.gameObject.AddComponent<FinnAnimator>();
        animator.body = body;
        animator.leftArm = leftArm;
        animator.rightArm = rightArm;
        animator.leftLeg = leftLeg;
        animator.rightLeg = rightLeg;
        return animator;
    }

    static Transform Arm(string name, Transform parent, float side)
    {
        Transform pivot = Pivot(name, parent, new Vector3(0.27f * side, 1.15f, 0f));
        pivot.localRotation = Quaternion.Euler(0f, 0f, 8f * side); // arms slightly out from the body
        Part("Sleeve", PrimitiveType.Sphere, pivot, new Vector3(0f, -0.02f, 0f), new Vector3(0.17f, 0.17f, 0.17f), ShirtBlue);
        Part("Arm", PrimitiveType.Cylinder, pivot, new Vector3(0f, -0.2f, 0f), new Vector3(0.08f, 0.18f, 0.08f), Skin);
        Part("Hand", PrimitiveType.Sphere, pivot, new Vector3(0f, -0.4f, 0f), new Vector3(0.11f, 0.11f, 0.11f), Skin);
        return pivot;
    }

    static Transform Leg(string name, Transform parent, float side)
    {
        Transform pivot = Pivot(name, parent, new Vector3(0.11f * side, 0.62f, 0f));
        Part("Leg", PrimitiveType.Cylinder, pivot, new Vector3(0f, -0.07f, 0f), new Vector3(0.1f, 0.07f, 0.1f), Skin);
        Part("Sock", PrimitiveType.Cylinder, pivot, new Vector3(0f, -0.33f, 0f), new Vector3(0.11f, 0.2f, 0.11f), HatWhite);
        Part("Shoe", PrimitiveType.Sphere, pivot, new Vector3(0f, -0.56f, 0.04f), new Vector3(0.16f, 0.12f, 0.26f), Black);
        return pivot;
    }

    static Transform Pivot(string name, Transform parent, Vector3 localPosition)
    {
        var pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = localPosition;
        return pivot;
    }

    static void Part(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Object.Destroy(part.GetComponent<Collider>()); // the CharacterController handles collisions
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = GetMaterial(color);
    }

    static Material GetMaterial(Color color)
    {
        if (!materials.TryGetValue(color, out Material material) || material == null)
        {
            material = new Material(baseMaterial) { color = color };
            materials[color] = material;
        }
        return material;
    }
}
