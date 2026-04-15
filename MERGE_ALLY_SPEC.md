# Merge ally (SpinSquadClone)

- **Stack**: tối đa **3** ally **cùng `unitId` + cùng `Rarity`** trong một ô lưới ally.
- **Merge**: chọn ô đủ 3 → nút **mũi tên lên** → **1** ally còn lại, `Rarity + 1` (tối đa **Mythic**), HP × **1.78**, Attack × **1.52** (hằng trong `AllyMergeRules`).
- **Kéo**: chỉ **leader** (ally spawn đầu trong stack) kéo được; follower bám offset; vị trí lưới theo **leader**.
- **Hình**: `UnitBodyShape` trên `UnitDefinition` — vuông / tròn / tam giác; **Triangle** tự bật ranged trong `OnValidate`.

Chi tiết roll máy vẫn ở `ROLL_MACHINE_SPEC.md`.
