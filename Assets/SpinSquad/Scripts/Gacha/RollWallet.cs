using UnityEngine;

namespace SpinSquad.Gacha
{
    /// <summary>Coin chỉ dùng để roll trong trận — lưu PlayerPrefs (mỗi lần vào trận reset về mức prep).</summary>
    public static class RollWallet
    {
        const string Key = "SpinSquad_RollCoins";
        const int DefaultCoins = 40;

        public static int Balance
        {
            get => PlayerPrefs.GetInt(Key, DefaultCoins);
            set
            {
                PlayerPrefs.SetInt(Key, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>Đặt lại ví khi bắt đầu / restart trận (mặc định 40 = đúng 2 lần roll × 20 trước wave 1).</summary>
        public static void ResetForNewBattle(int startingCoins)
        {
            Balance = Mathf.Max(0, startingCoins);
        }

        public static bool TrySpend(int amount)
        {
            if (amount <= 0)
                return true;
            var b = Balance;
            if (b < amount)
                return false;
            Balance = b - amount;
            return true;
        }

        public static void Add(int amount)
        {
            if (amount == 0)
                return;
            Balance = Balance + amount;
        }
    }
}
