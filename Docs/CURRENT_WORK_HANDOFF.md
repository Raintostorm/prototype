# SpinSquadClone — Bàn giao công việc hiện tại

> Mapping animation trong tài liệu này đã bị thay thế bởi đợt reset ngày 2026-09-27. Nguồn hiện hành: `PROJECT_DIRECTION.md` và `NEW_2D_CHARACTER_SET.md`.

## 1. Tổng quan

- Tên project: **SpinSquadClone / Prototype**.
- Engine hiện tại: **Unity 6.4 (6000.4.1f1)**.
- Định hướng: game **2.5D auto-battler**, màn hình dọc cho mobile.
- Mốc kiểm tra giao diện chính: **iPhone X portrait**.
- Ngôn ngữ trong game: **tiếng Anh**.
- Phong cách UI đang hướng tới: fantasy mobile, xanh dương đậm + viền vàng, dễ nhìn, đồng nhất giữa mọi scene.

## 2. Công việc đang làm ngay lúc bàn giao

Đang sửa đội hình test khởi đầu của Wave 1 để chỉ có đúng hai Metal allies và không cần roll:

1. **Metal Common / bậc trắng**
   - Phải là **robot bay/hover cũ**, có phần lò xo hoặc cuộn xoắn phía dưới.
   - Không được dùng White Knight.
   - Không được dùng robot đen đứng dưới đất.

2. **Metal Rare / bậc xanh dương**
   - Phải là **robot cầm súng mới** lấy từ `robot_v2.zip`.
   - Phải quay đúng hướng về phía enemy.
   - Phải nằm giữa ô, chân/đế đúng baseline, không lệch lên trên hoặc sang bên.

Thiết lập hiện tại trong code đã cố định:

- Tiền roll đầu Wave 1: `0`.
- Starter 1: Metal Common.
- Starter 2: Metal Rare.

## 3. Lỗi đang còn tồn tại

Theo ảnh kiểm tra mới nhất của người dùng:

- Ô Common đang hiện **robot đen dưới đất**, sai với robot bay cần dùng.
- Robot Rare cầm súng vẫn có dấu hiệu **lệch vị trí**; trước đó còn bị quay ngược.
- Đã nhiều lần sửa bằng cách flip/offset nhưng chưa xử lý đúng nguồn gốc.

Khả năng nguyên nhân:

- Prefab đang trỏ nhầm sprite hoặc nhầm asset dù tên prefab có vẻ đúng.
- Bounds/pivot của các frame khác nhau làm nhân vật lệch khi đổi animation.
- Có nhiều lớp flip (`SpriteRenderer.flipX`, scale âm hoặc logic theo team) chồng lên nhau.
- Offset đang được sửa thủ công thay vì chuẩn hóa theo bounds/baseline.

**Không được tiếp tục đoán theo tên file.** Cần mở trực tiếp prefab, kiểm tra sprite thật, GUID/meta và đối chiếu hình trong Play Mode.

## 4. Các đường dẫn quan trọng

### Logic starter và prefab battle

- `Assets/SpinSquad/Scripts/Runtime/Gameplay/DuelDirector.cs`
  - `TrySpawnStarterTestAllies`: tạo hai starter Metal Common và Metal Rare.
  - `TryResolveBattlePrefabSource`: hiện đang ánh xạ:
    - Common → `Battle/RobotKimFlyingBattleVisual`
    - Rare → `Battle/RobotKimBattleVisual`

### Prefab robot

- `Assets/SpinSquad/Resources/Battle/RobotKimFlyingBattleVisual.prefab`
  - Dự kiến dành cho robot bay Common, nhưng phải kiểm tra sprite thật bên trong.
- `Assets/SpinSquad/Resources/Battle/RobotKimBattleVisual.prefab`
  - Dự kiến dành cho robot súng Rare từ `robot_v2.zip`.

### Unit data

- `Assets/SpinSquad/Data/Units/ally_kim.asset`
  - Hiện có đường dẫn battle prefab mặc định là `Battle/RobotKimFlyingBattleVisual`.

### Các file nguồn nén còn trong project

- `robot.zip`
- `robot_v2.zip`
- `croco.zip`
- `buttons.zip`
- `button2.zip`

## 5. Cách sửa robot đúng trong lượt tiếp theo

1. Dừng Play Mode trước khi sửa asset/prefab.
2. Tìm robot bay cũ bằng **hình ảnh thực tế**, không chỉ dựa vào tên file.
3. Kiểm tra từng prefab robot và toàn bộ SpriteRenderer/Animator bên trong.
4. Xác nhận hướng gốc của từng frame:
   - Allies phải nhìn về phía enemy.
   - Chỉ một tầng logic chịu trách nhiệm flip.
5. Chuẩn hóa vị trí theo sprite bounds:
   - Căn giữa theo trục X.
   - Căn chân/đế theo cùng baseline trên trục Y.
   - Không kéo giãn ảnh để chữa lệch.
6. Chạy Wave 1 với đúng hai starter và kiểm tra Idle, Move, Attack, Hit, Die.
7. Chụp ảnh Play Mode làm bằng chứng trước khi báo hoàn thành.

## 6. Trạng thái gameplay liên quan

- Battle loop, wave, roll, bag, formation và bảng thống kê Damage/Taken/Healing đã có nền tảng.
- Bảng thống kê đang tách Top 4 Allies và Top 4 Enemies.
- Enemy Croco đã có bộ asset/prefab thay thế đang được tích hợp; tránh làm regress khi sửa robot.
- Mục tiêu test ngắn hạn là vào battle nhanh với hai starter, không mất thời gian roll.

## 7. Trạng thái UI

UI đã qua nhiều lần thay đổi nhưng vẫn cần chuẩn hóa toàn diện:

- Giữ một concept thống nhất: nền xanh đậm, viền vàng, icon rõ ở kích thước mobile.
- Mọi button cần tối thiểu:
  - Normal.
  - Pressed/On-click tối hơn nhẹ.
  - Disabled nếu chức năng có trạng thái khóa.
- Không kéo giãn icon hoặc button tùy tiện; giữ đúng tỉ lệ ảnh.
- Căn kích thước, khoảng cách và baseline đồng nhất.

Nhóm nút cần tiếp tục rà soát:

- Home, Battle/Start, Upgrade, Treasure, Settings.
- Shop, Mail, Bag.
- Pause, Resume, Restart, Back.
- Damage, Taken, Healing.
- Các nút trong Upgrade, Upgrade Detail và Treasure.

Các vấn đề UI từng gặp và không được tái diễn:

- Icon bị kéo dãn.
- Chữ chồng lên icon hoặc chạy ra ngoài nút.
- Button lệch tâm hoặc có kích thước không đồng đều.
- Nội dung Upgrade bị tràn màn hình; danh sách dài phải cuộn/kéo như giao diện mobile.
- Bag đóng nhưng ally preview vẫn lộ ra.

## 8. Quy tắc làm việc quan trọng

- Không xóa hoặc hoàn tác các thay đổi hiện có nếu chưa xác định rõ nguồn gốc.
- Không thay nhân vật bằng asset “gần giống”. Phải dùng đúng asset người dùng chỉ định.
- Không sửa hướng bằng nhiều lớp flip chồng nhau.
- Không báo đã kiểm tra animation nếu mới chỉ compile; cần Play Mode và quan sát trực tiếp.
- Kiểm tra cả logic và hình ảnh sau mỗi thay đổi lớn.
- Chỉ commit/push thẳng lên `main` khi người dùng yêu cầu trong lượt đó.

## 9. Tình trạng Git tại thời điểm bàn giao

Working tree đang có nhiều file đã sửa và asset mới chưa được track, gồm gameplay, UI, robot, Croco và các UI kit. Đây là công việc hiện tại của người dùng; cần giữ nguyên và chỉ stage đúng phạm vi khi commit.

## 10. Việc nên làm tiếp theo

Ưu tiên theo thứ tự:

1. Sửa đúng robot bay Common.
2. Căn giữa và xác nhận hướng robot súng Rare.
3. Chạy test Wave 1 với đúng hai robot, roll bằng 0.
4. Kiểm tra Idle/Move/Attack/Die và bullet của robot Rare.
5. Sau khi hai robot ổn định mới tiếp tục chuẩn hóa toàn bộ button/UI.

## Prompt gợi ý cho chat mới

> Hãy đọc `Docs/CURRENT_WORK_HANDOFF.md` trước. Tiếp tục sửa Wave 1 để starter Common là đúng robot bay cũ và starter Rare là robot súng trong `robot_v2.zip`. Không đoán theo tên prefab, không dùng knight/robot đen thay thế, không kéo giãn sprite. Hãy kiểm tra prefab, sprite, pivot, hướng và baseline thực tế; sau đó chạy Play Mode để xác nhận đúng hai robot, đúng hướng, đúng vị trí và đủ Idle/Move/Attack/Die.
