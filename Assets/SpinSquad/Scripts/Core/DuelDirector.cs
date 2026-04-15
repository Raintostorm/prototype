using System.Collections;
using System.Collections.Generic;
using SpinSquad.Data;
using SpinSquad.Gacha;
using SpinSquad.Meta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SpinSquad.Core
{
    public readonly struct AllyWaveSnapshot
    {
        public readonly int Row;
        public readonly int Col;
        public readonly string UnitId;
        public readonly Rarity Rarity;
        public readonly float MaxHp;
        public readonly float Attack;
        public readonly bool Ranged;
        public readonly float AttackRange;
        public readonly float RangedInterval;
        public readonly UnitBodyShape Shape;
        public readonly int AllyLineIndex;

        public AllyWaveSnapshot(
            int row,
            int col,
            string unitId,
            Rarity rarity,
            float maxHp,
            float attack,
            bool ranged,
            float attackRange,
            float rangedInterval,
            UnitBodyShape shape,
            int allyLineIndex)
        {
            Row = row;
            Col = col;
            UnitId = unitId;
            Rarity = rarity;
            MaxHp = maxHp;
            Attack = attack;
            Ranged = ranged;
            AttackRange = attackRange;
            RangedInterval = rangedInterval;
            Shape = shape;
            AllyLineIndex = allyLineIndex;
        }
    }

    /// <summary>
    /// Tạo fighter, UI, nút Bắt đầu / Chơi lại / Thêm ally; nhiều ally vs nhiều enemy theo wave.
    /// Thắng wave khi hết enemy (sau delay); thua khi hết ally. Tối đa 10 wave. Sau thắng wave, ally chết được hồi sinh full HP đúng ô đã snapshot lúc Bắt đầu wave đó, rồi mới cho Thêm ally.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class DuelDirector : MonoBehaviour
    {
        public const int MaxWaves = 10;
        public const int MaxLevels = 3;

        const int MaxEnemyUnitsOnGrid =
            BattleGrid.GridSize * BattleGrid.GridSize * AllyMergeRules.MaxStackPerCell;

        readonly struct AllyKey
        {
            public readonly int LineIndex;
            public readonly Rarity RarityTier;

            public AllyKey(int lineIndex, Rarity rarityTier)
            {
                LineIndex = AllyLineCatalog.ClampLineIndex(lineIndex);
                RarityTier = rarityTier;
            }

            public bool Matches(AllyInstanceSpec spec) =>
                spec != null && spec.AllyLineIndex == LineIndex && spec.RarityTier == RarityTier;
        }

        [SerializeField] private float allyMaxHp = 100f;
        [SerializeField] private float enemyMaxHp = 40f;
        [SerializeField] private float allyStrikeDamage = 10f;
        [SerializeField] private float enemyStrikeDamage = 5f;

        [SerializeField] private int currentWave = 1;
        [SerializeField] private int currentLevel = 1;

        [SerializeField] private UnitCatalog unitCatalog;
        [SerializeField] private string allyUnitId = "unit_slip_slinger";
        [SerializeField] private string enemyUnitId = "unit_moss_oracle";

        /// <summary>Spawn wave lẻ: melee vuông (Moss); wave chẵn: ranged tam giác (Spore).</summary>
        const string WaveEnemyMeleeCatalogId = "unit_moss_oracle";
        const string WaveEnemyRangedCatalogId = "unit_enemy_ranged";

        [Tooltip("Sau khi hết enemy, chờ bấy nhiêu giây rồi mới kết thúc wave (đếm ngược: round(delay) bước, mỗi bước delay/steps).")]
        [SerializeField] private float waveCompleteDelaySeconds = 2f;

        [Tooltip("Scene load sau khi xong 10 wave — trang Homepage (phải có trong Build Settings).")]
        [SerializeField] private string homepageSceneName = "Homepage";

        [Tooltip("BoxCollider2D local trên ally (scale ~0.095) — prep để dễ kéo / trùng sprite.")]
        [SerializeField] private Vector2 allyPrepColliderLocalSize = new(4.2f, 4.2f);

        [Tooltip("Nhả trong bấy nhiêu giây + slop dưới đây thì mở menu ô (tap).")]
        [SerializeField] private float allyCellContextTapMaxSeconds = 0.45f;

        [SerializeField] private float allyCellContextTapSlopScreenPixels = 28f;

        [Tooltip("Coin roll khi vào / restart trận (40 = 2 lần roll × 20).")]
        [SerializeField] private int startingRollCoins = 40;

        [Tooltip("Cộng coin roll sau mỗi wave thắng (prep wave kế).")]
        [SerializeField] private int wavePrepRollCoinGrant = 40;

        [Tooltip("Coin roll hoàn lại mỗi lần bán 1 ally khỏi ô (prep).")]
        [SerializeField] int sellAllyCoinRefund = 12;

        [Tooltip("Bật để hiện nhãn debug key ally trong prep (Lx/Rarity).")]
        [SerializeField] bool showAllyDebugKeyOverlay;

        static readonly Color PrepRollButtonColor = new Color(0.55f, 0.22f, 0.45f, 0.96f);
        static readonly Color PrepStartButtonColor = new Color(0.18f, 0.42f, 0.72f, 0.98f);

        static readonly Color RollSlotAllyText = Color.white;
        static readonly Color RollSlotCoinText = new Color(1f, 0.88f, 0.2f, 1f);
        static readonly Color RollSlotBuffText = new Color(0.35f, 0.95f, 0.5f, 1f);
        static readonly Color RollSlotAllyBg = new Color(0.22f, 0.24f, 0.3f, 0.95f);
        static readonly Color RollSlotCoinBg = new Color(0.28f, 0.22f, 0.08f, 0.95f);
        static readonly Color RollSlotBuffBg = new Color(0.1f, 0.26f, 0.16f, 0.95f);

        public bool BattleEnded { get; private set; }

        public bool CombatStarted { get; private set; }

        public bool IsDraggingGridUnit { get; private set; }

        public int CurrentWave => currentWave;
        public int CurrentLevel => currentLevel;

        public string CurrentAllyUnitId => allyUnitId;

        public string CurrentEnemyUnitId => enemyUnitId;

        /// <summary>Wave chẵn → enemy ranged tam giác; wave lẻ → melee vuông.</summary>
        public static bool WaveUsesRangedEnemy(int wave) =>
            Mathf.Clamp(wave, 1, MaxWaves) % 2 == 0;

        UnitDefinition ResolveEnemyDefinitionForWave(int wave)
        {
            if (unitCatalog == null)
                return null;
            var id = WaveUsesRangedEnemy(wave) ? WaveEnemyRangedCatalogId : WaveEnemyMeleeCatalogId;
            if (unitCatalog.TryGet(id, out var def))
                return def;
            if (unitCatalog.TryGet(enemyUnitId, out var fb))
                return fb;
            return null;
        }

        float CurrentLevelEnemyHpMultiplier() => currentLevel switch
        {
            1 => 1f,
            2 => 1.15f,
            3 => 1.3f,
            _ => 1f
        };

        float CurrentLevelEnemyAtkMultiplier() => currentLevel switch
        {
            1 => 1f,
            2 => 1.12f,
            3 => 1.24f,
            _ => 1f
        };

        int LevelClearGoldReward() => MetaProgressionStore.LevelCompleteGoldReward(currentLevel);

        int LevelClearTreasureKeyReward() => MetaProgressionStore.LevelCompleteTreasureKeyReward();

        void BuildWaveIntroBannerText()
        {
            if (unitCatalog != null && unitCatalog.TryGet(allyUnitId, out var allyDef))
            {
                var ed = ResolveEnemyDefinitionForWave(currentWave);
                _bannerIntroText = ed != null
                    ? $"Level {currentLevel} - Wave {currentWave}: {allyDef.DisplayName}  vs  {ed.DisplayName}"
                    : $"Level {currentLevel} - Wave {currentWave}: {allyDef.DisplayName}  vs  ?";
            }
            else
                _bannerIntroText = $"Level {currentLevel} - Wave {currentWave} (fallback)";
        }

        /// <summary>Dùng scene test: đổi id unit từ catalog rồi spawn lại toàn bộ trận (prep, chưa combat).</summary>
        public void ApplyTestLoadout(string allyId, string enemyId)
        {
            if (!string.IsNullOrWhiteSpace(allyId))
                allyUnitId = allyId.Trim();
            if (!string.IsNullOrWhiteSpace(enemyId))
                enemyUnitId = enemyId.Trim();
            unitCatalog?.RebuildIndex();
            RestartDuel();
        }

        /// <summary>Scene test: chỉ cho thêm/xóa lẻ khi chưa combat (tránh phá logic wave).</summary>
        public bool SandboxCanMutateUnits => !CombatStarted && !BattleEnded;

        /// <summary>Thêm 1 ally theo id catalog vào ô trống ngẫu nhiên (prep).</summary>
        public bool SandboxTryAddAlly(string catalogUnitId)
        {
            return SandboxTryAddInternal(catalogUnitId, UnitTeamKind.Ally);
        }

        /// <summary>Thêm 1 enemy theo id catalog vào ô trống ngẫu nhiên (prep).</summary>
        public bool SandboxTryAddEnemy(string catalogUnitId)
        {
            return SandboxTryAddInternal(catalogUnitId, UnitTeamKind.Enemy);
        }

        /// <summary>Xóa ngẫu nhiên 1 đơn vị đúng phe + kiểu cận/xa (prep).</summary>
        public bool SandboxTryRemoveOne(bool isEnemy, bool ranged)
        {
            if (!SandboxCanMutateUnits)
                return false;

            var pool = isEnemy ? (IReadOnlyList<CombatHealth>)_enemies : _allies;
            var candidates = new List<CombatHealth>(8);
            foreach (var h in pool)
            {
                if (h == null || h.IsDead)
                    continue;
                var actor = h.GetComponent<DuelActor>();
                if (actor == null || actor.IsRangedCombat != ranged)
                    continue;
                candidates.Add(h);
            }

            if (candidates.Count == 0)
                return false;

            var pick = candidates[Random.Range(0, candidates.Count)];
            pick.TakeDamage(pick.Max + 999f);
            return true;
        }

        bool SandboxTryAddInternal(string catalogUnitId, UnitTeamKind expectedTeam)
        {
            if (!SandboxCanMutateUnits || unitCatalog == null || string.IsNullOrWhiteSpace(catalogUnitId))
                return false;
            if (!unitCatalog.TryGet(catalogUnitId.Trim(), out var def) || def.TeamKind != expectedTeam)
                return false;

            if (expectedTeam == UnitTeamKind.Ally)
            {
                var line = AllyLineCatalog.LineIndexFromUnitId(def.UnitId);
                if (CountLivingAllies() >= 16 || !TryPickRandomAllyCellForAdd(line, def.Rarity, out var row, out var col))
                    return false;
                var (ahp, aatk) = AllyStatScaling.ScaleStats(def, def.Rarity);
                SpawnAllyStackMember(row, col, def, def.Rarity, ahp, aatk, line);
                return true;
            }

            if (CountLivingEnemies() >= MaxEnemyUnitsOnGrid || !TryPickRandomEnemyCellForAdd(def, out var er, out var ec))
                return false;

            var tier = def != null ? def.Rarity : Rarity.Common;
            var hp = def != null ? def.MaxHitPoints : enemyMaxHp;
            var atk = def != null ? def.Attack : enemyStrikeDamage;
            SpawnEnemyStackMember(er, ec, def, tier, hp, atk);
            return true;
        }

        int CountLivingEnemies()
        {
            var n = 0;
            foreach (var e in _enemies)
            {
                if (e != null && !e.IsDead)
                    n++;
            }

            return n;
        }

        static string EnemyStackUnitId(UnitDefinition def) => def != null ? def.UnitId : string.Empty;

        List<CombatHealth> GetEnemyMembersInCell(int row, int col)
        {
            var list = new List<CombatHealth>();
            foreach (var e in _enemies)
            {
                if (e == null || e.IsDead)
                    continue;
                BattleGrid.WorldToEnemyCell(EnemyGridSampleWorld(e), out var r, out var c);
                if (r == row && c == col)
                    list.Add(e);
            }

            return list;
        }

        int CountEnemyStackInCell(int row, int col, string unitId, Rarity rarity)
        {
            var n = 0;
            foreach (var e in _enemies)
            {
                if (e == null || e.IsDead)
                    continue;
                var spec = e.GetComponent<EnemyInstanceSpec>();
                if (spec == null)
                    continue;
                BattleGrid.WorldToEnemyCell(EnemyGridSampleWorld(e), out var r, out var c);
                if (r == row && c == col && spec.CatalogUnitId == unitId && spec.RarityTier == rarity)
                    n++;
            }

            return n;
        }

        bool CellAllowsAddEnemy(int row, int col, UnitDefinition def)
        {
            var members = GetEnemyMembersInCell(row, col);
            if (members.Count == 0)
                return true;
            if (members.Count >= AllyMergeRules.MaxStackPerCell)
                return false;
            var fs = members[0].GetComponent<EnemyInstanceSpec>();
            if (fs == null)
                return false;
            var unitId = EnemyStackUnitId(def);
            var tier = def != null ? def.Rarity : Rarity.Common;
            return fs.CatalogUnitId == unitId && fs.RarityTier == tier;
        }

        bool TryPickRandomEnemyCellForAdd(UnitDefinition def, out int row, out int col)
        {
            var unitId = EnemyStackUnitId(def);
            var tier = def != null ? def.Rarity : Rarity.Common;
            var stackPref = new List<(int r, int c)>(BattleGrid.GridSize * BattleGrid.GridSize);
            var emptyOrOther = new List<(int r, int c)>(BattleGrid.GridSize * BattleGrid.GridSize);
            for (var r = 0; r < BattleGrid.GridSize; r++)
            {
                for (var c = 0; c < BattleGrid.GridSize; c++)
                {
                    if (!CellAllowsAddEnemy(r, c, def) ||
                        CountEnemyStackInCell(r, c, unitId, tier) >= AllyMergeRules.MaxStackPerCell)
                        continue;
                    var cnt = CountEnemyStackInCell(r, c, unitId, tier);
                    if (cnt > 0)
                        stackPref.Add((r, c));
                    else
                        emptyOrOther.Add((r, c));
                }
            }

            var candidates = stackPref.Count > 0 ? stackPref : emptyOrOther;
            if (candidates.Count == 0)
            {
                row = col = 0;
                return false;
            }

            var pick = candidates[Random.Range(0, candidates.Count)];
            row = pick.r;
            col = pick.c;
            return true;
        }

        EnemyInstanceSpec FindLeaderEnemyInCell(int row, int col, string unitId, Rarity rarity)
        {
            foreach (var e in _enemies)
            {
                if (e == null || e.IsDead)
                    continue;
                var spec = e.GetComponent<EnemyInstanceSpec>();
                if (spec == null || !spec.StackLeader)
                    continue;
                BattleGrid.WorldToEnemyCell(EnemyGridSampleWorld(e), out var r, out var c);
                if (r == row && c == col && spec.CatalogUnitId == unitId && spec.RarityTier == rarity)
                    return spec;
            }

            return null;
        }

        void SpawnEnemyStackMember(int row, int col, UnitDefinition def, Rarity tier, float hp, float atk)
        {
            var unitId = EnemyStackUnitId(def);
            var before = CountEnemyStackInCell(row, col, unitId, tier);
            if (before >= AllyMergeRules.MaxStackPerCell)
                return;

            var center = BattleGrid.GetEnemyCellCenter(row, col);
            var pos = center + StackVisualOffset(before);
            var tint = def != null ? RarityPalette.UnitTint(tier, def.TeamKind) : RarityPalette.UnitTint(tier, UnitTeamKind.Enemy);
            var shape = def != null ? def.BodyShape : UnitBodyShape.Square;
            var idx = CountLivingEnemies() + 1;
            var disp = def != null ? def.DisplayName : "Enemy";
            var enemyGo = CreateFighter($"{disp} (+{idx})", pos, tint, shape, false);
            ApplyEnemyDefinitionVisualScale(enemyGo.transform, def, shape);
            var es = enemyGo.AddComponent<EnemyInstanceSpec>();
            es.InitFromDefinition(def, tier, hp, atk);
            ConfigureEnemy(enemyGo, def);
            enemyGo.GetComponent<SpriteRenderer>().color = tint;

            if (before == 0)
                SetupAsStackLeader(enemyGo);
            else
            {
                var leaderSpec = FindLeaderEnemyInCell(row, col, unitId, tier);
                if (leaderSpec != null)
                    SetupEnemyStackFollower(enemyGo, leaderSpec);
            }

            RegisterEnemy(enemyGo);
        }

        static void SetupEnemyStackFollower(GameObject go, EnemyInstanceSpec leaderSpec)
        {
            var mySpec = go.GetComponent<EnemyInstanceSpec>();
            mySpec.SetStackFollower(leaderSpec);
            foreach (var old in go.GetComponents<AllyStackFollower>())
                Destroy(old);

            var leaderTf = leaderSpec.transform;
            var f = go.AddComponent<AllyStackFollower>();
            f.Init(leaderTf, go.transform.position - leaderTf.position);
            var drag = go.GetComponent<DuelUnitGridDrag>();
            if (drag != null)
                drag.enabled = false;
        }

        private Text _banner;
        private Text _prepCoinText;
        private Text _startCombatButtonLabel;
        private Button _startCombatButton;
        private Button _restartCombatButton;
        private Button _addAllyButton;
        private string _bannerIntroText;

        private readonly List<CombatHealth> _allies = new();
        private readonly List<CombatHealth> _enemies = new();

        /// <summary>Snapshot ally lúc bấm Bắt đầu wave — hồi sinh đúng id/rarity/chỉ số.</summary>
        private readonly List<AllyWaveSnapshot> _waveAllySnapshots = new();

        int _selectedAllyRow = -1;
        int _selectedAllyCol = -1;
        int _selectedEnemyRow = -1;
        int _selectedEnemyCol = -1;
        GameObject _allyMergeHint;
        GameObject _enemyMergeHint;

        Transform _allyCellPickersRoot;
        RectTransform _uiCanvasRt;
        GameObject _allyCellMenuRoot;
        RectTransform _allyCellMenuRt;
        Button _allyCtxSellButton;
        Button _allyCtxMergeButton;
        Button _allyCtxCloseButton;
        Text _allyCtxTitleText;

        bool _pendingAllyCellContextTap;
        int _pendingAllyCellRow;
        int _pendingAllyCellCol;
        Vector2 _pendingAllyCellPressScreen;
        float _pendingAllyCellPressUnscaledTime;
        float _pendingAllyCellMaxScreenDelta;

        /// <summary>Chỉ bật Thêm ally sau khi đã snap ally về ô (xong prep sau wave).</summary>
        private bool _prepSnapReadyForAddAlly = true;

        Coroutine _waveCompleteRoutine;
        bool _waveCompletePending;

        GameObject _rollUiRoot;
        Image[] _rollSlotBacks = new Image[6];
        Text[] _rollSlotTexts = new Text[6];
        Text _buffStatusText;
        Text _rollSummaryText;
        Text _rollJackpotText;
        bool _rollBusy;
        readonly System.Random _allyLineRandom = new((int)(System.DateTime.UtcNow.Ticks ^ System.Guid.NewGuid().GetHashCode()));

        private void Start()
        {
            EnsureDirectionalLightIfNeeded();
            EnsureUi();
            BattleGrid.BuildVisuals(transform);

            if (unitCatalog == null)
                unitCatalog = Resources.Load<UnitCatalog>("UnitCatalog_Main");

            unitCatalog?.RebuildIndex();

            SpawnAndRegisterFighters();
            BuildAllyCellPickers();
        }

        void Update()
        {
            HandlePrepAllyGridCellPointerPressForPendingMenu();
            RefreshPendingAllyCellContextTapScreenDelta();
            RefreshAllyCellMenuLayout();

            RefreshAddAllyButton();
            RefreshMergeUi();
            RefreshPrepPrimaryUi();
            RefreshBuffStatusBar();
        }

        void LateUpdate()
        {
            if (!PrepGridPointer.WasReleasedThisFrame())
                return;

            if (!_pendingAllyCellContextTap)
                return;

            if (!SandboxCanMutateUnits)
            {
                CancelPendingAllyCellContextTap();
                return;
            }

            var elapsed = Time.unscaledTime - _pendingAllyCellPressUnscaledTime;
            var withinTap =
                _pendingAllyCellMaxScreenDelta <= allyCellContextTapSlopScreenPixels &&
                elapsed <= allyCellContextTapMaxSeconds;

            if (withinTap)
            {
                _selectedEnemyRow = -1;
                _selectedEnemyCol = -1;
                OpenAllyCellContextMenu(_pendingAllyCellRow, _pendingAllyCellCol);
            }

            CancelPendingAllyCellContextTap();
        }

        internal void CancelPendingAllyCellContextTap()
        {
            _pendingAllyCellContextTap = false;
            _pendingAllyCellMaxScreenDelta = 0f;
        }

        void RefreshPendingAllyCellContextTapScreenDelta()
        {
            if (!_pendingAllyCellContextTap || !SandboxCanMutateUnits)
                return;
            if (!PrepGridPointer.IsPressed())
                return;
            if (!PrepGridPointer.TryGetPrimaryScreenPosition(out var s))
                return;

            var d = (s - _pendingAllyCellPressScreen).magnitude;
            if (d > _pendingAllyCellMaxScreenDelta)
                _pendingAllyCellMaxScreenDelta = d;
        }

        void RefreshAddAllyButton()
        {
            if (_addAllyButton == null)
                return;

            var prepPhase = !CombatStarted && !BattleEnded;
            _addAllyButton.gameObject.SetActive(prepPhase);
            if (prepPhase)
                _addAllyButton.interactable =
                    _prepSnapReadyForAddAlly && CountLivingAllies() < 16 && HasEmptyAllyCell();
        }

        public void BeginCombat()
        {
            if (CombatStarted || BattleEnded)
                return;

            if (RollWallet.Balance >= SixSlotRollResolver.RollCostCoins)
                return;

            if (_allies.Count == 0 || _enemies.Count == 0)
                return;

            ClearAllySelection();
            SnapshotAllyGridForPostWaveRestore();

            CombatStarted = true;
            if (_startCombatButton != null)
                _startCombatButton.gameObject.SetActive(false);
            if (_prepCoinText != null)
                _prepCoinText.gameObject.SetActive(false);
            if (_rollUiRoot != null)
                _rollUiRoot.SetActive(false);
        }

        void SnapshotAllyGridForPostWaveRestore()
        {
            _waveAllySnapshots.Clear();
            foreach (var a in _allies)
            {
                if (a == null || a.IsDead)
                    continue;
                var w = AllyGridSampleWorld(a);
                var snapped = BattleGrid.SnapWorldToAllyGrid(w);
                BattleGrid.WorldToAllyCell(snapped, out var row, out var col);
                var spec = a.GetComponent<AllyInstanceSpec>();
                if (spec != null && unitCatalog != null && unitCatalog.TryGet(spec.CatalogUnitId, out var def))
                {
                    _waveAllySnapshots.Add(new AllyWaveSnapshot(
                        row,
                        col,
                        spec.CatalogUnitId,
                        spec.RarityTier,
                        spec.CombatMaxHitPoints,
                        spec.CombatAttack,
                        def.RangedAttack,
                        def.AttackRange,
                        def.RangedShotIntervalSeconds,
                        spec.BodyShape,
                        spec.AllyLineIndex));
                }
                else if (unitCatalog != null && unitCatalog.TryGet(allyUnitId, out var d2))
                {
                    _waveAllySnapshots.Add(new AllyWaveSnapshot(
                        row,
                        col,
                        allyUnitId,
                        d2.Rarity,
                        allyMaxHp,
                        allyStrikeDamage,
                        d2.RangedAttack,
                        d2.AttackRange,
                        d2.RangedShotIntervalSeconds,
                        d2.BodyShape,
                        AllyLineCatalog.LineIndexFromUnitId(allyUnitId)));
                }
            }
        }

        void ClearAllAlliesOnly()
        {
            foreach (var a in _allies)
            {
                if (a == null)
                    continue;
                a.Died -= OnUnitDied;
                Destroy(a.gameObject);
            }

            _allies.Clear();
        }

        void ReviveAlliesFromLastWaveLayout()
        {
            ClearAllAlliesOnly();
            foreach (var s in _waveAllySnapshots)
            {
                if (unitCatalog == null || !unitCatalog.TryGet(s.UnitId, out var def))
                    continue;
                SpawnAllyStackMember(s.Row, s.Col, def, s.Rarity, s.MaxHp, s.Attack, s.AllyLineIndex);
            }

            if (_allies.Count != 0)
                return;

            if (unitCatalog != null && unitCatalog.TryGet(allyUnitId, out var allyDef))
            {
                var ar = BattleGrid.DefaultAllyRow;
                var ac = BattleGrid.DefaultAllyCol;
                var (rvHp, rvAtk) = AllyStatScaling.ScaleStats(allyDef, Rarity.Common);
                var rvLine = AllyLineCatalog.LineIndexFromUnitId(allyUnitId);
                SpawnAllyStackMember(ar, ac, allyDef, Rarity.Common, rvHp, rvAtk, rvLine);
            }
            else
            {
                var pos = BattleGrid.DefaultAllySpawn;
                var (fb2Hp, fb2Atk) = AllyStatScaling.ScaleFallback(allyMaxHp, allyStrikeDamage, Rarity.Common);
                var fb2Tint = RarityPalette.UnitTint(Rarity.Common, UnitTeamKind.Ally);
                var allyGo = CreateFighter("Ally", pos, fb2Tint, UnitBodyShape.Square, true);
                var spec = allyGo.GetComponent<AllyInstanceSpec>();
                spec.InitFromDefinition(null, Rarity.Common, fb2Hp, fb2Atk, 0);
                ConfigureAlly(allyGo, fb2Hp, fb2Atk, null);
                RegisterAlly(allyGo);
            }
        }

        /// <summary>Về trạng thái mới: wave 1, spawn lại ally mặc định + enemy, tắt combat.</summary>
        public void RestartDuel()
        {
            CancelPendingWaveComplete();
            IsDraggingGridUnit = false;
            CombatStarted = false;
            BattleEnded = false;
            ClearAllySelection();

            SpawnAndRegisterFighters();
            BuildAllyCellPickers();

            if (_startCombatButton != null)
                _startCombatButton.gameObject.SetActive(true);
        }

        internal void SetDraggingGridUnit(bool dragging)
        {
            IsDraggingGridUnit = dragging;
        }

        internal CombatHealth FindNearestLivingEnemy(Vector3 from)
        {
            CombatHealth best = null;
            var bestSqr = float.MaxValue;
            foreach (var e in _enemies)
            {
                if (e == null || e.IsDead)
                    continue;
                var sqr = (e.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = e;
                }
            }

            return best;
        }

        internal CombatHealth FindNearestLivingAlly(Vector3 from)
        {
            CombatHealth best = null;
            var bestSqr = float.MaxValue;
            foreach (var a in _allies)
            {
                if (a == null || a.IsDead)
                    continue;
                var sqr = (AllyGridSampleWorld(a) - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = a;
                }
            }

            return best;
        }

        void SpawnAndRegisterFighters()
        {
            TeardownFighters();

            BattleRunBuffs.Clear();
            PendingRollGrants.Clear();
            var metaPassives = MetaProgressionStore.GetTreasurePassiveTotals();
            RollWallet.ResetForNewBattle(startingRollCoins + metaPassives.StartRollCoinBonus);

            currentLevel = Mathf.Clamp(MetaProgressionStore.SelectedLevel, 1, MaxLevels);
            currentWave = 1;
            BattleEnded = false;
            CombatStarted = false;
            CancelPendingWaveComplete();
            _waveAllySnapshots.Clear();
            _prepSnapReadyForAddAlly = true;

            var spawnedFromCatalog = false;
            if (unitCatalog != null && unitCatalog.TryGet(allyUnitId, out var allyDef))
            {
                var waveEnemyDef = ResolveEnemyDefinitionForWave(currentWave);
                if (waveEnemyDef == null && unitCatalog.TryGet(enemyUnitId, out var ep))
                    waveEnemyDef = ep;

                if (waveEnemyDef != null)
                {
                    var ar = BattleGrid.DefaultAllyRow;
                    var ac = BattleGrid.DefaultAllyCol;
                    var (awHp, awAtk) = AllyStatScaling.ScaleStats(allyDef, Rarity.Common);
                    var startLine = AllyLineCatalog.LineIndexFromUnitId(allyUnitId);
                    for (var i = 0; i < AllyMergeRules.MaxStackPerCell; i++)
                        SpawnAllyStackMember(ar, ac, allyDef, Rarity.Common, awHp, awAtk, startLine);

                    ClearEnemiesOnly();
                    var er = BattleGrid.DefaultEnemyRow;
                    var ec = BattleGrid.DefaultEnemyCol;
                    var et = waveEnemyDef.Rarity;
                    var ehp = waveEnemyDef.MaxHitPoints * CurrentLevelEnemyHpMultiplier();
                    var eatk = waveEnemyDef.Attack * CurrentLevelEnemyAtkMultiplier();
                    for (var i = 0; i < AllyMergeRules.MaxStackPerCell; i++)
                        SpawnEnemyStackMember(er, ec, waveEnemyDef, et, ehp, eatk);
                    spawnedFromCatalog = true;
                }
            }

            if (!spawnedFromCatalog)
            {
                if (unitCatalog == null)
                    Debug.LogWarning("[DuelDirector] Chưa gán UnitCatalog — dùng fallback không đọc data.");
                else
                    Debug.LogWarning("[DuelDirector] Không tìm thấy unit id — dùng fallback. Kiểm tra allyUnitId / enemyUnitId.");

                var (fbHp, fbAtk) = AllyStatScaling.ScaleFallback(allyMaxHp, allyStrikeDamage, Rarity.Common);
                var fbTint = RarityPalette.UnitTint(Rarity.Common, UnitTeamKind.Ally);
                var allyGo = CreateFighter("Ally", BattleGrid.DefaultAllySpawn, fbTint, UnitBodyShape.Square, true);
                const float fallbackMeleeSquareScale = 0.55f;
                allyGo.transform.localScale = new Vector3(fallbackMeleeSquareScale, fallbackMeleeSquareScale, 1f);
                var spec = allyGo.GetComponent<AllyInstanceSpec>();
                spec.InitFromDefinition(null, Rarity.Common, fbHp, fbAtk, 0);
                ConfigureAlly(allyGo, fbHp, fbAtk, null);
                RegisterAlly(allyGo);

                _bannerIntroText = $"Level {currentLevel} - Wave {currentWave} (fallback)";

                SpawnEnemiesForWaveWithTint(null);
            }
            else
                BuildWaveIntroBannerText();

            SetBanner(_bannerIntroText);
            RefreshPrepPrimaryUi();
        }

        void DrainPendingRollAllyGrants()
        {
            while (PendingRollGrants.TryDequeue(out var grant))
            {
                var line = AllyLineCatalog.ClampLineIndex(grant.AllyLineIndex);
                var catalogId = AllyLineCatalog.UnitIdForLine(line);
                if (unitCatalog == null || !unitCatalog.TryGet(catalogId, out var def))
                    continue;
                if (CountLivingAllies() >= 16)
                {
                    Debug.LogWarning("[DuelDirector] Bỏ qua ally từ roll — đã đủ 16 ally.");
                    break;
                }

                if (!TryPickRandomAllyCellForAdd(line, grant.RarityTier, out var row, out var col))
                {
                    Debug.LogWarning("[DuelDirector] Bỏ qua ally từ roll — không còn ô stack hợp lệ.");
                    break;
                }

                var (hp, atk) = AllyStatScaling.ScaleStats(def, grant.RarityTier);
                SpawnAllyStackMember(row, col, def, grant.RarityTier, hp, atk, line);
            }
        }

        /// <summary>Crit roll — damage đã gồm DmgPct từ ConfigureAlly.</summary>
        internal float GetOutgoingDamageForAlly(DuelActor attacker, float baseDamage)
        {
            if (attacker == null || attacker.Health.Faction != CombatFaction.Ally)
                return baseDamage;
            var treasure = MetaProgressionStore.GetTreasurePassiveTotals();
            var extraCrit = Mathf.Clamp01(treasure.AllyCritChanceFlat);
            if (!BattleRunBuffs.BuffsApply(currentWave))
            {
                if (extraCrit <= 0f)
                    return baseDamage;
                return Random.value < extraCrit ? baseDamage * 2f : baseDamage;
            }

            var c = Mathf.Clamp01(BattleRunBuffs.CritChance + extraCrit);
            if (c > 0f && Random.value < c)
                return baseDamage * 2f;
            return baseDamage;
        }

        void ConfigureAlly(GameObject allyGo, float maxHp, float strike, UnitDefinition def = null)
        {
            var hp = maxHp;
            var stk = strike;
            var lineIndex = 0;
            var rarityTier = def != null ? def.Rarity : Rarity.Common;
            var spec = allyGo.GetComponent<AllyInstanceSpec>();
            if (spec != null)
            {
                lineIndex = AllyLineCatalog.ClampLineIndex(spec.AllyLineIndex);
                rarityTier = spec.RarityTier;
            }
            else if (def != null)
                lineIndex = AllyLineCatalog.LineIndexFromUnitId(def.UnitId);

            hp *= 1f + MetaProgressionStore.GetLineUpgradeHpPct(lineIndex, rarityTier);
            stk *= 1f + MetaProgressionStore.GetLineUpgradeDmgPct(lineIndex, rarityTier);

            var treasure = MetaProgressionStore.GetTreasurePassiveTotals();
            hp *= 1f + treasure.AllyHpPct;
            stk *= 1f + treasure.AllyDmgPct;

            if (BattleRunBuffs.BuffsApply(currentWave))
            {
                hp *= 1f + BattleRunBuffs.HpPct;
                stk *= 1f + BattleRunBuffs.DmgPct;
            }

            var h = allyGo.GetComponent<CombatHealth>();
            h.Configure(CombatFaction.Ally, hp);
            var ranged = def != null && def.RangedAttack;
            var atkRange = def != null ? def.AttackRange : 1.65f;
            var interval = def != null ? def.RangedShotIntervalSeconds : 0.52f;
            var lineAtkSpeed = MetaProgressionStore.GetLineUpgradeAtkSpeedPct(lineIndex, rarityTier);
            if (lineAtkSpeed > 0f)
                interval = Mathf.Max(0.12f, interval / (1f + lineAtkSpeed));
            if (treasure.AllyAtkSpeedPct > 0f)
                interval = Mathf.Max(0.12f, interval / (1f + treasure.AllyAtkSpeedPct));
            if (BattleRunBuffs.BuffsApply(currentWave) && BattleRunBuffs.AtkSpeedPct > 0f)
                interval = Mathf.Max(0.12f, interval / (1f + BattleRunBuffs.AtkSpeedPct));

            var actor = allyGo.GetComponent<DuelActor>();
            actor.ResetMixedClinchStrikeBase(0.5f);
            actor.ConfigureCombat(stk, ranged, atkRange, interval);
            if (BattleRunBuffs.BuffsApply(currentWave) && BattleRunBuffs.AtkSpeedPct > 0f)
                actor.ScaleMixedClinchInterval(1f / (1f + BattleRunBuffs.AtkSpeedPct));
        }

        void ConfigureEnemy(GameObject enemyGo, UnitDefinition def)
        {
            var maxHp = def != null ? def.MaxHitPoints : enemyMaxHp;
            var strike = def != null ? def.Attack : enemyStrikeDamage;
            ConfigureEnemy(enemyGo, maxHp, strike, def);
        }

        void ConfigureEnemy(GameObject enemyGo, float maxHp, float strike, UnitDefinition def)
        {
            var hp = maxHp;
            var stk = strike;
            if (BattleRunBuffs.BuffsApply(currentWave))
            {
                hp *= 1f + BattleRunBuffs.HpPct;
                stk *= 1f + BattleRunBuffs.DmgPct;
            }

            var h = enemyGo.GetComponent<CombatHealth>();
            h.Configure(CombatFaction.Enemy, hp);
            var ranged = def != null && def.RangedAttack;
            var atkRange = def != null ? def.AttackRange : 1.65f;
            var interval = def != null ? def.RangedShotIntervalSeconds : 0.52f;
            if (BattleRunBuffs.BuffsApply(currentWave) && BattleRunBuffs.AtkSpeedPct > 0f)
                interval = Mathf.Max(0.12f, interval / (1f + BattleRunBuffs.AtkSpeedPct));

            enemyGo.GetComponent<DuelActor>().ConfigureCombat(stk, ranged, atkRange, interval);
        }

        void RegisterAlly(GameObject allyGo)
        {
            var h = allyGo.GetComponent<CombatHealth>();
            allyGo.GetComponent<DuelActor>().Init(this);
            allyGo.GetComponent<DuelUnitGridDrag>().Init(this);
            h.Died += OnUnitDied;
            _allies.Add(h);
        }

        void RegisterEnemy(GameObject enemyGo)
        {
            var h = enemyGo.GetComponent<CombatHealth>();
            enemyGo.GetComponent<DuelActor>().Init(this);
            enemyGo.GetComponent<DuelUnitGridDrag>().Init(this);
            h.Died += OnUnitDied;
            _enemies.Add(h);
        }

        void SpawnEnemiesForWaveWithTint(UnitDefinition enemyDef)
        {
            ClearEnemiesOnly();

            var n = Mathf.Clamp(currentWave, 1, MaxWaves);
            var tier = enemyDef != null ? enemyDef.Rarity : Rarity.Common;
            var hp = (enemyDef != null ? enemyDef.MaxHitPoints : enemyMaxHp) * CurrentLevelEnemyHpMultiplier();
            var atk = (enemyDef != null ? enemyDef.Attack : enemyStrikeDamage) * CurrentLevelEnemyAtkMultiplier();

            var placed = 0;
            for (var r = 0; r < BattleGrid.GridSize && placed < n; r++)
            {
                for (var c = 0; c < BattleGrid.GridSize && placed < n; c++)
                {
                    while (placed < n && CellAllowsAddEnemy(r, c, enemyDef) &&
                           CountEnemyStackInCell(r, c, EnemyStackUnitId(enemyDef), tier) <
                           AllyMergeRules.MaxStackPerCell)
                    {
                        SpawnEnemyStackMember(r, c, enemyDef, tier, hp, atk);
                        placed++;
                    }
                }
            }
        }

        void ClearEnemiesOnly()
        {
            foreach (var e in _enemies)
            {
                if (e == null)
                    continue;
                e.Died -= OnUnitDied;
                Destroy(e.gameObject);
            }

            _enemies.Clear();
        }

        void AddAllyFromButton()
        {
            if (CombatStarted || BattleEnded || !_prepSnapReadyForAddAlly)
                return;

            if (CountLivingAllies() >= 16)
                return;

            if (unitCatalog == null || !unitCatalog.TryGet(allyUnitId, out var def))
                return;

            var addLine = AllyLineCatalog.LineIndexFromUnitId(allyUnitId);
            if (!TryPickRandomAllyCellForAdd(addLine, Rarity.Common, out var row, out var col))
                return;

            var (hpA, atkA) = AllyStatScaling.ScaleStats(def, Rarity.Common);
            SpawnAllyStackMember(row, col, def, Rarity.Common, hpA, atkA, addLine);
        }

        int CountLivingAllies()
        {
            var n = 0;
            foreach (var a in _allies)
            {
                if (a != null && !a.IsDead)
                    n++;
            }

            return n;
        }

        bool HasEmptyAllyCell()
        {
            if (unitCatalog != null && unitCatalog.TryGet(allyUnitId, out var def))
                return TryPickRandomAllyCellForAdd(def, out _, out _);

            return HasFullyEmptyAllyCell();
        }

        bool HasFullyEmptyAllyCell()
        {
            for (var r = 0; r < BattleGrid.GridSize; r++)
            {
                for (var c = 0; c < BattleGrid.GridSize; c++)
                {
                    if (CountAnyAlliesInCell(r, c) == 0)
                        return true;
                }
            }

            return false;
        }

        bool TryPickRandomAllyCellForAdd(UnitDefinition def, out int row, out int col) =>
            TryPickRandomAllyCellForAdd(def, def.Rarity, out row, out col);

        bool TryPickRandomAllyCellForAdd(UnitDefinition def, Rarity stackTier, out int row, out int col)
        {
            var line = AllyLineCatalog.LineIndexFromUnitId(def.UnitId);
            return TryPickRandomAllyCellForAdd(line, stackTier, out row, out col);
        }

        bool TryPickRandomAllyCellForAdd(int allyLineIndex, Rarity stackTier, out int row, out int col)
        {
            allyLineIndex = AllyLineCatalog.ClampLineIndex(allyLineIndex);
            var stackPref = new List<(int r, int c)>(BattleGrid.GridSize * BattleGrid.GridSize);
            var emptyOrOther = new List<(int r, int c)>(BattleGrid.GridSize * BattleGrid.GridSize);
            for (var r = 0; r < BattleGrid.GridSize; r++)
            {
                for (var c = 0; c < BattleGrid.GridSize; c++)
                {
                    if (!CellAllowsAddAlly(r, c, allyLineIndex, stackTier))
                        continue;
                    var cnt = CountStackInCell(r, c, allyLineIndex, stackTier);
                    if (cnt > 0 && cnt < AllyMergeRules.MaxStackPerCell)
                        stackPref.Add((r, c));
                    else
                        emptyOrOther.Add((r, c));
                }
            }

            var candidates = stackPref.Count > 0 ? stackPref : emptyOrOther;
            if (candidates.Count == 0)
            {
                row = col = 0;
                return false;
            }

            candidates.Sort(CompareGridCell);
            row = candidates[0].r;
            col = candidates[0].c;
            return true;
        }

        static int CompareGridCell((int r, int c) a, (int r, int c) b)
        {
            var cmp = a.r.CompareTo(b.r);
            return cmp != 0 ? cmp : a.c.CompareTo(b.c);
        }

        /// <summary>Mỗi lần đấm đủ hai chiều (một lần gọi) → một cặp số damage tại chỗ va.</summary>
        internal void ReportHitExchange(
            float damageAllyReceived,
            float damageEnemyReceived,
            Vector3 allyWorldPos,
            Vector3 enemyWorldPos)
        {
            if (!CombatStarted || BattleEnded)
                return;

            var mid = (allyWorldPos + enemyWorldPos) * 0.5f;
            mid.z = 0f;

            var allyPop = Vector3.Lerp(mid, allyWorldPos, 0.4f) + Vector3.up * 0.12f;
            var enemyPop = Vector3.Lerp(mid, enemyWorldPos, 0.4f) + Vector3.up * 0.12f;

            if (damageAllyReceived > 0.0001f)
                FloatingDamagePopup.SpawnAt(allyPop, damageAllyReceived, new Color(0.55f, 0.88f, 1f, 1f));
            if (damageEnemyReceived > 0.0001f)
                FloatingDamagePopup.SpawnAt(enemyPop, damageEnemyReceived, new Color(1f, 0.4f, 0.32f, 1f));
        }

        /// <summary>Nhát làm đối thủ chết — trước đây không qua <see cref="ReportHitExchange"/>.</summary>
        internal void ReportKillingHit(CombatHealth victim, float damageDealt)
        {
            if (damageDealt <= 0f || victim == null)
                return;

            var pos = victim.transform.position + Vector3.up * 0.12f;
            var enemyHit = victim.Faction == CombatFaction.Enemy;
            FloatingDamagePopup.SpawnAt(
                pos,
                damageDealt,
                enemyHit ? new Color(1f, 0.4f, 0.32f, 1f) : new Color(0.55f, 0.88f, 1f, 1f));
        }

        void TeardownFighters()
        {
            ClearAllAlliesOnly();
            _waveAllySnapshots.Clear();
            ClearEnemiesOnly();
        }

        private void OnDestroy()
        {
            if (_allyMergeHint != null)
                Destroy(_allyMergeHint);
            if (_enemyMergeHint != null)
                Destroy(_enemyMergeHint);
            if (_allyCellPickersRoot != null)
                Destroy(_allyCellPickersRoot.gameObject);

            if (_startCombatButton != null)
                _startCombatButton.onClick.RemoveListener(OnPrepPrimaryClicked);
            if (_restartCombatButton != null)
                _restartCombatButton.onClick.RemoveListener(RestartDuel);
            if (_addAllyButton != null)
                _addAllyButton.onClick.RemoveListener(AddAllyFromButton);
            CancelPendingWaveComplete();

            foreach (var a in _allies)
            {
                if (a != null)
                    a.Died -= OnUnitDied;
            }

            foreach (var e in _enemies)
            {
                if (e != null)
                    e.Died -= OnUnitDied;
            }
        }

        private void OnUnitDied(CombatHealth dead)
        {
            if (dead == null)
                return;

            dead.Died -= OnUnitDied;

            _allies.Remove(dead);
            _enemies.Remove(dead);

            Destroy(dead.gameObject);

            if (!CombatStarted)
                return;

            var anyAlly = false;
            foreach (var a in _allies)
            {
                if (a != null && !a.IsDead)
                {
                    anyAlly = true;
                    break;
                }
            }

            var anyEnemy = false;
            foreach (var e in _enemies)
            {
                if (e != null && !e.IsDead)
                {
                    anyEnemy = true;
                    break;
                }
            }

            if (!anyAlly)
            {
                CancelPendingWaveComplete();
                EndBattleLoss();
                return;
            }

            if (!anyEnemy)
                ScheduleWaveCompleteAfterDelay();
        }

        void CancelPendingWaveComplete()
        {
            if (_waveCompleteRoutine != null)
            {
                StopCoroutine(_waveCompleteRoutine);
                _waveCompleteRoutine = null;
            }

            _waveCompletePending = false;
        }

        void ScheduleWaveCompleteAfterDelay()
        {
            if (_waveCompletePending)
                return;

            _waveCompletePending = true;
            _waveCompleteRoutine = StartCoroutine(WaveCompleteAfterDelayRoutine());
        }

        IEnumerator WaveCompleteAfterDelayRoutine()
        {
            var total = Mathf.Max(0.01f, waveCompleteDelaySeconds);
            var steps = Mathf.Max(1, Mathf.RoundToInt(total));
            var dt = total / steps;

            for (var n = steps; n >= 1; n--)
            {
                SetBanner(n.ToString());
                yield return new WaitForSeconds(dt);
            }

            _waveCompleteRoutine = null;
            _waveCompletePending = false;

            if (!CombatStarted || BattleEnded)
                yield break;

            if (HasLivingEnemy())
                yield break;

            if (!AnyLivingAllyIn(_allies))
            {
                EndBattleLoss();
                yield break;
            }

            EndBattleWaveWin();
        }

        static bool AnyLivingAllyIn(IReadOnlyList<CombatHealth> list)
        {
            foreach (var a in list)
            {
                if (a != null && !a.IsDead)
                    return true;
            }

            return false;
        }

        static bool AnyLivingEnemyIn(IReadOnlyList<CombatHealth> list)
        {
            foreach (var e in list)
            {
                if (e != null && !e.IsDead)
                    return true;
            }

            return false;
        }

        bool HasLivingAlly() => AnyLivingAllyIn(_allies);

        bool HasLivingEnemy() => AnyLivingEnemyIn(_enemies);

        void EndBattleLoss()
        {
            CancelPendingWaveComplete();
            CombatStarted = false;
            BattleEnded = true;
            SetBanner("Thua! (Ally đã gục)");
            if (_startCombatButton != null)
                _startCombatButton.gameObject.SetActive(false);
        }

        void EndBattleWaveWin()
        {
            CancelPendingWaveComplete();
            CombatStarted = false;
            BattleEnded = false;
            _prepSnapReadyForAddAlly = false;

            var completed = currentWave;

            if (completed >= MaxWaves)
            {
                var goldReward = LevelClearGoldReward();
                var keyReward = LevelClearTreasureKeyReward();
                MetaProgressionStore.AddGold(goldReward);
                MetaProgressionStore.AddTreasureKeys(keyReward);
                if (currentLevel < MaxLevels)
                    MetaProgressionStore.UnlockLevel(currentLevel + 1);

                CombatStarted = false;
                ClearEnemiesOnly();
                BattleEnded = true;
                _prepSnapReadyForAddAlly = true;

                if (Application.CanStreamedLevelBeLoaded(homepageSceneName))
                {
                    SceneManager.LoadScene(homepageSceneName);
                    return;
                }

                ReviveAlliesFromLastWaveLayout();
                SetBanner($"Hoàn thành level {currentLevel}! +{goldReward} Gold, +{keyReward} Key.");
                if (_startCombatButton != null)
                    _startCombatButton.gameObject.SetActive(false);
                return;
            }

            currentWave = Mathf.Min(completed + 1, MaxWaves);

            var waveCoinBonus = MetaProgressionStore.GetTreasurePassiveTotals().WaveRollCoinBonus;
            RollWallet.Add(wavePrepRollCoinGrant + waveCoinBonus);

            ReviveAlliesFromLastWaveLayout();

            var nextEnemyDef = ResolveEnemyDefinitionForWave(currentWave);
            if (nextEnemyDef == null && unitCatalog != null && unitCatalog.TryGet(enemyUnitId, out var fbEnemy))
                nextEnemyDef = fbEnemy;
            if (nextEnemyDef != null)
                SpawnEnemiesForWaveWithTint(nextEnemyDef);
            else
                SpawnEnemiesForWaveWithTint(null);

            BuildWaveIntroBannerText();
            SetBanner($"Thắng L{currentLevel}-W{completed}! Chuẩn bị wave {currentWave}. {_bannerIntroText}  Nhấn Bắt đầu.");

            _prepSnapReadyForAddAlly = true;

            if (_startCombatButton != null)
                _startCombatButton.gameObject.SetActive(true);
        }

        private void SetBanner(string text)
        {
            if (_banner != null)
                _banner.text = text;
            Debug.Log("[Duel] " + text);
        }

        private static void EnsureDirectionalLightIfNeeded()
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (l.type == LightType.Directional && l.isActiveAndEnabled)
                    return;
            }

            var go = new GameObject("Directional Light (auto)");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void EnsureEventSystemForUi()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void EnsureUi()
        {
            EnsureEventSystemForUi();

            var canvasGo = new GameObject("DuelUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _uiCanvasRt = canvasGo.GetComponent<RectTransform>();
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("ResultBanner");
            textGo.transform.SetParent(canvasGo.transform, false);
            _banner = textGo.AddComponent<Text>();
            _banner.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_banner.font == null)
                Debug.LogWarning("[DuelDirector] Không tìm thấy font built-in cho UI — chữ có thể không hiện; Console vẫn có log [Duel].");
            _banner.fontSize = 34;
            _banner.alignment = TextAnchor.MiddleCenter;
            _banner.color = Color.white;
            _banner.raycastTarget = false;

            _buffStatusText = CreateTopBuffStatusText(canvasGo.transform, _banner.font);

            // Neo UI đáy màn hình (reference 1080×1920): ally → restart → Bắt đầu/Roll → dòng coin → banner.
            const float padBottom = 28f;
            const float btnH = 88f;
            const float gap = 14f;
            const float bannerH = 108f;
            const float coinLineH = 40f;

            var y = padBottom;
            _addAllyButton = CreateBottomTextButton(canvasGo.transform, _banner.font, "Thêm ally", y,
                new Color(0.16f, 0.52f, 0.38f, 0.96f));
            _addAllyButton.onClick.AddListener(AddAllyFromButton);
            y += btnH + gap;

            _restartCombatButton = CreateBottomTextButton(canvasGo.transform, _banner.font, "Chơi lại", y,
                new Color(0.22f, 0.22f, 0.26f, 0.96f));
            _restartCombatButton.onClick.AddListener(RestartDuel);
            y += btnH + gap;

            _startCombatButton = CreateBottomTextButton(canvasGo.transform, _banner.font, "Bắt đầu", y, PrepStartButtonColor);
            _startCombatButton.onClick.AddListener(OnPrepPrimaryClicked);
            _startCombatButtonLabel = _startCombatButton.GetComponentInChildren<Text>();
            y += btnH + gap;

            _prepCoinText = CreateBottomInfoText(canvasGo.transform, _banner.font, y, coinLineH);
            y += coinLineH + gap;

            var rt = _banner.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(900f, bannerH);

            BuildRollGachaUi(canvasGo.transform, _banner.font);
            BuildAllyCellContextMenu(canvasGo.transform, _banner.font);
        }

        void BuildAllyCellContextMenu(Transform canvasParent, Font font)
        {
            _allyCellMenuRoot = new GameObject("AllyCellContextMenu");
            _allyCellMenuRoot.transform.SetParent(canvasParent, false);
            _allyCellMenuRt = _allyCellMenuRoot.AddComponent<RectTransform>();
            _allyCellMenuRt.sizeDelta = new Vector2(320f, 220f);

            var bg = _allyCellMenuRoot.AddComponent<Image>();
            bg.sprite = CreateWhiteSprite();
            bg.color = new Color(0.12f, 0.14f, 0.2f, 0.96f);
            bg.raycastTarget = true;

            float y = 68f;
            _allyCtxTitleText = CreateCtxLabel(_allyCellMenuRoot.transform, font, "Ô ally", 22, new Vector2(0f, y), new Vector2(300f, 36f));
            y -= 48f;
            _allyCtxSellButton = CreateCtxButton(_allyCellMenuRoot.transform, font, "Bán 1 ally", y, new Color(0.38f, 0.28f, 0.18f, 0.98f));
            _allyCtxSellButton.onClick.AddListener(OnAllyCtxSellClicked);
            y -= 52f;
            _allyCtxMergeButton = CreateCtxButton(_allyCellMenuRoot.transform, font, "Merge allies", y, new Color(0.22f, 0.42f, 0.28f, 0.98f));
            _allyCtxMergeButton.onClick.AddListener(OnAllyCtxMergeClicked);
            y -= 52f;
            _allyCtxCloseButton = CreateCtxButton(_allyCellMenuRoot.transform, font, "Đóng", y, new Color(0.25f, 0.25f, 0.3f, 0.98f));
            _allyCtxCloseButton.onClick.AddListener(OnAllyCtxCloseClicked);

            _allyCellMenuRoot.SetActive(false);
        }

        static Text CreateCtxLabel(Transform parent, Font font, string text, int size, Vector2 anchored, Vector2 sizeDelta)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.92f, 0.95f, 1f, 1f);
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = sizeDelta;
            return t;
        }

        static Button CreateCtxButton(Transform parent, Font font, string label, float yFromCenter, Color bg)
        {
            var btnGo = new GameObject(label + "Btn");
            btnGo.transform.SetParent(parent, false);
            var img = btnGo.AddComponent<Image>();
            img.sprite = CreateWhiteSprite();
            img.color = bg;
            img.raycastTarget = true;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = bg * 1.12f;
            colors.pressedColor = bg * 0.82f;
            btn.colors = colors;

            var labelGo = new GameObject("Txt");
            labelGo.transform.SetParent(btnGo.transform, false);
            var tx = labelGo.AddComponent<Text>();
            tx.font = font;
            tx.text = label;
            tx.fontSize = 26;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = Color.white;
            tx.raycastTarget = false;
            var lrt = tx.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, yFromCenter);
            rt.sizeDelta = new Vector2(280f, 46f);
            return btn;
        }

        void BuildAllyCellPickers()
        {
            if (_allyCellPickersRoot != null)
            {
                Destroy(_allyCellPickersRoot.gameObject);
                _allyCellPickersRoot = null;
            }

            var root = new GameObject("AllyCellPickers");
            root.transform.SetParent(transform, false);
            _allyCellPickersRoot = root.transform;

            for (var r = 0; r < BattleGrid.GridSize; r++)
            {
                for (var c = 0; c < BattleGrid.GridSize; c++)
                {
                    var go = new GameObject($"AllyCell_{r}_{c}");
                    go.transform.SetParent(_allyCellPickersRoot, false);
                    go.transform.position = BattleGrid.GetAllyCellCenter(r, c);
                    var box = go.AddComponent<BoxCollider2D>();
                    box.isTrigger = true;
                    box.size = new Vector2(BattleGrid.Cell * 0.98f, BattleGrid.Cell * 0.98f);
                    var pick = go.AddComponent<AllyCellPicker>();
                    pick.Row = r;
                    pick.Col = c;
                }
            }
        }

        void HandlePrepAllyGridCellPointerPressForPendingMenu()
        {
            if (!SandboxCanMutateUnits)
                return;

            if (!PrepGridPointer.WasPressedThisFrame())
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (!PrepGridPointer.TryGetPrimaryScreenPosition(out var pressScreen))
                return;

            var w = ScreenToWorldOnPlane(pressScreen);
            var hits = Physics2D.OverlapPointAll(w);

            AllyCellPicker picked = null;
            foreach (var h in hits)
            {
                if (h == null)
                    continue;
                var p = h.GetComponent<AllyCellPicker>();
                if (p != null)
                {
                    picked = p;
                    break;
                }
            }

            if (picked == null)
                return;

            if (CountAnyAlliesInCell(picked.Row, picked.Col) == 0)
                return;

            _pendingAllyCellContextTap = true;
            _pendingAllyCellRow = picked.Row;
            _pendingAllyCellCol = picked.Col;
            _pendingAllyCellPressScreen = pressScreen;
            _pendingAllyCellPressUnscaledTime = Time.unscaledTime;
            _pendingAllyCellMaxScreenDelta = 0f;
        }

        static Vector2 ScreenToWorldOnPlane(Vector2 screen)
        {
            var cam = Camera.main;
            if (cam == null)
                return Vector2.zero;

            var depth = Mathf.Abs(cam.transform.position.z);
            var p = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            return new Vector2(p.x, p.y);
        }

        void RefreshAllyCellMenuLayout()
        {
            if (_allyCellMenuRoot == null || !_allyCellMenuRoot.activeSelf)
                return;
            if (_selectedAllyRow < 0 || _uiCanvasRt == null)
                return;

            var center = BattleGrid.GetAllyCellCenter(_selectedAllyRow, _selectedAllyCol);
            var cam = Camera.main;
            if (cam == null)
                return;

            var sp = (Vector2)cam.WorldToScreenPoint(center);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiCanvasRt, sp, null, out var local);
            _allyCellMenuRt.anchoredPosition = local + new Vector2(0f, 140f);
        }

        void OpenAllyCellContextMenu(int row, int col)
        {
            if (_allyCellMenuRoot == null)
                return;

            _selectedAllyRow = row;
            _selectedAllyCol = col;
            _allyCellMenuRoot.SetActive(true);
            if (_allyCtxTitleText != null)
                _allyCtxTitleText.text = $"Ô ({row + 1},{col + 1})";
            RefreshAllyCellContextMenuButtons();
        }

        void CloseAllyCellContextMenu()
        {
            if (_allyCellMenuRoot != null)
                _allyCellMenuRoot.SetActive(false);
        }

        void RefreshAllyCellContextMenuButtons()
        {
            if (_allyCtxMergeButton == null || _allyCtxSellButton == null)
                return;

            var canMerge = _selectedAllyRow >= 0 && CanMergeAt(_selectedAllyRow, _selectedAllyCol);
            var canCombine = _selectedAllyRow >= 0 && CanCombineAllyAt(_selectedAllyRow, _selectedAllyCol);
            _allyCtxMergeButton.interactable = canMerge || canCombine;
            var mergeLabel = _allyCtxMergeButton.GetComponentInChildren<Text>();
            if (mergeLabel != null)
                mergeLabel.text = canCombine ? "Combine (Mythic)" : "Merge allies";

            var n = _selectedAllyRow >= 0 ? CountAnyAlliesInCell(_selectedAllyRow, _selectedAllyCol) : 0;
            _allyCtxSellButton.interactable = n > 0;
        }

        void OnAllyCtxSellClicked()
        {
            if (!SandboxCanMutateUnits || _selectedAllyRow < 0)
                return;
            TrySellOneAllyFromCell(_selectedAllyRow, _selectedAllyCol);
            RefreshPrepPrimaryUi();
            if (CountAnyAlliesInCell(_selectedAllyRow, _selectedAllyCol) == 0)
                ClearAllySelection();
            else
                RefreshAllyCellContextMenuButtons();
        }

        void OnAllyCtxMergeClicked()
        {
            if (!SandboxCanMutateUnits || _selectedAllyRow < 0)
                return;
            if (CanCombineAllyAt(_selectedAllyRow, _selectedAllyCol))
                TryCombineAllyAt(_selectedAllyRow, _selectedAllyCol);
            else if (CanMergeAt(_selectedAllyRow, _selectedAllyCol))
                TryMergeAt(_selectedAllyRow, _selectedAllyCol);
            RefreshAllyCellContextMenuButtons();
            if (_selectedAllyRow >= 0 && CountAnyAlliesInCell(_selectedAllyRow, _selectedAllyCol) == 0)
                ClearAllySelection();
        }

        void OnAllyCtxCloseClicked() => ClearAllySelection();

        void TrySellOneAllyFromCell(int row, int col)
        {
            if (!SandboxCanMutateUnits)
                return;

            var members = GetAllyMembersInCell(row, col);
            if (members.Count == 0)
                return;

            var victim = members[members.Count - 1];

            victim.Died -= OnUnitDied;
            _allies.Remove(victim);
            Destroy(victim.gameObject);

            RollWallet.Add(Mathf.Max(0, sellAllyCoinRefund));

            RepackAllyCell(row, col);
        }

        static int CompareByWorldPosition(CombatHealth a, CombatHealth b)
        {
            var ax = a.transform.position.x.CompareTo(b.transform.position.x);
            return ax != 0 ? ax : a.transform.position.y.CompareTo(b.transform.position.y);
        }

        void RepackAllyCell(int row, int col)
        {
            var members = GetAllyMembersInCell(row, col);
            if (members.Count == 0)
                return;

            if (!TryGetAllyKey(members[0], out var key))
                return;

            var keyMembers = GetAllyMembersInCellByKey(row, col, key);
            if (keyMembers.Count == 0)
                return;

            keyMembers.Sort(CompareByWorldPosition);
            SnapStackPositions(keyMembers, row, col, null);
        }

        static Text CreateTopBuffStatusText(Transform canvasParent, Font font)
        {
            var go = new GameObject("BuffStatusBar");
            go.transform.SetParent(canvasParent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = 22;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.82f, 0.9f, 1f, 1f);
            t.raycastTarget = false;
            t.text = "Buff: —";
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -188f);
            rt.sizeDelta = new Vector2(980f, 64f);
            return t;
        }

        void RefreshBuffStatusBar()
        {
            if (_buffStatusText == null)
                return;

            var hp = BattleRunBuffs.HpPct;
            var dmg = BattleRunBuffs.DmgPct;
            var asp = BattleRunBuffs.AtkSpeedPct;
            var cr = BattleRunBuffs.CritChance;
            var waveFrom = BattleRunBuffs.ActiveFromWave;
            var has =
                hp > 0.0001f || dmg > 0.0001f || asp > 0.0001f || cr > 0.0001f;
            if (!has)
            {
                _buffStatusText.text = "Buff: (chưa có — roll để nhận)";
                return;
            }

            _buffStatusText.text =
                $"Buff: HP +{hp * 100f:0.#}%  DMG +{dmg * 100f:0.#}%  ATKSPD +{asp * 100f:0.#}%  CRIT +{cr * 100f:0.#}%  | từ wave {waveFrom}";
        }

        static Text CreateBottomInfoText(Transform canvasParent, Font font, float yFromBottom, float height)
        {
            var go = new GameObject("PrepCoinLine");
            go.transform.SetParent(canvasParent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = 26;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(1f, 0.92f, 0.55f, 1f);
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, yFromBottom);
            rt.sizeDelta = new Vector2(960f, height);
            return t;
        }

        void OnPrepPrimaryClicked()
        {
            if (_rollBusy || CombatStarted || BattleEnded)
                return;

            if (RollWallet.Balance >= SixSlotRollResolver.RollCostCoins)
                StartCoroutine(RollGachaRoutine());
            else
                BeginCombat();
        }

        void BuildRollGachaUi(Transform canvasParent, Font font)
        {
            _rollUiRoot = new GameObject("RollGachaOverlay");
            _rollUiRoot.transform.SetParent(canvasParent, false);
            var rootRt = _rollUiRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(0f, 140f);
            rootRt.sizeDelta = new Vector2(940f, 360f);

            var bgGo = new GameObject("Backdrop");
            bgGo.transform.SetParent(_rollUiRoot.transform, false);
            var bg = bgGo.AddComponent<Image>();
            bg.sprite = CreateWhiteSprite();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
            bg.raycastTarget = true;
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            const float slotW = 122f;
            const float gap = 12f;
            var startX = -2.5f * (slotW + gap);
            const float yBase = 40f;

            for (var i = 0; i < 6; i++)
            {
                var go = new GameObject($"Slot{i}");
                go.transform.SetParent(_rollUiRoot.transform, false);
                var img = go.AddComponent<Image>();
                img.sprite = CreateWhiteSprite();
                img.color = RollSlotAllyBg;
                img.raycastTarget = false;
                _rollSlotBacks[i] = img;

                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(go.transform, false);
                var t = labelGo.AddComponent<Text>();
                t.font = font;
                t.fontSize = 24;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;
                t.text = "—";
                t.raycastTarget = false;
                t.rectTransform.anchorMin = Vector2.zero;
                t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.offsetMin = new Vector2(4f, 4f);
                t.rectTransform.offsetMax = new Vector2(-4f, -4f);
                _rollSlotTexts[i] = t;

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(startX + i * (slotW + gap), -yBase);
                rt.sizeDelta = new Vector2(slotW, 92f);
            }

            var sumGo = new GameObject("RollSummary");
            sumGo.transform.SetParent(_rollUiRoot.transform, false);
            _rollSummaryText = sumGo.AddComponent<Text>();
            _rollSummaryText.font = font;
            _rollSummaryText.fontSize = 22;
            _rollSummaryText.alignment = TextAnchor.MiddleCenter;
            _rollSummaryText.color = new Color(0.85f, 0.92f, 1f, 1f);
            _rollSummaryText.text = string.Empty;
            _rollSummaryText.raycastTarget = false;
            var sumRt = _rollSummaryText.rectTransform;
            sumRt.anchorMin = sumRt.anchorMax = new Vector2(0.5f, 1f);
            sumRt.pivot = new Vector2(0.5f, 1f);
            sumRt.anchoredPosition = new Vector2(0f, -140f);
            sumRt.sizeDelta = new Vector2(900f, 88f);

            var jpGo = new GameObject("Jackpot");
            jpGo.transform.SetParent(_rollUiRoot.transform, false);
            _rollJackpotText = jpGo.AddComponent<Text>();
            _rollJackpotText.font = font;
            _rollJackpotText.fontSize = 26;
            _rollJackpotText.alignment = TextAnchor.MiddleCenter;
            _rollJackpotText.color = new Color(1f, 0.85f, 0.2f, 1f);
            _rollJackpotText.text = string.Empty;
            _rollJackpotText.raycastTarget = false;
            var jpRt = _rollJackpotText.rectTransform;
            jpRt.anchorMin = jpRt.anchorMax = new Vector2(0.5f, 1f);
            jpRt.pivot = new Vector2(0.5f, 1f);
            jpRt.anchoredPosition = new Vector2(0f, -240f);
            jpRt.sizeDelta = new Vector2(900f, 44f);

            _rollUiRoot.SetActive(false);
        }

        void RefreshPrepPrimaryUi()
        {
            var prep = !CombatStarted && !BattleEnded;
            if (_prepCoinText != null)
            {
                _prepCoinText.gameObject.SetActive(prep);
                if (prep)
                {
                    _prepCoinText.text =
                        $"Coin: {RollWallet.Balance}  (−{SixSlotRollResolver.RollCostCoins}/roll — hết coin mới Bắt đầu)";
                }
            }

            if (_startCombatButton == null || _startCombatButtonLabel == null)
                return;

            _startCombatButton.gameObject.SetActive(prep);
            if (!prep)
                return;

            var cost = SixSlotRollResolver.RollCostCoins;
            var canRoll = RollWallet.Balance >= cost;
            if (canRoll)
            {
                _startCombatButtonLabel.text = $"Roll (-{cost})";
                ApplyPrimaryButtonVisual(PrepRollButtonColor);
                _startCombatButton.interactable = !_rollBusy;
            }
            else
            {
                _startCombatButtonLabel.text = "Bắt đầu";
                ApplyPrimaryButtonVisual(PrepStartButtonColor);
                var canStart = !_rollBusy && _allies.Count > 0 && _enemies.Count > 0;
                _startCombatButton.interactable = canStart;
            }
        }

        void ApplyPrimaryButtonVisual(Color bg)
        {
            if (_startCombatButton == null)
                return;
            var img = _startCombatButton.GetComponent<Image>();
            if (img != null)
                img.color = bg;
            var cb = _startCombatButton.colors;
            cb.normalColor = bg;
            cb.highlightedColor = bg * 1.12f;
            cb.pressedColor = bg * 0.78f;
            cb.selectedColor = bg;
            cb.disabledColor = new Color(bg.r * 0.55f, bg.g * 0.55f, bg.b * 0.55f, 0.55f);
            _startCombatButton.colors = cb;
        }

        IEnumerator RollGachaRoutine()
        {
            _rollBusy = true;
            RefreshPrepPrimaryUi();
            if (_rollUiRoot != null)
                _rollUiRoot.SetActive(true);
            ClearRollSlotDisplays();

            if (!RollGachaSession.TryExecuteRoll(out var payout, out var err))
            {
                ClearRollSlotDisplays();
                if (_rollSummaryText != null)
                    _rollSummaryText.text = err ?? "Roll thất bại.";
                if (_rollJackpotText != null)
                    _rollJackpotText.text = string.Empty;
                RefreshPrepPrimaryUi();
                yield return new WaitForSeconds(2f);
                if (_rollUiRoot != null)
                    _rollUiRoot.SetActive(false);
                _rollBusy = false;
                RefreshPrepPrimaryUi();
                yield break;
            }

            for (var t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                for (var i = 0; i < 6; i++)
                    ApplyRollSlotVisual(i, RandomRollSlotKind());
                yield return null;
            }

            for (var i = 0; i < 6; i++)
                ApplyRollSlotVisual(i, payout.Cells[i]);

            if (_rollJackpotText != null)
                _rollJackpotText.text = payout.Jackpot ? "Jackpot! 6 ô cùng loại!" : string.Empty;
            if (_rollSummaryText != null)
                _rollSummaryText.text = payout.SummaryLine;

            DrainPendingRollAllyGrants();
            ReapplyAllLivingAlliesFromBuffs();

            RefreshPrepPrimaryUi();
            RefreshBuffStatusBar();
            yield return new WaitForSeconds(2f);

            RefreshPrepPrimaryUi();
            RefreshBuffStatusBar();

            if (_rollUiRoot != null)
                _rollUiRoot.SetActive(false);
            _rollBusy = false;
            RefreshPrepPrimaryUi();
        }

        void ClearRollSlotDisplays()
        {
            for (var i = 0; i < 6; i++)
            {
                if (_rollSlotTexts[i] != null)
                {
                    _rollSlotTexts[i].text = "—";
                    _rollSlotTexts[i].color = new Color(0.75f, 0.78f, 0.85f, 1f);
                }

                if (_rollSlotBacks[i] != null)
                    _rollSlotBacks[i].color = new Color(0.18f, 0.22f, 0.32f, 0.95f);
            }
        }

        void ApplyRollSlotVisual(int index, RollCellKind kind)
        {
            if (index < 0 || index >= 6 || _rollSlotTexts[index] == null)
                return;
            _rollSlotTexts[index].text = RollCellLabel(kind);
            _rollSlotTexts[index].color = RollCellKindToTextColor(kind);
            if (_rollSlotBacks[index] != null)
                _rollSlotBacks[index].color = RollCellKindToSlotBg(kind);
        }

        static Color RollCellKindToTextColor(RollCellKind k) => k switch
        {
            RollCellKind.Coin => RollSlotCoinText,
            RollCellKind.Ally => RollSlotAllyText,
            _ => RollSlotBuffText
        };

        static Color RollCellKindToSlotBg(RollCellKind k) => k switch
        {
            RollCellKind.Coin => RollSlotCoinBg,
            RollCellKind.Ally => RollSlotAllyBg,
            _ => RollSlotBuffBg
        };

        static RollCellKind RandomRollSlotKind() => (RollCellKind)Random.Range(0, 3);

        static string RollCellLabel(RollCellKind k) => k switch
        {
            RollCellKind.Coin => "Coin",
            RollCellKind.Ally => "Ally",
            _ => "Buff"
        };

        void ReapplyAllLivingAlliesFromBuffs()
        {
            foreach (var h in _allies)
            {
                if (h == null || h.IsDead)
                    continue;
                var spec = h.GetComponent<AllyInstanceSpec>();
                if (spec == null)
                    continue;
                UnitDefinition def = null;
                if (!string.IsNullOrEmpty(spec.CatalogUnitId) && unitCatalog != null)
                    unitCatalog.TryGet(spec.CatalogUnitId, out def);
                ConfigureAlly(h.gameObject, spec.CombatMaxHitPoints, spec.CombatAttack, def);
                h.ResetToFull();
            }
        }

        static Button CreateBottomTextButton(Transform canvasParent, Font font, string label, float yFromBottom, Color bg)
        {
            var btnGo = new GameObject(label + "Button");
            btnGo.transform.SetParent(canvasParent, false);

            var image = btnGo.AddComponent<Image>();
            image.sprite = CreateWhiteSprite();
            image.color = bg;
            image.raycastTarget = true;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = image;
            var colors = btn.colors;
            colors.highlightedColor = bg * 1.15f;
            colors.pressedColor = bg * 0.75f;
            btn.colors = colors;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var text = labelGo.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            var labelRt = text.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, yFromBottom);
            btnRt.sizeDelta = new Vector2(420f, 88f);

            return btn;
        }

        internal static Vector3 AllyGridSampleWorld(CombatHealth h)
        {
            if (h == null)
                return Vector3.zero;
            return h.transform.position;
        }

        internal static Vector3 EnemyGridSampleWorld(CombatHealth h)
        {
            if (h == null)
                return Vector3.zero;
            var spec = h.GetComponent<EnemyInstanceSpec>();
            if (spec != null && spec.StackLeaderSpec != null)
                return spec.StackLeaderSpec.transform.position;
            return h.transform.position;
        }

        int CountAnyAlliesInCell(int row, int col)
        {
            var n = 0;
            foreach (var a in _allies)
            {
                if (a == null || a.IsDead)
                    continue;
                BattleGrid.WorldToAllyCell(AllyGridSampleWorld(a), out var r, out var c);
                if (r == row && c == col)
                    n++;
            }

            return n;
        }

        static AllyKey MakeAllyKey(int allyLineIndex, Rarity rarity) =>
            new(allyLineIndex, rarity);

        bool TryGetAllyKey(CombatHealth unit, out AllyKey key)
        {
            key = default;
            if (unit == null || unit.IsDead)
                return false;
            var spec = unit.GetComponent<AllyInstanceSpec>();
            if (spec == null)
                return false;
            key = MakeAllyKey(spec.AllyLineIndex, spec.RarityTier);
            return true;
        }

        int CountStackInCell(int row, int col, AllyKey key)
        {
            var n = 0;
            foreach (var a in _allies)
            {
                if (a == null || a.IsDead)
                    continue;
                var spec = a.GetComponent<AllyInstanceSpec>();
                if (!key.Matches(spec))
                    continue;
                BattleGrid.WorldToAllyCell(AllyGridSampleWorld(a), out var r, out var c);
                if (r == row && c == col)
                    n++;
            }

            return n;
        }

        int CountStackInCell(int row, int col, int allyLineIndex, Rarity rarity) =>
            CountStackInCell(row, col, MakeAllyKey(allyLineIndex, rarity));

        bool CanStackInCell(int row, int col, AllyKey key)
        {
            var members = GetAllyMembersInCell(row, col);
            if (members.Count == 0)
                return true;
            if (members.Count >= AllyMergeRules.MaxStackPerCell)
                return false;
            foreach (var m in members)
            {
                var spec = m.GetComponent<AllyInstanceSpec>();
                if (!key.Matches(spec))
                    return false;
            }

            return true;
        }

        bool CellAllowsAddAlly(int row, int col, int allyLineIndex, Rarity rarity) =>
            CanStackInCell(row, col, MakeAllyKey(allyLineIndex, rarity));

        List<CombatHealth> GetAllyMembersInCell(int row, int col)
        {
            var list = new List<CombatHealth>();
            foreach (var a in _allies)
            {
                if (a == null || a.IsDead)
                    continue;
                BattleGrid.WorldToAllyCell(AllyGridSampleWorld(a), out var r, out var c);
                if (r == row && c == col)
                    list.Add(a);
            }

            return list;
        }

        /// <summary>3 vị trí trong ô — tam giác (1 trên, 2 dưới), dùng chung ally + enemy.</summary>
        static Vector3 StackVisualOffset(int indexInStack)
        {
            const float c = BattleGrid.Cell;
            var halfBase = c * 0.19f;
            var rise = c * 0.11f;
            var drop = c * 0.1f;
            return indexInStack switch
            {
                0 => new Vector3(0f, rise, 0f),
                1 => new Vector3(-halfBase, -drop, 0f),
                2 => new Vector3(halfBase, -drop, 0f),
                _ => Vector3.zero
            };
        }

        List<CombatHealth> GetAllyMembersInCellByKey(int row, int col, AllyKey key)
        {
            var list = new List<CombatHealth>();
            foreach (var a in _allies)
            {
                if (a == null || a.IsDead)
                    continue;
                var spec = a.GetComponent<AllyInstanceSpec>();
                if (!key.Matches(spec))
                    continue;
                BattleGrid.WorldToAllyCell(AllyGridSampleWorld(a), out var r, out var c);
                if (r == row && c == col)
                    list.Add(a);
            }

            return list;
        }

        bool TryGetUniformAllyKeyInCell(int row, int col, out AllyKey key)
        {
            key = default;
            var members = GetAllyMembersInCell(row, col);
            if (members.Count == 0)
                return false;
            if (!TryGetAllyKey(members[0], out var baseKey))
                return false;

            foreach (var m in members)
            {
                if (!TryGetAllyKey(m, out var mk))
                    return false;
                if (mk.LineIndex != baseKey.LineIndex || mk.RarityTier != baseKey.RarityTier)
                    return false;
            }

            key = baseKey;
            return true;
        }

        void SpawnAllyStackMember(int row, int col, UnitDefinition def, Rarity tier, float hp, float atk, int allyLineIndex)
        {
            var key = MakeAllyKey(allyLineIndex, tier);
            var before = CountStackInCell(row, col, key);
            if (before >= AllyMergeRules.MaxStackPerCell)
                return;

            var center = BattleGrid.GetAllyCellCenter(row, col);
            var pos = center + StackVisualOffset(before);
            var tint = RarityPalette.UnitTint(tier, def.TeamKind);
            var go = CreateFighter(def.DisplayName, pos, tint, def.BodyShape, true);
            ApplyAllyDefinitionVisualScale(go.transform, def);
            var spec = go.GetComponent<AllyInstanceSpec>();
            spec.InitFromDefinition(def, tier, hp, atk, key.LineIndex);
            ConfigureAlly(go, hp, atk, def);
            go.GetComponent<SpriteRenderer>().color = tint;
            ApplyAllyDebugOverlay(go, spec);

            SetupAsStackLeader(go);

            RegisterAlly(go);
        }

        void SetupAsStackLeader(GameObject go)
        {
            var drag = go.GetComponent<DuelUnitGridDrag>();
            if (drag != null)
                drag.enabled = true;
            var col = go.GetComponent<Collider2D>();
            if (col != null)
                col.enabled = true;
            if (go.GetComponent<AllyInstanceSpec>() != null && col is BoxCollider2D box)
                ApplySafeAllyPrepColliderSize(box);
            var allySpec = go.GetComponent<AllyInstanceSpec>();
            if (allySpec != null)
                allySpec.SetStackLeader();
            foreach (var old in go.GetComponents<AllyStackFollower>())
                Destroy(old);
        }

        void ApplySafeAllyPrepColliderSize(BoxCollider2D box)
        {
            if (box == null)
                return;

            var desiredLocal = allyPrepColliderLocalSize;
            var sx = Mathf.Max(0.0001f, Mathf.Abs(box.transform.lossyScale.x));
            var sy = Mathf.Max(0.0001f, Mathf.Abs(box.transform.lossyScale.y));

            const float worldCellRatio = 0.86f;
            var maxWorld = BattleGrid.Cell * worldCellRatio;
            var worldW = desiredLocal.x * sx;
            var worldH = desiredLocal.y * sy;

            if (worldW > maxWorld)
                desiredLocal.x = maxWorld / sx;
            if (worldH > maxWorld)
                desiredLocal.y = maxWorld / sy;

            box.size = desiredLocal;
        }

        void ApplyAllyDebugOverlay(GameObject allyGo, AllyInstanceSpec spec)
        {
            if (allyGo == null || spec == null)
                return;

            var existing = allyGo.transform.Find("AllyKeyDebugText");
            if (existing != null)
                Destroy(existing.gameObject);

            if (!showAllyDebugKeyOverlay)
                return;

            var tag = new GameObject("AllyKeyDebugText");
            tag.transform.SetParent(allyGo.transform, false);
            tag.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            var tm = tag.AddComponent<TextMesh>();
            tm.text = $"L{spec.AllyLineIndex}/{spec.RarityTier}";
            tm.fontSize = 32;
            tm.characterSize = 0.04f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(0.95f, 0.98f, 1f, 1f);
            var mr = tag.GetComponent<MeshRenderer>();
            mr.sortingOrder = 8;
        }

        void ClearAllySelection()
        {
            CancelPendingAllyCellContextTap();
            CloseAllyCellContextMenu();
            _selectedAllyRow = -1;
            _selectedAllyCol = -1;
            _selectedEnemyRow = -1;
            _selectedEnemyCol = -1;
            SetMergeHintsVisible(false, false);
        }

        void EnsureMergeHints()
        {
            if (_allyMergeHint != null)
                return;

            _allyMergeHint = CreateMergeHintObject(true);
            _enemyMergeHint = CreateMergeHintObject(false);
        }

        GameObject CreateMergeHintObject(bool allyGrid)
        {
            var go = new GameObject(allyGrid ? "MergeHintAlly" : "MergeHintEnemy");
            go.transform.SetParent(transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UnitSpriteFactory.GetSprite(UnitBodyShape.Triangle);
            sr.sortingOrder = 50;

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = false;
            box.size = new Vector2(0.58f, 0.68f);

            var afford = go.AddComponent<WorldMergeAffordance>();
            afford.Bind(this, allyGrid);

            go.SetActive(false);
            return go;
        }

        void SetMergeHintsVisible(bool allyOn, bool enemyOn)
        {
            if (_allyMergeHint != null)
                _allyMergeHint.SetActive(allyOn);
            if (_enemyMergeHint != null)
                _enemyMergeHint.SetActive(enemyOn);
        }

        void RefreshMergeUi()
        {
            EnsureMergeHints();

            var enemyCombine = SandboxCanMutateUnits && _selectedEnemyRow >= 0 &&
                               CanCombineEnemyAt(_selectedEnemyRow, _selectedEnemyCol);
            var enemyMerge = SandboxCanMutateUnits && _selectedEnemyRow >= 0 &&
                             CanMergeEnemyAt(_selectedEnemyRow, _selectedEnemyCol);

            // Ally: merge/combine qua menu ô — không dùng mũi tên world nữa.
            if (_allyMergeHint != null)
                _allyMergeHint.SetActive(false);

            if (enemyMerge || enemyCombine)
            {
                var center = BattleGrid.GetEnemyCellCenter(_selectedEnemyRow, _selectedEnemyCol);
                _enemyMergeHint.transform.position = center + new Vector3(0f, BattleGrid.Cell * 0.92f, 0f);
                _enemyMergeHint.transform.localScale = new Vector3(0.26f, 0.26f, 1f);
                _enemyMergeHint.transform.rotation = Quaternion.identity;
                var sr = _enemyMergeHint.GetComponent<SpriteRenderer>();
                sr.color = enemyCombine
                    ? new Color(1f, 0.82f, 0.25f, 0.95f)
                    : new Color(1f, 0.55f, 0.45f, 0.95f);
                _enemyMergeHint.SetActive(true);
            }
            else
                _enemyMergeHint.SetActive(false);
        }

        internal void OnWorldMergeAffordanceClicked(bool allyGrid)
        {
            if (!SandboxCanMutateUnits)
                return;

            if (allyGrid)
            {
                if (_selectedAllyRow < 0)
                    return;
                if (CanCombineAllyAt(_selectedAllyRow, _selectedAllyCol))
                    TryCombineAllyAt(_selectedAllyRow, _selectedAllyCol);
                else if (CanMergeAt(_selectedAllyRow, _selectedAllyCol))
                    TryMergeAt(_selectedAllyRow, _selectedAllyCol);
            }
            else
            {
                if (_selectedEnemyRow < 0)
                    return;
                if (CanCombineEnemyAt(_selectedEnemyRow, _selectedEnemyCol))
                    TryCombineEnemyAt(_selectedEnemyRow, _selectedEnemyCol);
                else if (CanMergeEnemyAt(_selectedEnemyRow, _selectedEnemyCol))
                    TryMergeEnemyAt(_selectedEnemyRow, _selectedEnemyCol);
            }
        }

        bool CanMergeAt(int row, int col)
        {
            var members = GetAllyMembersInCell(row, col);
            if (members.Count != AllyMergeRules.MaxStackPerCell)
                return false;

            if (!TryGetUniformAllyKeyInCell(row, col, out var key))
                return false;

            return AllyMergeRules.CanMerge(key.RarityTier);
        }

        bool CanCombineAllyAt(int row, int col)
        {
            var members = GetAllyMembersInCell(row, col);
            if (members.Count != AllyMergeRules.MaxStackPerCell)
                return false;

            if (!TryGetUniformAllyKeyInCell(row, col, out var key))
                return false;

            return AllyMergeRules.CanCombine(key.RarityTier);
        }

        bool CanMergeEnemyAt(int row, int col)
        {
            var members = GetEnemyMembersInCell(row, col);
            if (members.Count != AllyMergeRules.MaxStackPerCell)
                return false;

            var s0 = members[0].GetComponent<EnemyInstanceSpec>();
            if (s0 == null)
                return false;

            foreach (var m in members)
            {
                var s = m.GetComponent<EnemyInstanceSpec>();
                if (s == null || s.CatalogUnitId != s0.CatalogUnitId || s.RarityTier != s0.RarityTier)
                    return false;
            }

            return AllyMergeRules.CanMerge(s0.RarityTier);
        }

        bool CanCombineEnemyAt(int row, int col)
        {
            var members = GetEnemyMembersInCell(row, col);
            if (members.Count != AllyMergeRules.MaxStackPerCell)
                return false;

            var s0 = members[0].GetComponent<EnemyInstanceSpec>();
            if (s0 == null)
                return false;

            foreach (var m in members)
            {
                var s = m.GetComponent<EnemyInstanceSpec>();
                if (s == null || s.CatalogUnitId != s0.CatalogUnitId || s.RarityTier != s0.RarityTier)
                    return false;
            }

            return AllyMergeRules.CanCombine(s0.RarityTier);
        }

        /// <summary>Ô đầu tiên (row→col) có stack cùng dòng 0–2 + rarity chưa đầy; bỏ qua ô trống.</summary>
        bool TryFindFirstPartialStackAllyCell(int allyLineIndex, Rarity rarity, out int row, out int col)
        {
            allyLineIndex = AllyLineCatalog.ClampLineIndex(allyLineIndex);
            for (var r = 0; r < BattleGrid.GridSize; r++)
            {
                for (var c = 0; c < BattleGrid.GridSize; c++)
                {
                    if (!CellAllowsAddAlly(r, c, allyLineIndex, rarity))
                        continue;
                    var cnt = CountStackInCell(r, c, allyLineIndex, rarity);
                    if (cnt > 0 && cnt < AllyMergeRules.MaxStackPerCell)
                    {
                        row = r;
                        col = c;
                        return true;
                    }
                }
            }

            row = col = 0;
            return false;
        }

        void TryMergeAt(int row, int col)
        {
            if (!SandboxCanMutateUnits || !CanMergeAt(row, col))
                return;

            var members = GetAllyMembersInCell(row, col);
            if (!TryGetUniformAllyKeyInCell(row, col, out var preMergeKey))
                return;

            foreach (var d in members)
            {
                if (d == null || d.IsDead)
                    continue;
                d.Died -= OnUnitDied;
                _allies.Remove(d);
                Destroy(d.gameObject);
            }

            var newRarity = AllyMergeRules.NextRarity(preMergeKey.RarityTier);
            var mergeLine = AllyLineCatalog.PickRandomLineIndex(_allyLineRandom);
            var catalogId = AllyLineCatalog.UnitIdForLine(mergeLine);
            if (unitCatalog == null || !unitCatalog.TryGet(catalogId, out var pickedDef))
                return;

            if (!TryFindFirstPartialStackAllyCell(mergeLine, newRarity, out var spawnRow, out var spawnCol))
            {
                spawnRow = row;
                spawnCol = col;
            }

            var (hp, atk) = AllyStatScaling.ScaleStats(pickedDef, newRarity);
            SpawnAllyStackMember(spawnRow, spawnCol, pickedDef, newRarity, hp, atk, mergeLine);
            RepackAllyCell(spawnRow, spawnCol);
            ClearAllySelection();
        }

        void TryMergeEnemyAt(int row, int col)
        {
            if (!SandboxCanMutateUnits || !CanMergeEnemyAt(row, col))
                return;

            var members = GetEnemyMembersInCell(row, col);
            EnemyInstanceSpec leaderSpec = null;
            foreach (var m in members)
            {
                if (m == null || m.IsDead)
                    continue;
                var s = m.GetComponent<EnemyInstanceSpec>();
                if (s != null && s.StackLeader)
                {
                    leaderSpec = s;
                    break;
                }
            }

            if (leaderSpec == null)
                return;

            var leader = leaderSpec.GetComponent<CombatHealth>();

            foreach (var d in members)
            {
                if (d == null || d == leader || d.IsDead)
                    continue;
                d.Died -= OnUnitDied;
                _enemies.Remove(d);
                Destroy(d.gameObject);
            }

            var nr = AllyMergeRules.NextRarity(leaderSpec.RarityTier);
            var nhp = leaderSpec.CombatMaxHitPoints * AllyMergeRules.HpMultiplier;
            var natk = leaderSpec.CombatAttack * AllyMergeRules.AttackMultiplier;
            leaderSpec.ApplyMergeUpgrade(nr, nhp, natk);

            if (unitCatalog != null && unitCatalog.TryGet(leaderSpec.CatalogUnitId, out var def))
            {
                ConfigureEnemy(leader.gameObject, nhp, natk, def);
                leader.gameObject.GetComponent<SpriteRenderer>().color = RarityPalette.UnitTint(nr, def.TeamKind);
            }
            else
            {
                ConfigureEnemy(leader.gameObject, nhp, natk, null);
            }

            foreach (var old in leader.gameObject.GetComponents<AllyStackFollower>())
                Destroy(old);

            SetupAsStackLeader(leader.gameObject);
            var center = BattleGrid.GetEnemyCellCenter(row, col);
            leader.transform.position = center + StackVisualOffset(0);
            ClearAllySelection();
        }

        void TryCombineAllyAt(int row, int col)
        {
            if (!SandboxCanMutateUnits || !CanCombineAllyAt(row, col))
                return;

            SetBanner("Combine Mythic (ally): công thức — sắp có");
            Debug.Log("[DuelDirector] Combine ally — placeholder (3× Mythic cùng unit).");
        }

        void TryCombineEnemyAt(int row, int col)
        {
            if (!SandboxCanMutateUnits || !CanCombineEnemyAt(row, col))
                return;

            SetBanner("Combine Mythic (enemy): công thức — sắp có");
            Debug.Log("[DuelDirector] Combine enemy — placeholder (3× Mythic cùng unit).");
        }

        internal void NotifyAllyShortTap(CombatHealth h)
        {
            if (!SandboxCanMutateUnits || h == null)
                return;

            // Cell-picker input là nguồn sự thật cho prep; nếu đang có pending theo ô thì bỏ qua tap theo unit.
            if (_pendingAllyCellContextTap)
                return;

            CancelPendingAllyCellContextTap();

            var spec = h.GetComponent<AllyInstanceSpec>();
            if (spec == null)
                return;

            _selectedEnemyRow = -1;
            _selectedEnemyCol = -1;
            var w = AllyGridSampleWorld(h);
            var snapped = BattleGrid.SnapWorldToAllyGrid(w);
            BattleGrid.WorldToAllyCell(snapped, out _selectedAllyRow, out _selectedAllyCol);
            OpenAllyCellContextMenu(_selectedAllyRow, _selectedAllyCol);
        }

        internal void NotifyEnemyShortTap(CombatHealth h)
        {
            if (!SandboxCanMutateUnits || h == null)
                return;

            var spec = h.GetComponent<EnemyInstanceSpec>();
            if (spec == null)
                return;

            CloseAllyCellContextMenu();
            _selectedAllyRow = -1;
            _selectedAllyCol = -1;
            var w = EnemyGridSampleWorld(h);
            var snapped = BattleGrid.SnapWorldToEnemyGrid(w);
            BattleGrid.WorldToEnemyCell(snapped, out _selectedEnemyRow, out _selectedEnemyCol);
        }

        List<CombatHealth> GetStackMembersForEnemyLeader(CombatHealth anyInStack)
        {
            var spec = anyInStack.GetComponent<EnemyInstanceSpec>();
            if (spec == null)
                return new List<CombatHealth> { anyInStack };

            var leaderSp = spec.ResolveLeader();
            var leaderH = leaderSp.GetComponent<CombatHealth>();
            var list = new List<CombatHealth> { leaderH };
            foreach (var e in _enemies)
            {
                if (e == null || e.IsDead || e == leaderH)
                    continue;
                var s = e.GetComponent<EnemyInstanceSpec>();
                if (s != null && s.StackLeaderSpec == leaderSp)
                    list.Add(e);
            }

            return list;
        }

        int CountNonMatchingEnemiesInCellExcludeGroup(int row, int col, string unitId, Rarity rarity, List<CombatHealth> exclude)
        {
            var n = 0;
            foreach (var e in _enemies)
            {
                if (e == null || e.IsDead || exclude.Contains(e))
                    continue;
                BattleGrid.WorldToEnemyCell(EnemyGridSampleWorld(e), out var r, out var c);
                if (r != row || c != col)
                    continue;
                var s = e.GetComponent<EnemyInstanceSpec>();
                if (s == null || s.CatalogUnitId != unitId || s.RarityTier != rarity)
                    n++;
            }

            return n;
        }

        int CountNonMatchingAlliesInCellExcludeGroup(int row, int col, int allyLineIndex, Rarity rarity, List<CombatHealth> exclude)
        {
            var n = 0;
            foreach (var a in _allies)
            {
                if (a == null || a.IsDead || exclude.Contains(a))
                    continue;
                BattleGrid.WorldToAllyCell(AllyGridSampleWorld(a), out var r, out var c);
                if (r != row || c != col)
                    continue;
                var s = a.GetComponent<AllyInstanceSpec>();
                if (s == null || s.AllyLineIndex != allyLineIndex || s.RarityTier != rarity)
                    n++;
            }

            return n;
        }

        int CountStackInCellExcludeGroup(int row, int col, int allyLineIndex, Rarity rarity, List<CombatHealth> exclude)
        {
            var n = 0;
            foreach (var a in _allies)
            {
                if (a == null || a.IsDead || exclude.Contains(a))
                    continue;
                var spec = a.GetComponent<AllyInstanceSpec>();
                if (spec == null)
                    continue;
                BattleGrid.WorldToAllyCell(AllyGridSampleWorld(a), out var r, out var c);
                if (r == row && c == col && spec.AllyLineIndex == allyLineIndex && spec.RarityTier == rarity)
                    n++;
            }

            return n;
        }

        int CountEnemyStackInCellExcludeGroup(int row, int col, string unitId, Rarity rarity, List<CombatHealth> exclude)
        {
            var n = 0;
            foreach (var e in _enemies)
            {
                if (e == null || e.IsDead || exclude.Contains(e))
                    continue;
                var spec = e.GetComponent<EnemyInstanceSpec>();
                if (spec == null)
                    continue;
                BattleGrid.WorldToEnemyCell(EnemyGridSampleWorld(e), out var r, out var c);
                if (r == row && c == col && spec.CatalogUnitId == unitId && spec.RarityTier == rarity)
                    n++;
            }

            return n;
        }

        internal bool TryRelocateAllyStack(CombatHealth leader, int fromRow, int fromCol, int toRow, int toCol, Vector3 revertLeaderWorld)
        {
            if (!TryGetAllyKey(leader, out var key))
            {
                var p = BattleGrid.SnapWorldToAllyGrid((Vector2)leader.transform.position);
                leader.transform.position = p;
                return true;
            }

            var members = GetAllyMembersInCellByKey(fromRow, fromCol, key);
            if (!members.Contains(leader))
                members.Add(leader);
            if (members.Count == 0)
            {
                var p = BattleGrid.SnapWorldToAllyGrid((Vector2)leader.transform.position);
                leader.transform.position = p;
                return false;
            }
            members.Sort((a, b) => a == leader ? -1 : b == leader ? 1 : 0);

            var n = members.Count;

            if (toRow == fromRow && toCol == fromCol)
            {
                SnapStackPositions(members, fromRow, fromCol, null);
                return true;
            }

            var other = CountNonMatchingAlliesInCellExcludeGroup(toRow, toCol, key.LineIndex, key.RarityTier, members);
            if (other > 0)
            {
                var targetMembers = GetAllyMembersInCell(toRow, toCol);
                if (targetMembers.Count == 0 || !TryGetAllyKey(targetMembers[0], out var targetKey))
                {
                    SnapStackPositions(members, fromRow, fromCol, revertLeaderWorld);
                    return false;
                }

                foreach (var tm in targetMembers)
                {
                    if (!TryGetAllyKey(tm, out var tmKey) || tmKey.LineIndex != targetKey.LineIndex || tmKey.RarityTier != targetKey.RarityTier)
                    {
                        SnapStackPositions(members, fromRow, fromCol, revertLeaderWorld);
                        return false;
                    }
                }

                targetMembers.Sort(CompareByWorldPosition);
                SnapStackPositions(members, toRow, toCol, null);
                SnapStackPositions(targetMembers, fromRow, fromCol, null);
                RepackAllyCell(fromRow, fromCol);
                RepackAllyCell(toRow, toCol);
                return true;
            }

            var existing = CountStackInCellExcludeGroup(toRow, toCol, key.LineIndex, key.RarityTier, members);
            if (existing + n > AllyMergeRules.MaxStackPerCell)
            {
                SnapStackPositions(members, fromRow, fromCol, revertLeaderWorld);
                return false;
            }

            SnapStackPositions(members, toRow, toCol, null);
            RepackAllyCell(fromRow, fromCol);
            RepackAllyCell(toRow, toCol);
            return true;
        }

        internal bool TryRelocateEnemyStack(CombatHealth leader, int fromRow, int fromCol, int toRow, int toCol, Vector3 revertLeaderWorld)
        {
            var ls = leader.GetComponent<EnemyInstanceSpec>();
            if (ls == null)
            {
                var p = BattleGrid.SnapWorldToEnemyGrid((Vector2)leader.transform.position);
                leader.transform.position = p;
                return true;
            }

            var members = GetStackMembersForEnemyLeader(leader);
            members.Sort((a, b) => a == leader ? -1 : b == leader ? 1 : 0);

            if (toRow == fromRow && toCol == fromCol)
            {
                SnapEnemyStackPositions(members, fromRow, fromCol, null);
                return true;
            }

            var other = CountNonMatchingEnemiesInCellExcludeGroup(toRow, toCol, ls.CatalogUnitId, ls.RarityTier, members);
            if (other > 0)
            {
                SnapEnemyStackPositions(members, fromRow, fromCol, revertLeaderWorld);
                return false;
            }

            var existing = CountEnemyStackInCellExcludeGroup(toRow, toCol, ls.CatalogUnitId, ls.RarityTier, members);
            if (existing + members.Count > AllyMergeRules.MaxStackPerCell)
            {
                SnapEnemyStackPositions(members, fromRow, fromCol, revertLeaderWorld);
                return false;
            }

            var center = BattleGrid.GetEnemyCellCenter(toRow, toCol);
            leader.transform.position = center + StackVisualOffset(0);
            for (var i = 1; i < members.Count; i++)
            {
                var m = members[i];
                foreach (var old in m.GetComponents<AllyStackFollower>())
                    Destroy(old);

                m.GetComponent<EnemyInstanceSpec>().SetStackFollower(ls);
                var nf = m.gameObject.AddComponent<AllyStackFollower>();
                nf.Init(ls.transform, StackVisualOffset(i) - StackVisualOffset(0));
            }

            return true;
        }

        void SnapStackPositions(IReadOnlyList<CombatHealth> members, int row, int col, Vector3? forcedLeaderWorld)
        {
            if (members == null || members.Count == 0)
                return;

            var center = BattleGrid.GetAllyCellCenter(row, col);
            for (var i = 0; i < members.Count; i++)
            {
                var m = members[i];
                if (m == null || m.IsDead)
                    continue;
                foreach (var old in m.GetComponents<AllyStackFollower>())
                    Destroy(old);
                var spec = m.GetComponent<AllyInstanceSpec>();
                if (spec != null)
                    spec.SetStackLeader();
                m.transform.position = i == 0 && forcedLeaderWorld.HasValue
                    ? forcedLeaderWorld.Value
                    : center + StackVisualOffset(i);
            }
        }

        void SnapEnemyStackPositions(IReadOnlyList<CombatHealth> members, int row, int col, Vector3? forcedLeaderWorld)
        {
            if (members == null || members.Count == 0)
                return;

            var center = BattleGrid.GetEnemyCellCenter(row, col);
            var leader = members[0];
            var ls = leader.GetComponent<EnemyInstanceSpec>();
            leader.transform.position = forcedLeaderWorld ?? center + StackVisualOffset(0);
            if (ls == null)
                return;
            for (var i = 1; i < members.Count; i++)
            {
                var m = members[i];
                foreach (var old in m.GetComponents<AllyStackFollower>())
                    Destroy(old);

                m.GetComponent<EnemyInstanceSpec>().SetStackFollower(ls);
                var nf = m.gameObject.AddComponent<AllyStackFollower>();
                nf.Init(ls.transform, StackVisualOffset(i) - StackVisualOffset(0));
            }
        }

        static Sprite CreateWhiteSprite()
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        /// <summary>Melee vuông 0.55, tròn 0.135; ranged tam giác 0.135.</summary>
        static void ApplyAllyDefinitionVisualScale(Transform t, UnitDefinition def)
        {
            if (def == null)
                return;
            if (def.BodyShape == UnitBodyShape.Triangle)
            {
                const float rangedScale = 0.135f;
                t.localScale = new Vector3(rangedScale, rangedScale, 1f);
                return;
            }

            if (def.BodyShape == UnitBodyShape.Circle)
            {
                const float circleScale = 0.135f;
                t.localScale = new Vector3(circleScale, circleScale, 1f);
                return;
            }

            if (def.BodyShape == UnitBodyShape.Square)
            {
                const float meleeScale = 0.55f;
                t.localScale = new Vector3(meleeScale, meleeScale, 1f);
            }
        }

        /// <summary>Melee vuông 0.55, tròn 0.135; ranged tam giác 0.135.</summary>
        static void ApplyEnemyDefinitionVisualScale(Transform t, UnitDefinition def, UnitBodyShape shapeUsed)
        {
            var shape = def != null ? def.BodyShape : shapeUsed;
            if (shape == UnitBodyShape.Triangle)
            {
                const float rangedScale = 0.135f;
                t.localScale = new Vector3(rangedScale, rangedScale, 1f);
                return;
            }

            if (shape == UnitBodyShape.Circle)
            {
                const float circleScale = 0.135f;
                t.localScale = new Vector3(circleScale, circleScale, 1f);
                return;
            }

            if (shape == UnitBodyShape.Square)
            {
                const float meleeScale = 0.55f;
                t.localScale = new Vector3(meleeScale, meleeScale, 1f);
            }
        }

        GameObject CreateFighter(string name, Vector3 pos, Color tint, UnitBodyShape shape, bool attachAllySpec)
        {
            var go = new GameObject(name);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UnitSpriteFactory.GetSprite(shape);
            sr.color = tint;
            sr.sortingOrder = 1;

            go.AddComponent<CombatHealth>();

            var box = go.AddComponent<BoxCollider2D>();
            box.size = attachAllySpec ? allyPrepColliderLocalSize : new Vector2(0.48f, 0.48f);
            if (attachAllySpec)
                ApplySafeAllyPrepColliderSize(box);

            go.AddComponent<DuelActor>();
            go.AddComponent<DuelUnitGridDrag>();
            go.AddComponent<WorldUnitHealthBar>();
            // Ally nhỏ hơn enemy để 3 con/ô không chồng; enemy cũng vừa 3/ô.
            var s = attachAllySpec ? 0.095f : 0.27f;
            go.transform.localScale = new Vector3(s, s, 1f);
            if (attachAllySpec)
                go.AddComponent<AllyInstanceSpec>();
            return go;
        }
    }
}
