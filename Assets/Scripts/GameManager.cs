using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fight state, hit-stop / slow motion, camera shake on hits, floating damage numbers,
/// health bars, and the win / lose screens. Press R to restart.
/// </summary>
public class GameManager : MonoBehaviour
{
    struct DamagePopup
    {
        public Vector3 position;
        public string text;
        public Color color;
        public float startTime;
    }

    public static GameManager Instance { get; private set; }

    public Combatant player;
    public Combatant rival;

    readonly List<DamagePopup> popups = new List<DamagePopup>();
    float slowUntil;
    float slowScale = 1f;
    float gameOverTime;
    GUIStyle labelStyle;
    GUIStyle bigStyle;
    GUIStyle smallStyle;
    GUIStyle popupStyle;

    public bool IsGameOver => (player && player.IsDead) || (rival && rival.IsDead);

    void Awake() => Instance = this;

    void OnDestroy() => Time.timeScale = 1f;

    public void Restart()
    {
        Time.timeScale = 1f;
        slowUntil = 0f;
        popups.Clear();
        GameBootstrap.SpawnFighters(this);
    }

    /// <summary>Freezes the game for a moment so hits feel heavy.</summary>
    public void HitStop(float duration)
    {
        if (!IsGameOver) SlowMotion(duration, 0.05f); // the knockout slow-mo takes over on the final blow
    }

    public void SlowMotion(float duration, float scale)
    {
        if (duration <= 0f) return;
        slowUntil = Mathf.Max(slowUntil, Time.unscaledTime + duration);
        slowScale = Mathf.Min(scale, Time.timeScale < 1f ? slowScale : 1f);
        Time.timeScale = slowScale;
    }

    public void OnHit(Combatant target, float damage)
    {
        popups.Add(new DamagePopup
        {
            position = target.transform.position + Vector3.up * 1.9f + Random.insideUnitSphere * 0.3f,
            text = Mathf.RoundToInt(damage).ToString(),
            color = target == player ? new Color(1f, 0.3f, 0.3f) : Color.white,
            startTime = Time.unscaledTime,
        });

        if (ThirdPersonCamera.Instance)
            ThirdPersonCamera.Instance.Shake(Mathf.Clamp(damage / 25f, 0.25f, 1f) * (target == player ? 1f : 0.7f));
    }

    public void OnCombatantDied(Combatant who)
    {
        gameOverTime = Time.unscaledTime;
        SlowMotion(1.2f, 0.25f); // dramatic slow-mo on the knockout
    }

    void Update()
    {
        if (Time.timeScale < 1f && Time.unscaledTime >= slowUntil) Time.timeScale = 1f;
        if (GameInput.RestartPressed) Restart();
        popups.RemoveAll(p => Time.unscaledTime - p.startTime > 0.9f);
    }

    void OnGUI()
    {
        if (labelStyle == null) CreateStyles();

        if (player) DrawBar(new Rect(20, 20, 300, 28), player, new Color(0.3f, 0.85f, 0.35f), TextAnchor.MiddleLeft);
        if (rival && !rival.IsDead)
        {
            float width = Mathf.Min(500f, Screen.width - 40f);
            DrawBar(new Rect((Screen.width - width) / 2f, 60, width, 22), rival, new Color(0.85f, 0.2f, 0.2f), TextAnchor.MiddleCenter);
            GUI.Label(new Rect(0, 28, Screen.width, 30), rival.displayName.ToUpperInvariant(), new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter });
        }

        DrawPopups();

        GUI.Label(new Rect(20, Screen.height - 40, 1200, 30),
            "WASD move · Mouse look · Shift sprint · Space jump · Left click/J light combo · Right click/K heavy · Ctrl/Q dodge · R restart · Esc cursor",
            smallStyle);

        if (!IsGameOver || Time.unscaledTime - gameOverTime < 0.8f) return;
        var center = new Rect(0, Screen.height / 2f - 90, Screen.width, 120);
        var hint = new Rect(0, Screen.height / 2f + 30, Screen.width, 40);
        if (player && player.IsDead)
        {
            bigStyle.normal.textColor = new Color(0.85f, 0.1f, 0.1f);
            GUI.Label(center, "WASTED", bigStyle);
        }
        else
        {
            bigStyle.normal.textColor = new Color(1f, 0.8f, 0.2f);
            GUI.Label(center, "RIVAL DOWN", bigStyle);
        }
        GUI.Label(hint, "Press R to fight again", new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter });
    }

    void CreateStyles()
    {
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        labelStyle.normal.textColor = Color.white;
        bigStyle = new GUIStyle(labelStyle) { fontSize = 90, alignment = TextAnchor.MiddleCenter };
        smallStyle = new GUIStyle(labelStyle) { fontSize = 14 };
        popupStyle = new GUIStyle(labelStyle) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
    }

    void DrawBar(Rect rect, Combatant who, Color fillColor, TextAnchor alignment)
    {
        float fraction = who.Health / who.maxHealth;
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(rect.x + 3, rect.y + 3, (rect.width - 6) * fraction, rect.height - 6), Texture2D.whiteTexture);
        GUI.color = previous;

        string text = who == player ? $"  {who.displayName}  {Mathf.CeilToInt(who.Health)} / {who.maxHealth:0}" : $"{Mathf.CeilToInt(who.Health)}";
        GUI.Label(rect, text, new GUIStyle(smallStyle) { alignment = alignment });
    }

    void DrawPopups()
    {
        Camera cam = Camera.main;
        if (!cam) return;

        Color previous = GUI.color;
        foreach (DamagePopup popup in popups)
        {
            float age = Time.unscaledTime - popup.startTime;
            Vector3 screen = cam.WorldToScreenPoint(popup.position + Vector3.up * age * 1.2f);
            if (screen.z < 0f) continue;

            GUI.color = new Color(popup.color.r, popup.color.g, popup.color.b, 1f - age / 0.9f);
            GUI.Label(new Rect(screen.x - 50, Screen.height - screen.y - 20, 100, 40), popup.text, popupStyle);
        }
        GUI.color = previous;
    }
}
