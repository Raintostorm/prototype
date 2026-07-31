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
        [SerializeField] float idleBobWorld = 0.026f;
        [SerializeField] float idleCycleSeconds = 1.15f;
        [SerializeField] float attackDurationSeconds = 0.46f;
        [SerializeField] float attackLungeWorld = 0.42f;
        [SerializeField] float attackHitNormalizedTime = 0.42f;
        [SerializeField] float attackMinInterval = 0f;

        CombatHealth _health;
        Transform _visualRoot;
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
        AnimalKitEnemySprites.Profile _profile;
        Sprite _lastAppliedSprite;

        void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = FindAnimalSpriteRenderer();
            _visualRoot = spriteRenderer != null ? spriteRenderer.transform : transform;
            _health = GetComponent<CombatHealth>();
            _baseLocalPosition = _visualRoot.localPosition;
            _baseLocalScale = _visualRoot.localScale;
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
            if (_dead)
                return;

            _nextAttackTime = Time.time + Mathf.Max(0.01f, attackMinInterval);
            _onHit = onHit;
            _onComplete = onComplete;
            ApplySprite(_profile?.Attack);

            if (_attackRoutine != null)
                StopCoroutine(_attackRoutine);
            _attackRoutine = StartCoroutine(AttackRoutine());
        }

        public void SetMoving(bool isMoving)
        {
            if (_dead)
                return;
            _moving = isMoving;
            if (_attackRoutine == null)
                ApplySprite(_moving ? _profile?.Walk : _profile?.Idle);
        }

        public void ForceIdleLoop()
        {
            if (_dead)
                return;
            _moving = false;
            if (_attackRoutine == null)
            {
                ApplySprite(_profile?.Idle);
                ResetPose();
            }
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

            if (_moving)
            {
                // The extracted kit already supplies a distinct run pose. Alternate it
                // by distance travelled, but do not add the old procedural wobble.
                var useRunPose = Mathf.FloorToInt(_moveDistance / 0.07f) % 2 == 0;
                ApplySprite(useRunPose ? _profile?.Walk : _profile?.Idle);
                ResetPose();
                return;
            }

            var phase = Mathf.Sin((Time.time / Mathf.Max(0.01f, idleCycleSeconds)) * Mathf.PI * 2f);
            _visualRoot.localPosition = _baseLocalPosition + new Vector3(0f, phase * idleBobWorld, 0f);
            _visualRoot.localRotation = Quaternion.Euler(0f, 0f, phase * 1.8f);
            _visualRoot.localScale = _baseLocalScale;
        }

        IEnumerator AttackRoutine()
        {
            // Crocodile's defining pose is the open jaw, so keep it readable even
            // at normal combat speed instead of flashing for less than half a second.
            var profileDuration = _profile != null && _profile.Name == "Crocodile"
                ? Mathf.Max(0.68f, attackDurationSeconds)
                : attackDurationSeconds;
            var dur = Mathf.Max(0.05f, profileDuration);
            var hitTime = dur * Mathf.Clamp01(attackHitNormalizedTime);
            var hitFired = false;
            var t = 0f;

            while (t < dur)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / dur);
                var strikeCurve = Mathf.Sin(k * Mathf.PI);
                var anticipation = k < 0.28f ? -0.12f * (k / 0.28f) : 0f;
                var lunge = (anticipation + strikeCurve * attackLungeWorld) * _facingSign;
                var squash = 1f + strikeCurve * 0.16f;
                _visualRoot.localPosition = _baseLocalPosition + new Vector3(lunge, strikeCurve * 0.035f, 0f);
                _visualRoot.localRotation = Quaternion.Euler(0f, 0f, -_facingSign * strikeCurve * 9f);
                _visualRoot.localScale = new Vector3(_baseLocalScale.x * (1f + strikeCurve * 0.08f), _baseLocalScale.y * squash, _baseLocalScale.z);

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
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }
            ClearAttack();
            ApplySprite(_profile?.Die);
            // The extracted "down" sprite already contains the complete death pose.
            // Preserve its authored silhouette instead of rotating or squashing it.
            _visualRoot.localPosition = _baseLocalPosition;
            _visualRoot.localRotation = Quaternion.identity;
            _visualRoot.localScale = _baseLocalScale;
        }

        void ResetPose()
        {
            if (!_dead && !_moving)
                ApplySprite(_profile?.Idle);
            _visualRoot.localPosition = _baseLocalPosition;
            _visualRoot.localRotation = Quaternion.identity;
            _visualRoot.localScale = _baseLocalScale;
        }

        public void ConfigureProfile(AnimalKitEnemySprites.Profile profile)
        {
            _profile = profile;
            ApplySprite(_profile?.Idle);
        }

        void ClearAttack()
        {
            _onHit = null;
            _onComplete = null;
        }

        SpriteRenderer FindAnimalSpriteRenderer()
        {
            var child = transform.Find("AnimalKitSprite");
            if (child != null && child.TryGetComponent<SpriteRenderer>(out var childSr))
                return childSr;
            return GetComponent<SpriteRenderer>();
        }

        void ApplySprite(Sprite sprite)
        {
            if (spriteRenderer == null || sprite == null || _lastAppliedSprite == sprite)
                return;
            spriteRenderer.sprite = sprite;
            _lastAppliedSprite = sprite;
        }
    }
}
