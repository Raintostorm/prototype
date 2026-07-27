using System;
using System.Collections;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>
    /// Lightweight battle animation driver for line1 legends melee sprite.
    /// Uses Animator states (Idle/Attack/Die) and a short cooldown to avoid trigger spam.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Line1BattleSpriteAnimator : MonoBehaviour, IBattleVisualDriver
    {
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] string idleStateName = "Idle";
        [SerializeField] string walkStateName = "Walk";
        [SerializeField] string attackStateName = "Attack";
        [SerializeField] string dieStateName = "Die";
        [SerializeField] float attackMinInterval = 0.42f;
        [SerializeField] float attackStateDuration = 0.4f;
        [Range(0.05f, 0.95f)]
        [SerializeField] float attackHitNormalizedTime = 0.33f;
        [SerializeField] bool lockFacingOnDie = true;
        [SerializeField] bool useMovementFacing = true;
        [SerializeField] bool invertFacingSign = true;
        [SerializeField] float idleCycleSeconds = 1.4f;
        [SerializeField] float walkCycleSeconds = 0.6f;
        [SerializeField] float walkStrideWorld = 0.2f;
        [SerializeField] float fallbackIdleTiltDegrees = 3f;
        [SerializeField] float fallbackWalkTiltDegrees = 9f;
        [SerializeField] float fallbackWalkPulseScale = 0.08f;
        /// <summary>Idle: only arms (shoulder pivots) bob; head/body stay fixed.</summary>
        [SerializeField] float idleArmBobDegrees = 1.6f;
        [SerializeField] float walkBodyBob = 0.9f;
        [SerializeField] float attackBodyLunge = 1.4f;

        CombatHealth _health;
        bool _dead;
        float _nextAttackTime;
        Coroutine _returnIdleRoutine;
        float _baseAbsScaleX;
        bool _hasBaseScaleX;
        bool _isMoving;
        string _currentStateName;
        float _attackBlend;
        float _walkDistanceAccum;
        DebugPreviewMode _debugPreview = DebugPreviewMode.None;

        Transform _rigRoot;
        Transform _bodyPivot;
        Transform _headPivot;
        Transform _armLRoot;
        Transform _armRRoot;
        Transform _legLPivot;
        Transform _legRPivot;

        Vector3 _bodyBaseLocalPosition;
        float _headBaseZ;
        float _armLBaseZ;
        float _armRBaseZ;
        float _legLBaseZ;
        float _legRBaseZ;
        Vector3 _fallbackBaseScale;
        bool _hasFallbackBaseScale;
        Action _onAttackHit;
        Action _onAttackComplete;
        bool _attackHitFired;
        int _attackToken;

        enum DebugPreviewMode
        {
            None,
            Idle,
            Walk
        }

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<CombatHealth>();
            CacheRigReferences();
        }

        void OnEnable()
        {
            if (_health == null)
                _health = GetComponent<CombatHealth>();
            if (_health != null)
                _health.Died += OnDied;
            _dead = false;
            ApplyDefaultFacingForFaction();
        }

        void OnDisable()
        {
            if (_health != null)
                _health.Died -= OnDied;
            ClearAttackCallbacks();
        }

        public void Configure(Animator runtimeAnimator)
        {
            if (runtimeAnimator != null)
                animator = runtimeAnimator;
            CacheBaseScaleX();
            CacheRigReferences();
            ApplyDefaultFacingForFaction();
            PlayState(idleStateName, true);
        }

        public void UpdateFacing(float dirX)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (_dead && lockFacingOnDie)
                return;
            if (!useMovementFacing)
                return;
            if (Mathf.Abs(dirX) < 0.01f)
                return;
            ApplyFacing(dirX < 0f ? -1f : 1f);
        }

        public void SetMoving(bool isMoving)
        {
            if (_dead)
                return;
            if (_debugPreview != DebugPreviewMode.None)
                return;
            _isMoving = isMoving;
            if (_returnIdleRoutine != null)
                return;
            PlayState(_isMoving ? walkStateName : idleStateName);
        }

        public void ForceIdleLoop()
        {
            if (_dead)
                return;
            if (_debugPreview != DebugPreviewMode.None)
                _debugPreview = DebugPreviewMode.None;
            _isMoving = false;
            if (_returnIdleRoutine != null)
            {
                StopCoroutine(_returnIdleRoutine);
                _returnIdleRoutine = null;
            }
            _attackBlend = 0f;
            PlayState(idleStateName, true);
        }

        public void AddMoveDistance(float worldDistance)
        {
            if (worldDistance <= 0f || _debugPreview != DebugPreviewMode.None)
                return;
            _walkDistanceAccum += worldDistance;
        }

        public void DebugPreviewIdle()
        {
            _debugPreview = DebugPreviewMode.Idle;
            _dead = false;
            _attackBlend = 0f;
            _isMoving = false;
            if (_returnIdleRoutine != null)
            {
                StopCoroutine(_returnIdleRoutine);
                _returnIdleRoutine = null;
            }
            PlayState(idleStateName, true);
        }

        public void DebugPreviewWalk()
        {
            _debugPreview = DebugPreviewMode.Walk;
            _dead = false;
            _attackBlend = 0f;
            _isMoving = true;
            PlayState(walkStateName, true);
        }

        public void DebugPreviewAttack()
        {
            _debugPreview = DebugPreviewMode.None;
            _dead = false;
            PlayAttack(null);
        }

        public void DebugPreviewDie()
        {
            _debugPreview = DebugPreviewMode.None;
            PlayDie();
        }

        public void DebugClearPreview()
        {
            _debugPreview = DebugPreviewMode.None;
        }

        public string DebugBindingSummary()
        {
            CacheRigReferences();
            var missing = string.Empty;
            if (_bodyPivot == null) missing += " body_pivot";
            if (_headPivot == null) missing += " head_pivot";
            if (_armLRoot == null) missing += " arm_l_root";
            if (_armRRoot == null) missing += " arm_r_root";
            if (_legLPivot == null) missing += " leg_l_pivot";
            if (_legRPivot == null) missing += " leg_r_pivot";
            return string.IsNullOrWhiteSpace(missing)
                ? "Binding OK"
                : "Missing:" + missing;
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
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = sign < 0f;
                return;
            }

            CacheBaseScaleX();
            if (!_hasBaseScaleX)
                return;
            var t = transform;
            var s = t.localScale;
            s.x = sign * _baseAbsScaleX;
            t.localScale = s;
        }

        public void PlayAttack(Action onHit, Action onComplete = null)
        {
            if (!this || !isActiveAndEnabled)
                return;
            if (_dead || animator == null || string.IsNullOrWhiteSpace(attackStateName))
                return;
            if (Time.time < _nextAttackTime)
                return;

            _nextAttackTime = Time.time + Mathf.Max(0.01f, attackMinInterval);
            _attackBlend = 1f;
            _onAttackHit = onHit;
            _onAttackComplete = onComplete;
            _attackHitFired = false;
            _attackToken++;
            PlayState(attackStateName, true);

            if (_returnIdleRoutine != null)
                StopCoroutine(_returnIdleRoutine);
            _returnIdleRoutine = StartCoroutine(ReturnToIdleAfterAttack(_attackToken));
        }

        public void PlayDie()
        {
            if (_dead)
                return;
            _dead = true;
            ClearAttackCallbacks();
            _attackBlend = 0f;
            if (_returnIdleRoutine != null)
            {
                StopCoroutine(_returnIdleRoutine);
                _returnIdleRoutine = null;
            }
            PlayState(dieStateName, true);
        }

        IEnumerator ReturnToIdleAfterAttack(int token)
        {
            var duration = Mathf.Max(0.01f, attackStateDuration);
            var hitDelay = duration * Mathf.Clamp01(attackHitNormalizedTime);
            if (hitDelay > 0f)
                yield return new WaitForSeconds(hitDelay);
            if (token != _attackToken || !this || !isActiveAndEnabled || _dead)
                yield break;
            FireAttackHitOnce();

            var remain = Mathf.Max(0f, duration - hitDelay);
            if (remain > 0f)
                yield return new WaitForSeconds(remain);
            _returnIdleRoutine = null;
            if (token != _attackToken || !this || !isActiveAndEnabled || _dead)
                yield break;
            _onAttackComplete?.Invoke();
            ClearAttackCallbacks();
            PlayState(_isMoving ? walkStateName : idleStateName, true);
        }

        void FireAttackHitOnce()
        {
            if (_attackHitFired)
                return;
            _attackHitFired = true;
            _onAttackHit?.Invoke();
        }

        void ClearAttackCallbacks()
        {
            _onAttackHit = null;
            _onAttackComplete = null;
            _attackHitFired = false;
        }

        void OnDied(CombatHealth dead)
        {
            if (dead == null || dead != _health)
                return;
            PlayDie();
        }

        void CacheBaseScaleX()
        {
            if (_hasBaseScaleX)
                return;
            _baseAbsScaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.localScale.x));
            _hasBaseScaleX = true;
        }

        void LateUpdate()
        {
            CacheRigReferences();
            if (_bodyPivot == null)
            {
                ApplyFallbackSpritePose();
                return;
            }

            if (_dead)
            {
                ApplyDiePose();
                return;
            }

            if (_attackBlend > 0f)
            {
                ApplyAttackPose();
                _attackBlend = Mathf.Max(0f, _attackBlend - (Time.deltaTime / Mathf.Max(0.01f, attackStateDuration)));
                return;
            }

            var previewMoving = _debugPreview == DebugPreviewMode.Walk;
            var previewIdle = _debugPreview == DebugPreviewMode.Idle;
            if (previewMoving || (_isMoving && !previewIdle))
                ApplyWalkPose();
            else
                ApplyIdlePose();
        }

        void PlayState(string stateName, bool forceRestart = false)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
                return;
            if (!forceRestart && _currentStateName == stateName)
                return;
            animator.Play(stateName, 0, 0f);
            _currentStateName = stateName;
        }

        void CacheRigReferences()
        {
            if (_bodyPivot != null)
                return;

            _rigRoot = animator != null ? animator.transform : transform;
            _bodyPivot = FindRigChild("body_pivot");
            _headPivot = FindRigChild("head_pivot");
            _armLRoot = FindRigChild("arm_l_root");
            _armRRoot = FindRigChild("arm_r_root");
            _legLPivot = FindRigChild("leg_l_pivot");
            _legRPivot = FindRigChild("leg_r_pivot");

            if (_bodyPivot != null)
                _bodyBaseLocalPosition = _bodyPivot.localPosition;
            if (_headPivot != null)
                _headBaseZ = NormalizeAngle(_headPivot.localEulerAngles.z);
            if (_armLRoot != null)
                _armLBaseZ = NormalizeAngle(_armLRoot.localEulerAngles.z);
            if (_armRRoot != null)
                _armRBaseZ = NormalizeAngle(_armRRoot.localEulerAngles.z);
            if (_legLPivot != null)
                _legLBaseZ = NormalizeAngle(_legLPivot.localEulerAngles.z);
            if (_legRPivot != null)
                _legRBaseZ = NormalizeAngle(_legRPivot.localEulerAngles.z);
        }

        Transform FindRigChild(string childName)
        {
            if (_rigRoot == null)
                return null;
            var all = _rigRoot.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].name == childName)
                    return all[i];
            }
            return null;
        }

        void ApplyIdlePose()
        {
            if (_bodyPivot != null)
                _bodyPivot.localPosition = _bodyBaseLocalPosition;

            SetLocalZ(_headPivot, _headBaseZ);
            SetLocalZ(_legLPivot, _legLBaseZ);
            SetLocalZ(_legRPivot, _legRBaseZ);

            var phase = Mathf.Sin((Time.time / Mathf.Max(0.01f, idleCycleSeconds)) * Mathf.PI * 2f);
            var amp = Mathf.Max(0f, idleArmBobDegrees);
            SetLocalZ(_armLRoot, _armLBaseZ - phase * amp);
            SetLocalZ(_armRRoot, _armRBaseZ + phase * amp);
        }

        void ApplyWalkPose()
        {
            var stride = Mathf.Max(0.01f, walkStrideWorld);
            var phaseByDistance = Mathf.Sin((_walkDistanceAccum / stride) * Mathf.PI * 2f);
            var phaseByTime = Mathf.Sin((Time.time / Mathf.Max(0.01f, walkCycleSeconds)) * Mathf.PI * 2f);
            var phase = Mathf.Abs(phaseByDistance) > 0.01f ? phaseByDistance : phaseByTime;
            SetBodyY(_bodyBaseLocalPosition.y + Mathf.Abs(phase) * walkBodyBob);
            SetLocalZ(_headPivot, _headBaseZ + phase * 4f);
            SetLocalZ(_armLRoot, _armLBaseZ - phase * 10f);
            SetLocalZ(_armRRoot, _armRBaseZ + phase * 10f);
            SetLocalZ(_legLPivot, _legLBaseZ + phase * 8f);
            SetLocalZ(_legRPivot, _legRBaseZ - phase * 8f);
        }

        void ApplyAttackPose()
        {
            var progress = 1f - _attackBlend;
            if (progress < 0.35f)
            {
                var windup = progress / 0.35f;
                SetBodyXZ(
                    _bodyBaseLocalPosition.x - windup * (attackBodyLunge * 0.8f),
                    _bodyBaseLocalPosition.y + windup * 0.3f);
                SetLocalZ(_headPivot, _headBaseZ + windup * 8f);
                SetLocalZ(_armLRoot, _armLBaseZ - windup * 14f);
                SetLocalZ(_armRRoot, _armRBaseZ + windup * 20f);
                SetLocalZ(_legLPivot, _legLBaseZ - windup * 5f);
                SetLocalZ(_legRPivot, _legRBaseZ + windup * 5f);
                return;
            }

            var strike = (progress - 0.35f) / 0.65f;
            SetBodyXZ(
                _bodyBaseLocalPosition.x - (attackBodyLunge * 0.8f) + strike * (attackBodyLunge * 1.6f),
                _bodyBaseLocalPosition.y + 0.3f - strike * 0.2f);
            SetLocalZ(_headPivot, _headBaseZ + 8f - strike * 16f);
            SetLocalZ(_armLRoot, _armLBaseZ - 14f + strike * 26f);
            SetLocalZ(_armRRoot, _armRBaseZ + 20f - strike * 44f);
            SetLocalZ(_legLPivot, _legLBaseZ - 5f + strike * 8f);
            SetLocalZ(_legRPivot, _legRBaseZ + 5f - strike * 8f);
        }

        void ApplyDiePose()
        {
            SetBodyXZ(_bodyBaseLocalPosition.x, _bodyBaseLocalPosition.y - 1.1f);
            SetLocalZ(_bodyPivot, 14f);
            SetLocalZ(_headPivot, _headBaseZ - 22f);
            SetLocalZ(_armLRoot, _armLBaseZ + 24f);
            SetLocalZ(_armRRoot, _armRBaseZ - 30f);
            SetLocalZ(_legLPivot, _legLBaseZ + 9f);
            SetLocalZ(_legRPivot, _legRBaseZ - 9f);
        }

        void ApplyFallbackSpritePose()
        {
            if (!_hasFallbackBaseScale)
            {
                _fallbackBaseScale = transform.localScale;
                _hasFallbackBaseScale = true;
            }

            if (_dead)
            {
                transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
                transform.localScale = _fallbackBaseScale;
                return;
            }

            var previewMoving = _debugPreview == DebugPreviewMode.Walk;
            var previewIdle = _debugPreview == DebugPreviewMode.Idle;
            var moving = previewMoving || (_isMoving && !previewIdle);
            var phase = Mathf.Sin((Time.time / Mathf.Max(0.01f, moving ? walkCycleSeconds : idleCycleSeconds)) * Mathf.PI * 2f);
            var tilt = moving ? fallbackWalkTiltDegrees : fallbackIdleTiltDegrees;
            transform.localRotation = Quaternion.Euler(0f, 0f, phase * tilt);

            if (moving)
            {
                var pulse = 1f + Mathf.Abs(phase) * fallbackWalkPulseScale;
                var baseScale = _fallbackBaseScale;
                transform.localScale = new Vector3(baseScale.x, baseScale.y * pulse, baseScale.z);
                return;
            }

            transform.localScale = _fallbackBaseScale;
        }

        void SetBodyY(float y)
        {
            if (_bodyPivot == null)
                return;
            var p = _bodyPivot.localPosition;
            p.x = _bodyBaseLocalPosition.x;
            p.y = y;
            _bodyPivot.localPosition = p;
        }

        void SetBodyXZ(float x, float y)
        {
            if (_bodyPivot == null)
                return;
            var p = _bodyPivot.localPosition;
            p.x = x;
            p.y = y;
            _bodyPivot.localPosition = p;
        }

        void SetLocalZ(Transform target, float z)
        {
            if (target == null)
                return;
            var e = target.localEulerAngles;
            e.z = z;
            target.localEulerAngles = e;
        }

        static float NormalizeAngle(float angle)
        {
            while (angle > 180f)
                angle -= 360f;
            while (angle < -180f)
                angle += 360f;
            return angle;
        }


    }
}
