# Homepage UI Layout

Homepage được dựng runtime bởi `HomepageHub` trên canvas tham chiếu `1080 × 1920`.

## Cấu trúc

- `Backdrop`: nền toàn màn hình, nằm ngoài Safe Area.
- `HeaderStrip`: dải tương phản phía trên.
- `SafeArea`: chứa toàn bộ nút và nội dung tương tác.
- `MetaResourceHud`: Gold, Keys và Energy bên trái.
- `CampaignSelector`: level đang chọn và hai nút chuyển level.
- `BATTLEButton`: CTA duy nhất để mở bảng chọn level.
- `BottomNav`: Home, Upgrade, Treasure và Settings.
- `LevelSelectOverlay`: mặc định tắt, chỉ bật sau khi bấm Battle.

## Nguyên tắc

- Bottom navigation neo đáy với `anchorMin.y = anchorMax.y = 0`.
- Không đặt Battle trong bottom navigation vì đã có CTA chính.
- Icon và label của tab là hai lớp riêng; sprite không được làm ẩn label.
- Debug grant panel không tự mở trên Homepage.
- Tất cả vùng tương tác nằm trong `SafeAreaFitter`.

## Kiểm tra

Trong Unity chọn:

**SpinSquad → Validate → Homepage UI Layout**

Sau đó mở:

**SpinSquad → Play → Open Homepage**

và kiểm tra ở các Game View preset dọc khác nhau.
