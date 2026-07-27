# Legends Melee UI Flow (SpinSquadClone)

Tài liệu này mô tả cách đưa `1 file PSD` thành UI ally hoàn chỉnh cho rarity `Legends`, type `Melee`.
Mục tiêu là để bạn và agent làm song song: bạn xử lý art, agent xử lý pipeline + code + logic.
Hướng dẫn thao tác Unity từng bước nằm ở: `UNITY_ANIMATION_SETUP_LEGENDS_MELEE.md`.

---

## 1) Trạng thái hiện tại của project

- Project đang có:
  - Unity 6 (`6000.4.1f1`)
  - `com.unity.feature.2d`
  - UI scene đã có (`Homepage`, `Upgrade`, `Treasure`, ...)
  - combat/gacha có logic cơ bản
- Project đang thiếu:
  - mapping chính thức từ `unitId -> portrait/card/icon`
  - pipeline art cho ally UI
  - animation UI theo style game (reveal/upgrade pulse/selection)
- PSD mới của bạn:
  - `Assets/SpinSquad/Data/Alliespart/legendsrobot.psd`
- Trạng thái tích hợp hiện tại:
  - Đã thêm unit mới `unit_legends_melee_robot` (Legends + Melee) vào catalog.
  - Đã map unit này vào line ally để nhìn thấy ngay trong UI `Upgrade/UpgradeDetail`.
  - Tạm thời dùng sprite legends có sẵn (`alliesUI/legends/tile004`) để test UI end-to-end.
  - Bước tiếp theo sẽ thay sprite tạm bằng PSD/layer rig thật từ `legendsrobot.psd`.

---

## 2) Hai hướng triển khai

## Hướng A - Nhanh để có kết quả (Static UI trước)

Phù hợp khi muốn "lên được UI đẹp hơn ngay", chưa cần rig 2D phức tạp.

### A.1 Đầu vào cần có

- Từ PSD export tối thiểu:
  - `portrait` (512x512 hoặc 1024x1024, nền trong)
  - `card_full` (dùng cho card list)
  - `icon_small` (128x128 hoặc 256x256)
- Đặt tên file rõ ràng:
  - `legend_melee_robot_portrait.png`
  - `legend_melee_robot_card.png`
  - `legend_melee_robot_icon.png`

### A.2 Kết quả

- UI `Upgrade`, `UpgradeDetail`, `roll result`, `list` hiện sprite thật.
- Chưa có animation rig theo layer, chỉ có tween/hiệu ứng đơn giản.

### A.3 Ưu/nhược

- Ưu:
  - Nhanh
  - Rủi ro thấp
  - Dễ debug
- Nhược:
  - Chưa đạt "feel game" tối đa
  - Khó làm animation kỹ xảo theo từng bộ phận nhân vật

---

## Hướng B - Chuẩn game hơn (PSD/Krita -> PNG parts -> Unity 2D Animation)

Phù hợp khi muốn chất lượng cao, animate được theo từng bộ phận và tránh phụ thuộc PSD importer.

### B.1 Đầu vào cần có trong PSD/Krita

Cần tách layer sạch và có tên ổn định, sau đó export ra PNG parts:

- Bắt buộc:
  - `body`
  - `head`
  - `arm_l`
  - `arm_r`
  - `weapon`
- Nên có thêm:
  - `shadow`
  - `glow`
  - `fx_slash`
  - `fx_legend_aura`

Quy ước naming:

- lower_snake_case, không dấu, không khoảng trắng
- không trùng tên layer
- pivot gần hợp lý với khớp quay (vai, cổ tay, đầu)

### B.2 Kết quả

- Unity import PNG parts thành nhiều sprite rõ ràng
- setup rig 2D + animator clips:
  - `idle_breath`
  - `reveal_in`
  - `tap_bounce`
  - `upgrade_flash`
- Code trigger animation theo state UI.

### B.3 Ưu/nhược

- Ưu:
  - Gần với "UI như game"
  - Motion đẹp, có độ sâu
- Nhược:
  - Tốn thời gian hơn
  - Cần kỷ luật cao về layer/pivot
  - Dễ phát sinh bug import/rig nếu naming lỗi

---

## 3) Checklist chi tiết cho 1 ally Legends + Melee

## 3.1 Art checklist (bạn làm)

- [ ] PSD có layer rõ ràng (theo hướng A hoặc B)
- [ ] Background trong suốt
- [ ] Màu đã được color balance để hợp palette game
- [ ] Export đúng kích thước
- [ ] Không để layer ẩn, layer rác, text guide trong file final

## 3.2 Unity asset checklist (agent + bạn)

- [ ] Đặt file vào folder pipeline chốt
- [ ] Import setting đúng (`Sprite (2D and UI)`, alpha on)
- [ ] PPU thống nhất (để xác nhận: 100 hay giá trị khác)
- [ ] Tạo asset map từ `unitId` đến portrait/card/icon
- [ ] Link vào UI scene (`Upgrade`, `UpgradeDetail`, roll UI)

## 3.3 Data checklist (agent)

- [ ] `UnitDefinition` có tham chiếu visual cần thiết
- [ ] Có fallback nếu sprite null
- [ ] Có phân biệt rarity frame (Legends)
- [ ] Có phân biệt type badge (Melee)

## 3.4 Animation checklist (nếu theo Hướng B)

- [ ] Clip idle nhẹ (0.8s-1.2s loop)
- [ ] Clip reveal ngắn (0.35s-0.6s)
- [ ] Clip upgrade nhấn mạnh rarity
- [ ] Trigger code đã map đúng state

---

## 4) Đề xuất lộ trình làm việc song song

## Sprint 1 (theo hướng bạn chọn: Hướng B)

1. Chốt dùng Hướng B cho `legendsrobot.psd`.
2. Bạn chuẩn hóa layer PSD/Krita theo checklist B.1 rồi export PNG parts.
3. Agent hỗ trợ map vào 1 unit Legends Melee duy nhất.
4. Kiểm tra trên `Upgrade` + `UpgradeDetail`.
5. Chốt naming + import rules + animation clip tối thiểu.

## Sprint 2 (mở rộng)

1. Nhân bản pipeline cho các ally khác.
2. Thêm animation reveal/upgrade nâng cao.
3. Tối ưu atlas và memory.

---

## 5) Khi nào agent sẽ hỏi ngược lại bạn

Agent sẽ hỏi ngay nếu gặp 1 trong các điểm mơ hồ sau:

1. Ally này map vào `unitId` nào?
2. PPU muốn chốt bao nhiêu cho UI sprites?
3. Dùng 1 bộ art cho nhiều rarity hay mỗi rarity 1 bộ riêng?
4. Mục tiêu ưu tiên của sprint hiện tại: animation trước hay data trước?
5. Cần rig full (nhiều xương) hay semi-rig (ít xương, nhanh hơn)?

Nếu bạn chưa rõ, chỉ cần trả lời ngắn:

- `unitId`
- "ưu tiên animation" hoặc "ưu tiên data"
- "rig full" hoặc "semi-rig"

Agent sẽ tự động đề xuất tiếp các bước còn lại.

---

## 6) Quyết định để bắt đầu ngay (theo mục tiêu học)

Vì bạn làm để học và muốn chuẩn, ta bắt đầu bằng **Hướng B**:

1. Chuẩn hóa PSD layer trước.
2. Export PNG parts và import vào Unity đúng setting.
3. Dựng rig + clip tối thiểu.
4. Nối code để UI chạy end-to-end.

Đây là đường học đúng bản chất pipeline game UI animation.
