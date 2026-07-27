#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Spine.Unity;
using Spine.Unity.Editor;
using SpinSquad.Core;
using UnityEditor;
using UnityEngine;

namespace SpinSquad.Editor
{
    /// <summary>One-time / on-demand import of element ally Spine exports + battle prefabs under Resources.</summary>
    public static class SpinSquadAllySpineSetup
    {
        public const string KnightAnimConfigPath = "Assets/SpinSquad/Data/Spine/Knight/KnightSpineBattleAnimConfig.asset";

        public static readonly (string colorFolder, string prefabFileName, string resourcesLoadName)[] AllySpecs =
        {
            ("Blue", "BlueAllyBattleVisual", "Battle/BlueAllyBattleVisual"),
            ("Green", "GreenAllyBattleVisual", "Battle/GreenAllyBattleVisual"),
            ("Red", "RedAllyBattleVisual", "Battle/RedAllyBattleVisual"),
            ("White", "WhiteAllyBattleVisual", "Battle/WhiteAllyBattleVisual"),
        };

        [MenuItem("SpinSquad/Spine/Import + Generate All Ally Element Assets")]
        public static void ImportAndGenerateAll()
        {
            ImportAllySpineJson();
            GenerateAllyBattlePrefabs();
        }

        [MenuItem("SpinSquad/Spine/Import Ally Element Spine Assets")]
        public static void ImportAllySpineJson()
        {
            var paths = new List<string>();
            foreach (var (folder, _, _) in AllySpecs)
            {
                var json = $"Assets/SpinSquad/Data/Spine/Allies/{folder}/skeleton.json";
                if (!File.Exists(json))
                {
                    Debug.LogError("[SpinSquad] Missing " + json);
                    continue;
                }

                paths.Add(json);
            }

            if (paths.Count == 0)
                return;
            AssetUtility.ImportSpineContent(paths.ToArray(), new List<string>(), reimport: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SpinSquad] Spine import finished for " + paths.Count + " ally skeleton(s).");
        }

        [MenuItem("SpinSquad/Spine/Generate Ally Element Battle Prefabs")]
        public static void GenerateAllyBattlePrefabs()
        {
            var animConfig = AssetDatabase.LoadAssetAtPath<SpineBattleAnimConfig>(KnightAnimConfigPath);
            if (animConfig == null)
                Debug.LogWarning("[SpinSquad] Missing anim config at " + KnightAnimConfigPath + " — prefabs will use SpineBattleAnimator defaults.");

            EnsureFolder("Assets/SpinSquad/Resources/Battle");

            foreach (var (folder, prefabFileName, _) in AllySpecs)
            {
                var dataPath = $"Assets/SpinSquad/Data/Spine/Allies/{folder}/skeleton_SkeletonData.asset";
                var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(dataPath);
                if (data == null)
                {
                    Debug.LogError("[SpinSquad] Missing SkeletonDataAsset: " + dataPath + " — run SpinSquad/Spine/Import Ally Element Spine Assets first.");
                    continue;
                }

                var skel = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
                var go = skel.gameObject;
                go.name = prefabFileName;
                skel.loop = true;
                skel.Initialize(true);
                skel.AnimationName = string.Empty;
                var idle = data.GetSkeletonData(false)?.FindAnimation("idle");
                if (idle != null)
                    skel.AnimationState.SetAnimation(0, idle.Name, true);
                skel.Update(0f);
                skel.LateUpdate();

                var driver = go.GetComponent<SpineBattleAnimator>() ?? go.AddComponent<SpineBattleAnimator>();
                if (animConfig != null)
                {
                    var so = new SerializedObject(driver);
                    so.FindProperty("animConfig").objectReferenceValue = animConfig;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                var prefabPath = $"Assets/SpinSquad/Resources/Battle/{prefabFileName}.prefab";
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);
                Debug.Log("[SpinSquad] Saved " + prefabPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
