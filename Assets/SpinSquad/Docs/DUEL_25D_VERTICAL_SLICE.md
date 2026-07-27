# SpinSquad — 2.5D Vertical Slice

## Mục tiêu

`Duel25DVerticalSlice` là scene thử nghiệm presentation 2.5D độc lập. Scene giữ nguyên
combat, grid, kéo/thả và merge trên mặt phẳng XY; chiều sâu được tạo bằng các tín hiệu
hình ảnh an toàn, không buộc gameplay phải chuyển sang physics hoặc tọa độ 3D.

## Mở và chạy

1. Trong Unity chọn **SpinSquad → Play → Open 2.5D Vertical Slice**.
2. Bấm **Play**.
3. Chọn preset trong arena nếu cần, rồi dùng **Áp dụng & reset trận**.
4. Quan sát các hàng trước/sau, bóng chân, hit flash, camera impact và death fade.

Phải mở qua menu trên: menu sẽ tạm bỏ điều hướng Play về Homepage để sandbox chạy
trực tiếp. Khi Unity reload lại scripts, luồng Play mặc định về Homepage được phục hồi.

Scene cũng có trong Build Settings tại:

`Assets/SpinSquad/Scenes/Duel25DVerticalSlice.unity`

Kiểm tra tự động trong Editor:

**SpinSquad → Validate → 2.5D Vertical Slice**

## Thành phần

| Thành phần | Vai trò |
|---|---|
| `BattlePresentation25D` | Chỉ tự cài trong scene vertical slice; nhận unit spawn/reset |
| `UnitPresentation25D` | Scale theo hàng, dynamic sorting, contact shadow và hit flash |
| `BattleCameraFeedback25D` | Camera shake và hit-stop ngắn cho đòn mạnh |
| `BattleBackdrop25D` | Các lớp màu/haze thủ tục, không cần thêm asset |
| `CombatHealth.Damaged` | Sự kiện presentation, không thay đổi công thức damage |

## Nguyên tắc an toàn

- Không đổi tọa độ gameplay từ XY sang XZ.
- Camera vẫn orthographic để `ScreenToWorldPoint` và kéo/thả giữ chính xác.
- Không chỉnh `DuelTestArena`, `SampleScene` hoặc prefab unit hiện tại.
- Unit được gắn presentation lúc runtime; unit spawn sau và reset trận vẫn được nhận.
- Scene khác không tự bật presentation 2.5D.

## Checklist đánh giá

- Unit ở hàng thấp hơn trông lớn và nằm trước unit hàng cao hơn.
- Mỗi unit có bóng ellipse mềm dưới chân.
- Unit chớp màu ngắn khi nhận damage và trở lại màu gốc.
- Đòn mạnh/death tạo camera feedback nhưng không làm hỏng chế độ tốc độ ×2.
- Reset arena nhiều lần không tạo nhiều `UnitPresentation25D` trên cùng một unit.
- Kéo/thả và merge ở prep phase hoạt động như arena cũ.
