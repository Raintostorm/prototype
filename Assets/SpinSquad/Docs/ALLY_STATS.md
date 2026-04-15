# Ally stats (SpinSquad demo)

Tài liệu tham chiếu nhanh: **catalog** (`UnitDefinition` ScriptableObject) + **fallback** khi không có def (`DuelDirector`).

Nguồn dữ liệu trong repo:

| Nguồn | Đường dẫn |
|--------|------------|
| Định nghĩa unit | `Assets/SpinSquad/Data/Units/*.asset` |
| Catalog index | `Assets/SpinSquad/Resources/UnitCatalog_Main.asset` |
| Fallback HP/ATK scene | `DuelDirector` — `allyMaxHp`, `allyStrikeDamage` (Inspector) |
| Merge 3→1 ally | `DuelDirector.TryMergeAt` — xóa cả 3, tăng bậc `NextRarity`, random `line` trong `{0,1,2}` rồi map `line -> UnitId` qua `AllyLineCatalog`; HP/ATK dùng `AllyStatScaling.ScaleStats(def, tier)`; ưu tiên spawn vào ô đầu tiên (row→col) đã có stack cùng `(line, rarity)` chưa đầy, không thì ô vừa merge. `MaxStackPerCell` = 3 từ `AllyMergeRules` (enemy merge vẫn dùng `HpMultiplier` / `AttackMultiplier` riêng) |
| Chọn ô thêm ally / roll | `TryPickRandomAllyCellForAdd` — ưu tiên stack chưa đầy, **ô đầu tiên** theo thứ tự `(row, col)` (không random) |
| Hết wave (delay) | `DuelDirector.waveCompleteDelaySeconds` — scene demo ghi **2** giây |
| Crit buff | `GetOutgoingDamageForAlly` — `CritChance` clamp 0–1, crit **×2** damage lên enemy |
| Buff run (wave ≥ ActiveFromWave) | `BattleRunBuffs` — cộng dồn `%HP`, `%DMG`, `%AtkSpeed`, `CritChance`; `ConfigureAlly` nhân HP/DMG và scale interval ranged/melee theo AtkSpeed |

---

## Default scene (không đổi catalog trên Inspector)

Trên `DuelDirector`, mặc định code gợi ý:

- `allyUnitId` = `unit_slip_slinger` (có thể đổi trong Inspector)
- Fallback khi spawn **không** dùng def: **HP** = `allyMaxHp` (**100**), **Attack** = `allyStrikeDamage` (**10**) — xem field `[SerializeField]` trong `DuelDirector.cs`

---

## Bảng unit Ally trong catalog (`TeamKind = Ally`)

| `unitId` | Display name | Rarity | Max HP | Attack | Body | Ranged |
|----------|----------------|--------|--------|--------|------|--------|
| `unit_slip_slinger` | Slip Slinger | Common | 100 | 18 | Square (melee vuông) | Không |
| `unit_brush_warden` | Brush Warden | Rare | 120 | 20 | Circle | Không |
| `unit_ally_ranged` | Pinshot Ranger | Rare | 110 | 18 | Triangle | Có — range **2.1**, shot interval **0.48** s |

Ghi chú:

- **Rarity** trong asset: Common = 0, Rare = 1, Epic = 2, …
- **Triangle** = ranged; **Square**/**Circle** = melee (quy ước demo). `OnValidate` ép tam giác bật ranged.
- Scale world trên lưới (`DuelDirector.ApplyAllyDefinitionVisualScale`): melee **0.55**, ranged tam giác **0.135**.
- Chỉ số catalog ally (Common / Rare spawn) set **cao hơn** enemy melee Moss (88/15) và ranged Spore (52/6) ở bậc catalog tương ứng; bậc runtime cao hơn rarity trên def nhân **`AllyStatScaling.TierStepMultiplier` (3.5)** mỗi bậc chênh (`Pow(3.5, tier − def.Rarity)`).
- Sau **merge** ally: **một** con mới bậc cao hơn một bậc (tới trước Legendary theo `AllyMergeRules`), `UnitId` random trong bộ line chuẩn `{0,1,2}` (chung với roll) rồi map qua `AllyLineCatalog`; HP/ATK theo `AllyStatScaling` tại bậc đó. Một ô chỉ chứa stack cùng `(line, rarity)`, tối đa **3** / ô.
- Kéo stack ally trong prep: ô trống = move; ô cùng `(line, rarity)` = stack nếu chưa đầy; ô khác key = **swap** hai stack.

---

## Runtime (`AllyInstanceSpec`)

Mỗi instance lưu `CombatMaxHitPoints`, `CombatAttack`, `CatalogUnitId`, `AllyLineIndex`, `RarityTier` — sync với merge và roll grant.

Cập nhật file này khi thêm `.asset` ally mới hoặc đổi default `DuelDirector`.
