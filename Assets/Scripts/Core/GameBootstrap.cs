using UnityEngine;

/// <summary>
/// Builds the whole game from code when you press Play, so it runs in any scene
/// (even a brand-new empty one) without setting up prefabs or importing art.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindAnyObjectByType<GameManager>() != null) return;

        // The default scene's sun would light up the whole map; the level brings its own lights
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            light.gameObject.SetActive(false);

        new GameObject("GameManager").AddComponent<GameManager>();
    }
}
