# Bộ nhân vật 2D mới — baseline

Ngày chốt: 2026-09-27.

## Trạng thái sạch

- Game tiếp tục bằng Unity 2D và C#.
- Không còn animation hoặc prefab gameplay cũ của Alpha, Robot Kim, Croco, AnimalKit, UnitCatalog, ally Spine màu hoặc các tile/legend nhập cũ.
- Mọi `UnitDefinition.battlePrefabResourcesPath` cũ đã được bỏ. Gameplay dùng hình fallback, vẫn giữ combat, stats, roll, buff, merge và save.
- Knight còn nguyên data Spine, prefab và scene test nhưng chỉ là reference. Knight không được map vào unit gameplay.
- Giữ `IBattleVisualDriver`, `SpineBattleAnimator` và logic gọi hit của `DuelActor` làm điểm nối C#. Chưa chọn Sprite Frame hay Unity 2D cutout cho bộ mới.

## Bộ mới phải được thiết kế như một hệ thống

Bộ character bao phủ Human, Robot, Monster, Elemental và Beastfolk. Mỗi thiết kế phải phân biệt rõ melee/ranged và Common/Rare/Epic/Legendary bằng silhouette, kích thước, trang bị và mật độ chi tiết.

Chuẩn animation tối thiểu của mỗi character:

- Idle: loop rõ ở kích thước điện thoại.
- Walk: chân và trọng tâm di chuyển thật, không chỉ nhún root.
- Attack: anticipation, impact, recovery; event hit chỉ phát một lần.
- Hit: flash/reaction ngắn, không phá attack callback.
- Die: chuyển sang pose nằm rõ và giữ frame cuối.
- Skill: bắt buộc từ Epic/Legendary, hình đọc khác basic attack.

## Cổng duyệt trước khi vào battle

1. Source art có alpha thật và cùng tỷ lệ/góc nhìn.
2. Character Lab phát từng state độc lập, không lỗi Console.
3. Silhouette đọc được trên viewport điện thoại.
4. Attack event khớp đúng frame impact.
5. Prefab không chứa đường dẫn hoặc controller của bộ cũ.
6. Chỉ sau khi duyệt Character Lab mới map vào UnitDefinition và battle.
