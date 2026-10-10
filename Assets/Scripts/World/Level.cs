using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The level: an ASCII map turned into textured, lit 3D geometry (Doom-style sectors with their own
/// floor and ceiling heights), plus doors, monsters, items and lights. Also keeps the data the
/// monsters use to find their way to you, the secret areas and what the automap has revealed.
/// </summary>
public class Level : MonoBehaviour
{
    public const string Title = "E1M1: Toxin Hangar";
    public const float Cell = 3f;      // one map character = 3x3 meters
    public const float MaxStep = 0.6f; // tallest step you (and monsters) can walk up
    const float TexScale = 0.5f;       // textures repeat every 2 meters
    const int ChunkSize = 4;           // geometry is split into 4x4-cell chunks so lights stay local

    /*
     Legend
       Walls:    # brick   T tech panels   % stone   X exit switch
       Doors:    D door    R red key door   S secret door (looks like a wall)
       Floors:   . room   : tall room   - corridor   , outdoor courtyard (sky)   ~ toxic slime
                 = tech hall   + secret room   1 2 3 4 stairs (each one step higher)
       Things:   P player start   z zombieman   i imp   d demon   c cacodemon
                 h stimpack   m medikit   a clip   s shells   r rockets   A armor   k red keycard
                 G shotgun   C chaingun   L rocket launcher   o soulsphere   b barrel
                 * ceiling light (a lamp outdoors)   ! flickering light
       Things stand on the floor of the cell to their left (or right, above, below).
    */
    static readonly string[] Map =
    {
        "##############################################",
        "#########TTTTTTTTTTTTTTTTTT",
        "#########T==*====!=====*==T",
        "#########T=z===i====z==c=TTTTTT",
        "#########T=====TT=TT=====D====X",
        "#########T=b===TT=TT==s==T=*==T",
        "#########T=z======C===z==TTTTTT",
        "#########T=*==h=a=====*==T",
        "#########TTTTTTT===TTTTTTT",
        "###############T-a-T",
        "################R",
        "#######,,,,,,,,,,,,,,,,,,,,,",
        "#######,*,,,,,,,,,m,,,,,,,*,",
        "#######,,,b,,i,,,,,,,i,,b,,,",
        "#######,,,,,,~~~~~~~~,,,,,,,######%1122334444%",
        "#######,,,,,,~~~~~~~~,,z,,,,######%:::::::444%",
        "#######,,i,,,~~~~~~~~,,,i,,,D-----D::d::::4k4%",
        "#######,,,,,,~~~~~~~~,,,,,,,######%:s:::::444%",
        "#%%%%%%,,,*,,,,,,,,,,,,,*,,,######%:*::d::4i4%",
        "#%.*..%,,,,,,,,,,,,,,,,,,,,,######%:::::::444%",
        "#%.z..D,,,b,,,,z,,,,,,,b,,,,######%:m:::*:r44%",
        "#%A.G.%,,,,,,,,,,,,,,,,,,,,,######%%%%%%%%%%%%",
        "#%.s.z%,,,h,,,,,,,,,s,,,,,,,",
        "#%.*.a%,,*,,,,,,,,,,,,,,,*,,",
        "#%%%%%%#########D############",
        "################-",
        "###########+++##-",
        "###########o+LS!-",
        "###########+r+##-",
        "################-",
        "############....-....#",
        "############.*.....*.#",
        "############....P....#",
        "############.h.....a.#",
        "######################",
    };

    class Sector
    {
        public float floor, ceil;
        public string floorTex, ceilTex, upperTex;
        public bool sky, hurts, secret;

        public Sector(float floor, float ceil, string floorTex, string ceilTex, string upperTex,
            bool sky = false, bool hurts = false, bool secret = false)
        {
            this.floor = floor;
            this.ceil = ceil;
            this.floorTex = floorTex;
            this.ceilTex = ceilTex;
            this.upperTex = upperTex;
            this.sky = sky;
            this.hurts = hurts;
            this.secret = secret;
        }

        public Sector Clone() => (Sector)MemberwiseClone();
    }

    static readonly Dictionary<char, Sector> Sectors = new Dictionary<char, Sector>
    {
        { '.', new Sector(0f, 4f, "Tiles", "Ceiling", "Stone") },
        { ':', new Sector(0f, 6f, "Tiles", "Ceiling", "Stone") },
        { '-', new Sector(0f, 3.5f, "Grate", "Ceiling", "Metal") },
        { ',', new Sector(0f, 8f, "Dirt", null, "Stone", sky: true) },
        { '~', new Sector(-0.5f, 8f, "Nukage0", null, "Stone", sky: true, hurts: true) },
        { '=', new Sector(0f, 5f, "Grate", "Ceiling", "Tech") },
        { '+', new Sector(0f, 3f, "Wood", "Ceiling", "Wood", secret: true) },
        { '1', new Sector(0.4f, 6f, "Step", "Ceiling", "StepSide") },
        { '2', new Sector(0.8f, 6f, "Step", "Ceiling", "StepSide") },
        { '3', new Sector(1.2f, 6f, "Step", "Ceiling", "StepSide") },
        { '4', new Sector(1.6f, 6f, "Step", "Ceiling", "StepSide") },
    };

    static readonly Dictionary<char, string> WallTextures = new Dictionary<char, string>
    {
        { '#', "Brick" }, { 'T', "Tech" }, { '%', "Stone" }, { 'X', "SwitchOff" },
    };

    static readonly Vector2Int[] Dirs4 = { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1) };

    public static Level Current { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }
    public Vector3 PlayerStart { get; private set; }
    public int TotalMonsters { get; private set; }
    public int TotalItems { get; private set; }
    public int TotalSecrets { get; private set; }
    public bool[,] Seen { get; private set; }

    char[,] grid;
    Sector[,] sectors;
    Door[,] doors;
    int[,] secretRegion;
    bool[] secretFound;

    int[] flow;
    Vector2Int flowSource = new Vector2Int(-1, -1);
    float nextFlowUpdate, nextReveal;

    Material nukage;
    Texture2D[] nukageFrames;
    Material exitMaterial;
    bool exitUsed;

    // =====================================================================
    // Building
    // =====================================================================

    public static Level Build(Transform root)
    {
        var level = new GameObject("Level Geometry").AddComponent<Level>();
        level.transform.SetParent(root, false);
        Current = level;
        level.Parse();
        level.BuildGeometry();
        level.SpawnThings();
        return level;
    }

    void Parse()
    {
        Height = Map.Length;
        foreach (string row in Map) Width = Mathf.Max(Width, row.Length);
        grid = new char[Width, Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                grid[x, y] = x < Map[y].Length ? Map[y][x] : '#';

        sectors = new Sector[Width, Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                char c = grid[x, y];
                if (IsSolidChar(c)) continue;
                if (Sectors.TryGetValue(c, out Sector s)) sectors[x, y] = s;
                else sectors[x, y] = NeighborSector(x, y) ?? Sectors['.'];
            }

        // Door cells: as tall as the lower of the two rooms they join, with a metal underside
        doors = new Door[Width, Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (!IsDoorChar(grid[x, y])) continue;
                Sector s = sectors[x, y].Clone();
                s.sky = false;
                s.hurts = false;
                s.secret = false;
                s.ceilTex = "Metal";
                s.ceil = 99f;
                foreach (Vector2Int d in Dirs4)
                {
                    Sector n = SectorAt(x + d.x, y + d.y);
                    if (n != null && !IsDoorChar(At(x + d.x, y + d.y))) s.ceil = Mathf.Min(s.ceil, n.ceil);
                    if (IsSolidChar(At(x + d.x, y + d.y))) s.upperTex = WallTextures[At(x + d.x, y + d.y)];
                }
                if (s.ceil > 50f) s.ceil = 4f;
                sectors[x, y] = s;
            }

        // Secret areas: every connected group of secret cells counts as one secret
        secretRegion = new int[Width, Height];
        var found = new List<bool>();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                secretRegion[x, y] = -1;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (secretRegion[x, y] >= 0 || sectors[x, y] == null || !sectors[x, y].secret) continue;
                int id = found.Count;
                found.Add(false);
                var stack = new Stack<Vector2Int>();
                stack.Push(new Vector2Int(x, y));
                secretRegion[x, y] = id;
                while (stack.Count > 0)
                {
                    Vector2Int p = stack.Pop();
                    foreach (Vector2Int d in Dirs4)
                    {
                        int nx = p.x + d.x, ny = p.y + d.y;
                        Sector n = SectorAt(nx, ny);
                        if (n == null || !n.secret || secretRegion[nx, ny] >= 0) continue;
                        secretRegion[nx, ny] = id;
                        stack.Push(new Vector2Int(nx, ny));
                    }
                }
            }
        secretFound = found.ToArray();
        TotalSecrets = secretFound.Length;
        Seen = new bool[Width, Height];
        flow = new int[Width * Height];
    }

    Sector NeighborSector(int x, int y)
    {
        foreach (Vector2Int d in Dirs4)
            if (Sectors.TryGetValue(At(x + d.x, y + d.y), out Sector s)) return s;
        return null;
    }

    class MeshBuilder
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> triangles = new List<int>();

        /// <summary>a, b, c, d go clockwise as seen from the visible side.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            int i = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            for (int k = 0; k < 4; k++) normals.Add(n);
            uvs.Add(ua);
            uvs.Add(ub);
            uvs.Add(uc);
            uvs.Add(ud);
            triangles.Add(i);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
            triangles.Add(i);
            triangles.Add(i + 2);
            triangles.Add(i + 3);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    readonly Dictionary<string, MeshBuilder> builders = new Dictionary<string, MeshBuilder>();

    MeshBuilder Builder(int x, int y, string texture)
    {
        string key = (x / ChunkSize) + "," + (y / ChunkSize) + "," + texture;
        if (!builders.TryGetValue(key, out MeshBuilder b)) builders[key] = b = new MeshBuilder();
        return b;
    }

    void BuildGeometry()
    {
        var switchBuilder = new MeshBuilder();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                Sector s = sectors[x, y];
                if (s == null) continue;
                float x0 = x * Cell, x1 = x0 + Cell;
                float z0 = (Height - 1 - y) * Cell, z1 = z0 + Cell;
                char c = grid[x, y];
                bool isDoor = IsDoorChar(c);

                // Floor (seen from above) and ceiling (seen from below)
                Builder(x, y, s.floorTex).Quad(
                    new Vector3(x0, s.floor, z0), new Vector3(x0, s.floor, z1), new Vector3(x1, s.floor, z1), new Vector3(x1, s.floor, z0),
                    Uv(x0, z0), Uv(x0, z1), Uv(x1, z1), Uv(x1, z0));
                if (!s.sky && c != '!')
                {
                    string ceilTex = c == '*' ? "CeilLight" : s.ceilTex;
                    CeilingQuad(Builder(x, y, ceilTex), x0, x1, z0, z1, s.ceil);
                }

                // Walls, steps and the strips of wall above lower ceilings, facing into this cell
                for (int dir = 0; dir < 4; dir++)
                {
                    Vector2Int d = Dirs4[dir];
                    int nx = x + d.x, ny = y + d.y;
                    char nc = At(nx, ny);
                    // Edge endpoints as seen from inside this cell: left (a) and right (b)
                    Vector3 a, b;
                    switch (dir)
                    {
                        case 0: a = new Vector3(x0, 0, z0); b = new Vector3(x0, 0, z1); break;  // west wall
                        case 1: a = new Vector3(x1, 0, z1); b = new Vector3(x1, 0, z0); break;  // east wall
                        case 2: a = new Vector3(x0, 0, z1); b = new Vector3(x1, 0, z1); break;  // north wall
                        default: a = new Vector3(x1, 0, z0); b = new Vector3(x0, 0, z0); break; // south wall
                    }

                    if (IsSolidChar(nc))
                    {
                        string tex = isDoor && c != 'S' ? "DoorTrack" : WallTextures[nc];
                        MeshBuilder mb = nc == 'X' ? switchBuilder : Builder(x, y, tex);
                        Wall(mb, a, b, s.floor, s.ceil);
                        continue;
                    }

                    Sector n = sectors[nx, ny];
                    if (n.floor > s.floor) Wall(Builder(x, y, n.upperTex), a, b, s.floor, n.floor);
                    if (n.ceil < s.ceil && !(n.sky && s.sky)) Wall(Builder(x, y, n.upperTex), a, b, n.ceil, s.ceil);
                }
            }

        foreach (KeyValuePair<string, MeshBuilder> pair in builders)
        {
            string texture = pair.Key.Substring(pair.Key.LastIndexOf(',') + 1);
            CreateMeshObject("Chunk " + pair.Key, pair.Value.ToMesh(pair.Key), Gfx.Surface(texture));
        }
        builders.Clear();

        exitMaterial = Gfx.Textured(TextureLibrary.Get("SwitchOff"));
        CreateMeshObject("Exit Switch", switchBuilder.ToMesh("Exit Switch"), exitMaterial);

        nukage = Gfx.Surface("Nukage0");
        nukageFrames = new[] { TextureLibrary.Get("Nukage0"), TextureLibrary.Get("Nukage1"), TextureLibrary.Get("Nukage2") };
    }

    static Vector2 Uv(float x, float z) => new Vector2(x * TexScale, z * TexScale);

    static void CeilingQuad(MeshBuilder mb, float x0, float x1, float z0, float z1, float h) =>
        mb.Quad(new Vector3(x1, h, z0), new Vector3(x1, h, z1), new Vector3(x0, h, z1), new Vector3(x0, h, z0),
            Uv(x1, z0), Uv(x1, z1), Uv(x0, z1), Uv(x0, z0));

    static void Wall(MeshBuilder mb, Vector3 a, Vector3 b, float bottom, float top)
    {
        if (top - bottom < 0.01f) return;
        // u runs along the wall in world space so textures line up across neighboring cells
        Vector3 along = (b - a).normalized;
        float ua = Vector3.Dot(a, along) * TexScale, ub = Vector3.Dot(b, along) * TexScale;
        float vb = bottom * TexScale, vt = top * TexScale;
        mb.Quad(new Vector3(a.x, bottom, a.z), new Vector3(a.x, top, a.z), new Vector3(b.x, top, b.z), new Vector3(b.x, bottom, b.z),
            new Vector2(ua, vb), new Vector2(ua, vt), new Vector2(ub, vt), new Vector2(ub, vb));
    }

    void CreateMeshObject(string name, Mesh mesh, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    void SpawnThings()
    {
        Transform root = GameManager.WorldRoot;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                Sector s = sectors[x, y];
                if (s == null) continue;
                char c = grid[x, y];
                Vector3 pos = CellCenter(x, y);

                if (IsDoorChar(c))
                {
                    bool spanX = IsSolidChar(At(x - 1, y)) && IsSolidChar(At(x + 1, y)); // walls left/right: door runs along x
                    Door.Kind kind = c == 'R' ? Door.Kind.Red : c == 'S' ? Door.Kind.Secret : Door.Kind.Normal;
                    string wallTex = WallTextures[spanX ? At(x - 1, y) : At(x, y - 1)];
                    doors[x, y] = Door.Create(transform, pos, spanX, s.floor, s.ceil, kind, wallTex);
                    continue;
                }

                switch (c)
                {
                    case 'P': PlayerStart = pos; break;
                    case 'z': Monster.Spawn(MonsterType.Zombie, pos); TotalMonsters++; break;
                    case 'i': Monster.Spawn(MonsterType.Imp, pos); TotalMonsters++; break;
                    case 'd': Monster.Spawn(MonsterType.Demon, pos); TotalMonsters++; break;
                    case 'c': Monster.Spawn(MonsterType.Caco, pos); TotalMonsters++; break;
                    case 'h': Item(Pickup.Kind.Stimpack, pos); break;
                    case 'm': Item(Pickup.Kind.Medkit, pos); break;
                    case 'a': Item(Pickup.Kind.Clip, pos); break;
                    case 's': Item(Pickup.Kind.Shells, pos); break;
                    case 'r': Item(Pickup.Kind.Rockets, pos); break;
                    case 'A': Item(Pickup.Kind.Armor, pos); break;
                    case 'k': Item(Pickup.Kind.RedKey, pos); break;
                    case 'G': Item(Pickup.Kind.Shotgun, pos); break;
                    case 'C': Item(Pickup.Kind.Chaingun, pos); break;
                    case 'L': Item(Pickup.Kind.Launcher, pos); break;
                    case 'o': Item(Pickup.Kind.Soulsphere, pos); break;
                    case 'b': Barrel.Spawn(pos); break;
                    case '*':
                        if (s.sky) Lamp(pos, root);
                        else Lighting.Create("Ceiling Light", root, pos + Vector3.up * (s.ceil - s.floor - 0.3f),
                            new Color(1f, 0.93f, 0.8f), 11f, 1.7f);
                        break;
                    case '!':
                        FlickeringLight(x, y, pos, s, root);
                        break;
                }

                // Toxic slime glows
                if (s.hurts && x % 3 == 0 && y % 2 == 0)
                    Lighting.Create("Slime Glow", root, pos + Vector3.up * 0.8f, new Color(0.4f, 1f, 0.3f), 6f, 1f);
            }
    }

    void Item(Pickup.Kind kind, Vector3 pos)
    {
        Pickup.Spawn(kind, pos);
        TotalItems++;
    }

    static void Lamp(Vector3 pos, Transform root)
    {
        Billboard bb = Billboard.Create("Lamp", root, pos, SpriteLibrary.Lamp[0], true);
        var col = bb.gameObject.AddComponent<CapsuleCollider>();
        col.radius = 0.25f;
        col.height = 2.2f;
        col.center = new Vector3(0f, 1.1f, 0f);
        Lighting.Create("Lamp Light", bb.transform, pos + Vector3.up * 2.1f, new Color(1f, 0.85f, 0.6f), 11f, 1.9f);
    }

    void FlickeringLight(int x, int y, Vector3 pos, Sector s, Transform root)
    {
        // Its ceiling panel gets its own material so it can flicker with the light
        var mb = new MeshBuilder();
        float x0 = x * Cell, z0 = (Height - 1 - y) * Cell;
        CeilingQuad(mb, x0, x0 + Cell, z0, z0 + Cell, s.ceil);
        Material panel = Gfx.Textured(TextureLibrary.Get("CeilLight"), true);
        CreateMeshObject("Flickering Panel", mb.ToMesh("Flickering Panel"), panel);

        Light light = Lighting.Create("Flickering Light", root, pos + Vector3.up * (s.ceil - s.floor - 0.3f),
            new Color(1f, 0.9f, 0.75f), 10f, 1.6f);
        light.gameObject.AddComponent<FlickerLight>().glowMaterial = panel;
    }

    // =====================================================================
    // Queries
    // =====================================================================

    static bool IsSolidChar(char c) => c == '#' || c == 'T' || c == '%' || c == 'X';
    static bool IsDoorChar(char c) => c == 'D' || c == 'R' || c == 'S';

    char At(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height ? grid[x, y] : '#';
    Sector SectorAt(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height ? sectors[x, y] : null;

    public bool IsOpen(int x, int y) => SectorAt(x, y) != null;
    public bool IsWall(int x, int y) => IsSolidChar(At(x, y));
    public Door DoorAt(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height ? doors[x, y] : null;

    public Vector2Int CellOf(Vector3 p) =>
        new Vector2Int(Mathf.FloorToInt(p.x / Cell), Height - 1 - Mathf.FloorToInt(p.z / Cell));

    /// <summary>Center of a cell, at floor height.</summary>
    public Vector3 CellCenter(int x, int y)
    {
        Sector s = SectorAt(x, y);
        return new Vector3((x + 0.5f) * Cell, s != null ? s.floor : 0f, (Height - 1 - y + 0.5f) * Cell);
    }

    public float FloorAt(Vector3 p)
    {
        Vector2Int c = CellOf(p);
        Sector s = SectorAt(c.x, c.y);
        return s != null ? s.floor : 0f;
    }

    public float CeilingAt(Vector3 p)
    {
        Vector2Int c = CellOf(p);
        Sector s = SectorAt(c.x, c.y);
        return s != null ? s.ceil : 4f;
    }

    public bool HurtsAt(Vector3 p)
    {
        Vector2Int c = CellOf(p);
        Sector s = SectorAt(c.x, c.y);
        return s != null && s.hurts;
    }

    /// <summary>Called when the player presses Use on a wall. Returns true if it was the exit switch.</summary>
    public bool TryUseWall(Vector3 point, Vector3 normal)
    {
        Vector2Int c = CellOf(point - normal * 0.2f);
        if (At(c.x, c.y) != 'X') return false;
        if (exitUsed) return true;
        exitUsed = true;
        exitMaterial.mainTexture = TextureLibrary.Get("SwitchOn");
        Gfx.SetGlow(exitMaterial, exitMaterial.mainTexture, 0.4f);
        Sfx.PlayAt(Sfx.SwitchClick, point);
        GameManager.Instance.CompleteLevel();
        return true;
    }

    /// <summary>
    /// Where a monster at <paramref name="from"/> should walk next to get closer to you when it can't see you.
    /// Uses a distance map that is flooded out from your position through the open cells and doors.
    /// </summary>
    public bool NextStep(Vector3 from, out Vector3 waypoint, out Door closedDoor)
    {
        waypoint = from;
        closedDoor = null;
        Vector2Int c = CellOf(from);
        if (!InBounds(c.x, c.y)) return false;
        int here = flow[c.y * Width + c.x];
        if (here <= 0) return false;

        int best = here;
        Vector2Int bestCell = c;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = c.x + dx, ny = c.y + dy;
                if (!InBounds(nx, ny)) continue;
                if (dx != 0 && dy != 0 && (!Passable(c.x + dx, c.y) || !Passable(c.x, c.y + dy))) continue; // no corner cutting
                int d = flow[ny * Width + nx];
                if (d < 0 || d >= best) continue;
                best = d;
                bestCell = new Vector2Int(nx, ny);
            }
        if (bestCell == c) return false;
        waypoint = CellCenter(bestCell.x, bestCell.y);
        Door door = DoorAt(bestCell.x, bestCell.y);
        if (door && !door.IsOpen) closedDoor = door;
        return true;
    }

    bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    bool Passable(int x, int y)
    {
        if (!IsOpen(x, y)) return false;
        Door door = doors[x, y];
        return !door || door.MonstersCanPass;
    }

    void UpdateFlow(Vector2Int source)
    {
        for (int i = 0; i < flow.Length; i++) flow[i] = -1;
        if (!InBounds(source.x, source.y) || !IsOpen(source.x, source.y)) return;
        var queue = new Queue<Vector2Int>();
        flow[source.y * Width + source.x] = 0;
        queue.Enqueue(source);
        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            int d = flow[p.y * Width + p.x];
            foreach (Vector2Int dir in Dirs4)
            {
                int nx = p.x + dir.x, ny = p.y + dir.y;
                if (!InBounds(nx, ny) || flow[ny * Width + nx] >= 0 || !Passable(nx, ny)) continue;
                // A monster would walk from n to p, so it has to be able to climb that step
                if (sectors[p.x, p.y].floor - sectors[nx, ny].floor > MaxStep) continue;
                flow[ny * Width + nx] = d + 1;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }
    }

    // =====================================================================
    // Per-frame
    // =====================================================================

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    void Update()
    {
        if (nukage) nukage.mainTexture = nukageFrames[Mathf.FloorToInt(Time.time * 3f) % nukageFrames.Length];
        if (nukage) nukage.SetTexture("_EmissionMap", nukage.mainTexture);

        Player player = Player.Instance;
        if (!player) return;
        Vector2Int cell = CellOf(player.transform.position);

        if (cell != flowSource || Time.time >= nextFlowUpdate)
        {
            flowSource = cell;
            nextFlowUpdate = Time.time + 0.5f;
            UpdateFlow(cell);
        }

        if (Time.time >= nextReveal)
        {
            nextReveal = Time.time + 0.15f;
            Reveal(player.transform.position);
        }

        if (InBounds(cell.x, cell.y))
        {
            int region = secretRegion[cell.x, cell.y];
            if (region >= 0 && !secretFound[region])
            {
                secretFound[region] = true;
                GameManager.Instance.secrets++;
                GameManager.Instance.Message("You found a secret area!");
                Sfx.Play2D(Sfx.PowerUp, 0.6f);
            }
        }
    }

    /// <summary>Marks the cells you can see on the automap by casting rays across the grid.</summary>
    void Reveal(Vector3 from)
    {
        var start = new Vector2(from.x / Cell, Height - from.z / Cell);
        for (int i = 0; i < 72; i++)
        {
            float a = i * Mathf.PI * 2f / 72f;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            for (float t = 0f; t < 14f; t += 0.25f)
            {
                Vector2 p = start + dir * t;
                int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
                if (!InBounds(x, y)) break;
                Seen[x, y] = true;
                if (!IsOpen(x, y)) break;
                Door door = doors[x, y];
                if (door && !door.IsOpen && t > 0.5f) break;
            }
        }
    }

    // Automap helpers
    public enum MapCell { Unknown, Open, Wall, Door, LockedDoor, Exit }

    public MapCell AutomapCell(int x, int y)
    {
        if (!InBounds(x, y) || !Seen[x, y]) return MapCell.Unknown;
        char c = grid[x, y];
        if (c == 'X') return MapCell.Exit;
        if (c == 'R') return MapCell.LockedDoor;
        if (c == 'D') return MapCell.Door;
        if (IsSolidChar(c) || c == 'S') return MapCell.Wall;
        return MapCell.Open;
    }
}
