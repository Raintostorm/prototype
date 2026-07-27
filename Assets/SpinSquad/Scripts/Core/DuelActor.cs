using System;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>
    /// Ba nhánh combat (sau khi combat đã bắt đầu):
    /// <list type="bullet">
    /// <item><b>Melee vs melee</b> — va chạm, đổi sát thương theo chu kỳ; không bật lùi (clinch dính nhau).</item>
    /// <item><b>Mixed clinch</b> — không bounce: melee gõ <b>100% attackDamage</b> theo chu kỳ; melee phía enemy lên ally ranged cũng 100% theo chu kỳ (ally driver). Ranged vẫn bắn theo <c>_rangedInterval</c>.</item>
    /// <item><b>Ranged (xa / trong clinch)</b> — mỗi lượt chỉ lock target nhận <c>attackDamage</c> của shooter.</item>
    /// </list>
    /// </summary>
    public sealed class DuelActor : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 1.35f;
        [SerializeField] float contactSkinWidth = 0.02f;
        [SerializeField] float minContactRadius = 0.06f;
        [SerializeField] float touchStrikeInterval = 0.24f;
        [SerializeField] float locomotionMoveThreshold = 0.001f;
        [SerializeField] float locomotionHoldSeconds = 0.12f;

        [SerializeField] float attackDamage = 10f;

        [Tooltip("Chu kỳ nhát melee đầy đủ lên đối phương trong clinch mixed (không bounce).")]
        [SerializeField] float mixedClinchMeleeStrikeInterval = 0.45f;

        CombatHealth _health;
        SpriteRenderer _sprite;
        DuelDirector _director;
        IBattleVisualDriver _battleVisual;

        CombatHealth _lockTarget;

        bool _ranged;
        float _attackRange = 1.65f;
        float _rangedInterval = 0.52f;
        float _rangedCooldown;

        string _rangedBoltTextureResourcesPath;

        float _touchStrikeCooldown;

        float _mixedClinchAllyMeleeStrikeCd;
        float _mixedClinchEnemyMeleeOnAllyCd;
        bool _idleLoopApplied;
        bool _hasSentMovingState;
        bool _lastSentMovingState;
        float _movingLatchUntil;

        public CombatHealth Health => _health;
        public float AttackDamage => attackDamage;
        public bool IsRangedCombat => _ranged;

        void Awake()
        {
            _health = GetComponent<CombatHealth>();
            _sprite = GetComponent<SpriteRenderer>();
            _battleVisual = ResolveBattleVisualDriver();
        }

        public void Init(DuelDirector director)
        {
            _director = director;
            _lockTarget = null;
            _touchStrikeCooldown = 0f;
            _rangedCooldown = UnityEngine.Random.Range(0.04f, 0.2f);
            ResetMixedClinchStrikeCooldowns(desync: true);
        }

        public void ConfigureCombat(
            float damage,
            bool ranged = false,
            float attackRange = 1.65f,
            float rangedShotIntervalSeconds = 0.52f,
            string rangedBoltTextureResourcesPath = null)
        {
            attackDamage = Mathf.Max(0.5f, damage);
            _ranged = ranged;
            _attackRange = Mathf.Max(0.4f, attackRange);
            _rangedInterval = Mathf.Max(0.12f, rangedShotIntervalSeconds);
            _rangedBoltTextureResourcesPath = rangedBoltTextureResourcesPath;
            _rangedCooldown = UnityEngine.Random.Range(0.04f, 0.2f);
        }

        /// <summary>Reset chu kỳ mixed clinch về gốc trước khi áp buff AtkSpeed (tránh nhân chồng khi ConfigureAlly gọi lại).</summary>
        public void ResetMixedClinchStrikeBase(float baseSeconds)
        {
            mixedClinchMeleeStrikeInterval = Mathf.Max(0.12f, baseSeconds);
        }

        /// <summary>Giảm interval melee mixed khi có buff AtkSpeed (ally).</summary>
        public void ScaleMixedClinchInterval(float factor)
        {
            mixedClinchMeleeStrikeInterval = Mathf.Max(0.12f, mixedClinchMeleeStrikeInterval * factor);
        }

        float AllyOutgoingTo(CombatHealth enemyTarget)
        {
            if (_health.Faction != CombatFaction.Ally || enemyTarget == null || _director == null)
                return attackDamage;
            return _director.GetOutgoingDamageForAlly(this, attackDamage);
        }

        bool TryPlayLine1AttackAnimation(Action onHit, Action onComplete = null)
        {
            if (_health == null)
                return false;
            if (!TryGetBattleVisualDriver(out var visual))
                return false;
            // Entering attack range: switch immediately from walk to attack visual.
            UpdateLine1Moving(false);
            visual.PlayAttack(onHit, onComplete);
            return true;
        }

        public void QueueAttackHit(Action onHit, Action onComplete = null)
        {
            TryPlayLine1AttackAnimation(onHit, onComplete);
        }

        void UpdateLine1Facing(float dirX)
        {
            if (_health == null)
                return;
            if (!TryGetBattleVisualDriver(out var visual))
                return;
            visual.UpdateFacing(dirX);
        }

        void UpdateLine1Moving(bool isMoving)
        {
            if (_health == null)
                return;
            if (_hasSentMovingState && _lastSentMovingState == isMoving)
                return;
            if (!TryGetBattleVisualDriver(out var visual))
                return;
            visual.SetMoving(isMoving);
            _lastSentMovingState = isMoving;
            _hasSentMovingState = true;
        }

        void ForceLine1IdleLoop()
        {
            if (_health == null)
                return;
            if (!TryGetBattleVisualDriver(out var visual))
                return;
            visual.ForceIdleLoop();
        }

        void AddLine1MoveDistance(float worldDistance)
        {
            if (_health == null)
                return;
            if (!TryGetBattleVisualDriver(out var visual))
                return;
            visual.AddMoveDistance(worldDistance);
        }

        IBattleVisualDriver ResolveBattleVisualDriver()
        {
            // Prefer root driver when present; fallback to child visual root/prefab setups.
            var rootDriver = GetComponent<IBattleVisualDriver>();
            if (rootDriver != null)
                return rootDriver;
            return GetComponentInChildren<IBattleVisualDriver>(true);
        }

        bool TryGetBattleVisualDriver(out IBattleVisualDriver visual)
        {
            if (!IsBattleVisualDriverAlive(_battleVisual))
                _battleVisual = ResolveBattleVisualDriver();

            if (!IsBattleVisualDriverAlive(_battleVisual))
            {
                visual = null;
                return false;
            }

            visual = _battleVisual;
            return true;
        }

        static bool IsBattleVisualDriverAlive(IBattleVisualDriver driver)
        {
            if (driver == null)
                return false;
            if (driver is UnityEngine.Object unityObject)
                return unityObject;
            return true;
        }

        bool IsOutgamePhase()
        {
            if (_director == null || Health.IsDead)
                return false;
            if (!_director.CombatStarted || _director.BattleEnded)
                return true;
            return !_director.CombatEngaged;
        }

        public float ContactRadius()
        {
            if (_sprite != null && _sprite.sprite != null)
            {
                var e = _sprite.bounds.extents;
                return Mathf.Min(0.4f, Mathf.Max(e.x, e.y));
            }

            var renderers = GetComponentsInChildren<SpriteRenderer>();
            var hasBound = false;
            var b = new Bounds(transform.position, Vector3.zero);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i].sprite == null)
                    continue;
                if (!hasBound)
                {
                    b = renderers[i].bounds;
                    hasBound = true;
                }
                else
                    b.Encapsulate(renderers[i].bounds);
            }

            if (!hasBound)
            {
                var meshes = GetComponentsInChildren<MeshRenderer>();
                if (meshes != null && meshes.Length > 0)
                {
                    hasBound = false;
                    for (var i = 0; i < meshes.Length; i++)
                    {
                        if (meshes[i] == null)
                            continue;
                        if (!hasBound)
                        {
                            b = meshes[i].bounds;
                            hasBound = true;
                        }
                        else
                            b.Encapsulate(meshes[i].bounds);
                    }
                    if (hasBound)
                    {
                        var me = b.extents;
                        return Mathf.Min(0.4f, Mathf.Max(me.x, me.y));
                    }
                }
                return Mathf.Max(0.02f, minContactRadius);
            }
            var ext = b.extents;
            return Mathf.Min(0.4f, Mathf.Max(ext.x, ext.y));
        }

        public float TouchDistanceTo(DuelActor other)
        {
            if (other == null)
                return minContactRadius;
            return Mathf.Max(
                minContactRadius,
                ContactRadius() + other.ContactRadius() + contactSkinWidth);
        }

        void Update()
        {
            if (_director == null || Health.IsDead)
                return;

            if (IsOutgamePhase())
            {
                UpdateLine1Moving(false);
                // Apply idle loop once on phase entry to avoid restarting the same clip every frame.
                if (!_idleLoopApplied)
                {
                    ForceLine1IdleLoop();
                    _idleLoopApplied = true;
                }
                // Outgame: idle + quay mặt về phía phe đối diện (ally → phải, enemy → trái).
                if (_health != null)
                {
                    if (_health.Faction == CombatFaction.Ally)
                        UpdateLine1Facing(1f);
                    else
                        UpdateLine1Facing(-1f);
                }

                return;
            }
            if (_lockTarget == null || _lockTarget.IsDead)
            {
                _lockTarget = _health.Faction == CombatFaction.Ally
                    ? _director.FindNearestLivingEnemy(transform.position)
                    : _director.FindNearestLivingAlly(transform.position);
                _touchStrikeCooldown = 0f;
                ResetMixedClinchStrikeCooldowns(desync: true);
            }

            if (_lockTarget == null || _lockTarget.IsDead)
            {
                UpdateLine1Moving(false);
                // Keep idle loop visible in-combat when no valid target exists.
                if (!_idleLoopApplied)
                {
                    ForceLine1IdleLoop();
                    _idleLoopApplied = true;
                }
                return;
            }

            var otherActor = _lockTarget.GetComponent<DuelActor>();
            if (otherActor == null)
            {
                UpdateLine1Moving(false);
                if (!_idleLoopApplied)
                {
                    ForceLine1IdleLoop();
                    _idleLoopApplied = true;
                }
                return;
            }

            var selfT = transform;
            var self = (Vector2)selfT.position;
            var targetPos = (Vector2)_lockTarget.transform.position;
            var delta = targetPos - self;
            var dist = delta.magnitude;
            var dir = dist > 0.0001f ? delta / dist : Vector2.right;
            UpdateLine1Facing(dir.x);
            var step = moveSpeed * Time.deltaTime;
            const float holdSlack = 0.03f;
            var beforeMove = (Vector2)selfT.position;

            var touch = TouchDistanceTo(otherActor);
            var mixedPair = _ranged != otherActor.IsRangedCombat;
            var mixedClinch = mixedPair && dist <= touch;
            var pureMeleePair = !_ranged && !otherActor.IsRangedCombat;

            var intentMoving = false;
            if (_ranged && !mixedClinch)
            {
                if (dist > _attackRange + holdSlack)
                {
                    intentMoving = true;
                    var maxStep = Mathf.Min(step, dist - (_attackRange + holdSlack));
                    if (maxStep > 0f)
                    {
                        self += dir * maxStep;
                        selfT.position = new Vector3(self.x, self.y, selfT.position.z);
                        ClampToArena(selfT);
                    }
                }
            }
            else if (!_ranged)
            {
                // Avoid dead-zone: if touch < dist <= touch+holdSlack, old logic neither moved nor attacked.
                // Melee should keep closing gap until true contact.
                if (dist > touch)
                {
                    intentMoving = true;
                    self += dir * step;
                    selfT.position = new Vector3(self.x, self.y, selfT.position.z);
                    ClampToArena(selfT);
                }
            }

            var moved = Vector2.Distance(beforeMove, (Vector2)selfT.position);
            if (moved > Mathf.Max(0.00005f, locomotionMoveThreshold))
                _movingLatchUntil = Time.time + Mathf.Max(0f, locomotionHoldSeconds);
            var isMovingThisFrame = intentMoving || Time.time < _movingLatchUntil;
            UpdateLine1Moving(isMovingThisFrame);
            if (isMovingThisFrame)
            {
                _idleLoopApplied = false;
                AddLine1MoveDistance(moved);
            }

            if (_touchStrikeCooldown > 0f)
                _touchStrikeCooldown = Mathf.Max(0f, _touchStrikeCooldown - Time.deltaTime);

            delta = (Vector2)_lockTarget.transform.position - (Vector2)selfT.position;
            dist = delta.magnitude;
            dir = dist > 0.0001f ? delta / dist : Vector2.right;
            touch = TouchDistanceTo(otherActor);
            mixedPair = _ranged != otherActor.IsRangedCombat;
            mixedClinch = mixedPair && dist <= touch;
            pureMeleePair = !_ranged && !otherActor.IsRangedCombat;

            if (_health.Faction == CombatFaction.Ally)
            {
                if (dist > touch + 0.04f)
                {
                    _touchStrikeCooldown = 0f;
                    ResetMixedClinchStrikeCooldowns(desync: false);
                }

                if (dist <= touch && pureMeleePair)
                {
                    TryMeleeVsMeleeAsAlly(otherActor, dist, dir);
                    return;
                }

                if (dist <= touch && mixedClinch)
                {
                    // Revert mixed contact to classic collision behavior:
                    // apply touch exchange once, then bounce both units apart.
                    TryMeleeVsMeleeAsAlly(otherActor, dist, dir);
                    return;
                }
            }

            if (_ranged && dist <= _attackRange + holdSlack && !_lockTarget.IsDead)
            {
                _rangedCooldown -= Time.deltaTime;
                if (_rangedCooldown <= 0f)
                {
                    _rangedCooldown = _rangedInterval;
                    TryRangedOneSidedShot();
                }
            }
        }

        void ResetMixedClinchStrikeCooldowns(bool desync)
        {
            if (desync)
            {
                _mixedClinchAllyMeleeStrikeCd = UnityEngine.Random.Range(0.02f, 0.12f);
                _mixedClinchEnemyMeleeOnAllyCd = UnityEngine.Random.Range(0.06f, 0.18f);
            }
            else
            {
                _mixedClinchAllyMeleeStrikeCd = 0f;
                _mixedClinchEnemyMeleeOnAllyCd = 0f;
            }
        }

        void TryMixedClinchFullDamageStrikes(DuelActor otherActor)
        {
            if (_director.BattleEnded || _lockTarget == null || _lockTarget.IsDead)
                return;

            var dt = Time.deltaTime;
            var period = Mathf.Max(0.12f, mixedClinchMeleeStrikeInterval);
            var allyPos = _health.transform.position;
            var enemyPos = _lockTarget.transform.position;

            if (!_ranged && otherActor.IsRangedCombat)
            {
                _mixedClinchAllyMeleeStrikeCd -= dt;
                if (_mixedClinchAllyMeleeStrikeCd > 0f)
                    return;

                _mixedClinchAllyMeleeStrikeCd = period;

                var enemyHealth = _lockTarget.Faction == CombatFaction.Enemy ? _lockTarget : _health;
                var dmg = AllyOutgoingTo(enemyHealth);
                QueueAttackHit(() =>
                {
                    if (_director == null || _director.BattleEnded || enemyHealth == null || enemyHealth.IsDead)
                        return;
                    enemyHealth.TakeDamage(dmg);
                    if (enemyHealth.IsDead || _director.BattleEnded)
                    {
                        if (enemyHealth.IsDead)
                            _director.ReportKillingHit(enemyHealth, dmg);
                        return;
                    }

                    _director.ReportHitExchange(0f, dmg, allyPos, enemyPos);
                });
                return;
            }

            if (_ranged && !otherActor.IsRangedCombat)
            {
                _mixedClinchEnemyMeleeOnAllyCd -= dt;
                if (_mixedClinchEnemyMeleeOnAllyCd > 0f)
                    return;

                _mixedClinchEnemyMeleeOnAllyCd = period;
                otherActor.QueueAttackHit(() =>
                {
                    if (_director == null || _director.BattleEnded || _health == null || Health.IsDead)
                        return;
                    _health.TakeDamage(otherActor.AttackDamage);
                    if (Health.IsDead || _director.BattleEnded)
                    {
                        if (Health.IsDead)
                            _director.ReportKillingHit(_health, otherActor.AttackDamage);
                        return;
                    }

                    _director.ReportHitExchange(otherActor.AttackDamage, 0f, allyPos, enemyPos);
                });
            }
        }

        void TryMeleeVsMeleeAsAlly(DuelActor otherActor, float dist, Vector2 dir)
        {
            if (_touchStrikeCooldown > 0f)
                return;

            _touchStrikeCooldown = Mathf.Max(0.02f, touchStrikeInterval);

            var enemyHealth = _lockTarget;
            var allyDmg = AllyOutgoingTo(enemyHealth);
            QueueAttackHit(() =>
            {
                if (_director == null || _director.BattleEnded || enemyHealth == null || enemyHealth.IsDead)
                    return;
                enemyHealth.TakeDamage(allyDmg);
                if (enemyHealth.IsDead || _director.BattleEnded)
                {
                    if (enemyHealth.IsDead)
                        _director.ReportKillingHit(enemyHealth, allyDmg);
                    return;
                }

                _director.ReportHitExchange(
                    0f,
                    allyDmg,
                    _health.transform.position,
                    enemyHealth.transform.position);
            });

            otherActor.QueueAttackHit(() =>
            {
                if (_director == null || _director.BattleEnded || _health == null || Health.IsDead)
                    return;
                _health.TakeDamage(otherActor.AttackDamage);
                if (Health.IsDead || _director.BattleEnded)
                {
                    if (Health.IsDead)
                        _director.ReportKillingHit(_health, otherActor.AttackDamage);
                    return;
                }

                _director.ReportHitExchange(
                    otherActor.AttackDamage,
                    0f,
                    _health.transform.position,
                    enemyHealth != null ? enemyHealth.transform.position : _health.transform.position);
            });
        }

        void TryRangedOneSidedShot()
        {
            var targetHealth = _lockTarget;
            if (targetHealth == null || targetHealth.IsDead)
                return;

            var outgoing = _health.Faction == CombatFaction.Ally ? AllyOutgoingTo(targetHealth) : attackDamage;
            var fromW = _health.transform.position;
            var toW = targetHealth.transform.position;
            QueueAttackHit(() =>
            {
                if (_director == null || _director.BattleEnded || targetHealth == null || targetHealth.IsDead)
                    return;
                targetHealth.TakeDamage(outgoing);
                if (targetHealth.IsDead || _director.BattleEnded)
                {
                    if (targetHealth.IsDead)
                        _director.ReportKillingHit(targetHealth, outgoing);
                    return;
                }

                var allyPos = _health.Faction == CombatFaction.Ally ? _health.transform.position : targetHealth.transform.position;
                var enemyPos = _health.Faction == CombatFaction.Ally ? targetHealth.transform.position : _health.transform.position;

                if (_health.Faction == CombatFaction.Ally)
                    _director.ReportHitExchange(0f, outgoing, allyPos, enemyPos);
                else
                    _director.ReportHitExchange(outgoing, 0f, allyPos, enemyPos);
            });
            var boltSprite = RangedShotVfx.GetOrCreateBoltSprite(_rangedBoltTextureResourcesPath);
            RangedShotVfx.Spawn(fromW, toW, _health.Faction, boltSprite);
        }

        static void ClampToArena(Transform t)
        {
            var p = t.position;
            p.x = Mathf.Clamp(p.x, -5.4f, 5.4f);
            p.y = Mathf.Clamp(p.y, -1.35f, 1.35f);
            t.position = p;
        }
    }
}
