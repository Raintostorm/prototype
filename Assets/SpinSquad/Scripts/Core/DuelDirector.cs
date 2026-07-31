using System.Collections;
using System.Collections.Generic;
using SpinSquad.Data;
using SpinSquad.UI;
using SpinSquad.Gacha;
using SpinSquad.Meta;
using SpinSquad.Presentation;
using SpinSquad.Scenes;
using Spine;
using Spine.Unity;
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
            BattleGrid.CellCount * AllyMergeRules.MaxStackPerCell;

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
        [SerializeField] private string allyUnitId = "common_melee";

        [Tooltip("Level 1 wave 1: spawn đủ 5 ally starter (Mộc/Hỏa/Kim/Thủy + Knight) mỗi con một ô — không phụ thuộc allyUnitId. Tắt khi không cần test.")]
        [SerializeField] private bool spawnFiveStarterElementAlliesOnWave1 = true;

        [SerializeField] private string enemyUnitId = "unit_moss_oracle";

        [Tooltip("Wave lẻ (enemy melee): id trong UnitCatalog. Scene test Knight Spine có thể đặt unit_enemy_knight.")]
        [SerializeField] private string waveEnemyMeleeCatalogId = "unit_moss_oracle";

        [Tooltip("Wave chẵn (enemy ranged).")]
        [SerializeField] private string waveEnemyRangedCatalogId = "unit_enemy_ranged";

        [Tooltip("SampleScene: gán ally học (unit_legends_melee_robot) khi catalog có id — tắt để test allyUnitId tay.")]
        [SerializeField] private bool applyLearningCampaignAllyInSampleScene = true;
        [Tooltip("SampleScene: áp id enemy test bên dưới (nếu valid) để kiểm tra prefab/Spine theo scene.")]
        [SerializeField] private bool applySampleSceneEnemyIds;
        [SerializeField] private string sampleSceneEnemyMeleeCatalogId = string.Empty;
        [SerializeField] private string sampleSceneEnemyRangedCatalogId = string.Empty;
        [SerializeField] private GameObject line1BattleRigPrefab;
        [Tooltip("Offset local của VisualRig (Spine) so với root unit — mặc định 0; chỉ chỉnh khi art cần lệch tâm ô.")]
        [SerializeField] private Vector3 line1BattleRigVisualLocalOffset;

        [Tooltip("Auto-center VisualRig using renderer bounds (helps Spine pivots).")]
        [SerializeField] private bool autoCenterRigVisualByBounds = true;
        [Tooltip("Optional anchor child under VisualRig (eg. Anchor_Foot). If found, rig aligns by this anchor first.")]
        [SerializeField] private string rigAnchorChildName = "Anchor_Foot";
        [Tooltip("Debug metrics for grid/cell/anchor/facing. Logs once on unit spawn.")]
        [SerializeField] private bool debugBattleVisualMetrics;

        [Tooltip("Bật log Console cho SpineBattleAnimator (idle/walk/attack/died). Ally spawn runtime không có prefab — dùng cái này thay vì tick từng instance.")]
        [SerializeField] private bool debugSpineBattleAnimatorLogs;

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

        [Tooltip("Vòng nền dưới chân theo phẩm chất (trắng/lam/tím/vàng); sorting thấp hơn Spine.")]
        [SerializeField] bool showFootGroundDecor = true;

        static readonly Color PrepRollButtonColor = new Color(0.55f, 0.22f, 0.45f, 0.96f);
        static readonly Color PrepStartButtonColor = new Color(0.18f, 0.42f, 0.72f, 0.98f);

        static readonly Color RollSlotAllyText = Color.white;
        static readonly Color RollSlotCoinText = new Color(1f, 0.88f, 0.2f, 1f);
        static readonly Color RollSlotBuffText = new Color(0.35f, 0.95f, 0.5f, 1f);
        static readonly Color RollSlotAllyBg = new Color(0.22f, 0.24f, 0.3f, 0.95f);
        static readonly Color RollSlotCoinBg = new Color(0.28f, 0.22f, 0.08f, 0.95f);
        static readonly Color RollSlotBuffBg = new Color(0.1f, 0.26f, 0.16f, 0.95f);
        static readonly bool EnableAnimationTestUi = false;

        public bool BattleEnded { get; private set; }

        public bool CombatStarted { get; private set; }

        /// <summary>Sau ~2s kể từ BeginCombat — unit mới di chuyển/đánh.</summary>
        public bool CombatEngaged { get; private set; }

        public const float CombatEngageDelaySeconds = 2f;

        public bool IsDraggingGridUnit { get; private set; }

        public int CurrentWave => currentWave;
        public int CurrentLevel => currentLevel;

        public string CurrentAllyUnitId => allyUnitId;

        public string CurrentEnemyUnitId => enemyUnitId;

        enum RewardPrimaryAction
        {
            Close,
            Restart,
            Home
        }

        /// <summary>Wave chẵn → enemy ranged tam giác; wave lẻ → melee vuông.</summary>
        public static bool WaveUsesRangedEnemy(int wave) =>
            Mathf.Clamp(wave, 1, MaxWaves) % 2 == 0;

        UnitDefinition ResolveEnemyDefinitionForWave(int wave)
        {
            if (unitCatalog == null)
                return null;
            var id = WaveUsesRangedEnemy(wave) ? waveEnemyRangedCatalogId : waveEnemyMeleeCatalogId;
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
                if (CountLivingAllies() >= BattleGrid.CellCount || !TryPickRandomAllyCellForAdd(line, def.Rarity, out var row, out var col))
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
            if (members.Count >= AllyMergeRules.MaxEnemyStackPerCell)
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
            var stackPref = new List<(int r, int c)>(BattleGrid.CellCount);
            var emptyOrOther = new List<(int r, int c)>(BattleGrid.CellCount);
            for (var r = 0; r < BattleGrid.Rows; r++)
            {
                for (var c = 0; c < BattleGrid.Cols; c++)
                {
                    if (!CellAllowsAddEnemy(r, c, def) ||
                        CountEnemyStackInCell(r, c, unitId, tier) >= AllyMergeRules.MaxEnemyStackPerCell)
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
            if (before >= AllyMergeRules.MaxEnemyStackPerCell)
                return;

            var center = BattleGrid.GetEnemyCellCenter(row, col);
            var pos = center + StackVisualOffset(before);
            var tint = def != null ? RarityPalette.UnitTint(tier, def.TeamKind) : RarityPalette.UnitTint(tier, UnitTeamKind.Enemy);
            var shape = def != null ? def.BodyShape : UnitBodyShape.Square;
            var idx = CountLivingEnemies() + 1;
            var disp = def != null ? def.DisplayName : "Enemy";
            var enemyGo = CreateCombatUnitVisual(
                $"{disp} (+{idx})",
                pos,
                tint,
                def,
                shape,
                false,
                out var enemyUsedFallbackSprite,
                currentWave,
                idx - 1);
            if (enemyUsedFallbackSprite)
                ApplyEnemyDefinitionVisualScale(enemyGo.transform, def, shape);
            EnsureBattleVisualDriver(enemyGo);
            WarnIfMissingBattleVisualDriver(enemyGo, def, "enemy");
            var es = enemyGo.AddComponent<EnemyInstanceSpec>();
            es.InitFromDefinition(def, tier, hp, atk);
            ConfigureEnemy(enemyGo, def);
            var rootSr = enemyGo.GetComponent<SpriteRenderer>();
            if (rootSr != null && enemyGo.GetComponent<AnimalKitEnemyVisualMarker>() == null)
                rootSr.color = tint;
            ApplyFootGroundDecorIfEnabled(enemyGo.transform, tier);

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
        Text _objectiveText;
        private Text _startCombatButtonLabel;
        private Button _startCombatButton;
        Button _pauseRestartButton;
        Button _pauseHomeButton;
        private Button _addAllyButton;
        GameObject _duelInfoStripRoot;
        GameObject _duelBottomBarRoot;
        Button _alliesBagToggleButton;
        bool _alliesBagExpanded;
        private Button _animationTestButton;
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

        GameObject _prepStatPanelRoot;
        Text _prepStatTitleText;
        Text _prepStatBodyText;
        CombatHealth _prepStatTarget;

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
        GameObject _animationTestPanelRoot;
        RectTransform _animationTestPanelRt;
        Text _animationTestTargetText;
        Text _animationTestInfoText;
        Button _animationTestPrevButton;
        Button _animationTestNextButton;
        Button _animationTestIdleButton;
        Button _animationTestWalkButton;
        Button _animationTestAttackButton;
        Button _animationTestDieButton;
        Button _animationTestAutoButton;
        Button _animationTestCloseButton;
        Button _pauseTopButton;
        Button _settingsTopButton;
        Button _speed2xStubButton;
        bool _speed2xActive;
        GameObject _pauseOverlayRoot;
        Button _pauseResumeButton;
        GameObject _rewardOverlayRoot;
        Text _rewardTitleText;
        Text _rewardBodyText;
        Button _rewardPrimaryButton;
        Button _rewardSecondaryButton;
        RewardPrimaryAction _rewardPrimaryAction;
        RewardPrimaryAction _rewardSecondaryAction;
        GameObject _settingsStubRoot;
        Button _settingsStubCloseButton;
        readonly List<Line1BattleSpriteAnimator> _animationTestTargets = new();
        int _animationTestTargetIndex;
        bool _rollBusy;
        bool _suppressAllyAutoMerge;
        Coroutine _combatEngageRoutine;
        AllyBenchInventory _allyBench;
        Transform _inventoryPickersRoot;
        readonly System.Random _allyLineRandom = new((int)(System.DateTime.UtcNow.Ticks ^ System.Guid.NewGuid().GetHashCode()));

        private void Start()
        {
            EnsureDirectionalLightIfNeeded();
            EnsureUi();
            // Grid 2×4: place ally/enemy pair centered on current camera.
            var cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = new Color(0.06f, 0.09f, 0.14f, 1f);
            var uiWorldLift = MetaHudTheme.DuelPrepBottomUiWorldHeight(cam);
            BattleGrid.ConfigureForCenteredPair(cam, gapBetweenGrids: 0.86f, baseY: -0.74f, extraBaseYOffset: uiWorldLift * 0.55f);
            BattleGrid.BuildVisuals(transform);
            RefreshDuelSceneBackground();

            if (unitCatalog == null)
                unitCatalog = Resources.Load<UnitCatalog>("UnitCatalog_Main");

            unitCatalog?.RebuildIndex();
            _allyBench = new AllyBenchInventory(this, unitCatalog, transform);

            ApplyLearningCampaignBeforeSpawn();
            SpineBattleAnimator.DebugLogAllInstances = debugSpineBattleAnimatorLogs;
            SpawnAndRegisterFighters();
            BuildAllyCellPickers();
            BuildInventorySlotPickers();
            RefreshAlliesBagVisibility();
        }

        void ApplyLearningCampaignBeforeSpawn()
        {
            if (!applyLearningCampaignAllyInSampleScene)
            {
                ApplySampleSceneEnemyIdsIfNeeded();
                return;
            }
            ApplyLearningCampaignAllyIfSampleScene();
            ApplySampleSceneEnemyIdsIfNeeded();
        }

        void Update()
        {
            HandlePrepAllyGridCellPointerPressForPendingMenu();
            TryHandlePrepInventoryDeployTap();
            RefreshPendingAllyCellContextTapScreenDelta();
            RefreshAllyCellMenuLayout();

            RefreshAddAllyButton();
            RefreshBattleGridPrepVisuals();
            RefreshMergeUi();
            RefreshPrepPrimaryUi();
            RefreshBuffStatusBar();
            if (EnableAnimationTestUi)
                RefreshAnimationTestPanel();
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
            if (_duelBottomBarRoot != null)
                _duelBottomBarRoot.SetActive(prepPhase);
            if (_duelInfoStripRoot != null)
                _duelInfoStripRoot.SetActive(!BattleEnded);
            if (prepPhase)
            {
                _addAllyButton.interactable =
                    _prepSnapReadyForAddAlly && CanAcceptMoreAllies();
                RefreshAlliesBagVisibility();
            }
        }

        public void BeginCombat()
        {
            if (CombatStarted || BattleEnded)
                return;

            if (RollWallet.Balance >= SixSlotRollResolver.RollCostCoins)
            {
                SetBanner("Roll hết coin trước để nhận đủ ally/buff cho wave này.");
                return;
            }

            if (CountLivingAllies() == 0)
            {
                SetBanner("Chưa có ally sống — cần ít nhất 1 ally để bắt đầu.");
                return;
            }

            if (CountLivingEnemies() == 0)
            {
                SetBanner("Wave này chưa có enemy.");
                return;
            }

            ClearAllySelection();
            SnapshotAllyGridForPostWaveRestore();

            CombatStarted = true;
            CombatEngaged = false;
            CancelCombatEngageRoutine();
            _combatEngageRoutine = StartCoroutine(EngageCombatAfterDelayRoutine());
            ClosePrepUnitStatInspect();
            RefreshBattleGridPrepVisuals();
            RefreshSpeed2xButton();
            if (_startCombatButton != null)
                _startCombatButton.gameObject.SetActive(false);
            if (_prepCoinText != null)
                _prepCoinText.gameObject.SetActive(false);
            if (_objectiveText != null)
                _objectiveText.gameObject.SetActive(false);
            if (_rollUiRoot != null)
                _rollUiRoot.SetActive(false);
            SetBanner($"Wave {currentWave} bắt đầu! Chuẩn bị va chạm...");
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
            _suppressAllyAutoMerge = true;
            foreach (var s in _waveAllySnapshots)
            {
                if (unitCatalog == null || !unitCatalog.TryGet(s.UnitId, out var def))
                    continue;
                SpawnAllyStackMember(s.Row, s.Col, def, s.Rarity, s.MaxHp, s.Attack, s.AllyLineIndex);
            }

            _suppressAllyAutoMerge = false;
            TryAutoMergeAllAllyBoard();

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
            ClosePauseAndRestoreTimeScale();
            CloseSettingsStubPanel();
            CloseRewardOverlay();
            ResetSpeed2x();
            CancelPendingWaveComplete();
            IsDraggingGridUnit = false;
            CombatStarted = false;
            CombatEngaged = false;
            CancelCombatEngageRoutine();
            BattleEnded = false;
            RefreshBattleGridPrepVisuals();
            ClearAllySelection();

            SpawnAndRegisterFighters();
            BuildAllyCellPickers();
            BuildInventorySlotPickers();

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

        const string LearningCampaignAllyUnitId = "unit_legends_melee_robot";
        const string Line1BattleControllerResourcesPath = "Animations/Battle/LegendsMeleeRobot/LegendsMeleeRobot_Battle";
        static bool _line1BattleControllerMissingWarned;
        static bool _line1RigPrefabMissingWarned;

        /// <summary>
        /// Trận campaign mặc định (SampleScene): luôn spawn ally học nếu catalog có id này.
        /// Tránh trường hợp scene lưu <c>allyUnitId</c> cũ (Pinshot/Slip) dù file .unity đã đổi.
        /// </summary>
        void ApplyLearningCampaignAllyIfSampleScene()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene")
                return;
            if (unitCatalog == null)
                unitCatalog = Resources.Load<UnitCatalog>("UnitCatalog_Main");
            unitCatalog?.RebuildIndex();
            if (unitCatalog != null && unitCatalog.TryGet(LearningCampaignAllyUnitId, out _))
                allyUnitId = LearningCampaignAllyUnitId;
        }

        void ApplySampleSceneEnemyIdsIfNeeded()
        {
            if (!applySampleSceneEnemyIds || SceneManager.GetActiveScene().name != "SampleScene")
                return;
            if (unitCatalog == null)
                unitCatalog = Resources.Load<UnitCatalog>("UnitCatalog_Main");
            unitCatalog?.RebuildIndex();

            if (!string.IsNullOrWhiteSpace(sampleSceneEnemyMeleeCatalogId) &&
                unitCatalog != null &&
                unitCatalog.TryGet(sampleSceneEnemyMeleeCatalogId.Trim(), out var meleeDef) &&
                meleeDef != null &&
                meleeDef.TeamKind == UnitTeamKind.Enemy)
            {
                waveEnemyMeleeCatalogId = sampleSceneEnemyMeleeCatalogId.Trim();
                enemyUnitId = waveEnemyMeleeCatalogId;
            }

            if (!string.IsNullOrWhiteSpace(sampleSceneEnemyRangedCatalogId) &&
                unitCatalog != null &&
                unitCatalog.TryGet(sampleSceneEnemyRangedCatalogId.Trim(), out var rangedDef) &&
                rangedDef != null &&
                rangedDef.TeamKind == UnitTeamKind.Enemy)
            {
                waveEnemyRangedCatalogId = sampleSceneEnemyRangedCatalogId.Trim();
            }
        }

        /// <summary>Spawn 5 ally catalog (một ô mỗi loại) + enemy wave 1 — trả false nếu thiếu id trong catalog.</summary>
        bool TrySpawnFiveStarterElementAllies(UnitDefinition waveEnemyDef, float wave1TestHpMultiplier, int enemySpawnCount)
        {
            if (unitCatalog == null || waveEnemyDef == null)
                return false;

            var placements = new (string id, int line, int row, int col)[]
            {
                (AllyLineCatalog.Line0Id, 0, 0, 0),
                (AllyLineCatalog.Line1Id, 1, 1, 0),
                (AllyLineCatalog.Line2Id, 2, 2, 0),
                (AllyLineCatalog.Line3Id, 3, 3, 0),
                (AllyLineCatalog.Line4Id, 4, 0, 1),
            };

            var defs = new UnitDefinition[placements.Length];
            for (var i = 0; i < placements.Length; i++)
            {
                if (!unitCatalog.TryGet(placements[i].id, out defs[i]) || defs[i] == null)
                {
                    Debug.LogWarning($"[DuelDirector] spawnFiveStarter: thiếu unit id '{placements[i].id}' trong catalog.");
                    return false;
                }
            }

            _suppressAllyAutoMerge = true;
            for (var i = 0; i < placements.Length; i++)
            {
                var p = placements[i];
                var def = defs[i];
                var (awHp, awAtk) = AllyStatScaling.ScaleStats(def, Rarity.Common);
                awHp *= wave1TestHpMultiplier;
                SpawnAllyStackMember(p.row, p.col, def, Rarity.Common, awHp, awAtk, p.line);
            }

            _suppressAllyAutoMerge = false;
            TryAutoMergeAllAllyBoard();

            ClearEnemiesOnly();
            var er = BattleGrid.DefaultEnemyRow;
            var ec = BattleGrid.DefaultEnemyCol;
            var et = waveEnemyDef.Rarity;
            var ehp = waveEnemyDef.MaxHitPoints * CurrentLevelEnemyHpMultiplier() * wave1TestHpMultiplier;
            var eatk = waveEnemyDef.Attack * CurrentLevelEnemyAtkMultiplier();
            for (var i = 0; i < enemySpawnCount; i++)
                SpawnEnemyStackMember(er, ec, waveEnemyDef, et, ehp, eatk);

            return true;
        }

        void SpawnAndRegisterFighters()
        {
            TeardownFighters();

            BattleRunBuffs.Clear();
            PendingRollGrants.Clear();
            var metaPassives = MetaProgressionStore.GetTreasurePassiveTotals();
            RollWallet.ResetForNewBattle(startingRollCoins + metaPassives.StartRollCoinBonus);

            var maxPlayable = Mathf.Min(MaxLevels, MetaProgressionStore.UnlockedLevel);
            currentLevel = Mathf.Clamp(MetaProgressionStore.SelectedLevel, 1, maxPlayable);
            currentWave = 1;
            BattleEnded = false;
            CombatStarted = false;
            RefreshBattleGridPrepVisuals();
            CancelPendingWaveComplete();
            _waveAllySnapshots.Clear();
            _prepSnapReadyForAddAlly = true;

            var spawnedFromCatalog = false;
            var isLevel1Wave1 = currentLevel == 1 && currentWave == 1;
            var wave1TestSpawnCount = isLevel1Wave1 ? 1 : AllyMergeRules.MaxStackPerCell;
            var wave1TestHpMultiplier = isLevel1Wave1 ? 2f : 1f;
            if (unitCatalog != null)
            {
                unitCatalog.RebuildIndex();
                var waveEnemyDef = ResolveEnemyDefinitionForWave(currentWave);
                if (waveEnemyDef == null && unitCatalog.TryGet(enemyUnitId, out var ep))
                    waveEnemyDef = ep;

                if (waveEnemyDef != null)
                {
                    if (spawnFiveStarterElementAlliesOnWave1 && isLevel1Wave1 &&
                        TrySpawnFiveStarterElementAllies(waveEnemyDef, wave1TestHpMultiplier, wave1TestSpawnCount))
                    {
                        spawnedFromCatalog = true;
                    }
                    else if (unitCatalog.TryGet(allyUnitId, out var allyDef))
                    {
                        var ar = BattleGrid.DefaultAllyRow;
                        var ac = BattleGrid.DefaultAllyCol;
                        var (awHp, awAtk) = AllyStatScaling.ScaleStats(allyDef, Rarity.Common);
                        awHp *= wave1TestHpMultiplier;
                        var startLine = AllyLineCatalog.LineIndexFromUnitId(allyUnitId);
                        for (var i = 0; i < wave1TestSpawnCount; i++)
                            SpawnAllyStackMember(ar, ac, allyDef, Rarity.Common, awHp, awAtk, startLine);

                        ClearEnemiesOnly();
                        var er = BattleGrid.DefaultEnemyRow;
                        var ec = BattleGrid.DefaultEnemyCol;
                        var et = waveEnemyDef.Rarity;
                        var ehp = waveEnemyDef.MaxHitPoints * CurrentLevelEnemyHpMultiplier() * wave1TestHpMultiplier;
                        var eatk = waveEnemyDef.Attack * CurrentLevelEnemyAtkMultiplier();
                        for (var i = 0; i < wave1TestSpawnCount; i++)
                            SpawnEnemyStackMember(er, ec, waveEnemyDef, et, ehp, eatk);
                        spawnedFromCatalog = true;
                    }
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
                const float fallbackMeleeSquareScale = 0.68f;
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
                var catalogId = AllyLineCatalog.UnitIdForLine(line, grant.RarityTier);
                if (unitCatalog == null || !unitCatalog.TryGet(catalogId, out var def))
                    continue;
                var (hp, atk) = AllyStatScaling.ScaleStats(def, grant.RarityTier);
                if (!TryAddAllyToBoardOrBench(def, grant.RarityTier, hp, atk, line))
                {
                    Debug.LogWarning("[DuelDirector] Bỏ qua ally từ roll — lưới và inventory đầy.");
                    break;
                }
            }

            TryAutoMergeAllAllyBoard();
            _allyBench?.TryAutoMergeAll();
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
            var boltPath = def != null ? def.RangedBoltTextureResourcesPath : null;
            actor.ConfigureCombat(stk, ranged, atkRange, interval, boltPath);
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

            var boltPath = def != null ? def.RangedBoltTextureResourcesPath : null;
            enemyGo.GetComponent<DuelActor>().ConfigureCombat(stk, ranged, atkRange, interval, boltPath);
        }

        void RegisterAlly(GameObject allyGo)
        {
            var h = allyGo.GetComponent<CombatHealth>();
            allyGo.GetComponent<DuelActor>().Init(this);
            allyGo.GetComponent<DuelUnitGridDrag>().Init(this);
            EnsurePrepUnitInfoButton(allyGo);
            h.Died += OnUnitDied;
            _allies.Add(h);
        }

        void RegisterEnemy(GameObject enemyGo)
        {
            var h = enemyGo.GetComponent<CombatHealth>();
            enemyGo.GetComponent<DuelActor>().Init(this);
            enemyGo.GetComponent<DuelUnitGridDrag>().Init(this);
            EnsurePrepUnitInfoButton(enemyGo);
            h.Died += OnUnitDied;
            _enemies.Add(h);
        }

        void EnsurePrepUnitInfoButton(GameObject unitGo)
        {
            var btn = unitGo.GetComponent<PrepUnitInfoButton>();
            if (btn == null)
                btn = unitGo.AddComponent<PrepUnitInfoButton>();
            btn.Init(this);
        }

        void SpawnEnemiesForWaveWithTint(UnitDefinition enemyDef)
        {
            ClearEnemiesOnly();

            var n = Mathf.Clamp(currentWave, 1, MaxWaves);
            var tier = enemyDef != null ? enemyDef.Rarity : Rarity.Common;
            var hp = (enemyDef != null ? enemyDef.MaxHitPoints : enemyMaxHp) * CurrentLevelEnemyHpMultiplier();
            var atk = (enemyDef != null ? enemyDef.Attack : enemyStrikeDamage) * CurrentLevelEnemyAtkMultiplier();

            var placed = 0;
            for (var r = 0; r < BattleGrid.Rows && placed < n; r++)
            {
                for (var c = 0; c < BattleGrid.Cols && placed < n; c++)
                {
                    while (placed < n && CellAllowsAddEnemy(r, c, enemyDef) &&
                           CountEnemyStackInCell(r, c, EnemyStackUnitId(enemyDef), tier) <
                           AllyMergeRules.MaxEnemyStackPerCell)
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

            if (!CanAcceptMoreAllies())
                return;

            if (unitCatalog == null || !unitCatalog.TryGet(allyUnitId, out var def))
                return;

            var addLine = AllyLineCatalog.LineIndexFromUnitId(allyUnitId);
            var (hpA, atkA) = AllyStatScaling.ScaleStats(def, Rarity.Common);
            TryAddAllyToBoardOrBench(def, Rarity.Common, hpA, atkA, addLine);
        }

        bool CanAcceptMoreAllies()
        {
            if (CountLivingAllies() < BattleGrid.CellCount && HasEmptyAllyCell())
                return true;
            return _allyBench != null && _allyBench.HasFreeSlot;
        }

        bool TryAddAllyToBoardOrBench(UnitDefinition def, Rarity tier, float hp, float atk, int line)
        {
            line = AllyLineCatalog.ClampLineIndex(line);
            if (CountLivingAllies() < BattleGrid.CellCount &&
                TryPickRandomAllyCellForAdd(line, tier, out var row, out var col))
            {
                SpawnAllyStackMember(row, col, def, tier, hp, atk, line);
                return true;
            }

            if (_allyBench != null && _allyBench.TryAdd(def, tier, hp, atk, line))
                return true;

            return false;
        }

        internal bool TryPickBoardCellForAllyAdd(int allyLineIndex, Rarity stackTier, out int row, out int col) =>
            TryPickRandomAllyCellForAdd(allyLineIndex, stackTier, out row, out col);

        internal void SpawnAllyOnBoardFromBench(
            int row,
            int col,
            UnitDefinition def,
            Rarity tier,
            float hp,
            float atk,
            int allyLineIndex)
        {
            SpawnAllyStackMember(row, col, def, tier, hp, atk, allyLineIndex);
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
            for (var r = 0; r < BattleGrid.Rows; r++)
            {
                for (var c = 0; c < BattleGrid.Cols; c++)
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
            var stackPref = new List<(int r, int c)>(BattleGrid.CellCount);
            var emptyOrOther = new List<(int r, int c)>(BattleGrid.CellCount);
            for (var r = 0; r < BattleGrid.Rows; r++)
            {
                for (var c = 0; c < BattleGrid.Cols; c++)
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
            if (!CombatStarted || !CombatEngaged || BattleEnded)
                return;

            var mid = (allyWorldPos + enemyWorldPos) * 0.5f;
            mid.z = 0f;

            var allyPop = Vector3.Lerp(mid, allyWorldPos, 0.4f) + Vector3.up * 0.12f;
            var enemyPop = Vector3.Lerp(mid, enemyWorldPos, 0.4f) + Vector3.up * 0.12f;
            var totalDamage = Mathf.Max(damageAllyReceived, damageEnemyReceived);
            var strong = totalDamage >= 18f;
            var impactScale = strong ? 0.54f : 0.38f;
            CombatImpactVfx.SpawnSlash(mid + Vector3.up * 0.06f, new Color(1f, 0.86f, 0.34f, 0.95f), impactScale);
            CombatImpactVfx.SpawnSpark(mid + Vector3.up * 0.08f, Color.white, impactScale * 0.72f);
            PlayCombatImpactFeedback(totalDamage, strong);

            if (damageAllyReceived > 0.0001f)
                FloatingDamagePopup.SpawnAt(allyPop, damageAllyReceived, new Color(0.55f, 0.88f, 1f, 1f), strong);
            if (damageEnemyReceived > 0.0001f)
                FloatingDamagePopup.SpawnAt(enemyPop, damageEnemyReceived, new Color(1f, 0.4f, 0.32f, 1f), strong);
        }

        /// <summary>Nhát làm đối thủ chết — trước đây không qua <see cref="ReportHitExchange"/>.</summary>
        internal void ReportKillingHit(CombatHealth victim, float damageDealt)
        {
            if (damageDealt <= 0f || victim == null)
                return;

            var pos = victim.transform.position + Vector3.up * 0.12f;
            var enemyHit = victim.Faction == CombatFaction.Enemy;
            CombatImpactVfx.SpawnSlash(pos, new Color(1f, 0.92f, 0.48f, 1f), 0.68f);
            CombatImpactVfx.SpawnSpark(pos, new Color(1f, 1f, 1f, 0.96f), 0.52f);
            PlayCombatImpactFeedback(damageDealt, true);
            FloatingDamagePopup.SpawnAt(
                pos,
                damageDealt,
                enemyHit ? new Color(1f, 0.4f, 0.32f, 1f) : new Color(0.55f, 0.88f, 1f, 1f),
                true);
        }

        static void PlayCombatImpactFeedback(float damage, bool strong)
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            var feedback = BattleCameraFeedback25D.Active ?? BattleCameraFeedback25D.Ensure(cam);
            if (feedback == null)
                return;
            var amplitude = Mathf.Clamp(0.012f + damage * 0.00075f, 0.014f, strong ? 0.05f : 0.032f);
            feedback.PlayImpact(amplitude, strong);
        }

        void TeardownFighters()
        {
            foreach (var seq in Object.FindObjectsByType<UnitDeathSequence>(FindObjectsInactive.Include))
            {
                if (seq != null)
                    Destroy(seq.gameObject);
            }

            ClearAllAlliesOnly();
            _waveAllySnapshots.Clear();
            ClearEnemiesOnly();
            _allyBench?.ClearAll();
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
            if (_pauseRestartButton != null)
                _pauseRestartButton.onClick.RemoveListener(OnPauseRestartClicked);
            if (_pauseHomeButton != null)
                _pauseHomeButton.onClick.RemoveListener(OnPauseHomeClicked);
            if (_addAllyButton != null)
                _addAllyButton.onClick.RemoveListener(AddAllyFromButton);
            if (_animationTestButton != null)
                _animationTestButton.onClick.RemoveListener(OnAnimationTestButtonClicked);
            if (_animationTestPrevButton != null)
                _animationTestPrevButton.onClick.RemoveListener(OnAnimationTestPrevClicked);
            if (_animationTestNextButton != null)
                _animationTestNextButton.onClick.RemoveListener(OnAnimationTestNextClicked);
            if (_animationTestIdleButton != null)
                _animationTestIdleButton.onClick.RemoveListener(OnAnimationTestIdleClicked);
            if (_animationTestWalkButton != null)
                _animationTestWalkButton.onClick.RemoveListener(OnAnimationTestWalkClicked);
            if (_animationTestAttackButton != null)
                _animationTestAttackButton.onClick.RemoveListener(OnAnimationTestAttackClicked);
            if (_animationTestDieButton != null)
                _animationTestDieButton.onClick.RemoveListener(OnAnimationTestDieClicked);
            if (_animationTestAutoButton != null)
                _animationTestAutoButton.onClick.RemoveListener(OnAnimationTestAutoClicked);
            if (_animationTestCloseButton != null)
                _animationTestCloseButton.onClick.RemoveListener(OnAnimationTestCloseClicked);
            if (_pauseTopButton != null)
                _pauseTopButton.onClick.RemoveListener(OnPauseTopButtonClicked);
            if (_settingsTopButton != null)
                _settingsTopButton.onClick.RemoveListener(OnSettingsTopButtonClicked);
            if (_pauseResumeButton != null)
                _pauseResumeButton.onClick.RemoveListener(OnPauseResumeClicked);
            if (_settingsStubCloseButton != null)
                _settingsStubCloseButton.onClick.RemoveListener(OnSettingsStubCloseClicked);
            if (_rewardPrimaryButton != null)
                _rewardPrimaryButton.onClick.RemoveListener(OnRewardPrimaryClicked);
            if (_rewardSecondaryButton != null)
                _rewardSecondaryButton.onClick.RemoveListener(OnRewardSecondaryClicked);
            if (_speed2xStubButton != null)
                _speed2xStubButton.onClick.RemoveListener(OnSpeed2xClicked);
            _speed2xActive = false;
            Time.timeScale = 1f;
            ClosePauseAndRestoreTimeScale();
            CloseSettingsStubPanel();
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

            var seq = dead.GetComponent<UnitDeathSequence>();
            if (seq == null)
                seq = dead.gameObject.AddComponent<UnitDeathSequence>();
            seq.Begin();

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
            SetBanner("Wave clear! Đang tổng kết...");
            _waveCompleteRoutine = StartCoroutine(WaveCompleteAfterDelayRoutine());
        }

        IEnumerator WaveCompleteAfterDelayRoutine()
        {
            var total = Mathf.Max(0.01f, waveCompleteDelaySeconds);
            var steps = Mathf.Max(1, Mathf.RoundToInt(total));
            var dt = total / steps;

            for (var n = steps; n >= 1; n--)
            {
                SetBanner($"Wave clear! {n}");
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
            ResetSpeed2x();
            CombatStarted = false;
            CombatEngaged = false;
            CancelCombatEngageRoutine();
            BattleEnded = true;
            RefreshBattleGridPrepVisuals();
            SetBanner("Thua! (Ally đã gục)");
            if (_startCombatButton != null)
                _startCombatButton.gameObject.SetActive(false);
            ShowRewardOverlay(
                "Defeat",
                $"Level {currentLevel} - Wave {currentWave}\nĐội hình đã gục. Chơi lại để thử sắp xếp khác hoặc roll khác.",
                "Chơi lại",
                false,
                "Home",
                true);
        }

        void EndBattleWaveWin()
        {
            CancelPendingWaveComplete();
            ResetSpeed2x();
            CombatStarted = false;
            CombatEngaged = false;
            CancelCombatEngageRoutine();
            BattleEnded = false;
            RefreshBattleGridPrepVisuals();
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
                RefreshBattleGridPrepVisuals();
                _prepSnapReadyForAddAlly = true;

                ReviveAlliesFromLastWaveLayout();
                SetBanner($"Hoàn thành level {currentLevel}! +{goldReward} Gold, +{keyReward} Key.");
                if (_startCombatButton != null)
                    _startCombatButton.gameObject.SetActive(false);
                ShowRewardOverlay(
                    $"Level {currentLevel} Clear!",
                    $"+{goldReward} Gold\n+{keyReward} Treasure Keys\nLevel tiếp theo đã mở nếu còn trong prototype.",
                    "Về Home",
                    true,
                    "Chơi lại",
                    false);
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
            ShowRewardOverlay(
                $"Wave {completed} Clear",
                $"+{wavePrepRollCoinGrant + waveCoinBonus} roll coin\nWave {currentWave} đã sẵn sàng. Roll/sắp đội hình rồi tiếp tục.",
                "Tiếp tục",
                false,
                null,
                false);

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
            BuildDuelTopBar(canvasGo.transform, _banner.font);
            BuildPauseOverlay(canvasGo.transform, _banner.font);
            BuildRewardOverlay(canvasGo.transform, _banner.font);
            BuildSettingsStubPanel(canvasGo.transform, _banner.font);

            BuildDuelBottomUi(canvasGo.transform, _banner.font);

            if (EnableAnimationTestUi)
                BuildAnimationTestUi(canvasGo.transform, _banner.font, MetaHudTheme.BottomActionBarHeight + MetaHudTheme.InfoStripHeight + 16f);

            BuildRollGachaUi(canvasGo.transform, _banner.font);
            BuildAllyCellContextMenu(canvasGo.transform, _banner.font);
            BuildPrepStatInspectPanel(canvasGo.transform, _banner.font);
        }

        void BuildDuelBottomUi(Transform canvasParent, Font font)
        {
            var barH = MetaHudTheme.BottomActionBarHeight;
            var infoH = MetaHudTheme.InfoStripHeight;

            var infoLane = MetaHudTheme.CreateBottomLanePanel(
                canvasParent, infoH, new Color(0.06f, 0.08f, 0.12f, 0.82f), "DuelInfoStrip");
            _duelInfoStripRoot = infoLane.gameObject;
            infoLane.anchoredPosition = new Vector2(0f, barH);

            _prepCoinText = CreateBottomInfoText(infoLane.transform, font, 58f, 26f);
            _prepCoinText.fontSize = 20;

            _objectiveText = CreateBottomInfoText(infoLane.transform, font, 33f, 26f);
            _objectiveText.fontSize = 18;
            _objectiveText.color = new Color(0.76f, 0.88f, 1f, 1f);

            _banner.transform.SetParent(infoLane.transform, false);
            var bannerRt = _banner.rectTransform;
            bannerRt.anchorMin = new Vector2(0.5f, 0f);
            bannerRt.anchorMax = new Vector2(0.5f, 0f);
            bannerRt.pivot = new Vector2(0.5f, 0f);
            bannerRt.anchoredPosition = new Vector2(0f, 4f);
            bannerRt.sizeDelta = new Vector2(900f, 28f);
            _banner.fontSize = 20;

            var barLane = MetaHudTheme.CreateBottomLanePanel(
                canvasParent, barH, MetaHudTheme.BottomLaneBand, "DuelBottomBar");
            _duelBottomBarRoot = barLane.gameObject;
            var row = MetaHudTheme.CreateHorizontalRowParent(barLane, 8f);

            _addAllyButton = CreateBottomTextButton(
                row, font, "Thêm ally", 0f,
                new Color(0.16f, 0.52f, 0.38f, 0.96f), HudUiSprites.CombatBlueButton);
            _addAllyButton.onClick.AddListener(AddAllyFromButton);

            _startCombatButton = CreateBottomTextButton(
                row, font, "Bắt đầu", 0f, PrepStartButtonColor, HudUiSprites.CombatOrangeButton);
            _startCombatButton.onClick.AddListener(OnPrepPrimaryClicked);
            _startCombatButtonLabel = _startCombatButton.GetComponentInChildren<Text>();

            _alliesBagToggleButton = CreateBottomTextButton(
                row, font, "Túi 0/16 ▼", 0f,
                new Color(0.22f, 0.38f, 0.55f, 0.98f), HudUiSprites.CombatBlueButton);
            _alliesBagToggleButton.onClick.AddListener(OnToggleAlliesBagClicked);

            var children = new List<RectTransform>
            {
                _addAllyButton.GetComponent<RectTransform>(),
                _startCombatButton.GetComponent<RectTransform>(),
                _alliesBagToggleButton.GetComponent<RectTransform>()
            };
            var fracs = new List<float> { 0.34f, 0.42f, 0.24f };
            MetaHudTheme.LayoutHorizontalRow(row, MetaHudTheme.ActionRowGap, children, fracs);
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
            _allyCtxSellButton = CreateCtxButton(_allyCellMenuRoot.transform, font, "", y, new Color(0.38f, 0.28f, 0.18f, 0.98f), HudUiSprites.CombatSell, new Vector2(280f, 52f));
            _allyCtxSellButton.onClick.AddListener(OnAllyCtxSellClicked);
            y -= 56f;
            _allyCtxMergeButton = CreateCtxButton(_allyCellMenuRoot.transform, font, "", y, new Color(0.22f, 0.42f, 0.28f, 0.98f), HudUiSprites.CombatMerge, new Vector2(280f, 52f));
            _allyCtxMergeButton.onClick.AddListener(OnAllyCtxMergeClicked);
            y -= 52f;
            _allyCtxCloseButton = CreateCtxButton(_allyCellMenuRoot.transform, font, "Đóng", y, new Color(0.25f, 0.25f, 0.3f, 0.98f));
            _allyCtxCloseButton.onClick.AddListener(OnAllyCtxCloseClicked);

            _allyCellMenuRoot.SetActive(false);
        }

        void BuildPrepStatInspectPanel(Transform canvasParent, Font font)
        {
            _prepStatPanelRoot = new GameObject("PrepStatInspectPanel");
            _prepStatPanelRoot.transform.SetParent(canvasParent, false);
            var rt = _prepStatPanelRoot.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 120f);
            rt.sizeDelta = new Vector2(560f, 360f);

            var bg = _prepStatPanelRoot.AddComponent<Image>();
            bg.sprite = CreateWhiteSprite();
            bg.color = new Color(0.1f, 0.12f, 0.18f, 0.97f);
            bg.raycastTarget = true;

            _prepStatTitleText = CreateCtxLabel(_prepStatPanelRoot.transform, font, "Chỉ số", 28, new Vector2(0f, 118f), new Vector2(520f, 44f));
            _prepStatBodyText = CreateCtxLabel(_prepStatPanelRoot.transform, font, "", 22, new Vector2(0f, 12f), new Vector2(520f, 200f));
            _prepStatBodyText.alignment = TextAnchor.UpperCenter;

            var closeBtn = CreateCtxButton(_prepStatPanelRoot.transform, font, "Đóng", -138f, new Color(0.25f, 0.25f, 0.3f, 0.98f), null, new Vector2(240f, 52f));
            closeBtn.onClick.AddListener(ClosePrepUnitStatInspect);

            _prepStatPanelRoot.SetActive(false);
        }

        public void ShowPrepUnitStatInspect(CombatHealth target)
        {
            if (!SandboxCanMutateUnits || target == null || target.IsDead || _prepStatPanelRoot == null)
                return;

            _prepStatTarget = target;
            FormatPrepStatInspectText(target, out var title, out var body);
            if (_prepStatTitleText != null)
                _prepStatTitleText.text = title;
            if (_prepStatBodyText != null)
                _prepStatBodyText.text = body;
            _prepStatPanelRoot.SetActive(true);
            CloseAllyCellContextMenu();
        }

        public void ClosePrepUnitStatInspect()
        {
            _prepStatTarget = null;
            if (_prepStatPanelRoot != null)
                _prepStatPanelRoot.SetActive(false);
        }

        void FormatPrepStatInspectText(CombatHealth hp, out string title, out string body)
        {
            title = hp.Faction == CombatFaction.Ally ? "Ally" : "Enemy";
            body = "";

            var actor = hp.GetComponent<DuelActor>();
            var allySpec = hp.GetComponent<AllyInstanceSpec>();
            var enemySpec = hp.GetComponent<EnemyInstanceSpec>();

            UnitDefinition def = null;
            if (allySpec != null && unitCatalog != null)
                unitCatalog.TryGet(allySpec.ResolveLeader().CatalogUnitId, out def);
            else if (enemySpec != null && unitCatalog != null)
                unitCatalog.TryGet(enemySpec.ResolveLeader().CatalogUnitId, out def);

            var displayName = def != null ? def.DisplayName : hp.gameObject.name;
            title = displayName;

            var rarity = allySpec != null
                ? allySpec.ResolveLeader().RarityTier
                : enemySpec != null
                    ? enemySpec.ResolveLeader().RarityTier
                    : Rarity.Common;

            var maxHp = hp.Max;
            var atk = actor != null ? actor.AttackDamage : 0f;
            var ranged = actor != null && actor.IsRangedCombat;
            var range = def != null ? def.AttackRange : 0f;
            var interval = def != null ? def.RangedShotIntervalSeconds : 0f;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Phẩm: {rarity}");
            sb.AppendLine($"HP: {hp.Current:0.#} / {maxHp:0.#}");
            sb.AppendLine($"Tấn công: {atk:0.#}");

            if (allySpec != null)
            {
                var line = AllyLineCatalog.ClampLineIndex(allySpec.ResolveLeader().AllyLineIndex);
                sb.AppendLine($"Dòng: {PrepLineLabel(line)}");
            }

            if (ranged)
                sb.AppendLine($"Tầm bắn: {range:0.##}  |  Chu kỳ: {interval:0.##}s");
            else
                sb.AppendLine("Cận chiến");

            if (def != null && def.RangedAttack)
                sb.AppendLine("(Định nghĩa: ranged)");
            if (AllyMergeRules.CanMerge(rarity))
                sb.AppendLine("Có thể auto-merge (3 cùng dòng + phẩm)");
            else
                sb.AppendLine("Không merge thêm");

            body = sb.ToString().TrimEnd();
        }

        static string PrepLineLabel(int line) => line switch
        {
            0 => "Mộc",
            1 => "Hỏa",
            2 => "Kim",
            3 => "Thủy",
            4 => "Thổ / Knight",
            _ => $"Dòng {line}"
        };

        void BuildAnimationTestUi(Transform canvasParent, Font font, float yFromBottom)
        {
            _animationTestButton = CreateBottomTextButton(
                canvasParent,
                font,
                "Test",
                yFromBottom,
                new Color(0.74f, 0.52f, 0.18f, 0.95f));
            _animationTestButton.onClick.AddListener(OnAnimationTestButtonClicked);

            var testBtnRt = _animationTestButton.GetComponent<RectTransform>();
            testBtnRt.anchorMin = new Vector2(1f, 0f);
            testBtnRt.anchorMax = new Vector2(1f, 0f);
            testBtnRt.pivot = new Vector2(1f, 0f);
            testBtnRt.anchoredPosition = new Vector2(-20f, yFromBottom);
            testBtnRt.sizeDelta = new Vector2(190f, 78f);

            var testBtnLabel = _animationTestButton.GetComponentInChildren<Text>();
            if (testBtnLabel != null)
                testBtnLabel.fontSize = 30;

            _animationTestPanelRoot = new GameObject("AnimationTestPanel");
            _animationTestPanelRoot.transform.SetParent(canvasParent, false);
            _animationTestPanelRt = _animationTestPanelRoot.AddComponent<RectTransform>();
            _animationTestPanelRt.anchorMin = new Vector2(0.5f, 0.5f);
            _animationTestPanelRt.anchorMax = new Vector2(0.5f, 0.5f);
            _animationTestPanelRt.pivot = new Vector2(0.5f, 0.5f);
            _animationTestPanelRt.anchoredPosition = new Vector2(0f, 240f);
            _animationTestPanelRt.sizeDelta = new Vector2(600f, 430f);

            var panelBg = _animationTestPanelRoot.AddComponent<Image>();
            panelBg.sprite = CreateWhiteSprite();
            panelBg.color = new Color(0.08f, 0.1f, 0.15f, 0.94f);
            panelBg.raycastTarget = true;

            CreateCtxLabel(_animationTestPanelRoot.transform, font, "Animation Test", 32, new Vector2(0f, 176f), new Vector2(540f, 44f));
            _animationTestTargetText = CreateCtxLabel(_animationTestPanelRoot.transform, font, "Target: --", 22, new Vector2(0f, 126f), new Vector2(540f, 56f));
            _animationTestInfoText = CreateCtxLabel(_animationTestPanelRoot.transform, font, "Info: --", 19, new Vector2(0f, 80f), new Vector2(540f, 58f));

            _animationTestPrevButton = CreatePanelButton(_animationTestPanelRoot.transform, font, "< Prev", new Vector2(-135f, 24f), new Vector2(220f, 52f), new Color(0.28f, 0.33f, 0.4f, 0.98f));
            _animationTestNextButton = CreatePanelButton(_animationTestPanelRoot.transform, font, "Next >", new Vector2(135f, 24f), new Vector2(220f, 52f), new Color(0.28f, 0.33f, 0.4f, 0.98f));
            _animationTestIdleButton = CreatePanelButton(_animationTestPanelRoot.transform, font, "Idle", new Vector2(-205f, -44f), new Vector2(180f, 52f), new Color(0.22f, 0.36f, 0.58f, 0.98f));
            _animationTestWalkButton = CreatePanelButton(_animationTestPanelRoot.transform, font, "Walk", new Vector2(0f, -44f), new Vector2(180f, 52f), new Color(0.18f, 0.44f, 0.35f, 0.98f));
            _animationTestAttackButton = CreatePanelButton(_animationTestPanelRoot.transform, font, "Attack", new Vector2(205f, -44f), new Vector2(180f, 52f), new Color(0.58f, 0.28f, 0.25f, 0.98f));
            _animationTestDieButton = CreatePanelButton(_animationTestPanelRoot.transform, font, "Die", new Vector2(-102f, -112f), new Vector2(180f, 52f), new Color(0.42f, 0.21f, 0.2f, 0.98f));
            _animationTestAutoButton = CreatePanelButton(_animationTestPanelRoot.transform, font, "Auto", new Vector2(102f, -112f), new Vector2(180f, 52f), new Color(0.24f, 0.42f, 0.3f, 0.98f));
            _animationTestCloseButton = CreatePanelButton(_animationTestPanelRoot.transform, font, "Đóng", new Vector2(0f, -178f), new Vector2(240f, 52f), new Color(0.25f, 0.25f, 0.3f, 0.98f));

            _animationTestPrevButton.onClick.AddListener(OnAnimationTestPrevClicked);
            _animationTestNextButton.onClick.AddListener(OnAnimationTestNextClicked);
            _animationTestIdleButton.onClick.AddListener(OnAnimationTestIdleClicked);
            _animationTestWalkButton.onClick.AddListener(OnAnimationTestWalkClicked);
            _animationTestAttackButton.onClick.AddListener(OnAnimationTestAttackClicked);
            _animationTestDieButton.onClick.AddListener(OnAnimationTestDieClicked);
            _animationTestAutoButton.onClick.AddListener(OnAnimationTestAutoClicked);
            _animationTestCloseButton.onClick.AddListener(OnAnimationTestCloseClicked);

            _animationTestPanelRoot.SetActive(false);
        }

        void OnAnimationTestButtonClicked()
        {
            if (_animationTestPanelRoot == null)
                return;
            var show = !_animationTestPanelRoot.activeSelf;
            _animationTestPanelRoot.SetActive(show);
            if (show)
                RefreshAnimationTestPanel();
        }

        void OnAnimationTestCloseClicked()
        {
            if (_animationTestPanelRoot != null)
                _animationTestPanelRoot.SetActive(false);
        }

        void OnAnimationTestPrevClicked()
        {
            RefreshAnimationTestTargets();
            if (_animationTestTargets.Count == 0)
                return;
            _animationTestTargetIndex = (_animationTestTargetIndex - 1 + _animationTestTargets.Count) % _animationTestTargets.Count;
            RefreshAnimationTestPanel();
        }

        void OnAnimationTestNextClicked()
        {
            RefreshAnimationTestTargets();
            if (_animationTestTargets.Count == 0)
                return;
            _animationTestTargetIndex = (_animationTestTargetIndex + 1) % _animationTestTargets.Count;
            RefreshAnimationTestPanel();
        }

        void OnAnimationTestIdleClicked()
        {
            var anim = GetAnimationTestTarget();
            anim?.DebugPreviewIdle();
            RefreshAnimationTestPanel();
        }

        void OnAnimationTestWalkClicked()
        {
            var anim = GetAnimationTestTarget();
            anim?.DebugPreviewWalk();
            RefreshAnimationTestPanel();
        }

        void OnAnimationTestAttackClicked()
        {
            var anim = GetAnimationTestTarget();
            anim?.DebugPreviewAttack();
            RefreshAnimationTestPanel();
        }

        void OnAnimationTestDieClicked()
        {
            var anim = GetAnimationTestTarget();
            anim?.DebugPreviewDie();
            RefreshAnimationTestPanel();
        }

        void OnAnimationTestAutoClicked()
        {
            var anim = GetAnimationTestTarget();
            anim?.DebugClearPreview();
            RefreshAnimationTestPanel();
        }

        void RefreshAnimationTestTargets()
        {
            _animationTestTargets.Clear();
            var animators = Object.FindObjectsByType<Line1BattleSpriteAnimator>(FindObjectsInactive.Exclude);
            foreach (var anim in animators)
            {
                if (anim == null)
                    continue;
                _animationTestTargets.Add(anim);
            }

            if (_animationTestTargetIndex >= _animationTestTargets.Count)
                _animationTestTargetIndex = 0;
            if (_animationTestTargetIndex < 0)
                _animationTestTargetIndex = 0;
        }

        Line1BattleSpriteAnimator GetAnimationTestTarget()
        {
            RefreshAnimationTestTargets();
            if (_animationTestTargets.Count == 0)
                return null;
            return _animationTestTargets[_animationTestTargetIndex];
        }

        void RefreshAnimationTestPanel()
        {
            if (_animationTestPanelRoot == null || !_animationTestPanelRoot.activeSelf)
                return;

            RefreshAnimationTestTargets();
            var hasTarget = _animationTestTargets.Count > 0;
            if (_animationTestPrevButton != null) _animationTestPrevButton.interactable = hasTarget;
            if (_animationTestNextButton != null) _animationTestNextButton.interactable = hasTarget;
            if (_animationTestIdleButton != null) _animationTestIdleButton.interactable = hasTarget;
            if (_animationTestWalkButton != null) _animationTestWalkButton.interactable = hasTarget;
            if (_animationTestAttackButton != null) _animationTestAttackButton.interactable = hasTarget;
            if (_animationTestDieButton != null) _animationTestDieButton.interactable = hasTarget;
            if (_animationTestAutoButton != null) _animationTestAutoButton.interactable = hasTarget;

            if (!hasTarget)
            {
                if (_animationTestTargetText != null)
                    _animationTestTargetText.text = "Target: (không có ally có animator bridge)";
                if (_animationTestInfoText != null)
                    _animationTestInfoText.text = "Info: spawn ally line có animation rồi bấm Test lại";
                return;
            }

            var anim = _animationTestTargets[_animationTestTargetIndex];
            var host = anim != null ? anim.gameObject : null;
            var spec = host != null ? host.GetComponent<AllyInstanceSpec>() : null;
            var hp = host != null ? host.GetComponent<CombatHealth>() : null;
            var faction = hp != null ? hp.Faction.ToString() : "Unknown";
            var unitId = spec != null && !string.IsNullOrWhiteSpace(spec.CatalogUnitId)
                ? spec.CatalogUnitId
                : (host != null ? host.name : "unknown");

            if (_animationTestTargetText != null)
                _animationTestTargetText.text = $"Target {_animationTestTargetIndex + 1}/{_animationTestTargets.Count}: {unitId} ({faction})";

            var hpText = hp != null ? $"HP {hp.Current:0}/{hp.Max:0}" : "HP --";
            var bind = anim != null ? anim.DebugBindingSummary() : "Binding --";
            if (_animationTestInfoText != null)
                _animationTestInfoText.text = $"{hpText} | {bind}";
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

        static Button CreateCtxButton(
            Transform parent,
            Font font,
            string label,
            float yFromCenter,
            Color bg,
            Sprite iconSprite = null,
            Vector2? sizeOverride = null)
        {
            var btnGo = new GameObject(string.IsNullOrEmpty(label) ? "CtxBtn" : label + "Btn");
            btnGo.transform.SetParent(parent, false);
            var img = btnGo.AddComponent<Image>();
            HudUiSprites.ApplyIcon(img, iconSprite, bg);
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
            labelGo.SetActive(iconSprite == null || !string.IsNullOrEmpty(label));
            var lrt = tx.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, yFromCenter);
            rt.sizeDelta = sizeOverride ?? new Vector2(280f, 46f);
            return btn;
        }

        static Button CreatePanelButton(
            Transform parent,
            Font font,
            string label,
            Vector2 anchored,
            Vector2 size,
            Color bg,
            Sprite iconSprite = null)
        {
            var btnGo = new GameObject(label + "PanelBtn");
            btnGo.transform.SetParent(parent, false);
            var img = btnGo.AddComponent<Image>();
            HudUiSprites.ApplyIcon(img, iconSprite, bg);
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
            tx.fontSize = 25;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = Color.white;
            tx.raycastTarget = false;
            labelGo.SetActive(iconSprite == null || !string.IsNullOrEmpty(label));
            var lrt = tx.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;
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

            for (var r = 0; r < BattleGrid.Rows; r++)
            {
                for (var c = 0; c < BattleGrid.Cols; c++)
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

        void BuildInventorySlotPickers()
        {
            if (_inventoryPickersRoot != null)
            {
                Destroy(_inventoryPickersRoot.gameObject);
                _inventoryPickersRoot = null;
            }

            var root = new GameObject("InventorySlotPickers");
            root.transform.SetParent(transform, false);
            _inventoryPickersRoot = root.transform;

            for (var i = 0; i < BattleGrid.InventorySlotCount; i++)
            {
                var go = new GameObject($"InvPicker_{i}");
                go.transform.SetParent(_inventoryPickersRoot, false);
                go.transform.position = BattleGrid.GetInventorySlotCenter(i);
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(BattleGrid.InventoryCell * 0.94f, BattleGrid.InventoryCell * 0.94f);
                var pick = go.AddComponent<InventorySlotPicker>();
                pick.SlotIndex = i;
            }
        }

        void TryHandlePrepInventoryDeployTap()
        {
            if (!SandboxCanMutateUnits || _allyBench == null)
                return;
            if (!PrepGridPointer.WasPressedThisFrame())
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            if (!PrepGridPointer.TryGetPrimaryScreenPosition(out var pressScreen))
                return;

            var w = ScreenToWorldOnPlane(pressScreen);
            var hits = Physics2D.OverlapPointAll(w);
            foreach (var h in hits)
            {
                if (h == null)
                    continue;
                var bench = h.GetComponentInParent<BenchAllyMarker>();
                if (bench != null)
                {
                    _allyBench.TryDeploySlotToBoard(bench.SlotIndex);
                    return;
                }

                var picker = h.GetComponent<InventorySlotPicker>();
                if (picker != null)
                {
                    _allyBench.TryDeploySlotToBoard(picker.SlotIndex);
                    return;
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

            foreach (var h in hits)
            {
                if (h == null)
                    continue;
                if (h.GetComponentInParent<PrepUnitInfoButton>() != null)
                    return;
            }

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

            if (_allyCtxMergeButton != null)
                _allyCtxMergeButton.gameObject.SetActive(false);

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
            TryAutoMergeAllAllyBoard();
            SetBanner($"Đã bán 1 ally ở ô {CellLabel(row, col)}. +{sellAllyCoinRefund} roll coin.");
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

        void ClosePauseAndRestoreTimeScale()
        {
            if (_pauseOverlayRoot != null)
                _pauseOverlayRoot.SetActive(false);
            ApplyCombatTimeScale();
            RefreshPauseTopIcon(false);
        }

        void ApplyCombatTimeScale()
        {
            if (_pauseOverlayRoot != null && _pauseOverlayRoot.activeSelf)
            {
                Time.timeScale = 0f;
                return;
            }

            Time.timeScale = _speed2xActive && CombatStarted && !BattleEnded ? 2f : 1f;
        }

        void ResetSpeed2x()
        {
            _speed2xActive = false;
            RefreshSpeed2xButton();
            ApplyCombatTimeScale();
        }

        void OnSpeed2xClicked()
        {
            if (!CombatStarted || BattleEnded)
                return;
            _speed2xActive = !_speed2xActive;
            ApplyCombatTimeScale();
            RefreshSpeed2xButton();
        }

        void RefreshSpeed2xButton()
        {
            if (_speed2xStubButton == null)
                return;
            var inCombat = CombatStarted && !BattleEnded;
            _speed2xStubButton.interactable = inCombat;
            var image = _speed2xStubButton.GetComponent<Image>();
            var sprite = _speed2xActive && inCombat ? HudUiSprites.CombatSpeed2On : HudUiSprites.CombatSpeed2Off;
            HudUiSprites.ApplyIcon(image, sprite, inCombat ? MetaHudTheme.ButtonIconBar : MetaHudTheme.ButtonStubDisabled);
        }

        void CloseSettingsStubPanel()
        {
            if (_settingsStubRoot != null)
                _settingsStubRoot.SetActive(false);
        }

        void OnPauseTopButtonClicked()
        {
            if (_pauseOverlayRoot == null)
                return;
            if (_pauseOverlayRoot.activeSelf)
            {
                ClosePauseAndRestoreTimeScale();
                return;
            }

            CloseSettingsStubPanel();
            _pauseOverlayRoot.SetActive(true);
            _pauseOverlayRoot.transform.SetAsLastSibling();
            ApplyCombatTimeScale();
            RefreshPauseTopIcon(true);
        }

        void OnPauseResumeClicked()
        {
            ClosePauseAndRestoreTimeScale();
        }

        void OnSettingsTopButtonClicked()
        {
            CloseSettingsStubPanel();
            if (_settingsStubRoot == null)
                return;
            _settingsStubRoot.SetActive(true);
            _settingsStubRoot.transform.SetAsLastSibling();
        }

        void OnSettingsStubCloseClicked()
        {
            CloseSettingsStubPanel();
        }

        void BuildDuelTopBar(Transform canvasParent, Font font)
        {
            var w = MetaHudTheme.IconBarButtonSize.x;
            var h = MetaHudTheme.IconBarButtonSize.y;
            var gap = MetaHudTheme.IconBarGap;
            var insetX = MetaHudTheme.SafeEdgeX + MetaHudTheme.TopIconBarExtraInsetX;
            var insetY = MetaHudTheme.SafeEdgeYTop + MetaHudTheme.TopIconBarExtraInsetY;
            var px = -insetX - w * 0.5f;
            var py = -insetY - h * 0.5f;

            _pauseTopButton = CreateTopRightAnchoredTextButton(
                canvasParent, font, "", new Vector2(px, py), new Vector2(148f, 64f),
                MetaHudTheme.ButtonIconBar, OnPauseTopButtonClicked, true, "DuelPauseTop", HudUiSprites.CombatPause);

            px -= w + gap;
            _settingsTopButton = CreateTopRightAnchoredTextButton(
                canvasParent, font, "", new Vector2(px, py), MetaHudTheme.IconBarButtonSize,
                MetaHudTheme.ButtonIconBar, OnSettingsTopButtonClicked, true, "DuelSettingsTop", HudUiSprites.MetaSetting);

            px -= w + gap;
            _speed2xStubButton = CreateTopRightAnchoredTextButton(
                canvasParent, font, "", new Vector2(px, py), MetaHudTheme.IconBarButtonSize,
                MetaHudTheme.ButtonIconBar, OnSpeed2xClicked, false, "DuelSpeed2Stub", HudUiSprites.CombatSpeed2Off);
            RefreshSpeed2xButton();
        }

        void BuildPauseOverlay(Transform canvasParent, Font font)
        {
            _pauseOverlayRoot = new GameObject("PauseOverlay");
            _pauseOverlayRoot.transform.SetParent(canvasParent, false);
            var rootRt = _pauseOverlayRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(_pauseOverlayRoot.transform, false);
            var dimRt = dimGo.AddComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.sprite = CreateWhiteSprite();
            dimImg.color = MetaHudTheme.OverlayDim;
            dimImg.raycastTarget = true;

            var panelGo = new GameObject("PausePanel");
            panelGo.transform.SetParent(_pauseOverlayRoot.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(580f, 480f);
            var panelBg = panelGo.AddComponent<Image>();
            panelBg.sprite = CreateWhiteSprite();
            panelBg.color = new Color(0.1f, 0.12f, 0.18f, 0.96f);
            panelBg.raycastTarget = true;

            var titleGo = new GameObject("PauseTitle");
            titleGo.transform.SetParent(panelGo.transform, false);
            var title = titleGo.AddComponent<Text>();
            title.font = font;
            title.text = "Tạm dừng";
            title.fontSize = MetaHudTheme.FontOverlayTitle;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = MetaHudTheme.TextPrimary;
            title.raycastTarget = false;
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = new Vector2(0f, 188f);
            titleRt.sizeDelta = new Vector2(520f, 48f);

            _pauseResumeButton = CreatePanelButton(
                panelGo.transform,
                font,
                "",
                new Vector2(0f, 96f),
                new Vector2(280f, 64f),
                PrepStartButtonColor,
                HudUiSprites.CombatContinue);
            _pauseResumeButton.onClick.AddListener(OnPauseResumeClicked);

            _pauseRestartButton = CreatePanelButton(
                panelGo.transform, font, "Chơi lại", new Vector2(0f, 8f), new Vector2(360f, 72f),
                new Color(0.22f, 0.22f, 0.26f, 0.96f), HudUiSprites.CombatRestart);
            _pauseRestartButton.onClick.AddListener(OnPauseRestartClicked);

            _pauseHomeButton = CreatePanelButton(
                panelGo.transform, font, "Màn hình chính", new Vector2(0f, -80f), new Vector2(360f, 72f),
                new Color(0.2f, 0.26f, 0.4f, 0.96f), HudUiSprites.CombatHomeInBattle);
            _pauseHomeButton.onClick.AddListener(OnPauseHomeClicked);

            _pauseOverlayRoot.SetActive(false);
        }

        void BuildRewardOverlay(Transform canvasParent, Font font)
        {
            _rewardOverlayRoot = new GameObject("RewardOverlay");
            _rewardOverlayRoot.transform.SetParent(canvasParent, false);
            var rootRt = _rewardOverlayRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(_rewardOverlayRoot.transform, false);
            var dimRt = dimGo.AddComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.sprite = CreateWhiteSprite();
            dimImg.color = new Color(0f, 0f, 0f, 0.5f);
            dimImg.raycastTarget = true;

            var panelGo = new GameObject("RewardPanel");
            panelGo.transform.SetParent(_rewardOverlayRoot.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = new Vector2(0f, 42f);
            panelRt.sizeDelta = new Vector2(640f, 520f);
            var panelBg = panelGo.AddComponent<Image>();
            panelBg.sprite = CreateWhiteSprite();
            panelBg.color = new Color(0.07f, 0.1f, 0.16f, 0.97f);
            panelBg.raycastTarget = true;

            _rewardTitleText = CreateCtxLabel(panelGo.transform, font, "Wave Clear", 36, new Vector2(0f, 184f), new Vector2(580f, 58f));
            _rewardTitleText.color = new Color(1f, 0.86f, 0.32f, 1f);

            _rewardBodyText = CreateCtxLabel(panelGo.transform, font, "", 25, new Vector2(0f, 58f), new Vector2(560f, 210f));
            _rewardBodyText.alignment = TextAnchor.MiddleCenter;
            _rewardBodyText.color = new Color(0.9f, 0.95f, 1f, 1f);

            _rewardPrimaryButton = CreatePanelButton(
                panelGo.transform,
                font,
                "Tiếp tục",
                new Vector2(0f, -92f),
                new Vector2(360f, 72f),
                PrepStartButtonColor,
                HudUiSprites.CombatContinue);
            _rewardPrimaryButton.onClick.AddListener(OnRewardPrimaryClicked);

            _rewardSecondaryButton = CreatePanelButton(
                panelGo.transform,
                font,
                "Home",
                new Vector2(0f, -178f),
                new Vector2(300f, 60f),
                MetaHudTheme.ButtonBack,
                HudUiSprites.CombatHomeInBattle);
            _rewardSecondaryButton.onClick.AddListener(OnRewardSecondaryClicked);

            _rewardOverlayRoot.SetActive(false);
        }

        void ShowRewardOverlay(
            string title,
            string body,
            string primaryLabel,
            bool primaryGoesHome,
            string secondaryLabel,
            bool secondaryGoesHome)
        {
            if (_rewardOverlayRoot == null)
                return;

            if (_rewardTitleText != null)
                _rewardTitleText.text = title;
            if (_rewardBodyText != null)
                _rewardBodyText.text = body;

            _rewardPrimaryAction = primaryGoesHome
                ? RewardPrimaryAction.Home
                : BattleEnded
                    ? RewardPrimaryAction.Restart
                    : RewardPrimaryAction.Close;
            _rewardSecondaryAction = secondaryGoesHome
                ? RewardPrimaryAction.Home
                : BattleEnded
                    ? RewardPrimaryAction.Restart
                    : RewardPrimaryAction.Close;

            SetButtonLabel(_rewardPrimaryButton, primaryLabel);
            SetButtonLabel(_rewardSecondaryButton, secondaryLabel);

            if (_rewardPrimaryButton != null)
                _rewardPrimaryButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(primaryLabel));
            if (_rewardSecondaryButton != null)
                _rewardSecondaryButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(secondaryLabel));

            _rewardOverlayRoot.SetActive(true);
            _rewardOverlayRoot.transform.SetAsLastSibling();
        }

        void CloseRewardOverlay()
        {
            if (_rewardOverlayRoot != null)
                _rewardOverlayRoot.SetActive(false);
        }

        static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
                return;
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = label ?? string.Empty;
        }

        void OnRewardPrimaryClicked()
        {
            if (_rewardPrimaryAction == RewardPrimaryAction.Home)
            {
                OnHomeButtonClicked();
                return;
            }

            if (_rewardPrimaryAction == RewardPrimaryAction.Restart)
            {
                if (_rewardOverlayRoot != null)
                    _rewardOverlayRoot.SetActive(false);
                RestartDuel();
                return;
            }

            if (_rewardOverlayRoot != null)
                _rewardOverlayRoot.SetActive(false);
            RefreshPrepPrimaryUi();
        }

        void OnRewardSecondaryClicked()
        {
            if (_rewardSecondaryAction == RewardPrimaryAction.Home)
            {
                OnHomeButtonClicked();
                return;
            }

            if (_rewardSecondaryAction == RewardPrimaryAction.Restart)
            {
                CloseRewardOverlay();
                RestartDuel();
                return;
            }

            CloseRewardOverlay();
        }

        void OnPauseRestartClicked()
        {
            ClosePauseAndRestoreTimeScale();
            RestartDuel();
        }

        void OnPauseHomeClicked()
        {
            ClosePauseAndRestoreTimeScale();
            OnHomeButtonClicked();
        }

        void BuildSettingsStubPanel(Transform canvasParent, Font font)
        {
            _settingsStubRoot = new GameObject("SettingsStubOverlay");
            _settingsStubRoot.transform.SetParent(canvasParent, false);
            var rootRt = _settingsStubRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(_settingsStubRoot.transform, false);
            var dimRt = dimGo.AddComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.sprite = CreateWhiteSprite();
            dimImg.color = new Color(0f, 0f, 0f, 0.45f);
            dimImg.raycastTarget = true;

            var panelGo = new GameObject("SettingsPanel");
            panelGo.transform.SetParent(_settingsStubRoot.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(520f, 320f);
            var panelBg = panelGo.AddComponent<Image>();
            panelBg.sprite = CreateWhiteSprite();
            panelBg.color = new Color(0.12f, 0.14f, 0.2f, 0.98f);
            panelBg.raycastTarget = true;

            var titleGo = new GameObject("SettingsTitle");
            titleGo.transform.SetParent(panelGo.transform, false);
            var title = titleGo.AddComponent<Text>();
            title.font = font;
            title.text = "Cài đặt";
            title.fontSize = MetaHudTheme.FontOverlayTitle;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = MetaHudTheme.TextPrimary;
            title.raycastTarget = false;
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = new Vector2(0f, 96f);
            titleRt.sizeDelta = new Vector2(480f, 44f);

            var bodyGo = new GameObject("SettingsBody");
            bodyGo.transform.SetParent(panelGo.transform, false);
            var body = bodyGo.AddComponent<Text>();
            body.font = font;
            body.text = "Tính năng sắp có — bản build sau sẽ nối menu đầy đủ.";
            body.fontSize = MetaHudTheme.FontOverlayRow;
            body.alignment = TextAnchor.MiddleCenter;
            body.color = MetaHudTheme.TextSecondary;
            body.raycastTarget = false;
            var bodyRt = body.rectTransform;
            bodyRt.anchorMin = bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.pivot = new Vector2(0.5f, 0.5f);
            bodyRt.anchoredPosition = new Vector2(0f, 12f);
            bodyRt.sizeDelta = new Vector2(460f, 120f);

            CreateSettingsStubIconRow(panelGo.transform, new Vector2(-120f, 28f), HudUiSprites.MetaVolumeOn);
            CreateSettingsStubIconRow(panelGo.transform, new Vector2(0f, 28f), HudUiSprites.MetaVolumeOff);
            CreateSettingsStubIconRow(panelGo.transform, new Vector2(120f, 28f), HudUiSprites.MetaOn);
            CreateSettingsStubIconRow(panelGo.transform, new Vector2(0f, -36f), HudUiSprites.MetaTurnOff);

            _settingsStubCloseButton = CreatePanelButton(panelGo.transform, font, "Đóng", new Vector2(0f, -108f), new Vector2(280f, 64f), MetaHudTheme.ButtonBack);
            _settingsStubCloseButton.onClick.AddListener(OnSettingsStubCloseClicked);

            _settingsStubRoot.SetActive(false);
        }

        Button CreateTopRightAnchoredTextButton(
            Transform canvasParent,
            Font font,
            string label,
            Vector2 anchoredTopRight,
            Vector2 size,
            Color bg,
            UnityEngine.Events.UnityAction onClick,
            bool interactable,
            string hierarchyName,
            Sprite iconSprite = null)
        {
            var btnGo = new GameObject(string.IsNullOrEmpty(hierarchyName) ? "TopBarBtn" : hierarchyName);
            btnGo.transform.SetParent(canvasParent, false);

            var image = btnGo.AddComponent<Image>();
            HudUiSprites.ApplyIcon(image, iconSprite, bg);
            image.raycastTarget = true;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = image;
            var colors = btn.colors;
            colors.highlightedColor = bg * 1.12f;
            colors.pressedColor = bg * 0.82f;
            colors.disabledColor = new Color(bg.r, bg.g, bg.b, 0.55f);
            btn.colors = colors;
            btn.interactable = interactable;
            if (onClick != null)
                btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var text = labelGo.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            labelGo.SetActive(iconSprite == null || !string.IsNullOrEmpty(label));

            var labelRt = text.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.pivot = new Vector2(1f, 1f);
            btnRt.anchoredPosition = anchoredTopRight;
            btnRt.sizeDelta = size;

            return btn;
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

        void RefreshBattleGridPrepVisuals()
        {
            BattleGrid.SetPrepPhaseGridVisuals(SandboxCanMutateUnits);
            RefreshDuelSceneBackground();
            RefreshAlliesBagVisibility();
            if (!SandboxCanMutateUnits)
                ClosePrepUnitStatInspect();
        }

        void RefreshDuelSceneBackground()
        {
            if (BattleEnded)
            {
                BattleGrid.SetBattleBackgroundVisible(false);
                return;
            }

            var sprite = CombatStarted
                ? BattleUiSprites.GetBackgroundBattleForLevel(currentLevel)
                : BattleUiSprites.GetBackgroundPrep();

            if (sprite == null)
            {
                BattleGrid.SetBattleBackgroundVisible(false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[DuelDirector] Không load được nền duel — chạy Tools/sync_background_from_root.sh");
#endif
                return;
            }

            BattleGrid.SetBattleBackgroundVisible(true);
            if (CombatStarted)
                BattleGrid.ApplyBattleBackgroundForLevel(currentLevel, 1f);
            else
                BattleGrid.ApplyPrepBackground(0.96f);
        }

        void OnToggleAlliesBagClicked()
        {
            _alliesBagExpanded = !_alliesBagExpanded;
            RefreshAlliesBagVisibility();

            if (!SandboxCanMutateUnits)
                return;

            SetBanner(_alliesBagExpanded
                ? "Túi allies đã mở — chạm ally dự bị để đưa lên ô trống."
                : "Túi allies đã đóng. Sắp xếp đội hình rồi bấm Bắt đầu.");
        }

        void RefreshAlliesBagVisibility()
        {
            var prep = SandboxCanMutateUnits;
            var showGrid = prep && _alliesBagExpanded;
            BattleGrid.SetInventorySectionVisible(showGrid);
            _allyBench?.ApplyBagVisibility(prep, _alliesBagExpanded);
            if (_inventoryPickersRoot != null)
                _inventoryPickersRoot.gameObject.SetActive(showGrid);
            if (_alliesBagToggleButton != null)
            {
                var n = _allyBench != null ? _allyBench.OccupiedCount : 0;
                var label = _alliesBagToggleButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = _alliesBagExpanded
                        ? $"Túi {n}/{AllyBenchInventory.SlotCount} ▼"
                        : $"Túi {n}/{AllyBenchInventory.SlotCount} ▶";
                }
            }
        }

        void CancelCombatEngageRoutine()
        {
            if (_combatEngageRoutine == null)
                return;
            StopCoroutine(_combatEngageRoutine);
            _combatEngageRoutine = null;
        }

        IEnumerator EngageCombatAfterDelayRoutine()
        {
            yield return new WaitForSeconds(CombatEngageDelaySeconds);
            CombatEngaged = true;
            _combatEngageRoutine = null;
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

        void OnHomeButtonClicked()
        {
            ClosePauseAndRestoreTimeScale();
            CloseSettingsStubPanel();
            CloseRewardOverlay();
            if (!Application.CanStreamedLevelBeLoaded(homepageSceneName))
            {
                Debug.LogWarning("[DuelDirector] Không load được scene Homepage: " + homepageSceneName);
                return;
            }

            SceneManager.LoadScene(homepageSceneName);
        }

        void BuildRollGachaUi(Transform canvasParent, Font font)
        {
            _rollUiRoot = new GameObject("RollGachaOverlay");
            _rollUiRoot.transform.SetParent(canvasParent, false);
            var rootRt = _rollUiRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(
                0f,
                MetaHudTheme.BottomActionBarHeight + MetaHudTheme.InfoStripHeight + 32f);
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
                HudUiSprites.ApplyIcon(img, HudUiSprites.MetaNotSelected, RollSlotAllyBg);
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
            RefreshSpeed2xButton();
            if (_prepCoinText != null)
            {
                _prepCoinText.gameObject.SetActive(prep);
                if (prep)
                {
                    var rollsLeft = RollWallet.Balance / SixSlotRollResolver.RollCostCoins;
                    _prepCoinText.text = rollsLeft > 0
                        ? $"Roll coin: {RollWallet.Balance}  |  lượt roll còn: {rollsLeft}"
                        : $"Đội hình: {CountLivingAllies()} ally  |  Enemy: {CountLivingEnemies()}";
                }
            }

            if (_objectiveText != null)
            {
                _objectiveText.gameObject.SetActive(prep);
                if (prep)
                    _objectiveText.text = PrepObjectiveText();
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

        string PrepObjectiveText()
        {
            if (_rollBusy)
                return "Đang roll: nhận ally/buff rồi sắp xếp đội hình.";
            if (RollWallet.Balance >= SixSlotRollResolver.RollCostCoins)
                return "Bước 1: roll hết coin để nhận thêm ally/buff.";
            if (CountLivingAllies() == 0)
                return "Cần ít nhất 1 ally sống để bắt đầu.";
            if (CountLivingEnemies() == 0)
                return "Không có enemy trong wave này.";
            if (_allyBench != null && _allyBench.OccupiedCount > 0)
                return "Bước 2: kéo/sắp ally, mở Túi để đưa quân dự bị lên bàn, rồi Bắt đầu.";
            return "Bước 2: kéo/sắp ally trên lưới, chạm unit để xem/chọn, rồi Bắt đầu.";
        }

        void ApplyPrimaryButtonVisual(Color bg)
        {
            if (_startCombatButton == null)
                return;
            var img = _startCombatButton.GetComponent<Image>();
            if (img != null)
            {
                var rollSprite = BattleUiSprites.RollButton;
                if (rollSprite != null && _startCombatButtonLabel != null &&
                    _startCombatButtonLabel.text.StartsWith("Roll", System.StringComparison.Ordinal))
                {
                    HudUiSprites.ApplyIcon(img, rollSprite, bg);
                }
                else
                {
                    img.sprite = CreateWhiteSprite();
                    img.color = bg;
                }
            }
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
            if (RollWallet.Balance < SixSlotRollResolver.RollCostCoins)
                SetBanner("Roll xong. Sắp xếp đội hình rồi bấm Bắt đầu.");
            else
                SetBanner("Nhận thưởng roll. Tiếp tục roll lượt còn lại.");
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
                    HudUiSprites.ApplyIcon(_rollSlotBacks[i], HudUiSprites.MetaNotSelected, new Color(0.18f, 0.22f, 0.32f, 0.95f));
            }
        }

        void ApplyRollSlotVisual(int index, RollCellKind kind)
        {
            if (index < 0 || index >= 6 || _rollSlotTexts[index] == null)
                return;
            _rollSlotTexts[index].text = RollCellLabel(kind);
            _rollSlotTexts[index].color = RollCellKindToTextColor(kind);
            if (_rollSlotBacks[index] != null)
            {
                var slotSprite = kind == RollCellKind.Ally ? HudUiSprites.MetaSelected : HudUiSprites.MetaNotSelected;
                HudUiSprites.ApplyIcon(_rollSlotBacks[index], slotSprite, RollCellKindToSlotBg(kind));
            }
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

        static Button CreateBottomTextButton(
            Transform canvasParent,
            Font font,
            string label,
            float yFromBottom,
            Color bg,
            Sprite iconSprite = null)
        {
            var btnGo = new GameObject(label + "Button");
            btnGo.transform.SetParent(canvasParent, false);

            var image = btnGo.AddComponent<Image>();
            HudUiSprites.ApplyIcon(image, iconSprite, bg);
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
            labelGo.SetActive(iconSprite == null || !string.IsNullOrEmpty(label));

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
            btnRt.sizeDelta = iconSprite != null ? new Vector2(480f, 96f) : new Vector2(420f, 88f);

            return btn;
        }

        void RefreshPauseTopIcon(bool paused)
        {
            if (_pauseTopButton == null)
                return;
            var image = _pauseTopButton.GetComponent<Image>();
            var sprite = paused ? HudUiSprites.CombatContinue : HudUiSprites.CombatPause;
            HudUiSprites.ApplyIcon(image, sprite, MetaHudTheme.ButtonIconBar);
        }

        static void CreateSettingsStubIconRow(Transform parent, Vector2 anchoredPos, Sprite sprite)
        {
            var go = new GameObject(sprite != null ? sprite.name : "SettingsIcon");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            HudUiSprites.ApplyIcon(img, sprite, MetaHudTheme.ButtonIconBar);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(72f, 72f);
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
            // Keep triangle stack, but center the leader closer to the cell center line.
            var halfBase = c * 0.145f;
            var rise = c * 0.015f;
            var drop = c * 0.07f;
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

            var center = BattleGrid.GetAllyUnitAnchor(row, col);
            var pos = center + StackVisualOffset(before);
            var tint = RarityPalette.UnitTint(tier, def.TeamKind);
            var go = CreateCombatUnitVisual(def.DisplayName, pos, tint, def, def.BodyShape, true, out var allyUsedFallbackSprite);
            if (allyUsedFallbackSprite)
                ApplyAllyDefinitionVisualScale(go.transform, def);
            EnsureBattleVisualDriver(go);
            WarnIfMissingBattleVisualDriver(go, def, "ally");
            var spec = go.GetComponent<AllyInstanceSpec>();
            spec.InitFromDefinition(def, tier, hp, atk, key.LineIndex);
            ConfigureAlly(go, hp, atk, def);
            var rootSr = go.GetComponent<SpriteRenderer>();
            if (rootSr != null)
                rootSr.color = tint;
            ApplyAllyDebugOverlay(go, spec);
            ApplyFootGroundDecorIfEnabled(go.transform, tier);

            SetupAsStackLeader(go);

            RegisterAlly(go);
            if (!_suppressAllyAutoMerge)
                TryAutoMergeAllAllyBoard();
        }

        void TryAutoMergeAllAllyBoard()
        {
            if (!SandboxCanMutateUnits)
                return;

            var safety = 0;
            while (safety++ < 24)
            {
                if (!TryAutoMergeOneAllyGroup())
                    break;
            }
        }

        bool TryAutoMergeOneAllyGroup()
        {
            var groups = new Dictionary<AllyKey, List<CombatHealth>>();
            foreach (var a in _allies)
            {
                if (a == null || a.IsDead)
                    continue;
                if (!TryGetAllyKey(a, out var key))
                    continue;
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<CombatHealth>(4);
                    groups[key] = list;
                }

                list.Add(a);
            }

            foreach (var pair in groups)
            {
                var key = pair.Key;
                var list = pair.Value;
                if (list.Count < AllyMergeRules.AlliesRequiredForAutoMerge)
                    continue;
                if (!AllyMergeRules.CanMerge(key.RarityTier))
                    continue;

                list.Sort(CompareByWorldPosition);
                var victims = list.GetRange(0, AllyMergeRules.AlliesRequiredForAutoMerge);
                BattleGrid.WorldToAllyCell(AllyGridSampleWorld(victims[0]), out var spawnRow, out var spawnCol);

                foreach (var d in victims)
                {
                    if (d == null || d.IsDead)
                        continue;
                    d.Died -= OnUnitDied;
                    _allies.Remove(d);
                    Destroy(d.gameObject);
                }

                var newRarity = AllyMergeRules.NextRarity(key.RarityTier);
                var catalogId = AllyLineCatalog.UnitIdForLine(key.LineIndex, newRarity);
                if (unitCatalog == null || !unitCatalog.TryGet(catalogId, out var pickedDef))
                    return true;

                var (hp, atk) = AllyStatScaling.ScaleStats(pickedDef, newRarity);
                _suppressAllyAutoMerge = true;
                SpawnAllyStackMember(spawnRow, spawnCol, pickedDef, newRarity, hp, atk, key.LineIndex);
                _suppressAllyAutoMerge = false;
                RepackAllyCell(spawnRow, spawnCol);
                SetBanner($"Auto-merge 3 ally thành {newRarity} ở ô {CellLabel(spawnRow, spawnCol)}.");
                return true;
            }

            return false;
        }

        void ApplyFootGroundDecorIfEnabled(Transform unitRoot, Rarity rarity)
        {
            if (!showFootGroundDecor || unitRoot == null)
                return;

            var old = unitRoot.Find("FootGroundDecor");
            if (old != null)
                Destroy(old.gameObject);

            var pad = new GameObject("FootGroundDecor");
            pad.transform.SetParent(unitRoot, false);
            pad.transform.localPosition = new Vector3(0f, -0.03f, 0.008f);
            pad.transform.localRotation = Quaternion.identity;

            var sr = pad.AddComponent<SpriteRenderer>();
            sr.sprite = UnitSpriteFactory.GetSprite(UnitBodyShape.Circle);
            sr.color = RarityPalette.FootGroundRingColor(rarity);
            sr.sortingOrder = -8;

            var ls = unitRoot.lossyScale;
            var sx = Mathf.Max(0.022f, Mathf.Abs(ls.x));
            var sy = Mathf.Max(0.022f, Mathf.Abs(ls.y));
            const float targetWorldW = 0.24f;
            const float targetWorldH = 0.07f;
            pad.transform.localScale = new Vector3(targetWorldW / sx, targetWorldH / sy, 1f);
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
            if (members.Count != AllyMergeRules.MaxEnemyStackPerCell)
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
            if (members.Count != AllyMergeRules.MaxEnemyStackPerCell)
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
            for (var r = 0; r < BattleGrid.Rows; r++)
            {
                for (var c = 0; c < BattleGrid.Cols; c++)
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
            var catalogId = AllyLineCatalog.UnitIdForLine(mergeLine, newRarity);
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
            SetBanner($"Merge thủ công thành {newRarity} ở ô {CellLabel(spawnRow, spawnCol)}.");
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
                    SetBanner("Không thể đổi ô: ô đích đang có stack không hợp lệ.");
                    return false;
                }

                foreach (var tm in targetMembers)
                {
                    if (!TryGetAllyKey(tm, out var tmKey) || tmKey.LineIndex != targetKey.LineIndex || tmKey.RarityTier != targetKey.RarityTier)
                    {
                        SnapStackPositions(members, fromRow, fromCol, revertLeaderWorld);
                        SetBanner("Không thể đổi ô: ô đích có ally khác dòng/phẩm.");
                        return false;
                    }
                }

                targetMembers.Sort(CompareByWorldPosition);
                SnapStackPositions(members, toRow, toCol, null);
                SnapStackPositions(targetMembers, fromRow, fromCol, null);
                RepackAllyCell(fromRow, fromCol);
                RepackAllyCell(toRow, toCol);
                TryAutoMergeAllAllyBoard();
                SetBanner($"Đã đổi vị trí ally {CellLabel(fromRow, fromCol)} ↔ {CellLabel(toRow, toCol)}.");
                return true;
            }

            var existing = CountStackInCellExcludeGroup(toRow, toCol, key.LineIndex, key.RarityTier, members);
            if (existing + n > AllyMergeRules.MaxStackPerCell)
            {
                SnapStackPositions(members, fromRow, fromCol, revertLeaderWorld);
                SetBanner("Ô đích đã đầy — tối đa 3 ally cùng dòng/phẩm.");
                return false;
            }

            SnapStackPositions(members, toRow, toCol, null);
            RepackAllyCell(fromRow, fromCol);
            RepackAllyCell(toRow, toCol);
            TryAutoMergeAllAllyBoard();
            if (toRow != fromRow || toCol != fromCol)
                SetBanner($"Đã chuyển ally sang ô {CellLabel(toRow, toCol)}.");
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
                SetBanner("Không thể kéo enemy vào ô đang có loại khác.");
                return false;
            }

            var existing = CountEnemyStackInCellExcludeGroup(toRow, toCol, ls.CatalogUnitId, ls.RarityTier, members);
            if (existing + members.Count > AllyMergeRules.MaxEnemyStackPerCell)
            {
                SnapEnemyStackPositions(members, fromRow, fromCol, revertLeaderWorld);
                SetBanner("Ô enemy đã đầy.");
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

        static string CellLabel(int row, int col) => $"({row + 1},{col + 1})";

        void SnapStackPositions(IReadOnlyList<CombatHealth> members, int row, int col, Vector3? forcedLeaderWorld)
        {
            if (members == null || members.Count == 0)
                return;

            var center = BattleGrid.GetAllyUnitAnchor(row, col);
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

        static bool IsLine1LegendsMelee(UnitDefinition def) =>
            def != null && def.UnitId == LearningCampaignAllyUnitId;

        GameObject ResolveLine1RigPrefab()
        {
            if (line1BattleRigPrefab != null)
                return line1BattleRigPrefab;
            if (line1BattleRigPrefab == null && !_line1RigPrefabMissingWarned)
            {
                _line1RigPrefabMissingWarned = true;
                Debug.LogWarning("[DuelDirector] Missing line1 rig prefab reference. Assign line1BattleRigPrefab on DuelDirector.");
            }
            return line1BattleRigPrefab;
        }

        GameObject CreateLine1RigFighter(string name, Vector3 pos, Color tint)
        {
            var prefab = ResolveLine1RigPrefab();
            return CreateRigFighter(name, pos, tint, prefab, true);
        }

        GameObject CreateRigFighter(string name, Vector3 pos, Color tint, UnityEngine.Object prefabSource, bool attachAllySpec)
        {
            if (prefabSource == null)
                return null;

            try
            {
                var go = new GameObject(name);
                go.transform.position = pos;
                go.transform.localScale = new Vector3(0.08f, 0.08f, 1f);

                var visualObj = PrefabSourceUtility.InstantiatePrefabRoot(prefabSource);
                if (visualObj == null)
                {
                    Debug.LogWarning("[DuelDirector] Could not instantiate battle prefab root: " + prefabSource);
                    Destroy(go);
                    return null;
                }

                visualObj.transform.SetParent(go.transform, false);
                visualObj.name = "VisualRig";
                visualObj.transform.localPosition = line1BattleRigVisualLocalOffset;
                visualObj.transform.localRotation = Quaternion.identity;
                visualObj.transform.localScale = Vector3.one;

                var allRenderers = visualObj.GetComponentsInChildren<SpriteRenderer>(true);
                for (var i = 0; i < allRenderers.Length; i++)
                    allRenderers[i].color = tint;

                var hasSpine = visualObj.GetComponentInChildren<SkeletonAnimation>(true) != null;
                RemoveNestedBattleVisualDrivers(visualObj.transform);
                if (hasSpine && autoCenterRigVisualByBounds)
                    AlignRigVisualToAnchor(go.transform, visualObj.transform, rigAnchorChildName);
                if (!hasSpine)
                {
                    var meshRenderers = visualObj.GetComponentsInChildren<MeshRenderer>(true);
                    for (var i = 0; i < meshRenderers.Length; i++)
                    {
                        if (meshRenderers[i] == null)
                            continue;
                        var m = meshRenderers[i].material;
                        if (m != null && m.HasProperty("_Color"))
                            m.color = tint;
                    }
                }

                go.AddComponent<CombatHealth>();

                if (hasSpine && go.GetComponent<SpineBattleAnimator>() == null)
                    go.AddComponent<SpineBattleAnimator>();
                var box = go.AddComponent<BoxCollider2D>();
                box.size = attachAllySpec ? allyPrepColliderLocalSize : new Vector2(0.48f, 0.48f);
                if (attachAllySpec)
                    ApplySafeAllyPrepColliderSize(box);
                go.AddComponent<DuelActor>();
                go.AddComponent<DuelUnitGridDrag>();
                go.AddComponent<WorldUnitHealthBar>();
                if (attachAllySpec)
                    go.AddComponent<AllyInstanceSpec>();
                if (debugBattleVisualMetrics)
                    LogVisualMetricsOnSpawn(go.transform, hasSpine ? "spine" : "non-spine");
                return go;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[DuelDirector] Failed to instantiate line1 rig prefab, falling back to static sprite. " + ex);
                return null;
            }
        }

        static void AlignRigVisualToAnchor(Transform hostRoot, Transform visualRoot, string anchorName)
        {
            if (hostRoot == null || visualRoot == null)
                return;

            if (!string.IsNullOrWhiteSpace(anchorName))
            {
                var all = visualRoot.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < all.Length; i++)
                {
                    var t = all[i];
                    if (t == null || t.name != anchorName)
                        continue;
                    // Anchor may be nested; convert the anchor world point to host local to avoid compounded local offsets.
                    var lpAnchor = hostRoot.InverseTransformPoint(t.position);
                    var anchoredLp = visualRoot.localPosition;
                    anchoredLp.x -= lpAnchor.x;
                    anchoredLp.y -= lpAnchor.y;
                    visualRoot.localPosition = anchoredLp;
                    return;
                }
            }

            // Stable Spine fallback: setup-pose bounds (not runtime pose-dependent).
            var skeletonAnimation = visualRoot.GetComponentInChildren<SkeletonAnimation>(true);
            if (skeletonAnimation != null && TryAlignVisualBySpineSetupPose(hostRoot, visualRoot, skeletonAnimation))
                return;

            // Generic fallback: renderer bounds, feet anchor by bounds.min.y.
            var mesh = visualRoot.GetComponentsInChildren<MeshRenderer>(true);
            var has = false;
            var b = new Bounds(visualRoot.position, Vector3.zero);
            for (var i = 0; i < mesh.Length; i++)
            {
                if (mesh[i] == null)
                    continue;
                if (!has)
                {
                    b = mesh[i].bounds;
                    has = true;
                }
                else
                    b.Encapsulate(mesh[i].bounds);
            }

            if (!has)
            {
                var any = visualRoot.GetComponentsInChildren<Renderer>(true);
                for (var i = 0; i < any.Length; i++)
                {
                    if (any[i] == null)
                        continue;
                    if (!has)
                    {
                        b = any[i].bounds;
                        has = true;
                    }
                    else
                        b.Encapsulate(any[i].bounds);
                }
            }

            if (!has)
                return;

            // Foot-anchor placement: center by X, ground by Y (bounds.min.y).
            var centerLocal = hostRoot.InverseTransformPoint(b.center);
            var footLocal = hostRoot.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z));
            var boundsLp = visualRoot.localPosition;
            boundsLp.x -= centerLocal.x;
            boundsLp.y -= footLocal.y;
            visualRoot.localPosition = boundsLp;
        }

        static bool TryAlignVisualBySpineSetupPose(Transform hostRoot, Transform visualRoot, SkeletonAnimation skeletonAnimation)
        {
            if (hostRoot == null || visualRoot == null || skeletonAnimation == null)
                return false;

            var skeleton = skeletonAnimation.Skeleton;
            if (skeleton == null)
                return false;

            float[] tempVertices = null;
            skeleton.SetToSetupPose();
            skeleton.UpdateWorldTransform();
            skeleton.GetBounds(out var offsetX, out var offsetY, out var sizeX, out var sizeY, ref tempVertices);
            if (sizeX <= 0.0001f || sizeY <= 0.0001f)
                return false;

            var skTransform = skeletonAnimation.transform;
            var centerWorld = skTransform.TransformPoint(new Vector3(offsetX + sizeX * 0.5f, offsetY + sizeY * 0.5f, 0f));
            var footWorld = skTransform.TransformPoint(new Vector3(offsetX + sizeX * 0.5f, offsetY, 0f));
            var centerLocal = hostRoot.InverseTransformPoint(centerWorld);
            var footLocal = hostRoot.InverseTransformPoint(footWorld);

            var lp = visualRoot.localPosition;
            lp.x -= centerLocal.x;
            lp.y -= footLocal.y;
            visualRoot.localPosition = lp;
            return true;
        }

        static void RemoveNestedBattleVisualDrivers(Transform visualRoot)
        {
            if (visualRoot == null)
                return;

            var spineDrivers = visualRoot.GetComponentsInChildren<SpineBattleAnimator>(true);
            for (var i = 0; i < spineDrivers.Length; i++)
            {
                if (spineDrivers[i] == null)
                    continue;
                spineDrivers[i].enabled = false;
                Destroy(spineDrivers[i]);
            }

            var spriteDrivers = visualRoot.GetComponentsInChildren<Line1BattleSpriteAnimator>(true);
            for (var i = 0; i < spriteDrivers.Length; i++)
            {
                if (spriteDrivers[i] == null)
                    continue;
                spriteDrivers[i].enabled = false;
                Destroy(spriteDrivers[i]);
            }
        }

        void LogVisualMetricsOnSpawn(Transform unitRoot, string kind)
        {
            if (unitRoot == null)
                return;
            var p = unitRoot.position;
            var faction = unitRoot.GetComponent<CombatHealth>()?.Faction.ToString() ?? "Unknown";
            if (faction == "Ally")
            {
                BattleGrid.WorldToAllyCell(p, out var row, out var col);
                var center = BattleGrid.GetAllyCellCenter(row, col);
                Debug.Log($"[DuelMetrics] {unitRoot.name} kind={kind} faction={faction} row={row} col={col} pos={p} center={center} delta={p - center}");
            }
            else
            {
                BattleGrid.WorldToEnemyCell(p, out var row, out var col);
                var center = BattleGrid.GetEnemyCellCenter(row, col);
                Debug.Log($"[DuelMetrics] {unitRoot.name} kind={kind} faction={faction} row={row} col={col} pos={p} center={center} delta={p - center}");
            }
        }

        static bool HasBattlePrefabBinding(UnitDefinition def)
        {
            if (def == null)
                return false;
            if (!string.IsNullOrWhiteSpace(def.BattlePrefabResourcesPath))
                return true;
            return def.BattlePrefab != null;
        }

        /// <summary>
        /// Chỉ Legends melee fallback sprite: nếu unit đã có BattlePrefab (Spine/world) thì KHÔNG gắn portrait lên root.
        /// </summary>
        static bool ShouldApplyAllyPortraitOnBattleGrid(GameObject go, UnitDefinition def)
        {
            if (go == null || def == null)
                return false;
            if (HasBattlePrefabBinding(def))
                return false;
            return IsLine1LegendsMelee(def);
        }

        static void ApplyAllyPortraitSpriteForGrid(GameObject go, UnitDefinition def)
        {
            if (!ShouldApplyAllyPortraitOnBattleGrid(go, def))
                return;
            var sp = def.PortraitSprite != null ? def.PortraitSprite : def.CardSprite;
            if (sp == null)
                return;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = Mathf.Max(sr.sortingOrder, 8);
            if (IsLine1LegendsMelee(def))
                go.transform.localScale = new Vector3(0.32f, 0.32f, 1f);
        }

        static UnityEngine.Object TryResolveBattlePrefabSource(UnitDefinition def, string ownerTag)
        {
            if (def == null)
                return null;
            if (!string.IsNullOrWhiteSpace(def.BattlePrefabResourcesPath))
            {
                var rp = def.BattlePrefabResourcesPath.Trim();
                var fromResources = Resources.Load<GameObject>(rp);
                if (fromResources != null)
                    return fromResources;
                Debug.LogWarning("[DuelDirector] Resources.Load<GameObject> failed for " + ownerTag + " path: " + rp);
            }

            return def.BattlePrefab;
        }

        internal GameObject CreateBenchAllyVisual(
            UnitDefinition def,
            Rarity tier,
            float hp,
            float atk,
            int lineIndex,
            Vector3 pos)
        {
            var tint = RarityPalette.UnitTint(tier, def.TeamKind);
            var go = CreateCombatUnitVisual(
                def.DisplayName,
                pos,
                tint,
                def,
                def.BodyShape,
                true,
                out var usedFallback);
            if (usedFallback)
                ApplyAllyDefinitionVisualScale(go.transform, def);

            const float benchScale = 0.05f;
            go.transform.localScale = new Vector3(benchScale, benchScale, 1f);

            var spec = go.GetComponent<AllyInstanceSpec>();
            spec.InitFromDefinition(def, tier, hp, atk, lineIndex);
            ConfigureAlly(go, hp, atk, def);
            var rootSr = go.GetComponent<SpriteRenderer>();
            if (rootSr != null)
                rootSr.color = tint;
            ApplyFootGroundDecorIfEnabled(go.transform, tier);
            go.AddComponent<BenchAllyMarker>();

            var actor = go.GetComponent<DuelActor>();
            if (actor != null)
                actor.enabled = false;
            var drag = go.GetComponent<DuelUnitGridDrag>();
            if (drag != null)
                drag.enabled = false;

            EnsurePrepUnitInfoButton(go);
            return go;
        }

        GameObject CreateCombatUnitVisual(
            string displayName,
            Vector3 pos,
            Color tint,
            UnitDefinition def,
            UnitBodyShape fallbackShape,
            bool attachAllySpec,
            out bool usedFallbackSprite,
            int waveForEnemySprite = 1,
            int spawnIndexForEnemySprite = 0)
        {
            var prefabSource = TryResolveBattlePrefabSource(def, attachAllySpec ? "ally" : "enemy");
            GameObject go = null;
            if (prefabSource != null)
                go = CreateRigFighter(displayName, pos, tint, prefabSource, attachAllySpec);
            usedFallbackSprite = go == null;
            if (usedFallbackSprite)
            {
                go = CreateFighter(displayName, pos, tint, fallbackShape, attachAllySpec);
                if (!attachAllySpec)
                    usedFallbackSprite = !ApplyAnimalKitEnemySprite(go, def, waveForEnemySprite, spawnIndexForEnemySprite);
            }

            if (attachAllySpec)
            {
                ApplyAllyPortraitSpriteForGrid(go, def);
            }

            return go;
        }

        static bool ApplyAnimalKitEnemySprite(GameObject go, UnitDefinition def, int wave, int spawnIndex)
        {
            if (go == null)
                return false;

            var sprite = AnimalKitEnemySprites.Pick(def, wave, spawnIndex);
            if (sprite == null)
                return false;

            var rootSr = go.GetComponent<SpriteRenderer>();
            if (rootSr != null)
                rootSr.sprite = null;

            var visualGo = new GameObject("AnimalKitSprite");
            visualGo.transform.SetParent(go.transform, false);
            visualGo.transform.localPosition = Vector3.zero;
            visualGo.transform.localRotation = Quaternion.identity;
            visualGo.transform.localScale = Vector3.one;

            var sr = visualGo.AddComponent<SpriteRenderer>();

            if (go.GetComponent<AnimalKitEnemyVisualMarker>() == null)
                go.AddComponent<AnimalKitEnemyVisualMarker>();

            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = Mathf.Max(sr.sortingOrder, 7);

            var bounds = sprite.bounds.size;
            var maxAxis = Mathf.Max(bounds.x, bounds.y, 0.01f);
            var targetWorldHeight = wave >= 5 && spawnIndex == 0 ? 0.56f : 0.42f;
            var scale = targetWorldHeight / maxAxis;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var collider = go.GetComponent<BoxCollider2D>();
            if (collider != null)
                collider.size = new Vector2(0.42f / Mathf.Max(scale, 0.01f), 0.42f / Mathf.Max(scale, 0.01f));

            if (go.GetComponent<AnimalKitEnemyBattleAnimator>() == null)
                go.AddComponent<AnimalKitEnemyBattleAnimator>();

            return true;
        }

        void EnsureBattleVisualDriver(GameObject go)
        {
            if (go == null)
                return;

            if (go.GetComponentInChildren<SkeletonAnimation>(true) != null)
            {
                if (go.GetComponent<SpineBattleAnimator>() == null)
                    go.AddComponent<SpineBattleAnimator>();
                return;
            }

            if (go.GetComponent<AnimalKitEnemyVisualMarker>() != null)
            {
                if (go.GetComponent<AnimalKitEnemyBattleAnimator>() == null)
                    go.AddComponent<AnimalKitEnemyBattleAnimator>();
                return;
            }

            var animator = go.GetComponent<Animator>();
            if (animator == null)
                animator = go.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = go.AddComponent<Animator>();

            var controller = Resources.Load<RuntimeAnimatorController>(Line1BattleControllerResourcesPath);
            if (controller != null)
            {
                if (animator.runtimeAnimatorController != controller)
                    animator.runtimeAnimatorController = controller;
            }
            else if (!_line1BattleControllerMissingWarned)
            {
                _line1BattleControllerMissingWarned = true;
                Debug.LogWarning("[DuelDirector] Missing line1 battle animator controller at Resources/" + Line1BattleControllerResourcesPath);
            }

            var bridge = go.GetComponent<Line1BattleSpriteAnimator>();
            if (bridge == null)
                bridge = go.AddComponent<Line1BattleSpriteAnimator>();
            bridge.Configure(animator);
        }

        static void WarnIfMissingBattleVisualDriver(GameObject go, UnitDefinition def, string side)
        {
            if (go == null)
                return;
            var driver = go.GetComponent<IBattleVisualDriver>() ?? go.GetComponentInChildren<IBattleVisualDriver>(true);
            if (driver != null)
                return;
            var unitId = def != null ? def.UnitId : "null";
            Debug.LogWarning("[DuelDirector] Missing IBattleVisualDriver after spawn. side=" + side + " unitId=" + unitId + " name=" + go.name);
        }

        /// <summary>Melee vuông ~0.68; tròn/tam giác 0.135.</summary>
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
                const float meleeScale = 0.68f;
                t.localScale = new Vector3(meleeScale, meleeScale, 1f);
            }
        }

        /// <summary>Melee vuông ~0.68; tròn/tam giác 0.135.</summary>
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
                const float meleeScale = 0.68f;
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
