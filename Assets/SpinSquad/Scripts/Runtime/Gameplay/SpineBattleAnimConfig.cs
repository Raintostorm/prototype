using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>Clip names + attack cadence for <see cref="SpineBattleAnimator"/>; assign on prefab to keep data separate from locomotion logic.</summary>
    [CreateAssetMenu(fileName = "SpineBattleAnimConfig", menuName = "SpinSquad/Combat/Spine Battle Anim Config")]
    public sealed class SpineBattleAnimConfig : ScriptableObject
    {
        [SerializeField] string idleAnimationName = "idle";
        [Tooltip("Empty uses literal \"walk\"; missing clip falls back to idle in SpineBattleAnimator.")]
        [SerializeField] string walkAnimationName = "walk";
        [SerializeField] string attackAnimationName = "attack";
        [SerializeField] string dieAnimationName = "died";
        [Tooltip("Khớp nhịp với DuelActor.touchStrikeInterval / ranged interval — quá cao sẽ bỏ qua nhiều PlayAttack.")]
        [SerializeField] float attackMinInterval = 0.12f;

        public string IdleAnimationName => idleAnimationName;
        public string WalkAnimationName => walkAnimationName;
        public string AttackAnimationName => attackAnimationName;
        public string DieAnimationName => dieAnimationName;
        public float AttackMinInterval => attackMinInterval;
    }
}
