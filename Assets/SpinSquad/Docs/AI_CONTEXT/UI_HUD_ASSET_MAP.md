# UI HUD Asset Map (machine-readable)

> BADF wired. **loadOk**: Unity **Audit All HUD Sprites**.

> `Resources.Load<Sprite>("UI/Hud/Meta/Setting")` — no extension.

## Summary

| Metric | Value |
|--------|-------|
| PNG files | 68 |
| wired | 44 |
| gap | 24 |

## Scene wiring (BADF)

| Scene | Features |
|-------|----------|
| Homepage | Resource HUD (Meta/Resource); Prev/Next; CTAs |
| Upgrade | Resource HUD; line card BG; Lock; detail Green_button |
| Treasure | Resource HUD; tabs; Reward roll; list icons; detail Table_1 + stats |
| Duel | Speed×2 toggle (no audio logic) |

## Master table

| fileName | resourcesPath | loadOk | wiredInCode | uiRole | notes |
|----------|---------------|--------|-------------|--------|-------|
| Back.PNG | UI/Hud/Combat/Back | audit | yes | homepage_level_overlay_close; meta_scenes_back | 248×107 |
| Background_combat.PNG | UI/Hud/Combat/Background_combat | audit | no | gap_unused_pack_background | 1579×906 |
| Home_in_battle.PNG | UI/Hud/Combat/Home_in_battle | audit | yes | duel_bottom_home | 187×115 |
| Merge.PNG | UI/Hud/Combat/Merge | audit | yes | duel_ally_menu_merge | 313×120 |
| Restart.PNG | UI/Hud/Combat/Restart | audit | yes | duel_bottom_restart | 123×119 |
| Sell.PNG | UI/Hud/Combat/Sell | audit | yes | duel_ally_menu_sell | 303×121 |
| Shop_icon.PNG | UI/Hud/Combat/Shop_icon | audit | yes | homepage_icon_shop | 248×110 |
| Speed_x2_off.PNG | UI/Hud/Combat/Speed_x2_off | audit | yes | duel_top_speed_off | 169×111 |
| Speed_x2_on.PNG | UI/Hud/Combat/Speed_x2_on | audit | yes | duel_top_speed_on | 153×111 |
| Stop_off.PNG | UI/Hud/Combat/Stop_off | audit | yes | duel_top_pause_overlay_active | 133×115 |
| Stop_on.PNG | UI/Hud/Combat/Stop_on | audit | yes | duel_top_pause | 129×115 |
| Volume_off.PNG | UI/Hud/Combat/Volume_off | audit | yes | duel_settings_decor_only | 131×114 |
| Volume_on.PNG | UI/Hud/Combat/Volume_on | audit | yes | duel_settings_decor_only | 130×114 |
| Add.PNG | UI/Hud/Meta/Add | audit | no | gap_no_ui_hook | 39×37 |
| Ads_block.PNG | UI/Hud/Meta/Ads_block | audit | no | gap_no_ui_hook | 57×66 |
| Advance_chest.PNG | UI/Hud/Meta/Advance_chest | audit | no | gap_treasure_feature | 74×67 |
| All_icon.PNG | UI/Hud/Meta/All_icon | audit | no | gap_no_ui_hook | 38×38 |
| All_tab.PNG | UI/Hud/Meta/All_tab | audit | yes | treasure_tab_common | 70×39 |
| Arena_button.PNG | UI/Hud/Meta/Arena_button | audit | no | gap_no_ui_hook | 164×107 |
| Background.PNG | UI/Hud/Meta/Background | audit | no | gap_unused_pack_background | 1512×1012 |
| Bag.PNG | UI/Hud/Meta/Bag | audit | no | gap_inventory_feature | 54×63 |
| Battle_button.PNG | UI/Hud/Meta/Battle_button | audit | yes | homepage_cta_play | 250×88 |
| Blue_button.PNG | UI/Hud/Meta/Blue_button | audit | yes | duel_bottom_add_ally | 88×40 |
| Chest.PNG | UI/Hud/Meta/Chest | audit | yes | homepage_cta_treasure | 70×65 |
| Clam_notify.PNG | UI/Hud/Meta/Clam_notify | audit | no | gap_typo_clan_notify | 28×28 |
| Clan_button.PNG | UI/Hud/Meta/Clan_button | audit | no | gap_no_ui_hook | 62×56 |
| Complete_notify.PNG | UI/Hud/Meta/Complete_notify | audit | no | gap_no_ui_hook | 27×28 |
| Earth_card.PNG | UI/Hud/Meta/Earth_card | audit | yes | upgrade_card_line4_bg | 88×93 |
| Earth_icon.PNG | UI/Hud/Meta/Earth_icon | audit | yes | upgrade_card_line4_fallback | 37×37 |
| Energy.PNG | UI/Hud/Meta/Energy | audit | no | gap_energy_icon_unused | 55×66 |
| Fillter.PNG | UI/Hud/Meta/Fillter | audit | no | gap_no_ui_hook | 25×25 |
| Fire_card.PNG | UI/Hud/Meta/Fire_card | audit | yes | upgrade_card_line1_bg | 84×93 |
| Fire_icon.PNG | UI/Hud/Meta/Fire_icon | audit | yes | upgrade_card_line1_fallback | 36×37 |
| Gear_tab.PNG | UI/Hud/Meta/Gear_tab | audit | yes | treasure_tab_rare | 69×39 |
| Gift.PNG | UI/Hud/Meta/Gift | audit | no | gap_no_ui_hook | 64×64 |
| Green_button.PNG | UI/Hud/Meta/Green_button | audit | yes | homepage_cta_upgrade; upgrade_detail_cta | 88×41 |
| Hints.PNG | UI/Hud/Meta/Hints | audit | no | gap_no_ui_hook | 55×65 |
| Item_tab.PNG | UI/Hud/Meta/Item_tab | audit | yes | treasure_tab_epic | 71×39 |
| Lock.PNG | UI/Hud/Meta/Lock | audit | yes | upgrade_card_need_gold | 24×29 |
| Mail_button.PNG | UI/Hud/Meta/Mail_button | audit | yes | homepage_icon_mail | 62×56 |
| Material_tab.PNG | UI/Hud/Meta/Material_tab | audit | yes | treasure_tab_legendary | 70×38 |
| Metal_card.PNG | UI/Hud/Meta/Metal_card | audit | yes | upgrade_card_line2_bg | 86×93 |
| Metal_icon.PNG | UI/Hud/Meta/Metal_icon | audit | yes | upgrade_card_line2_fallback | 38×37 |
| Next.PNG | UI/Hud/Meta/Next | audit | yes | homepage_level_nav_next | 61×34 |
| Not_selected.PNG | UI/Hud/Meta/Not_selected | audit | yes | duel_roll_slot_normal | 31×32 |
| Notify.PNG | UI/Hud/Meta/Notify | audit | no | gap_no_ui_hook | 23×23 |
| Off.PNG | UI/Hud/Meta/Off | audit | no | gap_sound_off_icon | 71×32 |
| On.PNG | UI/Hud/Meta/On | audit | yes | duel_settings_decor_only | 70×32 |
| Orange_button.PNG | UI/Hud/Meta/Orange_button | audit | yes | duel_bottom_start_combat | 88×41 |
| Piggy_bank.PNG | UI/Hud/Meta/Piggy_bank | audit | no | gap_no_ui_hook | 59×64 |
| Prev.PNG | UI/Hud/Meta/Prev | audit | yes | homepage_level_nav_prev | 60×33 |
| Quest_button.PNG | UI/Hud/Meta/Quest_button | audit | no | gap_no_ui_hook | 62×56 |
| Red_button.PNG | UI/Hud/Meta/Red_button | audit | no | gap_cta_unused | 87×41 |
| Reward_button.PNG | UI/Hud/Meta/Reward_button | audit | yes | treasure_roll_cta | 62×57 |
| Search.PNG | UI/Hud/Meta/Search | audit | no | gap_no_ui_hook | 26×27 |
| Selected.PNG | UI/Hud/Meta/Selected | audit | yes | duel_roll_slot_highlight | 32×32 |
| Setting.PNG | UI/Hud/Meta/Setting | audit | yes | homepage_icon_settings; duel_top_settings | 45×45 |
| Shuffle.PNG | UI/Hud/Meta/Shuffle | audit | no | gap_no_ui_hook | 56×66 |
| Summon_button.PNG | UI/Hud/Meta/Summon_button | audit | no | gap_no_ui_hook | 149×129 |
| Timer.PNG | UI/Hud/Meta/Timer | audit | no | gap_no_ui_hook | 54×65 |
| Turn_off.PNG | UI/Hud/Meta/Turn_off | audit | yes | duel_settings_decor_only | 43×44 |
| Water_card.PNG | UI/Hud/Meta/Water_card | audit | yes | upgrade_card_line3_bg | 85×93 |
| Water_icon.PNG | UI/Hud/Meta/Water_icon | audit | yes | upgrade_card_line3_fallback | 37×37 |
| Wood_card.PNG | UI/Hud/Meta/Wood_card | audit | yes | upgrade_card_line0_bg | 86×93 |
| Wood_icon.PNG | UI/Hud/Meta/Wood_icon | audit | yes | upgrade_card_line0_fallback | 37×37 |


## GAP



- **Background_combat** — unused_pack_background
- **Add** — no_ui_hook
- **Ads_block** — no_ui_hook
- **Advance_chest** — treasure_feature
- **All_icon** — no_ui_hook
- **Arena_button** — no_ui_hook
- **Background** — unused_pack_background
- **Bag** — inventory_feature
- **Clam_notify** — typo_clan_notify
- **Clan_button** — no_ui_hook
- **Complete_notify** — no_ui_hook
- **Energy** — energy_icon_unused
- **Fillter** — no_ui_hook
- **Gift** — no_ui_hook
- **Hints** — no_ui_hook
- **Notify** — no_ui_hook
- **Off** — sound_off_icon
- **Piggy_bank** — no_ui_hook
- **Quest_button** — no_ui_hook
- **Red_button** — cta_unused
- **Search** — no_ui_hook
- **Shuffle** — no_ui_hook
- **Summon_button** — no_ui_hook
- **Timer** — no_ui_hook


## Meta resource HUD (`bar_and_resource/`)

| file | resourcesPath | wired | notes |
|------|---------------|-------|-------|
| coin + coin_bar | UI/Meta/Resource/coin, coin_bar | yes | Gold row |
| key + key_bar | UI/Meta/Resource/key, key_bar | yes | Treasure Keys row |
| energy + energy_bar | UI/Meta/Resource/energy, energy_bar | yes | Energy stub row |

Sync: `Tools/sync_bar_resource_from_root.sh` — loader: `MetaResourceUiSprites.cs`, UI: `MetaResourceHudBar.cs`

Removed legacy HUD: `Hud/Meta/coin_bar`, `diamond_bar`, `energy_bar` (orphan metas deleted).

## Treasure pack (non-HUD)

| Path | Count | Loader | Notes |
|------|-------|--------|-------|
| `UI/Treasure/Icons/c_1` … `c_10` | 10 | `TreasureUiSprites` | Common; id `tr_c_01` … |
| `UI/Treasure/Icons/r_1` … `r_7` | 7 | same | Rare |
| `UI/Treasure/Icons/e_1` … `e_4` | 4 | same | Epic |
| `UI/Treasure/Icons/l_1` … `l_4` | 4 | same | Legendary |
| `table_1` | 1 | treasure **detail** panel backdrop | |
| `type_1` … `type_3` | 3 | detail effect badge | |

Sync: `Tools/sync_treasures_from_root.sh` → `python3 Tools/fix_treasure_sprite_metas.py`

## Related docs

- [`ASSET_REQUEST.md`](../ASSET_REQUEST.md)
- [`SYSTEM_MAP.md`](SYSTEM_MAP.md)
