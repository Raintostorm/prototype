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

        static PlayModeStartHomepage()
        {
            Apply();
        }

        public static void Apply()
        {
            HomepageSceneBuilder.EnsureRequiredScenesInBuildSettings();
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(HomepageScenePath);
            EditorSceneManager.playModeStartScene = scene;
            if (scene == null)
                Debug.LogWarning("[SpinSquad] Chưa có Homepage scene tại " + HomepageScenePath +
                    " — Play mode sẽ dùng scene đang mở. Chạy menu SpinSquad → Setup Homepage Scene.");
        }
    }
}
#endif
