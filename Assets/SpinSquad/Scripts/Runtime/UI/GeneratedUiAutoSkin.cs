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

            // Every control gets the same touch-down feedback, including buttons
            // that already own a purpose-built sprite and return early below.
            ButtonHoldTint.Ensure(button);

            var label = button.GetComponentInChildren<Text>(true);
            var text = label != null ? label.text.Trim() : string.Empty;
            // Screens such as Homepage already provide purpose-built navigation,
            // campaign and CTA sprites. Replacing those with a generic skin
            // destroys their hierarchy (a square icon skin gets stretched into
            // a wide tab). The generated kit is a fallback for unstyled buttons,
            // not a global override for authored controls.
            if (button.gameObject.scene.IsValid() &&
                button.gameObject.scene.name == "Homepage" &&
                HasAuthoredSkin(image.sprite))
                return;

            if (TryApplyExactButtonKit(button, image, label, text))
                return;

            if (HasAuthoredSkin(image.sprite))
                return;

            var key = (button.name + " " + text).ToLowerInvariant();
            var rect = image.rectTransform.rect;
            var square = rect.height > 1f && rect.width / rect.height < 1.55f;
            var danger = ContainsAny(key, "sell", "remove", "delete", "quit", "restart");
            var secondary = ContainsAny(key, "back", "close", "cancel", "settings", "pause", "speed", "bag", "home");

            PreserveStandaloneIcon(button, image, label);
            var role = square
                ? SpinSquadButtonRole.Icon
                : danger
                    ? SpinSquadButtonRole.Danger
                    : secondary
                        ? SpinSquadButtonRole.Secondary
                        : SpinSquadButtonTheme.FromLabel(text);
            SpinSquadButtonTheme.Apply(image, role);

            SpinSquadButtonTheme.ApplyInteraction(button);

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

        static bool TryApplyExactButtonKit(Button button, Image image, Text label, string text)
        {
            var key = (text ?? string.Empty).Trim().ToLowerInvariant();
            Sprite normal = null;
            Sprite pressed = null;
            Sprite disabled = null;
            switch (key)
            {
                case "battle":
                case "start": normal = ButtonKitSprites.Start ?? ButtonKitSprites.BattleOn; pressed = ButtonKitSprites.StartPressed; disabled = ButtonKitSprites.BattleOff; break;
                case "upgrade": normal = ButtonKitSprites.UpgradeOn; disabled = ButtonKitSprites.UpgradeOff; break;
                case "danger": normal = ButtonKitSprites.DangerOn; disabled = ButtonKitSprites.DangerOff; break;
                case "back": normal = ButtonKitSprites.Back; break;
                case "ok": normal = ButtonKitSprites.Ok; break;
                case "merge": normal = ButtonKitSprites.Merge; pressed = ButtonKitSprites.MergePressed; break;
                case "sell": normal = ButtonKitSprites.Sell; pressed = ButtonKitSprites.SellPressed; break;
                case "info": normal = ButtonKitSprites.Info; pressed = ButtonKitSprites.InfoPressed; break;
                case "close": normal = ButtonKitSprites.Close; pressed = ButtonKitSprites.ClosePressed; break;
                case "resume":
                case "continue": normal = ButtonKitSprites.Resume; pressed = ButtonKitSprites.ResumePressed; break;
                case "restart": normal = ButtonKitSprites.Restart; pressed = ButtonKitSprites.RestartPressed; break;
            }

            if (normal == null && key.StartsWith("roll"))
            {
                normal = ButtonKitSprites.Roll;
                pressed = ButtonKitSprites.RollPressed;
            }
            if (normal == null && key.StartsWith("add ally"))
            {
                normal = ButtonKitSprites.AddAlly;
                pressed = ButtonKitSprites.AddAllyPressed;
            }
            if (normal == null && key.StartsWith("merge"))
            {
                normal = ButtonKitSprites.Merge;
                pressed = ButtonKitSprites.MergePressed;
            }
            if (normal == null && key.StartsWith("sell"))
            {
                normal = ButtonKitSprites.Sell;
                pressed = ButtonKitSprites.SellPressed;
            }
            if (normal == null && key.StartsWith("bag"))
            {
                // The battle HUD uses the down arrow for an expanded bag and
                // the right arrow for a collapsed bag; its label never contains
                // the literal word "open".
                var isOpen = key.Contains("open") || key.Contains("▼");
                normal = isOpen ? ButtonKitSprites.BagOpen : ButtonKitSprites.BagClosed;
                pressed = isOpen ? ButtonKitSprites.BagOpenPressed : ButtonKitSprites.BagClosedPressed;
            }

            if (normal == null && TryApplyKitIcon(button, image, key))
                return true;

            if (normal == null)
                return false;

            image.sprite = normal;
            image.type = Image.Type.Simple;
            // These are authored, complete button images rather than scalable
            // nine-slice frames. Never force them to inherit the aspect ratio
            // of an arbitrary layout cell: that visibly stretches the icon,
            // border and caption together.
            image.preserveAspect = true;
            image.color = Color.white;
            button.transition = Selectable.Transition.SpriteSwap;
            var states = button.spriteState;
            states.highlightedSprite = normal;
            states.pressedSprite = pressed != null ? pressed : normal;
            states.selectedSprite = normal;
            states.disabledSprite = disabled != null ? disabled : normal;
            button.spriteState = states;
            var holdTint = button.GetComponent<ButtonHoldTint>();
            if (holdTint != null)
                holdTint.SetFallbackTintEnabled(pressed == null);
            HideExistingDecorativeIcons(button, image);
            if (label != null)
            {
                // Approved button art already contains its English caption.
                // Keep the logical Text object active for gameplay decisions,
                // but stop rendering a duplicate caption over the sprite.
                label.gameObject.SetActive(true);
                label.color = Color.clear;
                label.raycastTarget = false;
            }
            ConfigureDynamicKitValue(button, label, key);
            return true;
        }

        static void ConfigureDynamicKitValue(Button button, Text source, string key)
        {
            var child = button.transform.Find("KitDynamicValue");
            var value = child != null ? child.GetComponent<Text>() : null;
            var shown = string.Empty;
            if (key.StartsWith("roll"))
            {
                var open = key.IndexOf('(');
                var close = key.IndexOf(')');
                if (open >= 0 && close > open)
                    shown = key.Substring(open + 1, close - open - 1).Trim();
            }

            if (string.IsNullOrEmpty(shown))
            {
                if (value != null)
                    value.gameObject.SetActive(false);
                return;
            }

            if (value == null)
            {
                var go = new GameObject("KitDynamicValue");
                go.transform.SetParent(button.transform, false);
                value = go.AddComponent<Text>();
                value.font = source != null ? source.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                value.fontStyle = FontStyle.Bold;
                value.alignment = TextAnchor.MiddleCenter;
                value.color = Color.white;
                value.raycastTarget = false;
                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(0.02f, 0.08f, 0.18f, 0.95f);
                outline.effectDistance = new Vector2(2f, -2f);
                var rt = value.rectTransform;
                rt.anchorMin = new Vector2(0.76f, 0.02f);
                rt.anchorMax = new Vector2(0.98f, 0.45f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            value.gameObject.SetActive(true);
            value.text = shown;
            value.fontSize = 24;
        }

        static bool TryApplyKitIcon(Button button, Image rootImage, string key)
        {
            Sprite icon = null;
            if (key.StartsWith("bag")) icon = ButtonKitSprites.Bag;
            else if (key.Contains("pause")) icon = ButtonKitSprites.Pause;
            else if (key.Contains("mail")) icon = ButtonKitSprites.NavigationMail ?? ButtonKitSprites.Mail;
            else if (key.Contains("setting")) icon = ButtonKitSprites.NavigationSettings ?? ButtonKitSprites.Settings;
            else if (key.Contains("shop")) icon = ButtonKitSprites.NavigationShop ?? ButtonKitSprites.Shop;
            else if (key.Contains("treasure")) icon = ButtonKitSprites.NavigationTreasure;
            else if (key == "home") icon = ButtonKitSprites.NavigationHome ?? ButtonKitSprites.Home;
            if (icon == null)
                return false;

            var child = button.transform.Find("ApprovedKitIcon");
            Image iconImage = child != null ? child.GetComponent<Image>() : FindExistingIcon(button, rootImage);
            if (iconImage == null)
            {
                var go = new GameObject("ApprovedKitIcon");
                go.transform.SetParent(button.transform, false);
                iconImage = go.AddComponent<Image>();
            }

            iconImage.sprite = icon;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var rt = iconImage.rectTransform;
            rt.anchorMin = new Vector2(0.04f, 0.14f);
            rt.anchorMax = new Vector2(0.34f, 0.86f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                var labelRt = label.rectTransform;
                labelRt.offsetMin = new Vector2(Mathf.Max(labelRt.offsetMin.x, 34f), labelRt.offsetMin.y);
            }
            return true;
        }

        static Image FindExistingIcon(Button button, Image rootImage)
        {
            foreach (var candidate in button.GetComponentsInChildren<Image>(true))
            {
                if (candidate == null || candidate == rootImage)
                    continue;
                if (candidate.name == "GeneratedButtonIcon" || candidate.name == "ApprovedKitIcon")
                    continue;
                return candidate;
            }
            return null;
        }

        static void HideExistingDecorativeIcons(Button button, Image rootImage)
        {
            foreach (var candidate in button.GetComponentsInChildren<Image>(true))
            {
                if (candidate != null && candidate != rootImage)
                    candidate.gameObject.SetActive(false);
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
            if (string.IsNullOrEmpty(name))
                return false;

            // GeneratedUI sprites are the authored Spin Squad atlas skin. Treating
            // them as an unstyled fallback made the refresher repeatedly replace
            // wide CTA/nav controls with the square icon skin.
            if (name.StartsWith("GeneratedUI_"))
                return true;

            return name.StartsWith("MetaHomeUI_")
                || name.StartsWith("HudUI_")
                || name.StartsWith("BattleUI_")
                || name.StartsWith("TreasureUI_")
                || name.StartsWith("AnimalKitUI_");
        }
    }
}
