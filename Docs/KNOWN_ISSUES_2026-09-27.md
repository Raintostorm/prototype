# Known issues — 2026-09-27

## Trạng thái hiện tại

Project đã chốt Unity 2D và C#. Toàn bộ bộ character animation cũ đã được gỡ; Knight được giữ riêng để tham khảo animation và không còn được map vào unit gameplay.

Runtime C# và Editor C# biên dịch thành công bằng compiler đi kèm Unity 6000.4.1f1. Không còn GUID trong project trỏ tới 668 asset legacy đã loại bỏ.

## Vấn đề đang mở

1. **Chưa có bộ character production mới.** Ally và enemy hiện rơi về hình học fallback. Đây là trạng thái chuyển tiếp có chủ ý nhưng chất lượng hình ảnh trong battle chưa đại diện cho game cuối.
2. **Chưa chốt pipeline animation 2D.** Cần chọn Sprite Frame hoặc Unity 2D cutout sau một vertical slice có thể so sánh; không nhập hàng loạt asset trước quyết định này.
3. **Chưa có Character Lab mới.** Bộ mới cần scene duyệt riêng cho Idle, Walk, Attack, Hit, Die và Skill trước khi được map vào battle.
4. **Chưa chạy xác nhận Play Mode đầy đủ sau reset.** Lần chạy batch Play Mode trước bị Unity Licensing timeout/handshake; hiện mới xác nhận bằng compile runtime và Editor.
5. **Knight chỉ là motion reference.** Style, UI và model Knight không phù hợp với hướng mỹ thuật mới. Chỉ tham khảo timing, anticipation, impact, recovery và death hold.
6. **Battle không còn tự gắn controller sprite legacy.** Unit không có `IBattleVisualDriver` vẫn gây damage nhờ fallback trong `DuelActor`, nhưng sẽ không có animation cho tới khi prefab mới cung cấp driver.
7. **Cảnh báo compile cũ còn tồn tại.** `BattlePresentation25D` và `GeneratedUiAutoSkin` dùng một số API Unity đã đánh dấu obsolete; `DuelDirector` có hai trường healing chưa được đọc. Chúng chưa chặn compile.
8. **Cần kiểm tra lại UI và scene bằng Game View.** Worktree có các thay đổi UI/gameplay trước đợt reset; compile không thay thế kiểm tra trực quan trên iPhone X portrait.

## Điều kiện để đóng các vấn đề animation

- Style sheet chung cho năm chủng tộc và bốn rarity được duyệt.
- Một character vertical slice đạt chuẩn mobile trong Character Lab.
- Attack phát damage đúng một lần tại impact; Hit không cắt callback; Die giữ pose cuối.
- Prefab mới không tham chiếu asset/controller legacy.
- Battle và UI được kiểm tra trực quan trong Play Mode, sau đó mới mở rộng thành cả bộ.

Baseline cho bộ mới: [NEW_2D_CHARACTER_SET.md](NEW_2D_CHARACTER_SET.md).
