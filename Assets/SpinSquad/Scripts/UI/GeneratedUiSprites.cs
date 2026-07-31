using UnityEngine;

namespace SpinSquad.UI
{
    /// <summary>Project-owned generated UI art, converted to reusable runtime sprites.</summary>
    public static class GeneratedUiSprites
    {
        const string PrimaryButtonPath = "UI/Generated/button_primary_v1";
        static Sprite _primaryButton;

        public static Sprite PrimaryButton
        {
            get
            {
                if (_primaryButton != null)
                    return _primaryButton;

                var texture = Resources.Load<Texture2D>(PrimaryButtonPath);
                if (texture == null)
                    return null;

                var borderX = texture.width * 0.13f;
                var borderY = texture.height * 0.28f;
                _primaryButton = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(borderX, borderY, borderX, borderY));
                _primaryButton.name = "GeneratedPrimaryButton";
                return _primaryButton;
            }
        }
    }
}
