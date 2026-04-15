# Spec: Máy roll 6 ô (SpinSquadClone)

Tài liệu tổng hợp thiết kế từ hội thoại — dùng làm nguồn chân lý khi implement hoặc khi cần nhắc lại sau khi clear cache.

---

## 1. Tổng quan

| Hạng mục | Nội dung |
|----------|----------|
| **Số ô** | 6 ô mỗi lần roll |
| **Loại ô** | Chỉ 3 loại: **coin**, **ally**, **buff** |
| **Animation** | 6 ô **quay đồng thời**, **dừng cùng lúc** |
| **UX** | Sau khi dừng, cho người chơi **~2 giây** xem kết quả trước bước tiếp theo |
| **Chi phí** | **20 coin** / lần roll; coin này **chỉ dùng để roll** |
| **Jackpot (chúc mừng)** | Khi **6 ô cùng một loại** (6 coin **hoặc** 6 ally **hoặc** 6 buff) — hiếm nên có thông báo chúc mừng |

Không có ô thứ 7, không Mythic trên máy này.

---

## 2. “Độ hiếm” = kích thước cụm (không phải tier từng ô)

Độ hiếm của **phần thưởng** gắn với **số ô cùng loại được tính** cho loại đó:

| Số ô trùng loại (trong cụm được tính) | Độ hiếm |
|--------------------------------------|---------|
| 3 | Common |
| 4 | Rare |
| 5 | Epic |
| 6 | Legendary |

**Cụm 1 hoặc 2 ô** cùng loại: **không tính thưởng** cho loại đó (không đạt ngưỡng).

---

## 3. Luật “loại nào được tính” trên bảng 6 ô

### 3.1 Một loại thắng đa số (không hòa cụm lớn nhất)

Ví dụ: **3 coin + 2 ally + 1 buff** → **coin** thắng → chỉ áp bảng thưởng **coin** với cụm **3 ô** → Common → **15 coin**. Hai ally + một buff **không mang thưởng** lần đó.

Tương tự: **4 coin + 1 ally + 1 buff** → 40 coin (Rare), v.v.

### 3.2 Hòa cụm lớn nhất

Ví dụ: **3 coin + 3 ally** → nhận **cả hai** dòng thưởng tương ứng từng cụm:

- **15 coin** (cụm 3 coin → Common)
- **1 ally bậc trắng** (cụm 3 ally → Common theo bảng ally)

### 3.3 Generator — không có bảng 2–2–2

Thiết kế **đặt giới hạn RNG**: luôn đảm bảo **có ít nhất một loại xuất hiện ≥ 3 ô** (ít nhất một “cụm 3”). Như vậy **2 coin + 2 ally + 2 buff** **không** xảy ra.

---

## 4. Bảng thưởng — Coin

Áp khi **coin** là loại được tính (đa số, hoặc một nhánh trong trường hợp hòa có coin).

| Cụm coin | Coin nhận được |
|----------|----------------|
| 3 | 15 |
| 4 | 40 |
| 5 | 80 |
| 6 | 200 |

---

## 5. Bảng thưởng — Ally

Áp khi **ally** là loại được tính với cụm tương ứng.

| Cụm ally | Bậc ally |
|----------|----------|
| 3 | Trắng |
| 4 | Xanh |
| 5 | Tím |
| 6 | Vàng |

*(Mapping sang Common/Rare/Epic/Legendary nếu cần cho code/data: trắng → xanh → tím → vàng.)*

---

## 6. Bảng thưởng — Buff

### 6.1 Bốn loại status (chỉ một status mỗi lần nhận buff)

Mỗi lần roll ra **buff** hợp lệ (cụm buff được tính), người chơi nhận **một** buff cho **một** trong bốn:

- `%HP`
- `%DMG` (sát thương)
- `%AtkSpeed`
- `%CritChance`

**Chưa chốt trong hội thoại:** chọn trong 4 status là **random đều** hay **trọng số** — khi implement nên ghi rõ trong code/config.

### 6.2 Giá trị theo cụm (bản đơn giản — game gốc có “special” ở 5–6)

| Cụm buff | HP | DMG | AtkSpeed | Crit |
|----------|-----|-----|----------|------|
| 3 | 10% | 10% | 5% | 1% |
| 4 | 25% | 25% | 15% | 2% |
| 5 | 100% | 100% | 50% | 5% |
| 6 | 200% | 200% | 100% | 15% |

**Game gốc (tham chiếu, chưa làm trong clone):** ở roll buff **5–6** có thể là phần thưởng **đặc biệt** (vd. 2 ally Legend, +1 ô hồi máu liên tục đến hết trận, nâng bậc toàn ally đang có, …). **Hiện tại clone:** chỉ dùng bảng % như trên.

### 6.3 Phạm vi buff (từ hội thoại trước)

- Buff áp cho **cả level** đang chơi; level có **nhiều wave**.
- Hiệu lực: từ **wave lúc roll được buff** đến **hết wave cuối** của level đó.

---

## 7. Checklist implement (gợi ý)

- [ ] Trừ 20 coin trước khi roll (hoặc rollback nếu không đủ).
- [ ] RNG 6 ô với **constraint** “≥1 loại có count ≥ 3”.
- [ ] Giải quyết đa số / hòa → danh sách **dòng thưởng** (coin + ally + buff có thể nhiều dòng khi hòa).
- [ ] Map cụm 3/4/5/6 → Common/Rare/Epic/Legendary + UI jackpot khi **6/6 cùng loại**.
- [ ] Pause ~2s sau khi 6 ô dừng.
- [ ] Buff: một `BuffRollOutcome` (stat + %) + áp dụng vào combat level theo wave rule ở §6.3.
- [ ] Chốt quy tắc chọn 1 trong 4 status (đều vs weight).

---

## 8. Liên quan repo khác

- Lộ trình tổng: `SPINSQUAD_LOGIC_ROADMAP.md`
- Tham chiếu game thương mại: `SPINSQUAD_TONG_HOP.md`
- Merge ally (3→1, stack): `MERGE_ALLY_SPEC.md`

---

*Tài liệu này phản ánh spec đã thống nhất trong chat; nếu thay đổi thiết kế, sửa trực tiếp file này làm nguồn mới nhất.*
