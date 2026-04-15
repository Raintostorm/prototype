# SpinSquad — Tổng hợp tham chiếu & trạng thái project

Tài liệu được tạo tự động dựa trên workspace Unity `SpinSquad` và nguồn công khai về game thương mại cùng tên (Spin Squad!).

---

## 1. Game tham chiếu: Spin Squad! (cơ chế lõi roll / gacha)

Đây là **game di động đã phát hành**, không phải mã nguồn trong repo của bạn. Tên gần giống project Unity: **Spin Squad!** (tiếng Anh; từng được cộng đồng gọi là **Lucky Offense**).

| Mục | Thông tin |
|-----|-----------|
| **Nhà phát triển / bản quyền** | 111% Inc. (App Store ghi nhà bán: Crater Co., Ltd.) |
| **Thể loại** | Casual strategy — nhấn mạnh RNG (may mắn) kết hợp chiến thuật |
| **Cơ chế lõi liên quan gacha / roll** | Quay (spin) để nhận **Guardian** theo độ hiếm; slogan/marketing kiểu “Spin it, win it, RNG it!” — RNG và quay thưởng là trục nội dung chính |
| **Độ hiếm (theo mô tả store / hướng dẫn cộng đồng)** | Common → Rare → Epic → Legendary → **Mythic** (Mythic thường gắn với merge từ Guardian yếu hơn) |
| **Hệ thống bổ trợ** | Merge Guardian để tạo Mythic; đội hình theo **role / trait / synergy**; boss battle; nội dung sự kiện / chapter theo bản cập nhật |
| **Monetization** | IAP, Battle Pass, gói quay / boost; App Store ghi nhãn **Loot Boxes** (hộp quà ngẫu nhiên) |

**Liên kết tham khảo (nguồn chính thức / tổng hợp):**

- [Spin Squad! trên App Store (US)](https://apps.apple.com/us/app/spin-squad/id6511224968) — mô tả tính năng, IAP, privacy, phiên bản.
- [The Ultimate Spin Squad Tier List and Reroll Guide — Pro Game Guides](https://progameguides.com/guides/lucky-offense-tier-list-reroll-guide/) — hướng dẫn reroll / meta (nội dung bên thứ ba).

**Tóm tắt ý tưởng lõi (để đối chiếu khi thiết kế clone / game riêng):**

1. **Roll gacha** là vòng lặp chính: người chơi kỳ vọng đơn vị mạnh / đúng synergy từ mỗi lần quay.
2. **Tiến triển** gắn với merge và nâng cấp (đặc biệt hướng Mythic).
3. **Chiến thuật** được marketing đặt song song với RNG — team build và counter boss, không chỉ “quay trúng là thắng”.

---

## 2. Project Unity trong workspace `c:\app\forUnity\SpinSquad`

### 2.1 Xác nhận cấu hình

- **Tên sản phẩm (Player Settings):** `SpinSquad` — khớp thư mục project.
- **Phiên bản Unity:** `6000.4.1f1` (Unity 6, dòng 6000.x).
- **Gói chính (`Packages/manifest.json`):** Feature 2D, **Input System** `1.19.0`, **UGUI** `2.0.0`, Timeline, Visual Scripting, Test Framework, Collab Proxy, IDE Rider/Visual Studio, v.v.

### 2.2 Nội dung thư mục `Assets` (thực tế trong repo)

Chỉ có tài nguyên mặc định / tối thiểu:

| Đường dẫn | Vai trò |
|-----------|---------|
| `Assets/SpinSquad/Scenes/SampleScene.unity` | Scene mẫu mặc định |
| `Assets/SpinSquad/Settings/InputSystem_Actions.inputactions` | Map input mẫu (Input System) |
| Các file `.meta` | Metadata Unity |

**Không có:** thư mục `Scripts`, prefab gameplay, UI gacha, ScriptableObject tỉ lệ rơi, scene riêng cho spin, v.v.

### 2.3 Đánh giá trạng thái project

| Tiêu chí | Mức độ | Ghi chú |
|-----------|--------|---------|
| **Mức độ hoàn thiện gameplay** | Rất sớm / template | Chưa có logic gacha hay bất kỳ gameplay tùy chỉnh nào trong `Assets`. |
| **Khớp với game tham chiếu Spin Squad!** | Chỉ trùng **tên** | Project là shell Unity 6 + Input System + 2D; chưa thể hiện cơ chế roll/gacha. |
| **Sẵn sàng mở rộng** | Cao | Unity 6, Input System và UGUI đã có — phù hợp làm nền cho UI quay thưởng và gameplay 2D sau này. |
| **Rủi ro / lưu ý** | Repo nặng do cache | Thư mục `Library`, `Temp`, `Logs` thường không commit; nếu đang backup cả project, dung lượng lớn là bình thường. |

**Kết luận:** Đây là **project Unity mới hoặc vừa đặt tên `SpinSquad`**, chưa triển khai phần lõi “roll gacha”. Để tiến gần game tham chiếu, cần thêm thiết kế hệ thống (bảng tỉ lệ, pool, pity, currency), UI spin, model dữ liệu đơn vị (Guardian-like), và vòng gameplay (merge, combat) — tất cả hiện **chưa có** trong `Assets`.

---

## 3. Phân biệt rõ ràng

- **Spin Squad!** (app store): sản phẩm thương mại của 111%, có gacha/loot box và meta phức tạp.
- **Repo `SpinSquad` (Unity):** project cục bộ của bạn, cùng tên gói nhưng **chưa chứa** implementation tương ứng trong phần mã/tài nguyên đã quét.

Nếu bạn muốn bước tiếp theo trong code (ví dụ: prototype một `GachaController` + `ScriptableObject` pool + UI một nút Spin), có thể nói rõ platform mục tiêu (mobile / PC) và quy tắc tỉ lệ mong muốn.
