using UnityEngine;
using UnityEngine.InputSystem;

namespace SpinSquad.Core
{
    /// <summary>Chuột hoặc một ngón chạm — prep lưới / menu ô dùng chung.</summary>
    internal static class PrepGridPointer
    {
        public static bool TryGetPrimaryScreenPosition(out Vector2 screen)
        {
            var m = Mouse.current;
            if (m != null)
            {
                screen = m.position.ReadValue();
                return true;
            }

            var ts = Touchscreen.current;
            if (ts != null)
            {
                screen = ts.primaryTouch.position.ReadValue();
                return true;
            }

            screen = default;
            return false;
        }

        public static bool WasPressedThisFrame()
        {
            var m = Mouse.current;
            if (m != null && m.leftButton.wasPressedThisFrame)
                return true;

            var ts = Touchscreen.current;
            return ts != null && ts.primaryTouch.press.wasPressedThisFrame;
        }

        public static bool WasReleasedThisFrame()
        {
            var m = Mouse.current;
            if (m != null && m.leftButton.wasReleasedThisFrame)
                return true;

            var ts = Touchscreen.current;
            return ts != null && ts.primaryTouch.press.wasReleasedThisFrame;
        }

        public static bool IsPressed()
        {
            var m = Mouse.current;
            if (m != null && m.leftButton.isPressed)
                return true;

            var ts = Touchscreen.current;
            return ts != null && ts.primaryTouch.press.isPressed;
        }
    }
}
