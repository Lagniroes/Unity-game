using UnityEngine;

/// <summary>
/// You: fast Doom-style movement, mouse look, health and armor, using doors and switches,
/// toxic slime damage, view bob and the death camera.
/// </summary>
public class Player : MonoBehaviour, IDamageable
{
    public static Player Instance { get; private set; }

    public const float EyeHeight = 1.5f;
    const float Sensitivity = 2f;
    const float WalkSpeed = 7f;
    const float RunSpeed = 11f;
    const float UseRange = 2.7f;

    public int health = 100;
    public int armor;
    public bool hasRedKey;

    public WeaponSystem Weapons { get; private set; }
    public Transform Head { get; private set; }
    public float BobPhase { get; private set; }
    public float BobAmount { get; private set; }
    public float DamageFlash { get; private set; }
    public float PickupFlash { get; private set; }
    public float LastHurtTime { get; private set; } = -10f;
    public float GrinUntil { get; private set; }

    public bool IsDead => health <= 0;
    public Vector3 Center => transform.position + Vector3.up * 0.9f;
    public Vector3 EyePosition => Head.position;

    CharacterController controller;
    float yaw, pitch;
    Vector3 velocity;
    float verticalSpeed;
    float viewY;
    float slimeTimer;
    float lastPainSound;

    public static Player Create(Vector3 position, float yaw, Camera cam)
    {
        var go = new GameObject("Player") { layer = 2 }; // "Ignore Raycast": your own shots and the monsters' line of sight pass through you
        go.transform.SetParent(GameManager.WorldRoot, false);
        go.transform.SetPositionAndRotation(position + Vector3.up * 0.05f, Quaternion.Euler(0f, yaw, 0f));

        var cc = go.AddComponent<CharacterController>();
        cc.height = 1.7f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.85f, 0f);
        cc.stepOffset = Level.MaxStep;
        cc.slopeLimit = 50f;
        cc.skinWidth = 0.04f;

        var head = new GameObject("Head").transform;
        head.SetParent(go.transform, false);
        head.localPosition = new Vector3(0f, EyeHeight, 0f);
        cam.transform.SetParent(head, false);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;

        var player = go.AddComponent<Player>();
        player.controller = cc;
        player.Head = head;
        player.yaw = yaw;
        player.viewY = go.transform.position.y;
        player.Weapons = go.AddComponent<WeaponSystem>();
        return player;
    }

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        DamageFlash = Mathf.MoveTowards(DamageFlash, 0f, dt * 1.2f);
        PickupFlash = Mathf.MoveTowards(PickupFlash, 0f, dt * 1.5f);

        if (IsDead)
        {
            Gravity(dt);
            controller.Move(Vector3.up * verticalSpeed * dt);
            // Slump to the floor
            Vector3 hp = Head.localPosition;
            hp.y = Mathf.MoveTowards(hp.y, 0.3f, dt * 2.5f);
            Head.localPosition = hp;
            Head.localRotation = Quaternion.RotateTowards(Head.localRotation, Quaternion.Euler(-10f, 0f, 25f), dt * 60f);
            return;
        }

        bool playing = GameManager.Instance.state == GameManager.State.Playing;
        if (playing && GameInput.CursorLocked)
        {
            Vector2 look = GameInput.Look * Sensitivity;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -75f, 75f);
        }
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Vector2 shake = GameManager.Instance.ShakeOffset;
        Head.localRotation = Quaternion.Euler(pitch + shake.x, shake.y, 0f);

        // Movement with a little momentum
        Vector2 input = playing ? GameInput.Move : Vector2.zero;
        float speed = GameInput.Run ? RunSpeed : WalkSpeed;
        Vector3 wish = (transform.forward * input.y + transform.right * input.x) * speed;
        velocity = Vector3.MoveTowards(velocity, wish, (wish.sqrMagnitude > 0.01f ? 70f : 45f) * dt);
        Gravity(dt);
        controller.Move((velocity + Vector3.up * verticalSpeed) * dt);

        // View bob and smooth stepping up stairs
        Vector3 v = controller.velocity;
        float horizontalSpeed = new Vector2(v.x, v.z).magnitude;
        BobAmount = Mathf.MoveTowards(BobAmount, controller.isGrounded ? Mathf.Clamp01(horizontalSpeed / RunSpeed) : 0f, dt * 4f);
        BobPhase += dt * (5f + horizontalSpeed * 0.55f);
        float y = transform.position.y;
        viewY = y > viewY ? Mathf.MoveTowards(viewY, y, dt * 5f) : y;
        float bob = Mathf.Abs(Mathf.Sin(BobPhase)) * 0.09f * BobAmount - 0.045f * BobAmount;
        Head.localPosition = new Vector3(0f, EyeHeight + (viewY - y) + bob, 0f);

        if (!playing) return;
        if (GameInput.UsePressed) Use();
        Slime(dt);
    }

    void Gravity(float dt)
    {
        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        else verticalSpeed -= 25f * dt;
    }

    void Use()
    {
        if (!Physics.Raycast(EyePosition, Head.forward, out RaycastHit hit, UseRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;
        Door door = hit.collider.GetComponentInParent<Door>();
        if (door)
        {
            door.Use(true);
            return;
        }
        if (Level.Current && Level.Current.TryUseWall(hit.point, hit.normal)) return;
        Sfx.Play2D(Sfx.NoWay, 0.7f); // "oof" - nothing to use here
    }

    void Slime(float dt)
    {
        Level level = Level.Current;
        if (!level || !controller.isGrounded || !level.HurtsAt(transform.position) ||
            transform.position.y > level.FloorAt(transform.position) + 0.15f)
        {
            slimeTimer = 0.6f; // a little grace when you step in
            return;
        }
        slimeTimer -= dt;
        if (slimeTimer > 0f) return;
        slimeTimer = 1f;
        Damage(5, false);
    }

    public void TakeDamage(float amount, Vector3 from) => Damage(Mathf.RoundToInt(amount), true);

    void Damage(int amount, bool armorProtects)
    {
        if (IsDead || amount <= 0 || GameManager.Instance.state != GameManager.State.Playing) return;
        if (armorProtects && armor > 0)
        {
            int saved = Mathf.Min(armor, amount / 3); // green armor absorbs a third
            armor -= saved;
            amount -= saved;
        }
        health -= amount;
        LastHurtTime = Time.time;
        DamageFlash = Mathf.Min(0.75f, DamageFlash + 0.15f + amount / 40f);
        GameManager.Instance.Shake(Mathf.Clamp01(amount / 30f) * 0.7f);

        if (health <= 0)
        {
            health = 0;
            Sfx.Play2D(Sfx.PlayerDeath);
            GameManager.Instance.OnPlayerDied();
            return;
        }
        if (Time.time > lastPainSound + 0.35f)
        {
            lastPainSound = Time.time;
            Sfx.Play2D(Sfx.PlayerPain, 0.8f);
        }
    }

    /// <summary>Adds health up to a cap. Returns false if you were already at the cap.</summary>
    public bool Heal(int amount, int cap)
    {
        if (health >= cap) return false;
        health = Mathf.Min(cap, health + amount);
        return true;
    }

    public void OnPickup(bool grin)
    {
        PickupFlash = 0.3f;
        if (grin) GrinUntil = Time.time + 1.5f;
    }
}
