# Adventure Time Fan Game (Unity)

A small third-person 3D fan game made in Unity, starring a low-poly Finn the Human built from code. Run around, fight off ooze mobs with Finn's golden sword, jump up a spiral of floating platforms and collect every coin as fast as you can. Each coin heals 1 HP; if your health runs out, Finn gets knocked out.

## How to open and play

1. On GitHub click the green **Code** button → **Download ZIP**, and unzip it.
2. In Unity Hub click **Add → Add project from disk** and select the unzipped folder
   (the one that contains `Assets`, `Packages` and `ProjectSettings`).
3. Open it. If Hub says the editor version isn't installed, just pick the Unity version
   you have (Unity 6 or 2022.3 LTS) and confirm.
4. Press **Play** ▶.

## Controls

| Key | Action |
| --- | --- |
| W A S D / arrows | Move |
| Mouse | Look around |
| Mouse wheel | Zoom camera |
| Space | Jump |
| Left Shift | Sprint |
| Left click / F | Swing sword |
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
  Enemy.cs              Ooze mob: chases, attacks, knockback, health bar
  PlayerHealth.cs       Finn's HP, invulnerability blink, knockback
  PlayerCombat.cs       Sword attack and hit detection
  FinnModel.cs          Builds Finn (hat, face, shirt, shorts, backpack, limbs) from primitives
  FinnAnimator.cs       Code-driven run / jump / idle animation for Finn
  GameInput.cs          Input wrapper that supports both the old and the new Input System
```

## Ideas for what to add next

- Jake and Land of Ooo scenery
- More mob types and a boss
- More levels, a main menu, sound effects and music
- Moving platforms or a double jump

---

Adventure Time and its characters belong to Cartoon Network. This is a personal, non-commercial fan project.
