# Định hướng chính thức — 2026-09-27

Theo quyết định của người dùng: game là **2D, viết bằng C#, trong Unity**.

- Tiếp tục từ gameplay 2D hiện tại: combat, stats, buff, roll, allies/enemies, coin và save.
- Nhân vật dùng asset và animation 2D; chưa chốt lại kỹ thuật sprite từng frame hay rig 2D. Không tự chuyển sang model/rig 3D.
- Giữ hướng mỹ thuật chibi, màu sáng, dễ đọc trên điện thoại; phân biệt chủng tộc, đánh gần/xa và rarity.
- Phần Character3DCore vừa thử đã gỡ khỏi code, menu Editor và tests. Battle trở về luồng trước thử nghiệm này.
- Toàn bộ asset/prototype animation cũ đã được đưa ra khỏi project. Knight được giữ riêng làm mẫu tham khảo chuyển động và không còn được map vào unit gameplay.
- Các frame H03 trước đây vẫn là bản nháp, chưa phải bộ animation/prefab hoàn chỉnh. Task cũ không tự động tiếp tục; triển khai tiếp theo dựa trên hướng 2D này.
