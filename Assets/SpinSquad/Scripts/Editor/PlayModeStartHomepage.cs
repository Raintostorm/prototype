#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpinSquad.EditorTools
{
    /// <summary>
    /// Bấm Play từ scene bất kỳ vẫn vào <c>Homepage</c> trước (Editor only).
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartHomepage
    {
        public const string HomepageScenePath = "Assets/SpinSquad/Scenes/Homepage.unity";
        const string KnightTestScenePath = "Assets/SpinSquad/Scenes/KnightSpineAnimTest.unity";
        const string IsolatedPreviewSessionKey = "SpinSquad.PlayOpenSceneOnce";

        static PlayModeStartHomepage()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChangedInEditMode;
            Apply();
        }

        /// <summary>Lets a dedicated visual QA scene play once without product-flow redirection.</summary>
        public static void PlayOpenSceneOnce()
        {
            SessionState.SetBool(IsolatedPreviewSessionKey, true);
            EditorSceneManager.playModeStartScene = null;
        }

        public static void Apply()
        {
            if (SessionState.GetBool(IsolatedPreviewSessionKey, false))
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }
            HomepageSceneBuilder.EnsureRequiredScenesInBuildSettings();
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(HomepageScenePath);
            EditorSceneManager.playModeStartScene = scene;
            if (scene == null)
                Debug.LogWarning("[SpinSquad] Chưa có Homepage scene tại " + HomepageScenePath +
                    " — Play mode sẽ dùng scene đang mở. Chạy menu SpinSquad → Setup Homepage Scene.");
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // The bypass is only for the preview run; regular Play returns to Homepage.
            if (state == PlayModeStateChange.ExitingPlayMode)
                SessionState.SetBool(IsolatedPreviewSessionKey, false);
        }

        static void OnActiveSceneChangedInEditMode(UnityEngine.SceneManagement.Scene previousScene,
            UnityEngine.SceneManagement.Scene nextScene)
        {
            // A dedicated visual QA scene must always be playable directly. This
            // removes the hidden Homepage redirect from the review workflow.
            if (!IsIsolatedPreviewScene(nextScene.path))
                return;

            SessionState.SetBool(IsolatedPreviewSessionKey, true);
            EditorSceneManager.playModeStartScene = null;
        }

        static bool IsIsolatedPreviewScene(string path)
        {
            return path == KnightTestScenePath;
        }
    }
}
#endif
