using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// References to a rat's joints (filled in by RatModel) and its own materials,
/// so the whole rat can flash white when hit.
/// </summary>
public class RatRig : MonoBehaviour
{
    [System.Serializable]
    public class Limb
    {
        public Transform upper;   // hip or shoulder
        public Transform middle;  // knee or elbow
        public Transform end;     // ankle or wrist
    }

    public Transform roll;        // pivot at the belly, spun for forward rolls
    public Transform hips;        // pelvis; everything above the legs
    public Transform chest;
    public Transform head;
    public Limb leftLeg = new Limb();
    public Limb rightLeg = new Limb();
    public Limb leftArm = new Limb();
    public Limb rightArm = new Limb();
    public Transform weapon;
    public Transform[] tail;
    public float rollPivotHeight;

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
