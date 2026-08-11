using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpinSquad.UI
{
    /// <summary>
    /// Applies the project-owned English fantasy button kit to runtime-built UI.
    /// Most prototype screens are created from code, so one scene-safe skinning
    /// pass keeps their visuals and interaction states consistent.
    /// </summary>
    public sealed class GeneratedUiAutoSkin : MonoBehaviour
    {
        static GeneratedUiAutoSkin _instance;
        readonly HashSet<int> _styled = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (_instance != null)
                return;
            var go = new GameObject("Generated UI Theme");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GeneratedUiAutoSkin>();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(RefreshLoop());
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _styled.Clear();
            StartCoroutine(ApplyAfterLayout());
        }

        IEnumerator ApplyAfterLayout()
        {
            yield return null;
            yield return null;
            ApplyAll();
        }

        IEnumerator RefreshLoop()
        {
            var wait = new WaitForSecondsRealtime(0.35f);
            yield return null;
            yield return null;
            while (enabled)
            {
                ApplyAll();
                yield return wait;
            }
        }

        void ApplyAll()
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                if (button == null || !button.gameObject.scene.IsValid())
                    continue;
                var id = button.GetInstanceID();
                if (_styled.Contains(id))
                    continue;
                Apply(button);
                _styled.Add(id);
            }
        }

        public static void Apply(Button button)
        {
            if (button == null)
                return;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null)
                return;

            // Screens such as Homepage already provide purpose-built navigation,
            // campaign and CTA sprites. Replacing those with a generic skin
            // destroys their hierarchy (a square icon skin gets stretched into
            // a wide tab). The generated kit is a fallback for unstyled buttons,
            // not a global override for authored controls.
            if (HasAuthoredSkin(image.sprite))
                return;

            var label = button.GetComponentInChildren<Text>(true);
            var text = label != null ? label.text.Trim() : string.Empty;
            var key = (button.name + " " + text).ToLowerInvariant();
            var rect = image.rectTransform.rect;
            var square = rect.height > 1f && rect.width / rect.height < 1.55f;
            var danger = ContainsAny(key, "sell", "remove", "delete", "quit", "reroll", "restart", "play again");
            var secondary = ContainsAny(key, "back", "close", "cancel", "settings", "pause", "speed", "bag", "home");

            Sprite sprite;
            if (!button.interactable)
                sprite = square ? GeneratedUiSprites.IconDisabled : GeneratedUiSprites.DisabledButton;
            else if (danger)
                sprite = square ? GeneratedUiSprites.DangerButton : GeneratedUiSprites.DangerAltButton;
            else if (square)
                sprite = secondary ? GeneratedUiSprites.IconDark : GeneratedUiSprites.IconPrimary;
            else
                sprite = secondary ? GeneratedUiSprites.SecondaryButton : GeneratedUiSprites.PrimaryButton;

            if (sprite == null)
                return;

            PreserveStandaloneIcon(button, image, label);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = Color.white;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.78f, 0.92f, 1f);
            colors.selectedColor = new Color(1f, 0.92f, 0.55f, 1f);
            colors.disabledColor = new Color(0.62f, 0.66f, 0.72f, 0.78f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            if (label != null)
            {
                label.color = button.interactable ? Color.white : new Color(0.72f, 0.76f, 0.82f, 1f);
                label.fontStyle = FontStyle.Bold;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 14;
                label.resizeTextMaxSize = Mathf.Max(label.fontSize, 20);
                var rt = label.rectTransform;
                rt.offsetMin = new Vector2(Mathf.Max(rt.offsetMin.x, 18f), Mathf.Max(rt.offsetMin.y, 8f));
                rt.offsetMax = new Vector2(Mathf.Min(rt.offsetMax.x, -18f), Mathf.Min(rt.offsetMax.y, -8f));
            }
        }

        static void PreserveStandaloneIcon(Button button, Image rootImage, Text label)
        {
            if (label != null && !string.IsNullOrWhiteSpace(label.text))
                return;
            var old = rootImage.sprite;
            if (old == null || old.name.StartsWith("GeneratedUI_"))
                return;
            var n = old.name.ToLowerInvariant();
            if (ContainsAny(n, "button", "panel", "tab", "selected", "background"))
                return;
            if (button.transform.Find("GeneratedButtonIcon") != null)
                return;

            var iconGo = new GameObject("GeneratedButtonIcon");
            iconGo.transform.SetParent(button.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = old;
            icon.color = rootImage.color;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var rt = icon.rectTransform;
            rt.anchorMin = new Vector2(0.2f, 0.2f);
            rt.anchorMax = new Vector2(0.8f, 0.8f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static bool ContainsAny(string value, params string[] needles)
        {
            foreach (var needle in needles)
                if (value.Contains(needle))
                    return true;
            return false;
        }

        static bool HasAuthoredSkin(Sprite sprite)
        {
            if (sprite == null)
                return false;

            var name = sprite.name;
            if (string.IsNullOrEmpty(name) || name.StartsWith("GeneratedUI_"))
                return false;

            return name.StartsWith("MetaHomeUI_")
                || name.StartsWith("HudUI_")
                || name.StartsWith("BattleUI_")
                || name.StartsWith("TreasureUI_")
                || name.StartsWith("AnimalKitUI_");
        }
    }
}
