using UnityEngine;

namespace SpinSquad.Gacha
{
    /// <summary>
    /// Buff cộng dồn cho run combat (SampleScene). Áp từ <see cref="ActiveFromWave"/> đến hết run.
    /// Clear khi <see cref="DuelDirector"/> spawn / restart trận (<c>SpawnAndRegisterFighters</c>).
    /// </summary>
    public static class BattleRunBuffs
    {
        public static float HpPct { get; private set; }
        public static float DmgPct { get; private set; }
        public static float AtkSpeedPct { get; private set; }
        /// <summary>0–1 mỗi nhát (roll ngẫu nhiên khi gây damage).</summary>
        public static float CritChance { get; private set; }

        public static int ActiveFromWave { get; private set; } = 1;

        public static void Clear()
        {
            HpPct = 0f;
            DmgPct = 0f;
            AtkSpeedPct = 0f;
            CritChance = 0f;
            ActiveFromWave = 1;
        }

        public static bool BuffsApply(int currentWave) => currentWave >= ActiveFromWave;

        public static void AddFromRoll(BuffRollAdd add, int activeFromWave = 1)
        {
            ActiveFromWave = Mathf.Min(ActiveFromWave, activeFromWave);
            switch (add.Stat)
            {
                case BuffStatKind.HpPct:
                    HpPct += add.Value;
                    break;
                case BuffStatKind.DmgPct:
                    DmgPct += add.Value;
                    break;
                case BuffStatKind.AtkSpeedPct:
                    AtkSpeedPct += add.Value;
                    break;
                case BuffStatKind.CritChance:
                    CritChance += add.Value;
                    break;
                default:
                    break;
            }
        }
    }
}
