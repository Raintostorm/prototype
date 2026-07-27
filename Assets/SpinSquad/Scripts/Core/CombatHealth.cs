using System;
using UnityEngine;

namespace SpinSquad.Core
{
    public enum CombatFaction
    {
        Ally,
        Enemy
    }

    /// <summary>
    /// Máu + sự kiện chết. Gắn cùng GameObject với <see cref="DuelActor"/>.
    /// </summary>
    public sealed class CombatHealth : MonoBehaviour
    {
        [SerializeField] private CombatFaction faction;
        [SerializeField] private float maxHitPoints = 30f;

        private float _current;

        public CombatFaction Faction => faction;
        public float Current => _current;
        public float Max => maxHitPoints;
        public bool IsDead => _current <= 0f;

        /// <summary>Raised after valid damage is applied, including a killing hit.</summary>
        public event Action<CombatHealth, float> Damaged;
        public event Action<CombatHealth> Died;

        private void Awake()
        {
            _current = maxHitPoints;
        }

        public void Configure(CombatFaction f, float maxHp)
        {
            faction = f;
            maxHitPoints = maxHp;
            _current = maxHitPoints;
        }

        /// <summary>Hồi đủ máu (sau restart / setup lại trận).</summary>
        public void ResetToFull()
        {
            _current = maxHitPoints;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            var applied = Mathf.Min(_current, amount);
            _current -= amount;
            Damaged?.Invoke(this, applied);
            if (_current <= 0f)
            {
                _current = 0f;
                Died?.Invoke(this);
            }
        }
    }
}
