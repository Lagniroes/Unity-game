using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// References to a rat's body parts (filled in by RatModel) and its own materials,
/// so the whole rat can flash white when hit.
/// </summary>
public class RatRig : MonoBehaviour
{
    public Transform upper;
    public Transform head;
    public Transform leftArm;
    public Transform rightArm;
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform weapon;
    public Transform[] tail;

    readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
    float flashUntil;
    bool flashing;

    /// <summary>This rat's material for a color (one per color, not shared with other rats).</summary>
    public Material Mat(Color color)
    {
        if (!materials.TryGetValue(color, out Material material))
        {
            material = Shapes.Unique(color);
            materials[color] = material;
        }
        return material;
    }

    public void Flash(float duration) => flashUntil = Time.time + duration;

    void LateUpdate()
    {
        bool shouldFlash = Time.time < flashUntil;
        if (shouldFlash == flashing) return;
        flashing = shouldFlash;
        foreach (KeyValuePair<Color, Material> entry in materials)
            entry.Value.color = flashing ? Color.white : entry.Key;
    }

    void OnDestroy()
    {
        foreach (Material material in materials.Values) Destroy(material);
    }
}
