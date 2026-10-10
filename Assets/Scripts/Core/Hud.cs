using UnityEngine;

/// <summary>
/// Everything drawn on screen: the first-person weapon, damage/pickup flashes, the classic
/// status bar (ammo, health, arms, face, armor, keys, ammo counts), messages, the automap,
/// and the title / death / level-complete screens. Sizes are in "Doom pixels" (screen height / 200).
/// </summary>
public class Hud : MonoBehaviour
{
    static readonly Color HudRed = new Color(0.9f, 0.12f, 0.06f);
    static readonly Color HudGold = new Color(1f, 0.82f, 0.25f);
    static readonly Color HudGray = new Color(0.78f, 0.76f, 0.72f);

    GUIStyle number, label, small, message, title, subtitle;
    int styleSize = -1;
    float nextFaceLook;
    int faceLook = 1;

    float S => Screen.height / 200f;

    void EnsureStyles()
    {
        int size = Mathf.RoundToInt(S * 10f);
        if (size == styleSize && number != null) return;
        styleSize = size;
        GUIStyle Make(float px, Color color, TextAnchor anchor) => new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(8, Mathf.RoundToInt(px * S)),
            fontStyle = FontStyle.Bold,
            alignment = anchor,
            normal = { textColor = color },
            clipping = TextClipping.Overflow,
            wordWrap = false,
        };
        number = Make(14f, HudRed, TextAnchor.MiddleCenter);
        label = Make(5f, HudGray, TextAnchor.MiddleCenter);
        small = Make(5f, HudGold, TextAnchor.MiddleLeft);
        message = Make(6.5f, HudRed, TextAnchor.UpperLeft);
        title = Make(18f, HudRed, TextAnchor.MiddleCenter);
        subtitle = Make(7f, HudGray, TextAnchor.MiddleCenter);
    }

    void OnGUI()
    {
        GameManager gm = GameManager.Instance;
        Player p = Player.Instance;
        if (!gm || !p) return;
        EnsureStyles();

        float s = S;
        float barH = 32f * s;
        float viewH = Screen.height - barH;

        DrawWeapon(p, s, viewH);

        if (p.DamageFlash > 0f) Fill(new Rect(0, 0, Screen.width, viewH), new Color(1f, 0f, 0f, p.DamageFlash * 0.55f));
        if (p.PickupFlash > 0f) Fill(new Rect(0, 0, Screen.width, viewH), new Color(1f, 0.85f, 0.3f, p.PickupFlash * 0.45f));

        if (gm.state == GameManager.State.Playing && !gm.ShowAutomap)
        {
            float c = Mathf.Max(1f, s * 0.6f);
            Fill(new Rect(Screen.width * 0.5f - c * 0.5f, viewH * 0.5f - c * 3f, c, c * 2f), new Color(1f, 1f, 1f, 0.6f));
            Fill(new Rect(Screen.width * 0.5f - c * 0.5f, viewH * 0.5f + c, c, c * 2f), new Color(1f, 1f, 1f, 0.6f));
            Fill(new Rect(Screen.width * 0.5f - c * 3f, viewH * 0.5f - c * 0.5f, c * 2f, c), new Color(1f, 1f, 1f, 0.6f));
            Fill(new Rect(Screen.width * 0.5f + c, viewH * 0.5f - c * 0.5f, c * 2f, c), new Color(1f, 1f, 1f, 0.6f));
        }

        if (gm.ShowAutomap) DrawAutomap(p, s, viewH);
        DrawStatusBar(gm, p, s, barH);

        string msg = gm.MessageText;
        if (msg != null) Shadowed(new Rect(4f * s, 3f * s, Screen.width, 10f * s), msg, message);

        DrawOverlays(gm, s, viewH);
    }

    // ---------------------------------------------------------------------

    void DrawWeapon(Player p, float s, float viewH)
    {
        p.Weapons.GetView(out Texture2D sprite, out Texture2D flash, out Vector2 offset);
        if (!sprite) return;
        float w = sprite.width * s, h = sprite.height * s;
        var rect = new Rect(Screen.width * 0.5f - w * 0.5f + offset.x * s, viewH - h + 4f * s + offset.y * s, w, h);
        Color light = Lighting.Sample(p.EyePosition + p.Head.forward * 0.5f, p.EyePosition);
        GUI.color = new Color(Mathf.Clamp(light.r, 0.3f, 1f), Mathf.Clamp(light.g, 0.3f, 1f), Mathf.Clamp(light.b, 0.3f, 1f)); // your gun is lit by the room
        GUI.DrawTexture(rect, sprite);
        GUI.color = Color.white;
        if (flash) GUI.DrawTexture(rect, flash);
    }

    void DrawStatusBar(GameManager gm, Player p, float s, float barH)
    {
        float top = Screen.height - barH;
        Texture2D bg = TextureLibrary.Get("StatusBar");
        GUI.DrawTextureWithTexCoords(new Rect(0, top, Screen.width, barH), bg, new Rect(0, 0, Screen.width / (bg.width * s), 1f));
        Fill(new Rect(0, top, Screen.width, s), new Color(0.6f, 0.58f, 0.55f));

        float x0 = (Screen.width - 320f * s) * 0.5f;
        Rect R(float x, float y, float w, float h) => new Rect(x0 + x * s, top + y * s, w * s, h * s);
        void Divider(float x) => Fill(R(x, 2, 1, 30), new Color(0.15f, 0.14f, 0.13f));

        WeaponSystem weapons = p.Weapons;

        // AMMO
        int ammo = weapons.CurrentAmmo;
        Shadowed(R(0, 3, 48, 20), ammo >= 0 ? ammo.ToString() : "", number);
        Shadowed(R(0, 23, 48, 8), "AMMO", label);
        Divider(48);

        // HEALTH
        Shadowed(R(48, 3, 56, 20), p.health + "%", number);
        Shadowed(R(48, 23, 56, 8), "HEALTH", label);
        Divider(104);

        // ARMS: weapon slots 2-7 like the original (6 and 7 are always empty here)
        for (int i = 0; i < 6; i++)
        {
            int slot = i + 2;
            bool has = slot <= 5 && weapons.owned[slot - 1];
            Shadowed(R(106 + (i % 3) * 11, 4 + (i / 3) * 9, 10, 9), slot.ToString(), has ? small : label);
        }
        Shadowed(R(104, 23, 36, 8), "ARMS", label);
        Divider(140);

        // Face
        Fill(R(143, 2, 34, 30), new Color(0.12f, 0.11f, 0.1f));
        GUI.DrawTexture(R(148, 2.5f, 24, 29.5f), FaceTexture(p));
        Divider(178);

        // ARMOR
        Shadowed(R(178, 3, 58, 20), p.armor + "%", number);
        Shadowed(R(178, 23, 58, 8), "ARMOR", label);
        Divider(236);

        // KEYS
        Fill(R(238, 3, 11, 26), new Color(0.15f, 0.14f, 0.13f));
        if (p.hasRedKey) GUI.DrawTexture(R(239.5f, 5, 8, 10), SpriteLibrary.KeyIcon);
        Divider(250);

        // Ammo counts
        string[] names = { "BULL", "SHEL", "RCKT" };
        for (int i = 0; i < 3; i++)
        {
            float y = 4 + i * 8;
            Shadowed(R(253, y, 22, 7), names[i], label);
            Shadowed(R(276, y, 44, 7), weapons.ammo[i] + " / " + WeaponSystem.MaxAmmo[i], small);
        }
    }

    Texture2D FaceTexture(Player p)
    {
        if (p.IsDead) return SpriteLibrary.FaceDead;
        int hp = p.health;
        int level = hp >= 80 ? 0 : hp >= 60 ? 1 : hp >= 40 ? 2 : hp >= 20 ? 3 : 4;
        if (Time.time - p.LastHurtTime < 0.6f) return SpriteLibrary.FaceOuch[level];
        if (Time.time < p.GrinUntil) return SpriteLibrary.FaceGrin[level];
        if (Time.time >= nextFaceLook)
        {
            nextFaceLook = Time.time + Random.Range(0.6f, 2f);
            faceLook = Random.value < 0.5f ? 1 : Random.Range(0, 3);
        }
        return SpriteLibrary.Face[level, faceLook];
    }

    void DrawAutomap(Player p, float s, float viewH)
    {
        Level level = Level.Current;
        if (!level) return;
        Fill(new Rect(0, 0, Screen.width, viewH), new Color(0f, 0f, 0f, 0.88f));

        float cellPx = 9f * s;
        float line = Mathf.Max(1f, s * 0.7f);
        Vector3 pos = p.transform.position;
        float gx = pos.x / Level.Cell, gy = level.Height - pos.z / Level.Cell;
        float cx = Screen.width * 0.5f, cy = viewH * 0.5f;
        var wallColor = new Color(0.85f, 0.15f, 0.1f);

        for (int y = 0; y < level.Height; y++)
            for (int x = 0; x < level.Width; x++)
            {
                Level.MapCell cell = level.AutomapCell(x, y);
                if (cell != Level.MapCell.Open && cell != Level.MapCell.Door && cell != Level.MapCell.LockedDoor) continue;
                float sx = cx + (x - gx) * cellPx, sy = cy + (y - gy) * cellPx;
                if (sx < -cellPx || sy < -cellPx || sx > Screen.width || sy > viewH) continue;

                if (cell == Level.MapCell.Door) Fill(new Rect(sx + cellPx * 0.2f, sy + cellPx * 0.2f, cellPx * 0.6f, cellPx * 0.6f), HudGold);
                if (cell == Level.MapCell.LockedDoor) Fill(new Rect(sx + cellPx * 0.2f, sy + cellPx * 0.2f, cellPx * 0.6f, cellPx * 0.6f), new Color(1f, 0.2f, 0.2f));

                // Draw an edge wherever this open cell touches a wall
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + (d == 0 ? -1 : d == 1 ? 1 : 0), ny = y + (d == 2 ? -1 : d == 3 ? 1 : 0);
                    Level.MapCell n = level.AutomapCell(nx, ny);
                    if (n != Level.MapCell.Wall && n != Level.MapCell.Exit) continue;
                    Color col = n == Level.MapCell.Exit ? new Color(0.2f, 1f, 0.3f) : wallColor;
                    float w = n == Level.MapCell.Exit ? line * 3f : line;
                    switch (d)
                    {
                        case 0: Fill(new Rect(sx, sy, w, cellPx), col); break;
                        case 1: Fill(new Rect(sx + cellPx - w, sy, w, cellPx), col); break;
                        case 2: Fill(new Rect(sx, sy, cellPx, w), col); break;
                        default: Fill(new Rect(sx, sy + cellPx - w, cellPx, w), col); break;
                    }
                }
            }

        // You: a white arrow pointing where you look
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(p.transform.eulerAngles.y, new Vector2(cx, cy));
        Fill(new Rect(cx - line, cy - cellPx * 0.6f, line * 2f, cellPx * 1.2f), Color.white);
        Fill(new Rect(cx - cellPx * 0.3f, cy - cellPx * 0.3f, cellPx * 0.6f, line * 2f), Color.white);
        GUI.matrix = old;

        Shadowed(new Rect(0, viewH - 12f * s, Screen.width, 10f * s), Level.Title, subtitle);
    }

    void DrawOverlays(GameManager gm, float s, float viewH)
    {
        float sinceStart = Time.time - gm.LevelStartTime;
        if (gm.state == GameManager.State.Playing && sinceStart < 5f)
        {
            float a = Mathf.Clamp01(5f - sinceStart);
            GUI.color = new Color(1f, 1f, 1f, a);
            Shadowed(new Rect(0, viewH * 0.18f, Screen.width, 24f * s), Level.Title, title);
            Shadowed(new Rect(0, viewH * 0.18f + 22f * s, Screen.width, 10f * s),
                "WASD move   Mouse look   Click/Ctrl fire   E use   1-5 weapons   Shift run   Tab map   P pixels   R restart", subtitle);
            GUI.color = Color.white;
        }

        if (gm.state == GameManager.State.Playing && !GameInput.CursorLocked)
            Shadowed(new Rect(0, viewH * 0.62f, Screen.width, 10f * s), "Click to play  -  Esc frees the mouse", subtitle);

        if (gm.state == GameManager.State.Dead)
        {
            Fill(new Rect(0, 0, Screen.width, viewH), new Color(0.4f, 0f, 0f, Mathf.Clamp01(gm.StateTime) * 0.45f));
            Shadowed(new Rect(0, viewH * 0.35f, Screen.width, 24f * s), "YOU DIED", title);
            if (gm.StateTime > 1.2f)
                Shadowed(new Rect(0, viewH * 0.35f + 22f * s, Screen.width, 10f * s), "Press R or click to try again", subtitle);
        }

        if (gm.state == GameManager.State.Complete)
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.05f, 0.02f, 0.02f, 0.92f));
            Level level = Level.Current;
            float y = viewH * 0.12f;
            Shadowed(new Rect(0, y, Screen.width, 20f * s), Level.Title, title);
            Shadowed(new Rect(0, y + 18f * s, Screen.width, 10f * s), "FINISHED", subtitle);
            string[] rows =
            {
                "KILLS      " + Percent(gm.kills, level ? level.TotalMonsters : 0),
                "ITEMS      " + Percent(gm.items, level ? level.TotalItems : 0),
                "SECRET     " + Percent(gm.secrets, level ? level.TotalSecrets : 0),
                "TIME       " + Mathf.FloorToInt(gm.levelTime / 60f) + ":" + Mathf.FloorToInt(gm.levelTime % 60f).ToString("00"),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                if (gm.StateTime < 0.4f + i * 0.5f) break; // reveal one line at a time
                Shadowed(new Rect(0, y + (40f + i * 18f) * s, Screen.width, 16f * s), rows[i], number);
            }
            if (gm.StateTime > 2.5f)
                Shadowed(new Rect(0, y + 120f * s, Screen.width, 10f * s), "Press R or click to play again", subtitle);
        }
    }

    static string Percent(int got, int total) => (total <= 0 ? 100 : Mathf.RoundToInt(100f * got / total)) + "%";

    // ---------------------------------------------------------------------

    static void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = new Color(c.r, c.g, c.b, c.a * old.a);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static void Shadowed(Rect r, string text, GUIStyle style)
    {
        if (string.IsNullOrEmpty(text)) return;
        Color textColor = style.normal.textColor;
        float o = Mathf.Max(1f, style.fontSize / 12f);
        style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), text, style);
        style.normal.textColor = textColor;
        GUI.Label(r, text, style);
    }
}
