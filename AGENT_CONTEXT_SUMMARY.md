# SpinSquadClone — bản tóm tắt cho agent (ngắn)

**Mục đích:** Unity clone/học theo *concept* game tham chiếu Spin Squad! (IP thương mại khác repo). UI/tên không copy game gốc.

**Unity:** `6000.4.1f1` | **Gói chính:** Feature 2D, Input System, UGUI (+ Timeline, Visual Scripting, Test Framework trong `Packages/manifest.json`). Spine runtime trong `Assets/Spine/`.

---

## Luồng & entry

| Vùng | Vai trò |
|------|--------|
| `Scripts/Core/DuelDirector.cs` | Điều phối prep/combat, spawn, kéo stack, merge, roll UI |
| `Scripts/Gacha/*` | Máy 6 ô (`SixSlotRollGenerator`, `SixSlotRollResolver`), ví roll, payout, buff run (`BattleRunBuffs`) |
| `Scripts/Meta/*` | Vàng, key treasure, unlock level, upgrade 12 slot, treasure passive — `PlayerPrefs` key `SpinSquad_MetaProgression_v1`; roll trong trận — `SpinSquad_RollCoins` (`RollWallet`) |
| `Scripts/Scenes/*` | `HomepageHub`, Upgrade/Treasure + Detail, `MetaUiSelectionContext` |
| `Scripts/Data/*` | `UnitDefinition`, `UnitCatalog`, `AllyLineCatalog`, rarity, `UnitBodyShape` (triangle → ranged) |

**Scenes (`Assets/SpinSquad/Scenes/`):** `SampleScene`, `DuelTestArena`, **`KnightSpineAnimTest`** (UI thử clip Spine knight), `Homepage`, `Upgrade`, `UpgradeDetail`, `Treasure`, `TreasureDetail`.

**Spine knight (duy nhất):** JSON/atlas/png với các asset nhập Unity trong [`Assets/SpinSquad/Data/Spine/Knight/`](Assets/SpinSquad/Data/Spine/Knight/), prefab chiến đấu [`Resources/Battle/KnightBattleVisual`](Assets/SpinSquad/Resources/Battle/KnightBattleVisual.prefab) — Menu **SpinSquad → Spine → Generate Knight Battle Visual Prefab** để tái sinh prefab sau khi đổi skeleton.

**Demo combat:** `SampleScene` / `DuelTestArena` — Unity Play chạy **scene đang mở**; xem [`Assets/SpinSquad/README.md`](Assets/SpinSquad/README.md). Menu Editor **SpinSquad** có shortcut (Open Sample Scene, Homepage, v.v.).

---

## DuelDirector — neo đọc nhanh (line numbers trong repo hiện tại)

File: [`Assets/SpinSquad/Scripts/Core/DuelDirector.cs`](Assets/SpinSquad/Scripts/Core/DuelDirector.cs) (~3569 dòng).

| Vùng | Dòng ~ | Method / chủ đề |
|------|--------|----------------|
| Lifecycle | `515` | `Start()` |
| Restart / combat gate | `614`, `729` | `BeginCombat()`, `RestartDuel()` |
| Spawn + reset ví/buff session | `807` | `SpawnAndRegisterFighters()` |
| Roll → spawn ally chờ queue | `881` | `DrainPendingRollAllyGrants()` |
| Ally stat + buff replay | `926`, `2395` | `ConfigureAlly()`, `ReapplyAllLivingAlliesFromBuffs()` |
| Prep roll UI coroutine | `2147`, `2292` | `BuildRollGachaUi()`, `RollGachaRoutine()` |
| Merge / combine ô | `2924`, `2960`, `3017`, `3026` | `TryMergeAt`, `TryMergeEnemyAt`, `TryCombineAllyAt`, `TryCombineEnemyAt` |
| Wave win/loss meta | `1326`, `1382`, `1392` | `WaveCompleteAfterDelayRoutine()`, loss/win handlers |

Đọc tiếp: grep `^\s+(void|IEnumerator|public void)` trong file để lấy full index cục bộ sau khi sửa — bảng trên chỉ neo chính.

---

## Inventory C# trong `Assets/SpinSquad` (58 files)

| Folder | Files |
|--------|------|
| `Scripts/Core/` | `DuelDirector`, `DuelActor`, `CombatHealth`, `BattleGrid`, `AllyMergeRules`, `AllyStatScaling`, `AllyInstanceSpec`, `EnemyInstanceSpec`, `AllyStackFollower`, `AllyCellPicker`, `DuelUnitGridDrag`, `PrepGridPointer`, `WorldMergeAffordance`, `UnitSpriteFactory`, `PrefabSourceUtility`, `WorldUnitHealthBar`, `FloatingDamagePopup`, `RangedShotVfx`, `UnitDeathSequence`, `Line1BattleSpriteAnimator`, `SpineBattleAnimator`, `IBattleVisualDriver` |
| `Scripts/Data/` | `UnitDefinition`, `UnitCatalog`, `AllyLineCatalog`, `Rarity`, `UnitTeamKind`, `UnitBodyShape`, `RarityPalette` |
| `Scripts/Gacha/` | `SixSlotRollGenerator`, `SixSlotRollResolver`, `RollCellKind`, `RollPayout`, `RollGachaSession`, `RollWallet`, `PendingRollGrants`, `AllyRollGrantBalance`, `BattleRunBuffs`, `BuffStatKind` |
| `Scripts/Meta/` | `MetaProgressionStore`, `MetaSaveData`, `TreasureDefinitions` |
| `Scripts/Scenes/` | `HomepageHub`, `UpgradeSceneController`, `UpgradeDetailSceneController`, `TreasureSceneController`, `TreasureDetailSceneController`, `MetaUiSelectionContext`, `MetaHudTheme`, `DuelTestArenaPanel` |
| `Scripts/UI/` | `AllyUiAnimatorBridge` |
| `Scripts/Editor/` | `UnitCatalogSampleGenerator`, `UnitCatalogVerifier`, `OpenSampleSceneMenu`, `GameViewIPhoneXOnPlay`, `HomepageSceneBuilder`, `PlayModeStartHomepage` |
| `Editor/` (root Assets/SpinSquad) | `SpinSquadSpineBattlePrefabBuilder` |

---

## Quy tắc gameplay đã doc (ground truth cho design)

- **Roll 6 ô:** [`ROLL_MACHINE_SPEC.md`](ROLL_MACHINE_SPEC.md) — implementation: `SixSlotRollGenerator`, `SixSlotRollResolver`, `RollGachaSession`.
- **Merge ally / stack:** [`MERGE_ALLY_SPEC.md`](MERGE_ALLY_SPEC.md); số chỉ và path runtime: [`Assets/SpinSquad/Docs/ALLY_STATS.md`](Assets/SpinSquad/Docs/ALLY_STATS.md) (`TryMergeAt`, `AllyStatScaling`, `AllyMergeRules.MaxStackPerCell`).
- **Campaign & meta:** [`Assets/SpinSquad/Docs/PROGRESSION_SPEC.md`](Assets/SpinSquad/Docs/PROGRESSION_SPEC.md).
- **Enemy wave:** [`ENEMY_STATS.md`](Assets/SpinSquad/Docs/ENEMY_STATS.md).
- **Bản đồ luồng:** [`Assets/SpinSquad/Docs/AI_CONTEXT/SYSTEM_MAP.md`](Assets/SpinSquad/Docs/AI_CONTEXT/SYSTEM_MAP.md).
- **Roadmap:** [`SPINSQUAD_LOGIC_ROADMAP.md`](SPINSQUAD_LOGIC_ROADMAP.md).
- **UI Legends / PNG rig:** `LEGENDS_MELEE_UI_FLOW.md`, `UNITY_ANIMATION_SETUP_LEGENDS_MELEE.md`.
- **Battle anim prompt (tile):** `UNITY_AI_ALLY_ANIMATION_PROMPT.md`.

---

## Doc drift đã biết (ưu tiên code)

| Nguồn | Vấn đề |
|-------|--------|
| [`SPINSQUAD_TONG_HOP.md`](SPINSQUAD_TONG_HOP.md) | Được cập nhật lại §2 để khớp repo hiện tại — trước đây §2–3 mô tả project không có Scripts/gacha (**lệch**). |
| Merge constants | [`MERGE_ALLY_SPEC`](MERGE_ALLY_SPEC.md) ghi multiplier cụ thể; [`AllyMergeRules`](Assets/SpinSquad/Scripts/Core/AllyMergeRules.cs) giữ multipliers khác nhau cho enemy vs ally (comment “ally spawn mới” dùng `AllyStatScaling`) — khi balance, đối chiếu **`ALLY_STATS` + code `TryMergeAt`**. |
| Buff phạm vi wave | [`ROLL_MACHINE_SPEC`](ROLL_MACHINE_SPEC.md) nói buff từ wave roll đến hết level; [`BattleRunBuffs`](Assets/SpinSquad/Scripts/Gacha/BattleRunBuffs.cs) là **cumulative run-level** và clear khi `SpawnAndRegisterFighters` — cùng hướng “sau wave 1 onward” trong session. |

Luôn **`AllyLineCatalog`** là nguồn map `(line + rarity) → unitId` cho grant/merge; legacy id trong catalog vẫn resolve qua [`LineIndexFromUnitId`](Assets/SpinSquad/Scripts/Data/AllyLineCatalog.cs).

---

## Cập nhật file này khi

Thêm scene, đổi save key (`MetaProgressionStore` / `RollWallet`), đổi luật roll/merge hoặc thêm milestone lớn — ghi một dòng và cập nhật bảng drift nếu cần.
