using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>
    /// Local-only enemy placeholder sprites from the animal kit.
    /// Assets are intentionally git-ignored; missing sprites safely fall back to shape placeholders.
    /// </summary>
    public static class AnimalKitEnemySprites
    {
        const string Folder = "Battle/AnimalKitLocal";

        static readonly string[] MeleePool =
        {
            "enemy_wolf",
            "enemy_boar",
            "enemy_leafbeast"
        };

        static readonly string[] RangedPool =
        {
            "enemy_shark",
            "enemy_spirit"
        };

        public static Sprite Pick(UnitDefinition def, int wave, int spawnIndex)
        {
            if (wave >= 5 && spawnIndex == 0)
            {
                var boss = Load("enemy_boss");
                if (boss != null)
                    return boss;
            }

            var ranged = def != null && def.RangedAttack;
            var pool = ranged ? RangedPool : MeleePool;
            if (pool.Length == 0)
                return null;

            var i = Mathf.Abs((wave - 1) + spawnIndex) % pool.Length;
            return Load(pool[i]);
        }

        static Sprite Load(string assetName)
        {
            return Resources.Load<Sprite>(Folder + "/" + assetName);
        }
    }
}
