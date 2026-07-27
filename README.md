# SpinSquad Prototype

SpinSquad Prototype is a Unity 6 mobile-first strategy prototype built around a simple loop:

`Homepage -> choose level -> prep board -> roll allies/buffs -> arrange squad -> battle waves -> rewards/progression`

The project is still a prototype. It is not production-ready yet, but the core systems are now present enough to test the intended game loop inside the Unity Editor.

## Current Development State

- Unity version: `6000.4.1f1`
- Main gameplay scene: `SampleScene`
- Meta scene: `Homepage`
- Current branch work includes:
  - responsive Homepage rebuild;
  - procedural homepage UI kit and icon pass;
  - isolated 2.5D battle vertical slice scene;
  - duel gameplay flow guidance;
  - combat impact feedback;
  - wave clear / reward / defeat overlay flow.

The game is playable as an editor prototype: you can enter battle, roll, arrange allies, start combat, clear waves, receive rewards, and return to the homepage.

## Core Game Concept

The player builds a squad through roll-based RNG and tactical placement. Each wave gives a prep moment where the player spends roll coins to gain allies, buffs, or more coins. The player then arranges units on the board and starts real-time combat.

The long-term direction is a 2.5D mobile tactics game rather than a full 3D game. Assets and Spine animation are expected to arrive later; the current build uses placeholders, procedural UI, and early Spine integration hooks.

## Core Loop

1. Homepage shows resources, selected level, and entry to battle.
2. Battle starts in prep mode.
3. The player rolls while roll coins remain.
4. Roll results can grant allies, buffs, or coins.
5. Allies are placed on the board or stored in the allies bag.
6. The player arranges the squad and starts combat.
7. Units auto-target, move, attack, take damage, and die.
8. Clearing a wave grants more roll coins and opens the next wave prep.
9. Clearing the level grants gold and treasure keys, then unlocks the next level.

## Core Systems

### Battle

Battle is coordinated by `DuelDirector`.

- Up to `10` waves per level.
- Allies and enemies spawn on a grid.
- Combat is real-time and automatic after the player presses Start.
- Melee units close distance and strike in contact.
- Ranged units shoot from range using projectile VFX.
- Damage popups, slash/spark impact VFX, kill feedback, and camera shake are now wired into combat.
- Wave clear, level clear, and defeat now use overlay panels instead of only a small banner.

### Roll / Gacha

Roll logic lives under `Assets/SpinSquad/Scripts/Gacha`.

- A roll costs roll coins.
- The current session uses a 6-slot roll.
- Results can grant ally units, buffs, or coins.
- Buffs can affect HP, damage, attack speed, and crit chance.
- Pending ally grants are drained into the board or allies bag.

### Allies / Merge / Bag

Allies use line and rarity data.

- Allies can be moved on the board during prep.
- Allies can be placed into an allies bag when the board is full.
- Three compatible allies can auto-merge into the next rarity tier.
- The allies bag currently works logically but its UI/layout still needs a major cleanup.

### Meta Progression

Meta progression is handled by `MetaProgressionStore`.

- Gold and treasure keys are tracked.
- Levels unlock after level clear.
- Upgrade and treasure systems already have early data/controller structure.
- Persistence is prototype-level and should be hardened later if preparing a real build.

### Presentation / 2.5D

The project is staying on a 2.5D direction.

- `Duel25DVerticalSlice` exists as an isolated battle presentation test.
- Runtime presentation scripts handle depth scaling, shadows, camera framing, hit feedback, and procedural backdrops.
- Spine integration exists, but production animation assets are not complete yet.

## Known Issues

- Allies bag UI is visually heavy and can clutter the battle board.
- Many art assets are placeholder or inconsistent.
- Enemy and ally readability still needs improvement.
- Combat balance is not final.
- Full automated PlayMode test coverage is not complete.
- Terminal GitHub authentication may fail on this machine; GitHub Desktop is currently the reliable push path.

## Recommended Next Work

1. Rebuild Allies Bag UI as a clean drawer/popup.
2. Improve combat readability: target indicators, unit roles, low HP state.
3. Tune early wave balance and reward pacing.
4. Build one complete Spine sample pipeline for one ally and one enemy.
5. Polish homepage and meta screens once battle loop feels good.

## Useful Internal Docs

- `Assets/SpinSquad/Docs/DUEL_25D_VERTICAL_SLICE.md`
- `Assets/SpinSquad/Docs/HOMEPAGE_UI_LAYOUT.md`
- `Assets/SpinSquad/Docs/PROGRESSION_SPEC.md`
- `Assets/SpinSquad/Docs/ALLY_STATS.md`
- `Assets/SpinSquad/Docs/ENEMY_STATS.md`
- `SPINSQUAD_TONG_HOP.md`
- `SPINSQUAD_LOGIC_ROADMAP.md`

## How To Test Quickly

1. Open the project in Unity `6000.4.1f1`.
2. Open `Homepage` and press Play to enter the flow from the home screen.
3. Or open `SampleScene` directly to test the battle prototype.
4. In battle:
   - roll until roll coins are spent;
   - arrange allies on the board;
   - optionally open the allies bag;
   - press Start;
   - watch combat, wave clear, reward, or defeat flow.
