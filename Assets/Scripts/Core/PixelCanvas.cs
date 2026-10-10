using UnityEngine;

/// <summary>
/// A tiny software rasterizer for making pixel art in code: shaded circles, ellipses and
/// capsules, triangles, rectangles, soft glows and an outline pass. y goes up and (0,0) is the
/// bottom-left pixel. Shapes go through an optional transform (origin, rotation, scale), which is
/// how monsters "fall over" in their death frames.
/// </summary>
public class PixelCanvas
{
    public readonly int Width, Height;
    readonly Color[] pixels;

    Vector2 origin;
    float cos = 1f, sin, scale = 1f;

    static readonly Vector2 LightDir = new Vector2(-0.45f, 0.9f).normalized;

    public PixelCanvas(int width, int height)
    {
        Width = width;
        Height = height;
        pixels = new Color[width * height];
    }

    /// <summary>Following shapes are rotated by angle (degrees, counter-clockwise) and scaled around newOrigin.</summary>
    public void SetTransform(Vector2 newOrigin, float angleDegrees = 0f, float newScale = 1f)
    {
        origin = newOrigin;
        float a = angleDegrees * Mathf.Deg2Rad;
        cos = Mathf.Cos(a);
        sin = Mathf.Sin(a);
        scale = newScale;
    }

    public void ResetTransform() => SetTransform(Vector2.zero);

    public Vector2 Transform(Vector2 p) =>
        origin + new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos) * scale;

    public Color Get(int x, int y) =>
        x >= 0 && y >= 0 && x < Width && y < Height ? pixels[y * Width + x] : Color.clear;

    /// <summary>Sets a pixel, alpha-blending over what is already there.</summary>
    public void Set(int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height || c.a <= 0f) return;
        int i = y * Width + x;
        if (c.a >= 1f)
        {
            pixels[i] = c;
            return;
        }
        Color d = pixels[i];
        float a = c.a + d.a * (1f - c.a);
        if (a <= 0f) return;
        pixels[i] = new Color(
            (c.r * c.a + d.r * d.a * (1f - c.a)) / a,
            (c.g * c.a + d.g * d.a * (1f - c.a)) / a,
            (c.b * c.a + d.b * d.a * (1f - c.a)) / a,
            a);
    }

    /// <summary>Axis-aligned rectangle in raw pixel coordinates (ignores the transform).</summary>
    public void Rect(int x, int y, int w, int h, Color c)
    {
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                Set(xx, yy, c);
    }

    /// <summary>Rectangle in transformed space (corner a, size), drawn as a filled quad.</summary>
    public void Box(Vector2 min, Vector2 max, Color c)
    {
        Tri(min, new Vector2(min.x, max.y), max, c);
        Tri(min, max, new Vector2(max.x, min.y), c);
    }

    public void Circle(Vector2 center, float r, Color col, float shade = 0.35f) =>
        Ellipse(center, r, r, col, shade);

    /// <summary>A shaded ellipse (lit from the top-left). Radii are not rotated by the transform.</summary>
    public void Ellipse(Vector2 center, float rx, float ry, Color col, float shade = 0.35f)
    {
        Vector2 c = Transform(center);
        rx *= scale;
        ry *= scale;
        if (rx <= 0f || ry <= 0f) return;
        int x0 = Mathf.FloorToInt(c.x - rx), x1 = Mathf.CeilToInt(c.x + rx);
        int y0 = Mathf.FloorToInt(c.y - ry), y1 = Mathf.CeilToInt(c.y + ry);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var n = new Vector2((x + 0.5f - c.x) / rx, (y + 0.5f - c.y) / ry);
                if (n.sqrMagnitude > 1f) continue;
                Set(x, y, Shaded(col, n, shade));
            }
    }

    public void Capsule(Vector2 a, Vector2 b, float r, Color col, float shade = 0.35f) =>
        Capsule(a, b, r, r, col, shade);

    /// <summary>A shaded, possibly tapered capsule from a (radius ra) to b (radius rb). Great for limbs.</summary>
    public void Capsule(Vector2 a, Vector2 b, float ra, float rb, Color col, float shade = 0.35f)
    {
        Vector2 A = Transform(a), B = Transform(b);
        ra *= scale;
        rb *= scale;
        float rMax = Mathf.Max(ra, rb);
        if (rMax <= 0f) return;
        int x0 = Mathf.FloorToInt(Mathf.Min(A.x, B.x) - rMax), x1 = Mathf.CeilToInt(Mathf.Max(A.x, B.x) + rMax);
        int y0 = Mathf.FloorToInt(Mathf.Min(A.y, B.y) - rMax), y1 = Mathf.CeilToInt(Mathf.Max(A.y, B.y) + rMax);
        Vector2 ab = B - A;
        float len2 = ab.sqrMagnitude;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float t = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - A, ab) / len2) : 0f;
                float r = Mathf.Lerp(ra, rb, t);
                if (r <= 0f) continue;
                Vector2 n = (p - (A + ab * t)) / r;
                if (n.sqrMagnitude > 1f) continue;
                Set(x, y, Shaded(col, n, shade));
            }
    }

    public void Line(Vector2 a, Vector2 b, Color col, float width = 1f) => Capsule(a, b, width * 0.5f, col, 0f);

    /// <summary>Flat-colored triangle.</summary>
    public void Tri(Vector2 a, Vector2 b, Vector2 c, Color col)
    {
        a = Transform(a);
        b = Transform(b);
        c = Transform(c);
        int x0 = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
        int x1 = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
        int y0 = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
        int y1 = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
        float area = Edge(a, b, c);
        if (Mathf.Approximately(area, 0f)) return;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float w0 = Edge(b, c, p) / area, w1 = Edge(c, a, p) / area, w2 = Edge(a, b, p) / area;
                if (w0 >= 0f && w1 >= 0f && w2 >= 0f) Set(x, y, col);
            }
    }

    /// <summary>A soft round glow that fades out towards its edge (for fire, lamps and flashes).</summary>
    public void Glow(Vector2 center, float r, Color col)
    {
        Vector2 c = Transform(center);
        r *= scale;
        int x0 = Mathf.FloorToInt(c.x - r), x1 = Mathf.CeilToInt(c.x + r);
        int y0 = Mathf.FloorToInt(c.y - r), y1 = Mathf.CeilToInt(c.y + r);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / r;
                if (d > 1f) continue;
                float k = (1f - d) * (1f - d);
                Set(x, y, new Color(col.r, col.g, col.b, col.a * Mathf.Clamp01(k * 1.6f)));
            }
    }

    /// <summary>Gives every shape a dark one-pixel border so sprites read well against any background.</summary>
    public void Outline(Color col)
    {
        var src = (Color[])pixels.Clone();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (src[y * Width + x].a >= 0.5f) continue;
                bool edge = (x > 0 && src[y * Width + x - 1].a >= 0.5f) ||
                            (x < Width - 1 && src[y * Width + x + 1].a >= 0.5f) ||
                            (y > 0 && src[(y - 1) * Width + x].a >= 0.5f) ||
                            (y < Height - 1 && src[(y + 1) * Width + x].a >= 0.5f);
                if (edge) pixels[y * Width + x] = col;
            }
    }

    /// <summary>Random brightness speckle on all opaque pixels, for a gritty hand-drawn look.</summary>
    public void Noise(float amount, int seed)
    {
        var rng = new System.Random(seed);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            if (c.a <= 0f) continue;
            float k = 1f + ((float)rng.NextDouble() - 0.5f) * amount;
            pixels[i] = new Color(c.r * k, c.g * k, c.b * k, c.a);
        }
    }

    public Texture2D ToTexture(bool repeat = false, bool mipmaps = false)
    {
        var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, mipmaps)
        {
            filterMode = FilterMode.Point,
            wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
        };
        tex.SetPixels(pixels);
        tex.Apply(mipmaps);
        return tex;
    }

    /// <summary>A world-space sprite with its pivot at the bottom center (where it stands on the floor).</summary>
    public Sprite ToSprite(float pixelsPerUnit = 32f) =>
        Sprite.Create(ToTexture(), new UnityEngine.Rect(0, 0, Width, Height), new Vector2(0.5f, 0f),
            pixelsPerUnit, 0, SpriteMeshType.FullRect);

    static float Edge(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);

    static Color Shaded(Color col, Vector2 n, float shade)
    {
        float l = 1f + shade * Vector2.Dot(n, LightDir) - shade * 0.4f * n.sqrMagnitude;
        return new Color(col.r * l, col.g * l, col.b * l, col.a);
    }
}
