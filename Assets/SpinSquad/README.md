# SpinSquad — project guide

Mọi nội dung game nằm dưới folder này để dễ tìm và tránh rải rác ở root `Assets/`.

## Chạy demo trong Editor (Play)

Unity **Play** luôn chạy **scene đang mở** trong tab Scene — không tự mở scene đầu tiên trong Build Settings. Demo combat (`DuelDirector`, catalog unit) nằm trong **`Scenes/SampleScene.unity`**. Nếu bạn để scene trống kiểu *Untitled* chỉ có Main Camera rồi bấm Play, Game view sẽ chỉ thấy **màu nền camera (xanh)**. Cách nhanh: menu **SpinSquad → Play → Open Sample Scene**, sau đó bấm **Play**.

## Cây thư mục

| Đường dẫn | Dùng cho |
|-----------|-----------|
| `Scenes/` | Scene build (hiện có `SampleScene`). |
| `Scripts/Runtime/Gameplay` | Battle loop, unit, inventory, animation bridge và combat VFX. |
| `Scripts/Runtime/Gacha` | Roll, reward, wallet và run buff. |
| `Scripts/Runtime/Meta` | Save data, progression và treasure definitions. |
| `Scripts/Runtime/Data` | Catalog, ScriptableObject và định nghĩa unit. |
| `Scripts/Runtime/Presentation` | Camera, backdrop và presentation 2.5D. |
| `Scripts/Runtime/Scenes` | Composition/controller dành riêng cho từng scene. |
| `Scripts/Runtime/UI` | Widget, sprite provider và theme UI dùng lại được. |
| `Scripts/Editor` | Importer, validator, scene builder và công cụ chỉ chạy trong Unity Editor. |
| `Scripts/Editor/ProjectTools` | Công cụ setup/validation cấp project đã được gom từ folder Editor cũ. |
| `Prefabs/Gameplay` | Prefab gameplay (không phải UI thuần). |
| `Prefabs/UI/Characters/Allies` | Prefab UI liên quan nhân vật phe đồng minh. |
| `Prefabs/UI/Characters/Enemies` | Prefab UI liên quan nhân vật địch. |
| `Art/Sprites/Characters/Allies` | Sprite/portrait đồng minh. |
| `Art/Sprites/Characters/Enemies` | Sprite/portrait địch. |
| `Art/Animations` | Animation clips / controllers (tuỳ bạn tổ chức thêm). |
| `Art/Fonts` | Font asset. |
| `Audio/Music` | Nhạc nền. |
| `Audio/SFX` | Hiệu ứng âm thanh. |
| `Settings/` | Input actions và cấu hình khác (ví dụ `InputSystem_Actions.inputactions`). |
| `ThirdParty/` | Asset / package tải ngoài (Store), giữ tách khỏi code nội bộ. |

Xem [PROJECT_ARCHITECTURE.md](Docs/PROJECT_ARCHITECTURE.md) để biết luồng phụ thuộc, quy tắc đặt file và điểm bắt đầu khi sửa từng hệ thống.

## Ghi chú

- Không đổi hoặc xóa file `.meta` khi di chuyển asset. GUID trong `.meta` giữ liên kết scene/prefab không bị gãy.
- Code runtime không được phụ thuộc code trong `Scripts/Editor`.
- Chỉ đặt asset vào `Resources/` khi code thực sự tải nó bằng `Resources.Load`; asset thông thường đặt ở `Art/`, `Data/` hoặc `Prefabs/`.
- Tên hiển thị trong game dùng tiếng Anh. Log kỹ thuật có thể chuyển dần sang tiếng Anh khi chạm vào file liên quan.
