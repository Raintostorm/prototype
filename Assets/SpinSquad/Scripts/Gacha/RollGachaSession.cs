using System;

namespace SpinSquad.Gacha
{
    /// <summary>Trừ coin, sinh 6 ô, resolve, cộng thưởng coin + enqueue ally + buff run.</summary>
    public static class RollGachaSession
    {
        public static bool TryExecuteRoll(out RollPayout payout, out string error)
        {
            payout = null;
            error = null;

            if (!RollWallet.TrySpend(SixSlotRollResolver.RollCostCoins))
            {
                error = $"Không đủ coin (cần {SixSlotRollResolver.RollCostCoins}).";
                return false;
            }

            var rng = new System.Random((int)(DateTime.UtcNow.Ticks ^ Guid.NewGuid().GetHashCode()));
            var cells = SixSlotRollGenerator.Generate(rng);
            payout = SixSlotRollResolver.Resolve(cells, rng);

            RollWallet.Add(payout.CoinAwarded);
            foreach (var g in payout.AllyGrants)
                PendingRollGrants.Enqueue(g);
            foreach (var b in payout.BuffAdds)
                BattleRunBuffs.AddFromRoll(b, 1);

            return true;
        }
    }
}
