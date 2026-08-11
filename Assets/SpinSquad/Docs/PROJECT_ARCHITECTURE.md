# SpinSquad project architecture

## Mục tiêu

Cấu trúc này tách rõ **game rule**, **presentation**, **scene composition**, **UI**, và **editor tooling**. Một người mới có thể tìm đúng nơi cần sửa mà không phải đọc toàn bộ project.

## Luồng phụ thuộc

```text
Data / Meta / Gacha
        ↓
     Gameplay
        ↓
  Presentation + UI
        ↓
      Scenes

Editor tools → có thể đọc Runtime
Runtime      → không được tham chiếu Editor tools
```

## Nơi đặt code

| Loại thay đổi | Folder |
|---|---|
| Luật battle, HP, targeting, grid, merge, unit lifecycle | `Scripts/Runtime/Gameplay` |
| Roll, reward, wallet, buff theo run | `Scripts/Runtime/Gacha` |
| Save, unlock, progression ngoài trận | `Scripts/Runtime/Meta` |
| Unit definition, rarity, catalog | `Scripts/Runtime/Data` |
| Camera shake, backdrop, visual 2.5D | `Scripts/Runtime/Presentation` |
| Widget/theme/sprite provider dùng chung | `Scripts/Runtime/UI` |
| Dựng và nối một scene cụ thể | `Scripts/Runtime/Scenes` |
| Import, validate, generate asset trong Editor | `Scripts/Editor` |
| Automated checks | `Tests/PlayMode` hoặc `Tests/EditMode` |

## Nơi đặt asset

| Asset | Folder chuẩn |
|---|---|
| Sprite nguồn, atlas, UI master | `Art/` |
| Runtime-loaded sprite bắt buộc dùng `Resources.Load` | `Resources/` |
| ScriptableObject/cấu hình game | `Data/` |
| Prefab | `Prefabs/Gameplay`, `Prefabs/Battle`, hoặc `Prefabs/UI` |
| Asset bên thứ ba chưa chỉnh sửa | `ThirdParty/<PackageName>` |
| Tài liệu kỹ thuật và game design | `Docs/` |

Không sao chép cùng một asset vào nhiều nơi. Nếu bắt buộc có bản nguồn và bản runtime, ghi rõ hậu tố `Source`/`Runtime` hoặc mô tả pipeline trong `Docs`.

## Điểm bắt đầu theo tính năng

- Battle loop: `Scripts/Runtime/Gameplay/DuelDirector.cs`
- Unit data: `Scripts/Runtime/Data/UnitDefinition.cs`
- Enemy animation: `Scripts/Runtime/Gameplay/AnimalKitEnemyBattleAnimator.cs`
- Homepage: `Scripts/Runtime/Scenes/HomepageHub.cs`
- UI theme: `Scripts/Runtime/Scenes/MetaHudTheme.cs`
- Shared UI art: `Scripts/Runtime/UI/MetaHomeUiSprites.cs`
- Generated button fallback: `Scripts/Runtime/UI/GeneratedUiAutoSkin.cs`
- Meta progression: `Scripts/Runtime/Meta/MetaProgressionStore.cs`

## Quy tắc UI

1. Mọi màn hình dùng reference resolution `1080 × 1920` và `SafeAreaFitter`.
2. Nút có thiết kế riêng giữ sprite riêng; generated button kit chỉ là fallback cho nút chưa được style.
3. Icon không được bake chung với label. Icon và text là child riêng để scale và localize độc lập.
4. Tối thiểu 44 px vùng bấm thực tế; text chính phải đọc được ở Game View scale 0.5.
5. Tất cả text hiển thị cho người chơi dùng tiếng Anh.

## Quy trình thay đổi an toàn

1. Giữ nguyên `.meta` khi chuyển file bằng Unity Project window hoặc chuyển cả file và `.meta` cùng lúc.
2. Chạy compile không giao diện.
3. Chạy asset validator liên quan.
4. Mở Homepage và SampleScene để kiểm tra trực quan ở tỉ lệ iPhone X.
5. Chỉ commit asset nguồn lớn khi quyền sử dụng đã được ghi nhận.
