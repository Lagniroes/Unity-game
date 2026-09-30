#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#define USE_NEW_INPUT
#endif

using UnityEngine;
#if USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Small input wrapper so the game works with either the old Input Manager
/// or the new Input System package (whichever the project has enabled).
/// </summary>
public static class GameInput
{
#if USE_NEW_INPUT
    static Keyboard Kb => Keyboard.current;
    static Mouse Ms => Mouse.current;

    static float Axis(bool negative, bool positive) => (positive ? 1f : 0f) - (negative ? 1f : 0f);

    public static Vector2 Move
    {
        get
        {
            if (Kb == null) return Vector2.zero;
            float x = Axis(Kb.aKey.isPressed || Kb.leftArrowKey.isPressed, Kb.dKey.isPressed || Kb.rightArrowKey.isPressed);
            float y = Axis(Kb.sKey.isPressed || Kb.downArrowKey.isPressed, Kb.wKey.isPressed || Kb.upArrowKey.isPressed);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }
    }

    public static Vector2 Look => Ms != null ? Ms.delta.ReadValue() * 0.1f : Vector2.zero;
    public static float Zoom
    {
        get
        {
            if (Ms == null) return 0f;
            float y = Ms.scroll.ReadValue().y;
            return Mathf.Approximately(y, 0f) ? 0f : Mathf.Sign(y) * 0.1f;
        }
    }
    public static bool JumpPressed => Kb != null && Kb.spaceKey.wasPressedThisFrame;
    public static bool Sprint => Kb != null && Kb.leftShiftKey.isPressed;
    public static bool RestartPressed => Kb != null && Kb.rKey.wasPressedThisFrame;
    public static bool UnlockCursorPressed => Kb != null && Kb.escapeKey.wasPressedThisFrame;
    public static bool LockCursorPressed => Ms != null && Ms.leftButton.wasPressedThisFrame;
#else
    public static Vector2 Move =>
        Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);

    public static Vector2 Look => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    public static float Zoom => Input.GetAxis("Mouse ScrollWheel");
    public static bool JumpPressed => Input.GetKeyDown(KeyCode.Space);
    public static bool Sprint => Input.GetKey(KeyCode.LeftShift);
    public static bool RestartPressed => Input.GetKeyDown(KeyCode.R);
    public static bool UnlockCursorPressed => Input.GetKeyDown(KeyCode.Escape);
    public static bool LockCursorPressed => Input.GetMouseButtonDown(0);
#endif
}
