using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A hopping purple ooze mob: chases Finn when he gets close, hurts him on contact,
/// gets knocked back when hit, and shows a health bar above its head.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new List<Enemy>();

    static readonly Color OozeColor = new Color(0.55f, 0.25f, 0.75f);

    public int maxHealth = 3;
    public float moveSpeed = 3f;
    public float detectRange = 12f;
    public float attackRange = 1.4f;
    public int damage = 1;
    public float attackCooldown = 1f;
    public float gravity = -20f;

    public Vector3 Center => transform.position + Vector3.up * 0.6f;
    public float Radius => controller ? controller.radius : 0.5f;

    CharacterController controller;
    Transform body;
    Vector3 bodyBaseScale;
    Material bodyMaterial;
    Transform player;
    PlayerHealth playerHealth;

    int health;
    float verticalVelocity;
    Vector3 knockback;
    float nextAttackTime;
    float flashUntil;
    float hopPhase;
    bool dying;

    public static Enemy Create(Vector3 position, Material baseMaterial, PlayerController target)
    {
        var go = new GameObject("Ooze");
        go.transform.position = position;

        var cc = go.AddComponent<CharacterController>();
        cc.height = 1.2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0f, 0.6f, 0f);

        var enemy = go.AddComponent<Enemy>();
        enemy.bodyMaterial = new Material(baseMaterial) { color = OozeColor };
        var white = new Material(baseMaterial) { color = Color.white };
        var black = new Material(baseMaterial) { color = new Color(0.05f, 0.05f, 0.05f) };

        enemy.body = Part("Body", PrimitiveType.Sphere, go.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.1f, 0.95f, 1.1f), enemy.bodyMaterial);
        enemy.bodyBaseScale = enemy.body.localScale;
        // Eyes and mouth are children of the body so they squash with it (positions are in body space)
        Part("EyeLeft", PrimitiveType.Sphere, enemy.body, new Vector3(-0.17f, 0.15f, 0.4f), new Vector3(0.22f, 0.26f, 0.15f), white);
        Part("EyeRight", PrimitiveType.Sphere, enemy.body, new Vector3(0.17f, 0.15f, 0.4f), new Vector3(0.22f, 0.26f, 0.15f), white);
        Part("PupilLeft", PrimitiveType.Sphere, enemy.body, new Vector3(-0.15f, 0.13f, 0.47f), new Vector3(0.09f, 0.11f, 0.05f), black);
        Part("PupilRight", PrimitiveType.Sphere, enemy.body, new Vector3(0.15f, 0.13f, 0.47f), new Vector3(0.09f, 0.11f, 0.05f), black);
        Part("Mouth", PrimitiveType.Cube, enemy.body, new Vector3(0f, -0.12f, 0.48f), new Vector3(0.3f, 0.05f, 0.05f), black);

        if (target)
        {
            enemy.player = target.transform;
            enemy.playerHealth = target.GetComponent<PlayerHealth>();
        }
        return enemy;
    }

    static Transform Part(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part.transform;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        health = maxHealth;
        hopPhase = Random.value * 10f;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Update()
    {
        if (dying) return;

        bool gameOver = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        Vector3 move = Vector3.zero;

        if (player && !gameOver)
        {
            Vector3 toPlayer = player.position - Center;
            float heightDifference = Mathf.Abs(toPlayer.y);
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            if (distance < detectRange && distance > attackRange * 0.7f)
                move = toPlayer / distance * moveSpeed;

            if (distance > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(toPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 8f * Time.deltaTime);
            }

            if (distance < attackRange && heightDifference < 1.5f && Time.time >= nextAttackTime && playerHealth)
            {
                playerHealth.TakeDamage(damage, transform.position);
                nextAttackTime = Time.time + attackCooldown;
            }
        }

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        knockback = Vector3.MoveTowards(knockback, Vector3.zero, 25f * Time.deltaTime);

        controller.Move((move + knockback + Vector3.up * verticalVelocity) * Time.deltaTime);
        if (transform.position.y < -20f) Destroy(gameObject);

        Animate(move.sqrMagnitude > 0.01f);
    }

    void Animate(bool moving)
    {
        hopPhase += Time.deltaTime * (moving ? 8f : 3f);
        float squash = Mathf.Sin(hopPhase) * (moving ? 0.12f : 0.05f);
        body.localScale = Vector3.Scale(bodyBaseScale, new Vector3(1f + squash, 1f - squash, 1f + squash));

        bodyMaterial.color = Time.time < flashUntil ? Color.white : OozeColor;
    }

    public void TakeHit(int amount, Vector3 source)
    {
        if (dying) return;

        health -= amount;
        flashUntil = Time.time + 0.1f;

        Vector3 away = transform.position - source;
        away.y = 0f;
        knockback = away.normalized * 10f;
        verticalVelocity = 5f;

        if (health <= 0) StartCoroutine(Die());
    }

    IEnumerator Die()
    {
        dying = true;
        All.Remove(this);
        if (GameManager.Instance != null) GameManager.Instance.OnEnemyKilled();

        Vector3 start = body.localScale;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            float k = t / 0.3f;
            body.localScale = Vector3.Scale(start, new Vector3(1f + k, 1f - k, 1f + k)); // splat!
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnGUI()
    {
        if (dying || !player) return;
        Camera cam = Camera.main;
        if (!cam) return;

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        if (screen.z < 0f || screen.z > 30f) return;

        const float width = 50f, height = 7f;
        var back = new Rect(screen.x - width / 2f, Screen.height - screen.y, width, height);
        var fill = new Rect(back.x + 1f, back.y + 1f, (width - 2f) * health / maxHealth, height - 2f);

        Color previous = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(back, Texture2D.whiteTexture);
        GUI.color = new Color(0.9f, 0.15f, 0.15f);
        GUI.DrawTexture(fill, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
