using UnityEngine;

/// <summary>
/// Player hit points, short invulnerability (with blinking) after being hit, and knockback.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public float invulnerableTime = 1f;
    public float knockbackForce = 8f;

    public int Current { get; private set; }
    public bool IsDead => Current <= 0;

    PlayerController controller;
    Renderer[] renderers;
    float invulnerableUntil;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        Current = maxHealth;
    }

    public void ResetHealth()
    {
        Current = maxHealth;
        invulnerableUntil = 0f;
        SetVisible(true);
    }

    public void Heal(int amount)
    {
        if (!IsDead) Current = Mathf.Min(maxHealth, Current + amount);
    }

    public void TakeDamage(int amount, Vector3 source)
    {
        if (IsDead || Time.time < invulnerableUntil) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        Current = Mathf.Max(0, Current - amount);
        invulnerableUntil = Time.time + invulnerableTime;

        Vector3 away = transform.position - source;
        away.y = 0f;
        controller.AddKnockback(away.normalized * knockbackForce, 5f);
    }

    void Update()
    {
        bool blinking = !IsDead && Time.time < invulnerableUntil;
        SetVisible(!blinking || Mathf.Repeat(Time.time * 10f, 1f) > 0.5f);
    }

    void SetVisible(bool visible)
    {
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            if (r) r.enabled = visible;
    }
}
