#if UNITY_EDITOR
using System;
using System.Reflection;
using SpinSquad.Scenes;
using SpinSquad.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSquad.EditorTools
{
    /// <summary>Validates the runtime-built Homepage hierarchy and its critical anchors.</summary>
    public static class HomepageUiValidator
    {
        const string ScenePath = OpenSampleSceneMenu.HomepageScenePath;

        [MenuItem("SpinSquad/Validate/Homepage UI Layout")]
        public static void ValidateFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Validate();
            EditorUtility.DisplayDialog("SpinSquad UI", "Homepage UI layout validation passed.", "OK");
        }

        public static void ValidateFromCommandLine()
        {
            Validate();
        }

        static void Validate()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded, "Could not load Homepage scene.");

            var hub = UnityEngine.Object.FindAnyObjectByType<HomepageHub>();
            Require(hub != null, "Homepage scene does not contain HomepageHub.");

            var previous = GameObject.Find("HomepageUI");
            if (previous != null)
                UnityEngine.Object.DestroyImmediate(previous);

            Invoke(hub, "ResolveUiFont");
            Invoke(hub, "BuildUi");
            Canvas.ForceUpdateCanvases();

            var root = GameObject.Find("HomepageUI");
            Require(root != null, "HomepageUI was not built.");

            try
            {
                var safe = Find(root.transform, "SafeArea");
                Require(safe.GetComponent<SafeAreaFitter>() != null, "SafeAreaFitter is missing.");

                var campaign = Find(safe, "CampaignSelector");
                Require(campaign.GetComponent<Image>() != null, "Campaign selector card is missing.");

                var bottomNav = Find(safe, "BottomNav").GetComponent<RectTransform>();
                Require(bottomNav != null, "Bottom navigation RectTransform is missing.");
                Require(Mathf.Approximately(bottomNav.anchorMin.y, 0f) &&
                        Mathf.Approximately(bottomNav.anchorMax.y, 0f),
                    "Bottom navigation must be anchored to the bottom, not stretched vertically.");
                Require(Mathf.Abs(bottomNav.sizeDelta.y - MetaHudTheme.BottomNavHeight) < 0.1f,
                    "Bottom navigation height does not match the theme.");

                var navRow = Find(bottomNav, "HorizontalRow");
                Require(navRow.childCount == 4, "Homepage should have exactly four bottom navigation tabs.");
                Require(navRow.Find("BattleNavTab") == null,
                    "Bottom navigation must not duplicate the main Battle CTA.");

                ValidateTab(navRow, "HomeNavTab");
                ValidateTab(navRow, "UpgradeNavTab");
                ValidateTab(navRow, "TreasureNavTab");
                ValidateTab(navRow, "SettingsNavTab");

                var battleCta = Find(safe, "BATTLEButton").GetComponent<Button>();
                Require(battleCta != null, "Main Battle CTA is missing.");

                var overlay = Find(root.transform, "LevelSelectOverlay");
                Require(!overlay.gameObject.activeSelf, "Level selection overlay must start closed.");

                Require(safe.Find("MetaDebugGrantPanel") == null,
                    "Expanded debug grant panel should not clutter Homepage.");

                Debug.Log("[SpinSquad][HomepageUI] Validation passed: Safe Area, campaign card, one Battle CTA, four bottom tabs and closed overlay.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static void ValidateTab(Transform row, string name)
        {
            var tab = Find(row, name);
            Require(tab.GetComponent<Button>() != null, $"{name} has no Button.");
            Require(tab.Find("Icon")?.GetComponent<Image>() != null, $"{name} has no icon layer.");
            var label = tab.Find("Label")?.GetComponent<Text>();
            Require(label != null && label.gameObject.activeSelf && !string.IsNullOrWhiteSpace(label.text),
                $"{name} label must remain visible.");
        }

        static Transform Find(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            Require(child != null, $"Missing UI object: {childName}");
            return child;
        }

        static void Invoke(object target, string methodName)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Require(method != null, $"Missing method: {methodName}");
            method.Invoke(target, null);
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException($"[SpinSquad][HomepageUI] {message}");
        }
    }
}
#endif
