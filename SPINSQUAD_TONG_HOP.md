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

## 2. Project Unity trong workspace (**SpinSquadClone** — clone/học tại máy cục bộ)

*(Đường dẫn máy của bạn có thể khác; nội dung dưới đây mô tả **trạng thái repo** hiện tại.)*

### 2.1 Xác nhận cấu hình

- **Tên sản phẩm (Player Settings):** `SpinSquad` — khớp thư mục `Assets/SpinSquad`.
- **Phiên bản Unity:** `6000.4.1f1` (Unity 6, dòng 6000.x).
- **Gói chính (`Packages/manifest.json`):** Feature 2D, **Input System** `1.19.0`, **UGUI** `2.0.0`, Timeline, Visual Scripting, Test Framework, Collab Proxy, IDE Rider/Visual Studio, AI Assistant inference package, v.v.

### 2.2 Nội dung thư mục `Assets` (thực tế trong repo)

Dưới `Assets/SpinSquad/` hiện có **code gameplay + meta**, scene flow, và tài nguyên game (Sprites, Spine, prefab, ScriptableObject), ví dụ:

| Đường dẫn | Vai trò |
|-----------|---------|
| `Assets/SpinSquad/Scripts/` | Core (`DuelDirector`, combat, grid, merge…), Data, `Gacha` (roll 6 ô), `Meta`, UI bridge, Scene controllers, Editor tiện ích |
| `Assets/SpinSquad/Scenes/` | `SampleScene`, `DuelTestArena`, `Homepage`, `Upgrade`, `Treasure`, + scene detail |
| `Assets/SpinSquad/Data/`, `Resources/` | Unit catalog, unit `.asset`, theme/palette |
| `Assets/SpinSquad/README.md`, `Assets/SpinSquad/Docs/` | Cấy trúc & spec ally/enemy/meta + `AI_CONTEXT/SYSTEM_MAP.md` |

**Thêm:** `Assets/Spine/` là runtime Spine bên thứ ba.

Tóm lại khác khúc §2 **cũ** (đã lỗi thời): repo **đã có** rollout gacha 6-slot trong trận, merge/stack ally, progression lưu `PlayerPrefs`, và campaign wave — xem **`AGENT_CONTEXT_SUMMARY.md`**, **`SPINSQUAD_LOGIC_ROADMAP.md`**, và **`SYSTEM_MAP.md`**.

### 2.3 Đánh giá trạng thái project

| Tiêu chí | Mức độ | Ghi chú |
|-----------|--------|---------|
| **Mức độ hoàn thiện gameplay** | Prototype/playable trong Editor | Duel + prep + roll + meta screens; không phải sản phẩm parity với Spin Squad!. |
| **Khớp với game tham chiếu** | Theo **concept loop** được doc (roll → merge/stack → combat → meta); IP/UI/asset gốc **không** copy. |
| **Sẵn sàng mở rộng** | Cao | Kiến trúc đã tách Data / Gacha / Meta / Duel; roadmap giai đoạn trong `SPINSQUAD_LOGIC_ROADMAP.md`. |
| **Rủi ro / lưu ý** | Cache build | `Library`, `Temp`, `Logs` thường không commit; clone lần đầu cần mở Unity để tái `.meta` nếu thiếu. |

**Kết luận:** Đây là **prototype Unity có đầy đủ lớp chính** (combat duel, máy roll 6 ô, merge/stack, persistence meta). Việc còn lại chủ yếu là chỉnh content, UX, balance, pity/banner riêng, và feature nằm ngoài scope hiện tại của roadmap — không còn mô tả “chưa có Scripts”.

---

## 3. Phân biệt rõ ràng

- **Spin Squad!** (app store): sản phẩm thương mại của 111%, có gacha/loot box và meta phức tạp.
- **Repo clone Unity (workspace này):** học/cải tiến theo **thiết kế** tự quy định; một phần spec tham chiếu game gốc nằm trong các file `.md` — **triển khai code và asset là riêng** của project.

Để chỉnh tiếp (balance, pity, IAP, multiplayer…), nối vào các entry đã có (`DuelDirector`, `RollGachaSession`, `MetaProgressionStore`) và spec trong `Assets/SpinSquad/Docs/`.
