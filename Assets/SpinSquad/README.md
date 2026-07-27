# SpinSquad — cấu trúc `Assets/SpinSquad`

Mọi nội dung game nằm dưới folder này để dễ tìm và tránh rải rác ở root `Assets/`.

## Chạy demo trong Editor (Play)

Unity **Play** luôn chạy **scene đang mở** trong tab Scene — không tự mở scene đầu tiên trong Build Settings. Demo combat (`DuelDirector`, catalog unit) nằm trong **`Scenes/SampleScene.unity`**. Nếu bạn để scene trống kiểu *Untitled* chỉ có Main Camera rồi bấm Play, Game view sẽ chỉ thấy **màu nền camera (xanh)**. Cách nhanh: menu **SpinSquad → Play → Open Sample Scene**, sau đó bấm **Play**.

## Cây thư mục

| Đường dẫn | Dùng cho |
|-----------|-----------|
| `Scenes/` | Scene build (hiện có `SampleScene`). |
| `Scripts/Core` | Khởi tạo, game loop, service chung. |
| `Scripts/Gacha` | Roll, pool, pity, RNG. |
| `Scripts/UI` | Code UI (màn hình, widget). |
| `Scripts/Data` | ScriptableObject, định nghĩa dữ liệu. |
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

## Ghi chú

- File `.gitkeep` chỉ để Git giữ folder trống; có thể xóa khi folder đã có asset thật.
- Sau khi clone repo, mở Unity một lần để sinh `.meta` cho folder/file mới (nếu thiếu).
