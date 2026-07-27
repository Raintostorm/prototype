#if UNITY_EDITOR
using Spine.Unity;
using SpinSquad.Core;
using UnityEditor;
using UnityEngine;

namespace SpinSquad.Editor
{
    /// <summary>Tạo / cập nhật prefab battle Spine knight dưới Resources.</summary>
    public static class SpinSquadSpineBattlePrefabBuilder
    {
        public const string SkeletonDataPath = "Assets/SpinSquad/Data/Spine/Knight/skeleton_SkeletonData.asset";
        public const string PrefabPath = "Assets/SpinSquad/Resources/Battle/KnightBattleVisual.prefab";

        [MenuItem("SpinSquad/Spine/Generate Knight Battle Visual Prefab")]
        public static void Generate()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(SkeletonDataPath);
            if (data == null)
            {
                Debug.LogError("[SpinSquad] Missing " + SkeletonDataPath + ". Cần json + atlas + png trong Assets/SpinSquad/Data/Spine/Knight/ và để Unity import.");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }

            EnsureFolder("Assets/SpinSquad/Resources/Battle");

            var skel = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
            var go = skel.gameObject;
            go.name = "KnightBattleVisual";
            skel.loop = true;
            skel.Initialize(true);
            skel.AnimationName = string.Empty;
            var idle = data.GetSkeletonData(false)?.FindAnimation("idle");
            if (idle != null)
                skel.AnimationState.SetAnimation(0, idle, true);
            skel.Update(0f);
            skel.LateUpdate();

            _ = go.GetComponent<SpineBattleAnimator>() ?? go.AddComponent<SpineBattleAnimator>();

            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            UnityEngine.Object.DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SpinSquad] Saved " + PrefabPath + " — kiểm tra SpineBattleAnimator trên prefab (idle/walk/attack/died).");

            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            var leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
