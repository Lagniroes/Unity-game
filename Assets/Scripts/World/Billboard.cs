using UnityEngine;

/// <summary>Anything that can be shot: the player, monsters and barrels.</summary>
public interface IDamageable
{
    void TakeDamage(float amount, Vector3 from);
    Vector3 Center { get; }
    bool IsDead { get; }
}

/// <summary>
/// A Doom-style sprite: always turned to face the camera, and shaded by the lights around it
/// (unless it is "full bright", like fireballs and explosions).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Billboard : MonoBehaviour
{
    public bool fullBright;
    public float lightHeight = 0.8f;

    SpriteRenderer spriteRenderer;

    public SpriteRenderer Renderer => spriteRenderer ? spriteRenderer : spriteRenderer = GetComponent<SpriteRenderer>();

    public Sprite Sprite
    {
        get => Renderer.sprite;
        set => Renderer.sprite = value;
    }

    /// <summary>Creates a child object with a billboarded sprite.</summary>
    public static Billboard Create(string name, Transform parent, Vector3 position, Sprite sprite, bool fullBright = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sr.receiveShadows = false;
        var bb = go.AddComponent<Billboard>();
        bb.fullBright = fullBright;
        return bb;
    }

    void LateUpdate()
    {
        Transform cam = GameManager.ViewTransform;
        if (!cam) return;
        transform.rotation = Quaternion.Euler(0f, cam.eulerAngles.y, 0f);
        Color c = fullBright ? Color.white : Lighting.Sample(transform.position + Vector3.up * lightHeight, cam.position);
        c.a = Renderer.color.a;
        Renderer.color = c;
    }
}

/// <summary>A sprite animation that plays once and removes itself (explosions, bullet puffs, blood).</summary>
public class SpriteEffect : MonoBehaviour
{
    Sprite[] frames;
    float frameTime;
    float age;
    Light glow;
    float glowIntensity;
    Billboard billboard;
    Vector3 drift;

    public static SpriteEffect Spawn(Sprite[] frames, Vector3 position, float frameTime, bool fullBright,
        Color? lightColor = null, float lightRange = 0f, float lightIntensity = 0f, float scale = 1f)
    {
        Billboard bb = Billboard.Create("Effect", GameManager.WorldRoot, position, frames[0], fullBright);
        bb.transform.localScale = Vector3.one * scale;
        var fx = bb.gameObject.AddComponent<SpriteEffect>();
        fx.frames = frames;
        fx.frameTime = frameTime;
        fx.billboard = bb;
        bb.lightHeight = 0f;
        if (lightColor.HasValue && lightRange > 0f)
        {
            fx.glow = Lighting.Create("Glow", bb.transform, position, lightColor.Value, lightRange, lightIntensity);
            fx.glowIntensity = lightIntensity;
        }
        return fx;
    }

    public SpriteEffect WithDrift(Vector3 velocity)
    {
        drift = velocity;
        return this;
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.position += drift * Time.deltaTime;
        int frame = Mathf.FloorToInt(age / frameTime);
        if (frame >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }
        billboard.Sprite = frames[frame];
        if (glow) glow.intensity = glowIntensity * (1f - age / (frameTime * frames.Length));
    }
}
