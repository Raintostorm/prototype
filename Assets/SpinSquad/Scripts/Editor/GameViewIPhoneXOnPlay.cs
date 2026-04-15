#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SpinSquad.EditorTools
{
    /// <summary>
    /// Khi bấm Play: ép cửa sổ Game sang độ phân giải cố định giống iPhone X dọc (1125×2436).
    /// Có thể gọi tay: menu <b>SpinSquad / Game View / Apply iPhone X (1125×2436)</b>.
    /// Unity 6: ưu tiên <c>PlayModeWindow.SetCustomRenderingResolution</c>; cũ hơn: reflection GameView.
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewIPhoneXOnPlay
    {
        /// <summary>iPhone X / XS / 11 Pro — pixel portrait (chuẩn asset @3x).</summary>
        public const int IPhoneXWidth = 1125;
        public const int IPhoneXHeight = 2436;

        const string SizeLabel = "SpinSquad iPhone X";

        static GameViewIPhoneXOnPlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                EditorApplication.delayCall += ApplyIPhoneXGameView;
        }

        [MenuItem("SpinSquad/Game View/Apply iPhone X (1125×2436)")]
        public static void ApplyFromMenu()
        {
            ApplyIPhoneXGameView();
        }

        public static void ApplyIPhoneXGameView()
        {
            try
            {
                if (TryApplyViaPlayModeWindow())
                    return;

                ApplyViaLegacyGameViewReflection();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SpinSquad] Game view preset failed: {e.Message}");
            }
        }

        static bool TryApplyViaPlayModeWindow()
        {
            var t = typeof(Editor).Assembly.GetType("UnityEditor.PlayModeWindow");
            if (t == null)
                return false;

            var method = t.GetMethod(
                "SetCustomRenderingResolution",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(uint), typeof(uint), typeof(string) },
                modifiers: null);

            if (method == null)
                return false;

            method.Invoke(null, new object[] { (uint)IPhoneXWidth, (uint)IPhoneXHeight, SizeLabel });
            return true;
        }

        static void ApplyViaLegacyGameViewReflection()
        {
            var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null)
            {
                Debug.LogWarning("[SpinSquad] UnityEditor.GameView type not found.");
                return;
            }

            var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
            if (gameView == null)
            {
                Debug.LogWarning("[SpinSquad] Game view window not found.");
                return;
            }

            var method = gameViewType.GetMethod(
                "SetCustomResolution",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
            {
                Debug.LogWarning(
                    "[SpinSquad] GameView.SetCustomResolution missing — Unity version may differ. " +
                    "Use Game tab: aspect dropdown → + → Fixed Resolution 1125×2436.");
                return;
            }

            method.Invoke(gameView, new object[] { new Vector2(IPhoneXWidth, IPhoneXHeight), SizeLabel });
        }
    }
}
#endif
