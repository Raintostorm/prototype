
# Unity Animation Setup - Legends Melee Robot

Tài liệu này là bản **quy trình ổn định** cho case của bạn:

- Nguồn gốc art: PSD/Krita
- Runtime trong Unity: PNG parts + rig + animation

Vì file PSD từ Krita có thể bị Unity import thành ảnh phẳng (không đọc layer), quy trình này ít rủi ro nhất.

---

## 1) Kết luận quan trọng trước khi làm

- Bạn **không làm sai**.
- Unity hiện import `legendsrobot.psd` bằng `TextureImporter (Default)` nên bị flatten.
- Do đó, đường làm đúng và ổn định là:
  1. Dùng PSD/Krita để vẽ/tách part
  2. Export từng part ra PNG
  3. Rig/animate trong Unity bằng các PNG đó

---

## 2) Vị trí file trong project

- PSD gốc: `Assets/SpinSquad/Data/Alliespart/legendsrobot.psd`
- Folder đề xuất cho PNG parts:
  - `Assets/SpinSquad/Data/Alliespart/exports/legends_melee_robot/`
- Unit data đã map: `Assets/SpinSquad/Data/Units/unit_legends_melee_robot.asset`
- Script bridge animation:
  - `Assets/SpinSquad/Scripts/UI/AllyUiAnimatorBridge.cs`
  - `Assets/SpinSquad/Scripts/Scenes/UpgradeDetailSceneController.cs`

---

## 3) Cần có package nào trong Unity

Mở `Window > Package Manager` và kiểm tra:

- `2D Animation`
- `2D Sprite`

`2D PSD Importer` có thể giữ, nhưng không bắt buộc cho pipeline PNG parts.

---

## 4) Chuẩn export từ Krita/PSD (rất quan trọng)

Export mỗi bộ phận thành 1 PNG riêng, nền trong suốt:

- `head.png`
- `torso.png`
- `upper_arm_l.png`, `upper_arm_r.png`
- `lower_arm_l.png`, `lower_arm_r.png`
- `hand_l.png`, `hand_r.png`
- `leg_l.png`, `leg_r.png`
- `foot_l.png`, `foot_r.png`
- `weapon.png` (nếu có)
- `fx_*.png` (tùy chọn)

Quy tắc tên:

- lower_snake_case
- không dấu
- không khoảng trắng

Kích thước:

- Có thể khác nhau giữa part (không cần ép cùng size)
- PPU thống nhất trong Unity: `100`

---

## 5) Import PNG parts vào Unity

Chọn toàn bộ PNG parts, trong Inspector đặt:

- `Texture Type`: `Sprite (2D and UI)`
- `Sprite Mode`: `Single`
- `Pixels Per Unit`: `100`
- `Mesh Type`: `Full Rect`
- `Alpha Is Transparency`: bật

Bấm `Apply`.

---

## 6) Dựng rig object trong Hierarchy

1. Tạo GameObject root: `LegendsRobot_UI_Rig`
2. Kéo từng PNG part vào làm con của root
3. Sắp xếp hierarchy gợi ý:
   - `root`
   - `torso`
   - `head` (con của torso)
   - `arm_l_root` -> `upper_arm_l` -> `lower_arm_l` -> `hand_l`
   - `arm_r_root` -> `upper_arm_r` -> `lower_arm_r` -> `hand_r`
   - `leg_l_root` -> `leg_l` -> `foot_l`
   - `leg_r_root` -> `leg_r` -> `foot_r`
   - `weapon` (gắn vào tay phải)
4. Chỉnh `Sorting Order` để part không chồng sai.

---

## 7) Tạo Animator Controller đúng path

Tạo file controller tại:

- `Assets/SpinSquad/Resources/Animations/UI/LegendsMeleeRobot/LegendsMeleeRobot_UI.controller`

Lý do: script bridge load từ `Resources` path:

- `Animations/UI/LegendsMeleeRobot/LegendsMeleeRobot_UI`

Nếu sai path, code không tìm thấy controller.

---

## 8) Thiết lập Animator state + parameter

States:

- `Idle` (default)
- `Reveal`
- `Attack`
- `Upgrade`

Parameters (đúng tên):

- Trigger `Reveal`
- Trigger `Attack`
- Trigger `Upgrade`
- Int `RarityTier`
- Bool `IsMelee`

---

## 9) Tạo clip animation

Mở `Window > Animation > Animation` khi chọn root rig:

- `Idle.anim`: lắc nhẹ thân/đầu, loop 0.8-1.2s
- `Reveal.anim`: scale/position vào khung trong 0.4-0.6s
- `Attack.anim`: tay phải + weapon swing
- `Upgrade.anim`: nhịp pulse + nhấn mạnh torso/head

---

## 10) Test trong UI của project

1. Mở scene `Upgrade`
2. Chọn card `Legends Melee Robot`
3. Vào `UpgradeDetail`
4. Kỳ vọng:
   - vào màn: chạy `Reveal`
   - sau đó: về `Idle`
   - bấm upgrade thành công: chạy `Upgrade`

---

## 11) Nếu không chạy, kiểm tra theo thứ tự

1. Controller có đúng path `Resources` chưa
2. Tên state có đúng `Idle/Reveal/Attack/Upgrade` chưa
3. Tên parameter có đúng chính tả chưa
4. Root portrait object có `Animator` + `AllyUiAnimatorBridge` chưa

---

## 12) Bạn cần gửi mình gì khi gặp lỗi

1. Ảnh hierarchy của rig object
2. Ảnh Animator graph + parameter list
3. Ảnh Inspector của 1 part PNG
4. Ảnh Console warning/error

Mình sẽ chỉ đúng lỗi cụ thể và sửa tiếp theo từng bước.
