using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds a dusk-lit city: a grid of streets with lane markings and crosswalks, sidewalks,
/// buildings with lit windows, streetlights, parked cars, and alley junk. The central
/// intersection (around the origin) is the fighting area.
/// </summary>
public static class CityBuilder
{
    const float BlockSize = 30f;
    const float BlockPitch = 42f;          // block + 12 m road
    const float RoadHalfWidth = (BlockPitch - BlockSize) / 2f;
    static readonly float[] BlockCenters = { -63f, -21f, 21f, 63f };
    static readonly float[] RoadCenters = { -84f, -42f, 0f, 42f, 84f };

    static readonly Color Asphalt = new Color(0.17f, 0.17f, 0.19f);
    static readonly Color Sidewalk = new Color(0.55f, 0.54f, 0.52f);
    static readonly Color LaneYellow = new Color(0.95f, 0.78f, 0.2f);
    static readonly Color PaintWhite = new Color(0.9f, 0.9f, 0.88f);
    static readonly Color Glass = new Color(0.12f, 0.16f, 0.24f);
    static readonly Color LitWindow = new Color(1f, 0.82f, 0.5f);
    static readonly Color Metal = new Color(0.2f, 0.21f, 0.22f);
    static readonly Color Tire = new Color(0.07f, 0.07f, 0.07f);

    static readonly Color[] BuildingColors =
    {
        new Color(0.55f, 0.3f, 0.25f),   // red brick
        new Color(0.62f, 0.55f, 0.45f),  // sandstone
        new Color(0.35f, 0.38f, 0.42f),  // concrete
        new Color(0.45f, 0.4f, 0.5f),    // dusty purple
        new Color(0.3f, 0.42f, 0.45f),   // teal
        new Color(0.7f, 0.65f, 0.58f),   // beige
    };

    static readonly Color[] CarColors =
    {
        new Color(0.75f, 0.1f, 0.1f), new Color(0.1f, 0.25f, 0.6f), new Color(0.9f, 0.9f, 0.9f),
        new Color(0.1f, 0.1f, 0.1f), new Color(0.95f, 0.75f, 0.1f), new Color(0.2f, 0.5f, 0.3f),
    };

    public static void Build()
    {
        var city = new GameObject("City").transform;
        var random = new System.Random(7);

        SetupLighting();

        Shapes.Create(PrimitiveType.Plane, "Street", city, Vector3.zero, new Vector3(24f, 1f, 24f), Shapes.Shared(Asphalt), true);

        foreach (float x in BlockCenters)
            foreach (float z in BlockCenters)
                BuildBlock(city, new Vector3(x, 0f, z), random);

        BuildRoadMarkings(city);
        BuildCrosswalks(city, Vector3.zero);
        BuildStreetProps(city, random);
    }

    static void SetupLighting()
    {
        Color fog = new Color(0.45f, 0.35f, 0.45f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 35f;
        RenderSettings.fogEndDistance = 160f;
        RenderSettings.fogColor = fog;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.33f, 0.42f);

        Light sun = null;
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional) sun = light;
        if (!sun)
        {
            sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.color = new Color(1f, 0.62f, 0.4f);   // low orange sunset
        sun.intensity = 1.1f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(22f, -40f, 0f);

        Camera cam = Camera.main;
        if (cam)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = fog;
            cam.farClipPlane = 250f;
        }
    }

    static void BuildBlock(Transform city, Vector3 center, System.Random random)
    {
        Transform block = Shapes.Pivot("Block", city, center);
        Shapes.Create(PrimitiveType.Cube, "Sidewalk", block, new Vector3(0f, 0.1f, 0f), new Vector3(BlockSize, 0.2f, BlockSize), Shapes.Shared(Sidewalk), true);

        // Four buildings per block, leaving 3 m of sidewalk around the edge
        for (int ix = -1; ix <= 1; ix += 2)
        {
            for (int iz = -1; iz <= 1; iz += 2)
            {
                float width = 10f + (float)random.NextDouble() * 1.5f;
                float depth = 10f + (float)random.NextDouble() * 1.5f;
                float height = 8f + (float)random.NextDouble() * 28f;
                Color color = BuildingColors[random.Next(BuildingColors.Length)];
                BuildBuilding(block, new Vector3(ix * 6.5f, 0.2f, iz * 6.5f), new Vector3(width, height, depth), color, random);
            }
        }

        // Streetlights on the four corners
        for (int ix = -1; ix <= 1; ix += 2)
            for (int iz = -1; iz <= 1; iz += 2)
                BuildStreetLight(block, new Vector3(ix * 14f, 0.2f, iz * 14f), Quaternion.Euler(0f, ix > 0 ? 90f : -90f, 0f));
    }

    static void BuildBuilding(Transform parent, Vector3 basePosition, Vector3 size, Color color, System.Random random)
    {
        // Unscaled pivot at the building's center so child positions are in meters
        Transform building = Shapes.Pivot("Building", parent, basePosition + Vector3.up * size.y / 2f);
        Shapes.Create(PrimitiveType.Cube, "Walls", building, Vector3.zero, size, Shapes.Shared(color), true);

        // Roof ledge
        Shapes.Create(PrimitiveType.Cube, "Roof", building, Vector3.up * (size.y / 2f + 0.15f),
            new Vector3(size.x + 0.4f, 0.3f, size.z + 0.4f), Shapes.Shared(color * 0.8f));

        // A strip of windows on every floor of every face; some are lit
        for (float y = 3f; y < size.y - 1.5f; y += 3.5f)
        {
            float localY = y - size.y / 2f;
            for (int face = 0; face < 4; face++)
            {
                bool alongX = face < 2;
                float sign = face % 2 == 0 ? 1f : -1f;
                float faceWidth = (alongX ? size.x : size.z) - 1.5f;
                Vector3 offset = alongX
                    ? new Vector3(0f, localY, sign * (size.z / 2f + 0.04f))
                    : new Vector3(sign * (size.x / 2f + 0.04f), localY, 0f);
                Vector3 scale = alongX ? new Vector3(faceWidth, 1.4f, 0.1f) : new Vector3(0.1f, 1.4f, faceWidth);
                Color glass = random.NextDouble() < 0.3 ? LitWindow : Glass;
                Shapes.Create(PrimitiveType.Cube, "Windows", building, offset, scale, Shapes.Shared(glass));
            }
        }
    }

    static void BuildStreetLight(Transform parent, Vector3 position, Quaternion rotation)
    {
        Transform pole = Shapes.Pivot("StreetLight", parent, position);
        pole.localRotation = rotation;
        Shapes.Create(PrimitiveType.Cylinder, "Pole", pole, new Vector3(0f, 3f, 0f), new Vector3(0.18f, 3f, 0.18f), Shapes.Shared(Metal), true);
        Shapes.Create(PrimitiveType.Cube, "Arm", pole, new Vector3(0f, 5.9f, 0.7f), new Vector3(0.12f, 0.12f, 1.5f), Shapes.Shared(Metal));
        Shapes.Create(PrimitiveType.Cube, "Lamp", pole, new Vector3(0f, 5.8f, 1.35f), new Vector3(0.45f, 0.15f, 0.6f), Shapes.Shared(LitWindow));
    }

    static void BuildRoadMarkings(Transform city)
    {
        Transform markings = Shapes.Pivot("RoadMarkings", city, Vector3.zero);
        Material yellow = Shapes.Shared(LaneYellow);

        foreach (float road in RoadCenters)
        {
            for (float t = -110f; t <= 110f; t += 5f)
            {
                if (NearRoad(t)) continue; // no dashes through intersections
                Shapes.Create(PrimitiveType.Cube, "Dash", markings, new Vector3(road, 0.01f, t), new Vector3(0.25f, 0.02f, 2.4f), yellow);
                Shapes.Create(PrimitiveType.Cube, "Dash", markings, new Vector3(t, 0.01f, road), new Vector3(2.4f, 0.02f, 0.25f), yellow);
            }
        }
    }

    static bool NearRoad(float coordinate)
    {
        foreach (float road in RoadCenters)
            if (Mathf.Abs(coordinate - road) < RoadHalfWidth + 1f) return true;
        return false;
    }

    static void BuildCrosswalks(Transform city, Vector3 intersection)
    {
        Transform crosswalks = Shapes.Pivot("Crosswalks", city, intersection);
        Material white = Shapes.Shared(PaintWhite);
        float edge = RoadHalfWidth + 1.8f;
        for (float s = -RoadHalfWidth + 1f; s <= RoadHalfWidth - 1f; s += 1.3f)
        {
            Shapes.Create(PrimitiveType.Cube, "Stripe", crosswalks, new Vector3(s, 0.012f, edge), new Vector3(0.6f, 0.02f, 3f), white);
            Shapes.Create(PrimitiveType.Cube, "Stripe", crosswalks, new Vector3(s, 0.012f, -edge), new Vector3(0.6f, 0.02f, 3f), white);
            Shapes.Create(PrimitiveType.Cube, "Stripe", crosswalks, new Vector3(edge, 0.012f, s), new Vector3(3f, 0.02f, 0.6f), white);
            Shapes.Create(PrimitiveType.Cube, "Stripe", crosswalks, new Vector3(-edge, 0.012f, s), new Vector3(3f, 0.02f, 0.6f), white);
        }
    }

    static void BuildStreetProps(Transform city, System.Random random)
    {
        Transform props = Shapes.Pivot("Props", city, Vector3.zero);

        // Parked cars along the curbs near the fight
        float[] spots = { 14f, 22f, 30f, -14f, -24f, -32f };
        foreach (float spot in spots)
        {
            BuildCar(props, new Vector3(4.3f, 0f, spot), 0f, CarColors[random.Next(CarColors.Length)]);
            BuildCar(props, new Vector3(-4.3f, 0f, spot + 3f), 180f, CarColors[random.Next(CarColors.Length)]);
            BuildCar(props, new Vector3(spot, 0f, -4.3f), 90f, CarColors[random.Next(CarColors.Length)]);
        }

        // Alley junk on the corners: dumpsters, trash bags, crates and a burning barrel
        BuildDumpster(props, new Vector3(7.8f, 0.2f, 12f), 90f);
        BuildDumpster(props, new Vector3(-12f, 0.2f, -7.8f), 0f);
        BuildTrashPile(props, new Vector3(7.8f, 0.2f, 17f), random);
        BuildTrashPile(props, new Vector3(-16f, 0.2f, -7.8f), random);
        BuildTrashPile(props, new Vector3(-7.8f, 0.2f, 14f), random);
        BuildCrates(props, new Vector3(7f, 0.2f, -12f));
        BuildFireBarrel(props, new Vector3(-8f, 0.2f, 9.5f));
    }

    static void BuildCar(Transform parent, Vector3 position, float yaw, Color paint)
    {
        Transform car = Shapes.Pivot("Car", parent, position);
        car.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Shapes.Create(PrimitiveType.Cube, "Body", car, new Vector3(0f, 0.6f, 0f), new Vector3(1.9f, 0.7f, 4.3f), Shapes.Shared(paint), true);
        Shapes.Create(PrimitiveType.Cube, "Cabin", car, new Vector3(0f, 1.2f, -0.2f), new Vector3(1.7f, 0.6f, 2.2f), Shapes.Shared(Glass), true);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Shapes.Create(PrimitiveType.Cylinder, "Wheel", car, new Vector3(sx * 0.92f, 0.35f, sz * 1.35f),
                    Quaternion.Euler(0f, 0f, 90f), new Vector3(0.7f, 0.12f, 0.7f), Shapes.Shared(Tire));
            }
            Shapes.Create(PrimitiveType.Cube, "Headlight", car, new Vector3(sx * 0.65f, 0.7f, 2.16f), new Vector3(0.35f, 0.15f, 0.05f), Shapes.Shared(LitWindow));
            Shapes.Create(PrimitiveType.Cube, "Taillight", car, new Vector3(sx * 0.65f, 0.7f, -2.16f), new Vector3(0.35f, 0.15f, 0.05f), Shapes.Shared(new Color(0.8f, 0.05f, 0.05f)));
        }
    }

    static void BuildDumpster(Transform parent, Vector3 position, float yaw)
    {
        Transform dumpster = Shapes.Pivot("Dumpster", parent, position);
        dumpster.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Color green = new Color(0.15f, 0.35f, 0.2f);
        Shapes.Create(PrimitiveType.Cube, "Bin", dumpster, new Vector3(0f, 0.65f, 0f), new Vector3(2.4f, 1.3f, 1.3f), Shapes.Shared(green), true);
        Shapes.Create(PrimitiveType.Cube, "Lid", dumpster, new Vector3(0f, 1.35f, -0.1f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(2.5f, 0.08f, 1.35f), Shapes.Shared(green * 0.7f));
    }

    static void BuildTrashPile(Transform parent, Vector3 position, System.Random random)
    {
        Material bag = Shapes.Shared(new Color(0.08f, 0.08f, 0.1f));
        for (int i = 0; i < 5; i++)
        {
            var offset = new Vector3((float)random.NextDouble() * 1.6f - 0.8f, 0.3f, (float)random.NextDouble() * 1.6f - 0.8f);
            float size = 0.6f + (float)random.NextDouble() * 0.3f;
            Shapes.Create(PrimitiveType.Sphere, "TrashBag", parent, position + offset, new Vector3(size, size * 0.8f, size), bag);
        }
    }

    static void BuildCrates(Transform parent, Vector3 position)
    {
        Material wood = Shapes.Shared(new Color(0.55f, 0.38f, 0.2f));
        Shapes.Create(PrimitiveType.Cube, "Crate", parent, position + new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f), wood, true);
        Shapes.Create(PrimitiveType.Cube, "Crate", parent, position + new Vector3(1.1f, 0.5f, 0.2f), Quaternion.Euler(0f, 20f, 0f), new Vector3(1f, 1f, 1f), wood, true);
        Shapes.Create(PrimitiveType.Cube, "Crate", parent, position + new Vector3(0.5f, 1.5f, 0.1f), Quaternion.Euler(0f, -10f, 0f), new Vector3(1f, 1f, 1f), wood, true);
    }

    static void BuildFireBarrel(Transform parent, Vector3 position)
    {
        Shapes.Create(PrimitiveType.Cylinder, "Barrel", parent, position + new Vector3(0f, 0.5f, 0f), new Vector3(0.7f, 0.5f, 0.7f),
            Shapes.Shared(new Color(0.4f, 0.22f, 0.12f)), true);
        Shapes.Create(PrimitiveType.Sphere, "Fire", parent, position + new Vector3(0f, 1.05f, 0f), new Vector3(0.55f, 0.5f, 0.55f),
            Shapes.Shared(new Color(1f, 0.5f, 0.1f)));

        var fireLight = new GameObject("FireLight").AddComponent<Light>();
        fireLight.transform.SetParent(parent, false);
        fireLight.transform.localPosition = position + new Vector3(0f, 1.6f, 0f);
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1f, 0.55f, 0.2f);
        fireLight.range = 9f;
        fireLight.intensity = 2f;
    }
}
