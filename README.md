# Unity 3D Coin Collector

A small third-person 3D game made in Unity. Run around, jump up a spiral of floating platforms and collect every coin as fast as you can.

## How to open and play

1. Install [Unity Hub](https://unity.com/download) and a **Unity 6** editor (2022.3 LTS or newer also works).
2. In Unity Hub, click **Add → Add project from disk** and pick this folder.
   (If Hub asks about the editor version, choose the one you have installed.)
3. When the editor opens, press **Play** ▶. It works in any scene, even the default empty one,
   because `GameBootstrap.cs` builds the level from code.
   To keep it, save the scene with **File → Save As…** (for example `Assets/Scenes/Main.unity`).

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
