# Rat City (Unity)

A GTA-style 3D street brawler starring rats. You play a grey street rat with a crowbar
and fight a rival rat, **Big Cheese**, in the middle of a city intersection at sunset.
Everything (the city, the rats, and their animations) is built from code, so the
project has no art assets.

## How to open and play

1. On GitHub click the green **Code** button → **Download ZIP**, and unzip it.
2. In Unity Hub click **Add → Add project from disk** and select the unzipped folder
   (the one that contains `Assets`, `Packages` and `ProjectSettings`).
3. Open it. If Hub says the editor version isn't installed, pick the Unity version you
   have (Unity 6 or 2022.3 LTS) and confirm.
4. Press **Play** ▶.

## Controls

| Key | Action |
| --- | --- |
| W A S D | Move |
| Mouse | Look around (scroll to zoom) |
| Left Shift | Sprint |
| Space | Jump |
| Left click / J | Light attack (tap for a 3-hit combo) |
| Right click / K | Heavy attack (slow, big damage and knockback) |
| Left Ctrl / Q | Dodge roll (invincible during the roll) |
| R | Restart the fight |
| Esc / left click | Free / lock the mouse cursor |

## Combat system

- **Attacks** have three phases: windup, active and recovery. Damage lands in the middle of the active part.
- **Light combo:** Swing → Backhand → Slam. Press again during an attack to buffer the next hit. The Slam finisher launches the enemy.
- **Heavy attack:** a long windup, then a big overhead smash.
- **Soft auto-aim:** you turn toward the nearest enemy when you start an attack.
- **Hit reactions:** hit-stun (interrupts attacks), knockback, a white flash, hit-stop, screen shake and floating damage numbers.
- **Dodge:** a quick dash with invincibility frames. It cancels your current attack.
- **Anti stun-lock:** after several stuns in a row, the rival gets armor for a moment and fights back.
- **Rival AI:** runs at you, circles at fighting range, and mixes light combos with heavy attacks. It gets more aggressive below 40% HP.

## Project layout

```
Assets/Scripts/
  GameBootstrap.cs      Creates the camera, city, player rat and rival rat when Play starts
  GameManager.cs        Fight state, HUD/health bars, damage numbers, hit-stop, slow-mo, restart
  CityBuilder.cs        Streets, sidewalks, buildings, streetlights, cars, dumpsters, sunset lighting
  RatModel.cs           Builds a rat (outfit, crowbar or pipe, tail) from primitives
  RatRig.cs             A rat's body-part references and hit flash
  RatAnimator.cs        Procedural animation: stance, run, jump, swings, stun, dodge, KO, tail
  Combatant.cs          Health, hit-stun, knockback, invincibility, anti stun-lock armor
  MeleeAttacker.cs      Attack data, combos, input buffering, auto-aim, hit detection
  PlayerController.cs   Player movement, jump, dodge and attack input
  RatAI.cs              Rival rat behaviour
  ThirdPersonCamera.cs  Orbit camera with collision and screen shake
  Shapes.cs             Primitive/material helpers
  GameInput.cs          Works with both the old and new Unity Input System
```

## Roadmap

- [x] City background
- [x] Player rat with a crowbar
- [x] Rival rat
- [x] Combat system
- [ ] Blocking and parrying
- [ ] More rats (a gang) and waves
- [ ] Stealing and driving cars
- [ ] Pedestrians, wanted level, police rats
- [ ] Missions, cheese as money, weapon pickups
- [ ] Sound effects and music
