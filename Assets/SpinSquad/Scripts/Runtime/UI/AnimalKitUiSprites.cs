using UnityEngine;

namespace SpinSquad.UI
{
    /// <summary>
    /// Local-only animal placeholder UI skin.
    /// Sprites live under Resources/UI/AnimalKitLocal and are intentionally git-ignored.
    /// If the local kit is missing, callers fall back to the normal generated/HUD sprites.
    /// </summary>
    public static class AnimalKitUiSprites
    {
        const string Folder = "UI/AnimalKitLocal";

        public static Sprite Home => Get("animal_home_boss");
        public static Sprite Upgrade => Get("animal_upgrade_wolf");
        public static Sprite Treasure => Get("animal_treasure_shark");
        public static Sprite Shop => Get("animal_shop_boar");
        public static Sprite Settings => Get("animal_settings_spirit");
        public static Sprite Bag => Get("animal_bag_leafbeast");
        public static Sprite Mail => Get("animal_mail_ladybug");
        public static Sprite Combat => Get("animal_combat_sheep");

        static Sprite Get(string assetName)
        {
            return Resources.Load<Sprite>(Folder + "/" + assetName);
        }
    }
}
