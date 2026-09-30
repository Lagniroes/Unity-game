using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helpers for building everything out of Unity primitives, plus a material cache.
/// The base material is taken from a primitive, so colors work in Built-in, URP and HDRP.
/// </summary>
public static class Shapes
{
    static Material baseMaterial;
    static readonly Dictionary<Color, Material> sharedMaterials = new Dictionary<Color, Material>();

    public static Material BaseMaterial
    {
        get
        {
            if (!baseMaterial)
            {
                GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseMaterial = probe.GetComponent<Renderer>().sharedMaterial;
                Object.DestroyImmediate(probe);
            }
            return baseMaterial;
        }
    }

    /// <summary>A material shared by everything with this color (for static scenery).</summary>
    public static Material Shared(Color color)
    {
        if (!sharedMaterials.TryGetValue(color, out Material material) || !material)
        {
            material = new Material(BaseMaterial) { color = color, enableInstancing = true };
            sharedMaterials[color] = material;
        }
        return material;
    }

    /// <summary>A new material that can be changed on its own (e.g. to flash when hit).</summary>
    public static Material Unique(Color color) => new Material(BaseMaterial) { color = color };

    public static Transform Create(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
        Vector3 localScale, Material material, bool keepCollider = false)
    {
        return Create(type, name, parent, localPosition, Quaternion.identity, localScale, material, keepCollider);
    }

    public static Transform Create(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
        Quaternion localRotation, Vector3 localScale, Material material, bool keepCollider = false)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        // Removed immediately so decorative parts never block a CharacterController, even for one frame
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());

        Transform t = go.transform;
        if (parent) t.SetParent(parent, false);
        t.localPosition = localPosition;
        t.localRotation = localRotation;
        t.localScale = localScale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return t;
    }

    public static Transform Pivot(string name, Transform parent, Vector3 localPosition)
    {
        var pivot = new GameObject(name).transform;
        if (parent) pivot.SetParent(parent, false);
        pivot.localPosition = localPosition;
        return pivot;
    }
}
