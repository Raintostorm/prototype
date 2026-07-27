using System;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>
    /// Spine 3.8 battle driver: mirrors <see cref="Line1BattleSpriteAnimator"/> hooks used by <see cref="DuelActor"/>.
    /// Facing: <see cref="DuelActor"/> passes movement direction; prep ally uses +X (toward enemy grid on the right).
    /// Facing uses <see cref="Skeleton.ScaleX"/> (Spine flip), not negative Unity scale — mesh follows skeleton flip. <see cref="invertFacingSign"/> adjusts
    /// vs faction defaults. Re-applied after <c>Initialize</c> because <see cref="SkeletonAnimation.Initialize"/> resets skeleton scale from <c>initialFlipX</c>.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    [DisallowMultipleComponent]
    public sealed class SpineBattleAnimator : MonoBehaviour, IBattleVisualDriver
    {
        [Tooltip("Optional: clip names + attack interval. When set, overrides inline fields below.")]
        [SerializeField] SpineBattleAnimConfig animConfig;

        [SerializeField] string idleAnimationName = "idle";
        [Tooltip("Để trống = dùng \"walk\"; không có clip walk thì khi di chuyển fallback idle.")]
        [SerializeField] string walkAnimationName = "walk";
        [SerializeField] string attackAnimationName = "attack";
        [SerializeField] string dieAnimationName = "died";
        [SerializeField] float attackMinInterval = 0.12f;
        [Tooltip("Chỉ tăng tốc phát clip attack (visual), không đổi damage timing trong DuelActor.")]
        [SerializeField] float attackAnimationSpeed = 2.4f;
        [Tooltip("Fallback hit timing (0..1) when Spine attack clip has no 'hit' event.")]
        [Range(0.05f, 0.95f)]
        [SerializeField] float fallbackHitNormalizedTime = 0.33f;
        [SerializeField] bool lockFacingOnDie = true;
        [SerializeField] bool invertFacingSign = false;

        [Tooltip("Log Console: clip locomotion/attack/die và lý do bỏ qua. Unit spawn runtime: bật SpineBattleAnimator.DebugLogAllInstances.")]
        [SerializeField] bool debugAnimationLog;

        /// <summary>Bật log cho mọi instance (ally spawn bằng code không có prefab để tick Inspector).</summary>
        public static bool DebugLogAllInstances;

        SkeletonAnimation _skeletonAnimation;
        Spine.AnimationState _state;
        CombatHealth _health;
        bool _dead;
        float _nextAttackTime;
        bool _isMoving;
        Spine.TrackEntry _attackEntry;
        float _baseAbsScaleX;
        bool _hasBaseScaleX;
        string _lastLocomotionLogKey;
        bool _loggedMissingSkeleton;
        Action _onAttackHit;
        Action _onAttackComplete;
        bool _attackHitFired;

        bool LogEnabled => DebugLogAllInstances || debugAnimationLog;

        string IdleName => animConfig != null ? animConfig.IdleAnimationName : idleAnimationName;
        string WalkName => animConfig != null ? animConfig.WalkAnimationName : walkAnimationName;
        string AttackName => animConfig != null ? animConfig.AttackAnimationName : attackAnimationName;
        string DieName => animConfig != null ? animConfig.DieAnimationName : dieAnimationName;
        float AttackInterval => animConfig != null ? animConfig.AttackMinInterval : attackMinInterval;

        void Awake()
        {
            _health = GetComponent<CombatHealth>();
            ResolveSkeleton();
        }

        void Start()
        {
            // Initialize(reset) chạy lại sau OnEnable — cần facing + locomotion đồng bộ.
            ResolveSkeleton();
            if (_health != null)
                ApplyDefaultFacingForFaction();
            if (_state != null && !_dead)
                PlayLocomotion();
        }

        void OnEnable()
        {
            if (_health == null)
                _health = GetComponent<CombatHealth>();
            if (_health != null)
                _health.Died += OnDied;
            _dead = false;
            ResolveSkeleton();
            ApplyDefaultFacingForFaction();
            if (_state != null && !_dead)
                PlayLocomotion();
        }

        void OnDisable()
        {
            if (_health != null)
                _health.Died -= OnDied;
            ClearAttackCompleteHandler();
        }

        void ResolveSkeleton()
        {
            if (_skeletonAnimation == null)
                _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
            if (_skeletonAnimation == null)
            {
                if (LogEnabled && !_loggedMissingSkeleton)
                {
                    _loggedMissingSkeleton = true;
                    Debug.LogWarning("[SpineBattleAnimator] Không tìm thấy SkeletonAnimation trong children của " + name + ".", this);
                }
                return;
            }

            _skeletonAnimation.Initialize(true);
            _state = _skeletonAnimation.AnimationState;
            _hasBaseScaleX = false;
            CacheBaseScaleX();
            if (_health != null)
                ApplyDefaultFacingForFaction();
        }

        void OnDied(CombatHealth dead)
        {
            if (dead == null || dead != _health)
                return;
            PlayDie();
        }

        public void UpdateFacing(float dirX)
        {
            if (_dead && lockFacingOnDie)
                return;
            if (Mathf.Abs(dirX) < 0.01f)
                return;
            ApplyFacing(dirX < 0f ? -1f : 1f);
        }

        public void SetMoving(bool isMoving)
        {
            if (_dead || _state == null)
            {
                if (LogEnabled && _isMoving != isMoving)
                    Debug.Log("[SpineBattleAnimator] " + name + " SetMoving(" + isMoving + ") bỏ qua: dead=" + _dead + " stateNull=" + (_state == null) + ".", this);
                return;
            }
            _isMoving = isMoving;
            if (IsAttackPlayingOnTrack0())
                return;
            PlayLocomotion();
        }

        public void ForceIdleLoop()
        {
            if (_dead || _state == null)
                return;
            _isMoving = false;
            ClearAttackCompleteHandler();
            PlayLocomotion();
        }

        public void AddMoveDistance(float worldDistance)
        {
            // Line1 uses distance for stride bob; Spine walk loops — no-op.
        }

        public void PlayAttack(Action onHit, Action onComplete = null)
        {
            var atk = AttackName;
            if (_dead || _state == null || string.IsNullOrEmpty(atk))
                return;
            if (Time.time < _nextAttackTime)
                return;
            if (IsAttackPlayingOnTrack0())
                return;

            _nextAttackTime = Time.time + Mathf.Max(0.01f, AttackInterval);
            ClearAttackCompleteHandler();
            _onAttackHit = onHit;
            _onAttackComplete = onComplete;
            _attackHitFired = false;

            if (_skeletonAnimation.Skeleton.Data.FindAnimation(atk) == null)
            {
                if (LogEnabled)
                    Debug.LogWarning("[SpineBattleAnimator] " + name + " không có clip '" + atk + "' trong Skeleton.Data.", this);
                return;
            }

            _attackEntry = _state.SetAnimation(0, atk, false);
            if (_attackEntry != null)
                _attackEntry.TimeScale = Mathf.Max(0.05f, attackAnimationSpeed);
            if (LogEnabled)
                Debug.Log("[SpineBattleAnimator] " + name + " → attack '" + atk + "'.", this);
            if (_attackEntry != null)
            {
                _attackEntry.Event += OnAttackEvent;
                _attackEntry.Complete += OnAttackComplete;
            }
        }

        void OnAttackEvent(Spine.TrackEntry entry, Spine.Event e)
        {
            if (entry != _attackEntry || e == null)
                return;
            if (string.Equals(e.Data.Name, "hit", StringComparison.OrdinalIgnoreCase))
                FireAttackHitOnce();
        }

        void OnAttackComplete(Spine.TrackEntry entry)
        {
            if (entry != _attackEntry)
                return;
            FireAttackHitOnce();
            _onAttackComplete?.Invoke();
            ClearAttackCompleteHandler();
            if (_dead)
                return;
            PlayLocomotion();
        }

        void Update()
        {
            if (_dead || _state == null || _attackEntry == null)
                return;
            if (!_attackHitFired)
            {
                var anim = _attackEntry.Animation;
                var animEnd = anim != null ? anim.Duration : 0f;
                if (animEnd > 0.0001f)
                {
                    var normalized = _attackEntry.TrackTime / animEnd;
                    if (normalized >= Mathf.Clamp01(fallbackHitNormalizedTime))
                        FireAttackHitOnce();
                }
            }
            // Some transitions can replace track0 without firing Complete on our cached entry.
            if (_state.GetCurrent(0) != _attackEntry)
            {
                FireAttackHitOnce();
                _onAttackComplete?.Invoke();
                ClearAttackCompleteHandler();
                PlayLocomotion();
            }
        }

        bool IsAttackPlayingOnTrack0()
        {
            if (_attackEntry == null || _state == null)
                return false;
            if (_state.GetCurrent(0) != _attackEntry)
            {
                ClearAttackCompleteHandler();
                return false;
            }
            return true;
        }

        void ClearAttackCompleteHandler()
        {
            if (_attackEntry != null)
            {
                _attackEntry.Event -= OnAttackEvent;
                _attackEntry.Complete -= OnAttackComplete;
                _attackEntry = null;
            }
            _onAttackHit = null;
            _onAttackComplete = null;
            _attackHitFired = false;
        }

        void FireAttackHitOnce()
        {
            if (_attackHitFired)
                return;
            _attackHitFired = true;
            _onAttackHit?.Invoke();
        }

        void PlayLocomotion()
        {
            if (_state == null || _dead || _skeletonAnimation == null)
                return;
            var data = _skeletonAnimation.Skeleton?.Data;
            if (data == null)
                return;

            var idle = IdleName;
            string primary;
            if (_isMoving)
            {
                var w = WalkName;
                primary = string.IsNullOrEmpty(w) ? "walk" : w;
            }
            else
            {
                primary = idle;
            }

            if (string.IsNullOrEmpty(primary))
                primary = idle;

            var anim = data.FindAnimation(primary);
            var usedFallback = false;
            if (anim == null && _isMoving && primary != idle && !string.IsNullOrEmpty(idle))
            {
                anim = data.FindAnimation(idle);
                usedFallback = anim != null;
            }
            if (anim == null)
            {
                if (LogEnabled)
                    Debug.LogWarning("[SpineBattleAnimator] " + name + " FindAnimation thất bại: primary='" + primary + "' idle='" + idle + "' moving=" + _isMoving + ". Kiểm tra SkeletonData có đủ clip.", this);
                return;
            }

            var current = _state.GetCurrent(0);
            if (current != null && current.Animation != null && current.Animation.Name == anim.Name && current.Loop)
                return;

            _state.SetAnimation(0, anim.Name, true);
            if (LogEnabled)
            {
                var key = anim.Name + "|" + _isMoving + "|" + usedFallback;
                if (key != _lastLocomotionLogKey)
                {
                    _lastLocomotionLogKey = key;
                    var line = "[SpineBattleAnimator] " + name + " → locomotion '" + anim.Name + "' loop (moving=" + _isMoving;
                    if (usedFallback)
                        line += ", fallback idle vì không có walk";
                    line += ").";
                    Debug.Log(line, this);
                }
            }
        }

        void PlayDie()
        {
            if (_dead || _state == null)
                return;
            _dead = true;
            ClearAttackCompleteHandler();
            var die = DieName;
            if (string.IsNullOrEmpty(die))
                return;
            var anim = _skeletonAnimation.Skeleton.Data.FindAnimation(die);
            if (anim != null)
            {
                _state.SetAnimation(0, die, false);
                if (LogEnabled)
                    Debug.Log("[SpineBattleAnimator] " + name + " → died '" + die + "' (object có thể Destroy ngay sau đó).", this);
            }
            else if (LogEnabled)
                Debug.LogWarning("[SpineBattleAnimator] " + name + " không có clip died '" + die + "'.", this);
        }

        void ApplyDefaultFacingForFaction()
        {
            var sign = 1f;
            if (_health != null && _health.Faction == CombatFaction.Enemy)
                sign = -1f;
            ApplyFacing(sign);
        }

        void ApplyFacing(float sign)
        {
            if (invertFacingSign)
                sign = -sign;

            if (_skeletonAnimation != null)
            {
                var t = _skeletonAnimation.transform;
                CacheBaseScaleX();
                if (!_hasBaseScaleX)
                    return;
                var s = t.localScale;
                s.x = _baseAbsScaleX;
                s.y = Mathf.Abs(s.y);
                t.localScale = s;

                var sk = _skeletonAnimation.Skeleton;
                if (sk != null)
                    sk.ScaleX = sign >= 0f ? 1f : -1f;
                return;
            }

            CacheBaseScaleX();
            if (!_hasBaseScaleX)
                return;
            var root = transform;
            var ls = root.localScale;
            ls.x = sign * _baseAbsScaleX;
            root.localScale = ls;
        }

        void CacheBaseScaleX()
        {
            if (_hasBaseScaleX)
                return;
            Transform t = _skeletonAnimation != null ? _skeletonAnimation.transform : transform;
            _baseAbsScaleX = Mathf.Max(0.0001f, Mathf.Abs(t.localScale.x));
            _hasBaseScaleX = true;
        }
    }
}
