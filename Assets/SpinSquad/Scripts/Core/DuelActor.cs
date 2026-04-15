using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>
    /// Ba nhánh combat (sau khi combat đã bắt đầu):
    /// <list type="bullet">
    /// <item><b>Melee vs melee</b> — va chạm, bật, đổi sát thương đầy đủ (chỉ ally driver).</item>
    /// <item><b>Mixed clinch</b> — không bounce: melee gõ <b>100% attackDamage</b> theo chu kỳ; melee phía enemy lên ally ranged cũng 100% theo chu kỳ (ally driver). Ranged vẫn bắn theo <c>_rangedInterval</c>.</item>
    /// <item><b>Ranged (xa / trong clinch)</b> — mỗi lượt chỉ lock target nhận <c>attackDamage</c> của shooter.</item>
    /// </list>
    /// </summary>
    public sealed class DuelActor : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 1.15f;
        [SerializeField] float contactSkinWidth = 0.02f;
        [SerializeField] float minContactRadius = 0.06f;
        [SerializeField] float knockbackDistance = 0.48f;
        [SerializeField] float touchReleasePadding = 0.06f;

        [SerializeField] float attackDamage = 10f;

        [Tooltip("Chu kỳ nhát melee đầy đủ lên đối phương trong clinch mixed (không bounce).")]
        [SerializeField] float mixedClinchMeleeStrikeInterval = 0.5f;

        CombatHealth _health;
        SpriteRenderer _sprite;
        DuelDirector _director;

        CombatHealth _lockTarget;

        bool _ranged;
        float _attackRange = 1.65f;
        float _rangedInterval = 0.52f;
        float _rangedCooldown;

        bool _canDealTouchDamage = true;

        float _mixedClinchAllyMeleeStrikeCd;
        float _mixedClinchEnemyMeleeOnAllyCd;

        public CombatHealth Health => _health;
        public float AttackDamage => attackDamage;
        public bool IsRangedCombat => _ranged;

        void Awake()
        {
            _health = GetComponent<CombatHealth>();
            _sprite = GetComponent<SpriteRenderer>();
        }

        public void Init(DuelDirector director)
        {
            _director = director;
            _lockTarget = null;
            _canDealTouchDamage = true;
            _rangedCooldown = Random.Range(0.04f, 0.2f);
            ResetMixedClinchStrikeCooldowns(desync: true);
        }

        public void ConfigureCombat(
            float damage,
            bool ranged = false,
            float attackRange = 1.65f,
            float rangedShotIntervalSeconds = 0.52f)
        {
            attackDamage = Mathf.Max(0.5f, damage);
            _ranged = ranged;
            _attackRange = Mathf.Max(0.4f, attackRange);
            _rangedInterval = Mathf.Max(0.12f, rangedShotIntervalSeconds);
            _rangedCooldown = Random.Range(0.04f, 0.2f);
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

        public float ContactRadius()
        {
            if (_sprite == null || _sprite.sprite == null)
                return Mathf.Max(0.02f, minContactRadius);
            var e = _sprite.bounds.extents;
            return Mathf.Min(0.4f, Mathf.Max(e.x, e.y));
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
            if (_director == null || _director.BattleEnded || Health.IsDead)
                return;

            if (!_director.CombatStarted)
                return;

            if (_lockTarget == null || _lockTarget.IsDead)
            {
                _lockTarget = _health.Faction == CombatFaction.Ally
                    ? _director.FindNearestLivingEnemy(transform.position)
                    : _director.FindNearestLivingAlly(transform.position);
                _canDealTouchDamage = true;
                ResetMixedClinchStrikeCooldowns(desync: true);
            }

            if (_lockTarget == null || _lockTarget.IsDead)
                return;

            var otherActor = _lockTarget.GetComponent<DuelActor>();
            if (otherActor == null)
                return;

            var selfT = transform;
            var self = (Vector2)selfT.position;
            var targetPos = (Vector2)_lockTarget.transform.position;
            var delta = targetPos - self;
            var dist = delta.magnitude;
            var dir = dist > 0.0001f ? delta / dist : Vector2.right;
            var step = moveSpeed * Time.deltaTime;
            const float holdSlack = 0.03f;

            var touch = TouchDistanceTo(otherActor);
            var mixedPair = _ranged != otherActor.IsRangedCombat;
            var mixedClinch = mixedPair && dist <= touch;
            var pureMeleePair = !_ranged && !otherActor.IsRangedCombat;

            if (_ranged && !mixedClinch)
            {
                if (dist > _attackRange + holdSlack)
                {
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
                self += dir * step;
                selfT.position = new Vector3(self.x, self.y, selfT.position.z);
                ClampToArena(selfT);
            }

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
                    _canDealTouchDamage = true;
                    ResetMixedClinchStrikeCooldowns(desync: false);
                }

                if (dist <= touch && pureMeleePair)
                {
                    TryMeleeVsMeleeAsAlly(otherActor, dist, dir);
                    return;
                }

                if (dist <= touch && mixedClinch)
                    TryMixedClinchFullDamageStrikes(otherActor);
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
                _mixedClinchAllyMeleeStrikeCd = Random.Range(0.02f, 0.12f);
                _mixedClinchEnemyMeleeOnAllyCd = Random.Range(0.06f, 0.18f);
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
                enemyHealth.TakeDamage(dmg);
                if (enemyHealth.IsDead || _director.BattleEnded)
                {
                    if (enemyHealth.IsDead)
                        _director.ReportKillingHit(enemyHealth, dmg);
                    return;
                }

                _director.ReportHitExchange(0f, dmg, allyPos, enemyPos);
                return;
            }

            if (_ranged && !otherActor.IsRangedCombat)
            {
                _mixedClinchEnemyMeleeOnAllyCd -= dt;
                if (_mixedClinchEnemyMeleeOnAllyCd > 0f)
                    return;

                _mixedClinchEnemyMeleeOnAllyCd = period;

                _health.TakeDamage(otherActor.AttackDamage);
                if (Health.IsDead || _director.BattleEnded)
                {
                    if (Health.IsDead)
                        _director.ReportKillingHit(_health, otherActor.AttackDamage);
                    return;
                }

                _director.ReportHitExchange(otherActor.AttackDamage, 0f, allyPos, enemyPos);
            }
        }

        void TryMeleeVsMeleeAsAlly(DuelActor otherActor, float dist, Vector2 dir)
        {
            if (!_canDealTouchDamage)
                return;

            _canDealTouchDamage = false;

            var enemyHealth = _lockTarget;
            var allyDmg = AllyOutgoingTo(enemyHealth);
            enemyHealth.TakeDamage(allyDmg);
            if (enemyHealth.IsDead || _director.BattleEnded)
            {
                if (enemyHealth.IsDead)
                    _director.ReportKillingHit(enemyHealth, allyDmg);
                ApplyBounce(this, otherActor, dir);
                return;
            }

            _health.TakeDamage(otherActor.AttackDamage);
            if (Health.IsDead || _director.BattleEnded)
            {
                if (Health.IsDead)
                    _director.ReportKillingHit(_health, otherActor.AttackDamage);
                ApplyBounce(this, otherActor, dir);
                return;
            }

            _director.ReportHitExchange(
                otherActor.AttackDamage,
                allyDmg,
                _health.transform.position,
                enemyHealth.transform.position);
            ApplyBounce(this, otherActor, dir);
        }

        void TryRangedOneSidedShot()
        {
            var targetHealth = _lockTarget;
            if (targetHealth == null || targetHealth.IsDead)
                return;

            var outgoing = _health.Faction == CombatFaction.Ally ? AllyOutgoingTo(targetHealth) : attackDamage;
            var fromW = _health.transform.position;
            var toW = targetHealth.transform.position;
            targetHealth.TakeDamage(outgoing);
            RangedShotVfx.Spawn(fromW, toW, _health.Faction);
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
        }

        static void ApplyBounce(DuelActor selfActor, DuelActor otherActor, Vector2 dirFromSelfToOther)
        {
            var selfT = selfActor.transform;
            var otherT = otherActor.transform;
            var dist = Vector2.Distance(selfT.position, otherT.position);
            var touch = selfActor.TouchDistanceTo(otherActor);
            var needGap = touch + selfActor.touchReleasePadding - dist;
            var perSide = selfActor.knockbackDistance * 0.56f;
            if (needGap > 0f)
                perSide = Mathf.Max(perSide, needGap * 0.5f + 0.02f);

            var a = (Vector2)selfT.position - dirFromSelfToOther * perSide;
            var e = (Vector2)otherT.position + dirFromSelfToOther * perSide;
            selfT.position = new Vector3(a.x, a.y, selfT.position.z);
            otherT.position = new Vector3(e.x, e.y, otherT.position.z);
            ClampToArena(selfT);
            ClampToArena(otherT);
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
