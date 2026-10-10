using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural 64x64 wall, floor and ceiling textures (bricks, tech panels, doors, slime...),
/// drawn pixel by pixel at startup so the project needs no image files.
/// </summary>
public static class TextureLibrary
{
    static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    public static bool IsGlowing(string name) => name == "CeilLight" || name.StartsWith("Nukage") || name == "SwitchOn";

    public static Texture2D Get(string name)
    {
        if (cache.TryGetValue(name, out Texture2D t) && t) return t;
        PixelCanvas c;
        switch (name)
        {
            case "Brick": c = Brick(); break;
            case "Stone": c = Stone(); break;
            case "Tech": c = Tech(); break;
            case "Metal": c = Metal(); break;
            case "Door": c = Door(false); break;
            case "RedDoor": c = Door(true); break;
            case "DoorTrack": c = DoorTrack(); break;
            case "SwitchOff": c = Switch(false); break;
            case "SwitchOn": c = Switch(true); break;
            case "Tiles": c = Tiles(); break;
            case "Dirt": c = Dirt(); break;
            case "Grate": c = Grate(); break;
            case "Step": c = Step(); break;
            case "StepSide": c = StepSide(); break;
            case "Ceiling": c = Ceiling(); break;
            case "CeilLight": c = CeilLight(); break;
            case "Wood": c = Wood(); break;
            case "Nukage0": c = Nukage(0); break;
            case "Nukage1": c = Nukage(1); break;
            case "Nukage2": c = Nukage(2); break;
            case "StatusBar": c = StatusBar(); break;
            default:
                Debug.LogWarning("Unknown texture " + name);
                c = Stone();
                break;
        }
        t = c.ToTexture(repeat: true, mipmaps: name != "StatusBar");
        t.name = name;
        cache[name] = t;
        return t;
    }

    // ---------- noise ----------

    public static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 982451653);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xffffff) / 16777215f;
        }
    }

    static int Mod(int a, int m) => ((a % m) + m) % m;

    /// <summary>Smooth value noise that tiles over [0,1) in both directions.</summary>
    static float ValueNoise(float x, float y, int cells, int seed)
    {
        float fx = x * cells, fy = y * cells;
        int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
        float tx = fx - ix, ty = fy - iy;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float a = Hash(Mod(ix, cells), Mod(iy, cells), seed);
        float b = Hash(Mod(ix + 1, cells), Mod(iy, cells), seed);
        float c = Hash(Mod(ix, cells), Mod(iy + 1, cells), seed);
        float d = Hash(Mod(ix + 1, cells), Mod(iy + 1, cells), seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }

    static float Fbm(int x, int y, int seed, float offset = 0f)
    {
        float u = x / 64f + offset, v = y / 64f;
        return 0.5f * ValueNoise(u, v, 4, seed) + 0.3f * ValueNoise(u, v, 8, seed + 1) + 0.2f * ValueNoise(u, v, 16, seed + 2);
    }

    static Color Rgb(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

    static Color Grain(Color c, int x, int y, int seed, float fbm = 0.3f, float speck = 0.14f) =>
        Rgb(c, (1f - fbm * 0.5f + fbm * Fbm(x, y, seed)) * (1f - speck * 0.5f + speck * Hash(x, y, seed + 7)));

    // ---------- walls ----------

    static PixelCanvas Brick()
    {
        var c = new PixelCanvas(64, 64);
        var brick = new Color(0.5f, 0.24f, 0.15f);
        var mortar = new Color(0.2f, 0.17f, 0.15f);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                int row = y / 8, off = (row % 2) * 8;
                int bx = (x + off) / 16;
                Color col;
                if (y % 8 == 0 || (x + off) % 16 == 0) col = Grain(mortar, x, y, 9, 0.2f, 0.4f);
                else
                {
                    col = Grain(Rgb(brick, 0.75f + 0.4f * Hash(bx, row, 3)), x, y, 5);
                    if (y % 8 == 7) col = Rgb(col, 1.2f);
                    else if (y % 8 == 1) col = Rgb(col, 0.75f);
                }
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas Stone()
    {
        var c = new PixelCanvas(64, 64);
        var stone = new Color(0.44f, 0.42f, 0.38f);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                int row = y / 16, off = (row % 2) * 16;
                int bx = (x + off) / 32;
                int lx = (x + off) % 32, ly = y % 16;
                Color col = Grain(Rgb(stone, 0.8f + 0.3f * Hash(bx, row, 11)), x, y, 13, 0.45f, 0.2f);
                if (ly == 0 || lx == 0) col = Rgb(col, 0.4f);
                else if (ly == 15 || lx == 1) col = Rgb(col, 1.25f);
                else if (ly == 1 || lx == 31) col = Rgb(col, 0.7f);
                if (Fbm(x, y, 17) > 0.68f) col = Rgb(col, 0.6f); // cracks and stains
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas Tech()
    {
        var c = new PixelCanvas(64, 64);
        var panel = new Color(0.38f, 0.4f, 0.44f);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                int lx = x % 32, ly = y % 32;
                Color col = Grain(panel, x, y, 21, 0.25f, 0.12f);
                if (lx == 0 || ly == 31) col = Rgb(col, 1.35f);
                else if (lx == 31 || ly == 0) col = Rgb(col, 0.5f);
                bool rivet = (lx == 3 || lx == 28) && (ly == 3 || ly == 28);
                if (rivet) col = Rgb(panel, 1.6f);
                if (y >= 26 && y <= 37)
                {
                    col = Grain(new Color(0.12f, 0.13f, 0.15f), x, y, 23);
                    if (y == 26) col = Rgb(col, 0.6f);
                    if (y == 37) col = Rgb(panel, 1.2f);
                    if (y >= 30 && y <= 33 && x % 8 >= 2 && x % 8 <= 4)
                        col = (x / 8) % 3 == 0 ? new Color(1f, 0.25f, 0.15f) : new Color(0.25f, 1f, 0.35f);
                }
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas Metal()
    {
        var c = new PixelCanvas(64, 64);
        var metal = new Color(0.42f, 0.36f, 0.3f);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Color col = Grain(metal, x, y, 31, 0.3f, 0.15f);
                if (x % 16 == 0) col = Rgb(col, 0.45f);
                else if (x % 16 == 1) col = Rgb(col, 1.3f);
                if (x % 16 == 8 && y % 16 == 8) col = Rgb(metal, 1.6f);
                if (Fbm(x, y, 33) > 0.62f) col = Grain(new Color(0.45f, 0.24f, 0.1f), x, y, 35); // rust
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas Door(bool red)
    {
        var c = new PixelCanvas(64, 64);
        var steel = new Color(0.45f, 0.46f, 0.5f);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Color col = Grain(steel, x, y, 41, 0.2f, 0.1f);
                int ly = y % 16;
                if (ly == 0) col = Rgb(col, 0.45f);
                else if (ly == 1) col = Rgb(col, 1.3f);
                if (x < 2 || x > 61) col = Rgb(col, 0.6f);
                if (y < 6 || y > 57) // hazard stripes at the top and bottom
                    col = ((x + y) / 4) % 2 == 0 ? new Color(0.85f, 0.7f, 0.1f) : new Color(0.08f, 0.08f, 0.08f);
                if (x >= 20 && x <= 43 && y >= 22 && y <= 41)
                {
                    col = Grain(new Color(0.2f, 0.2f, 0.23f), x, y, 43);
                    if (x == 20 || y == 41) col = Rgb(col, 0.6f);
                    if (x == 43 || y == 22) col = Rgb(steel, 1.3f);
                }
                if (red && y >= 28 && y <= 35 && (x < 20 || x > 43)) col = Grain(new Color(0.75f, 0.08f, 0.06f), x, y, 45);
                c.Set(x, y, col);
            }
        if (red)
        {
            c.Circle(new Vector2(32, 32), 7f, new Color(0.85f, 0.1f, 0.08f), 0.4f);
            c.Rect(29, 32, 2, 2, Color.black);
            c.Rect(34, 32, 2, 2, Color.black);
            c.Rect(30, 27, 5, 1, Color.black);
        }
        else
        {
            for (int i = 0; i < 4; i++) c.Rect(24 + i * 5, 30, 3, 4, new Color(0.2f, 0.9f, 0.3f));
        }
        return c;
    }

    static PixelCanvas DoorTrack()
    {
        var c = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Color col = Grain(new Color(0.27f, 0.27f, 0.29f), x, y, 51);
                if (x % 8 == 0) col = Rgb(col, 0.5f);
                else if (x % 8 == 1) col = Rgb(col, 1.3f);
                c.Set(x, y, col);
            }
        return c;
    }

    static readonly string[] Font =
    {
        // 3x5 letters, top row first: E X I T
        "111100110100111", "101101010101101", "111010010010111", "111010010010010",
    };

    static PixelCanvas Switch(bool on)
    {
        PixelCanvas c = Tech();
        var frame = new Color(0.15f, 0.15f, 0.17f);
        c.Rect(20, 12, 24, 36, frame);
        c.Rect(22, 14, 20, 32, new Color(0.3f, 0.3f, 0.33f));
        c.Rect(29, on ? 30 : 16, 6, 14, new Color(0.6f, 0.6f, 0.62f)); // lever
        c.Rect(28, on ? 42 : 16, 8, 3, new Color(0.15f, 0.15f, 0.15f));
        c.Circle(new Vector2(32, 52), 3.5f, on ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.15f, 0.1f), 0.2f);
        // "EXIT" in red letters
        for (int letter = 0; letter < 4; letter++)
            for (int i = 0; i < 15; i++)
                if (Font[letter][i] == '1') c.Rect(24 + letter * 4 + i % 3, 61 - i / 3, 1, 1, new Color(1f, 0.1f, 0.05f));
        return c;
    }

    // ---------- floors and ceilings ----------

    static PixelCanvas Tiles()
    {
        var c = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Color tile = Rgb(new Color(0.4f, 0.37f, 0.33f), 0.8f + 0.35f * Hash(x / 16, y / 16, 61));
                Color col = Grain(tile, x, y, 63, 0.3f, 0.15f);
                if (x % 16 == 0 || y % 16 == 0) col = Rgb(col, 0.45f);
                else if (x % 16 == 1 || y % 16 == 15) col = Rgb(col, 1.2f);
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas Dirt()
    {
        var c = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Color col = Grain(new Color(0.36f, 0.27f, 0.19f), x, y, 71, 0.8f, 0.3f);
                if (Hash(x, y, 73) > 0.96f) col = Rgb(new Color(0.55f, 0.5f, 0.44f), 0.8f + 0.4f * Hash(x, y, 74));
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas Grate()
    {
        var c = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                int lx = x % 8, ly = y % 8;
                Color col = Grain(new Color(0.3f, 0.31f, 0.33f), x, y, 81);
                if (lx >= 2 && lx <= 6 && ly >= 2 && ly <= 6) col = new Color(0.05f, 0.05f, 0.06f);
                else if (lx == 1 || ly == 7) col = Rgb(col, 1.3f);
                if (x % 32 == 0 || y % 32 == 0) col = Rgb(new Color(0.22f, 0.22f, 0.24f), 1f);
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas Step()
    {
        var c = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Color col = Grain(new Color(0.42f, 0.42f, 0.45f), x, y, 91, 0.2f, 0.1f);
                if ((x + y) % 8 == 0 || (x - y + 64) % 8 == 4) col = Rgb(col, 1.3f); // diamond plate
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas StepSide()
    {
        var c = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Color col = ((x + y) / 6) % 2 == 0 ? new Color(0.85f, 0.68f, 0.12f) : new Color(0.1f, 0.1f, 0.1f);
                col = Grain(col, x, y, 95, 0.2f, 0.1f);
                if (y % 32 == 0) col = Rgb(col, 0.4f);
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas Ceiling()
    {
        var c = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Color col = Grain(new Color(0.27f, 0.27f, 0.3f), x, y, 101);
                if (x % 32 == 0 || y % 32 == 0) col = Rgb(col, 0.45f);
                if (x % 32 >= 10 && x % 32 <= 21 && y % 32 >= 12 && y % 32 <= 19 && y % 2 == 0) col = Rgb(col, 0.5f); // vents
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas CeilLight()
    {
        var c = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                int edge = Mathf.Min(Mathf.Min(x, 63 - x), Mathf.Min(y, 63 - y));
                Color col = edge < 6
                    ? Grain(new Color(0.3f, 0.3f, 0.32f), x, y, 111)
                    : Rgb(new Color(1f, 0.97f, 0.85f), 0.9f + 0.1f * Mathf.Clamp01(edge / 30f));
                if (edge >= 6 && (x - 6) % 13 == 0) col = Rgb(new Color(0.75f, 0.72f, 0.6f), 1f);
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas Wood()
    {
        var c = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                int plank = x / 8;
                Color wood = Rgb(new Color(0.42f, 0.26f, 0.12f), 0.8f + 0.35f * Hash(plank, 0, 121));
                float grain = Mathf.Sin((y * 0.3f) + Fbm(x * 4, y, 123) * 8f + plank * 2f) * 0.5f + 0.5f;
                Color col = Rgb(wood, 0.85f + 0.25f * grain);
                if (x % 8 == 0) col = Rgb(col, 0.4f);
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas Nukage(int frame)
    {
        var c = new PixelCanvas(64, 64);
        float offset = frame / 3f;
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float n = Fbm(x, y, 131, offset);
                Color col = Rgb(new Color(0.2f, 0.75f, 0.1f), 0.45f + 0.9f * n);
                if (n > 0.66f) col = new Color(0.65f, 1f, 0.35f);
                c.Set(x, y, col);
            }
        return c;
    }

    static PixelCanvas StatusBar()
    {
        var c = new PixelCanvas(64, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 64; x++)
            {
                Color col = Grain(new Color(0.35f, 0.34f, 0.32f), x, y * 2, 141, 0.5f, 0.25f);
                if (y == 31) col = Rgb(col, 1.4f);
                c.Set(x, y, col);
            }
        return c;
    }
}
