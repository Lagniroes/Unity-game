using UnityEngine;

/// <summary>
/// A sliding door that rises into the ceiling. Normal doors close again after a few seconds
/// (and pop back open if something is in the way), red doors need the red keycard, and
/// secret doors look exactly like the wall around them and stay open.
/// </summary>
public class Door : MonoBehaviour
{
    public enum Kind { Normal, Red, Secret }

    const float Speed = 3.5f;
    const float WaitTime = 4f;

    Kind kind;
    Transform slab;
    float closedY, openY;
    int direction;     // 1 opening, -1 closing, 0 stopped
    float closeAt = -1f;
    Bounds area;

    /// <summary>0 = closed, 1 = fully open.</summary>
    public float OpenAmount => Mathf.InverseLerp(closedY, openY, slab.localPosition.y);
    public bool IsOpen => OpenAmount > 0.9f;
    public bool MonstersCanPass => kind == Kind.Normal || IsOpen;

    public static Door Create(Transform parent, Vector3 floorCenter, bool spanX, float floor, float ceil, Kind kind, string wallTexture)
    {
        var go = new GameObject(kind + " Door");
        go.transform.SetParent(parent, false);
        go.transform.position = floorCenter;
        var door = go.AddComponent<Door>();
        door.kind = kind;

        float height = ceil - floor;
        float thickness = kind == Kind.Secret ? Level.Cell : 0.5f;
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Slab";
        door.slab = cube.transform;
        door.slab.SetParent(go.transform, false);
        door.slab.localScale = spanX ? new Vector3(Level.Cell, height, thickness) : new Vector3(thickness, height, Level.Cell);
        door.closedY = height * 0.5f;
        door.openY = height * 1.5f - 0.15f;
        door.slab.localPosition = new Vector3(0f, door.closedY, 0f);

        Material material;
        if (kind == Kind.Secret)
        {
            // Same texture and scale as the surrounding walls so it blends in
            material = Gfx.Textured(TextureLibrary.Get(wallTexture));
            material.mainTextureScale = new Vector2(Level.Cell * 0.5f, height * 0.5f);
        }
        else material = Gfx.Surface(kind == Kind.Red ? "RedDoor" : "Door");
        var mr = cube.GetComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        door.area = new Bounds(floorCenter + Vector3.up * height * 0.5f, new Vector3(Level.Cell + 0.8f, height, Level.Cell + 0.8f));
        return door;
    }

    /// <summary>Tries to open the door. Returns false if it's locked for whoever is trying.</summary>
    public bool Use(bool byPlayer)
    {
        if (kind == Kind.Red)
        {
            if (!byPlayer) return false;
            if (!Player.Instance.hasRedKey)
            {
                GameManager.Instance.Message("You need a red key to open this door");
                Sfx.Play2D(Sfx.NoWay);
                return false;
            }
        }
        if (direction == 1 || (IsOpen && direction == 0))
        {
            closeAt = Time.time + WaitTime;
            return true;
        }
        direction = 1;
        Sfx.PlayAt(Sfx.DoorOpen, slab.position, 0.8f);
        return true;
    }

    void Update()
    {
        if (direction == 0)
        {
            if (kind != Kind.Secret && closeAt > 0f && Time.time >= closeAt)
            {
                if (SomethingInside()) closeAt = Time.time + 1f;
                else
                {
                    direction = -1;
                    closeAt = -1f;
                    Sfx.PlayAt(Sfx.DoorClose, slab.position, 0.8f);
                }
            }
            return;
        }

        if (direction == -1 && SomethingInside())
        {
            direction = 1; // bounce back up like Doom doors do
            Sfx.PlayAt(Sfx.DoorOpen, slab.position, 0.8f);
        }

        Vector3 p = slab.localPosition;
        float target = direction > 0 ? openY : closedY;
        p.y = Mathf.MoveTowards(p.y, target, Speed * Time.deltaTime);
        slab.localPosition = p;
        if (Mathf.Approximately(p.y, target))
        {
            if (direction > 0) closeAt = Time.time + WaitTime;
            direction = 0;
        }
    }

    bool SomethingInside()
    {
        Player player = Player.Instance;
        if (player && !player.IsDead && area.Contains(player.Center)) return true;
        foreach (Monster m in Monster.All)
            if (!m.IsDead && area.Contains(m.Center)) return true;
        return false;
    }
}
