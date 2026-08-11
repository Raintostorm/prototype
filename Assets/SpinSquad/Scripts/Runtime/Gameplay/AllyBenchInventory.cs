using System.Collections.Generic;
using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>Bench 4×4 (16 ô) dưới lưới ally — lưu ally dư, auto-merge 3→1.</summary>
    public sealed class AllyBenchInventory
    {
        public const int SlotCount = BattleGrid.InventorySlotCount;

        readonly struct BenchEntry
        {
            public readonly string UnitId;
            public readonly int LineIndex;
            public readonly Rarity RarityTier;
            public readonly float Hp;
            public readonly float Atk;

            public BenchEntry(string unitId, int lineIndex, Rarity rarityTier, float hp, float atk)
            {
                UnitId = unitId;
                LineIndex = lineIndex;
                RarityTier = rarityTier;
                Hp = hp;
                Atk = atk;
            }

            public bool Matches(int line, Rarity rarity) =>
                LineIndex == line && RarityTier == rarity;
        }

        readonly BenchEntry?[] _slots = new BenchEntry?[SlotCount];
        readonly GameObject[] _visuals = new GameObject[SlotCount];
        readonly DuelDirector _director;
        readonly UnitCatalog _catalog;
        readonly Transform _visualsRoot;
        bool _suppressAutoMerge;

        public AllyBenchInventory(DuelDirector director, UnitCatalog catalog, Transform parent)
        {
            _director = director;
            _catalog = catalog;
            var rootGo = new GameObject("AllyBenchInventory");
            rootGo.transform.SetParent(parent, false);
            _visualsRoot = rootGo.transform;
        }

        public int OccupiedCount
        {
            get
            {
                var n = 0;
                for (var i = 0; i < SlotCount; i++)
                {
                    if (_slots[i].HasValue)
                        n++;
                }

                return n;
            }
        }

        public bool HasFreeSlot => FindFreeSlotIndex() >= 0;

        public void ApplyBagVisibility(bool prepPhase, bool showBagUnits)
        {
            if (_visualsRoot != null)
                _visualsRoot.gameObject.SetActive(prepPhase && showBagUnits);
        }

        public void ClearAll()
        {
            for (var i = 0; i < SlotCount; i++)
                ClearSlot(i);
        }

        public bool TryAdd(UnitDefinition def, Rarity tier, float hp, float atk, int lineIndex)
        {
            if (def == null)
                return false;
            var slot = FindFreeSlotIndex();
            if (slot < 0)
                return false;

            lineIndex = AllyLineCatalog.ClampLineIndex(lineIndex);
            _slots[slot] = new BenchEntry(def.UnitId, lineIndex, tier, hp, atk);
            SpawnVisual(slot, def, tier, hp, atk, lineIndex);
            if (!_suppressAutoMerge)
                TryAutoMergeAll();
            return true;
        }

        public bool TryDeploySlotToBoard(int slotIndex)
        {
            if (!_director.SandboxCanMutateUnits)
                return false;
            if (slotIndex < 0 || slotIndex >= SlotCount || !_slots[slotIndex].HasValue)
                return false;

            var e = _slots[slotIndex].Value;
            if (!_catalog.TryGet(e.UnitId, out var def))
                return false;

            if (!_director.TryPickBoardCellForAllyAdd(e.LineIndex, e.RarityTier, out var row, out var col))
                return false;

            ClearSlot(slotIndex);
            _director.SpawnAllyOnBoardFromBench(row, col, def, e.RarityTier, e.Hp, e.Atk, e.LineIndex);
            return true;
        }

        public void TryAutoMergeAll()
        {
            if (_director == null || !_director.SandboxCanMutateUnits)
                return;

            var safety = 0;
            while (safety++ < 24)
            {
                if (!TryAutoMergeOneGroup())
                    break;
            }
        }

        bool TryAutoMergeOneGroup()
        {
            var groups = new Dictionary<(int line, Rarity rarity), List<int>>();
            for (var i = 0; i < SlotCount; i++)
            {
                if (!_slots[i].HasValue)
                    continue;
                var e = _slots[i].Value;
                var key = (e.LineIndex, e.RarityTier);
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<int>(4);
                    groups[key] = list;
                }

                list.Add(i);
            }

            foreach (var pair in groups)
            {
                var (line, rarity) = pair.Key;
                var indices = pair.Value;
                if (indices.Count < AllyMergeRules.AlliesRequiredForAutoMerge)
                    continue;
                if (!AllyMergeRules.CanMerge(rarity))
                    continue;

                indices.Sort();
                var targetSlot = indices[0];
                for (var v = 0; v < AllyMergeRules.AlliesRequiredForAutoMerge; v++)
                    ClearSlot(indices[v]);

                var newRarity = AllyMergeRules.NextRarity(rarity);
                var catalogId = AllyLineCatalog.UnitIdForLine(line, newRarity);
                if (!_catalog.TryGet(catalogId, out var def))
                    return true;

                var (hp, atk) = AllyStatScaling.ScaleStats(def, newRarity);
                _suppressAutoMerge = true;
                _slots[targetSlot] = new BenchEntry(def.UnitId, line, newRarity, hp, atk);
                SpawnVisual(targetSlot, def, newRarity, hp, atk, line);
                _suppressAutoMerge = false;
                return true;
            }

            return false;
        }

        int FindFreeSlotIndex()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                if (!_slots[i].HasValue)
                    return i;
            }

            return -1;
        }

        void ClearSlot(int slot)
        {
            _slots[slot] = null;
            if (_visuals[slot] != null)
            {
                Object.Destroy(_visuals[slot]);
                _visuals[slot] = null;
            }
        }

        void SpawnVisual(int slot, UnitDefinition def, Rarity tier, float hp, float atk, int lineIndex)
        {
            if (_visuals[slot] != null)
            {
                Object.Destroy(_visuals[slot]);
                _visuals[slot] = null;
            }

            var pos = BattleGrid.GetInventorySlotCenter(slot);
            var go = _director.CreateBenchAllyVisual(def, tier, hp, atk, lineIndex, pos);
            var marker = go.GetComponent<BenchAllyMarker>();
            marker.SlotIndex = slot;
            _visuals[slot] = go;
        }
    }
}
