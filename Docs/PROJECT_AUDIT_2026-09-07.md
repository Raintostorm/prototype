# SpinSquadClone — audit nền tảng ngày 2026-09-07

## Cập nhật sau audit: người dùng chọn golden character

Người dùng đã xác nhận Knight hoạt động đúng và chọn animation của Knight làm chuẩn tham khảo; UI của Knight không phù hợp game. Quyết định này thay thế trạng thái "ứng viên chưa chốt" trong bản audit ban đầu bên dưới. Nguồn xác nhận là người dùng, không phải một lượt Play Mode mới do agent thực hiện.

Chi tiết reverse-engineering và phạm vi kế thừa: [KNIGHT_GOLDEN_CHARACTER.md](KNIGHT_GOLDEN_CHARACTER.md). Knight là chuẩn tham khảo cấu trúc/animation; tạo hình và UI cho character mới cần phù hợp game. Chưa thay đổi code hoặc asset.

## Phạm vi và trạng thái

- Yêu cầu: audit project trước khi sửa; xác định kiến trúc hiện tại và nhân vật phù hợp làm canonical reference.
- Đã kiểm kê cấu trúc và tham chiếu serialized trong `Assets/`, đọc dependency trong `Packages/`, cấu hình trong `ProjectSettings/`; đọc sâu code character, combat, animation, progression và các tài liệu bàn giao.
- Đối chiếu GUID với `.meta` trong Assets, Packages và Library/PackageCache. Đây là kiểm tra tham chiếu trên đĩa, không thay thế Unity AssetDatabase hoặc kiểm tra subasset fileID.
- Chưa chạy build, compile mới, automated tests hoặc Play Mode; chưa kiểm chứng hình ảnh và chuyển động thực tế.
- Không sửa code, prefab, scene, asset hay cấu hình trong đợt audit. File báo cáo này được tạo sau đó theo yêu cầu người dùng.
- Working tree có nhiều thay đổi và asset chưa track từ trước; báo cáo phản ánh trạng thái đọc được, không phải một commit cố định.
- Đây là audit nền tảng, chưa phải chứng nhận toàn bộ project hoạt động đúng hay đã đọc từng dòng code/kiểm tra từng ảnh.

## Kết quả theo hạng mục

| Hạng mục | Kết quả |
|---|---|
| Unity | `6000.4.1f1`, revision `336a400b9ea2`, theo `ProjectSettings/ProjectVersion.txt`. |
| Render pipeline | Built-in. `GraphicsSettings.m_CustomRenderPipeline` và `customRenderPipeline` của cả 6 Quality đều bằng 0; không có URP/HDRP trong dependency. |
| Input | `com.unity.inputsystem` 1.19.0; `activeInputHandler: 1`. UI dùng InputSystemUIInputModule; PrepGridPointer đọc Mouse và Touchscreen. |
| 2D packages | Animation 14.0.3; PSD Importer 13.0.2; Sprite 1.0.0; Aseprite 4.0.1; SpriteShape 14.0.1; Pixel Perfect 5.1.1. Có package không chứng minh nhân vật đã có SpriteSkin/IK hoạt động. |
| Spine | Runtime trong `Assets/Spine`, package khai báo 3.8.0. Tám skeleton được kiểm tra đều export 3.8.75. |
| Character architecture | Object gốc giữ gameplay; `VisualRig` giữ phần nhìn. DuelActor gọi `IBattleVisualDriver`. Có nhiều driver Spine, Unity Animator, sprite theo frame và prototype tồn tại song song. |
| Prefab | Prefab battle chủ yếu là visual. DuelDirector tạo host và gắn CombatHealth, driver, BoxCollider2D, DuelActor, DuelUnitGridDrag, WorldUnitHealthBar và instance spec khi spawn. |
| Controllers | 11 Animator Controller, 10 Animator Override Controller. Nhánh legacy có Idle/Walk/Attack/Die; Alpha có idle/walk/atk/hit/died; Spine dùng animation state riêng. |
| ScriptableObjects | UnitDefinition, UnitCatalog, SpineBattleAnimConfig; SkeletonData/Atlas và các asset của Spine. UnitCatalog_Main có 23 definition được tham chiếu. Treasure definitions nằm trong code. |
| Combat | Auto-combat thời gian thực; MaxLevels 3, MaxWaves 10. Chọn mục tiêu gần, di chuyển XY, melee/ranged; damage nối callback animation. |
| Stats | UnitDefinition chứa HP, ATK, rarity, phe, tầm đánh, chu kỳ bắn, visual binding. Chỉ số runtime còn qua rarity scaling, meta upgrade, treasure, buff roll và cấu hình test. |
| Sorting | Default và các layer bộ phận cơ thể. Prefab battle chính chủ yếu dùng Default + sortingOrder. UnitPresentation25D có sorting theo Y; tự cài trong Duel25DVerticalSlice, không mặc định cho mọi scene. |
| Physics | Layer bộ phận cơ thể, chưa có layer Ally/Enemy riêng. Ma trận va chạm 2D và 3D bật tất cả. Combat chính tính khoảng cách/đổi Transform; collider hỗ trợ chọn và kéo. Portrait dùng layer 30 chưa đặt tên. |
| Loading | Không thấy Addressables được cài/sử dụng. Chủ yếu Resources.Load; đếm 687 file không tính meta trong các thư mục Resources. |
| Scenes | 12 scene; 9 được bật trong EditorBuildSettings. Nhiều UI/object được dựng runtime. |
| Save | JSON qua PlayerPrefs; key `SpinSquad_MetaProgression_v1`, schema Version 3. Lưu level, vàng, key, energy, nâng cấp, treasure. Roll coin dùng key `SpinSquad_RollCoins`. Chưa thấy cơ chế resume đầy đủ trận đang chơi. |

### Scene trong Build Settings

Homepage, SampleScene, Upgrade, UpgradeDetail, Treasure, TreasureDetail, DuelTestArena, Duel25DVerticalSlice, KnightSpineAnimTest.

Ba scene lab ngoài danh sách build: Alpha3DPrototype, AlphaFrameSheetLab, KayKitRigReference.

### Quy tắc và dữ liệu đáng chú ý

- Roll 6 ô, giá 20 coin; kết quả coin/ally/buff; cluster 3–6 tương ứng Common–Legendary.
- Ally tối đa 1 unit/ô; auto-merge cần 3 cùng line và rarity. Enemy có giới hạn stack riêng là 3.
- Bảng HP/ATK rarity cơ sở: Common 100/15, Rare 300/45, Epic 900/135, Legendary 2700/305, Mythic 8100/915; còn nhân tỉ lệ definition so với mốc 100/15.
- Upgrade hiện là 5 line × 4 rarity, 20 entry; tài liệu cũ nói 12 slot không phản ánh cấu trúc hiện tại.

## Canonical reference đề xuất — Knight, chưa chốt bằng Play Mode

Ứng viên: `Assets/SpinSquad/Resources/Battle/KnightBattleVisual.prefab`.

Lý do chọn làm mốc kỹ thuật:

1. Chuỗi prefab → SkeletonData → JSON/atlas → material → shader/texture đều resolve được bằng GUID.
2. Skeleton có 30 bones, đủ `idle`, `walk`, `attack`, `died`, với dữ liệu chuyển động trên nhiều xương.
3. Có unit definition cho cả ally/enemy trỏ tới prefab này; cấu hình arena dùng các unit Knight.
4. Có scene `KnightSpineAnimTest` chuyên kiểm tra clip.
5. Đi qua nhánh Spine chung, ít ngoại lệ theo tên hơn Kim/Alpha/Croco.

Chuỗi asset chính:

```text
Resources/Battle/KnightBattleVisual.prefab
  Data/Spine/Knight/skeleton_SkeletonData.asset
    skeleton.json
    skeleton_Atlas.asset
      skeleton.atlas.txt
      skeleton_Material.mat
        skeleton.png
        Assets/Spine/Runtime/spine-unity/Shaders/Spine-Skeleton.shader
  Data/Spine/Knight/KnightSpineBattleAnimConfig.asset
```

Giới hạn:

- Chưa quan sát Play Mode trong đợt audit; không được ghi nhận là nhân vật “hoạt động tốt nhất” đã được chứng minh.
- Không có clip hit/skill trong skeleton Knight.
- Không có event hit trong JSON; driver dùng thời điểm fallback để phát damage callback.
- Config trên prefab không tự chứng minh config đó được bảo toàn khi spawn: pipeline xóa nested driver rồi gắn driver trên host. Cần đối chiếu giá trị runtime trong đợt sau.
- Đây là ứng viên chuẩn cho pipeline battle Spine, chưa phải chuẩn chung cho mọi pipeline 2D/3D.

## Phát hiện cần ưu tiên xác minh

### A1 — Alpha bị thay controller và có thêm driver trong đường spawn

Mức bằng chứng: xác định từ luồng code; chưa tái hiện trong Play Mode.

- `DuelDirector.CreateRigFighter` gắn `AlphaUnityRigBattleAnimator` khi prefab là AlphaBattleVisual.
- `SpawnAllyStackMember` tiếp tục gọi `EnsureBattleVisualDriver`.
- Hàm này không loại trừ AlphaUnityRigBattleAnimator; với visual không có Spine, nó tìm Animator, gán LegendsMeleeRobot_Battle controller rồi thêm Line1BattleSpriteAnimator.
- Alpha gọi các state `walk/idle/atk/hit/died`, khác state của controller legacy.
- Hậu quả dự kiến: hai driver cùng tồn tại và controller dành cho Alpha bị thay. Không dùng Alpha làm canonical trước khi xác minh/xử lý xung đột này.

Nguồn: `Scripts/Runtime/Gameplay/DuelDirector.cs` — SpawnAllyStackMember (~4344), CreateRigFighter (~5248), EnsureBattleVisualDriver (~5804); `AlphaUnityRigBattleAnimator.cs`.

### A2 — Catalog nhập ngoài thiếu dependency

Mức bằng chứng: quét GUID serialized trên đĩa.

- 181 GUID chưa resolve trong 316 file; toàn bộ file bị ảnh hưởng nằm trong `Assets/SpinSquad/Resources/Battle/UnitCatalog/`.
- Phân bố: 10 prefab, 270 asset, 10 override controller, 26 anim.
- Cả 10 override controller thiếu base controller GUID `90aeea94d1680ba4eaa59f1ae29e5064`.
- Không tìm thấy GUID chưa resolve ngoài thư mục này trong các loại file đã quét: unity, prefab, asset, mat, controller, overrideController, anim; đã loại GUID built-in đặc biệt.
- Chưa kiểm tra đầy đủ subasset fileID, binary asset, import status hoặc mọi đường tải Resources động. Không đồng nghĩa mọi file bị ảnh hưởng đều đang được sử dụng trong trận.
- Asset còn nằm trong Resources dù tài liệu nói dừng dùng catalog; cần xác minh ảnh hưởng tới build/dependency trước mọi đề xuất dọn dẹp.

### A3 — Migration save sai thứ tự

Mức bằng chứng: xác định từ code, chưa chạy test với save mẫu.

- `MetaProgressionStore.Normalize` gán Version = 3 khi Version < 3.
- Sau đó mới kiểm tra Version < 2 để chuyển LineUpgradeLevels sang LineRarityUpgradeLevels.
- Với save phiên bản 1, điều kiện migration thứ hai không còn đúng; nâng cấp legacy có thể không được chuyển.

Nguồn: `Scripts/Runtime/Meta/MetaProgressionStore.cs`, khoảng dòng 478 và 509.

### A4 — Cấu hình test can thiệp gameplay

- SpawnAndRegisterFighters reset tiền roll về 0, bỏ bonus khởi đầu trong đường này.
- Starter test level 1/wave 1 khai báo 5 ally; line 2 là Epic.
- Starter ép rangedOverride = false cho cả đội, kể cả unit vốn đánh xa.
- Wave 1 level 1 còn dùng HP multiplier 2.
- Chưa nên dùng cấu hình này đánh giá balance hoặc vai trò chuẩn của nhân vật.

Nguồn: `DuelDirector.cs` — TrySpawnStarterTestAllies, SpawnAndRegisterFighters.

### A5 — Tài liệu lệch trạng thái thực tế

- CURRENT_WORK_HANDOFF nói 2 robot; AI_CHANGE_MAP nói 1 Epic; code hiện khai báo 5 starter.
- Comment/ghi chú nói Epic Kim fallback robot bay, nhưng TryResolveBattlePrefabSource trỏ Epic sang AlphaBattleVisual.
- PlayModeStartHomepage mặc định route Play về Homepage, có bypass cho preview. Hướng dẫn cũ “Play luôn chạy scene đang mở” không đúng trong mọi trường hợp.
- Không lấy mô tả cũ làm bằng chứng một lỗi đã được sửa hoặc nhân vật đã được duyệt.

## Kiểm tra đã thực hiện

- Đọc manifest/lock, ProjectVersion, GraphicsSettings, QualitySettings, TagManager, Physics2DSettings, DynamicsManager, EditorBuildSettings và activeInputHandler.
- Kiểm kê prefab/controller/scene/ScriptableObject; đọc binding các prefab battle chính.
- Đọc JSON của 8 skeleton để kiểm tra version, số bones, tên clip và event.
- Quét GUID của các loại asset serialized nêu trên, đối chiếu meta trong project/package cache.
- Theo đường spawn → visual driver → controller; đọc combat callback, stat scaling và save normalization.
- Đọc các test hiện có: test callback attack nhánh Line1 và test presentation 2.5D. Chưa chạy; chúng chưa chứng minh prefab Knight/Alpha hoạt động trong battle thực tế.

## Đợt kiểm tra sâu tiếp theo — chưa thực hiện

1. Xác minh A1 bằng chuỗi object/component/controller runtime, gồm cả board và bag; xem driver nào DuelActor thực sự chọn.
2. So sánh Knight, Green, Red, Blue, Kim và Croco theo cùng tiêu chí: dependency, spawn, hướng, baseline, idle/walk/attack/death, hit timing; chưa chốt “tốt nhất” chỉ từ số bones hoặc tên prefab.
3. Kiểm tra cấu hình bị mất khi xóa nested driver, đặc biệt KnightSpineBattleAnimConfig và thông số attack speed/interval.
4. Kiểm tra callback damage khi thiếu clip, attack đang chạy, đổi mục tiêu, chết, disable hoặc restart.
5. Kiểm tra animation bindings, material/shader, pivot/bounds và sorting ở SampleScene so với vertical slice.
6. Kiểm tra migration bằng dữ liệu save mẫu riêng; không ghi đè PlayerPrefs thật.
7. Kiểm tra Resources path động, duplicate key, scene routing và dependency catalog ảnh hưởng tới build.
8. Khi chuyển sang Play Mode, lưu ý game có thể ghi PlayerPrefs ngay khi bắt đầu trận. Cần bảo toàn save người dùng và trạng thái scene trước khi chạy.

Chưa được sửa gameplay, prefab, scene, asset hoặc cấu hình theo các đề xuất trong báo cáo khi người dùng vẫn đang yêu cầu audit-only.
