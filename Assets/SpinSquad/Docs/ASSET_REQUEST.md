# SpinSquad — Brief asset cho Art / Audio

Tài liệu liệt kê asset **cần có hoặc cần nâng cấp** cho build hiện tại (**Unity 6**, mobile dọc **~1080×1920**). Đường dẫn trong doc là **trong repo Unity** (đối chiếu chỗ đã có placeholder).

---

## Đọc nhanh

| | |
|:---|:---|
| **Repo / gốc project** | Thư mục clone (ví dụ `SpinSquadClone/`): cạnh `Assets/` có `allies/` và `Tools/`. |
| **Ai chạy Unity / script** | Art có thể chỉ gửi file export; **dev** (hoặc người có project) chạy `./Tools/sync_allies_from_root.sh` rồi mở Unity **Refresh/Reimport**. |
| **Ally battle Spine** | **Một bộ skeleton / atlas cho mỗi màu** (Mộc/Hỏa/Kim/Thủy), không tách theo Rare/Epic trong pipeline hiện tại — chi tiết **§1.1**. |
| **Knight (dòng 5)** | Spine riêng trong `Data/Spine/Knight/`, **không** đồng bộ từ `allies/*_spine`. |

---

## Mục lục

| § | Nội dung |
|---|----------|
| **0** | [Đồng bộ `allies/` → Unity](#0-đồng-bộ-allies--unity) |
| **1** | [Spine ally battle](#1-spine--ally-battle-ngũ-hành--tướng) |
| **2** | [Enemy](#2-spine--sprite--enemy) |
| **3** | [VFX](#3-vfx-2d--particle) |
| **4** | [UI global / HUD](#4-ui--global-mobile-hud) — registry repo: [`AI_CONTEXT/UI_HUD_ASSET_MAP.md`](AI_CONTEXT/UI_HUD_ASSET_MAP.md) |
| **5** | [UI card / meta](#5-ui--card--meta-upgrade-treasure) |
| **6** | [Âm thanh](#6-âm-thanh) |
| **7** | [Font](#7-font--localization) |
| **8** | [Marketing](#8-marketing--store-tuỳ-scope) |
| **Phụ lục** | [Data / Scene](#phụ-lục-a--file-data--catalog) · [Giao việc](#giao-việc-checklist) |

---

## 0. Đồng bộ `allies/` → Unity

### 0.1 Hai cách giao file

| Cách | Việc của Art | Việc của Dev / người có repo |
|:-----|:-------------|:-------------------------------|
| **A — Khuyến nghị** | Đặt export vào đúng folder **`allies/blue_spine/`**, `green_spine/`, `red_spine/`, `white_spine/` (tên file chuẩn bên dưới). | Từ **gốc repo**, chạy `./Tools/sync_allies_from_root.sh`, mở Unity → Refresh / để Spine reimport. |
| **B** | Gửi zip / Drive; dev giải nén vào `allies/*_spine/` rồi làm như A. | Giống A. |

**File tối thiểu mỗi màu:** `skeleton.json`, `skeleton.atlas.txt`, `skeleton.png`  
**Thủy thêm:** `projectile.PNG` (copy sang `Resources/Vfx/AllyThuyProjectile.png` khi sync).

Script: [`Tools/sync_allies_from_root.sh`](../../../Tools/sync_allies_from_root.sh).

### 0.2 Map thư mục (nguồn → đích trong Unity)

| Nguồn (`allies/`) | Đích (`Assets/SpinSquad/...`) |
|:------------------|:-------------------------------|
| `blue_spine/` | [`Data/Spine/Allies/Blue/`](Assets/SpinSquad/Data/Spine/Allies/Blue/) |
| `green_spine/` | [`Data/Spine/Allies/Green/`](Assets/SpinSquad/Data/Spine/Allies/Green/) |
| `red_spine/` | [`Data/Spine/Allies/Red/`](Assets/SpinSquad/Data/Spine/Allies/Red/) |
| `white_spine/` | [`Data/Spine/Allies/White/`](Assets/SpinSquad/Data/Spine/Allies/White/) |

Sau sync: **giữ** `.meta` và `skeleton_SkeletonData.asset` có sẵn trong từng folder; script chỉ ghi đè JSON / atlas / PNG nguồn.

### 0.3 Một lệnh (từ gốc repo)

```bash
./Tools/sync_allies_from_root.sh
```

### 0.4 Chuẩn hóa `images` trong JSON

Export Spine đôi khi để `"images":"./<uuid>/"`. Script **ép** lần đầu `"images"` → `"./"` để khớp **một atlas + `skeleton.png` cùng thư mục**. Nếu cố ý tách ảnh ra subfolder — báo dev để **không** chạy bước đó hoặc sửa script.

### 0.5 File `.zip` trong `allies/`

Zip chỉ là bản đóng gói; **nguồn chuẩn** là thư mục `*_spine/` đã giải nén.

### 0.6 Knight (Thổ / dòng 5)

`common_melee` / Knight: [`Assets/SpinSquad/Data/Spine/Knight/`](Assets/SpinSquad/Data/Spine/Knight/) — **không** thuộc luồng `allies/*_spine`.

---

## 1. Spine — Ally battle (ngũ hành + tướng)

### 1.1 Bậc hiếm (Rare…) so với hình ảnh

| Khía cạnh | Tình trạng hiện tại |
|:----------|:--------------------|
| **Combat** | Chỉ số theo bậc (Common → Rare → …) đã scale trong game (merge, roll, v.v.). |
| **Một nhân vật / một Spine cho mỗi màu** | Đủ cho **toàn bộ** bậc hiếm trong battle — không yêu cầu **skin Spine riêng** cho Rare/Epic/Legendary **trong brief này**. |
| **UI Upgrade (card / portrait)** | Màn hình meta có **ô theo rarity**; có thể dùng **cùng portrait**, chỉ đổi viền/màu theo bậc. Nếu sau này muốn **ảnh riêng từng bậc** → scope riêng + chỉnh code/catalog (hiện **chưa** bắt buộc). |

### 1.2 Bảng ally — đường dẫn trong repo

| Mục | Mô tả kỹ thuật | Spine + prefab battle |
|:----|:---------------|:------------------------|
| **Mộc** (xanh lá) | Skeleton **3.8**, atlas PNG, clip idle / walk / attack / died; pivot chân khớp lưới | [`Data/Spine/Allies/Green/`](Assets/SpinSquad/Data/Spine/Allies/Green/) · [`Resources/Battle/GreenAllyBattleVisual`](Assets/SpinSquad/Resources/Battle/GreenAllyBattleVisual.prefab) |
| **Hỏa** (đỏ) | Như trên | [`Data/Spine/Allies/Red/`](Assets/SpinSquad/Data/Spine/Allies/Red/) · [`RedAllyBattleVisual`](Assets/SpinSquad/Resources/Battle/RedAllyBattleVisual.prefab) |
| **Kim** (trắng) | Như trên | [`Data/Spine/Allies/White/`](Assets/SpinSquad/Data/Spine/Allies/White/) · [`WhiteAllyBattleVisual`](Assets/SpinSquad/Resources/Battle/WhiteAllyBattleVisual.prefab) |
| **Thủy** (xanh dương, ranged) | Như trên + staff / anim bắn | [`Data/Spine/Allies/Blue/`](Assets/SpinSquad/Data/Spine/Allies/Blue/) · [`BlueAllyBattleVisual`](Assets/SpinSquad/Resources/Battle/BlueAllyBattleVisual.prefab) |
| **Knight / melee chung** | Spine knight hoặc tương đương | [`Data/Spine/Knight/`](Assets/SpinSquad/Data/Spine/Knight/) · [`KnightBattleVisual`](Assets/SpinSquad/Resources/Battle/KnightBattleVisual.prefab) |

Nguồn đặt ngoài `Assets/` sync vào các đường trên qua **mục 0** (4 màu ngũ hành).

**Export:** PNG atlas (không nén quá mạnh), tên region ổn định; JSON **Spine 3.8.x**, tương thích **spine-unity**; khung an toàn để scale root battle ~**0.08** world; event **`hit`** trên clip attack nếu sync VFX.

---

## 2. Spine / sprite — Enemy

| Mục | Mô tả | Tham chiếu repo |
|:----|:------|:----------------|
| Melee (oracle / moss…) | Body + anim tối thiểu như ally | [`Data/Units/unit_moss_oracle.asset`](Assets/SpinSquad/Data/Units/unit_moss_oracle.asset) + catalog |
| Ranged (tam giác / sniper) | Pose ranged + đạn riêng nếu có | [`unit_enemy_ranged.asset`](Assets/SpinSquad/Data/Units/unit_enemy_ranged.asset) |
| Knight / boss test | Prefab battle nếu dùng Spine | [`unit_enemy_knight.asset`](Assets/SpinSquad/Data/Units/unit_enemy_knight.asset) |

---

## 3. VFX (2D / particle)

| Mục | Chi tiết | Repo |
|:----|:---------|:-----|
| Đạn ranged Thủy | Texture / Sprite `Resources` | [`Resources/Vfx/AllyThuyProjectile.png`](Assets/SpinSquad/Resources/Vfx/AllyThuyProjectile.png) |
| Hit / số damage | Style nhất quán | Có thể bổ sung cho [`FloatingDamagePopup`](Assets/SpinSquad/Scripts/Core/FloatingDamagePopup.cs) |
| Merge ally | Pop nhẹ, glow ô | Chưa có asset riêng — cần concept |
| Roll gacha | Khung 6 ô, highlight jackpot | Runtime trong [`DuelDirector`](Assets/SpinSquad/Scripts/Core/DuelDirector.cs) — có thể thay sprite khung |

---

## 4. UI — Global (mobile HUD)

**Layout tham chiếu:** `CanvasScaler` **1080×1920**, safe area ~**36px** ngang, ~**40px** trên — [`MetaHudTheme`](Assets/SpinSquad/Scripts/Scenes/MetaHudTheme.cs).

| Loại asset | Kích thước / ghi chú | Trạng thái code |
|:-----------|:---------------------|:----------------|
| CTA lớn (Play, Upgrade, Treasure) | ~520×96 | `Image` + màu flat |
| Back / Muted | ~320×80 | Nhiều scene |
| Icon bar (Pause / Set / Shop / Thư) | ~88×76 (`IconBarButtonSize`) | Đang **chữ trên nền** — cần **PNG** (pressed/disabled) |
| Panel pause | Dim fullscreen + panel ~560×280 | Có thể 9-slice |
| Panel settings | ~520×320 | Placeholder |
| Header / backdrop | Full bleed | [`MetaHudTheme.AddFullScreenBackdrop`](Assets/SpinSquad/Scripts/Scenes/MetaHudTheme.cs) |
| Menu ô ally (merge / bán) | ~320×220 | Có thể skin |
| Health bar trên đầu unit | World space | [`WorldUnitHealthBar`](Assets/SpinSquad/Scripts/Core/WorldUnitHealthBar.cs) |

**Stub (log “sắp có”):** Shop, Thư, Set trên Homepage; x2 tốc độ Duel (disabled).

---

## 5. UI — Card / meta (Upgrade, Treasure)

| Mục | Ghi chú |
|:----|:--------|
| Thẻ upgrade (dòng × rarity) | Màu có thể align `RarityPalette` / `FootGroundRingColor` |
| Icon kho báu | `Resources/UI/Treasure/Icons/` — 25 icon (10/7/4/4), `TreasureUiSprites.cs`, sync `Tools/sync_treasures_from_root.sh` |
| Tab rarity Treasure | HUD `All_tab` … `Material_tab` (wired) |
| Overlay chọn level | Homepage `LevelSelectOverlay` |

---

## 6. Âm thanh

| Mục | Gợi ý |
|:----|:------|
| BGM menu | OGG/WAV, loop ~30–60s seamless |
| BGM combat | Nhẹ, không át SFX |
| SFX nút | <0.3s |
| SFX ranged / hit / death | Tách theo faction |
| SFX win wave / lose | 1 file / loại |

**Lưu ý:** chưa có pipeline `AudioSource` tập trung — code gắn sau. Đặt file đề xuất vào `Assets/SpinSquad/Audio/` (tạo khi import).

---

## 7. Font & localization

| Mục | Ghi chú |
|:----|:--------|
| Font chính | **Tiếng Việt** + Latin; hiện UI dùng built-in Legacy |
| TMP (sau) | Font asset + fallback; outline optional |

---

## 8. Marketing / store (tuỳ scope)

- Icon app **1024×1024**
- Splash / key art **16:9** + crop **9:16**
- Ảnh store (nếu publish)

---

## Phụ lục A — File data / catalog

| File | Vai trò |
|:-----|:--------|
| [`Resources/UnitCatalog_Main.asset`](Assets/SpinSquad/Resources/UnitCatalog_Main.asset) | Danh sách `UnitDefinition` |
| [`Data/Units/*.asset`](Assets/SpinSquad/Data/Units/) | Định nghĩa unit (prefab, portrait, ranged…) |
| [`Scripts/Data/AllyLineCatalog.cs`](Assets/SpinSquad/Scripts/Data/AllyLineCatalog.cs) | Map 5 dòng ally |

---

## Phụ lục B — Scene Unity

| Scene | Vai trò |
|:------|:--------|
| [`SampleScene`](Assets/SpinSquad/Scenes/SampleScene.unity) | Campaign / duel chính |
| [`Homepage`](Assets/SpinSquad/Scenes/Homepage.unity) | Hub meta |
| [`Upgrade`](Assets/SpinSquad/Scenes/Upgrade.unity), [`UpgradeDetail`](Assets/SpinSquad/Scenes/UpgradeDetail.unity) | Nâng cấp |
| [`Treasure`](Assets/SpinSquad/Scenes/Treasure.unity), [`TreasureDetail`](Assets/SpinSquad/Scenes/TreasureDetail.unity) | Kho báu |
| [`DuelTestArena`](Assets/SpinSquad/Scenes/DuelTestArena.unity), [`KnightSpineAnimTest`](Assets/SpinSquad/Scenes/KnightSpineAnimTest.unity) | Test |

---

## Giao việc (checklist)

### Art

1. Icon HUD (pause, settings, shop, mail, tốc độ): PNG trong suốt + **normal / pressed / disabled** nếu có.  
2. 9-slice: panel pause, settings, khung roll.  
3. Spine: **4 ally màu** + enemy **tối thiểu** melee + ranged.  
4. VFX: đạn Thủy + hit nhẹ (tuỳ scope).  

### Audio

5. Pack prototype: BGM menu + combat ngắn, SFX nút / bắn / hit / death / win–lose — kèm **bảng tên file ↔ event** để code gắn sau.

### Dev (khi nhận file từ Art)

6. Giải nén vào `allies/*_spine/` (hoặc nhận đúng cấu trúc), chạy `./Tools/sync_allies_from_root.sh`, Unity **Refresh / Reimport** Spine.

---

*Cập nhật file này khi scope thay đổi (PvP, shop thật, portrait theo bậc hiếm, v.v.).*
