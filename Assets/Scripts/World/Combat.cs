using System.Collections.Generic;
using UnityEngine;

/// <summary>Bullet puffs, blood, explosions and splash damage.</summary>
public static class Effects
{
    static readonly List<IDamageable> targets = new List<IDamageable>();

    public static void Puff(Vector3 position) =>
        SpriteEffect.Spawn(SpriteLibrary.Puff, position, 0.07f, true).WithDrift(Vector3.up * 0.6f);

    public static void Blood(Vector3 position) =>
        SpriteEffect.Spawn(SpriteLibrary.Blood, position, 0.08f, false).WithDrift(Vector3.down * 1.2f);

    /// <summary>A rocket or barrel blast: big fireball, light, sound and splash damage.</summary>
    public static void Explosion(Vector3 position, float maxDamage, float radius)
    {
        SpriteEffect.Spawn(SpriteLibrary.Explosion, position, 0.09f, true,
            new Color(1f, 0.6f, 0.25f), 12f, 4f, 1.6f);
        Sfx.PlayAt(Sfx.Explosion, position, 1f);
        GameManager.Instance.Shake(Player.Instance ? Mathf.Clamp01(1.4f - Vector3.Distance(position, Player.Instance.Center) / 10f) : 0f);
        Splash(position, maxDamage, radius);
    }

    /// <summary>Damage that falls off with distance and is blocked by walls (like Doom's radius attack).</summary>
    public static void Splash(Vector3 position, float maxDamage, float radius)
    {
        targets.Clear();
        foreach (Monster m in Monster.All) targets.Add(m);
        foreach (Barrel b in Barrel.All) targets.Add(b);
        if (Player.Instance) targets.Add(Player.Instance);

        foreach (IDamageable target in targets)
        {
            if (target == null || target.IsDead) continue;
            Vector3 center = target.Center;
            float distance = Vector3.Distance(position, center) - 0.4f;
            if (distance >= radius) continue;
            var targetCollider = ((Component)target).GetComponent<Collider>();
            if (Physics.Linecast(position, center, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.collider != targetCollider) continue; // a wall is in the way
            float damage = maxDamage * (1f - Mathf.Max(0f, distance) / radius);
            target.TakeDamage(damage, position);
        }
    }
}

/// <summary>Fireballs and rockets: flying sprites with a light that hurt whatever they touch.</summary>
public class Projectile : MonoBehaviour
{
    public enum Kind { ImpFireball, CacoBall, Rocket }

    Kind kind;
    Vector3 velocity;
    Collider owner;
    Billboard billboard;
    Sprite[] frames;
    float age;
    readonly RaycastHit[] hits = new RaycastHit[8];

    const float Radius = 0.2f;

    public static Projectile Spawn(Kind kind, Vector3 position, Vector3 direction, Collider owner)
    {
        Sprite[] frames = kind == Kind.ImpFireball ? SpriteLibrary.Fireball
            : kind == Kind.CacoBall ? SpriteLibrary.CacoBall : SpriteLibrary.Rocket;
        Billboard bb = Billboard.Create(kind.ToString(), GameManager.WorldRoot, position, frames[0], true);
        bb.lightHeight = 0f;
        var p = bb.gameObject.AddComponent<Projectile>();
        p.kind = kind;
        p.frames = frames;
        p.billboard = bb;
        p.owner = owner;
        float speed = kind == Kind.Rocket ? 22f : kind == Kind.ImpFireball ? 12f : 11f;
        p.velocity = direction.normalized * speed;
        Color glow = kind == Kind.CacoBall ? new Color(1f, 0.3f, 1f) : new Color(1f, 0.55f, 0.2f);
        Lighting.Create("Glow", bb.transform, position, glow, 6f, 1.6f);
        return p;
    }

    void Update()
    {
        age += Time.deltaTime;
        billboard.Sprite = frames[Mathf.FloorToInt(age * 10f) % frames.Length];
        if (age > 8f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 step = velocity * Time.deltaTime;
        int count = Physics.SphereCastNonAlloc(transform.position, Radius, step.normalized, hits, step.magnitude,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        int bestIndex = -1;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider == owner) continue;
            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                bestIndex = i;
            }
        }
        if (bestIndex < 0)
        {
            transform.position += step;
            return;
        }

        RaycastHit hit = hits[bestIndex];
        Vector3 impact = hit.distance > 0f ? transform.position + step.normalized * hit.distance : transform.position;
        Explode(impact, hit.collider);
    }

    void Explode(Vector3 impact, Collider hitCollider)
    {
        var target = hitCollider.GetComponent<IDamageable>();
        switch (kind)
        {
            case Kind.Rocket:
                target?.TakeDamage(Random.Range(1, 9) * 20f, impact);
                Effects.Explosion(impact - velocity.normalized * 0.3f, 128f, 4f);
                break;
            case Kind.ImpFireball:
                target?.TakeDamage(Random.Range(1, 9) * 3f, impact);
                Burst(impact);
                break;
            case Kind.CacoBall:
                target?.TakeDamage(Random.Range(1, 9) * 5f, impact);
                Burst(impact);
                break;
        }
        Destroy(gameObject);
    }

    void Burst(Vector3 impact)
    {
        SpriteEffect.Spawn(SpriteLibrary.BallBurst, impact - velocity.normalized * 0.2f, 0.08f, true,
            kind == Kind.CacoBall ? new Color(1f, 0.3f, 1f) : new Color(1f, 0.55f, 0.2f), 6f, 2f);
        Sfx.PlayAt(Sfx.FireballHit, impact, 0.7f);
    }
}

/// <summary>A barrel of toxic waste. Shoot it and it blows up, hurting everything nearby.</summary>
public class Barrel : MonoBehaviour, IDamageable
{
    public static readonly List<Barrel> All = new List<Barrel>();

    float health = 20f;
    float explodeAt = -1f;
    Billboard billboard;

    public Vector3 Center => transform.position + Vector3.up * 0.5f;
    public bool IsDead => explodeAt >= 0f;

    public static Barrel Spawn(Vector3 position)
    {
        Billboard bb = Billboard.Create("Barrel", GameManager.WorldRoot, position, SpriteLibrary.Barrel[0]);
        var col = bb.gameObject.AddComponent<CapsuleCollider>();
        col.radius = 0.38f;
        col.height = 1.05f;
        col.center = new Vector3(0f, 0.53f, 0f);
        var barrel = bb.gameObject.AddComponent<Barrel>();
        barrel.billboard = bb;
        return barrel;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public void TakeDamage(float amount, Vector3 from)
    {
        if (IsDead) return;
        health -= amount;
        if (health > 0f) return;
        explodeAt = Time.time + 0.15f; // a short delay makes chain reactions ripple nicely
        billboard.Sprite = SpriteLibrary.Barrel[1];
        billboard.fullBright = true;
    }

    void Update()
    {
        if (explodeAt < 0f || Time.time < explodeAt) return;
        GetComponent<Collider>().enabled = false;
        enabled = false;
        Effects.Explosion(Center, 128f, 4f);
        Destroy(gameObject);
    }
}
