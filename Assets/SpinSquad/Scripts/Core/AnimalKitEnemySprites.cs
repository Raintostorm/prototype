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
        const string FramesFolder = "Battle/AnimalKitFrames";

        static readonly string[] FrameProfilePool =
        {
            "Crocodile",
            "Rabbit",
            "Tiger"
        };

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

        public sealed class Profile
        {
            public readonly string Name;
            public readonly Sprite Idle;
            public readonly Sprite Walk;
            public readonly Sprite Attack;
            public readonly Sprite Die;

            public Profile(string name, Sprite idle, Sprite walk, Sprite attack, Sprite die)
            {
                Name = name;
                Idle = idle;
                Walk = walk != null ? walk : idle;
                Attack = attack != null ? attack : idle;
                Die = die != null ? die : idle;
            }
        }

        public static Profile PickProfile(UnitDefinition def, int wave, int spawnIndex)
        {
            var profileName = FrameProfilePool[Mathf.Abs((wave - 1) + spawnIndex) % FrameProfilePool.Length];
            var profile = LoadProfile(profileName);
            if (profile != null && profile.Idle != null)
                return profile;

            var fallback = Pick(def, wave, spawnIndex);
            return fallback != null ? new Profile("Fallback", fallback, fallback, fallback, fallback) : null;
        }

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

        static Profile LoadProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return null;

            var basePath = FramesFolder + "/" + profileName + "/";
            var idle = Resources.Load<Sprite>(basePath + "idle_0");
            var walk = Resources.Load<Sprite>(basePath + "walk_0");
            var attack = Resources.Load<Sprite>(basePath + "attack_0");
            var die = Resources.Load<Sprite>(basePath + "die_0");

            // The extracted Crocodile kit only contains two verified character poses:
            // 3158 = mouth closed, 3543 = mouth open. The previous mapping reused
            // the open-mouth pose for Walk, making the Attack transition invisible.
            if (profileName == "Crocodile")
                walk = idle;

            return new Profile(profileName, idle, walk, attack, die);
        }
    }
}
