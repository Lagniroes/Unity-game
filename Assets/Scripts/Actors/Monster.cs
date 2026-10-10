using System.Collections.Generic;
using UnityEngine;

public enum MonsterType { Zombie, Imp, Demon, Caco }

/// <summary>
/// A demon with Doom-like behaviour: it sleeps until it sees or hears you, then chases you
/// (finding its way around walls and through doors), and attacks when it has a clear shot.
/// Getting hurt can stun it for a moment ("pain chance").
/// </summary>
public class Monster : MonoBehaviour, IDamageable
{
    class Definition
    {
        public float health, speed, radius, height, painChance, painTime, windup, attackHold;
        public bool flying, hitscan, dropsClip;
        public Projectile.Kind? projectile;
        public float meleeRange;
        public int meleeMin, meleeMax;
        public MonsterSprites sprites;
        public AudioClip sight, death, rangedSound, meleeSound;
    }

    static Definition Define(MonsterType type)
    {
        switch (type)
        {
            case MonsterType.Zombie:
                return new Definition
                {
                    health = 20f, speed = 3f, radius = 0.4f, height = 1.75f, painChance = 0.78f, painTime = 0.2f,
                    windup = 0.4f, attackHold = 0.25f, hitscan = true, dropsClip = true,
                    sprites = SpriteLibrary.Zombie, sight = Sfx.ZombieSight, death = Sfx.ZombieDeath, rangedSound = Sfx.Pistol,
                };
            case MonsterType.Imp:
                return new Definition
                {
                    health = 60f, speed = 3.2f, radius = 0.4f, height = 1.75f, painChance = 0.78f, painTime = 0.25f,
                    windup = 0.5f, attackHold = 0.3f, projectile = Projectile.Kind.ImpFireball,
                    meleeRange = 1.9f, meleeMin = 3, meleeMax = 24,
                    sprites = SpriteLibrary.Imp, sight = Sfx.ImpSight, death = Sfx.ImpDeath,
                    rangedSound = Sfx.FireballThrow, meleeSound = Sfx.Claw,
                };
            case MonsterType.Demon:
                return new Definition
                {
                    health = 150f, speed = 6f, radius = 0.6f, height = 1.55f, painChance = 0.7f, painTime = 0.3f,
                    windup = 0.35f, attackHold = 0.3f, meleeRange = 2.1f, meleeMin = 4, meleeMax = 40,
                    sprites = SpriteLibrary.Demon, sight = Sfx.DemonSight, death = Sfx.DemonDeath, meleeSound = Sfx.Bite,
                };
            default:
                return new Definition
                {
                    health = 400f, speed = 2.6f, radius = 0.75f, height = 1.5f, painChance = 0.5f, painTime = 0.35f,
                    windup = 0.6f, attackHold = 0.3f, flying = true, projectile = Projectile.Kind.CacoBall,
                    meleeRange = 2.3f, meleeMin = 10, meleeMax = 60,
                    sprites = SpriteLibrary.Caco, sight = Sfx.CacoSight, death = Sfx.CacoDeath,
                    rangedSound = Sfx.FireballThrow, meleeSound = Sfx.Bite,
                };
        }
    }

    enum State { Idle, Chase, Windup, Attack, Pain, Dying, Dead }

    public static readonly List<Monster> All = new List<Monster>();

    Definition def;
    CharacterController controller;
    Billboard billboard;
    float health;
    State state;
    float stateTime, animTime;
    float verticalSpeed;
    bool meleeAttack;

    bool seesPlayer;
    float nextSightCheck, nextAttackCheck, nextStrafe, nextActiveSound;
    float strafeAngle;
    float stuckTime, unstickUntil;
    Vector3 unstickDir;

    public bool IsDead => state == State.Dying || state == State.Dead;
    public Vector3 Center => transform.position + Vector3.up * (def.height * 0.5f + (def.flying ? 0.2f : 0f));
    Vector3 Eye => transform.position + Vector3.up * def.height * 0.85f;

    public static Monster Spawn(MonsterType type, Vector3 position)
    {
        Definition def = Define(type);
        var go = new GameObject(type.ToString());
        go.transform.SetParent(GameManager.WorldRoot, false);
        go.transform.position = position + (def.flying ? Vector3.up * 1f : Vector3.zero);

        var cc = go.AddComponent<CharacterController>();
        cc.radius = def.radius;
        cc.height = def.height;
        cc.center = new Vector3(0f, def.height * 0.5f + (def.flying ? 0.2f : 0f), 0f);
        cc.stepOffset = Level.MaxStep;
        cc.slopeLimit = 50f;
        cc.skinWidth = 0.04f;

        var monster = go.AddComponent<Monster>();
        monster.def = def;
        monster.controller = cc;
        monster.health = def.health;
        monster.billboard = Billboard.Create("Sprite", go.transform, go.transform.position, def.sprites.walk[0]);
        monster.billboard.lightHeight = 1f;
        monster.animTime = Random.value * 2f;
        return monster;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    /// <summary>Wakes up sleeping monsters near a loud noise (like your gunfire).</summary>
    public static void Alert(Vector3 position, float radius)
    {
        foreach (Monster m in All)
            if (m.state == State.Idle && Vector3.Distance(m.Center, position) < radius) m.Wake(true);
    }

    void Wake(bool sound)
    {
        if (state != State.Idle) return;
        SetState(State.Chase);
        nextAttackCheck = Time.time + Random.Range(0.4f, 1f);
        if (sound) Sfx.PlayAt(def.sight, Center);
    }

    void SetState(State s)
    {
        state = s;
        stateTime = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        stateTime += dt;
        animTime += dt;
        Player player = Player.Instance;
        bool playerAlive = player && !player.IsDead;

        if (!IsDead && playerAlive && Time.time >= nextSightCheck)
        {
            nextSightCheck = Time.time + 0.2f;
            seesPlayer = Vector3.Distance(Center, player.Center) < 40f && CanSee(player);
            if (state == State.Idle && seesPlayer) Wake(true);
        }

        switch (state)
        {
            case State.Idle:
                Move(Vector3.zero);
                billboard.Sprite = def.sprites.walk[0];
                break;

            case State.Chase:
                Chase(player, playerAlive);
                break;

            case State.Windup:
                Move(Vector3.zero);
                billboard.Sprite = def.sprites.windup;
                if (stateTime >= def.windup)
                {
                    PerformAttack(player);
                    SetState(State.Attack);
                }
                break;

            case State.Attack:
                Move(Vector3.zero);
                billboard.Sprite = def.sprites.attack;
                if (stateTime >= def.attackHold) SetState(State.Chase);
                break;

            case State.Pain:
                Move(Vector3.zero);
                billboard.Sprite = def.sprites.pain;
                if (stateTime >= def.painTime) SetState(State.Chase);
                break;

            case State.Dying:
            case State.Dead:
                UpdateDeath(dt);
                break;
        }
    }

    void Chase(Player player, bool playerAlive)
    {
        if (!playerAlive)
        {
            Move(Vector3.zero);
            billboard.Sprite = def.sprites.walk[0];
            return;
        }

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        // Attack?
        bool meleeReady = def.meleeRange > 0f && distance < def.meleeRange + 0.3f && seesPlayer;
        if (meleeReady && (def.projectile == null && !def.hitscan || Time.time >= nextAttackCheck))
        {
            StartAttack(true);
            return;
        }
        if (Time.time >= nextAttackCheck)
        {
            nextAttackCheck = Time.time + Random.Range(0.5f, 1.3f);
            bool ranged = def.hitscan || def.projectile != null;
            float chance = Mathf.Lerp(0.85f, 0.15f, distance / 30f);
            if (ranged && seesPlayer && Random.value < chance)
            {
                StartAttack(false);
                return;
            }
        }

        // Move: straight at you when it can see you, otherwise follow the level's distance map
        Vector3 dir = Vector3.zero;
        if (Time.time < unstickUntil) dir = unstickDir;
        else if (seesPlayer)
        {
            if (Time.time >= nextStrafe)
            {
                nextStrafe = Time.time + Random.Range(0.5f, 1.4f);
                strafeAngle = Random.Range(-45f, 45f);
            }
            float keepAway = def.meleeRange > 0f && def.projectile == null && !def.hitscan ? 0.8f : 2.5f;
            if (distance > keepAway) dir = Quaternion.Euler(0f, distance > 4f ? strafeAngle : 0f, 0f) * toPlayer.normalized;
        }
        else if (Level.Current && Level.Current.NextStep(transform.position, out Vector3 waypoint, out Door door))
        {
            if (door && Vector3.Distance(door.transform.position, transform.position) < Level.Cell * 1.2f) door.Use(false);
            Vector3 w = waypoint - transform.position;
            w.y = 0f;
            dir = w.normalized;
        }

        Vector3 before = transform.position;
        Move(dir * def.speed);

        // Stuck on a corner or another monster? Sidestep for a moment.
        if (dir != Vector3.zero)
        {
            float moved = new Vector2(transform.position.x - before.x, transform.position.z - before.z).magnitude;
            stuckTime = moved < def.speed * Time.deltaTime * 0.25f ? stuckTime + Time.deltaTime : 0f;
            if (stuckTime > 0.4f)
            {
                stuckTime = 0f;
                unstickUntil = Time.time + Random.Range(0.4f, 0.9f);
                unstickDir = Quaternion.Euler(0f, Random.value < 0.5f ? 90f : -90f, 0f) * dir;
            }
        }

        bool walking = dir != Vector3.zero;
        billboard.Sprite = def.sprites.walk[walking ? Mathf.FloorToInt(animTime * 4f) % 2 : 0];

        if (Time.time >= nextActiveSound)
        {
            if (nextActiveSound > 0f && Random.value < 0.3f) Sfx.PlayAt(Sfx.MonsterActive, Center, 0.5f);
            nextActiveSound = Time.time + Random.Range(5f, 12f);
        }
    }

    void StartAttack(bool melee)
    {
        meleeAttack = melee;
        SetState(State.Windup);
        nextAttackCheck = Time.time + Random.Range(0.8f, 1.6f);
    }

    void PerformAttack(Player player)
    {
        if (!player || player.IsDead) return;
        Vector3 toPlayer = player.Center - Center;
        float distance = new Vector2(toPlayer.x, toPlayer.z).magnitude;

        if (meleeAttack)
        {
            if (def.meleeSound) Sfx.PlayAt(def.meleeSound, Center);
            if (distance < def.meleeRange + 0.6f && CanSee(player))
                player.TakeDamage(Random.Range(def.meleeMin, def.meleeMax + 1), Center);
            return;
        }

        if (def.hitscan)
        {
            Sfx.PlayAt(def.rangedSound, Center);
            Lighting.Create("Muzzle Flash", transform, Eye, new Color(1f, 0.8f, 0.4f), 6f, 2f).gameObject.AddComponent<SelfDestruct>().life = 0.06f;
            float hitChance = Mathf.Clamp(0.9f - distance / 35f, 0.3f, 0.85f);
            if (CanSee(player) && Random.value < hitChance)
                player.TakeDamage(Random.Range(1, 6) * 3f, Center);
            else
            {
                // Missed: the bullet hits a wall somewhere near you
                Vector3 dir = Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(-8f, 8f), 0f) * toPlayer.normalized;
                if (Physics.Raycast(Eye, dir, out RaycastHit hit, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && hit.distance > distance)
                    Effects.Puff(hit.point + hit.normal * 0.05f - Vector3.up * 0.2f);
            }
            return;
        }

        if (def.projectile.HasValue)
        {
            Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z).normalized;
            Vector3 spawn = Center + flat * (def.radius + 0.35f) + Vector3.up * 0.25f;
            Vector3 aim = player.Center + Vector3.up * 0.2f - spawn;
            Projectile.Spawn(def.projectile.Value, spawn, aim, controller);
            if (def.rangedSound) Sfx.PlayAt(def.rangedSound, Center);
        }
    }

    void Move(Vector3 horizontalVelocity)
    {
        if (!controller.enabled) return;
        float dt = Time.deltaTime;
        if (def.flying)
        {
            Player player = Player.Instance;
            Level level = Level.Current;
            float y = transform.position.y;
            float targetY = player ? player.transform.position.y + 0.5f : y;
            if (level)
            {
                targetY = Mathf.Max(targetY, level.FloorAt(transform.position) + 0.3f);
                targetY = Mathf.Min(targetY, level.CeilingAt(transform.position) - def.height - 0.4f);
            }
            verticalSpeed = Mathf.Clamp((targetY - y) * 2f, -2.5f, 2.5f);
            if (state == State.Idle) verticalSpeed = 0f;
        }
        else
        {
            verticalSpeed = controller.isGrounded ? -2f : verticalSpeed - 25f * dt;
        }
        controller.Move((horizontalVelocity + Vector3.up * verticalSpeed) * dt);
    }

    bool CanSee(Player player) =>
        !Physics.Linecast(Eye, player.EyePosition, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

    public void TakeDamage(float amount, Vector3 from)
    {
        if (IsDead) return;
        health -= amount;
        Vector3 toward = from - Center;
        toward.y = 0f;
        Effects.Blood(Center + toward.normalized * def.radius + Random.insideUnitSphere * 0.25f);

        if (state == State.Idle) Wake(true);
        if (health <= 0f)
        {
            Die();
            return;
        }
        if (state != State.Pain && Random.value < def.painChance)
        {
            SetState(State.Pain);
            Sfx.PlayAt(Sfx.MonsterPain, Center, 0.8f);
        }
    }

    void Die()
    {
        SetState(State.Dying);
        controller.enabled = false; // corpses don't block bullets or movement
        Sfx.PlayAt(def.death, Center);
        GameManager.Instance.kills++;
        if (def.dropsClip && Level.Current)
            Pickup.Spawn(Pickup.Kind.Clip, transform.position + Vector3.right * 0.3f, true);
    }

    void UpdateDeath(float dt)
    {
        // Flying monsters drop to the floor
        if (def.flying && Level.Current)
        {
            float floor = Level.Current.FloorAt(transform.position);
            Vector3 p = transform.position;
            verticalSpeed -= 20f * dt;
            p.y = Mathf.Max(floor, p.y + verticalSpeed * dt);
            transform.position = p;
        }

        Sprite[] frames = def.sprites.death;
        int frame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(stateTime / 0.13f));
        billboard.Sprite = frames[frame];
        if (state == State.Dying && frame == frames.Length - 1) state = State.Dead;
    }
}

/// <summary>Removes its GameObject after a short time (used for muzzle flash lights).</summary>
public class SelfDestruct : MonoBehaviour
{
    public float life = 0.1f;

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);
    }
}
