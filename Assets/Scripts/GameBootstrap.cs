using UnityEngine;

/// <summary>
/// Builds the whole game from code when you press Play, so it runs in any scene
/// (even a brand-new empty one) without setting up prefabs by hand.
/// </summary>
public static class GameBootstrap
{
    static readonly Vector3 PlayerSpawn = new Vector3(0f, 0.1f, -7f);
    static readonly Vector3 RivalSpawn = new Vector3(0f, 0.1f, 7f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindAnyObjectByType<GameManager>() != null) return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.gameObject.tag = "MainCamera";
        }
        cam.gameObject.AddComponent<ThirdPersonCamera>();

        CityBuilder.Build();

        var manager = new GameObject("GameManager").AddComponent<GameManager>();
        SpawnFighters(manager);
    }

    /// <summary>(Re)creates the player rat and the rival rat.</summary>
    public static void SpawnFighters(GameManager manager)
    {
        Remove(manager.player);
        Remove(manager.rival);

        Camera cam = Camera.main;
        manager.player = CreatePlayer(cam ? cam.transform : null);
        manager.rival = CreateRival(manager.player);

        if (ThirdPersonCamera.Instance) ThirdPersonCamera.Instance.target = manager.player.transform;
    }

    static void Remove(Combatant fighter)
    {
        if (!fighter) return;
        fighter.gameObject.SetActive(false); // leaves Combatant.All right away
        Object.Destroy(fighter.gameObject);
    }

    static Combatant CreatePlayer(Transform cameraTransform)
    {
        GameObject go = CreateFighterBody("Player Rat", PlayerSpawn, Quaternion.identity, 1.7f, 0.3f);

        var combatant = go.AddComponent<Combatant>();
        combatant.Init("You", 0, 100f);
        go.AddComponent<MeleeAttacker>();
        go.AddComponent<PlayerController>().cameraTransform = cameraTransform;

        RatModel.Build(go.transform, new RatOutfit
        {
            fur = new Color(0.55f, 0.55f, 0.58f),       // grey street rat
            skin = new Color(0.95f, 0.65f, 0.68f),
            shirt = new Color(0.95f, 0.95f, 0.93f),     // white tank top
            pants = new Color(0.2f, 0.32f, 0.55f),      // jeans
            cap = true,
            capColor = new Color(0.8f, 0.12f, 0.1f),
            goldChain = true,
            weapon = RatWeapon.Crowbar,
        }, 1f);
        return combatant;
    }

    static Combatant CreateRival(Combatant target)
    {
        const float scale = 1.15f;
        GameObject go = CreateFighterBody("Rival Rat", RivalSpawn, Quaternion.Euler(0f, 180f, 0f), 1.7f * scale, 0.35f);

        var combatant = go.AddComponent<Combatant>();
        combatant.Init("Big Cheese", 1, 200f);
        combatant.maxChainStuns = 4; // can't be stun-locked forever

        var attacker = go.AddComponent<MeleeAttacker>();
        attacker.damageMultiplier = 0.7f;
        attacker.windupMultiplier = 1.35f; // slower, readable swings you can dodge

        go.AddComponent<RatAI>().target = target;

        RatModel.Build(go.transform, new RatOutfit
        {
            fur = new Color(0.42f, 0.3f, 0.2f),         // brown sewer rat
            skin = new Color(0.9f, 0.6f, 0.6f),
            shirt = new Color(0.2f, 0.45f, 0.25f),      // green hoodie
            pants = new Color(0.12f, 0.12f, 0.14f),
            sleeves = true,
            hood = true,
            weapon = RatWeapon.Pipe,
        }, scale);
        return combatant;
    }

    static GameObject CreateFighterBody(string name, Vector3 position, Quaternion rotation, float height, float radius)
    {
        var go = new GameObject(name);
        go.transform.SetPositionAndRotation(position, rotation);
        var controller = go.AddComponent<CharacterController>();
        controller.height = height;
        controller.radius = radius;
        controller.center = new Vector3(0f, height / 2f, 0f); // pivot at the feet
        controller.stepOffset = 0.35f;
        return go;
    }
}
