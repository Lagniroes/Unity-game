using UnityEngine;

/// <summary>Health, armor, ammo, weapons and keys lying around the level. Walk over them to pick them up.</summary>
public class Pickup : MonoBehaviour
{
    public enum Kind { Stimpack, Medkit, Clip, Shells, Rockets, Armor, RedKey, Shotgun, Chaingun, Launcher, Soulsphere }

    Kind kind;
    bool dropped; // dropped by a monster: gives less and doesn't count towards the item total
    Billboard billboard;
    Sprite[] frames;

    public static Pickup Spawn(Kind kind, Vector3 position, bool dropped = false)
    {
        Sprite[] frames = Frames(kind);
        Billboard bb = Billboard.Create(kind.ToString(), GameManager.WorldRoot, position, frames[0], kind == Kind.Soulsphere);
        bb.lightHeight = 0.3f;
        var p = bb.gameObject.AddComponent<Pickup>();
        p.kind = kind;
        p.dropped = dropped;
        p.billboard = bb;
        p.frames = frames;
        return p;
    }

    static Sprite[] Frames(Kind kind)
    {
        switch (kind)
        {
            case Kind.Stimpack: return new[] { SpriteLibrary.Stimpack };
            case Kind.Medkit: return new[] { SpriteLibrary.Medkit };
            case Kind.Clip: return new[] { SpriteLibrary.Clip };
            case Kind.Shells: return new[] { SpriteLibrary.Shells };
            case Kind.Rockets: return new[] { SpriteLibrary.Rockets };
            case Kind.Armor: return new[] { SpriteLibrary.Armor };
            case Kind.RedKey: return SpriteLibrary.RedKey;
            case Kind.Shotgun: return new[] { SpriteLibrary.ShotgunPickup };
            case Kind.Chaingun: return new[] { SpriteLibrary.ChaingunPickup };
            case Kind.Launcher: return new[] { SpriteLibrary.LauncherPickup };
            default: return SpriteLibrary.Soulsphere;
        }
    }

    void Update()
    {
        if (frames.Length > 1) billboard.Sprite = frames[Mathf.FloorToInt(Time.time * 4f) % frames.Length];

        Player player = Player.Instance;
        if (!player || player.IsDead) return;
        Vector3 d = player.transform.position - transform.position;
        if (Mathf.Abs(d.y) > 1.2f || new Vector2(d.x, d.z).sqrMagnitude > 0.9f * 0.9f) return;
        if (!Apply(player)) return;

        if (!dropped) GameManager.Instance.items++;
        Destroy(gameObject);
    }

    bool Apply(Player p)
    {
        WeaponSystem w = p.Weapons;
        switch (kind)
        {
            case Kind.Stimpack:
                if (!p.Heal(10, 100)) return false;
                return Got("Picked up a stimpack.", Sfx.ItemPickup);
            case Kind.Medkit:
                bool needed = p.health < 25;
                if (!p.Heal(25, 100)) return false;
                return Got(needed ? "Picked up a medikit that you REALLY need!" : "Picked up a medikit.", Sfx.ItemPickup);
            case Kind.Clip:
                if (!w.GiveAmmo(AmmoType.Bullets, dropped ? 5 : 10)) return false;
                return Got("Picked up a clip.", Sfx.ItemPickup);
            case Kind.Shells:
                if (!w.GiveAmmo(AmmoType.Shells, 4)) return false;
                return Got("Picked up 4 shotgun shells.", Sfx.ItemPickup);
            case Kind.Rockets:
                if (!w.GiveAmmo(AmmoType.Rockets, 5)) return false;
                return Got("Picked up a box of rockets.", Sfx.ItemPickup);
            case Kind.Armor:
                if (p.armor >= 100) return false;
                p.armor = 100;
                return Got("Picked up the armor.", Sfx.ItemPickup);
            case Kind.RedKey:
                p.hasRedKey = true;
                return Got("Picked up a red keycard.", Sfx.WeaponPickup, true);
            case Kind.Shotgun:
                w.GiveWeapon(WeaponId.Shotgun, AmmoType.Shells, 8);
                return Got("You got the shotgun!", Sfx.WeaponPickup, true);
            case Kind.Chaingun:
                w.GiveWeapon(WeaponId.Chaingun, AmmoType.Bullets, 20);
                return Got("You got the chaingun!", Sfx.WeaponPickup, true);
            case Kind.Launcher:
                w.GiveWeapon(WeaponId.Launcher, AmmoType.Rockets, 2);
                return Got("You got the rocket launcher!", Sfx.WeaponPickup, true);
            case Kind.Soulsphere:
                p.health = Mathf.Min(200, p.health + 100);
                return Got("Supercharge!", Sfx.PowerUp, true);
        }
        return false;
    }

    static bool Got(string message, AudioClip sound, bool grin = false)
    {
        GameManager.Instance.Message(message);
        Sfx.Play2D(sound);
        Player.Instance.OnPickup(grin);
        return true;
    }
}
