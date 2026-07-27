#if UNITY_EDITOR
using System.Linq;
using SpinSquad.Data;
using UnityEditor;
using UnityEngine;

namespace SpinSquad.EditorTools
{
    public static class UnitCatalogVerifier
    {
        [MenuItem("SpinSquad/Data/Verify Unit Catalog Resolution", priority = 11)]
        public static void Verify()
        {
            var guids = AssetDatabase.FindAssets("t:UnitCatalog");
            if (guids.Length == 0)
            {
                Debug.LogError("[SpinSquad] No UnitCatalog asset found. Run SpinSquad/Data/Generate Sample Unit Data first.");
                return;
            }

            var path = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p.Contains("UnitCatalog_Main") ? 0 : 1)
                .First();

            var catalog = AssetDatabase.LoadAssetAtPath<UnitCatalog>(path);
            if (catalog == null)
            {
                Debug.LogError($"[SpinSquad] Failed to load catalog at {path}");
                return;
            }

            catalog.RebuildIndex();

            var ids = new[] { "unit_brush_warden", "unit_slip_slinger", "unit_moss_oracle", "missing_id_xyz" };
            var ok = 0;
            foreach (var id in ids)
            {
                if (catalog.TryGet(id, out var def))
                {
                    ok++;
                    Debug.Log($"[SpinSquad] OK TryGet(\"{id}\") -> {def.DisplayName} ({def.Rarity}) HP={def.MaxHitPoints} ATK={def.Attack}");
                }
                else
                {
                    if (id == "missing_id_xyz")
                        Debug.Log($"[SpinSquad] Expected miss for \"{id}\".");
                    else
                        Debug.LogError($"[SpinSquad] FAIL TryGet(\"{id}\") — run Generate Sample Unit Data.");
                }
            }

            if (ok != 3)
                Debug.LogWarning("[SpinSquad] Verify finished with warnings (expected 3 successful lookups + 1 intentional miss).");
            else
                Debug.Log("[SpinSquad] Verify: all sample ids resolved; intentional miss OK.");
        }
    }
}
#endif
