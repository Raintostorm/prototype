# Alpha vertical slice plan

## Goal

Prove one original ranged ally from readable weapon motion through to a safe battle test before creating another unit.

## Order of work

1. **Alpha Lab — weapon read**
   - Fixed cannon mount, muzzle, straight basic bolt, and separate charged skill orb.
   - Verify no projectile intersects the cannon at any frame.
2. **Alpha Lab — motion acceptance**
   - Idle: grounded scan/breath.
   - Walk: diagonal gait, knee-led lift, planted support feet.
   - Attack: anticipate → bolt release → recoil → recovery.
   - Skill: charge → barrel elevation → high arc → recovery.
   - Died: collapse and hold.
3. **Battle bridge test**
   - Create an isolated test battle; do not replace the main roster.
   - Alpha moves, selects a dummy, fires a projectile, applies one damage event, then returns to idle.
4. **Approval gate**
   - Only after the test is visually accepted may Alpha replace a production battle visual.
   - Reuse this contract for every later original unit.

## Current state

- Alpha Lab weapon separation is implemented.
- The next acceptance check is visual: test basic shot and skill one state at a time in Play Mode.
