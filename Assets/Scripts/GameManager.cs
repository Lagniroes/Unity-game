using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks coins, score and time, draws the HUD, and handles restarting.
/// </summary>
public class GameManager : MonoBehaviour
{
    const string BestTimeKey = "BestTime";

    public static GameManager Instance { get; private set; }

    public PlayerController player;
    public Material coinMaterial;

    readonly List<Vector3> coinSpawns = new List<Vector3>();
    readonly List<GameObject> activeCoins = new List<GameObject>();

    int collected;
    float elapsed;
    float bestTime;
    GUIStyle hudStyle;
    GUIStyle bigStyle;

    public bool HasWon { get; private set; }

    void Awake()
    {
        Instance = this;
        bestTime = PlayerPrefs.GetFloat(BestTimeKey, 0f);
    }

    public void AddCoinSpawn(Vector3 position) => coinSpawns.Add(position);

    public void StartGame()
    {
        foreach (GameObject coin in activeCoins) Destroy(coin);
        activeCoins.Clear();
        foreach (Vector3 position in coinSpawns) SpawnCoin(position);

        collected = 0;
        elapsed = 0f;
        HasWon = false;
        if (player) player.Respawn();
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
        if (HasWon) return;

        activeCoins.Remove(coin.gameObject);
        Destroy(coin.gameObject);
        collected++;

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

    void Update()
    {
        if (!HasWon) elapsed += Time.deltaTime;
        if (GameInput.RestartPressed) StartGame();
    }

    void OnGUI()
    {
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            hudStyle.normal.textColor = Color.white;
            bigStyle = new GUIStyle(hudStyle) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
        }

        string best = bestTime > 0f ? $"   Best: {bestTime:0.0}s" : "";
        GUI.Label(new Rect(20, 15, 600, 40), $"Coins: {collected} / {coinSpawns.Count}", hudStyle);
        GUI.Label(new Rect(20, 50, 600, 40), $"Time: {elapsed:0.0}s{best}", hudStyle);
        GUI.Label(new Rect(20, Screen.height - 45, 900, 40),
            "WASD move · Mouse look · Space jump · Shift sprint · R restart · Esc free cursor",
            new GUIStyle(hudStyle) { fontSize = 16 });

        if (HasWon)
        {
            GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 120),
                $"You collected every coin!\n{elapsed:0.0}s — press R to play again", bigStyle);
        }
    }
}
