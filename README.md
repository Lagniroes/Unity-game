# Toxin Hangar (Unity)

A first-person shooter in the style of the original **Doom**. You fight your way through a
toxic UAC base full of zombiemen, imps, demons and a cacodemon, find the red keycard and hit
the exit switch. Everything (the level, every texture, every sprite, the weapons, the status
bar face and all the sound effects) is generated from code, so the project has no art or audio files.

## How to open and play

1. On GitHub click the green **Code** button → **Download ZIP**, and unzip it.
2. In Unity Hub click **Add → Add project from disk** and select the unzipped folder
   (the one that contains `Assets`, `Packages` and `ProjectSettings`).
3. Open it. If Hub says the editor version isn't installed, pick the Unity version you
   have (Unity 6 or 2022.3 LTS) and confirm.
4. Press **Play** ▶. The game builds itself in whatever scene is open, even an empty one.

## Controls

| Key | Action |
| --- | --- |
| W A S D / arrow keys | Move and strafe |
| Mouse | Look around |
| Hold Shift | Run |
| Left click / Ctrl | Fire |
| E / Space | Open doors, press switches |
| 1 – 5, mouse wheel | Fist, pistol, shotgun, chaingun, rocket launcher |
| Tab / M | Automap |
| P | Toggle the chunky retro pixels |
| R | Restart the level |
| Esc / click | Free / lock the mouse cursor |

## What's in it

- **The level, E1M1: Toxin Hangar.** A start room, a sky-lit courtyard around a pool of toxic
  slime, an armory, a stair hall with the red keycard on a raised platform, a tech hall behind
  the red door, and the exit room. There's also one secret to find. The map is ASCII art in
  `Level.cs`, and it becomes Doom-style sectors with their own floor and ceiling heights: stairs,
  sunken slime, tall rooms, low corridors and open sky.
- **Doors.** Doors slide up into the ceiling and close again by themselves. If something is
  standing in the way they bounce back open. The red door needs the red keycard, and the secret
  door looks just like the wall around it.
- **Weapons** with Doom's damage numbers. The fist, the pistol (the first shot is always
  accurate), the 7-pellet shotgun with its pump animation, the chaingun and the rocket launcher
  (its splash damage can hurt you too). Shots auto-aim up and down at whatever is in front of you.
- **Monsters.** The zombieman fires hitscan bullets and drops a clip when it dies. The imp throws
  fireballs and claws you up close. The demon is fast and bites. The cacodemon floats and spits
  lightning balls. Monsters sleep until they see you or hear your gunfire. When they can't see you,
  they find their way around walls and open doors to reach you. Getting hit can stun them
  ("pain chance"), and they fall over in a death animation.
- **Items.** Stimpacks, medikits, armor, clips, shells, rockets, the red keycard, weapon pickups,
  and a soulsphere. There are also explosive barrels, which set each other off in chain reactions.
- **The look.** The scene is rendered at low resolution with square pixels (press P to turn this
  off), with dark fog and flickering lights. Sprites are shaded by the lights around them, and
  the slime glows green under a red sky.
- **The HUD.** The classic status bar shows ammo, health, the weapons you own, the face, armor,
  keys and ammo counts. The face looks around, winces when you get hurt, grins at new weapons and
  gets bloodier as your health drops. You also get pickup messages, red and gold screen flashes,
  an automap that reveals what you've seen, and an end-of-level screen with your kills, items,
  secrets and time.

## Project layout

```
Assets/Scripts/
  Core/
    GameBootstrap.cs   Starts the game when Play is pressed (no scene setup needed)
    GameManager.cs     Rendering setup, level start/restart, stats, messages, screen shake
    Hud.cs             Weapon view, status bar, face, automap, title/death/intermission screens
    GameInput.cs       Works with both the old and new Unity Input System
    Gfx.cs             Matte materials, sprite lighting, flickering lights
    PixelCanvas.cs     Tiny pixel-art rasterizer (shaded circles, capsules, triangles, glows)
    RetroCamera.cs     Low-resolution pixelated rendering
    Sfx.cs             Synthesizes every sound effect from noise and waveforms
  Art/
    TextureLibrary.cs  Procedural wall, floor, ceiling, door, switch and slime textures
    SpriteLibrary.cs   Monster frames, items, effects, first-person weapons, status bar face
  World/
    Level.cs           The ASCII map, sectors to meshes, things, secrets, monster pathfinding
    Door.cs            Sliding doors (normal, red key, secret)
    Billboard.cs       Camera-facing, light-shaded sprites and one-shot sprite effects
    Combat.cs          Explosions and splash damage, projectiles, exploding barrels
    Pickup.cs          Health, armor, ammo, weapons, keys and powerups
  Actors/
    Player.cs          Movement, mouse look, health/armor, using things, slime damage
    WeaponSystem.cs    Weapons, ammo, switching, hitscan and auto-aim, view sprites
    Monster.cs         Monster types and AI: sight, chase, attacks, pain, death
```

## Ideas for later

- [ ] More levels (E1M2...) and a level-select screen
- [ ] Plasma rifle, BFG, chainsaw and berserk pack
- [ ] More monsters: Baron of Hell, Lost Soul, shotgun guy
- [ ] Monster infighting
- [ ] Difficulty levels
- [ ] Music
