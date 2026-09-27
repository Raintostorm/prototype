# Knight — golden character cho cấu trúc và animation

## Quyết định của người dùng

Ngày 2026-09-07, người dùng xác nhận: **Knight hiện hoạt động đúng; UI không phù hợp với game; animation là phần hợp lý nhất để tham khảo.**

Knight được chọn làm golden character theo xác nhận này. Dùng cơ chế dựng nhân vật, rig và animation làm mốc kỹ thuật cho character mới. UI, portrait, tạo hình và phong cách hình ảnh của character mới phải phù hợp game, không mặc nhiên kế thừa Knight.

Nguồn bằng chứng: người dùng xác nhận chất lượng chạy; agent reverse-engineer asset/code trên đĩa. Agent chưa tự chạy Play Mode trong đợt này. Chỉ cập nhật tài liệu, không tạo character mới hoặc sửa game.

## 1. Các asset đầu vào

Các đường dẫn bên dưới tính từ `Assets/SpinSquad/`, trừ khi ghi khác.

| Vai trò | Asset/code |
|---|---|
| Visual prefab | `Resources/Battle/KnightBattleVisual.prefab` |
| Skeleton export | `Data/Spine/Knight/skeleton.json`, Spine 3.8.75 |
| SkeletonData | `Data/Spine/Knight/skeleton_SkeletonData.asset` |
| Atlas | `Data/Spine/Knight/skeleton_Atlas.asset`, `skeleton.atlas.txt` |
| Material/texture | `Data/Spine/Knight/skeleton_Material.mat`, `skeleton.png` |
| Shader | `Assets/Spine/Runtime/spine-unity/Shaders/Spine-Skeleton.shader` |
| Config trên prefab | `Data/Spine/Knight/KnightSpineBattleAnimConfig.asset` |
| Ally definition chính | `Data/Units/common_melee.asset` — line 4/Thổ |
| Enemy definition | `Data/Units/unit_enemy_knight.asset` |
| Legacy alias dùng cùng visual | `Data/Units/unit_legends_melee_robot.asset` |
| Catalog | `Resources/UnitCatalog_Main.asset` |
| Driver | `Scripts/Runtime/Gameplay/SpineBattleAnimator.cs` |
| Combat | `Scripts/Runtime/Gameplay/DuelActor.cs`, `CombatHealth.cs`, `DuelDirector.cs` |
| Scene thử clip | `Scenes/KnightSpineAnimTest.unity` |
| Builder | `Scripts/Editor/ProjectTools/SpinSquadSpineBattlePrefabBuilder.cs` |

Chuỗi GUID prefab → SkeletonData → JSON/atlas → material → shader/texture đã resolve trên đĩa. Chưa kiểm chứng mọi subasset fileID qua Unity AssetDatabase.

## 2. Hierarchy và components của prefab trên đĩa

```text
KnightBattleVisual                  [GameObject duy nhất, không child]
├── Transform
├── MeshFilter                      [mesh serialized = null; Spine tạo mesh]
├── MeshRenderer
├── SkeletonAnimation
└── SpineBattleAnimator
```

Không có Collider2D, Rigidbody2D, Unity Animator, Animator Controller, SpriteRenderer con, CombatHealth hay DuelActor trên prefab này. Skeleton/bones của Spine là cấu trúc dữ liệu riêng, không phải 30 Transform con hiển thị trong Unity hierarchy.

### Serialized fields quan trọng

| Component/asset | Giá trị |
|---|---|
| GameObject | Active, Untagged, layer 0/Default |
| Transform | Position 0, rotation identity, scale 1 |
| MeshRenderer | Material skeleton_Material; sorting layer Default, order 0 |
| SkeletonAnimation | SkeletonData trỏ Knight; initialFlipX/Y false; animation `idle`; loop true; timeScale 1 |
| SkeletonAnimation mesh | zSpacing 0, useClipping true, pmaVertexColors true, tintBlack false |
| SkeletonData | Import scale 0.01; defaultMix 0.2 giây; không controller; không mix override theo cặp clip |
| Material | Spine/Skeleton; texture skeleton.png; StraightAlphaInput 0 |
| Driver trên prefab | animConfig trỏ KnightSpineBattleAnimConfig; tên idle/walk/attack/died; inline attackMinInterval 0.08; lockFacingOnDie true; invertFacingSign false |
| Config asset | idle/walk/attack/died; attackMinInterval **0.2** — ưu tiên hơn inline 0.08 khi driver prefab được dùng trực tiếp |

Các field không serialize trên prefab vẫn có default từ script; không được coi chúng là 0. Ví dụ attackAnimationSpeed default 2.4 và fallbackHitNormalizedTime default 0.33.

## 3. Rig và sprite hierarchy bên trong Spine

Skeleton có **30 bones, 20 slots, một skin default: 14 region attachments + 6 mesh attachments**. Cloak là weighted mesh; năm mesh còn lại không weighted theo cấu trúc vertices/UV của export. Không có IK hoặc transform constraints trong JSON này.

```text
root
└── main
    ├── chest → neck → head
    │                   ├── cap
    │                   └── eyes
    ├── right_side → right_shoulder → right_arm → right_hand → shield
    ├── left_side  → left_shoulder  → left_arm  → left_hand  → mace
    ├── left_leg  → left_angle  → left_foot
    ├── right_leg → right_angle → right_foot
    └── cloak
        ├── right_cloak → right_cloak_1 → right_cloak_2
        └── left_cloak  → left_cloack_1 → left_cloack_2
```

Tên `angle` và `cloack` được giữ đúng như export; không phải đề xuất đặt tên mới.

Slot draw order từ sau tới trước:

```text
cloak → left_leg → left_foot → right_leg → right_foot
→ neck_armor → chest_armor → chest_shield → head
→ head_hammer → helve_hammer → left_arm → left_hand
→ left_shoulder_armor → right_arm → right_hand
→ right_shoulder_armor → shield → eyes → coronet
```

Thứ tự này nằm trong Spine, không dựa vào sorting layers `leg/body/head` của Unity. Bài học để áp dụng: chia bộ phận theo khớp và quan hệ chuyển động; vũ khí theo tay, đầu theo cổ, áo choàng có chuỗi xương riêng. Không chỉ dịch hoặc lắc cả ảnh nhân vật.

## 4. Animation clips và điều khiển

Không có file Knight.controller hay bộ .anim dành cho battle Knight: animation nằm trong `skeleton.json` và phát bằng Spine AnimationState, track 0.

Thời lượng dưới đây tính theo thời điểm key cuối trong export; không phải thời gian đã đo trong battle.

| Clip | Thời lượng nguồn | Dữ liệu đáng chú ý | Cách driver dùng |
|---|---:|---|---|
| idle | 2.0 s | Chest, shield và hai side chuyển động nhẹ; side có scale | Loop khi đứng yên |
| walk | 1.0 s | Chân/bàn chân, thân, cổ, mace và shield chuyển động phối hợp | Loop khi DuelActor báo di chuyển |
| attack | 1.0 s | 14 bones được animate, gồm chân, thân, hai tay, mace, shield và root | One-shot; attack entry có speed riêng |
| died | 2.6667 s | Main, mace, mắt, bàn chân, thân và shield | One-shot, khóa facing |

Không có clip hit/skill và không có Spine event `hit` trong export. Không thể coi golden character này là bằng chứng pipeline đã hỗ trợ đầy đủ skill hoặc phản ứng trúng đòn bằng clip riêng.

Luồng animation:

```text
DuelActor
  SetMoving(bool) / ForceIdleLoop() / UpdateFacing(dirX)
  QueueAttackHit(callback)
    → IBattleVisualDriver.PlayAttack(onHit, onComplete)
      → SpineBattleAnimator → Spine.AnimationState
CombatHealth.Died
  → SpineBattleAnimator.PlayDie()
  → DuelDirector.OnUnitDied() → UnitDeathSequence
```

- Walk không thay attack đang chạy; sau attack driver về locomotion theo trạng thái moving.
- Facing runtime dùng Skeleton.ScaleX. Ally mặc định +X, enemy -X; driver giữ scale Transform visual dương.
- Damage chỉ phát một lần qua FireAttackHitOnce. Knight không có event hit nên dùng fallback 33% tiến trình attack; Complete còn có nhánh đảm bảo callback.
- Với attack nguồn 1 s và speed default 2.4, thời gian clip dự kiến khoảng 0.417 s; fallback hit khoảng 0.138 s theo game time, trước sai số frame/mix và tác động khác. Chưa đo runtime.
- Root translate trong clip chỉ di chuyển skeleton visual; DuelActor vẫn điều khiển vị trí host gameplay.

## 5. Nhân vật runtime được dựng như thế nào

```text
UnitDefinition → ResolveBattlePrefabSource → Resources.Load
→ CreateRigFighter → EnsureBattleVisualDriver
→ ConfigureAlly/Enemy → RegisterAlly/Enemy
```

Hierarchy runtime suy ra từ code, chưa chụp từ Play Mode:

```text
Combat unit host
├── CombatHealth
├── SpineBattleAnimator              [driver mới trên host]
├── BoxCollider2D
├── DuelActor
├── DuelUnitGridDrag
├── WorldUnitHealthBar
├── AllyInstanceSpec hoặc EnemyInstanceSpec [theo đường spawn]
└── VisualRig                       [instance của KnightBattleVisual]
    ├── MeshFilter + MeshRenderer
    └── SkeletonAnimation
```

Các object thanh máu, trang trí/overlay tùy cấu hình được tạo thêm khi chạy. UnitDeathSequence chỉ được thêm khi cần xử lý chết. Nhánh vertical slice bổ sung UnitPresentation25D và bóng tiếp xúc.

### Scale, anchor và physics

- CreateRigFighter dùng host scale `(0.08, 0.08, 1)` cho Knight; SkeletonData đã có scale 0.01. Đây là kích thước riêng của nguồn Knight, không sao chép mù sang art mới.
- VisualRig được reset scale 1, rotation identity. Scene battle được kiểm tra có visual offset 0.
- Auto-align mặc định bật: tìm Transform `Anchor_Foot`; nếu không có, dùng bounds setup pose của Spine để căn giữa X và baseline Y. Knight không có child Transform Anchor_Foot nên đi qua fallback này nếu skeleton khởi tạo thành công.
- Ally collider mặc định local 4.2 × 4.2, được giới hạn world size theo 86% ô. Enemy đi qua kích thước khác, mặc định 0.48 × 0.48 trong CreateRigFighter. Phải kiểm tra collider world thực tế khi làm nhân vật mới.
- Không có Rigidbody2D được thêm trong đường spawn Knight đã đọc. Combat di chuyển host bằng Transform và kiểm tra khoảng cách, không yêu cầu rigidbody simulation.

### Config prefab khác config battle runtime

CreateRigFighter disable/xóa SpineBattleAnimator trên visual, rồi thêm driver mới lên host. Code chỉ chuyển settings từ SpineBattleRigProfile nếu có; Knight prefab hiện không có profile này.

Vì vậy animConfig của prefab không được sao chép sang host. Driver battle Knight đi theo default script: idle/walk/attack/died, attackMinInterval 0.12, attack speed 2.4, hit normalized 0.33; không phải interval 0.2 của config asset trên prefab. Đây là kết luận từ code, cần quan sát Inspector runtime để xác nhận thực nghiệm.

## 6. ScriptableObject và chỉ số

| Definition | ID | Phe | Base HP/ATK | Rarity lưu trên definition |
|---|---|---|---|---|
| common_melee.asset | common_melee | Ally | 100/10 | Common |
| unit_enemy_knight.asset | unit_enemy_knight | Enemy | 100/10 | Common |
| unit_legends_melee_robot.asset | unit_legends_melee_robot | Ally | 115/19 | Legendary |

Cả ba trỏ Resources path `Battle/KnightBattleVisual`. Tên definition/ID không phải bằng chứng visual khác nhau. Rarity instance và các multiplier runtime mới quyết định chỉ số trong trận; không suy HP/ATK cuối cùng chỉ từ prefab.

`common_melee` là line 4 trong AllyLineCatalog. Ally stats qua AllyStatScaling rồi meta upgrade, treasure, roll buff; enemy có cấu hình và scaling riêng.

## 7. UI và tạo hình — phạm vi cần làm riêng

Người dùng không chọn UI của Knight làm chuẩn. Không sao chép mặc định portrait, card, icon, UI controller hoặc phong cách Knight sang character mới.

Binding hiện tại:

- common_melee và các alias dùng portrait/card/icon legacy tại `Data/alliesUI/legends/tile004.png`.
- common_melee và unit_legends_melee_robot còn trỏ UI controller `Animations/UI/LegendsMeleeRobot/LegendsMeleeRobot_UI`.
- Upgrade và UpgradeDetail gọi AllyUpgradePortraitResolver: instantiate battle prefab, render idle frame 0 qua camera layer 30 thành sprite 320 × 320, cache theo Resources path.
- Như vậy thay portrait field đơn lẻ chưa chắc thay được hình đang hiện trong Upgrade. Cần xác định từng màn lấy ảnh từ definition hay ảnh render prefab.
- Resolver hiện bỏ qua rarity khi chọn path và dùng BattlePrefabResourcesPath trực tiếp; đường này không giống mapping rarity override trong DuelDirector.

## 8. Các điểm cần kiểm tra trước khi dùng làm template triển khai

1. **Bảo toàn config:** đối chiếu driver prefab với host battle; không để template ghi interval 0.2 nhưng chạy 0.12 mà không giải thích.
2. **Death duration:** clip nguồn died 2.6667 s; UnitDeathSequence mặc định giữ 1.2 s rồi fade 0.45 s và Destroy (~1.65 s). Có khả năng đoạn cuối clip không được thấy; cần quan sát runtime, kể cả khi chuyển wave.
3. **Builder drift:** builder tạo driver mới nhưng không gán KnightSpineBattleAnimConfig. Chạy regenerate có thể mất config đang được lưu trên prefab; không chạy builder trong audit.
4. **Animation callback:** PlayAttack có nhánh return khi bận/cooldown/thiếu state hoặc clip; cần kiểm tra damage callback khi bị từ chối hoặc ngắt, không giả định interface tự bảo đảm mọi request đều gây damage.
5. **Facing/anchor:** kiểm tra cả hai phe, di chuyển đổi hướng, baseline giữa clip và quy mô hiển thị trên màn hình dọc.

Các điểm này không phủ nhận đánh giá Knight chạy đúng của người dùng; chúng giới hạn những gì có thể sao chép nguyên trạng thành chuẩn cho character khác.

## 9. Hợp đồng template cho character mới

Đây là đặc tả tài liệu, chưa tạo prefab hoặc asset mới.

- Giữ cấu trúc host gameplay tách khỏi visual; chỉ một driver điều khiển animation battle.
- Nếu tiếp tục Spine: cung cấp skeleton/atlas/texture/material tương thích runtime hiện tại, các clip và config mapping rõ ràng; không cần tạo Unity Animator Controller chỉ để giống ví dụ Hero.
- Rig dựa trên các khớp thật của thiết kế mới, phần phụ/vũ khí có quan hệ parent phù hợp. Tham khảo sự phối hợp chuyển động Knight; không bắt buộc có đúng 30 bones, áo choàng hoặc vũ khí giống Knight.
- Idle/walk/attack/died là bộ tham khảo đã có. Hit/skill chỉ bổ sung khi được thiết kế và kiểm chứng riêng.
- Thống nhất một nơi điều khiển facing, quy ước foot anchor, world size, sorting và collider; tránh offset/flip chồng nhiều tầng.
- Damage phải gắn với pha tiếp xúc của animation mới, không lấy 33% hoặc speed 2.4 làm hằng số cho mọi character.
- Definition có ID, stats, role, visual binding và UI assets riêng; kiểm tra mapping line/rarity hiện có trước khi thêm loại mới.
- Tạo hình và UI theo hướng nghệ thuật của game; battle visual và portrait/card/icon đều cần được duyệt.
- Kiểm tra cả trong scene preview và đường spawn thật: prep, drag, bag, merge, battle, chết, chuyển wave, restart; preview đúng chưa đủ kết luận tích hợp đúng.

## 10. Phạm vi chưa thực hiện

Chưa phát animation để phân tích trực quan từng pose hoặc thời điểm tiếp xúc. Chưa sửa những điểm tích hợp nêu trên. Chưa tạo character, đổi UI, regenerate prefab, build hoặc chạy tests. Tiếp tục audit trước khi người dùng cho phép triển khai.
