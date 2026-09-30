using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks coins, mobs, health, score and time, draws the HUD, and handles restarting.
/// </summary>
public class GameManager : MonoBehaviour
{
    const string BestTimeKey = "BestTime";

    public static GameManager Instance { get; private set; }

    public PlayerController player;
    public Material baseMaterial;
    public Material coinMaterial;

    readonly List<Vector3> coinSpawns = new List<Vector3>();
    readonly List<GameObject> activeCoins = new List<GameObject>();
    readonly List<Vector3> enemySpawns = new List<Vector3>();
    readonly List<Enemy> activeEnemies = new List<Enemy>();

    PlayerHealth playerHealth;
    int collected;
    int mobsDefeated;
    float elapsed;
    float bestTime;
    GUIStyle hudStyle;
    GUIStyle bigStyle;
    GUIStyle smallStyle;

    public bool HasWon { get; private set; }
    public bool IsPlayerDead => playerHealth && playerHealth.IsDead;
    public bool IsGameOver => HasWon || IsPlayerDead;

    void Awake()
    {
        Instance = this;
        bestTime = PlayerPrefs.GetFloat(BestTimeKey, 0f);
    }

    public void AddCoinSpawn(Vector3 position) => coinSpawns.Add(position);
    public void AddEnemySpawn(Vector3 position) => enemySpawns.Add(position);

    public void StartGame()
    {
        foreach (GameObject coin in activeCoins) Destroy(coin);
        activeCoins.Clear();
        foreach (Vector3 position in coinSpawns) SpawnCoin(position);

        foreach (Enemy enemy in activeEnemies)
            if (enemy) Destroy(enemy.gameObject);
        activeEnemies.Clear();
        foreach (Vector3 position in enemySpawns)
            activeEnemies.Add(Enemy.Create(position, baseMaterial, player));

        collected = 0;
        mobsDefeated = 0;
        elapsed = 0f;
        HasWon = false;
        if (player)
        {
            player.Respawn();
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth) playerHealth.ResetHealth();
        }
    }

    void SpawnCoin(Vector3 position)
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = "Coin";
        Destroy(coin.GetComponent<Collider>());
        coin.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, 0f, 0f));
        coin.transform.localScale = new Vector3(0.8f, 0.06f, 0.8f);
        coin.GetComponent<Renderer>().sharedMaterial = coinMaterial;
        coin.AddComponent<Coin>().Init(player ? player.transform : null);
        activeCoins.Add(coin);
    }

    public void CollectCoin(Coin coin)
    {
        if (IsGameOver) return;

        activeCoins.Remove(coin.gameObject);
        Destroy(coin.gameObject);
        collected++;
        if (playerHealth) playerHealth.Heal(1);

        if (collected >= coinSpawns.Count)
        {
            HasWon = true;
            if (bestTime <= 0f || elapsed < bestTime)
            {
                bestTime = elapsed;
                PlayerPrefs.SetFloat(BestTimeKey, bestTime);
            }
        }
    }

    public void OnEnemyKilled() => mobsDefeated++;

    void Update()
    {
        if (!IsGameOver) elapsed += Time.deltaTime;
        if (GameInput.RestartPressed) StartGame();
    }

    void OnGUI()
    {
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            hudStyle.normal.textColor = Color.white;
            bigStyle = new GUIStyle(hudStyle) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
            smallStyle = new GUIStyle(hudStyle) { fontSize = 16 };
        }

        DrawHealthBar(new Rect(20, 20, 260, 26));

        string best = bestTime > 0f ? $"   Best: {bestTime:0.0}s" : "";
        GUI.Label(new Rect(20, 55, 600, 40), $"Coins: {collected} / {coinSpawns.Count}   Mobs defeated: {mobsDefeated}", hudStyle);
        GUI.Label(new Rect(20, 90, 600, 40), $"Time: {elapsed:0.0}s{best}", hudStyle);
        GUI.Label(new Rect(20, Screen.height - 45, 1000, 40),
            "WASD move · Mouse look · Space jump · Shift sprint · Left click / F attack · R restart · Esc free cursor",
            smallStyle);

        var center = new Rect(0, Screen.height / 2f - 60, Screen.width, 120);
        if (HasWon)
            GUI.Label(center, $"You collected every coin!\n{elapsed:0.0}s — press R to play again", bigStyle);
        else if (IsPlayerDead)
            GUI.Label(center, "Finn got knocked out!\nPress R to try again", bigStyle);
    }

    void DrawHealthBar(Rect rect)
    {
        if (!playerHealth) return;

        float fraction = (float)playerHealth.Current / playerHealth.maxHealth;
        Color fillColor = Color.Lerp(new Color(0.9f, 0.15f, 0.15f), new Color(0.3f, 0.85f, 0.3f), fraction);

        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(rect.x + 3, rect.y + 3, (rect.width - 6) * fraction, rect.height - 6), Texture2D.whiteTexture);
        GUI.color = previous;

        var label = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter };
        GUI.Label(rect, $"HP {playerHealth.Current} / {playerHealth.maxHealth}", label);
    }
}
