using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpinSquad.Gacha
{
    /// <summary>Sinh 6 ô với ràng buộc không (2,2,2) — luôn có ít nhất một loại ≥3.</summary>
    public static class SixSlotRollGenerator
    {
        /// <summary>Lũy thừa trọng số theo số ô Ally (cao hơn = ally xuất hiện nhiều hơn).</summary>
        const float AllyCountBiasPow = 1.78f;

        /// <summary>Giảm nhẹ trọng số khi nhiều ô Coin hoặc Buff (để ally tương đối nổi hơn).</summary>
        const float CoinBuffCountBiasPow = 0.9f;

        public static RollCellKind[] Generate(System.Random rng)
        {
            var (coin, ally, buff) = PickPartitionTripleWeighted(rng);
            var kinds = new List<RollCellKind>(6);
            for (var i = 0; i < coin; i++)
                kinds.Add(RollCellKind.Coin);
            for (var i = 0; i < ally; i++)
                kinds.Add(RollCellKind.Ally);
            for (var i = 0; i < buff; i++)
                kinds.Add(RollCellKind.Buff);

            Shuffle(kinds, rng);

            var arr = new RollCellKind[6];
            for (var i = 0; i < 6; i++)
                arr[i] = kinds[i];
            return arr;
        }

        /// <summary>Chọn (coin, ally, buff) hợp lệ với trọng số nghiêng về nhiều ô Ally hơn Coin/Buff.</summary>
        static (int coin, int ally, int buff) PickPartitionTripleWeighted(System.Random rng)
        {
            var entries = new List<(int c, int a, int b, int w)>(12);
            for (var c = 1; c <= 4; c++)
            {
                for (var a = 1; a <= 4; a++)
                {
                    for (var b = 1; b <= 4; b++)
                    {
                        if (c + a + b != 6)
                            continue;
                        if (c == 2 && a == 2 && b == 2)
                            continue;

                        var wf = Mathf.Pow(AllyCountBiasPow, a)
                                 * Mathf.Pow(CoinBuffCountBiasPow, c + b);
                        var w = Mathf.Max(1, Mathf.RoundToInt(wf * 1000f));
                        entries.Add((c, a, b, w));
                    }
                }
            }

            var total = 0;
            foreach (var e in entries)
                total += e.w;

            var r = rng.Next(total);
            foreach (var e in entries)
            {
                r -= e.w;
                if (r < 0)
                    return (e.c, e.a, e.b);
            }

            var fallback = entries[0];
            return (fallback.c, fallback.a, fallback.b);
        }

        static void Shuffle(List<RollCellKind> list, System.Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
