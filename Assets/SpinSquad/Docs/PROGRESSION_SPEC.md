# Progression Spec

## Campaign

- Total campaign levels: `3`
- Waves per level: `10`
- Enemy scaling by level:
  - Level 1: HP x`1.00`, ATK x`1.00`
  - Level 2: HP x`1.15`, ATK x`1.12`
  - Level 3: HP x`1.30`, ATK x`1.24`

## Level Complete Rewards

- Reward on clearing wave 10:
  - Level `1`: `2500` Gold, `5` Keys
  - Level `2`: `5000` Gold, `5` Keys
  - Level `3`: `7500` Gold, `5` Keys
- Unlock next level up to level 3.

## Permanent Upgrade (Gold)

- Scope: line-rarity based (`L0/L1/L2 x Common/Rare/Epic/Legendary`) => `12` upgrade entries.
- Level cap: `50`.
- Upgrade cost formula:
  - `cost(level) = 60 + 26 * level + 8 * floor(level / 5)`
- Deterministic growth + rarity rules:
  - Flat buff seed per level:
    - HP `0.015`
    - DMG `0.0125`
    - ASPD `0.008`
  - Flat rarity factor:
    - each rarity +1 step multiplies flat buff by `x4` (Common->Rare->Epic->Legendary)
  - Percent buff:
    - base amplification from level curve (`lerp`),
    - plus `+0.2` percentage point per rarity step (`+0.002` in ratio)
  - Total upgrade buff:
    - `totalPct = ((1 + flatPct) * (1 + percentPct)) - 1`
- Upgrade status model:
  - `CanUpgrade`
  - `NeedGold`
  - `MaxLevel`

## Treasure Roll (Key)

- Roll cost: `1 TreasureKey`.
- Rarity weights:
  - Common `60%`
  - Rare `27%`
  - Epic `10%`
  - Legendary `3%`
- Duplicate fusion:
  - Duplicate adds shard.
  - Shards needed for next level: `1 + floor(currentLevel / 2)`.
  - Treasure level cap: `50`.
- Treasure status model:
  - `NotOwned`
  - `OwnedNeedDuplicates`
  - `OwnedCanLevelUp`
  - `MaxLevel`

## Card + Detail Scenes

- `Upgrade` scene: card list of `L0/L1/L2`.
- `UpgradeDetail` scene: selected line details + upgrade action.
- `Treasure` scene: card list of all treasure definitions (owned + not owned).
- `TreasureDetail` scene: owned state, level, duplicate requirement, current/next effect value.
- Selection handoff uses one-time consume context:
  - list scene sets selected id/index before load detail
  - detail scene consumes it once and shows `InvalidSelection` if opened directly without context
- UI refresh is event-driven (on enter/action) instead of per-frame polling.

## Treasure Passive Application

- Treasures are auto-active; no equip step.
- Passive categories currently supported:
  - Ally HP%
  - Ally ATK%
  - Ally ATK Speed%
  - Ally Crit Chance%
  - Start roll-coin bonus
  - Per-wave roll-coin bonus

## Save Model

- Stored in PlayerPrefs JSON key: `SpinSquad_MetaProgression_v1`.
- Persisted fields:
  - Unlocked/selected level
  - Gold, TreasureKeys
  - Line-rarity upgrade levels (`12` slots)
  - Owned treasures + level + duplicate shards
- Migration:
  - Legacy line-only upgrade values are migrated into `Common` rarity slots on first load of save version 2.
