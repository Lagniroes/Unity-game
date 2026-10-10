using UnityEngine;

public enum WeaponId { Fist, Pistol, Shotgun, Chaingun, Launcher }
public enum AmmoType { None = -1, Bullets, Shells, Rockets }

/// <summary>
/// Your arsenal: fist, pistol, shotgun, chaingun and rocket launcher. Handles switching
/// (lower / raise), firing with Doom-like damage and spread, vertical auto-aim, ammo,
/// the muzzle flash light, and which first-person sprite the HUD should draw.
/// </summary>
public class WeaponSystem : MonoBehaviour
{
    public static readonly int[] MaxAmmo = { 200, 50, 50 };
    public static readonly string[] Names = { "Fist", "Pistol", "Shotgun", "Chaingun", "Rocket Launcher" };

    public readonly int[] ammo = { 50, 0, 0 };
    public readonly bool[] owned = { true, true, false, false, false };

    public WeaponId Current { get; private set; } = WeaponId.Pistol;

    enum Phase { Ready, Firing, Lowering, Raising }

    Phase phase = Phase.Raising;
    WeaponId pending;
    float raise = 1f;      // 0 = weapon up, 1 = lowered off screen
    float fireTime;
    int burst;             // shots in a row while holding fire (first shot is always accurate)
    bool pumped;
    Light flashLight;
    float flashUntil;
    Player player;

    public static AmmoType AmmoOf(WeaponId w)
    {
        switch (w)
        {
            case WeaponId.Pistol:
            case WeaponId.Chaingun: return AmmoType.Bullets;
            case WeaponId.Shotgun: return AmmoType.Shells;
            case WeaponId.Launcher: return AmmoType.Rockets;
            default: return AmmoType.None;
        }
    }

    static float Cooldown(WeaponId w)
    {
        switch (w)
        {
            case WeaponId.Fist: return 0.45f;
            case WeaponId.Pistol: return 0.4f;
            case WeaponId.Shotgun: return 1.05f;
            case WeaponId.Chaingun: return 0.115f;
            default: return 0.7f;
        }
    }

    /// <summary>Ammo for the current weapon, or -1 for the fist.</summary>
    public int CurrentAmmo => AmmoOf(Current) == AmmoType.None ? -1 : ammo[(int)AmmoOf(Current)];

    bool HasAmmo(WeaponId w) => AmmoOf(w) == AmmoType.None || ammo[(int)AmmoOf(w)] > 0;

    void Start()
    {
        player = GetComponent<Player>();
        flashLight = Lighting.Create("Muzzle Flash", player.Head, player.Head.position + player.Head.forward * 0.6f,
            new Color(1f, 0.85f, 0.55f), 10f, 2.5f);
        flashLight.enabled = false;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        flashLight.enabled = Time.time < flashUntil;

        if (player.IsDead)
        {
            raise = Mathf.MoveTowards(raise, 1f, dt * 2f);
            return;
        }
        if (GameManager.Instance.state != GameManager.State.Playing) return;

        int slot = GameInput.WeaponSlotPressed;
        if (slot > 0) Select((WeaponId)(slot - 1));
        int scroll = GameInput.WeaponScroll;
        if (scroll != 0) Cycle(scroll);

        switch (phase)
        {
            case Phase.Lowering:
                raise += dt * 6f;
                if (raise >= 1f)
                {
                    raise = 1f;
                    Current = pending;
                    phase = Phase.Raising;
                }
                break;
            case Phase.Raising:
                raise -= dt * 6f;
                if (raise <= 0f)
                {
                    raise = 0f;
                    phase = Phase.Ready;
                }
                break;
            case Phase.Ready:
                if (GameInput.FireHeld) Fire();
                else burst = 0;
                break;
            case Phase.Firing:
                fireTime += dt;
                if (Current == WeaponId.Shotgun && !pumped && fireTime > 0.35f)
                {
                    pumped = true;
                    Sfx.Play2D(Sfx.ShotgunPump, 0.6f);
                }
                if (fireTime >= Cooldown(Current)) phase = Phase.Ready;
                break;
        }
    }

    void Select(WeaponId w)
    {
        if (!owned[(int)w] || !HasAmmo(w)) return;
        if (phase == Phase.Lowering ? pending == w : Current == w) return;
        pending = w;
        phase = Phase.Lowering;
    }

    void Cycle(int direction)
    {
        int start = (int)(phase == Phase.Lowering ? pending : Current);
        for (int i = 1; i <= 5; i++)
        {
            var w = (WeaponId)(((start + direction * i) % 5 + 5) % 5);
            if (owned[(int)w] && HasAmmo(w))
            {
                Select(w);
                return;
            }
        }
    }

    void SwitchToBest()
    {
        WeaponId[] preference = { WeaponId.Chaingun, WeaponId.Shotgun, WeaponId.Pistol, WeaponId.Launcher, WeaponId.Fist };
        foreach (WeaponId w in preference)
            if (owned[(int)w] && HasAmmo(w))
            {
                if (w != Current) Select(w);
                return;
            }
    }

    public bool GiveAmmo(AmmoType type, int amount)
    {
        int i = (int)type;
        if (ammo[i] >= MaxAmmo[i]) return false;
        bool wasEmpty = CurrentAmmo == 0;
        ammo[i] = Mathf.Min(MaxAmmo[i], ammo[i] + amount);
        if (wasEmpty) SwitchToBest();
        return true;
    }

    public void GiveWeapon(WeaponId weapon, AmmoType type, int amount)
    {
        bool isNew = !owned[(int)weapon];
        owned[(int)weapon] = true;
        ammo[(int)type] = Mathf.Min(MaxAmmo[(int)type], ammo[(int)type] + amount);
        if (isNew) Select(weapon);
    }

    // ---------------------------------------------------------------------
    // Firing
    // ---------------------------------------------------------------------

    void Fire()
    {
        AmmoType type = AmmoOf(Current);
        if (type != AmmoType.None && ammo[(int)type] <= 0)
        {
            SwitchToBest();
            return;
        }
        if (type != AmmoType.None) ammo[(int)type]--;

        phase = Phase.Firing;
        fireTime = 0f;
        pumped = false;
        burst++;

        Vector3 eye = player.EyePosition;
        Vector3 forward = player.Head.forward;
        Vector3 aim = AutoAim(eye, forward);
        switch (Current)
        {
            case WeaponId.Fist:
                Sfx.Play2D(Sfx.Punch, 0.6f);
                if (Physics.Raycast(eye, aim, out RaycastHit hit, 2.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    var target = hit.collider.GetComponent<IDamageable>();
                    if (target != null) target.TakeDamage(Random.Range(1, 11) * 2f, eye);
                    Sfx.Play2D(Sfx.PunchHit, 0.8f);
                }
                return; // punching is quiet
            case WeaponId.Pistol:
                Sfx.Play2D(Sfx.Pistol, 0.8f);
                Hitscan(eye, aim, burst > 1 ? 3f : 0f, 0f, Random.Range(1, 4) * 5f);
                break;
            case WeaponId.Shotgun:
                Sfx.Play2D(Sfx.Shotgun);
                for (int i = 0; i < 7; i++) Hitscan(eye, aim, 5.6f, 1.5f, Random.Range(1, 4) * 5f);
                break;
            case WeaponId.Chaingun:
                Sfx.Play2D(Sfx.Chaingun, 0.75f);
                Hitscan(eye, aim, burst > 1 ? 3f : 0f, 0f, Random.Range(1, 4) * 5f);
                break;
            case WeaponId.Launcher:
                Sfx.Play2D(Sfx.RocketLaunch);
                Projectile.Spawn(Projectile.Kind.Rocket, eye + aim * 0.5f - Vector3.up * 0.15f, aim, GetComponent<Collider>());
                break;
        }
        flashUntil = Time.time + 0.07f;
        Monster.Alert(player.Center, 25f);
    }

    static void Hitscan(Vector3 eye, Vector3 dir, float spreadH, float spreadV, float damage)
    {
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 d = Quaternion.AngleAxis(Random.Range(-spreadV, spreadV), right) *
                    (Quaternion.AngleAxis(Random.Range(-spreadH, spreadH), Vector3.up) * dir);
        if (!Physics.Raycast(eye, d, out RaycastHit hit, 150f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;

        var target = hit.collider.GetComponent<IDamageable>();
        if (target != null && !target.IsDead)
        {
            target.TakeDamage(damage, eye);
            if (target is Monster) return; // monsters bleed instead of puffing
        }
        Effects.Puff(hit.point + hit.normal * 0.1f);
    }

    /// <summary>Doom-style auto-aim: if a monster is roughly in front of you, aim up or down at it.</summary>
    static Vector3 AutoAim(Vector3 eye, Vector3 forward)
    {
        var flatForward = new Vector3(forward.x, 0f, forward.z);
        if (flatForward.sqrMagnitude < 0.0001f) return forward;
        flatForward.Normalize();

        Monster best = null;
        float bestDistance = float.MaxValue;
        Vector3 bestTo = Vector3.zero;
        foreach (Monster m in Monster.All)
        {
            if (m.IsDead) continue;
            Vector3 to = m.Center - eye;
            var flatTo = new Vector3(to.x, 0f, to.z);
            float distance = flatTo.magnitude;
            if (distance < 0.1f || distance > 60f || distance >= bestDistance) continue;
            if (Vector3.Angle(flatForward, flatTo) > 4f) continue;
            var col = m.GetComponent<Collider>();
            if (Physics.Linecast(eye, m.Center, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.collider != col) continue;
            best = m;
            bestDistance = distance;
            bestTo = to;
        }
        if (!best) return forward;
        return new Vector3(flatForward.x * bestDistance, bestTo.y, flatForward.z * bestDistance).normalized;
    }

    // ---------------------------------------------------------------------
    // What the HUD draws
    // ---------------------------------------------------------------------

    /// <summary>The first-person weapon sprite, its muzzle flash (or null) and its offset in "Doom pixels".</summary>
    public void GetView(out Texture2D sprite, out Texture2D flash, out Vector2 offset)
    {
        flash = null;
        float t = phase == Phase.Firing ? fireTime : 99f;
        switch (Current)
        {
            case WeaponId.Fist:
                sprite = SpriteLibrary.Fist[t < 0.1f ? 1 : t < 0.25f ? 2 : t < 0.35f ? 1 : 0];
                break;
            case WeaponId.Pistol:
                sprite = SpriteLibrary.Pistol[t < 0.08f ? 1 : t < 0.2f ? 2 : 0];
                if (t < 0.07f) flash = SpriteLibrary.SmallFlash;
                break;
            case WeaponId.Shotgun:
                sprite = SpriteLibrary.Shotgun[t < 0.1f ? 1 : t < 0.3f ? 0 : t < 0.5f ? 2 : t < 0.7f ? 3 : t < 0.85f ? 2 : 0];
                if (t < 0.08f) flash = SpriteLibrary.BigFlash;
                break;
            case WeaponId.Chaingun:
                sprite = SpriteLibrary.Chaingun[t < 0.12f ? 1 + burst % 2 : 0];
                if (t < 0.06f) flash = SpriteLibrary.SmallFlash;
                break;
            default:
                sprite = SpriteLibrary.Launcher[t < 0.15f ? 1 : 0];
                if (t < 0.1f) flash = SpriteLibrary.BigFlash;
                break;
        }

        float bob = player ? player.BobAmount : 0f;
        float phaseBob = player ? player.BobPhase : 0f;
        bool steady = phase == Phase.Firing;
        offset = new Vector2(
            steady ? 0f : Mathf.Cos(phaseBob) * 9f * bob,
            (steady ? 0f : Mathf.Abs(Mathf.Sin(phaseBob)) * 6f * bob) + raise * 75f);
    }
}
