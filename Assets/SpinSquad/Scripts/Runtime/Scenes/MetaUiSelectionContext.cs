using SpinSquad.Data;

namespace SpinSquad.Scenes
{
    /// <summary>
    /// Runtime-only handoff context between list scene and detail scene.
    /// </summary>
    public static class MetaUiSelectionContext
    {
        static bool _hasSelectedLine;
        static bool _hasSelectedTreasure;
        static bool _hasTreasureListReturnTab;

        public static int SelectedLineIndex { get; private set; } = -1;
        public static Rarity SelectedLineRarity { get; private set; } = Rarity.Common;
        public static string SelectedTreasureId { get; private set; } = string.Empty;
        public static Rarity TreasureListReturnTab { get; private set; } = Rarity.Common;

        public static void SetSelectedUpgrade(int lineIndex, Rarity rarity)
        {
            SelectedLineIndex = lineIndex;
            SelectedLineRarity = rarity;
            _hasSelectedLine = true;
            SelectedTreasureId = string.Empty;
            _hasSelectedTreasure = false;
        }

        public static void SetSelectedLine(int lineIndex)
        {
            SetSelectedUpgrade(lineIndex, Rarity.Common);
        }

        public static void SetSelectedTreasure(string treasureId)
        {
            SetSelectedTreasure(treasureId, Rarity.Common);
        }

        public static void SetSelectedTreasure(string treasureId, Rarity returnTab)
        {
            SelectedTreasureId = treasureId ?? string.Empty;
            _hasSelectedTreasure = !string.IsNullOrWhiteSpace(SelectedTreasureId);
            TreasureListReturnTab = returnTab;
            _hasTreasureListReturnTab = _hasSelectedTreasure;
            SelectedLineIndex = -1;
            SelectedLineRarity = Rarity.Common;
            _hasSelectedLine = false;
        }

        public static bool TryConsumeSelectedUpgrade(out int lineIndex, out Rarity rarity)
        {
            if (_hasSelectedLine)
            {
                lineIndex = SelectedLineIndex;
                rarity = SelectedLineRarity;
                Clear();
                return true;
            }

            lineIndex = -1;
            rarity = Rarity.Common;
            return false;
        }

        public static bool TryConsumeSelectedLine(out int lineIndex)
        {
            if (_hasSelectedLine)
            {
                lineIndex = SelectedLineIndex;
                Clear();
                return true;
            }

            lineIndex = -1;
            return false;
        }

        public static bool TryConsumeSelectedTreasure(out string treasureId)
        {
            if (_hasSelectedTreasure)
            {
                treasureId = SelectedTreasureId;
                ClearTreasureSelection();
                return true;
            }

            treasureId = string.Empty;
            return false;
        }

        public static bool TryConsumeTreasureListReturnTab(out Rarity tab)
        {
            if (_hasTreasureListReturnTab)
            {
                tab = TreasureListReturnTab;
                _hasTreasureListReturnTab = false;
                return true;
            }

            tab = Rarity.Common;
            return false;
        }

        static void ClearTreasureSelection()
        {
            SelectedTreasureId = string.Empty;
            _hasSelectedTreasure = false;
        }

        public static void Clear()
        {
            SelectedLineIndex = -1;
            SelectedLineRarity = Rarity.Common;
            SelectedTreasureId = string.Empty;
            TreasureListReturnTab = Rarity.Common;
            _hasSelectedLine = false;
            _hasSelectedTreasure = false;
            _hasTreasureListReturnTab = false;
        }
    }
}
