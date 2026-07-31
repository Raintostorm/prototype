using System;
using System.Collections;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>Simple local-only sprite animation driver for animal-kit prototype enemies.</summary>
    [DisallowMultipleComponent]
    public sealed class AnimalKitEnemyBattleAnimator : MonoBehaviour, IBattleVisualDriver
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] float idleBobWorld = 0.018f;
        [SerializeField] float idleCycleSeconds = 1.35f;
        [SerializeField] float walkBobWorld = 0.04f;
        [SerializeField] float walkCycleSeconds = 0.42f;
        [SerializeField] float attackDurationSeconds = 0.22f;
        [SerializeField] float attackLungeWorld = 0.16f;
        [SerializeField] float attackHitNormalizedTime = 0.48f;
        [SerializeField] float attackMinInterval = 0.18f;

        CombatHealth _health;
        Vector3 _baseLocalPosition;
        Vector3 _baseLocalScale;
        bool _moving;
        bool _dead;
        float _nextAttackTime;
        float _moveDistance;
        float _facingSign = -1f;
        Coroutine _attackRoutine;
        Action _onHit;
        Action _onComplete;

        void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<CombatHealth>();
            _baseLocalPosition = transform.localPosition;
            _baseLocalScale = transform.localScale;
        }

        void OnEnable()
        {
            if (_health == null)
                _health = GetComponent<CombatHealth>();
            if (_health != null)
                _health.Died += OnDied;
        }

        void OnDisable()
        {
            if (_health != null)
                _health.Died -= OnDied;
            ClearAttack();
        }

        public void PlayAttack(Action onHit, Action onComplete = null)
        {
            if (_dead || Time.time < _nextAttackTime)
                return;

            _nextAttackTime = Time.time + Mathf.Max(0.01f, attackMinInterval);
            _onHit = onHit;
            _onComplete = onComplete;

            if (_attackRoutine != null)
                StopCoroutine(_attackRoutine);
            _attackRoutine = StartCoroutine(AttackRoutine());
        }

        public void SetMoving(bool isMoving)
        {
            if (_dead)
                return;
            _moving = isMoving;
        }

        public void ForceIdleLoop()
        {
            if (_dead)
                return;
            _moving = false;
            if (_attackRoutine == null)
                ResetPose();
        }

        public void UpdateFacing(float dirX)
        {
            if (_dead || Mathf.Abs(dirX) < 0.01f)
                return;

            _facingSign = dirX < 0f ? -1f : 1f;
            if (spriteRenderer != null)
                spriteRenderer.flipX = _facingSign > 0f;
        }

        public void AddMoveDistance(float worldDistance)
        {
            if (worldDistance > 0f)
                _moveDistance += worldDistance;
        }

        void LateUpdate()
        {
            if (_dead || _attackRoutine != null)
                return;

            var cycle = Mathf.Max(0.01f, _moving ? walkCycleSeconds : idleCycleSeconds);
            var phaseByTime = Mathf.Sin((Time.time / cycle) * Mathf.PI * 2f);
            var phaseByDistance = Mathf.Sin((_moveDistance / 0.18f) * Mathf.PI * 2f);
            var phase = _moving ? phaseByDistance : phaseByTime;
            var bob = _moving ? Mathf.Abs(phase) * walkBobWorld : phase * idleBobWorld;
            transform.localPosition = _baseLocalPosition + new Vector3(0f, bob, 0f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = _baseLocalScale;
        }

        IEnumerator AttackRoutine()
        {
            var dur = Mathf.Max(0.05f, attackDurationSeconds);
            var hitTime = dur * Mathf.Clamp01(attackHitNormalizedTime);
            var hitFired = false;
            var t = 0f;

            while (t < dur)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / dur);
                var lunge = Mathf.Sin(k * Mathf.PI) * attackLungeWorld * _facingSign;
                var squash = 1f + Mathf.Sin(k * Mathf.PI) * 0.06f;
                transform.localPosition = _baseLocalPosition + new Vector3(lunge, 0f, 0f);
                transform.localScale = new Vector3(_baseLocalScale.x * (1f + (squash - 1f) * 0.4f), _baseLocalScale.y * squash, _baseLocalScale.z);

                if (!hitFired && t >= hitTime)
                {
                    hitFired = true;
                    _onHit?.Invoke();
                }

                yield return null;
            }

            if (!hitFired)
                _onHit?.Invoke();
            _onComplete?.Invoke();
            ClearAttack();
            _attackRoutine = null;
            ResetPose();
        }

        void OnDied(CombatHealth dead)
        {
            if (dead != _health || _dead)
                return;

            _dead = true;
            ClearAttack();
            transform.localPosition = _baseLocalPosition + new Vector3(0f, -0.04f, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, _facingSign < 0f ? 12f : -12f);
            transform.localScale = new Vector3(_baseLocalScale.x * 1.05f, _baseLocalScale.y * 0.82f, _baseLocalScale.z);
        }

        void ResetPose()
        {
            transform.localPosition = _baseLocalPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = _baseLocalScale;
        }

        void ClearAttack()
        {
            _onHit = null;
            _onComplete = null;
        }
    }
}
