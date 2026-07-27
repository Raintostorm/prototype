#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpinSquad.EditorTools
{
    /// <summary>Mở scene có <c>DuelDirector</c> — tránh Play trên scene Untitled trống (màn xanh).</summary>
    public static class OpenSampleSceneMenu
    {
        public const string HomepageScenePath = "Assets/SpinSquad/Scenes/Homepage.unity";
        public const string SampleScenePath = "Assets/SpinSquad/Scenes/SampleScene.unity";
        public const string DuelTestArenaPath = "Assets/SpinSquad/Scenes/DuelTestArena.unity";
        public const string KnightSpineAnimTestPath = "Assets/SpinSquad/Scenes/KnightSpineAnimTest.unity";

        [MenuItem("SpinSquad/Play/Open Homepage", priority = -5)]
        public static void OpenHomepage()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!System.IO.File.Exists(HomepageScenePath))
            {
                Debug.LogError($"[SpinSquad] Không tìm thấy scene: {HomepageScenePath}");
                return;
            }

            EditorSceneManager.OpenScene(HomepageScenePath, OpenSceneMode.Single);
            Debug.Log($"[SpinSquad] Đã mở {HomepageScenePath}.");
        }

        [MenuItem("SpinSquad/Play/Open Sample Scene", priority = 0)]
        public static void OpenSampleScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!System.IO.File.Exists(SampleScenePath))
            {
                Debug.LogError($"[SpinSquad] Không tìm thấy scene: {SampleScenePath}");
                return;
            }

            EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            Debug.Log($"[SpinSquad] Đã mở {SampleScenePath} — bấm Play để chạy demo duel.");
        }

        [MenuItem("SpinSquad/Play/Open Duel Test Arena", priority = 1)]
        public static void OpenDuelTestArena()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!System.IO.File.Exists(DuelTestArenaPath))
            {
                Debug.LogError($"[SpinSquad] Không tìm thấy scene: {DuelTestArenaPath}");
                return;
            }

            EditorSceneManager.OpenScene(DuelTestArenaPath, OpenSceneMode.Single);
            Debug.Log($"[SpinSquad] Đã mở {DuelTestArenaPath} — Knight Spine melee vs unit_enemy_knight; chọn preset rồi Áp dụng & reset trận.");
        }

        [MenuItem("SpinSquad/Play/Open Knight Spine Anim Test", priority = 2)]
        public static void OpenKnightSpineAnimTest()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!System.IO.File.Exists(KnightSpineAnimTestPath))
            {
                Debug.LogError($"[SpinSquad] Không tìm thấy scene: {KnightSpineAnimTestPath}");
                return;
            }

            EditorSceneManager.OpenScene(KnightSpineAnimTestPath, OpenSceneMode.Single);
            Debug.Log($"[SpinSquad] Đã mở {KnightSpineAnimTestPath} — bấm Play: UI thử idle/walk/attack/died.");
        }
    }
}
#endif
