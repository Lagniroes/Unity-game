using UnityEngine;

/// <summary>All the animation frames for one kind of monster.</summary>
public class MonsterSprites
{
    public Sprite[] walk;      // 2 frames
    public Sprite windup;      // getting ready to attack
    public Sprite attack;      // the shot / bite / throw
    public Sprite pain;
    public Sprite[] death;     // last frame is the corpse
}

/// <summary>
/// Draws every sprite in the game with <see cref="PixelCanvas"/>: monsters, items, projectiles,
/// explosions, the first-person weapons and the status bar face.
/// </summary>
public static class SpriteLibrary
{
    public const float PixelsPerUnit = 32f;

    public static MonsterSprites Zombie, Imp, Demon, Caco;

    public static Sprite Stimpack, Medkit, Clip, Shells, Rockets, Armor, ShotgunPickup, ChaingunPickup, LauncherPickup;
    public static Sprite[] RedKey, Soulsphere, Barrel, Lamp;
    public static Sprite[] Fireball, CacoBall, Rocket, Explosion, Puff, Blood, BallBurst;

    // First-person weapons (drawn on screen, so plain textures). See WeaponSystem for how frames are used.
    public static Texture2D[] Fist, Pistol, Shotgun, Chaingun, Launcher;
    public static Texture2D SmallFlash, BigFlash;

    // Status bar face: [health level 0-4][look left/center/right]
    public static Texture2D[,] Face;
    public static Texture2D[] FaceOuch, FaceGrin;
    public static Texture2D FaceDead, KeyIcon;

    static bool built;

    static readonly Color Outline = new Color(0.05f, 0.03f, 0.03f);
    static readonly Color BloodRed = new Color(0.6f, 0.02f, 0.02f);
    static readonly Color Skin = new Color(0.85f, 0.6f, 0.45f);
    static readonly Color Sleeve = new Color(0.25f, 0.42f, 0.18f);

    public static void Build()
    {
        if (built) return;
        built = true;
        BuildMonsters();
        BuildItems();
        BuildEffects();
        BuildWeapons();
        BuildFaces();
    }

    // =====================================================================
    // Monsters
    // =====================================================================

    class HumanStyle
    {
        public Color skin, torso, legs, boots, eyes, hair;
        public bool imp;
    }

    struct Pose
    {
        public float step;     // -1..1 which leg is lifted
        public int attack;     // 0 none, 1 windup, 2 attack
        public bool pain;
    }

    static void BuildMonsters()
    {
        var zombie = new HumanStyle
        {
            skin = new Color(0.55f, 0.62f, 0.45f), torso = new Color(0.32f, 0.36f, 0.22f),
            legs = new Color(0.4f, 0.32f, 0.2f), boots = new Color(0.15f, 0.12f, 0.1f),
            eyes = new Color(1f, 0.15f, 0.1f), hair = new Color(0.2f, 0.15f, 0.1f),
        };
        var imp = new HumanStyle
        {
            skin = new Color(0.55f, 0.36f, 0.22f), torso = new Color(0.5f, 0.32f, 0.2f),
            legs = new Color(0.5f, 0.33f, 0.21f), boots = new Color(0.35f, 0.22f, 0.14f),
            eyes = new Color(1f, 0.85f, 0.1f), hair = new Color(0.9f, 0.85f, 0.7f), imp = true,
        };
        Zombie = HumanFrames(zombie);
        Imp = HumanFrames(imp);
        Demon = DemonFrames();
        Caco = CacoFrames();
    }

    static MonsterSprites HumanFrames(HumanStyle s)
    {
        Sprite Frame(Pose p, float fall = 0f, int blood = 0)
        {
            var c = new PixelCanvas(64, 64);
            if (fall > 0.95f) c.Ellipse(new Vector2(34, 4), 22f, 4f, BloodRed, 0.2f);
            // Falls to the right: rotate around the feet while sliding over so the corpse stays centered
            c.SetTransform(new Vector2(32 - 26f * fall, 3f + 9f * fall), -88f * fall);
            DrawHuman(c, s, p, blood);
            c.ResetTransform();
            c.Outline(Outline);
            return c.ToSprite(PixelsPerUnit);
        }

        return new MonsterSprites
        {
            walk = new[] { Frame(new Pose { step = 1f }), Frame(new Pose { step = -1f }) },
            windup = Frame(new Pose { attack = 1 }),
            attack = Frame(new Pose { attack = 2 }),
            pain = Frame(new Pose { pain = true }, 0f, 1),
            death = new[]
            {
                Frame(new Pose { pain = true }, 0f, 3),
                Frame(new Pose { pain = true }, 0.35f, 3),
                Frame(new Pose { pain = true }, 0.7f, 4),
                Frame(new Pose { pain = true }, 1f, 5),
            },
        };
    }

    static void DrawHuman(PixelCanvas c, HumanStyle s, Pose p, int blood)
    {
        float liftL = Mathf.Max(0f, p.step) * 4f, liftR = Mathf.Max(0f, -p.step) * 4f;
        float hunch = s.imp ? 2f : 0f;

        // Legs and boots
        c.Capsule(new Vector2(-4, 24), new Vector2(-5, 6 + liftL), 4f, 3f, s.legs);
        c.Capsule(new Vector2(4, 24), new Vector2(5, 6 + liftR), 4f, 3f, s.legs);
        c.Capsule(new Vector2(-5, 5 + liftL), new Vector2(-6, 2 + liftL), 3.4f, s.boots);
        c.Capsule(new Vector2(5, 5 + liftR), new Vector2(6, 2 + liftR), 3.4f, s.boots);

        // Torso
        c.Capsule(new Vector2(0, 27), new Vector2(0, 39 - hunch), 8.5f, 9f, s.torso);
        if (s.imp)
        {
            c.Ellipse(new Vector2(0, 33), 5f, 6f, s.torso * 0.8f + new Color(0, 0, 0, 0.2f), 0.2f); // darker chest
            for (int i = 0; i < 3; i++) c.Line(new Vector2(-4, 29 + i * 3), new Vector2(4, 29 + i * 3), s.torso * 0.6f);
        }
        else
        {
            c.Capsule(new Vector2(-7, 27), new Vector2(7, 27), 1.2f, new Color(0.15f, 0.12f, 0.08f), 0f); // belt
        }

        // Head
        Vector2 head = new Vector2(p.pain ? 1.5f : 0f, 47 - hunch);
        c.Circle(head, 5.5f, s.skin);
        if (s.imp)
        {
            // Bone spikes on the head and shoulders
            c.Tri(head + new Vector2(-5, 2), head + new Vector2(-3, 4), head + new Vector2(-7, 9), s.hair);
            c.Tri(head + new Vector2(5, 2), head + new Vector2(3, 4), head + new Vector2(7, 9), s.hair);
            c.Tri(new Vector2(-9, 37), new Vector2(-6, 39), new Vector2(-12, 44), s.hair);
            c.Tri(new Vector2(9, 37), new Vector2(6, 39), new Vector2(12, 44), s.hair);
        }
        else
        {
            c.Capsule(head + new Vector2(-3.5f, 3f), head + new Vector2(3.5f, 3f), 2.6f, s.hair, 0.2f);
        }
        c.Circle(head + new Vector2(-2, 0.5f), 1f, s.eyes, 0f);
        c.Circle(head + new Vector2(2, 0.5f), 1f, s.eyes, 0f);
        if (p.pain || s.imp) c.Ellipse(head + new Vector2(0, -3), p.pain ? 1.8f : 2.5f, p.pain ? 1.5f : 0.8f, new Color(0.15f, 0.02f, 0.02f), 0f);

        // Arms
        var lShoulder = new Vector2(-9, 37 - hunch);
        var rShoulder = new Vector2(9, 37 - hunch);
        if (s.imp)
        {
            Vector2 lHand = new Vector2(-12, 22 + liftR), rHand = new Vector2(12, 22 + liftL);
            if (p.attack == 1) rHand = new Vector2(13, 53);
            if (p.attack == 2) rHand = new Vector2(5, 36);
            if (p.pain) { lHand = new Vector2(-13, 30); rHand = new Vector2(13, 30); }
            Arm(c, lShoulder, lHand, s.skin, s.hair);
            Arm(c, rShoulder, rHand, s.skin, s.hair);
            if (p.attack == 1)
            {
                c.Glow(rHand + new Vector2(0, 5), 8f, new Color(1f, 0.5f, 0.1f, 0.9f));
                c.Circle(rHand + new Vector2(0, 5), 3.5f, new Color(1f, 0.85f, 0.3f), 0.2f);
            }
        }
        else
        {
            float raise = p.attack > 0 ? 3f : 0f;
            Vector2 lHand = new Vector2(-3, 29 + raise), rHand = new Vector2(4, 33 + raise);
            if (p.pain) { lHand = new Vector2(-12, 24); rHand = new Vector2(12, 24); }
            else
            {
                // Rifle held across the body, pointing at you
                var gunMetal = new Color(0.22f, 0.22f, 0.24f);
                c.Capsule(new Vector2(-7, 24 + raise), new Vector2(5, 36 + raise), 1.8f, gunMetal);
                c.Circle(new Vector2(2, 33 + raise), 2.4f, gunMetal);
                c.Circle(new Vector2(2, 33 + raise), 1f, Color.black, 0f);
            }
            Arm(c, lShoulder, lHand, s.torso, s.skin);
            Arm(c, rShoulder, rHand, s.torso, s.skin);
            if (p.attack == 2)
            {
                c.Glow(new Vector2(2, 34 + raise), 9f, new Color(1f, 0.85f, 0.3f, 1f));
                c.Circle(new Vector2(2, 34 + raise), 2.5f, new Color(1f, 1f, 0.8f), 0f);
            }
        }

        // Blood splatter
        for (int i = 0; i < blood * 3; i++)
        {
            float a = i * 2.4f, r = 2f + (i % 4) * 2f;
            c.Circle(new Vector2(Mathf.Cos(a) * r, 36 + Mathf.Sin(a) * r * 1.2f), 1.2f + (i % 3) * 0.5f, BloodRed, 0.2f);
        }
    }

    static void Arm(PixelCanvas c, Vector2 shoulder, Vector2 hand, Color arm, Color handColor)
    {
        Vector2 mid = (shoulder + hand) * 0.5f + new Vector2(shoulder.x > 0 ? 2.5f : -2.5f, -1f);
        c.Capsule(shoulder, mid, 3.2f, 2.8f, arm);
        c.Capsule(mid, hand, 2.8f, 2.4f, arm);
        c.Circle(hand, 2.6f, handColor);
    }

    static MonsterSprites DemonFrames()
    {
        var pink = new Color(0.88f, 0.48f, 0.52f);
        Sprite Frame(float step, int mouth, bool pain, float fall = 0f, int blood = 0)
        {
            var c = new PixelCanvas(64, 64);
            if (fall > 0.95f) c.Ellipse(new Vector2(32, 4), 24f, 4.5f, BloodRed, 0.2f);
            c.SetTransform(new Vector2(32 - 22f * fall, 3f + 17f * fall), -85f * fall);

            float liftL = Mathf.Max(0f, step) * 4f, liftR = Mathf.Max(0f, -step) * 4f;
            c.Capsule(new Vector2(-9, 16), new Vector2(-11, 4 + liftL), 5.5f, 4.5f, pink * 0.85f + Color.black * 0.15f);
            c.Capsule(new Vector2(9, 16), new Vector2(11, 4 + liftR), 5.5f, 4.5f, pink * 0.85f + Color.black * 0.15f);
            c.Ellipse(new Vector2(-11, 2 + liftL), 4.5f, 2.5f, new Color(0.3f, 0.2f, 0.15f));
            c.Ellipse(new Vector2(11, 2 + liftR), 4.5f, 2.5f, new Color(0.3f, 0.2f, 0.15f));

            c.Circle(new Vector2(0, 36), 13f, pink * 0.9f + Color.black * 0.1f); // hump
            c.Circle(new Vector2(0, 26), 15.5f, pink);

            // Short arms
            c.Capsule(new Vector2(-14, 30), new Vector2(-18, 19), 3.8f, 3f, pink);
            c.Capsule(new Vector2(14, 30), new Vector2(18, 19), 3.8f, 3f, pink);
            c.Tri(new Vector2(-20, 18), new Vector2(-16, 18), new Vector2(-19, 13), Color.white);
            c.Tri(new Vector2(20, 18), new Vector2(16, 18), new Vector2(19, 13), Color.white);

            // Huge mouth full of teeth
            float open = mouth == 1 ? 8f : mouth == 2 ? 2.5f : 5f;
            c.Ellipse(new Vector2(0, 24), 11f, open, new Color(0.35f, 0.02f, 0.04f), 0.2f);
            for (int i = -4; i <= 4; i++)
            {
                c.Tri(new Vector2(i * 2.4f - 1.1f, 24 + open - 0.5f), new Vector2(i * 2.4f + 1.1f, 24 + open - 0.5f),
                    new Vector2(i * 2.4f, 24 + open - 3.5f), new Color(0.95f, 0.92f, 0.8f));
                c.Tri(new Vector2(i * 2.4f - 1.1f, 24 - open + 0.5f), new Vector2(i * 2.4f + 1.1f, 24 - open + 0.5f),
                    new Vector2(i * 2.4f, 24 - open + 3.5f), new Color(0.95f, 0.92f, 0.8f));
            }
            // Beady eyes under a heavy brow
            c.Capsule(new Vector2(-9, 38), new Vector2(-2, 36), 2f, pink * 0.7f + Color.black * 0.3f);
            c.Capsule(new Vector2(9, 38), new Vector2(2, 36), 2f, pink * 0.7f + Color.black * 0.3f);
            c.Circle(new Vector2(-5, 34.5f), 1.3f, pain ? Color.white : new Color(0.9f, 0.9f, 0.2f), 0f);
            c.Circle(new Vector2(5, 34.5f), 1.3f, pain ? Color.white : new Color(0.9f, 0.9f, 0.2f), 0f);

            for (int i = 0; i < blood * 3; i++)
            {
                float a = i * 2.4f, r = 3f + (i % 4) * 2.5f;
                c.Circle(new Vector2(Mathf.Cos(a) * r, 28 + Mathf.Sin(a) * r), 1.5f + (i % 3) * 0.6f, BloodRed, 0.2f);
            }
            c.ResetTransform();
            c.Outline(Outline);
            return c.ToSprite(PixelsPerUnit);
        }

        return new MonsterSprites
        {
            walk = new[] { Frame(1f, 0, false), Frame(-1f, 0, false) },
            windup = Frame(0f, 1, false),
            attack = Frame(0f, 2, false),
            pain = Frame(0f, 1, true, 0f, 1),
            death = new[] { Frame(0f, 1, true, 0f, 3), Frame(0f, 1, true, 0.35f, 3), Frame(0f, 1, true, 0.7f, 4), Frame(0f, 1, true, 1f, 5) },
        };
    }

    static MonsterSprites CacoFrames()
    {
        var red = new Color(0.82f, 0.14f, 0.1f);
        Sprite Frame(int mouth, bool pain, int dying = 0)
        {
            var c = new PixelCanvas(64, 64);
            if (dying >= 3)
            {
                c.Ellipse(new Vector2(32, 5), 26f, 5f, BloodRed, 0.2f);
                c.Ellipse(new Vector2(32, 8), 22f, 6f, red * 0.8f + Color.black * 0.2f, 0.4f);
                c.Circle(new Vector2(26, 9), 2.5f, new Color(0.2f, 0.8f, 0.3f), 0.2f);
                c.Outline(Outline);
                return c.ToSprite(PixelsPerUnit);
            }
            float squash = dying == 2 ? 0.55f : dying == 1 ? 0.85f : 1f;
            float cy = 8f + 24f * squash;
            c.SetTransform(new Vector2(32, 0));

            // Horns
            c.Tri(new Vector2(-14, cy + 18 * squash), new Vector2(-8, cy + 21 * squash), new Vector2(-13, cy + 30 * squash), new Color(0.85f, 0.8f, 0.7f));
            c.Tri(new Vector2(14, cy + 18 * squash), new Vector2(8, cy + 21 * squash), new Vector2(13, cy + 30 * squash), new Color(0.85f, 0.8f, 0.7f));
            c.Ellipse(new Vector2(0, cy), 24f, 24f * squash, red, 0.5f);

            // Mouth
            float open = mouth == 1 ? 8f : mouth == 2 ? 10f : 3.5f;
            Vector2 m = new Vector2(0, cy - 10 * squash);
            c.Ellipse(m, 14f, open * squash, new Color(0.25f, 0.02f, 0.05f), 0.2f);
            if (mouth == 1) c.Glow(m, 9f, new Color(1f, 0.4f, 0.9f, 0.9f));
            for (int i = -5; i <= 5; i++)
            {
                c.Tri(new Vector2(i * 2.4f - 1f, m.y + open * squash - 0.3f), new Vector2(i * 2.4f + 1f, m.y + open * squash - 0.3f),
                    new Vector2(i * 2.4f, m.y + open * squash - 3f), new Color(0.95f, 0.95f, 0.85f));
                c.Tri(new Vector2(i * 2.4f - 1f, m.y - open * squash + 0.3f), new Vector2(i * 2.4f + 1f, m.y - open * squash + 0.3f),
                    new Vector2(i * 2.4f, m.y - open * squash + 3f), new Color(0.95f, 0.95f, 0.85f));
            }

            // One big eye
            Vector2 e = new Vector2(0, cy + 8 * squash);
            if (dying > 0)
            {
                c.Circle(e, 6f, BloodRed, 0.2f);
                c.Circle(e + new Vector2(2, -1), 2.5f, new Color(0.3f, 0f, 0f), 0.1f);
            }
            else if (pain)
            {
                c.Ellipse(e, 7f, 1.2f, new Color(0.3f, 0.02f, 0.02f), 0f);
            }
            else
            {
                c.Circle(e, 7f, new Color(0.95f, 0.95f, 0.9f), 0.3f);
                c.Circle(e, 4.5f, new Color(0.15f, 0.75f, 0.25f), 0.2f);
                c.Circle(e, 2f, Color.black, 0f);
                c.Circle(e + new Vector2(-1.5f, 1.5f), 0.8f, Color.white, 0f);
            }
            c.ResetTransform();
            c.Outline(Outline);
            return c.ToSprite(PixelsPerUnit);
        }

        Sprite idle = Frame(0, false);
        return new MonsterSprites
        {
            walk = new[] { idle, idle },
            windup = Frame(1, false),
            attack = Frame(2, false),
            pain = Frame(0, true),
            death = new[] { Frame(2, true, 1), Frame(2, true, 1), Frame(2, true, 2), Frame(0, true, 3) },
        };
    }

    // =====================================================================
    // Items, decorations, projectiles, effects
    // =====================================================================

    static Sprite Item(int w, int h, System.Action<PixelCanvas> draw, bool outline = true)
    {
        var c = new PixelCanvas(w, h);
        draw(c);
        if (outline) c.Outline(Outline);
        return c.ToSprite(PixelsPerUnit);
    }

    static void MedBox(PixelCanvas c, int x, int y, int w, int h)
    {
        c.Rect(x, y, w, h, new Color(0.88f, 0.88f, 0.86f));
        c.Rect(x, y, w, 1, new Color(0.6f, 0.6f, 0.6f));
        c.Rect(x, y + h - 1, w, 1, Color.white);
        int cx = x + w / 2, cy = y + h / 2;
        c.Rect(cx - 1, cy - 3, 3, 7, new Color(0.85f, 0.05f, 0.05f));
        c.Rect(cx - 3, cy - 1, 7, 3, new Color(0.85f, 0.05f, 0.05f));
    }

    static void BuildItems()
    {
        Stimpack = Item(16, 12, c => MedBox(c, 2, 0, 12, 9));
        Medkit = Item(24, 18, c =>
        {
            MedBox(c, 1, 0, 22, 13);
            c.Rect(8, 13, 8, 1, new Color(0.3f, 0.3f, 0.3f));
            c.Rect(8, 13, 1, 3, new Color(0.3f, 0.3f, 0.3f));
            c.Rect(15, 13, 1, 3, new Color(0.3f, 0.3f, 0.3f));
            c.Rect(8, 15, 8, 1, new Color(0.3f, 0.3f, 0.3f));
        });
        Clip = Item(12, 14, c =>
        {
            c.Rect(3, 0, 6, 10, new Color(0.25f, 0.25f, 0.27f));
            c.Rect(3, 9, 6, 1, new Color(0.5f, 0.5f, 0.55f));
            for (int i = 0; i < 3; i++) c.Rect(3 + i * 2, 10, 1, 3, new Color(0.85f, 0.65f, 0.2f));
        });
        Shells = Item(22, 12, c =>
        {
            c.Rect(1, 0, 20, 7, new Color(0.6f, 0.1f, 0.08f));
            c.Rect(1, 6, 20, 1, new Color(0.8f, 0.25f, 0.2f));
            for (int i = 0; i < 6; i++)
            {
                c.Rect(2 + i * 3, 7, 2, 3, new Color(0.75f, 0.12f, 0.1f));
                c.Rect(2 + i * 3, 7, 2, 1, new Color(0.85f, 0.65f, 0.2f));
            }
        });
        Rockets = Item(28, 14, c =>
        {
            c.Rect(1, 0, 26, 8, new Color(0.4f, 0.32f, 0.18f));
            c.Rect(1, 7, 26, 1, new Color(0.55f, 0.45f, 0.28f));
            for (int i = 0; i < 4; i++)
            {
                c.Capsule(new Vector2(5 + i * 6, 6), new Vector2(5 + i * 6, 11), 2.2f, new Color(0.45f, 0.5f, 0.3f));
                c.Tri(new Vector2(3 + i * 6, 11), new Vector2(7 + i * 6, 11), new Vector2(5 + i * 6, 14), new Color(0.7f, 0.15f, 0.1f));
            }
        });
        Armor = Item(26, 24, c =>
        {
            var green = new Color(0.15f, 0.65f, 0.2f);
            c.Capsule(new Vector2(13, 3), new Vector2(13, 15), 9f, 10f, green, 0.5f);
            c.Circle(new Vector2(5, 19), 4.5f, green, 0.4f);
            c.Circle(new Vector2(21, 19), 4.5f, green, 0.4f);
            c.Ellipse(new Vector2(13, 21), 3.5f, 2.5f, new Color(0.08f, 0.3f, 0.1f), 0f); // collar
            c.Rect(12, 4, 3, 10, new Color(0.1f, 0.35f, 0.12f));
        });
        Sprite Key(bool lit) => Item(12, 16, c =>
        {
            c.Rect(2, 0, 8, 13, lit ? new Color(0.95f, 0.15f, 0.1f) : new Color(0.6f, 0.08f, 0.06f));
            c.Rect(3, 7, 6, 4, new Color(0.95f, 0.95f, 0.9f));
            c.Rect(3, 2, 6, 1, new Color(0.3f, 0.02f, 0.02f));
            c.Rect(3, 4, 4, 1, new Color(0.3f, 0.02f, 0.02f));
        });
        RedKey = new[] { Key(true), Key(false) };

        ShotgunPickup = Item(44, 12, c =>
        {
            c.Capsule(new Vector2(4, 5), new Vector2(14, 4), 3f, 2.2f, new Color(0.45f, 0.26f, 0.1f));
            c.Rect(14, 3, 8, 4, new Color(0.25f, 0.25f, 0.27f));
            c.Rect(22, 5, 20, 2, new Color(0.3f, 0.3f, 0.33f));
            c.Rect(24, 3, 10, 2, new Color(0.5f, 0.32f, 0.14f));
        });
        ChaingunPickup = Item(44, 16, c =>
        {
            c.Rect(4, 3, 14, 9, new Color(0.25f, 0.25f, 0.27f));
            c.Rect(8, 1, 3, 4, new Color(0.2f, 0.2f, 0.2f));
            for (int i = 0; i < 3; i++) c.Rect(18, 4 + i * 2, 24, 1, new Color(0.45f, 0.45f, 0.5f));
            c.Rect(36, 3, 3, 7, new Color(0.3f, 0.3f, 0.32f));
        });
        LauncherPickup = Item(48, 16, c =>
        {
            c.Capsule(new Vector2(6, 8), new Vector2(42, 8), 4.5f, new Color(0.38f, 0.42f, 0.26f), 0.5f);
            c.Rect(14, 1, 4, 5, new Color(0.25f, 0.25f, 0.25f));
            c.Rect(24, 12, 8, 3, new Color(0.25f, 0.25f, 0.25f));
            c.Rect(44, 6, 2, 5, new Color(0.1f, 0.1f, 0.1f));
        });

        Sprite Sphere(float phase) => Item(24, 30, c =>
        {
            c.Glow(new Vector2(12, 16), 12f, new Color(0.3f, 0.5f, 1f, 0.8f));
            c.Circle(new Vector2(12, 16), 8.5f, new Color(0.3f + phase * 0.2f, 0.5f, 1f), 0.5f);
            c.Circle(new Vector2(9, 18), 1.3f, new Color(0.05f, 0.05f, 0.3f), 0f);
            c.Circle(new Vector2(15, 18), 1.3f, new Color(0.05f, 0.05f, 0.3f), 0f);
            c.Ellipse(new Vector2(12, 12.5f), 2.5f, 1.2f, new Color(0.05f, 0.05f, 0.3f), 0f);
            c.Circle(new Vector2(9, 20), 1.5f, new Color(0.85f, 0.95f, 1f), 0f);
        }, false);
        Soulsphere = new[] { Sphere(0f), Sphere(1f) };

        Sprite BarrelFrame(int burst) => Item(26, 34, c =>
        {
            if (burst > 0)
            {
                c.Glow(new Vector2(13, 18), 13f + burst * 2, new Color(1f, 0.7f, 0.2f, 1f));
                c.Circle(new Vector2(13, 16), 6f + burst * 2, new Color(1f, 0.95f, 0.6f), 0.2f);
                return;
            }
            var body = new Color(0.36f, 0.42f, 0.3f);
            c.Capsule(new Vector2(13, 3), new Vector2(13, 26), 9f, body, 0.55f);
            c.Rect(4, 9, 18, 1, body * 0.6f + Color.black * 0.4f);
            c.Rect(4, 20, 18, 1, body * 0.6f + Color.black * 0.4f);
            c.Ellipse(new Vector2(13, 27), 8f, 2.5f, new Color(0.35f, 1f, 0.2f), 0.1f); // glowing slime on top
            c.Rect(8, 12, 3, 5, new Color(0.15f, 0.15f, 0.12f));
        }, burst == 0);
        Barrel = new[] { BarrelFrame(0), BarrelFrame(1) };

        Sprite LampFrame(float k) => Item(16, 72, c =>
        {
            c.Rect(5, 0, 6, 3, new Color(0.2f, 0.2f, 0.22f));
            c.Rect(7, 3, 2, 56, new Color(0.3f, 0.3f, 0.33f));
            c.Glow(new Vector2(8, 64), 8f, new Color(1f, 0.9f, 0.6f, 0.8f * k));
            c.Capsule(new Vector2(8, 60), new Vector2(8, 68), 3.5f, new Color(1f, 0.95f, 0.75f), 0.1f);
        }, false);
        Lamp = new[] { LampFrame(1f), LampFrame(0.7f) };
    }

    static Sprite[] Balls(Color outer, Color inner, int size)
    {
        var frames = new Sprite[2];
        for (int f = 0; f < 2; f++)
        {
            var c = new PixelCanvas(size, size);
            float h = size * 0.5f;
            c.Glow(new Vector2(h, h), h, new Color(outer.r, outer.g, outer.b, 0.9f));
            c.Circle(new Vector2(h + (f == 0 ? -0.5f : 0.5f), h), h * 0.45f, inner, 0.2f);
            frames[f] = Sprite.Create(c.ToTexture(), new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
        }
        return frames;
    }

    static Sprite[] Burst(int count, int size, System.Func<int, Color> outer, System.Func<int, Color> inner, float grow)
    {
        var frames = new Sprite[count];
        for (int f = 0; f < count; f++)
        {
            var c = new PixelCanvas(size, size);
            float h = size * 0.5f;
            float t = (f + 1f) / count;
            c.Glow(new Vector2(h, h), h * (0.5f + 0.5f * t), outer(f));
            var rng = new System.Random(f * 31 + size);
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector2(h + (float)(rng.NextDouble() - 0.5) * h * grow * t, h + (float)(rng.NextDouble() - 0.5) * h * grow * t);
                c.Glow(p, h * 0.35f, inner(f));
            }
            frames[f] = Sprite.Create(c.ToTexture(), new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
        }
        return frames;
    }

    static void BuildEffects()
    {
        Fireball = Balls(new Color(1f, 0.45f, 0.1f), new Color(1f, 0.9f, 0.5f), 16);
        CacoBall = Balls(new Color(0.9f, 0.2f, 0.9f), new Color(1f, 0.8f, 1f), 16);
        Rocket = Balls(new Color(1f, 0.6f, 0.2f), new Color(0.4f, 0.4f, 0.35f), 12);
        Explosion = Burst(4, 64,
            f => f < 2 ? new Color(1f, 0.6f, 0.15f, 1f) : new Color(0.6f, 0.15f, 0.05f, 0.8f - f * 0.15f),
            f => f < 2 ? new Color(1f, 0.95f, 0.6f, 1f) : new Color(0.3f, 0.25f, 0.2f, 0.6f), 1.2f);
        BallBurst = Burst(3, 24,
            f => new Color(1f, 0.55f, 0.2f, 1f - f * 0.25f), f => new Color(1f, 0.95f, 0.7f, 1f - f * 0.3f), 1f);
        Puff = Burst(3, 12,
            f => new Color(0.75f, 0.75f, 0.72f, 0.9f - f * 0.25f), f => f == 0 ? new Color(1f, 0.9f, 0.5f, 1f) : new Color(0.6f, 0.6f, 0.6f, 0.5f), 0.8f);
        Blood = Burst(3, 14,
            f => new Color(0.7f, 0.03f, 0.03f, 0.95f - f * 0.25f), f => new Color(0.45f, 0f, 0f, 1f), 1.4f);
    }

    // =====================================================================
    // First-person weapons (96x72, drawn from the bottom center of the screen)
    // =====================================================================

    const int WeaponW = 96, WeaponH = 72;

    static Texture2D Weapon(System.Action<PixelCanvas> draw, Vector2 shift, float tilt = 0f)
    {
        var c = new PixelCanvas(WeaponW, WeaponH);
        c.SetTransform(new Vector2(WeaponW / 2f + shift.x, shift.y), tilt);
        draw(c);
        c.ResetTransform();
        c.Outline(Outline);
        return c.ToTexture();
    }

    static void Hand(PixelCanvas c, Vector2 p, float r)
    {
        c.Circle(p, r, Skin);
        for (int i = -1; i <= 1; i++)
            c.Line(p + new Vector2(i * r * 0.45f - r * 0.3f, r * 0.2f), p + new Vector2(i * r * 0.45f + r * 0.3f, r * 0.2f), Skin * 0.7f + Color.black * 0.3f);
    }

    static void BuildWeapons()
    {
        var steel = new Color(0.28f, 0.29f, 0.32f);
        var steelLight = new Color(0.45f, 0.47f, 0.5f);
        var wood = new Color(0.48f, 0.29f, 0.12f);

        // Fist: idle, punch out, punch far
        void FistDraw(PixelCanvas c, Vector2 p, float r)
        {
            c.Capsule(p + new Vector2(18, -26), p + new Vector2(4, -6), 11f, 9f, Sleeve);
            c.Circle(p, r, Skin);
            for (int i = 0; i < 4; i++)
                c.Line(p + new Vector2(-r * 0.7f + i * r * 0.45f, r * 0.3f), p + new Vector2(-r * 0.7f + i * r * 0.45f, -r * 0.2f), Skin * 0.65f + Color.black * 0.35f);
        }
        Fist = new[]
        {
            Weapon(c => FistDraw(c, new Vector2(20, 12), 10f), Vector2.zero),
            Weapon(c => FistDraw(c, new Vector2(8, 26), 10f), Vector2.zero),
            Weapon(c => FistDraw(c, new Vector2(2, 34), 8.5f), Vector2.zero),
        };

        // Pistol: idle, fire (kicks up), recoil
        void PistolDraw(PixelCanvas c)
        {
            c.Capsule(new Vector2(12, -14), new Vector2(5, 6), 11f, 9f, Sleeve);
            c.Capsule(new Vector2(0, 10), new Vector2(0, 38), 5f, steel);
            c.Capsule(new Vector2(0, 22), new Vector2(0, 40), 4f, steelLight);
            c.Circle(new Vector2(0, 40), 2f, Color.black, 0f);
            Hand(c, new Vector2(1, 14), 8f);
        }
        Pistol = new[]
        {
            Weapon(PistolDraw, new Vector2(4, 0)),
            Weapon(PistolDraw, new Vector2(4, 5)),
            Weapon(PistolDraw, new Vector2(4, -3)),
        };

        // Shotgun: idle, fire, pump back, pump back further
        void ShotgunDraw(PixelCanvas c, float pump)
        {
            c.Capsule(new Vector2(-30, -12), new Vector2(-10, 30 - pump), 9f, 7f, Sleeve);          // left arm
            c.Capsule(new Vector2(30, -18), new Vector2(14, 2), 11f, 9f, Sleeve);                   // right arm
            c.Capsule(new Vector2(0, -8), new Vector2(0, 8), 11f, 9f, wood);                        // stock
            c.Capsule(new Vector2(0, 6), new Vector2(0, 22), 8f, steel);                            // receiver
            c.Capsule(new Vector2(0, 18), new Vector2(0, 64), 3.4f, steel);                         // barrel
            c.Capsule(new Vector2(1, 20), new Vector2(1, 62), 1.4f, steelLight, 0f);
            c.Circle(new Vector2(0, 64), 1.8f, Color.black, 0f);
            c.Capsule(new Vector2(0, 30 - pump), new Vector2(0, 44 - pump), 6f, wood);              // pump
            for (int i = 0; i < 4; i++) c.Line(new Vector2(-5, 33 + i * 3 - pump), new Vector2(5, 33 + i * 3 - pump), wood * 0.6f + Color.black * 0.4f);
            Hand(c, new Vector2(-8, 36 - pump), 6.5f);
            Hand(c, new Vector2(12, 4), 8f);
        }
        Shotgun = new[]
        {
            Weapon(c => ShotgunDraw(c, 0f), Vector2.zero),
            Weapon(c => ShotgunDraw(c, 0f), new Vector2(0, 4)),
            Weapon(c => ShotgunDraw(c, 8f), new Vector2(6, -4), 10f),
            Weapon(c => ShotgunDraw(c, 14f), new Vector2(10, -8), 18f),
        };

        // Chaingun: idle, spin A, spin B
        void ChaingunDraw(PixelCanvas c, float spin)
        {
            c.Capsule(new Vector2(-34, -14), new Vector2(-16, 14), 10f, 8f, Sleeve);
            c.Capsule(new Vector2(34, -14), new Vector2(16, 14), 10f, 8f, Sleeve);
            c.Capsule(new Vector2(0, 0), new Vector2(0, 22), 14f, 12f, steel);
            c.Capsule(new Vector2(0, 20), new Vector2(0, 58), 9f, steel * 0.8f + Color.black * 0.2f);
            for (int i = 0; i < 6; i++)
            {
                float a = (i / 6f + spin) * Mathf.PI * 2f;
                float x = Mathf.Sin(a) * 6.5f;
                if (Mathf.Cos(a) < 0f) continue; // only the barrels facing us
                c.Capsule(new Vector2(x, 22), new Vector2(x, 60), 1.8f, Color.Lerp(steel, steelLight, Mathf.Cos(a)));
            }
            c.Ellipse(new Vector2(0, 60), 9f, 3f, steel * 0.6f + Color.black * 0.4f, 0f);
            Hand(c, new Vector2(-15, 16), 7.5f);
            Hand(c, new Vector2(15, 16), 7.5f);
        }
        Chaingun = new[]
        {
            Weapon(c => ChaingunDraw(c, 0f), Vector2.zero),
            Weapon(c => ChaingunDraw(c, 1f / 12f), new Vector2(0, 3)),
            Weapon(c => ChaingunDraw(c, 1f / 6f), new Vector2(0, 1)),
        };

        // Rocket launcher: idle, fire
        void LauncherDraw(PixelCanvas c)
        {
            var olive = new Color(0.38f, 0.42f, 0.27f);
            c.Capsule(new Vector2(-34, -16), new Vector2(-16, 22), 10f, 8f, Sleeve);
            c.Capsule(new Vector2(34, -18), new Vector2(14, 6), 11f, 9f, Sleeve);
            c.Capsule(new Vector2(0, -6), new Vector2(0, 50), 14f, 13f, olive, 0.45f);
            c.Capsule(new Vector2(-13, 20), new Vector2(13, 20), 1.5f, olive * 0.6f + Color.black * 0.4f, 0f);
            c.Capsule(new Vector2(-13, 36), new Vector2(13, 36), 1.5f, olive * 0.6f + Color.black * 0.4f, 0f);
            c.Ellipse(new Vector2(0, 54), 10f, 5f, new Color(0.08f, 0.08f, 0.08f), 0f);
            c.Capsule(new Vector2(-12, 30), new Vector2(-12, 46), 3f, steel);                      // sight
            Hand(c, new Vector2(-15, 24), 7.5f);
            Hand(c, new Vector2(13, 8), 8f);
        }
        Launcher = new[]
        {
            Weapon(LauncherDraw, Vector2.zero),
            Weapon(LauncherDraw, new Vector2(0, -6)),
        };

        SmallFlash = FlashTexture(10f);
        BigFlash = FlashTexture(16f);
    }

    static Texture2D FlashTexture(float r)
    {
        var c = new PixelCanvas(WeaponW, WeaponH);
        var center = new Vector2(WeaponW / 2f, 58f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f + 0.3f;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var side = new Vector2(-dir.y, dir.x) * r * 0.25f;
            c.Tri(center + side, center - side, center + dir * r * (i % 2 == 0 ? 1.3f : 0.9f), new Color(1f, 0.75f, 0.2f));
        }
        c.Glow(center, r * 1.3f, new Color(1f, 0.85f, 0.4f, 1f));
        c.Circle(center, r * 0.45f, new Color(1f, 1f, 0.9f), 0f);
        return c.ToTexture();
    }

    // =====================================================================
    // Status bar face
    // =====================================================================

    static void BuildFaces()
    {
        Face = new Texture2D[5, 3];
        FaceOuch = new Texture2D[5];
        FaceGrin = new Texture2D[5];
        for (int level = 0; level < 5; level++)
        {
            for (int look = 0; look < 3; look++) Face[level, look] = DrawFace(level, look - 1, 0);
            FaceOuch[level] = DrawFace(level, 0, 1);
            FaceGrin[level] = DrawFace(level, 0, 2);
        }
        FaceDead = DrawFace(4, 0, 3);

        var k = new PixelCanvas(8, 10);
        k.Rect(1, 0, 6, 10, new Color(0.9f, 0.12f, 0.08f));
        k.Rect(2, 5, 4, 3, Color.white);
        KeyIcon = k.ToTexture();
    }

    /// <summary>mood: 0 normal, 1 ouch, 2 grin, 3 dead.</summary>
    static Texture2D DrawFace(int level, int look, int mood)
    {
        var c = new PixelCanvas(24, 30);
        Color skin = mood == 3 ? new Color(0.6f, 0.5f, 0.45f) : Skin;
        var dark = new Color(0.2f, 0.1f, 0.05f);
        c.Ellipse(new Vector2(12, 13), 10f, 13f, skin, 0.4f);
        c.Capsule(new Vector2(5, 22), new Vector2(19, 22), 4.5f, new Color(0.38f, 0.24f, 0.1f), 0.3f); // hair
        c.Rect(2, 12, 2, 5, skin * 0.85f + Color.black * 0.15f);  // ears
        c.Rect(20, 12, 2, 5, skin * 0.85f + Color.black * 0.15f);

        // Brows and eyes
        c.Rect(5, 17, 5, 1, dark);
        c.Rect(14, 17, 5, 1, dark);
        if (mood == 3)
        {
            c.Rect(6, 14, 4, 1, dark);
            c.Rect(14, 14, 4, 1, dark);
        }
        else
        {
            int eyeH = mood == 1 ? 3 : 2;
            c.Rect(6, 14, 4, eyeH, Color.white);
            c.Rect(14, 14, 4, eyeH, Color.white);
            c.Rect(7 + look, 14, 2, 2, new Color(0.15f, 0.3f, 0.6f));
            c.Rect(15 + look, 14, 2, 2, new Color(0.15f, 0.3f, 0.6f));
        }
        c.Rect(11, 9, 2, 4, skin * 0.75f + Color.black * 0.25f); // nose

        // Mouth
        if (mood == 1) c.Ellipse(new Vector2(12, 5.5f), 2.5f, 2f, new Color(0.25f, 0.02f, 0.02f), 0f);
        else if (mood == 2)
        {
            c.Rect(7, 4, 10, 3, dark);
            c.Rect(8, 5, 8, 1, Color.white);
        }
        else c.Rect(8, 5, 8, 1, dark);

        // Blood the more hurt you are
        var rng = new System.Random(7);
        int spots = mood == 3 ? 16 : level * 4;
        for (int i = 0; i < spots; i++)
        {
            int x = rng.Next(4, 20), y = rng.Next(2, 20);
            c.Rect(x, y, 1 + rng.Next(2), 1 + rng.Next(3), BloodRed);
        }
        c.Outline(Outline);
        return c.ToTexture();
    }
}
