using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpinSquad.UI
{
    /// <summary>
    /// Consistent touch feedback for runtime and authored buttons. It only tints
    /// the target graphic, so pressing never changes layout, scale or sprite.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ButtonHoldTint : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        static readonly Color PressMultiplier = new(0.76f, 0.78f, 0.82f, 1f);

        Button _button;
        Graphic _graphic;
        Color _restingColor;
        bool _pressed;
        bool _fallbackTintEnabled = true;

        public static void Ensure(Button button)
        {
            if (button == null || button.GetComponent<ButtonHoldTint>() != null)
                return;
            button.gameObject.AddComponent<ButtonHoldTint>();
        }

        public void SetFallbackTintEnabled(bool enabled)
        {
            if (_fallbackTintEnabled == enabled)
                return;
            Restore();
            _fallbackTintEnabled = enabled;
        }

        void Awake()
        {
            _button = GetComponent<Button>();
            ResolveGraphic();
        }

        void OnEnable()
        {
            ResolveGraphic();
            CaptureRestingColor();
        }

        void LateUpdate()
        {
            // Unity's Selectable color tween can run after OnPointerDown. Reapply
            // once per rendered frame while held so the feedback remains visible
            // for the full duration of a mouse or touch press.
            if (_pressed && _graphic != null)
                _graphic.color = Multiply(_restingColor, PressMultiplier);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_fallbackTintEnabled || _button == null || !_button.IsInteractable())
                return;

            ResolveGraphic();
            if (_graphic == null)
                return;

            CaptureRestingColor();
            _graphic.color = Multiply(_restingColor, PressMultiplier);
            _pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData) => Restore();

        public void OnPointerExit(PointerEventData eventData) => Restore();

        void OnDisable() => Restore();

        void ResolveGraphic()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            _graphic = _button != null ? _button.targetGraphic : null;
            if (_graphic == null)
                _graphic = GetComponent<Graphic>();
        }

        void CaptureRestingColor()
        {
            if (!_pressed && _graphic != null)
                _restingColor = _graphic.color;
        }

        void Restore()
        {
            if (!_pressed)
                return;
            if (_graphic != null)
                _graphic.color = _restingColor;
            _pressed = false;
        }

        static Color Multiply(Color value, Color multiplier)
        {
            return new Color(
                value.r * multiplier.r,
                value.g * multiplier.g,
                value.b * multiplier.b,
                value.a * multiplier.a);
        }
    }
}
