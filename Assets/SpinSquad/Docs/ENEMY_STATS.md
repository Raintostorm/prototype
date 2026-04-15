# Enemy stats (SpinSquad demo)

Tài liệu tham chiếu nhanh: **catalog** (`UnitDefinition`, `TeamKind = Enemy`) + **fallback** scene (`DuelDirector`).

| Nguồn | Đường dẫn |
|--------|------------|
| Định nghĩa unit | `Assets/SpinSquad/Data/Units/*.asset` |
| Catalog | `Assets/SpinSquad/Resources/UnitCatalog_Main.asset` |
| Fallback HP/ATK | `DuelDirector` — `enemyMaxHp`, `enemyStrikeDamage` (Inspector) |
| Merge enemy | Cùng pattern stack / rarity với `EnemyInstanceSpec` + `AllyMergeRules` (max stack, multipliers) |

---

## Default scene

- `enemyUnitId` mặc định trong code: `unit_moss_oracle` (đổi được trên `DuelDirector`).
- Fallback khi không có def: **HP** = `enemyMaxHp` (**40**), **Attack** = `enemyStrikeDamage` (**5**).

---

## Bảng unit Enemy trong catalog

| `unitId` | Display name | Rarity | Max HP | Attack | Body | Ranged |
|----------|----------------|--------|--------|--------|------|--------|
| `unit_moss_oracle` | Moss Oracle | Epic | 88 | 15 | Square (melee) | Không |
| `unit_enemy_ranged` | Spore Sniper | Epic | 52 | 6 | Triangle (ranged) | Có — range **1.85**, shot interval **0.55** s |

Ghi chú:

- **Luân phiên theo wave** (`DuelDirector`): wave **lẻ** (1,3,5…) spawn **Moss** melee vuông; wave **chẵn** (2,4,6…) spawn **Spore** ranged tam giác (fallback `enemyUnitId` nếu thiếu id trong catalog).
- Scale world: melee vuông/tròn **0.55**, ranged tam giác **0.135** (`ApplyEnemyDefinitionVisualScale`).
- Enemy wave spawn dùng `UnitDefinition.MaxHitPoints` / `Attack` khi có def; tint / tier theo `DuelDirector` spawn wave.
- **Buff run** (`BattleRunBuffs`) không đổi chỉ số gốc trên asset, nhưng **ally** nhận buff; damage ally lên enemy có thể có crit từ buff — balance combat nằm ở `DuelActor` + `GetOutgoingDamageForAlly`.

---

## Runtime (`EnemyInstanceSpec`)

Tương tự ally: `CombatMaxHitPoints`, `CombatAttack`, catalog id, rarity tier, stack leader/follower.

Cập nhật file này khi thêm enemy asset mới hoặc chỉnh default `DuelDirector` / wave spawn.
