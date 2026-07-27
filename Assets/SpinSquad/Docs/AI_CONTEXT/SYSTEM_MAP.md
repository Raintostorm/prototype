# SpinSquad AI Context Map

Mục tiêu file này: giúp AI/đồng đội đọc nhanh logic cốt lõi trước khi sửa code.

**HUD sprites (registry kỹ thuật):** [`UI_HUD_ASSET_MAP.md`](UI_HUD_ASSET_MAP.md) — 68 PNG dưới `Resources/UI/Hud/`, wiring runtime, mục GAP.

## 1) Điểm vào chính

- `Assets/SpinSquad/Scripts/Core/DuelDirector.cs`
  - Điều phối prep/combat, spawn ally/enemy, drag stack, merge/combine, roll UI.
- `Assets/SpinSquad/Scripts/Core/AllyInstanceSpec.cs`
  - Runtime state của ally (bao gồm `AllyLineIndex`, `RarityTier`, combat stats).
- `Assets/SpinSquad/Scripts/Gacha/*`
  - Roll wallet, roll resolver, grant queue, payout.
- `Assets/SpinSquad/Scripts/Data/AllyLineCatalog.cs`
  - Map chuẩn line `0..4` <-> `UnitId` (5 dòng ngũ hành starter + Knight).
- `Assets/SpinSquad/Scripts/Meta/*`
  - Meta save persistent (`Gold`, `TreasureKey`, unlocked level, line upgrades, treasures).
- `Assets/SpinSquad/Scripts/Scenes/HomepageHub.cs`
  - Trang Home runtime UI: Play / Upgrade / Treasure + chọn level.
- `Assets/SpinSquad/Scripts/Scenes/UpgradeSceneController.cs`
  - UI card list cho 5 line ally, click card để vào scene chi tiết.
- `Assets/SpinSquad/Scripts/Scenes/UpgradeDetailSceneController.cs`
  - Scene chi tiết upgrade line (status/cost/chỉ số hiện tại & kế tiếp), nâng cấp trực tiếp.
- `Assets/SpinSquad/Scripts/Scenes/TreasureSceneController.cs`
  - UI card list treasure definitions (owned + not owned), roll bằng key.
- `Assets/SpinSquad/Scripts/Scenes/TreasureDetailSceneController.cs`
  - Scene chi tiết treasure: owned status, cấp hiện tại, duplicate cần thêm.
- `Assets/SpinSquad/Scripts/Scenes/MetaUiSelectionContext.cs`
  - Handoff id line/treasure giữa list scene và detail scene.

## 2) Mô hình ally hiện tại (đã rút gọn)

Identity stack của ally dùng:

- `AllyLineIndex` (0..4)
- `RarityTier`

Không gian này được dùng thống nhất cho:

- Add/Spawn ally
- Roll grant ally
- Merge 3->1
- Relocate drag stack

Prep interaction ownership:

- Chọn ô/menu merge theo **cell-first** (`AllyCellPicker` trên grid).
- Unit collider chỉ hỗ trợ drag/va chạm; không còn là nguồn sự thật để chọn ô.

Map line → `unitId` (nguồn sự thật: `AllyLineCatalog.UnitIdForLine` — mọi rarity dùng cùng `UnitDefinition` base; scaling theo `AllyStatScaling`):

| Line | UnitId (element) |
|------|------------------|
| 0 | `ally_moc` (green Spine) |
| 1 | `ally_hoa` (red Spine) |
| 2 | `ally_kim` (white Spine) |
| 3 | `ally_thuy` (blue Spine, ranged) |
| 4 | `common_melee` (Knight) |

Legacy ids (`unit_l0_*`, `unit_l1_*`, `unit_l2_*`, `unit_slip_slinger`, `unit_iron_guard`, `unit_ally_ranged`, `unit_brush_warden`) vẫn map về một line 0–4 qua `LineIndexFromUnitId` để save/stack cũ không vỡ.

## 3) Quy tắc roll và merge

### Roll

- `SixSlotRollResolver` tạo ally grant với:
  - `RarityTier` theo kích thước cụm Ally (3/4/5/6)
  - `AllyLineIndex` random trong `{0,1,2,3,4}`
- `DuelDirector.DrainPendingRollAllyGrants()`:
  - đổi line -> `UnitId` qua `AllyLineCatalog`
  - tìm ô hợp lệ theo `(line, rarity)`
  - spawn ally vào stack.

### Merge 3->1

- Điều kiện merge: đúng 3 ally cùng ô, cùng `(line, rarity)`.
- Kết quả:
  - `newRarity = NextRarity(oldRarity)`
  - `line` random trong `{0,1,2,3,4}` (cùng resolver line như roll)
  - `UnitId` suy từ line qua `AllyLineCatalog`.
- Spawn sau merge:
  - ưu tiên ô đầu tiên (row->col) có partial stack cùng `(line, newRarity)` chưa đầy 3
  - nếu không có thì spawn lại ô vừa merge.

### Drag / Swap

- Kéo vào ô trống: move bình thường.
- Kéo vào ô cùng key: stack vào ô đích (không vượt quá 3).
- Kéo vào ô khác key: **always swap** toàn bộ stack giữa 2 ô.

## 4) Campaign + meta progression

- Campaign: `3 level`, mỗi level `10 wave`.
- Level đang chơi lấy từ `MetaProgressionStore.SelectedLevel`.
- Độ khó enemy tăng nhẹ theo level (HP/ATK multiplier theo level).
- Clear xong wave 10 của level:
  - cộng `Gold` + `TreasureKey`,
  - mở khóa level kế tiếp (tối đa 3),
  - quay về Homepage.

Meta resources:

- `Gold`: dùng trong scene Upgrade (`TryUpgradeLine(line, rarity)`).
- `TreasureKey`: dùng trong scene Treasure (`TrySpendTreasureKey`).
- Treasures là auto-passive persistent, stack theo level thông qua dupe shards.
- Upgrade ally hiện là `20 entry` = `5 line x 4 rarity (Common..Legendary)`.
- Rule rarity cho upgrade:
  - flat buff: mỗi rarity +1 step => `x4`
  - % buff: mỗi rarity +1 step => `+0.2` điểm phần trăm
- Cả upgrade và treasure đều cap `Lv50` với status rõ (`CanUpgrade/NeedGold/MaxLevel`, `NotOwned/NeedDuplicates/CanLevelUp/MaxLevel`).

Run economy trong trận:

- `startingRollCoins = 40` (base) + bonus từ treasure passive.
- `wavePrepRollCoinGrant = 40` (base) + bonus từ treasure passive.

## 5) Chỗ dễ nhầm

- Màu/shape là UI, không tự động là cùng stack nếu data key khác.
- `CatalogUnitId` vẫn có trên `AllyInstanceSpec` để lookup definition/combat config,
  nhưng luật stack ally, drag, merge đều dùng `(AllyLineIndex, RarityTier)`.
- Upgrade theo line là **vĩnh viễn** (PlayerPrefs JSON), không reset khi restart trận.
- Treasure passive apply tự động trong duel runtime (không cần equip thủ công).

## 6) Checklist nhanh trước khi sửa tiếp

1. Xác nhận scene đang chạy (`DuelTestArena` / `SampleScene`).
2. Restart trận để reset wallet.
3. Test các case:
   - Roll ra cùng `(line, rarity)` phải vào cùng stack nếu chưa đầy 3.
   - Merge 3 con cùng stack phải lên rarity và random line trong `{0,1,2,3,4}`.
   - Sau merge, nếu đã có partial stack cùng `(line, rarity mới)` ở ô khác thì phải dồn vào đó.
   - Kéo stack A sang ô có stack B khác key thì A/B phải đổi chỗ cho nhau.
