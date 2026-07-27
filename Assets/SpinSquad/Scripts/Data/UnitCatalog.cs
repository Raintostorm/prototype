using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpinSquad.Data
{
    [CreateAssetMenu(menuName = "SpinSquad/Data/Unit Catalog", fileName = "UnitCatalog")]
    public sealed class UnitCatalog : ScriptableObject
    {
        [SerializeField] private List<UnitDefinition> units = new();

        private Dictionary<string, UnitDefinition> _byId;

        public IReadOnlyList<UnitDefinition> All => units;

        private void OnEnable()
        {
            RebuildIndex();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildIndex();
        }
#endif

        public void RebuildIndex()
        {
            _byId = new Dictionary<string, UnitDefinition>(StringComparer.Ordinal);
            foreach (var u in units)
            {
                if (u == null || string.IsNullOrWhiteSpace(u.UnitId))
                    continue;
                var key = u.UnitId.Trim();
                if (_byId.ContainsKey(key))
                {
                    Debug.LogWarning($"[UnitCatalog] Duplicate unitId \"{key}\" — keeping first.");
                    continue;
                }

                _byId[key] = u;
            }
        }

        public bool TryGet(string unitId, out UnitDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(unitId))
                return false;
            if (_byId == null)
                RebuildIndex();
            return _byId.TryGetValue(unitId.Trim(), out definition);
        }

        public UnitDefinition GetOrNull(string unitId)
        {
            return TryGet(unitId, out var d) ? d : null;
        }

#if UNITY_EDITOR
        public void SetEditorEntries(List<UnitDefinition> list)
        {
            units = list ?? new List<UnitDefinition>();
            RebuildIndex();
        }
#endif
    }
}
