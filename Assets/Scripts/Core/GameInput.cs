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
    public static bool CursorLocked => Cursor.lockState == CursorLockMode.Locked;

#if USE_NEW_INPUT
    static Keyboard Kb => Keyboard.current;
    static Mouse Ms => Mouse.current;

    static float Axis(bool negative, bool positive) => (positive ? 1f : 0f) - (negative ? 1f : 0f);
    static bool Down(Key key) => Kb != null && Kb[key].wasPressedThisFrame;
    static bool Held(Key key) => Kb != null && Kb[key].isPressed;

    public static Vector2 Move
    {
        get
        {
            if (Kb == null) return Vector2.zero;
            float x = Axis(Held(Key.A) || Held(Key.LeftArrow), Held(Key.D) || Held(Key.RightArrow));
            float y = Axis(Held(Key.S) || Held(Key.DownArrow), Held(Key.W) || Held(Key.UpArrow));
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }
    }

    public static Vector2 Look => Ms != null ? Ms.delta.ReadValue() * 0.1f : Vector2.zero;
    public static int WeaponScroll
    {
        get
        {
            if (Ms == null) return 0;
            float y = Ms.scroll.ReadValue().y;
            return Mathf.Approximately(y, 0f) ? 0 : (y > 0f ? 1 : -1);
        }
    }
    public static bool FireHeld =>
        (Ms != null && CursorLocked && Ms.leftButton.isPressed) || Held(Key.LeftCtrl) || Held(Key.RightCtrl);
    public static bool UsePressed => Down(Key.E) || Down(Key.Space);
    public static bool Run => Held(Key.LeftShift) || Held(Key.RightShift);
    public static bool RestartPressed => Down(Key.R);
    public static bool UnlockCursorPressed => Down(Key.Escape);
    public static bool ClickPressed => Ms != null && Ms.leftButton.wasPressedThisFrame;
    public static bool AutomapPressed => Down(Key.Tab) || Down(Key.M);
    public static bool PixelTogglePressed => Down(Key.P);
    public static bool ContinuePressed => Down(Key.Enter) || Down(Key.Space) || ClickPressed;

    public static int WeaponSlotPressed
    {
        get
        {
            if (Down(Key.Digit1)) return 1;
            if (Down(Key.Digit2)) return 2;
            if (Down(Key.Digit3)) return 3;
            if (Down(Key.Digit4)) return 4;
            if (Down(Key.Digit5)) return 5;
            return 0;
        }
    }
#else
    public static Vector2 Move =>
        Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);

    public static Vector2 Look => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    public static int WeaponScroll
    {
        get
        {
            float y = Input.GetAxis("Mouse ScrollWheel");
            return Mathf.Approximately(y, 0f) ? 0 : (y > 0f ? 1 : -1);
        }
    }
    public static bool FireHeld =>
        (CursorLocked && Input.GetMouseButton(0)) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    public static bool UsePressed => Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
    public static bool Run => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    public static bool RestartPressed => Input.GetKeyDown(KeyCode.R);
    public static bool UnlockCursorPressed => Input.GetKeyDown(KeyCode.Escape);
    public static bool ClickPressed => Input.GetMouseButtonDown(0);
    public static bool AutomapPressed => Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.M);
    public static bool PixelTogglePressed => Input.GetKeyDown(KeyCode.P);
    public static bool ContinuePressed =>
        Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || ClickPressed;

    public static int WeaponSlotPressed
    {
        get
        {
            for (int i = 1; i <= 5; i++)
                if (Input.GetKeyDown(KeyCode.Alpha0 + i)) return i;
            return 0;
        }
    }
#endif
}
