using System.Collections.Generic;

namespace SpinSquad.UI
{
    /// <summary>Static map: sprite asset name → UI role (for audit doc + tooling).</summary>
    public static class HudUiWiringRegistry
    {
        public static readonly IReadOnlyDictionary<string, string> RoleByAssetName = new Dictionary<string, string>
        {
            { "Setting", "homepage_icon_settings; duel_top_settings" },
            { "Mail_button", "homepage_icon_mail" },
            { "Shop_icon", "homepage_icon_shop" },
            { "Battle_button", "homepage_cta_play" },
            { "Green_button", "homepage_cta_upgrade; upgrade_detail_cta" },
            { "Chest", "homepage_cta_treasure" },
            { "Orange_button", "duel_bottom_start_combat" },
            { "Blue_button", "duel_bottom_add_ally" },
            { "Background", "gap_unused_pack_background" },
            { "Background_combat", "gap_unused_pack_background" },
            { "Back", "homepage_level_overlay_close; meta_scenes_back" },
            { "continue", "duel_pause_resume" },
            { "pause", "duel_top_pause" },
            { "Stop_on", "duel_top_pause_legacy" },
            { "Stop_off", "duel_pause_overlay_legacy" },
            { "Speed_x2_off", "duel_top_speed_off" },
            { "Speed_x2_on", "duel_top_speed_on" },
            { "Restart", "duel_bottom_restart" },
            { "Home_in_battle", "duel_bottom_home" },
            { "Merge", "duel_ally_menu_merge" },
            { "Sell", "duel_ally_menu_sell" },
            { "Selected", "duel_roll_slot_highlight" },
            { "Not_selected", "duel_roll_slot_normal" },
            { "Volume_on", "duel_settings_decor_only" },
            { "Volume_off", "duel_settings_decor_only" },
            { "On", "duel_settings_decor_only" },
            { "Off", "gap_sound_off_icon" },
            { "Turn_off", "duel_settings_decor_only" },
            { "Wood_icon", "upgrade_card_line0_fallback" },
            { "Fire_icon", "upgrade_card_line1_fallback" },
            { "Metal_icon", "upgrade_card_line2_fallback" },
            { "Water_icon", "upgrade_card_line3_fallback" },
            { "Earth_icon", "upgrade_card_line4_fallback" },
            { "Wood_card", "upgrade_card_line0_bg" },
            { "Fire_card", "upgrade_card_line1_bg" },
            { "Metal_card", "upgrade_card_line2_bg" },
            { "Water_card", "upgrade_card_line3_bg" },
            { "Earth_card", "upgrade_card_line4_bg" },
            { "Prev", "homepage_level_nav_prev" },
            { "Next", "homepage_level_nav_next" },
            { "All_tab", "treasure_tab_common" },
            { "Gear_tab", "treasure_tab_rare" },
            { "Item_tab", "treasure_tab_epic" },
            { "Material_tab", "treasure_tab_legendary" },
            { "Lock", "upgrade_card_need_gold" },
            { "Reward_button", "treasure_roll_cta" },
            { "Ads_block", "gap_no_ui_hook" },
            { "Advance_chest", "gap_treasure_feature" },
            { "All_icon", "gap_no_ui_hook" },
            { "Arena_button", "gap_no_ui_hook" },
            { "Bag", "gap_inventory_feature" },
            { "Clam_notify", "gap_typo_clan_notify" },
            { "Clan_button", "gap_no_ui_hook" },
            { "Complete_notify", "gap_no_ui_hook" },
            { "Energy", "gap_energy_icon_unused" },
            { "Fillter", "gap_no_ui_hook" },
            { "Gift", "gap_no_ui_hook" },
            { "Hints", "gap_no_ui_hook" },
            { "Notify", "gap_no_ui_hook" },
            { "Piggy_bank", "gap_no_ui_hook" },
            { "Quest_button", "gap_no_ui_hook" },
            { "Red_button", "gap_cta_unused" },
            { "Search", "gap_no_ui_hook" },
            { "Shuffle", "gap_no_ui_hook" },
            { "Summon_button", "gap_no_ui_hook" },
            { "Timer", "gap_no_ui_hook" },
            { "Add", "gap_no_ui_hook" },
        };

        public static string ResourcesPathFor(string assetName, bool combat)
        {
            var folder = combat ? "UI/Hud/Combat" : "UI/Hud/Meta";
            return folder + "/" + assetName;
        }

        public static bool IsCombatAsset(string assetName)
        {
            switch (assetName)
            {
                case "Back":
                case "Background_combat":
                case "Home_in_battle":
                case "Merge":
                case "Restart":
                case "Sell":
                case "Shop_icon":
                case "Speed_x2_off":
                case "Speed_x2_on":
                case "Stop_off":
                case "Stop_on":
                case "Volume_off":
                case "Volume_on":
                    return true;
                default:
                    return false;
            }
        }
    }
}
