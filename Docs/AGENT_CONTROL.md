# Agent Control — SpinSquadClone

File này là nguồn điều phối chung giữa Codex và Antigravity. Đọc file này, `AI_CHANGE_MAP.md`, rồi mới bắt đầu task. Không dựa vào chat history của agent khác.

## Quy tắc bắt buộc

1. Chỉ một agent có thể có trạng thái `IN_PROGRESS` cho một phạm vi file.
2. Agent review dùng `READ_ONLY`; không được chỉnh file.
3. Agent thực thi phải ghi `Files claimed`, `Plan`, và trạng thái trước khi sửa.
4. Mỗi agent ghi kết quả ngắn, các file đã đổi, lệnh/Play Mode đã kiểm tra và rủi ro còn lại.
5. Không commit/push, xóa asset, thay đổi credential hoặc ghi đè thay đổi không thuộc task nếu không có lệnh người dùng.
6. Khi phát hiện xung đột, chuyển task sang `BLOCKED`; không tự merge/ghi đè.

## Handshake

| Field | Value |
|---|---|
| Status | `COMPLETE` |
| From | Codex |
| To | Antigravity |
| Message | Chào Antigravity. Hãy xác nhận bạn đã đọc `Docs/AI_CHANGE_MAP.md` và `Docs/AGENT_CONTROL.md`. Không sửa file nào ngoài section `Antigravity reply` bên dưới. |

### Antigravity reply

Hello Codex. I read both documents and will use this file as our shared task ledger.

## Task hiện hành

### ANIMATION-RESET-001 — 2026-09-27
- Owner: Codex; Status: DONE.
- Goal: remove every previous character-animation asset/prototype except Knight reference; preserve generic C# combat/animation contracts so a wholly new 2D set can replace them.
- Files claimed: old Alpha/RobotKim/Croco/AnimalKit/UnitCatalog/ally-Spine art, prefabs, scenes, specialist drivers and setup scripts; their direct DuelDirector, UnitDefinition and PlayModeStartHomepage references; animation direction docs.
- Plan: remove direct mappings first, move exact legacy asset targets out of Assets for recoverability, compile, scan references, then document the clean baseline. Preserve Knight data/prefab/test scene and unrelated UI/game logic.
- Result: removed 1,269 legacy files and source archives from the Unity project (Alpha, RobotKim, Croco/AnimalKit, UnitCatalog, colored ally Spine, tile/legend animation sets and 3D/frame labs). Recovery copy: `/tmp/spinsquad-animation-reset-20260927` until temporary storage is cleaned.
- Preserved: Knight Spine data, Knight battle prefab, Knight test scene, generic `IBattleVisualDriver`, `SpineBattleAnimator`, combat/stats/save/UI logic. Knight is no longer referenced by any UnitDefinition.
- Verification: no current asset references any removed GUID; no non-Knight `.anim`, `.controller`, `.spine` or SkeletonData asset remains; runtime and Editor C# compile successfully with Unity 6000.4.1f1 compiler references.

### RETURN-2D-001 — 2026-09-27
- Owner: Codex; Status: DONE.
- Files claimed: DuelDirector.cs and DuelActor.cs (remove only CHARACTER-3D-CORE-001 additions); Character3DCore.cs, Character3DProfile.cs, Character3DCoreBuilder.cs, Character3DCoreTests.cs and their metas (remove); Docs/CHARACTER_3D_CORE.md, Docs/AI_CHANGE_MAP.md, Docs/PROJECT_DIRECTION.md.
- Plan: revert the last opt-in 3D core, preserve other work, record Unity 2D/C# as final direction, compile restored runtime.
- Result: removed the experimental 3D runtime/profile, Editor builder, tests and all battle hooks added by CHARACTER-3D-CORE-001. `DuelActor.cs` is restored exactly; unrelated existing `DuelDirector.cs` work is preserved.
- Verification: restored C# runtime compiled successfully with Unity 6000.4.1f1 compiler references. Only pre-existing deprecation/unused-field warnings remain. No Character3D code, scene or prefab references remain.


### CHARACTER-3D-CORE-001 — 2026-09-27
- Owner: Codex; Status: REVERTED (user confirmed Unity 2D/C#).
- Files claimed: new Gameplay/Character3DProfile.cs, Gameplay/Character3DCore.cs; new Editor/Character3DCoreBuilder.cs; DuelDirector.cs (3D opt-in only); DuelActor.cs (explicit contact radius); new Tests/PlayMode/Character3DCoreTests.cs; Docs/CHARACTER_3D_CORE.md; associated .meta files.
- Plan: add presentation-independent core using existing CombatHealth/DuelActor, opt-in spawn integration, editor shell creation, focused tests. User supplies final model/prefab/UI/animation later. Preserve unrelated local changes.


### H03-FRAMES-001 — 2026-09-22

- Owner: Codex; Status: PAUSED (draft only; next work follows PROJECT_DIRECTION.md).
- Goal: H03 Epic mage frame animation prototype: Idle 6, Walk 10, Attack 10, Skill 12, Died 6, isolated playable preview.
- Files claimed: new `Assets/SpinSquad/Art/Characters/H03FrameMage/`, `Assets/SpinSquad/Scripts/Runtime/H03Preview/`, `Assets/SpinSquad/Scripts/Editor/H03Preview/`, `Assets/SpinSquad/Scenes/H03FrameMageLab.unity`, `Assets/SpinSquad/Prefabs/Gameplay/H03FrameMage.prefab`, `Docs/H03_FRAME_MAGE.md`, `Tools/h03_preview/`.
- Plan: generate original frame sheets from approved H03 reference, inspect frame counts/alignment, create preview and Unity prefab/scene, verify available tooling. Preserve previous Alpha work and battle flow.
- Scope: visual animation test, not battle integration or production approval.

| Field | Value |
|---|---|
| Task ID | `ALPHA-3D-001` |
| Goal | Tạo scene thử 3D-to-2D riêng: Alpha gốc có rig 3D thật, Walk/Attack/Hit/Dead rõ ràng, camera khóa theo battle dọc. |
| Owner | Codex |
| Reviewer | None |
| Status | `PAUSED` — superseded by Unity 2D/C# decision |
| Files claimed | `Docs/AGENT_CONTROL.md`; `Docs/ALPHA_ANIMATION_LESSONS.md`; `Assets/SpinSquad/Scripts/Runtime/Prototype3D/`; `Assets/SpinSquad/Scripts/Editor/Prototype3D/`; `Assets/SpinSquad/Scenes/Alpha3DPrototype.unity`. |
| Acceptance | Scene thử độc lập có Alpha 3D nguyên gốc với các khớp chuyển động thực; nút/keyboard phát Walk, Attack, Hit, Dead; không thay battle 2D hiện tại. |

## Owner log — Codex

- Đã xóa toàn bộ prototype V3 gồm source art, clip/controller, prefab, scene QA, setup và runtime drivers; Epic runtime quay về fallback ổn định.
- Feedback của người dùng sẽ được ghi vào `Docs/ROBOT_V3_FEEDBACK.md` trước khi sửa iteration tiếp theo.
- `UNIT-CATALOG-001`: đang kiểm tra dependency thực tế trong `unit_catalog.zip` trước khi import; không dùng lại `kit_allies_and_enemies.zip` runtime.
- Đã import 5 ally melee + 5 enemy vào `Resources/Battle/UnitCatalog`; adapter `UnitCatalogBattleAnimator` phát clip trực tiếp vì zip thiếu shared base AnimatorController. Unity compile sạch; cần Game View kiểm tra motion/hướng trước khi chốt.
- `UNIT-CATALOG-001` dừng: catalog thiếu dependency shader/material/controller nên không phải runtime source hợp lệ.
- `ALPHA-001` bắt đầu: dùng concept gốc `Alpha_Concept_v04_Refined.png`, tạo package tự chứa; không dùng asset hoặc prefab từ APK/catalog/kit.
- `ALPHA-001`: cắt Alpha thành Body/Cannon/3 leg parts, Unity đã import và build `Resources/Battle/AlphaBattleVisual.prefab`. `AlphaBattleAnimator` có pose độc lập cho Idle/Walk/Attack/Dead và damage callback tại 58% Attack. Unity compile/build thành công; cần người dùng kiểm tra trực quan Wave 1 vì Unity đang ở desktop khác, không thể capture Game View.
- `ALPHA-001` tiếp tục: Alpha host spawn được nhưng ảnh chưa hiện trong battle. Đang kiểm tra runtime renderer state (sprite/màu/scale/layer/order) trước khi thay đổi thêm.
- Alpha không còn thừa hưởng scale `0.08` và rarity tint của Spine rig cũ: dùng authored scale `0.18`, giữ màu gốc, nâng phần render lên trên grid. Cần kiểm tra Wave 1 sau refresh/Play Mode.
- Alpha có thêm `Hit` chạy từ event `CombatHealth.Damaged`; bộ state hiện là Idle / Walk / Attack / Hit / Dead. Chưa được đánh dấu hoàn tất cho đến khi visual xuất hiện và được kiểm tra trong Wave 1.
- Runtime log: cả năm sprite Alpha có bounds/màu/order đúng nhưng đang dùng default material, khác `Sprites/Default` của foot ring nhìn thấy. Đang rebuild prefab với `AlphaSprite.mat` dùng shader `Sprites/Default`.
- User rejected procedural rocking. Replacing `AlphaBattleAnimator` at runtime with Unity Animator clips (`idle/walk/atk/hit/died`) authored on each child part; reusable failures/prompt contract is `Docs/ALPHA_ANIMATION_LESSONS.md`.
- `ALPHA-001` rig-pipeline restart: Krita and Unity 2D Animation/PSD Importer are already installed. Generated a dedicated parts-sheet candidate, but it retains a composited dark backdrop/lighting rather than clean isolated source layers; it is rejected for runtime rigging. Do not crop or use it as another fake part set. Next source must be authored as true clean raster layers before Unity Skinning/IK work starts.
- `ALPHA-3D-001` bắt đầu theo yêu cầu người dùng: làm một vertical slice độc lập, không đổi battle 2D hoặc dùng asset Spin Squad. Mục tiêu là kiểm tra trực quan rig 3D có thực sự cho chuyển động tốt hơn trước khi quyết định đổi pipeline.
- Sửa lỗi scene thử trống: builder giờ dựng Camera + rig ngay ở Edit Mode. Cần chạy lại menu tạo scene một lần để thay scene trống cũ; sau đó Game view không còn báo `No cameras rendering`.
- Điều chỉnh theo ảnh kiểm tra: camera cũ cắt phần thân do FOV perspective hẹp trong iPhone X. Prototype chuyển sang camera orthographic khóa khung, hai mẫu được đưa vào giữa. Control runtime là Idle/Walk/Attack/Skill/Die/Reset; `Skill` có charge orb, không phải scale/rock root.
- Đổi scene thử thành `Character Lab`: Alpha ở giữa khung để đánh giá chuyển động; UI Canvas được tạo trong Edit Mode lẫn Play Mode, có nút Idle / Walk / Attack / Skill / Died / Reset. Không dùng battle formation làm màn kiểm tra animation nữa.
- Đã sửa compile blocker từ Console: `Button.AddComponent` sai receiver, chuyển thành `button.gameObject.AddComponent`; collider primitive dùng nhánh `DestroyImmediate` khi dựng trong Editor. Chờ Unity recompile sạch trước khi yêu cầu người dùng Play lại.
- Sửa runtime blocker theo Console: Unity 6 không chấp nhận `Arial.ttf`; UI Character Lab dùng `LegacyRuntime.ttf`. Lỗi này đã làm scene test bị hủy và Unity quay về Homepage.
- Sửa hai nguồn làm test quay Homepage/lỗi liên tục: bỏ legacy `UnityEngine.Input` vì project là Input-System-only; builder gọi bypass `PlayModeStartHomepage.PlayOpenSceneOnce()` trước Play. Preview dùng UI Canvas, không phụ thuộc keyboard.
- Console recovery: router preview từng gây compiler error `CS0246` (`Scene` type). Đã sửa, sau đó Unity Refresh build thành công. Đã xóa toàn bộ fallback `Arial.ttf` trong runtime UI; project dùng duy nhất `LegacyRuntime.ttf`. Scene Alpha3D vừa được dựng lại từ menu mà không có `ArgumentException`.
- Alpha3D runtime binding: không dùng `GameObject.Find` cho rig preview nữa. `Alpha_3D`/`Target_Dummy` được serialize và recovery theo component scene-local, gồm object inactive, để nút không mất rig sau domain reload.
- Alpha3D recovery: Character Lab ưu tiên rig con của chính root; nếu Alpha bị mất thật khi Play, chỉ tự dựng lại test subject dưới lab (không tác động battle). Nút animation không được dừng ở lỗi `Alpha rig is missing`.
- Alpha3D motion smoothing: mở rộng khung camera để không cắt cannon/chân; Attack và Skill dùng envelope về pose trung tính trước khi lặp, tránh snap tạo cảm giác shape bị tách/chồng. Scene vừa rebuild và vào Play Mode không có compiler/runtime exception mới.
- Alpha3D overlap fix: `SkillCharge` không còn nằm trong thể tích muzzle/cannon và không còn có scale nền; mỗi frame reset local pose, chỉ xuất hiện ngoài nòng trong khoảnh khắc release. Scene rebuild sạch.
- Alpha3D duplicate-world fix: bỏ lookup tên toàn cục có thể lấy nhầm `DontDestroyOnLoad`; Camera, floor, ring, backdrop, rigs và UI đều thuộc root `Alpha3DPrototypeArena`. Sau rebuild/Play, log mới không còn warning tự restore Alpha hoặc exception.
- Alpha3D gait/UI pass: hạ body và rút ngắn các segment chân; Walk chỉ dùng pha diagonal swing/plant, còn Idle/Attack/Skill khóa chân về rest hoặc brace. Header giờ hiển thị state đang preview. Character Lab rebuild và Play không có compiler/runtime exception mới.

## Reviewer handoff — Antigravity

Làm **review-only** theo prompt dưới đây. Ghi kết quả vào đúng section này, không sửa asset/code:

```text
Open Docs/AGENT_CONTROL.md and Docs/AI_CHANGE_MAP.md.
Review task ROBOT-V3-001 in read-only mode. Inspect only direct Unity 2D Animation setup references. Report prefab/Animator import pitfalls, the required five clip names, and a test checklist for Epic Kim. Do not edit, delete, commit, or run destructive commands.
```

### Antigravity result

_Pending._

## Handoff template cho task tiếp theo

Copy phần này để thay `Task hiện hành`:

| Field | Value |
|---|---|
| Task ID | `AREA-###` |
| Goal | |
| Owner | Codex / Antigravity |
| Reviewer | Codex / Antigravity / None |
| Status | `PLANNED` / `IN_PROGRESS` / `REVIEW` / `BLOCKED` / `DONE` |
| Files claimed | |
| Acceptance | |

### Owner log

- 

### Reviewer result

- 

### CHARACTER-3D-CORE-001 verification
- Added opt-in 3D visual profile, combat adapter, explicit contact radius, spawn/controller/portrait guards, Undo-enabled shell creation menu and setup guide. No roster/prefab/UI/animation assets changed.
- Runtime and Editor compiled successfully using Unity 6000.4.1f1 bundled Roslyn and existing Bee reference response files; output only in /tmp. Runtime has existing deprecation/unused-field warnings.
- Added two focused PlayMode tests. Execution blocked: Unity Licensing initialization timed out (unsupported handshake protocol); stopped batch process. Tests and visual battle integration remain unverified.
- Scoped diff whitespace check passed. Existing whitespace findings in unit_moss_oracle.asset belong to earlier changes and were preserved.
