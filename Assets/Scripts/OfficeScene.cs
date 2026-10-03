using UnityEngine;

/// <summary>
/// A small office, built far away from the city, where a rat in a shirt and tie sits at
/// a desk and drinks coffee on a loop. Press O to cut to it with movie-style camera
/// shots, and O again to go back to the street fight (which pauses meanwhile).
/// </summary>
public class OfficeScene : MonoBehaviour
{
    public static OfficeScene Instance { get; private set; }
    public static bool Viewing => Instance && Instance.viewing;

    static readonly Color Carpet = new Color(0.32f, 0.36f, 0.42f);
    static readonly Color Wall = new Color(0.86f, 0.84f, 0.78f);
    static readonly Color Ceiling = new Color(0.93f, 0.93f, 0.9f);
    static readonly Color Wood = new Color(0.55f, 0.38f, 0.22f);
    static readonly Color DarkGrey = new Color(0.12f, 0.12f, 0.13f);
    static readonly Color MidGrey = new Color(0.45f, 0.46f, 0.48f);
    static readonly Color LightGrey = new Color(0.78f, 0.78f, 0.8f);
    static readonly Color ScreenBlue = new Color(0.08f, 0.12f, 0.22f);
    static readonly Color LampGlow = new Color(1f, 0.95f, 0.8f);
    static readonly Color Plant = new Color(0.2f, 0.5f, 0.22f);
    static readonly Color Cheese = new Color(1f, 0.82f, 0.25f);

    static readonly Color[] CodeColors =
    {
        new Color(0.5f, 0.85f, 0.5f), new Color(0.95f, 0.5f, 0.75f), new Color(0.5f, 0.75f, 1f),
        new Color(1f, 0.85f, 0.4f), new Color(0.8f, 0.8f, 0.85f),
    };

    Camera officeCamera;
    Camera gameCamera;
    CoffeeRatAnimator rat;
    Transform hourHand;
    Transform minuteHand;
    Transform secondHand;
    Transform[] codeLines;
    float[] codeWidths;
    float[] codeTargets;
    int codeLine;
    bool viewing;
    GUIStyle captionStyle;
    GUIStyle bubbleStyle;

    /// <summary>Builds the office at <paramref name="origin"/> (keep it far from the city).</summary>
    public static OfficeScene Build(Vector3 origin, Camera gameCamera)
    {
        var root = new GameObject("Office").transform;
        root.position = origin;
        var office = root.gameObject.AddComponent<OfficeScene>();
        Instance = office;
        office.gameCamera = gameCamera;

        BuildRoom(root);
        BuildWindowView(root);
        Transform desk = BuildDesk(root, office, out Transform keyboard, out Transform mug, out Transform mugRest);
        BuildChair(root);
        BuildDecor(root, office);
        BuildLights(root);

        // The rat: hips on the seat (seat top 0.23 m), facing the monitor along +Z
        Transform holder = Shapes.Pivot("CoffeeRat", root, new Vector3(0f, -0.3f, 0f));
        RatRig rig = RatModel.Build(holder, new RatOutfit
        {
            fur = new Color(0.55f, 0.55f, 0.58f),
            furLight = new Color(0.78f, 0.77f, 0.76f),
            skin = new Color(0.95f, 0.65f, 0.68f),
            shirt = new Color(0.72f, 0.84f, 0.96f),     // light blue office shirt
            sleeves = true,
            tie = true,
            tieColor = new Color(0.7f, 0.1f, 0.12f),
            glasses = true,
            pants = new Color(0.22f, 0.23f, 0.26f),     // grey slacks
            sneakers = true,
            sneakerColor = new Color(0.2f, 0.12f, 0.07f), // brown office shoes
        }, 1f, animate: false);

        office.rat = rig.gameObject.AddComponent<CoffeeRatAnimator>();
        office.rat.rig = rig;
        office.rat.mug = mug;
        office.rat.mugRest = mugRest;
        office.rat.keyboard = keyboard;

        // Separate camera for the office, off until you press O
        var cameraObject = new GameObject("OfficeCamera");
        cameraObject.transform.SetParent(root, false);
        office.officeCamera = cameraObject.AddComponent<Camera>();
        office.officeCamera.nearClipPlane = 0.03f;
        office.officeCamera.fieldOfView = 45f;
        office.officeCamera.clearFlags = CameraClearFlags.SolidColor;
        office.officeCamera.backgroundColor = new Color(0.95f, 0.6f, 0.45f);
        office.officeCamera.enabled = false;
        return office;
    }

    void Update()
    {
        if (GameInput.OfficeTogglePressed) SetViewing(!viewing);
        UpdateClock();
        UpdateCode();
        if (viewing) UpdateCameraShot();
    }

    void SetViewing(bool value)
    {
        viewing = value;
        officeCamera.enabled = value;
        if (gameCamera) gameCamera.enabled = !value;
    }

    // ---------------------------------------------------------------- Camera

    void UpdateCameraShot()
    {
        float t = rat.LoopTime;
        Vector3 position, lookAt;
        float drift;
        if (t < 3f)
        {
            // Over the shoulder: the rat typing away, code on the screen
            drift = t / 3f;
            position = new Vector3(-0.55f + drift * 0.1f, 1.3f, -0.75f + drift * 0.08f);
            lookAt = new Vector3(0.05f, 0.92f, 0.85f);
        }
        else if (t < 9f)
        {
            // Close-up from the front-right: reaching, sipping, "ahh"
            drift = (t - 3f) / 6f;
            position = new Vector3(0.95f - drift * 0.15f, 1.12f, 0.95f - drift * 0.1f);
            lookAt = new Vector3(0.05f, 0.9f, 0.25f);
        }
        else
        {
            // Wide shot of the whole office, slowly pushing in
            drift = (t - 9f) / 3f;
            position = new Vector3(-2.3f + drift * 0.3f, 1.7f, -1.9f + drift * 0.3f);
            lookAt = new Vector3(0f, 0.8f, 0.5f);
        }

        Transform cam = officeCamera.transform;
        cam.position = transform.TransformPoint(position);
        cam.rotation = Quaternion.LookRotation(transform.TransformPoint(lookAt) - cam.position);
    }

    void OnGUI()
    {
        if (!viewing) return;
        if (captionStyle == null)
        {
            captionStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerCenter };
            captionStyle.normal.textColor = Color.white;
            bubbleStyle = new GUIStyle(GUI.skin.box) { fontSize = 22, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter };
            bubbleStyle.normal.textColor = Color.black;
            bubbleStyle.normal.background = Texture2D.whiteTexture;
        }

        // Cinematic letterbox bars
        float bar = Screen.height * 0.09f;
        Color previous = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, bar), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, Screen.height - bar, Screen.width, bar), Texture2D.whiteTexture);
        GUI.color = previous;

        GUI.Label(new Rect(0, Screen.height - bar, Screen.width, bar - 10), "Monday, 9:00 AM. Coffee first.   (press O to go back to the street)", captionStyle);

        // "Ahh~" speech bubble next to the rat's head
        if (rat.Ahh > 0.3f)
        {
            Vector3 head = rat.rig.head.TransformPoint(new Vector3(0.25f, 0.35f, 0.2f));
            Vector3 screen = officeCamera.WorldToScreenPoint(head);
            if (screen.z > 0f)
            {
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01((rat.Ahh - 0.3f) / 0.4f));
                GUI.Box(new Rect(screen.x - 50, Screen.height - screen.y - 25, 100, 50), "ahh~", bubbleStyle);
                GUI.color = previous;
            }
        }
    }

    // ---------------------------------------------------------------- Live details

    void UpdateClock()
    {
        System.DateTime now = System.DateTime.Now;
        float seconds = now.Second + now.Millisecond / 1000f;
        float minutes = now.Minute + seconds / 60f;
        float hours = now.Hour % 12 + minutes / 60f;
        secondHand.localRotation = Quaternion.Euler(0f, 0f, -seconds * 6f);
        minuteHand.localRotation = Quaternion.Euler(0f, 0f, -minutes * 6f);
        hourHand.localRotation = Quaternion.Euler(0f, 0f, -hours * 30f);
    }

    void UpdateCode()
    {
        // While the rat types, the current line of "code" on the monitor grows; full screen wraps around
        if (rat.Typing < 0.5f) return;
        codeWidths[codeLine] += Time.deltaTime * 0.35f;
        if (codeWidths[codeLine] >= codeTargets[codeLine])
        {
            codeWidths[codeLine] = codeTargets[codeLine];
            codeLine++;
            if (codeLine >= codeLines.Length)
            {
                codeLine = 0;
                for (int i = 0; i < codeWidths.Length; i++) codeWidths[i] = 0f;
            }
        }
        for (int i = 0; i < codeLines.Length; i++) SetCodeWidth(i);
    }

    void SetCodeWidth(int i)
    {
        float width = Mathf.Max(codeWidths[i], 0.0001f);
        Vector3 p = codeLines[i].localPosition;
        float indent = (i % 3) * 0.03f;
        codeLines[i].localPosition = new Vector3(-0.26f + indent + width / 2f, p.y, p.z);
        codeLines[i].localScale = new Vector3(width, 0.012f, 0.002f);
    }

    // ---------------------------------------------------------------- Building

    static Material M(Color c) => Shapes.Shared(c);

    static void Box(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        => Shapes.Create(PrimitiveType.Cube, name, parent, position, size, M(color));

    static void BuildRoom(Transform root)
    {
        Transform room = Shapes.Pivot("Room", root, Vector3.zero);
        Box(room, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(8f, 0.1f, 8f), Carpet);
        Box(room, "Ceiling", new Vector3(0f, 3.05f, 0f), new Vector3(8f, 0.1f, 8f), Ceiling);
        Box(room, "WallLeft", new Vector3(-4f, 1.5f, 0f), new Vector3(0.1f, 3f, 8f), Wall);
        Box(room, "WallRight", new Vector3(4f, 1.5f, 0f), new Vector3(0.1f, 3f, 8f), Wall);
        Box(room, "WallFront", new Vector3(0f, 1.5f, -4f), new Vector3(8f, 3f, 0.1f), Wall);

        // Back wall with a big window (opening x -1.5..1.5, y 1..2.4)
        Box(room, "WallBackLeft", new Vector3(-2.75f, 1.5f, 4f), new Vector3(2.5f, 3f, 0.1f), Wall);
        Box(room, "WallBackRight", new Vector3(2.75f, 1.5f, 4f), new Vector3(2.5f, 3f, 0.1f), Wall);
        Box(room, "WallBackBottom", new Vector3(0f, 0.5f, 4f), new Vector3(3f, 1f, 0.1f), Wall);
        Box(room, "WallBackTop", new Vector3(0f, 2.7f, 4f), new Vector3(3f, 0.6f, 0.1f), Wall);
        Color frame = new Color(0.25f, 0.25f, 0.27f);
        Box(room, "Sill", new Vector3(0f, 1f, 3.92f), new Vector3(3.1f, 0.05f, 0.2f), frame);
        Box(room, "FrameTop", new Vector3(0f, 2.4f, 3.95f), new Vector3(3.1f, 0.05f, 0.1f), frame);
        Box(room, "FrameLeft", new Vector3(-1.52f, 1.7f, 3.95f), new Vector3(0.05f, 1.45f, 0.1f), frame);
        Box(room, "FrameRight", new Vector3(1.52f, 1.7f, 3.95f), new Vector3(0.05f, 1.45f, 0.1f), frame);
        Box(room, "Mullion", new Vector3(0f, 1.7f, 3.95f), new Vector3(0.04f, 1.4f, 0.08f), frame);

        // Skirting boards
        Box(room, "Skirting", new Vector3(-3.94f, 0.05f, 0f), new Vector3(0.02f, 0.1f, 8f), Wood);
        Box(room, "Skirting", new Vector3(3.94f, 0.05f, 0f), new Vector3(0.02f, 0.1f, 8f), Wood);
    }

    static void BuildWindowView(Transform root)
    {
        // Sunrise sky and a city skyline outside the window
        Transform view = Shapes.Pivot("WindowView", root, Vector3.zero);
        Box(view, "Sky", new Vector3(0f, 4f, 16f), new Vector3(40f, 20f, 0.1f), new Color(0.98f, 0.62f, 0.45f));
        Box(view, "SkyHigh", new Vector3(0f, 12f, 15.9f), new Vector3(40f, 8f, 0.1f), new Color(0.55f, 0.55f, 0.8f));
        Shapes.Create(PrimitiveType.Sphere, "Sun", view, new Vector3(3f, 2.5f, 15.5f), Vector3.one * 2.5f, M(new Color(1f, 0.85f, 0.5f)));

        var random = new System.Random(3);
        Color building = new Color(0.32f, 0.26f, 0.38f);
        Color window = new Color(1f, 0.85f, 0.55f);
        for (float x = -12f; x < 12f; x += 1.6f + (float)random.NextDouble())
        {
            float height = 2f + (float)random.NextDouble() * 9f;
            float z = 10f + (float)random.NextDouble() * 3f;
            float width = 1.2f + (float)random.NextDouble();
            Box(view, "Skyline", new Vector3(x, height / 2f - 2f, z), new Vector3(width, height, 1f), building * (0.8f + (float)random.NextDouble() * 0.3f));
            for (int i = 0; i < 3; i++)
            {
                if (random.NextDouble() < 0.5) continue;
                float wy = -1.5f + (float)random.NextDouble() * (height - 1f);
                Box(view, "LitWindow", new Vector3(x + ((float)random.NextDouble() - 0.5f) * width * 0.6f, wy, z - 0.52f), new Vector3(0.18f, 0.25f, 0.02f), window);
            }
        }
    }

    static Transform BuildDesk(Transform root, OfficeScene office, out Transform keyboard, out Transform mug, out Transform mugRest)
    {
        Transform desk = Shapes.Pivot("Desk", root, Vector3.zero);

        // Desktop (top surface at 0.62 m), side panels, modesty panel, drawers
        Box(desk, "Top", new Vector3(0f, 0.6f, 0.7f), new Vector3(1.6f, 0.04f, 0.8f), Wood);
        Box(desk, "SideLeft", new Vector3(-0.78f, 0.29f, 0.7f), new Vector3(0.04f, 0.58f, 0.76f), Wood * 0.85f);
        Box(desk, "SideRight", new Vector3(0.78f, 0.29f, 0.7f), new Vector3(0.04f, 0.58f, 0.76f), Wood * 0.85f);
        Box(desk, "Modesty", new Vector3(0f, 0.35f, 1.07f), new Vector3(1.52f, 0.45f, 0.03f), Wood * 0.85f);
        Box(desk, "Drawers", new Vector3(-0.55f, 0.29f, 0.75f), new Vector3(0.4f, 0.58f, 0.6f), Wood * 0.9f);
        for (int i = 0; i < 3; i++)
            Box(desk, "Handle", new Vector3(-0.55f, 0.48f - i * 0.18f, 0.445f), new Vector3(0.12f, 0.015f, 0.015f), MidGrey);

        // Monitor
        Box(desk, "MonitorBase", new Vector3(0f, 0.628f, 0.95f), new Vector3(0.24f, 0.015f, 0.16f), DarkGrey);
        Box(desk, "MonitorNeck", new Vector3(0f, 0.74f, 0.98f), new Vector3(0.04f, 0.22f, 0.04f), DarkGrey);
        Transform monitor = Shapes.Pivot("Monitor", desk, new Vector3(0f, 0.98f, 0.95f));
        Box(monitor, "Bezel", Vector3.zero, new Vector3(0.64f, 0.4f, 0.03f), DarkGrey);
        Box(monitor, "Screen", new Vector3(0f, 0f, -0.016f), new Vector3(0.6f, 0.36f, 0.003f), ScreenBlue);
        Box(monitor, "StickyNote", new Vector3(0.29f, 0.17f, -0.02f), new Vector3(0.06f, 0.06f, 0.003f), new Color(1f, 0.95f, 0.4f));
        Box(monitor, "StickyNote", new Vector3(-0.3f, -0.15f, -0.02f), new Vector3(0.06f, 0.06f, 0.003f), new Color(0.6f, 0.95f, 0.6f));

        office.codeLines = new Transform[9];
        office.codeWidths = new float[9];
        office.codeTargets = new float[9];
        var random = new System.Random(11);
        for (int i = 0; i < office.codeLines.Length; i++)
        {
            Color color = CodeColors[random.Next(CodeColors.Length)];
            office.codeLines[i] = Shapes.Create(PrimitiveType.Cube, "Code", monitor, new Vector3(0f, 0.14f - i * 0.034f, -0.019f), Vector3.one * 0.001f, M(color));
            office.codeTargets[i] = 0.1f + (float)random.NextDouble() * 0.35f;
            office.codeWidths[i] = i < 4 ? office.codeTargets[i] : 0f;
        }
        office.codeLine = 4;
        for (int i = 0; i < office.codeLines.Length; i++) office.SetCodeWidth(i);

        // Keyboard with rows of keys
        keyboard = Shapes.Pivot("Keyboard", desk, new Vector3(0f, 0.64f, 0.42f));
        Box(keyboard, "Body", new Vector3(0f, -0.006f, 0f), new Vector3(0.42f, 0.02f, 0.14f), DarkGrey);
        for (int row = 0; row < 4; row++)
            Box(keyboard, "Keys", new Vector3(0f, 0.006f, 0.045f - row * 0.03f), new Vector3(0.38f, 0.008f, 0.022f), LightGrey);
        Box(keyboard, "Spacebar", new Vector3(0f, 0.006f, -0.058f), new Vector3(0.16f, 0.008f, 0.018f), LightGrey);

        // Coffee mug on a coaster (pivot at the bottom, handle on +X)
        mugRest = Shapes.Pivot("MugRest", desk, new Vector3(0.3f, 0.62f, 0.42f));
        Shapes.Create(PrimitiveType.Cylinder, "Coaster", desk, new Vector3(0.3f, 0.622f, 0.42f), new Vector3(0.13f, 0.003f, 0.13f), M(new Color(0.4f, 0.25f, 0.15f)));
        mug = Shapes.Pivot("Mug", desk, mugRest.localPosition);
        Color mugColor = new Color(0.85f, 0.2f, 0.18f);
        Shapes.Create(PrimitiveType.Cylinder, "Cup", mug, new Vector3(0f, 0.05f, 0f), new Vector3(0.09f, 0.05f, 0.09f), M(mugColor));
        Shapes.Create(PrimitiveType.Cylinder, "Coffee", mug, new Vector3(0f, 0.098f, 0f), new Vector3(0.078f, 0.003f, 0.078f), M(new Color(0.25f, 0.13f, 0.06f)));
        Shapes.Create(PrimitiveType.Cylinder, "Rim", mug, new Vector3(0f, 0.099f, 0f), new Vector3(0.092f, 0.002f, 0.092f), M(mugColor * 1.1f));
        Box(mug, "HandleTop", new Vector3(0.06f, 0.075f, 0f), new Vector3(0.04f, 0.012f, 0.015f), mugColor);
        Box(mug, "HandleSide", new Vector3(0.075f, 0.05f, 0f), new Vector3(0.012f, 0.05f, 0.015f), mugColor);
        Box(mug, "HandleBottom", new Vector3(0.06f, 0.025f, 0f), new Vector3(0.04f, 0.012f, 0.015f), mugColor);
        Box(mug, "Logo", new Vector3(0f, 0.05f, -0.046f), new Vector3(0.035f, 0.03f, 0.002f), Cheese);

        // Desk clutter: papers, a cheese snack, a lamp, a pen cup, a nameplate
        Shapes.Create(PrimitiveType.Cube, "Papers", desk, new Vector3(-0.42f, 0.63f, 0.6f), Quaternion.Euler(0f, 12f, 0f), new Vector3(0.21f, 0.02f, 0.29f), M(new Color(0.96f, 0.96f, 0.94f)));
        Shapes.Create(PrimitiveType.Cube, "Paper", desk, new Vector3(-0.4f, 0.642f, 0.58f), Quaternion.Euler(0f, -5f, 0f), new Vector3(0.21f, 0.002f, 0.29f), M(Color.white));
        Shapes.Create(PrimitiveType.Cylinder, "Plate", desk, new Vector3(-0.4f, 0.625f, 0.32f), new Vector3(0.16f, 0.006f, 0.16f), M(Color.white));
        Shapes.Create(PrimitiveType.Cube, "CheeseWedge", desk, new Vector3(-0.4f, 0.655f, 0.32f), Quaternion.Euler(0f, 30f, 0f), new Vector3(0.09f, 0.05f, 0.06f), M(Cheese));
        Shapes.Create(PrimitiveType.Sphere, "CheeseHole", desk, new Vector3(-0.385f, 0.665f, 0.3f), Vector3.one * 0.015f, M(Cheese * 0.8f));

        Shapes.Create(PrimitiveType.Cylinder, "LampBase", desk, new Vector3(0.6f, 0.63f, 0.95f), new Vector3(0.14f, 0.01f, 0.14f), M(DarkGrey));
        Shapes.Create(PrimitiveType.Cylinder, "LampArm", desk, new Vector3(0.58f, 0.82f, 0.9f), Quaternion.Euler(-15f, 0f, 10f), new Vector3(0.02f, 0.2f, 0.02f), M(DarkGrey));
        Shapes.Create(PrimitiveType.Cylinder, "LampShade", desk, new Vector3(0.53f, 1.0f, 0.82f), Quaternion.Euler(-40f, 0f, 20f), new Vector3(0.14f, 0.06f, 0.14f), M(new Color(0.15f, 0.35f, 0.3f)));
        Shapes.Create(PrimitiveType.Cylinder, "PenCup", desk, new Vector3(0.45f, 0.67f, 0.75f), new Vector3(0.07f, 0.05f, 0.07f), M(MidGrey));
        for (int i = 0; i < 3; i++)
            Shapes.Create(PrimitiveType.Cylinder, "Pen", desk, new Vector3(0.44f + i * 0.01f, 0.73f, 0.75f), Quaternion.Euler(8f * (i - 1), 0f, 10f * (i - 1)), new Vector3(0.008f, 0.07f, 0.008f), M(CodeColors[i]));
        Shapes.Create(PrimitiveType.Cube, "Nameplate", desk, new Vector3(0.6f, 0.65f, 0.4f), Quaternion.Euler(-20f, -20f, 0f), new Vector3(0.18f, 0.05f, 0.01f), M(new Color(0.3f, 0.2f, 0.1f)));
        Shapes.Create(PrimitiveType.Cube, "NameplateGold", desk, new Vector3(0.6f, 0.652f, 0.394f), Quaternion.Euler(-20f, -20f, 0f), new Vector3(0.14f, 0.025f, 0.002f), M(new Color(1f, 0.78f, 0.2f)));
        return desk;
    }

    static void BuildChair(Transform root)
    {
        Transform chair = Shapes.Pivot("Chair", root, Vector3.zero);
        Color fabric = new Color(0.15f, 0.16f, 0.2f);
        Box(chair, "Seat", new Vector3(0f, 0.2f, 0.02f), new Vector3(0.5f, 0.06f, 0.46f), fabric);
        Shapes.Create(PrimitiveType.Cube, "Back", chair, new Vector3(0f, 0.63f, -0.27f), Quaternion.Euler(-8f, 0f, 0f), new Vector3(0.48f, 0.5f, 0.06f), M(fabric));
        Box(chair, "BackSupport", new Vector3(0f, 0.3f, -0.26f), new Vector3(0.05f, 0.2f, 0.03f), DarkGrey);
        Box(chair, "BackBar", new Vector3(0f, 0.18f, -0.15f), new Vector3(0.05f, 0.03f, 0.25f), DarkGrey);
        Shapes.Create(PrimitiveType.Cylinder, "GasLift", chair, new Vector3(0f, 0.11f, 0f), new Vector3(0.05f, 0.07f, 0.05f), M(MidGrey));
        for (int i = 0; i < 5; i++)
        {
            Quaternion spoke = Quaternion.Euler(0f, i * 72f, 0f);
            Shapes.Create(PrimitiveType.Cube, "BaseLeg", chair, spoke * new Vector3(0f, 0.04f, 0.15f), spoke, new Vector3(0.04f, 0.03f, 0.3f), M(DarkGrey));
            Shapes.Create(PrimitiveType.Sphere, "Caster", chair, spoke * new Vector3(0f, 0.025f, 0.29f), Vector3.one * 0.05f, M(DarkGrey));
        }
    }

    static void BuildDecor(Transform root, OfficeScene office)
    {
        Transform decor = Shapes.Pivot("Decor", root, Vector3.zero);

        // Wall clock above the window, showing the real time
        Transform clock = Shapes.Pivot("Clock", decor, new Vector3(0f, 2.72f, 3.93f));
        Shapes.Create(PrimitiveType.Cylinder, "Rim", clock, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.42f, 0.02f, 0.42f), M(DarkGrey));
        Shapes.Create(PrimitiveType.Cylinder, "Face", clock, new Vector3(0f, 0f, -0.012f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.38f, 0.01f, 0.38f), M(Color.white));
        for (int i = 0; i < 12; i++)
        {
            Quaternion r = Quaternion.Euler(0f, 0f, -i * 30f);
            Shapes.Create(PrimitiveType.Cube, "Tick", clock, r * new Vector3(0f, 0.16f, -0.025f), r, new Vector3(0.012f, i % 3 == 0 ? 0.04f : 0.02f, 0.005f), M(DarkGrey));
        }
        office.hourHand = ClockHand(clock, 0.1f, 0.016f, -0.03f, DarkGrey);
        office.minuteHand = ClockHand(clock, 0.15f, 0.01f, -0.034f, DarkGrey);
        office.secondHand = ClockHand(clock, 0.16f, 0.004f, -0.038f, new Color(0.85f, 0.1f, 0.1f));

        // Whiteboard with a rising cheese chart
        Transform board = Shapes.Pivot("Whiteboard", decor, new Vector3(-3.93f, 1.6f, 0.8f));
        Box(board, "Frame", Vector3.zero, new Vector3(0.04f, 1.05f, 1.7f), MidGrey);
        Box(board, "Board", new Vector3(0.022f, 0f, 0f), new Vector3(0.005f, 0.95f, 1.6f), Color.white);
        for (int i = 0; i < 5; i++)
        {
            float h = 0.12f + i * 0.13f;
            Box(board, "Bar", new Vector3(0.028f, -0.4f + h / 2f, -0.55f + i * 0.25f), new Vector3(0.004f, h, 0.15f), Cheese);
        }
        Shapes.Create(PrimitiveType.Cube, "Trend", board, new Vector3(0.03f, 0.05f, 0f), Quaternion.Euler(-35f, 0f, 0f), new Vector3(0.004f, 0.015f, 1.3f), M(new Color(0.85f, 0.1f, 0.1f)));
        Box(board, "Tray", new Vector3(0.05f, -0.53f, 0f), new Vector3(0.08f, 0.02f, 1.2f), MidGrey);

        // Plants, filing cabinet, water cooler, a framed picture
        BuildPlant(decor, new Vector3(3.4f, 0f, 3.4f), 1.2f);
        BuildPlant(decor, new Vector3(-3.4f, 0f, 3.4f), 0.9f);
        Transform cabinet = Shapes.Pivot("FilingCabinet", decor, new Vector3(1.25f, 0f, 1.0f));
        Box(cabinet, "Body", new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 1.1f, 0.6f), MidGrey);
        for (int i = 0; i < 3; i++)
        {
            Box(cabinet, "Drawer", new Vector3(0f, 0.2f + i * 0.35f, -0.301f), new Vector3(0.44f, 0.3f, 0.005f), LightGrey);
            Box(cabinet, "Pull", new Vector3(0f, 0.28f + i * 0.35f, -0.31f), new Vector3(0.12f, 0.02f, 0.02f), DarkGrey);
        }
        Transform cooler = Shapes.Pivot("WaterCooler", decor, new Vector3(3.4f, 0f, -1.5f));
        Box(cooler, "Base", new Vector3(0f, 0.5f, 0f), new Vector3(0.35f, 1f, 0.35f), Color.white);
        Shapes.Create(PrimitiveType.Cylinder, "Bottle", cooler, new Vector3(0f, 1.25f, 0f), new Vector3(0.28f, 0.25f, 0.28f), M(new Color(0.45f, 0.7f, 0.95f)));
        Transform picture = Shapes.Pivot("Picture", decor, new Vector3(3.93f, 1.7f, 1.2f));
        Box(picture, "Frame", Vector3.zero, new Vector3(0.04f, 0.6f, 0.5f), Wood);
        Box(picture, "Art", new Vector3(-0.022f, 0f, 0f), new Vector3(0.005f, 0.5f, 0.4f), new Color(0.4f, 0.6f, 0.85f));
        Shapes.Create(PrimitiveType.Sphere, "ArtCheese", picture, new Vector3(-0.026f, -0.05f, 0f), new Vector3(0.005f, 0.2f, 0.2f), M(Cheese));

        // An empty cubicle in the background
        Box(decor, "Partition", new Vector3(2.2f, 0.65f, 2.0f), new Vector3(0.06f, 1.3f, 2.2f), new Color(0.5f, 0.55f, 0.62f));
        Box(decor, "Partition", new Vector3(3.1f, 0.65f, 0.9f), new Vector3(1.8f, 1.3f, 0.06f), new Color(0.5f, 0.55f, 0.62f));
        Box(decor, "OtherDesk", new Vector3(3.1f, 0.6f, 2.2f), new Vector3(1.6f, 0.04f, 0.8f), Wood);
        Box(decor, "OtherMonitor", new Vector3(3.1f, 0.85f, 2.45f), new Vector3(0.5f, 0.32f, 0.03f), DarkGrey);
    }

    static Transform ClockHand(Transform clock, float length, float width, float z, Color color)
    {
        Transform hand = Shapes.Pivot("Hand", clock, new Vector3(0f, 0f, z));
        Shapes.Create(PrimitiveType.Cube, "Pointer", hand, new Vector3(0f, length / 2f - 0.02f, 0f), new Vector3(width, length, 0.004f), M(color));
        return hand;
    }

    static void BuildPlant(Transform parent, Vector3 position, float size)
    {
        Transform plant = Shapes.Pivot("Plant", parent, position);
        Shapes.Create(PrimitiveType.Cylinder, "Pot", plant, new Vector3(0f, 0.2f * size, 0f), new Vector3(0.35f, 0.2f, 0.35f) * size, M(new Color(0.7f, 0.4f, 0.25f)));
        var random = new System.Random((int)(position.x * 10));
        for (int i = 0; i < 7; i++)
        {
            float angle = i * 51f;
            var leaf = Quaternion.Euler(-25f - (float)random.NextDouble() * 30f, angle, 0f);
            Shapes.Create(PrimitiveType.Sphere, "Leaf", plant, new Vector3(0f, 0.45f * size, 0f) + leaf * Vector3.up * 0.25f * size, leaf,
                new Vector3(0.12f, 0.4f, 0.05f) * size, M(Plant * (0.85f + (float)random.NextDouble() * 0.3f)));
        }
    }

    static void BuildLights(Transform root)
    {
        Transform lights = Shapes.Pivot("Lights", root, Vector3.zero);
        foreach (float x in new[] { -1.5f, 1.5f })
        {
            Box(lights, "CeilingPanel", new Vector3(x, 2.98f, 0.5f), new Vector3(1.2f, 0.04f, 0.6f), LampGlow);
            AddLight(lights, new Vector3(x, 2.7f, 0.5f), new Color(1f, 0.96f, 0.88f), 7f, 1.3f);
        }
        AddLight(lights, new Vector3(0.45f, 0.95f, 0.7f), new Color(1f, 0.85f, 0.6f), 1.6f, 1.2f);   // desk lamp
        AddLight(lights, new Vector3(0f, 0.98f, 0.75f), new Color(0.45f, 0.65f, 1f), 1.2f, 0.8f);    // monitor glow on the face
    }

    static void AddLight(Transform parent, Vector3 position, Color color, float range, float intensity)
    {
        var light = new GameObject("Light").AddComponent<Light>();
        light.transform.SetParent(parent, false);
        light.transform.localPosition = position;
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
    }
}
