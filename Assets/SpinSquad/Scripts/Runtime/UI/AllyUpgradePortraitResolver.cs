using System.Collections.Generic;
using Spine.Unity;
using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.UI
{
    /// <summary>Creates UI sprites from frame zero of each ally's Battle idle pose.</summary>
    public static class AllyUpgradePortraitResolver
    {
        const int PortraitLayer = 30;
        const int TextureSize = 320;
        static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite Resolve(UnitCatalog catalog, int line, Rarity rarity, UnitDefinition primary = null)
        {
            _ = rarity;
            var def = primary;
            if (def == null && catalog != null)
                catalog.TryGet(AllyLineCatalog.UnitIdForLine(line, Rarity.Common), out def);
            if (def == null || string.IsNullOrWhiteSpace(def.BattlePrefabResourcesPath))
                return null;

            var path = def.BattlePrefabResourcesPath.Trim();
            if (Cache.TryGetValue(path, out var cached) && cached != null)
                return cached;

            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
                return null;

            var instance = Object.Instantiate(prefab);
            instance.name = "IdlePortrait_" + def.UnitId;
            SetLayerRecursively(instance, PortraitLayer);
            instance.transform.position = new Vector3(1000f, 1000f, 0f);
            instance.transform.rotation = Quaternion.identity;

            var skeleton = instance.GetComponentInChildren<SkeletonAnimation>(true);
            if (skeleton != null)
            {
                skeleton.Initialize(true);
                var idle = skeleton.SkeletonDataAsset?.GetSkeletonData(false)?.FindAnimation("idle");
                if (idle != null)
                    skeleton.AnimationState.SetAnimation(0, idle.Name, true);
                skeleton.Update(0f);
                skeleton.LateUpdate();
            }

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Object.Destroy(instance);
                return null;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var cameraGo = new GameObject("IdlePortraitCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << PortraitLayer;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 10f);
            camera.transform.rotation = Quaternion.identity;
            camera.orthographicSize = Mathf.Max(0.25f, Mathf.Max(bounds.extents.y, bounds.extents.x) * 1.18f);

            var rt = RenderTexture.GetTemporary(TextureSize, TextureSize, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            camera.targetTexture = rt;
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, TextureSize, TextureSize), 0, 0);
            texture.Apply(false, false);
            RenderTexture.active = previous;

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = def.UnitId + "_BattleIdleFrame0";
            Cache[path] = sprite;

            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            // Destroy() is deferred until the end of the frame. Disable both
            // objects immediately so the next ally capture cannot include this
            // skeleton or reuse this camera and create a stacked portrait.
            instance.SetActive(false);
            cameraGo.SetActive(false);
            Object.Destroy(cameraGo);
            Object.Destroy(instance);
            return sprite;
        }

        static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            for (var i = 0; i < root.transform.childCount; i++)
                SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
        }
    }
}
