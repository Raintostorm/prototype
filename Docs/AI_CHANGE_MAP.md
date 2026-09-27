# SpinSquadClone — AI Change Map

Đọc file này trước khi sửa. Chỉ mở các file trong nhánh phù hợp; không quét toàn bộ `Assets` hoặc đoán asset theo tên.

**Quyết định hiện hành (2026-09-27): Unity 2D, C#.** Đọc `PROJECT_DIRECTION.md`; các thử nghiệm 3D trước đây không còn là hướng triển khai.

## 1. Battle: starter, vị trí, hướng, animation

| Mục cần sửa | File cần mở trước | Ghi chú |
|---|---|---|
| Starter Wave 1 / tiền roll | `Assets/SpinSquad/Scripts/Runtime/Gameplay/DuelDirector.cs` | `TrySpawnStarterTestAllies`; hiện chỉ spawn một Kim Epic. Mapping prefab Kim ở `TryResolveBattlePrefabSource`. |
| Spawn, căn giữa X, baseline Y | `Assets/SpinSquad/Scripts/Runtime/Gameplay/DuelDirector.cs` | `CreateRigFighter`, `AlignRigVisualToAnchor`, `TryAlignVisualBySpineSetupPose`. Không thêm offset thủ công trước khi kiểm tra bounds. |
| Flip lúc di chuyển/đánh | `Assets/SpinSquad/Scripts/Runtime/Gameplay/SpineBattleAnimator.cs` | Đây là nơi duy nhất thực hiện facing runtime bằng `Skeleton.ScaleX`. |
| Clip và convention của từng rig | `Assets/SpinSquad/Scripts/Runtime/Gameplay/SpineBattleRigProfile.cs` | `invertFacing` là một tầng flip duy nhất; `anchorOffset` chỉ là tinh chỉnh sau auto-align. |
| Bộ character mới | `Docs/NEW_2D_CHARACTER_SET.md` | Chưa có asset/prefab gameplay. Mọi unit dùng fallback hình học cho tới khi bộ mới được duyệt. |
| Knight tham khảo | `Assets/SpinSquad/Data/Spine/Knight/`, `Resources/Battle/KnightBattleVisual.prefab`, `Scenes/KnightSpineAnimTest.unity` | Chỉ tham khảo timing/chuyển động; không map vào UnitDefinition. |
| Driver dùng chung | `IBattleVisualDriver.cs`, `SpineBattleAnimator.cs` | Giữ hợp đồng C# và driver Knight; không phục hồi driver chuyên biệt cũ. |

## 2. Quy trình kiểm tra battle bắt buộc

1. Dựng và duyệt character trong Character Lab riêng.
2. Kiểm tra idle, walk, attack, hit, die và skill nếu có; Console phải sạch.
3. Chỉ sau khi duyệt mới map prefab vào UnitDefinition.
4. Kiểm tra Game View portrait và thời điểm damage trong battle.

## 3. UI và meta scenes

| Mục | File bắt đầu |
|---|---|
| Theme/button dùng chung | `Assets/SpinSquad/Scripts/Runtime/UI/SpinSquadButtonTheme.cs`, `GeneratedUiAutoSkin.cs` |
| Sprite/icon UI được nạp | `Assets/SpinSquad/Scripts/Runtime/UI/GeneratedUiSprites.cs`, `MetaHomeUiSprites.cs`, `ButtonKitSprites.cs` |
| Home | `Assets/SpinSquad/Scripts/Runtime/Scenes/HomepageHub.cs`, `MetaHudTheme.cs` |
| Upgrade/Treasure | `Assets/SpinSquad/Scripts/Runtime/Scenes/UpgradeSceneController.cs`, `UpgradeDetailSceneController.cs`, `TreasureSceneController.cs`, `TreasureDetailSceneController.cs` |
| Scene chính | `Assets/SpinSquad/Scenes/SampleScene.unity` |

## 4. Asset rules

- Runtime asset nằm trong `Assets/SpinSquad`; file nguồn nén/dữ liệu trích xuất ngoài Unity không giữ trong root project.
- Không xóa asset untracked chỉ vì nó mới; kiểm tra reference/GUID trước.
- Không commit hoặc push nếu người dùng chưa yêu cầu.
- Bối cảnh công việc gần nhất: `Docs/CURRENT_WORK_HANDOFF.md`.
