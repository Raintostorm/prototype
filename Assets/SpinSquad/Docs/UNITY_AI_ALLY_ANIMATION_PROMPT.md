# Unity AI Prompt - Ally Animation Tu PNG (Style Lock)

Tai lieu nay dung de copy/paste cho Unity AI (Assistant) de tao animation ally tu PNG
ma KHONG bi lech style so voi tile goc.

---

## 1) Prompt chinh (ban day du, uu tien dung)

Copy nguyen khoi ben duoi vao Unity AI:

```text
You are a Unity 2D animation assistant for project SpinSquad.

STRICT RULES (must follow):
1) DO NOT generate new character art.
2) DO NOT redesign, repaint, or restyle the ally.
3) Use ONLY provided source sprites (tile041, tile042, tile043) and their existing frames.
4) Preserve original silhouette, proportions, palette, shading, and outline thickness.
5) If source frames are missing, report missing frames. Do not invent or hallucinate visuals.

TASK:
Create battle animations from existing source frames only:
- Idle: 6 frames
- Walk: 6 frames
- Attack: 8 frames
- UseSkill: 8 frames
- Die: 6 frames

PIPELINE:
- Slice frames in stable order: left -> right, top -> bottom.
- Keep pivot and bounding consistent across clips to avoid jitter.
- Do not mix frames between different allies.
- Create clips:
  - <AllyName>_Idle.anim
  - <AllyName>_Walk.anim
  - <AllyName>_Attack.anim
  - <AllyName>_UseSkill.anim
  - <AllyName>_Die.anim
- Create controller:
  - <AllyName>_Battle.controller
- Default state: Idle
- Parameters:
  - Bool IsMoving
  - Trigger Attack
  - Trigger UseSkill
  - Trigger Die
- Transitions:
  - Idle <-> Walk
  - Any State -> Attack
  - Any State -> UseSkill
  - Any State -> Die
  - Attack -> Idle (with Exit Time)
  - UseSkill -> Idle (with Exit Time)
  - Die is terminal (no return transition)

FPS:
- Idle: 8
- Walk: 10
- Attack: 12
- UseSkill: 12
- Die: 10

SPRITE IMPORT SETTINGS:
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Multiple
- Keep original color/look (no color correction)
- Pixel art: Filter Mode Point, Compression None
- Keep Pixels Per Unit consistent with project
- Keep alpha edges clean, no artifacts

VALIDATION (MANDATORY):
- Compare output clips against source tiles and confirm no style drift.
- If style mismatch is detected, stop and report mismatch.
- Output final report with:
  1) paths of .anim clips
  2) path of .controller
  3) frame ranges used per state
  4) confirmation: "No new art generated; source style preserved."
```

---

## 2) Negative constraints (de tranh generate sai)

Neu AI van tao sai, them dong nay vao prompt:

```text
Hard ban:
- No AI paintover
- No upscaling redesign
- No anatomy/style changes
- No new costume/weapon/effects unless already present in source frames
- No frame interpolation that changes drawing style
```

---

## 3) Convention dat ten trong repo

- Folder clips: `Assets/SpinSquad/Resources/Animations/Battle/<AllyName>/`
- Clip names:
  - `<AllyName>_Idle.anim`
  - `<AllyName>_Walk.anim`
  - `<AllyName>_Attack.anim`
  - `<AllyName>_UseSkill.anim`
  - `<AllyName>_Die.anim`
- Controller:
  - `<AllyName>_Battle.controller`

Neu co UI animation rieng:

- `Assets/SpinSquad/Resources/Animations/UI/<AllyName>/`
- `<AllyName>_UI.controller`

---

## 4) Frame va movement rules (de clip nhin hop ly)

- Idle: loop nhe, uu tien breathing/stance, khong nhay vi tri chan.
- Walk: contact->pass->contact ro rang, giam foot sliding.
- Attack: co anticipation, hit frame ro, recovery ngan.
- UseSkill: frame impact khac Attack (doc la skill cast, khong trung nhat).
- Die: frame cuoi giu nguyen, khong loop.

---

## 5) Checklist reject output

Reject va lam lai neu co bat ky loi nao:

- [ ] Mat style tile goc (mau, lineart, shading khac).
- [ ] Sai so frame (khong dung 6/6/8/8/6).
- [ ] Pivot/bounding lech lam sprite giat.
- [ ] Die bi loop.
- [ ] Frame bi tron giua ally khac nhau.
- [ ] AI them chi tiet khong co trong source.

---

## 6) Prompt ngan gon (ban rut gon)

```text
Create ally battle animations from source tiles only (tile041/tile042/tile043).
No new art, no redesign, no style change.
Required frames: Idle 6, Walk 6, Attack 8, UseSkill 8, Die 6.
Build clips + controller with IsMoving/Attack/UseSkill/Die parameters.
Keep pivot/bounds stable, Die non-loop.
If frames are missing, report instead of inventing.
Confirm final output: "No new art generated; source style preserved."
```

