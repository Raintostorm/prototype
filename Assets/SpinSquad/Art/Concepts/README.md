# Spin Squad concept sources

These sheets are visual production references. They are not imported as final
runtime atlases yet.

- `crt_bot_parts_concept_v1.png`: rigging-friendly separated-parts direction
  for an existing ally.
- `rocky_croc_parts_concept_v1.png`: rigging-friendly separated-parts direction
  for an existing enemy.
- `spin_squad_ui_kit_concept_v1.png`: unified friendly mobile UI direction.
- `crt_bot_rig_parts_v2.png`: advanced robot mechanical anatomy, pivots,
  depth layers, and lighting masks.
- `crt_bot_states_fx_v2.png`: robot screen/emission states, damage swaps,
  detached death parts, and ordered FX frames.
- `rocky_croc_rig_parts_v2.png`: advanced crocodile anatomy, mouth parts,
  tail chain, overlays, pivots, and mesh guidance.
- `rocky_croc_states_fx_v2.png`: crocodile expression slots, jaw swaps,
  damage/death swaps, lighting overlays, and ordered FX frames.
- `rocky_croc_states_fx_streamlined_v3.png`: recommended production scope for
  the current crocodile. It removes redundant micro-variants while retaining
  the animation, damage, death, lighting, and FX states needed by the game.

Before runtime use, character parts should be exported as individual transparent
PNGs, checked for hidden joint coverage, assigned pivots, and rigged in Spine.
UI elements should be recreated/cut into individual transparent assets and
validated at the target mobile resolution.

## Recommended Spine separation

- Keep base color attachments separate from shadow, highlight, rim-light, and
  emission attachments.
- Use slot swaps for eyes, eyelids, pupils, jaws, screen states, damage, and
  dead states. Avoid deforming the full character for these changes.
- Put additive emission and flashes in dedicated slots above the base art.
- Use distinct left/right limb attachments and preserve hidden joint coverage.
- Death animations should switch to limp/detached attachments and fade emission;
  do not squash the entire sprite.
- The V2 sheets are production references. Export and validate every isolated
  attachment as a transparent PNG before importing into Spine.
