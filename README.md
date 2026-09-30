# Unity 3D Coin Collector

A small third-person 3D game made in Unity. Run around, jump up a spiral of floating platforms and collect every coin as fast as you can.

## How to open and play

The easiest way is to let Unity create the project, then drop the scripts in:

1. Install [Unity Hub](https://unity.com/download) and a Unity editor (Unity 6 or 2022.3 LTS).
2. In Unity Hub click **New project**, pick the **3D** template (Universal 3D is fine too),
   give it a name and click **Create project**.
3. Download this repository (green **Code** button, then **Download ZIP**, on the
   `claude/vigilant-fermat-eb7boo` branch) and unzip it.
4. Copy the `Assets/Scripts` folder from the zip into your new project's `Assets` folder
   (you can drag it into the **Project** window inside Unity).
5. Wait for Unity to finish compiling, then press **Play** ▶. The level is built from code,
   so it works in the default scene.

## Controls

| Key | Action |
| --- | --- |
| W A S D / arrows | Move |
| Mouse | Look around |
| Mouse wheel | Zoom camera |
| Space | Jump |
| Left Shift | Sprint |
| R | Restart |
| Esc / left click | Free / lock the mouse cursor |

## Project layout

```
Assets/Scripts/
  GameBootstrap.cs      Builds the level (ground, platforms, trees, player, camera) when Play starts
  GameManager.cs        Coins, timer, best time, HUD and restart
  PlayerController.cs   Movement, sprinting, jumping and gravity (CharacterController)
  ThirdPersonCamera.cs  Mouse-orbit follow camera that avoids clipping through walls
  Coin.cs               Spinning, bobbing collectible
  GameInput.cs          Input wrapper that supports both the old and the new Input System
```

## Ideas for what to add next

- Replace the capsule with a real character model and animations
- Enemies or moving obstacles
- More levels, a main menu, sound effects and music
- Moving platforms or a double jump
