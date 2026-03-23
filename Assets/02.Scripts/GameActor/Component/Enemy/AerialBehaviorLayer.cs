using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Component
{
    /// <summary>
    /// 공중 행동 결정자 (EnemyBrain과 독립적으로 동작).
    ///
    /// EnemyBrain이 0.1초 틱마다 Evaluate()를 호출하고,
    /// 반환값이 ShouldTakeOff이면 EnemyBrain이 EnemyTakeOffState로 전환을 요청한다.
    ///
    /// 공중 State(EnemyAerialState 등)에서도 이 Layer를 통해
    /// ShouldLand / CanAttack / SelectAerialSkill 등을 호출한다.
    /// </summary>
    public class AerialBehaviorLayer : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private AerialBehaviorSO _data;

        // ── 런타임 상태 ──────────────────────────────────────────────
        private bool  _isFlying;
        private float _takeOffCooldownTimer; // 착지 후 경과 시간
        private float _aerialTimer;          // 현재 체공 경과 시간
        private int   _aerialAttackCount;    // 현재 체공 중 공격 횟수

        // ── Poise Break 착지 강제 플래그 ─────────────────────────────
        private bool _forceLand;  // 단일 필드. _forceLoad 오타 제거.

        // ── Dive Attack 대기 스킬 ─────────────────────────────────────
        private EnemyAttackInfo _pendingDiveAttack;

        // ── 공중 스킬 쿨다운 ─────────────────────────────────────────
        private readonly Dictionary<EnemyAttackInfo, float> _aerialCooldowns
            = new Dictionary<EnemyAttackInfo, float>();

        // ── 컴포넌트 참조 ─────────────────────────────────────────────
        private EnemyDetection  _detection;
        private EnemyCombat     _combat;
        private IDamageable     _damageable;
        private EnemyBrain      _brain;
        private ActorMovementController _movement;

        // ── 스폰 지면 Y (고도 기준점) ─────────────────────────────────
        public float SpawnY { get; private set; }

        // ── 프로퍼티 ──────────────────────────────────────────────────
        public AerialBehaviorSO Data           => _data;
        public bool             IsFlying        => _isFlying;
        public bool             HasPendingDiveAttack => _pendingDiveAttack != null;
        public EnemyAttackInfo  DiveAttackSkill => _pendingDiveAttack;

        // 현재 페이즈 오버라이드된 파라미터
        private float TakeOffChance       => GetPhaseOverride(out var ph) && ph.overrideAerial ? ph.aerialTakeOffChance  : _data.takeOffChance;
        private int   MaxAerialAttackCount => GetPhaseOverride(out var ph) && ph.overrideAerial ? ph.aerialMaxAttackCount : _data.maxAerialAttackCount;
        private float AerialHpThreshold   => GetPhaseOverride(out var ph) && ph.overrideAerial ? ph.aerialHpThreshold    : _data.aerialHpThreshold;
        private float AerialDuration      => GetPhaseOverride(out var ph) && ph.overrideAerial ? ph.aerialDuration       : _data.aerialDuration;

        /// <summary> 공중 스킬의 최대 사거리 (CanAttack 접근 판단용) </summary>
        public float MaxAerialRange
        {
            get
            {
                float max = 0f;
                if (_combat?.AttackData == null) return max;
                foreach (var s in _combat.AttackData.skills)
                    if (s.isAerialSkill && s.maxRange > max) max = s.maxRange;
                return max;
            }
        }

        // ── Mono ──────────────────────────────────────────────────────

        private void Awake()
        {
            _detection  = GetComponent<EnemyDetection>();
            _combat     = GetComponent<EnemyCombat>();
            _damageable = GetComponent<IDamageable>();
            _brain      = GetComponent<EnemyBrain>();
            _movement   = GetComponent<ActorMovementController>();
            SpawnY      = transform.position.y;
        }

        private void Update()
        {
            if (!_isFlying)
                _takeOffCooldownTimer += Time.deltaTime;

            UpdateAerialCooldowns();
        }

        // ── EnemyBrain에서 호출 ───────────────────────────────────────

        /// <summary>
        /// EnemyBrain 틱(0.1초)마다 호출.
        /// true 반환 시 EnemyBrain이 EnemyTakeOffState로 전환.
        /// </summary>
        public bool ShouldTakeOff()
        {
            if (_data == null)           return false;
            if (_isFlying)               return false;
            if (_takeOffCooldownTimer < _data.takeOffCooldown) return false;
            if (!_detection.HasTarget)   return false;  // 전투 중일 때만 이륙

            // HP 조건
            float hpPercent = _damageable?.GetHealthPercent() ?? 1f;
            if (hpPercent > AerialHpThreshold) return false;

            // 확률
            return Random.value <= TakeOffChance;
        }

        /// <summary>
        /// 공중 State에서 매 틱 호출.
        /// true 반환 시 EnemyAerialState → EnemyLandState 전환.
        /// </summary>
        public bool ShouldLand()
        {
            if (!_isFlying) return false;
            if (_forceLand) return true;
            if (_aerialTimer >= AerialDuration)             return true;
            if (_aerialAttackCount >= MaxAerialAttackCount) return true;
            if (!_detection.HasTarget)                       return true;
            return false;
        }

        // ── 공중 State에서 호출 ───────────────────────────────────────

        /// <summary> EnemyAerialState.OnEnter에서 호출 </summary>
        public void OnEnterAerial()
        {
            _isFlying         = true;
            _aerialTimer      = 0f;
            _aerialAttackCount = 0;
            _forceLand        = false;
            _pendingDiveAttack = null;
        }

        /// <summary> EnemyLandState.OnExit에서 호출 </summary>
        public void OnLanded()
        {
            _isFlying             = false;
            _takeOffCooldownTimer = 0f;
            _forceLand            = false;
            _pendingDiveAttack    = null;
        }

        /// <summary> 체공 타이머 갱신. EnemyAerialState.UpdateState에서 호출. </summary>
        public void Tick(float deltaTime)
        {
            _aerialTimer += deltaTime;
        }

        /// <summary> Poise Break 등 외부에서 강제 착지 요청 </summary>
        public void RequestForceLand() => _forceLand = true;

        // ── 공중 공격 ─────────────────────────────────────────────────

        /// <summary> 공격 발동 가능 여부 (사거리 + 쿨다운 + 횟수 체크) </summary>
        public bool CanAttack(float dist)
        {
            if (_aerialAttackCount >= MaxAerialAttackCount) return false;
            return _combat?.AttackData?.GetAvailableAerialSkills(dist)?.Count > 0
                   && !HasAerialCooldownActive();
        }

        /// <summary> 거리 기반 공중 스킬 선택 </summary>
        public EnemyAttackInfo SelectAerialSkill(float dist)
        {
            if (_combat?.AttackData == null) return null;
            var available = new List<EnemyAttackInfo>();
            foreach (var s in _combat.AttackData.GetAvailableAerialSkills(dist))
            {
                if (!_aerialCooldowns.ContainsKey(s))
                    available.Add(s);
            }
            var skill = _combat.AttackData.SelectRandomAerialSkill(available);
            if (skill != null) _aerialCooldowns[skill] = skill.cooldown;
            return skill;
        }

        /// <summary> EnemyAerialAttackState.OnExit에서 호출 </summary>
        public void OnAerialAttackEnd()
        {
            _aerialAttackCount++;
        }

        // ── Dive Attack ───────────────────────────────────────────────

        public void SetPendingDiveAttack(EnemyAttackInfo skill)
        {
            _pendingDiveAttack = skill;
        }

        // ── 착지 충격 ─────────────────────────────────────────────────

        /// <summary>
        /// EnemyLandState.OnLandImpact에서 호출.
        /// 반경 내 플레이어에게 착지 충격 데미지 + 넉백.
        /// </summary>
        public void ApplyLandingImpact(Vector3 center, float radius)
        {
            var impactData = GetLandImpactData();
            if (impactData == null) return;

            Collider[] hits = Physics.OverlapSphere(center, radius,
                _combat != null ? LayerMask.GetMask("Player") : ~0,
                QueryTriggerInteraction.Ignore);

            foreach (var col in hits)
            {
                var dmg = col.GetComponent<IDamageable>();
                if (dmg == null || !dmg.CanTakeDamage()) continue;

                var data = new AttackData
                {
                    damage          = impactData.damage,
                    poiseDamage     = impactData.poiseDamage,
                    reactionType    = impactData.reactionType,
                    attackDirection = Vector3.down,
                    knockbackForce  = impactData.knockBackForce,
                    knockbackDrag   = impactData.knockBackDrag,
                    hitParticleName = impactData.hitParticleName,
                    hitPoint        = col.ClosestPoint(center),
                    attacker        = GetComponent<GameActor>(),
                };
                dmg.TakeDamage(data);
            }
        }

        // ── 내부 ──────────────────────────────────────────────────────

        private void UpdateAerialCooldowns()
        {
            var keys = new List<EnemyAttackInfo>(_aerialCooldowns.Keys);
            foreach (var k in keys)
            {
                _aerialCooldowns[k] -= Time.deltaTime;
                if (_aerialCooldowns[k] <= 0f)
                    _aerialCooldowns.Remove(k);
            }
        }

        private bool HasAerialCooldownActive()
        {
            if (_combat?.AttackData == null) return false;
            foreach (var s in _combat.AttackData.skills)
            {
                if (s.isAerialSkill && _aerialCooldowns.ContainsKey(s))
                    return true;
            }
            return false;
        }

        private bool GetPhaseOverride(out BehaviorPhase phase)
        {
            phase = null;
            if (_brain == null) return false;
            // EnemyBrain의 현재 페이즈 접근 — 이미 public 프로퍼티로 노출되어 있다면 직접 사용
            // 없다면 HP 기반으로 직접 계산
            float hp = _damageable?.GetHealthPercent() ?? 1f;
            var   so = GetComponent<EnemyBrain>()?.GetBehaviorSO();
            if (so?.phases == null) return false;
            foreach (var p in so.phases)
            {
                if (hp <= p.hpThreshold) { phase = p; return true; }
            }
            return false;
        }

        private HitPhaseData GetLandImpactData()
        {
            // 착지 충격 데미지는 별도 설정이 없으면 기본값 사용
            return new HitPhaseData
            {
                damage         = 25f,
                poiseDamage    = 50f,
                reactionType   = AttackReactionType.KnockBack,
                knockBackForce = 12f,
                knockBackDrag  = 15f,
                hitParticleName = "HeavyHit",
            };
        }
    }
}
