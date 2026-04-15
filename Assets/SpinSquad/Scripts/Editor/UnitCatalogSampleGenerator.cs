#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SpinSquad.Data;
using UnityEditor;
using UnityEngine;

namespace SpinSquad.EditorTools
{
    public static class UnitCatalogSampleGenerator
    {
        const string UnitsDir = "Assets/SpinSquad/Data/Units";
        const string CatalogPath = "Assets/SpinSquad/Resources/UnitCatalog_Main.asset";

        [MenuItem("SpinSquad/Data/Generate Sample Unit Data", priority = 10)]
        public static void Generate()
        {
            Directory.CreateDirectory(UnitsDir);

            var u1 = GetOrCreateUnit("unit_brush_warden", "Brush Warden", Rarity.Rare, 120f, 20f, UnitTeamKind.Ally);
            var u2 = GetOrCreateUnit("unit_slip_slinger", "Slip Slinger", Rarity.Common, 100f, 18f, UnitTeamKind.Ally);
            var u3 = GetOrCreateUnit("unit_moss_oracle", "Moss Oracle", Rarity.Epic, 88f, 15f, UnitTeamKind.Enemy);

            var catalog = AssetDatabase.LoadAssetAtPath<UnitCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<UnitCatalog>();
                catalog.SetEditorEntries(new List<UnitDefinition> { u1, u2, u3 });
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            else
            {
                catalog.SetEditorEntries(new List<UnitDefinition> { u1, u2, u3 });
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SpinSquad] Sample data ready: {CatalogPath}");
        }

        static UnitDefinition GetOrCreateUnit(string id, string display, Rarity r, float hp, float atk, UnitTeamKind team)
        {
            var path = $"{UnitsDir}/{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<UnitDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<UnitDefinition>();
                def.SetEditorData(id, display, r, hp, atk, team);
                AssetDatabase.CreateAsset(def, path);
            }
            else
            {
                def.SetEditorData(id, display, r, hp, atk, team);
                EditorUtility.SetDirty(def);
            }

            return def;
        }
    }
}
#endif
