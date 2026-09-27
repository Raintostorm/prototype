using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>
    /// Asset-authored settings copied onto the runtime Spine driver when a battle unit is spawned.
    /// Keeping these settings on the visual prefab avoids name-based flip/offset exceptions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpineBattleRigProfile : MonoBehaviour
    {
        [Header("Animation clips")]
        [SerializeField] string idleAnimationName = "idle";
        [SerializeField] string walkAnimationName = "walk";
        [SerializeField] string attackAnimationName = "attack";
        [SerializeField] string dieAnimationName = "died";
        [SerializeField, Min(0.05f)] float attackAnimationSpeed = 1f;

        [Header("Battle presentation")]
        [Tooltip("Enable only when the source skeleton was authored facing opposite to its team convention.")]
        [SerializeField] bool invertFacing;
        [Tooltip("Fine adjustment applied after automatic bounds alignment.")]
        [SerializeField] Vector2 anchorOffset;

        public string IdleAnimationName => idleAnimationName;
        public string WalkAnimationName => walkAnimationName;
        public string AttackAnimationName => attackAnimationName;
        public string DieAnimationName => dieAnimationName;
        public float AttackAnimationSpeed => Mathf.Max(0.05f, attackAnimationSpeed);
        public bool InvertFacing => invertFacing;
        public Vector2 AnchorOffset => anchorOffset;
    }
}
