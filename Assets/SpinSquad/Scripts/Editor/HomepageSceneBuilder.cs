#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SpinSquad.Scenes;

namespace SpinSquad.EditorTools
{
    public static class HomepageSceneBuilder
    {
        const string HomepagePath = "Assets/SpinSquad/Scenes/Homepage.unity";
        const string SamplePath = "Assets/SpinSquad/Scenes/SampleScene.unity";
        const string UpgradePath = "Assets/SpinSquad/Scenes/Upgrade.unity";
        const string UpgradeDetailPath = "Assets/SpinSquad/Scenes/UpgradeDetail.unity";
        const string TreasurePath = "Assets/SpinSquad/Scenes/Treasure.unity";
        const string TreasureDetailPath = "Assets/SpinSquad/Scenes/TreasureDetail.unity";
        const string TestArenaPath = "Assets/SpinSquad/Scenes/DuelTestArena.unity";

        static readonly string[] PreferredBuildOrder =
        {
            HomepagePath,
            SamplePath,
            UpgradePath,
            UpgradeDetailPath,
            TreasurePath,
            TreasureDetailPath,
            TestArenaPath,
        };

        [MenuItem("SpinSquad/Setup Homepage Scene")]
        public static void CreateOrUpdateSceneAndBuildSettings()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.16f, 0.28f, 1f);
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var hub = new GameObject("HomepageHub");
            hub.AddComponent<HomepageHub>();

            EditorSceneManager.SaveScene(scene, HomepagePath);

            RebuildBuildSettingsScenes();
            PlayModeStartHomepage.Apply();
            AssetDatabase.SaveAssets();
            Debug.Log("[SpinSquad] Đã tạo/cập nhật " + HomepagePath + " — Homepage là scene 0 trong Build; Play luôn mở Homepage.");
        }

        public static void EnsureRequiredScenesInBuildSettings()
        {
            RebuildBuildSettingsScenes();
        }

        static void RebuildBuildSettingsScenes()
        {
            var previous = EditorBuildSettings.scenes;
            var enabledByPath = previous.ToDictionary(s => s.path, s => s.enabled);

            var result = new List<EditorBuildSettingsScene>();
            foreach (var path in PreferredBuildOrder)
            {
                if (!File.Exists(path))
                    continue;
                var enabled = !enabledByPath.TryGetValue(path, out var was) || was;
                result.Add(new EditorBuildSettingsScene(path, enabled));
            }

            foreach (var s in previous)
            {
                if (PreferredBuildOrder.Contains(s.path))
                    continue;
                if (File.Exists(s.path))
                    result.Add(s);
            }

            EditorBuildSettings.scenes = result.ToArray();
        }
    }
}
#endif
