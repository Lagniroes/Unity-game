using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Material helpers. The base material is taken from a primitive, so it works in the Built-in
/// pipeline as well as URP/HDRP, and every surface gets a matte, retro look.
/// </summary>
public static class Gfx
{
    static Material baseMaterial;
    static readonly Dictionary<string, Material> textured = new Dictionary<string, Material>();

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

    /// <summary>A shared material for a named texture from <see cref="TextureLibrary"/>.</summary>
    public static Material Surface(string textureName)
    {
        if (textured.TryGetValue(textureName, out Material m) && m) return m;
        m = Textured(TextureLibrary.Get(textureName), TextureLibrary.IsGlowing(textureName));
        m.name = textureName;
        textured[textureName] = m;
        return m;
    }

    /// <summary>A new matte material with this texture. Glowing ones ignore the lighting (light panels, slime).</summary>
    public static Material Textured(Texture2D texture, bool glowing = false)
    {
        var m = new Material(BaseMaterial) { mainTexture = texture, color = Color.white, enableInstancing = true };
        m.SetFloat("_Glossiness", 0f);
        m.SetFloat("_Smoothness", 0f);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_SpecularHighlights", 0f);
        m.SetFloat("_GlossyReflections", 0f);
        m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        m.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
        if (glowing) SetGlow(m, texture, 0.9f);
        return m;
    }

    public static void SetGlow(Material m, Texture texture, float strength)
    {
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        m.SetTexture("_EmissionMap", texture);
        m.SetColor("_EmissionColor", new Color(strength, strength, strength));
    }

    /// <summary>Clears all cached materials (textures are rebuilt only once).</summary>
    public static void ClearCache() => textured.Clear();
}

/// <summary>
/// Keeps track of the dynamic lights so sprites (which are unlit) can be shaded to match
/// the rooms they stand in, Doom-style: bright near lamps, dark in the shadows, fading with fog.
/// </summary>
public static class Lighting
{
    public static readonly List<Light> Lights = new List<Light>();
    public static Color Ambient = new Color(0.3f, 0.28f, 0.27f);
    public static float FogDensity = 0.035f;

    public static Light Create(string name, Transform parent, Vector3 position, Color color, float range, float intensity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        go.AddComponent<RegisteredLight>();
        return light;
    }

    /// <summary>How bright a sprite at this point should look from the camera.</summary>
    public static Color Sample(Vector3 point, Vector3 cameraPosition)
    {
        Color c = Ambient;
        for (int i = 0; i < Lights.Count; i++)
        {
            Light l = Lights[i];
            if (!l || !l.enabled || l.intensity <= 0f) continue;
            float d = Vector3.Distance(point, l.transform.position);
            if (d >= l.range) continue;
            float k = 1f - d / l.range;
            c += l.color * (l.intensity * 0.55f * k * k);
        }
        float fog = Mathf.Exp(-Mathf.Pow(FogDensity * Vector3.Distance(point, cameraPosition), 2f));
        c *= fog;
        return new Color(Mathf.Min(c.r, 1.3f), Mathf.Min(c.g, 1.3f), Mathf.Min(c.b, 1.3f), 1f);
    }
}

/// <summary>Adds its light to <see cref="Lighting.Lights"/> while enabled.</summary>
public class RegisteredLight : MonoBehaviour
{
    Light lightComponent;

    void OnEnable()
    {
        if (!lightComponent) lightComponent = GetComponent<Light>();
        if (lightComponent) Lighting.Lights.Add(lightComponent);
    }

    void OnDisable() => Lighting.Lights.Remove(lightComponent);
}

/// <summary>A broken, flickering light like in Doom's damaged tech bases.</summary>
public class FlickerLight : MonoBehaviour
{
    public Material glowMaterial; // the ceiling panel, dimmed along with the light
    Light lightComponent;
    float baseIntensity;
    float nextChange;

    void Start()
    {
        lightComponent = GetComponent<Light>();
        baseIntensity = lightComponent.intensity;
    }

    void Update()
    {
        if (Time.time < nextChange) return;
        bool on = Random.value > 0.35f;
        lightComponent.intensity = on ? baseIntensity : baseIntensity * 0.08f;
        if (glowMaterial) glowMaterial.SetColor("_EmissionColor", on ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f));
        nextChange = Time.time + (on ? Random.Range(0.05f, 0.6f) : Random.Range(0.04f, 0.2f));
    }
}
