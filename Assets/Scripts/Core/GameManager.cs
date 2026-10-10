using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Runs the game: sets up the camera and the dark, foggy look, builds the level and the player,
/// tracks kills / items / secrets / time, screen shake and messages, and restarts on R.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Playing, Dead, Complete }

    public static GameManager Instance { get; private set; }
    /// <summary>Everything in the current level lives under this object, so restarting just destroys it.</summary>
    public static Transform WorldRoot { get; private set; }
    /// <summary>The camera's transform (sprites turn to face it).</summary>
    public static Transform ViewTransform { get; private set; }

    public State state;
    public int kills, items, secrets;
    public float levelTime;

    public float StateTime { get; private set; }
    public float LevelStartTime { get; private set; }
    public bool ShowAutomap { get; private set; }
    public Vector2 ShakeOffset { get; private set; }
    public string MessageText => Time.time - messageTime < 4f ? message : null;

    Camera cam;
    string message;
    float messageTime = -10f;
    float shake;

    void Awake()
    {
        Instance = this;
        SpriteLibrary.Build();
        Sfx.Build();
        SetupRendering();
        SetupCamera();
        gameObject.AddComponent<Hud>();
        StartLevel();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    static void SetupRendering()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = Lighting.FogDensity;
        RenderSettings.fogColor = new Color(0.03f, 0.02f, 0.02f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Lighting.Ambient;
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        QualitySettings.pixelLightCount = 8;
        QualitySettings.shadows = ShadowQuality.Disable;

        // A blood-red hell sky over the courtyard
        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader)
        {
            var sky = new Material(skyShader);
            sky.SetFloat("_SunDisk", 0f);
            sky.EnableKeyword("_SUNDISK_NONE");
            sky.DisableKeyword("_SUNDISK_SIMPLE");
            sky.DisableKeyword("_SUNDISK_HIGH_QUALITY");
            sky.SetFloat("_SunSize", 0f);
            sky.SetFloat("_AtmosphereThickness", 2.2f);
            sky.SetColor("_SkyTint", new Color(0.9f, 0.2f, 0.08f));
            sky.SetColor("_GroundColor", new Color(0.12f, 0.03f, 0.02f));
            sky.SetFloat("_Exposure", 0.55f);
            RenderSettings.skybox = sky;
        }
    }

    void SetupCamera()
    {
        cam = Camera.main;
        if (!cam)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.gameObject.tag = "MainCamera";
        }
        if (!cam.GetComponent<AudioListener>()) cam.gameObject.AddComponent<AudioListener>();
        cam.gameObject.AddComponent<RetroCamera>();
        cam.fieldOfView = 74f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 200f;
        cam.clearFlags = RenderSettings.skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.3f, 0.06f, 0.03f);
        cam.allowMSAA = false;
        ViewTransform = cam.transform;
    }

    /// <summary>(Re)builds the level and puts you at the start with a pistol.</summary>
    public void StartLevel()
    {
        cam.transform.SetParent(null, true); // keep the camera when the old player is destroyed
        if (WorldRoot)
        {
            WorldRoot.gameObject.SetActive(false); // unregisters monsters, barrels and lights right away
            Destroy(WorldRoot.gameObject);
        }
        WorldRoot = new GameObject("World").transform;

        kills = items = secrets = 0;
        levelTime = 0f;
        state = State.Playing;
        StateTime = 0f;
        LevelStartTime = Time.time;
        shake = 0f;
        message = null;

        Level level = Level.Build(WorldRoot);
        Player.Create(level.PlayerStart, 0f, cam);
        SetCursorLocked(true);
    }

    static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        StateTime += dt;
        if (state == State.Playing) levelTime += dt;

        shake = Mathf.MoveTowards(shake, 0f, dt * 2.5f);
        ShakeOffset = shake > 0f ? Random.insideUnitCircle * shake * 2.5f : Vector2.zero;

        if (GameInput.UnlockCursorPressed) SetCursorLocked(false);
        else if (!GameInput.CursorLocked && GameInput.ClickPressed) SetCursorLocked(true);

        if (GameInput.RestartPressed || (state != State.Playing && StateTime > 1.2f && GameInput.ContinuePressed))
        {
            StartLevel();
            return;
        }
        if (GameInput.AutomapPressed) ShowAutomap = !ShowAutomap;
        if (GameInput.PixelTogglePressed)
        {
            RetroCamera.Pixelate = !RetroCamera.Pixelate;
            Message(RetroCamera.Pixelate ? "Retro pixels ON" : "Retro pixels OFF");
        }
    }

    public void Message(string text)
    {
        message = text;
        messageTime = Time.time;
    }

    public void Shake(float amount) => shake = Mathf.Max(shake, amount);

    public void OnPlayerDied()
    {
        state = State.Dead;
        StateTime = 0f;
    }

    public void CompleteLevel()
    {
        if (state != State.Playing) return;
        state = State.Complete;
        StateTime = 0f;
        Sfx.Play2D(Sfx.WeaponPickup);
    }
}
