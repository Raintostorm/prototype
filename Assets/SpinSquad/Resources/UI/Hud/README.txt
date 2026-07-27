HUD sprites (Resources — load at runtime via HudUiSprites.cs)

  Meta/    — pack gốc button_icon (settings, mail, tabs, ngũ hành, …) — 55 PNG
  Combat/  — pack button_add (pause, x2, merge, sell, shop, …) — 13 PNG

Không dùng: Background.PNG (Meta), Background_combat.PNG (Combat) — thừa trong pack, UI nền = màu flat MetaHudTheme.

Gốc repo button_icon/ và button_add/ đã xóa sau khi import.

Nếu Console báo "Không load được sprite": menu SpinSquad → UI → Reimport HUD Sprites (fix Resources.Load), rồi Validate lại.

Unity: Assets → Refresh. Menu SpinSquad → UI → Validate HUD Sprites (Resources) để kiểm tra load.

Play Homepage / SampleScene để thấy UI (Canvas tạo lúc Play, không có trong Hierarchy khi Edit).

Gán thêm icon: thêm PNG vào Meta hoặc Combat, bổ sung property trong HudUiSprites.cs.
