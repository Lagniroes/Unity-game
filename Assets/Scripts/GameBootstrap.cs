using UnityEngine;

/// <summary>
/// Builds the whole level from code when you press Play, so the game runs in any
/// scene (even a brand-new empty one) without setting up prefabs by hand.
/// </summary>
public static class GameBootstrap
{
    static Material baseMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindAnyObjectByType<GameManager>() != null) return;
        BuildWorld();
    }

    static void BuildWorld()
    {
        var level = new GameObject("Level").transform;

        // Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        baseMaterial = ground.GetComponent<Renderer>().sharedMaterial; // works in Built-in, URP and HDRP
        ground.name = "Ground";
        ground.transform.SetParent(level);
        ground.transform.localScale = new Vector3(8f, 1f, 8f); // 80 x 80 meters
        SetColor(ground, new Color(0.35f, 0.65f, 0.3f));

        EnsureLight();

        // Player: Finn the Human
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 1.1f, 0f);
        var characterController = player.AddComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.35f;
        var controller = player.AddComponent<PlayerController>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerCombat>();
        FinnModel.Build(player.transform, new Vector3(0f, -1f, 0f), baseMaterial); // feet at the bottom of the controller

        // Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.gameObject.tag = "MainCamera";
        }
        cam.gameObject.AddComponent<ThirdPersonCamera>().target = player.transform;
        controller.cameraTransform = cam.transform;

        // Game manager
        var manager = new GameObject("GameManager").AddComponent<GameManager>();
        manager.player = controller;
        manager.baseMaterial = baseMaterial;
        manager.coinMaterial = new Material(baseMaterial) { color = new Color(1f, 0.82f, 0.1f) };

        // A ring of coins on the ground around the start
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI * 2f / 6f;
            manager.AddCoinSpawn(new Vector3(Mathf.Cos(angle) * 6f, 1f, Mathf.Sin(angle) * 6f));
        }

        // A spiral staircase of floating platforms, each with a coin on top
        const int steps = 10;
        const float radius = 12f;
        const float stepHeight = 1.3f;
        for (int i = 0; i < steps; i++)
        {
            float angle = i * 25f * Mathf.Deg2Rad;
            float top = stepHeight * (i + 1);
            var center = new Vector3(Mathf.Cos(angle) * radius, top - 0.25f, Mathf.Sin(angle) * radius);
            Color color = Color.HSVToRGB(i / (float)steps, 0.6f, 0.9f);
            CreateBlock("Platform " + (i + 1), center, new Vector3(3.5f, 0.5f, 3.5f), color, level);
            manager.AddCoinSpawn(center + Vector3.up * 1.25f);
        }

        // Some crates to jump on, and trees for scenery
        CreateBlock("Crate", new Vector3(-8f, 0.75f, 4f), new Vector3(1.5f, 1.5f, 1.5f), new Color(0.6f, 0.4f, 0.2f), level);
        CreateBlock("Crate", new Vector3(-9.5f, 1.5f, 6f), new Vector3(1.5f, 3f, 1.5f), new Color(0.6f, 0.4f, 0.2f), level);
        manager.AddCoinSpawn(new Vector3(-9.5f, 4f, 6f));

        // Ooze mobs roaming the field
        for (int i = 0; i < 7; i++)
        {
            float angle = (i * 360f / 7f + 20f) * Mathf.Deg2Rad;
            float distance = i % 2 == 0 ? 18f : 24f;
            manager.AddEnemySpawn(new Vector3(Mathf.Cos(angle) * distance, 0.1f, Mathf.Sin(angle) * distance));
        }

        var random = new System.Random(42);
        for (int i = 0; i < 25; i++)
        {
            var pos = new Vector3((float)random.NextDouble() * 70f - 35f, 0f, (float)random.NextDouble() * 70f - 35f);
            if (pos.magnitude < 18f) continue; // keep the play area clear
            CreateTree(pos, level);
        }

        manager.StartGame();
    }

    static GameObject CreateBlock(string name, Vector3 position, Vector3 size, Color color, Transform parent)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.position = position;
        block.transform.localScale = size;
        block.transform.SetParent(parent, true);
        SetColor(block, color);
        return block;
    }

    static void CreateTree(Vector3 position, Transform parent)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Tree";
        trunk.transform.position = position + Vector3.up * 1.5f;
        trunk.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
        trunk.transform.SetParent(parent, true);
        SetColor(trunk, new Color(0.45f, 0.3f, 0.15f));

        GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaves.name = "Leaves";
        leaves.transform.position = position + Vector3.up * 4f;
        leaves.transform.localScale = Vector3.one * 3f;
        leaves.transform.SetParent(trunk.transform, true);
        SetColor(leaves, new Color(0.15f, 0.5f, 0.2f));
    }

    static void EnsureLight()
    {
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional) return;

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void SetColor(GameObject go, Color color)
    {
        go.GetComponent<Renderer>().sharedMaterial = new Material(baseMaterial) { color = color };
    }
}
