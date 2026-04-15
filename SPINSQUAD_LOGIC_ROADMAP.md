# Lộ trình: game cùng **concept & logic** với Spin Squad! (UI có thể khác)

Tài liệu này mô tả **trạng thái repo hiện tại** và **những việc cần làm** để tiến tới một sản phẩm có **vòng lặp thiết kế** gần với game tham chiếu (gacha spin → thu thập đơn vị → tiến triển → combat/meta). **Giao diện, tên, nhân vật, asset thương mại** nên là **bộ riêng** của bạn để tránh nhầm lẫn với IP của nhà phát hành gốc.

Tham chiếu nội bộ: `SPINSQUAD_TONG_HOP.md` (tóm tắt game 111% + link store/guide).

---

## 1. Trạng thái project **SpinSquadClone** (đã đọc lại)

| Hạng mục | Thực tế |
|----------|---------|
| **Unity** | 6 — `6000.4.1f1` (`ProjectSettings/ProjectVersion.txt`) |
| **Gói** | 2D Feature, **Input System**, **UGUI**, Timeline, Visual Scripting, Test Framework, … (`Packages/manifest.json`) |
| **Cấu trúc** | Nội dung game dưới `Assets/SpinSquad/` — folder `Scripts/Gacha`, `Scripts/Data`, … đã **đặt tên sẵn**, phần lớn **chưa có logic** |
| **Scene** | `Assets/SpinSquad/Scenes/SampleScene.unity` — có prototype **đấu 1v1** (học tập), **không** phải loop gacha |
| **Code gameplay** | `Scripts/Core/`: `CombatHealth`, `DuelActor`, `DuelDirector` — hai phe chạy vào nhau, thắng/thua theo faction |
| **Code khác** | `Scripts/UI/UiButtonDemo.cs` — demo nút log; `Scripts/Editor/GameViewIPhoneXOnPlay.cs` — preset Game view mobile |
| **Gacha / Guardian / merge / chapter** | **Chưa có** implementation tương ứng |

**Kết luận:** Repo là **nền Unity 6 + khung thư mục + prototype combat tối giản**. Để “giống logic” game tham chiếu, cần xây **các hệ thống** dưới đây theo từng giai đoạn.

---

## 2. Game tham chiếu — **logic & concept** cần bám (tóm tắt)

*(Nguồn: mô tả store / cộng đồng — không phải source code chính thức.)*

1. **Spin / roll (gacha)** — tiêu tốn tài nguyên (energy, currency), RNG ra **đơn vị** theo **độ hiếm**.
2. **Đơn vị (Guardian-like)** — id, rarity, role/trait/synergy (phục vụ team build).
3. **Tiến triển** — level / star / duplicate → power; **merge** hướng tier cao (vd. Mythic trong meta game gốc).
4. **Đội hình** — slot cố định, giới hạn bản sao, bonus synergy.
5. **Combat PvE** — chapter / stage / boss; thắng thua cho phần thưởng & tiến độ.
6. **Meta ngoài trận** — inventory, shop/IAP (tuỳ scope), battle pass / sự kiện (tuỳ scope).
7. **Monetization kiểu gacha** (tuỳ bạn có làm hay không) — pity, rate table minh bạch nếu phát hành thật.

**UI có thể khác toàn bộ:** chỉ cần **cùng kiểu thông tin** (tỉ lệ, kết quả roll, đội, combat HUD) được trình bày theo style riêng.

---

## 3. Kiến trúc đề xuất (tách **data — logic — presentation**)

| Lớp | Nội dung | Gợi ý trong repo |
|-----|----------|------------------|
| **Data** | Định nghĩa đơn vị, pool gacha, bảng tỉ lệ, chapter | `Assets/SpinSquad/Scripts/Data/` + `ScriptableObject` / JSON tuỳ bạn |
| **Domain / logic** | Roll RNG, pity, inventory, merge rules, combat resolver | `Scripts/Gacha/`, `Scripts/Core/` hoặc thêm `Scripts/Combat/`, `Scripts/Progression/` |
| **Application** | Luồng màn hình: Hub → Spin → Kết quả → Team → Stage | `Scripts/UI/` + scene flow (có thể thêm `Scripts/App/` nếu cần) |
| **Presentation** | UGUI / animation / VFX — **thay đổi tự do** | Prefab dưới `Prefabs/UI/`, art dưới `Art/` |

Prototype `DuelDirector` hiện tại có thể **thay** bằng combat thật (wave, skill, stats) hoặc **giữ** làm scene test nội bộ.

---

## 4. Việc cần làm — theo **giai đoạn** (khả thi, có thứ tự)

### Giai đoạn A — Nền dữ liệu & từ vựng game

- [x] **Đã có trong repo:** `Rarity`, `UnitDefinition`, `UnitCatalog`, `UnitTeamKind`, `RarityPalette` trong [`Assets/SpinSquad/Scripts/Data/`](Assets/SpinSquad/Scripts/Data/), catalog mẫu [`Assets/SpinSquad/Resources/UnitCatalog_Main.asset`](Assets/SpinSquad/Resources/UnitCatalog_Main.asset) (load qua `Resources.Load("UnitCatalog_Main")`) và 3 unit dưới `Data/Units/`. Editor: **SpinSquad/Data/Generate Sample Unit Data**, **SpinSquad/Data/Verify Unit Catalog Resolution**.
- [ ] Mở rộng **UnitDefinition**: role/tags, icon, skill (ngoài `maxHitPoints` + `attack` hiện tại).
- [ ] **Pool / banner gacha**: SO hoặc table weight theo rarity, tham chiếu `UnitCatalog` hoặc subset.
- [ ] **Lưu tiến độ**: `PlayerPrefs` prototype → sau đó file JSON / cloud tuỳ mục tiêu phát hành.

### Giai đoạn B — Gacha (trái tim “spin”)

- [ ] **Currency / energy**: số nguyên, max energy, hồi theo thời gian (có thể đơn giản hóa lúc đầu).
- [ ] **RNG roll**: weighted random theo pool; seed tuỳ chọn (debug).
- [ ] **Pity** (soft/hard): đếm roll không ra epic+ → tăng weight hoặc bảo đảm lần thứ N.
- [ ] **Kết quả roll**: thêm vào inventory (duplicate → shard / material merge).
- [ ] **UI spin**: nút, animation quay (có thể fake tween), popup kết quả — **layout khác hẳn game gốc vẫn được**.

### Giai đoạn C — Collection & merge

- [ ] **Inventory / collection screen**: lọc theo rarity, xem chi tiết đơn vị.
- [ ] **Quy tắc merge**: input 2 (hoặc N) id + material → output id; validate server-side nếu sau này có online cheat concern (offline thì client đủ).
- [ ] **Tier sau merge**: cập nhật stats / skill unlock.

### Giai đoạn D — Team & synergy

- [ ] **Formation**: k slot (vd. 5), kéo thả hoặc chọn từ list (Input System + UI).
- [ ] **Synergy table**: tag A + tag B → bonus cụ thể (% atk, shield, …).
- [ ] **Power snapshot**: tính combat power để sort / gợi ý (optional).

### Giai đoạn E — Combat PvE (logic có thể giống “stage + boss”)

- [ ] **Stage data**: waves, enemy stats, reward table.
- [ ] **Combat loop**: turn-based hoặc real-time nhỏ — quyết định sớm (ảnh hưởng code nhiều).
- [ ] **Win/Lose**: điều kiện (boss chết / ally wipe), phần thưởng, mở stage tiếp.
- [ ] Thay hoặc mở rộng prototype `DuelDirector` thành **BattleController** tổng quát.

### Giai đoạn F — Meta & chỉnh balance (khi core đã chạy)

- [ ] Chapter / difficulty curve.
- [ ] Shop / gói quay (optional).
- [ ] Analytics local (log) trước khi gắn dịch vụ ngoài.
- [ ] Test tự động cho RNG distribution (nhiều lần roll, assert histogram trong ngưỡng).

### Giai đoạn G — Phát hành (nếu có ý định lên store)

- [ ] **Không** dùng tên nhân vật / asset / UI copy từ game thương mại.
- [ ] Tuân luật **loot box / gambling** theo khu vực (nhãn store, tỉ lệ công bố).
- [ ] Privacy / IAP theo platform.

---

## 5. Gói Unity có thể bổ sung (không bắt buộc ngay)

- **Addressables** — nếu nhiều asset đơn vị / bundle theo sự kiện.
- **Localization** — nếu đa ngôn ngữ.
- **Unity Gaming Services / backend** — chỉ khi cần đồng bộ server; prototype offline không cần.

---

## 6. Milestone đề xuất (đo được)

| Milestone | Tiêu chí “xong” |
|-----------|------------------|
| **M1** | Một lần bấm Spin → log ra đơn vị + rarity đúng pool + trừ currency |
| **M2** | Inventory hiển thị đơn vị đã trúng; duplicate tăng “shard” |
| **M3** | Một luật merge chạy được end-to-end |
| **M4** | Ghép đội k slot → vào một stage → thắng/thua có reward |
| **M5** | Nhiều stage + boss đơn giản + curve reward |

---

## 7. Liên kết tham khảo ngoài repo

- [Spin Squad! — App Store (US)](https://apps.apple.com/us/app/spin-squad/id6511224968) — mô tả tính năng (không dùng để copy asset).
- [Tier list / reroll guide (bên thứ ba)](https://progameguides.com/guides/lucky-offense-tier-list-reroll-guide/) — tham khảo meta, không phải spec kỹ thuật chính thức.

---

*Tài liệu này có thể chỉnh lại khi bạn chốt: **mobile-only vs PC**, **real-time vs turn-based**, và **phạm vi monetization**. Mỗi lần chốt, cập nhật lại mục Giai đoạn E–G cho khớp.*
