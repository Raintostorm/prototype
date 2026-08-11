using System;
using System.Collections.Generic;
using System.Text;
using SpinSquad.Data;

namespace SpinSquad.Gacha
{
    public static class SixSlotRollResolver
    {
        public const int RollCostCoins = 20;

        public static RollPayout Resolve(RollCellKind[] cells, System.Random rng)
        {
            if (cells == null || cells.Length != 6)
                throw new ArgumentException("Expected 6 cells.", nameof(cells));

            var payout = new RollPayout((RollCellKind[])cells.Clone());

            var cCoin = 0;
            var cAlly = 0;
            var cBuff = 0;
            foreach (var c in cells)
            {
                switch (c)
                {
                    case RollCellKind.Coin:
                        cCoin++;
                        break;
                    case RollCellKind.Ally:
                        cAlly++;
                        break;
                    case RollCellKind.Buff:
                        cBuff++;
                        break;
                }
            }

            if (cCoin == 6 || cAlly == 6 || cBuff == 6)
                payout.Jackpot = true;

            var max = Math.Max(cCoin, Math.Max(cAlly, cBuff));
            var winners = new List<RollCellKind>(3);
            if (cCoin == max)
                winners.Add(RollCellKind.Coin);
            if (cAlly == max)
                winners.Add(RollCellKind.Ally);
            if (cBuff == max)
                winners.Add(RollCellKind.Buff);

            var sb = new StringBuilder();
            foreach (var w in winners)
            {
                var n = w switch
                {
                    RollCellKind.Coin => cCoin,
                    RollCellKind.Ally => cAlly,
                    _ => cBuff
                };

                if (n < 3)
                    continue;

                switch (w)
                {
                    case RollCellKind.Coin:
                    {
                        var coins = CoinForCluster(n);
                        payout.CoinAwarded += coins;
                        sb.Append($"Coin +{coins} ({LabelRarity(n)}). ");
                        break;
                    }
                    case RollCellKind.Ally:
                    {
                        var r = RarityForAllyCluster(n);
                        var line = (byte)AllyLineCatalog.PickRandomLineIndex(rng);
                        payout.AllyGrants.Add(new AllyRollGrant(line, r));
                        sb.Append($"Ally L{line} {r} ({LabelRarity(n)}). ");
                        break;
                    }
                    default:
                    {
                        var stat = (BuffStatKind)rng.Next(0, 4);
                        var val = BuffValueFor(stat, n);
                        payout.BuffAdds.Add(new BuffRollAdd(stat, val));
                        sb.Append($"Buff {stat} +{FormatBuff(stat, val)} ({LabelRarity(n)}). ");
                        break;
                    }
                }
            }

            if (payout.Jackpot)
                sb.Append("Jackpot! ");

            payout.SummaryLine = sb.ToString().Trim();
            if (string.IsNullOrEmpty(payout.SummaryLine))
                payout.SummaryLine = "No reward (fewer than 3 matching slots).";

            return payout;
        }

        static string LabelRarity(int cluster) => cluster switch
        {
            3 => "Common",
            4 => "Rare",
            5 => "Epic",
            6 => "Legendary",
            _ => cluster.ToString()
        };

        static int CoinForCluster(int n) => n switch
        {
            3 => 15,
            4 => 40,
            5 => 80,
            6 => 200,
            _ => 0
        };

        static Rarity RarityForAllyCluster(int n) => n switch
        {
            3 => Rarity.Common,
            4 => Rarity.Rare,
            5 => Rarity.Epic,
            6 => Rarity.Legendary,
            _ => Rarity.Common
        };

        static float BuffValueFor(BuffStatKind stat, int cluster)
        {
            return stat switch
            {
                BuffStatKind.HpPct => cluster switch
                {
                    3 => 0.1f,
                    4 => 0.25f,
                    5 => 1f,
                    6 => 2f,
                    _ => 0f
                },
                BuffStatKind.DmgPct => cluster switch
                {
                    3 => 0.1f,
                    4 => 0.25f,
                    5 => 1f,
                    6 => 2f,
                    _ => 0f
                },
                BuffStatKind.AtkSpeedPct => cluster switch
                {
                    3 => 0.05f,
                    4 => 0.15f,
                    5 => 0.5f,
                    6 => 1f,
                    _ => 0f
                },
                _ => cluster switch
                {
                    3 => 0.01f,
                    4 => 0.02f,
                    5 => 0.05f,
                    6 => 0.15f,
                    _ => 0f
                }
            };
        }

        static string FormatBuff(BuffStatKind stat, float v)
        {
            if (stat == BuffStatKind.CritChance)
                return $"{v * 100f:0.#}% crit";
            return $"{v * 100f:0.#}%";
        }
    }
}
