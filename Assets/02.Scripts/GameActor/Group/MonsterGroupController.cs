using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;
using UPlayGround.State;

namespace UPlayGround.Group
{
    /// <summary>
    /// 그룹 내 멤버 우선순위.
    /// 슬롯 경쟁 시 Summoner > Normal > Summon 순으로 밀려난다.
    /// </summary>
    public enum MemberPriority { Summon, Normal, Summoner }

    public readonly struct GroupIntentBias
    {
        public GroupIntentBias(
            float attackMultiplier,
            float punishMultiplier,
            float counterMultiplier,
            float pressureBonus,
            float keepDistanceBonus,
            float retreatBonus,
            float breatherRemainingTime,
            int formationSlotIndex,
            float aggroFitness)
        {
            AttackMultiplier = attackMultiplier;
            PunishMultiplier = punishMultiplier;
            CounterMultiplier = counterMultiplier;
            PressureBonus = pressureBonus;
            KeepDistanceBonus = keepDistanceBonus;
            RetreatBonus = retreatBonus;
            BreatherRemainingTime = breatherRemainingTime;
            FormationSlotIndex = formationSlotIndex;
            AggroFitness = aggroFitness;
        }

        public static GroupIntentBias Neutral => new(1f, 1f, 1f, 0f, 0f, 0f, 0f, -1, 0f);

        public float AttackMultiplier { get; }
        public float PunishMultiplier { get; }
        public float CounterMultiplier { get; }
        public float PressureBonus { get; }
        public float KeepDistanceBonus { get; }
        public float RetreatBonus { get; }
        public float BreatherRemainingTime { get; }
        public int FormationSlotIndex { get; }
        public float AggroFitness { get; }
    }

    public static class MonsterGroupSlotPolicy
    {
        public static int CalculateLimit(int aliveCount, float ratio, int cap, bool reduceForRecentHit)
        {
            var limit = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Max(1, aliveCount) * Mathf.Clamp01(ratio)),
                1,
                Mathf.Max(1, cap));
            return reduceForRecentHit ? Mathf.Max(1, limit - 1) : limit;
        }

        public static bool HasNormalizedTakeoverMargin(float requesterFitness, float ownerFitness, float margin)
        {
            var difference = (requesterFitness - ownerFitness)
                             / Mathf.Max(0.01f, Mathf.Max(requesterFitness, ownerFitness));
            return difference > Mathf.Max(0f, margin);
        }
    }

    public readonly struct MonsterGroupDebugSnapshot
    {
        public MonsterGroupDebugSnapshot(
            int aliveCount,
            int meleeOwners,
            int rangedOwners,
            int meleeCandidates,
            int rangedCandidates,
            int formationOwners,
            float groupBreatherRemaining,
            float playerBreatherRemaining)
        {
            AliveCount = aliveCount;
            MeleeOwners = meleeOwners;
            RangedOwners = rangedOwners;
            MeleeCandidates = meleeCandidates;
            RangedCandidates = rangedCandidates;
            FormationOwners = formationOwners;
            GroupBreatherRemaining = groupBreatherRemaining;
            PlayerBreatherRemaining = playerBreatherRemaining;
        }

        public int AliveCount { get; }
        public int MeleeOwners { get; }
        public int RangedOwners { get; }
        public int MeleeCandidates { get; }
        public int RangedCandidates { get; }
        public int FormationOwners { get; }
        public float GroupBreatherRemaining { get; }
        public float PlayerBreatherRemaining { get; }
    }

    /// <summary>
    /// 몬스터 그룹 전체를 조율하는 컨트롤러.
    /// 씬의 MonsterGroup GameObject에 배치한다.
    ///
    /// 핵심 역할
    ///   1. Attack Slot 관리 - 근접/원거리 슬롯 분리
    ///   2. 우선순위 기반 슬롯 경쟁 (Summoner > Normal > Summon)
    ///   3. 경보 전파 - 한 명이 발견하면 그룹 전체 각성
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class MonsterGroupController : MonoBehaviour
    {
        [Header("Activation")]
        [Tooltip("활성화 트리거가 호출될 때까지 자식 몬스터를 비활성 상태로 유지합니다.")]
        [SerializeField] private bool _startDormant;

        [Header("Attack Slots")]
        [Tooltip("생존 인원 중 근접 공격 슬롯으로 허용할 비율")]
        [Range(0.1f, 1f)] [SerializeField] private float _meleeSlotRatio = 0.5f;
        [Tooltip("근접 공격 슬롯 상한. 기존 Max Melee Attackers 값이 이 필드로 이관됩니다.")]
        [FormerlySerializedAs("_maxMeleeAttackers")]
        [Min(1)] [SerializeField] private int _meleeSlotCap = 2;
        [Tooltip("생존 인원 중 원거리 공격 슬롯으로 허용할 비율")]
        [Range(0.1f, 1f)] [SerializeField] private float _rangedSlotRatio = 0.5f;
        [Tooltip("원거리 공격 슬롯 상한. 기존 Max Ranged Attackers 값이 이 필드로 이관됩니다.")]
        [FormerlySerializedAs("_maxRangedAttackers")]
        [Min(1)] [SerializeField] private int _rangedSlotCap = 2;

        [Header("Tempo")]
        [Tooltip("멤버 공격 종료 후 그룹 전체가 새 공격 슬롯을 잡지 못하는 시간(초)")]
        [SerializeField] private float _breatherDuration = 0.6f;
        [Tooltip("플레이어가 피격 리액션에 진입한 뒤 신규 공격 슬롯을 막는 시간(초)")]
        [SerializeField] private float _playerBreatherDuration = 0.45f;
        [Tooltip("그룹원이 피격된 직후 한 박자 거리를 두는 시간(초)")]
        [SerializeField] private float _recentGroupHitResponseDuration = 1.5f;
        [Tooltip("그룹원이 피격된 직후 추가할 거리 유지 점수")]
        [SerializeField] private float _recentGroupHitKeepDistanceBonus = 0.2f;

        [Header("Aggro Fitness")]
        [Tooltip("슬롯이 가득 찼을 때 더 적합한 멤버가 기존 점유자를 밀어내기 위해 필요한 최소 점수 차이")]
        [SerializeField] private float _aggroFitnessTakeoverMargin = 0.08f;
        [Tooltip("포화된 공격 슬롯 후보를 다시 평가하는 간격(초)")]
        [SerializeField] private float _aggroDecisionInterval = 0.1f;
        [Tooltip("슬롯을 획득한 멤버를 교체 대상으로부터 보호하는 최소 시간(초)")]
        [SerializeField] private float _minSlotHoldDuration = 0.5f;
        [Tooltip("화면 밖 멤버의 Aggro Fitness에 곱할 값")]
        [Range(0f, 1f)] [SerializeField] private float _offscreenAggroMultiplier = 0.55f;
        [Tooltip("비워두면 Camera.main을 사용합니다.")]
        [SerializeField] private Camera _visibilityCamera;

        [Header("Formation")]
        [Tooltip("플레이어 주변을 나누는 공간 슬롯 개수")]
        [SerializeField] private int _formationSlotCount = 8;

        [Header("Alert Propagation")]
        [Tooltip("최초 발견 지점에서 경보를 전달할 최대 거리")]
        [Min(0f)] [SerializeField] private float _alertRadius = 18f;
        [Tooltip("가까운 멤버부터 순차적으로 경보를 받는 기본 지연(초)")]
        [Min(0f)] [SerializeField] private float _alertPropagationDelay = 0.12f;
        [Tooltip("동일 타깃 경보의 재귀 전파를 합칠 시간(초)")]
        [Min(0f)] [SerializeField] private float _alertDedupeDuration = 2f;
        [Tooltip("0이 아니면 이 레이어에 가로막힌 멤버에게 경보를 전달하지 않습니다.")]
        [SerializeField] private LayerMask _alertObstructionMask;

        // 슬롯 점유 추적 — (actor, priority) 쌍으로 저장
        private readonly Dictionary<MonsterActor, MemberPriority> _meleeSlotOwners  = new();
        private readonly Dictionary<MonsterActor, MemberPriority> _rangedSlotOwners = new();
        private readonly Dictionary<MonsterActor, SlotCandidate> _meleeSlotCandidates = new();
        private readonly Dictionary<MonsterActor, SlotCandidate> _rangedSlotCandidates = new();
        private readonly Dictionary<MonsterActor, float> _meleeSlotAcquiredTimes = new();
        private readonly Dictionary<MonsterActor, float> _rangedSlotAcquiredTimes = new();
        private readonly Dictionary<FormationSlotKey, MonsterActor> _formationOwners = new();
        private readonly Dictionary<MonsterActor, FormationSlotKey> _formationSlots = new();

        // 멤버 레지스트리
        private readonly List<MonsterActor>                        _members    = new();
        private readonly Dictionary<MonsterActor, MemberPriority>  _priorities = new();
        private readonly HashSet<MonsterActor>                     _dormantActivationMembers = new();
        // 사망/무효 멤버 정리를 위한 공용 스크래치. CleanupDeadSlotOwners/CleanupDeadCandidates 양쪽이 사용.
        private readonly List<MonsterActor>                        _cleanupScratch = new();
        private readonly List<MonsterActor>                        _alertOrderScratch = new();
        private readonly DistanceFromPointComparer                 _alertDistanceComparer = new();
        private readonly List<FormationSlotKey>                    _deadFormationSlots = new();

        private bool _isActivated = false;
        private bool _activationRequested;
        private bool _defeatNotified;
        private int _peakAliveCount;
        private float _groupBreatherUntil = -999f;
        private float _playerBreatherUntil = -999f;
        private float _nextMeleeAggroDecisionTime = -999f;
        private float _nextRangedAggroDecisionTime = -999f;
        private MonsterGroupMemory _memory;
        private Transform _lastAlertTarget;
        private float _lastAlertTime = -999f;

        public MonsterGroupMemory Memory => _memory;
        public bool IsActivated => _isActivated;
        public int CurrentMeleeSlotLimit => GetDynamicSlotLimit(AttackType.Melee);
        public int CurrentRangedSlotLimit => GetDynamicSlotLimit(AttackType.Ranged);

        public MonsterGroupDebugSnapshot GetDebugSnapshot()
            => new(
                AliveCount,
                _meleeSlotOwners.Count,
                _rangedSlotOwners.Count,
                _meleeSlotCandidates.Count,
                _rangedSlotCandidates.Count,
                _formationOwners.Count,
                Mathf.Max(0f, _groupBreatherUntil - Time.time),
                Mathf.Max(0f, _playerBreatherUntil - Time.time));

    /// <summary>
    /// 그룹 내 모든 멤버가 사망했을 때 1회 발동.
    /// TriggerComposer 등 외부에서 구독해 후속 처리를 연결한다.
    /// </summary>
    public event Action OnGroupDefeated;

        #region 초기화

        private void Awake()
        {
            _memory ??= GetComponent<MonsterGroupMemory>();
            _memory ??= gameObject.AddComponent<MonsterGroupMemory>();

            if (!_startDormant)
                return;

            var actors = GetComponentsInChildren<MonsterActor>(includeInactive: true);
            foreach (var actor in actors)
            {
                EnsureMemberRegistered(actor);
                if (actor != null && actor.gameObject.activeSelf)
                {
                    _dormantActivationMembers.Add(actor);
                    actor.gameObject.SetActive(false);
                }
            }
        }

        private void Start()
        {
            _memory ??= GetComponent<MonsterGroupMemory>();
            _memory ??= gameObject.AddComponent<MonsterGroupMemory>();

            // Awake 타이밍에 호출하면 MonsterActor.AIController가 아직 null일 수 있음
            // Start에서 수집하면 모든 컴포넌트 Awake 완료 후 보장됨
            var actors = GetComponentsInChildren<MonsterActor>(includeInactive: true);
            foreach (var actor in actors)
            {
                EnsureMemberRegistered(actor);
                if (actor != null && actor.gameObject.activeInHierarchy && actor.AIController == null)
                {
                    Debug.LogWarning(
                        $"[MonsterGroupController] 활성 멤버 '{actor.name}'의 AIController가 없어 그룹을 바인딩하지 못했습니다.",
                        actor);
                }
            }

            _peakAliveCount = Mathf.Max(_peakAliveCount, AliveCount);

            _isActivated = _activationRequested || !_startDormant;
            if (_startDormant && !_isActivated)
            {
                foreach (var actor in actors)
                    if (actor != null)
                        actor.gameObject.SetActive(false);
            }
        }

        public void Activate()
        {
            if (_isActivated) return;
            _activationRequested = true;
            _isActivated = true;

            foreach (var member in _members)
            {
                if (member == null || !_dormantActivationMembers.Contains(member))
                    continue;

                member.gameObject.SetActive(true);
                if (!TryBindMemberController(member, _priorities[member]))
                {
                    Debug.LogWarning(
                        $"[MonsterGroupController] 잠복 해제된 멤버 '{member.name}'의 AIController가 없어 그룹을 바인딩하지 못했습니다.",
                        member);
                }
            }

            _peakAliveCount = Mathf.Max(_peakAliveCount, AliveCount);
        }

        #endregion

        #region 멤버 등록/해제

        public void RegisterMember(MonsterActor actor, MemberPriority priority)
        {
            if (actor == null)
                return;

            if (!_priorities.ContainsKey(actor))
            {
                if (_members.Count == 0)
                    _peakAliveCount = 0;

                _members.Add(actor);
                _priorities[actor] = priority;
                _peakAliveCount = Mathf.Max(_peakAliveCount, AliveCount);
                _defeatNotified = false;
            }
            else
            {
                _priorities[actor] = priority;
            }

            TryBindMemberController(actor, priority);
        }

        public void EnsureMemberRegistered(MonsterActor actor)
        {
            if (actor == null)
                return;

            if (_priorities.TryGetValue(actor, out var priority))
            {
                TryBindMemberController(actor, priority);
                return;
            }

            RegisterMember(actor, MemberPriority.Normal);
        }

        private bool TryBindMemberController(MonsterActor actor, MemberPriority priority)
        {
            if (actor == null || actor.AIController == null)
                return false;

            actor.AIController.SetGroup(this, priority);
            return true;
        }

        public void UnregisterMember(MonsterActor actor)
        {
            if (actor == null) return;

            _members.Remove(actor);
            _priorities.Remove(actor);
            _dormantActivationMembers.Remove(actor);
            _meleeSlotCandidates.Remove(actor);
            _rangedSlotCandidates.Remove(actor);
            ReleaseAttackSlot(actor);
            ReleaseFormationSlot(actor);

            // 전멸 감지 — 활성화된 그룹이 전부 사망했을 때만 발동
            if (_isActivated && !_defeatNotified && AliveCount == 0)
            {
                _defeatNotified = true;
                OnGroupDefeated?.Invoke();
            }
        }

        #endregion

        #region Attack Slot

        /// <summary>
        /// 슬롯을 요청한다.
        /// 빈 슬롯이 있으면 즉시 점유.
        /// 슬롯이 꽉 찼으면 자신보다 낮은 우선순위 점유자를 밀어내고 들어간다.
        /// 밀려난 쪽은 슬롯을 잃고 다음 판단 주기에 다시 요청하게 된다.
        /// </summary>
        public bool RequestAttackSlot(MonsterActor requester, AttackType attackType)
        {
            if (!_priorities.TryGetValue(requester, out var priority))
                return true; // 그룹 비소속 — 제한 없음

            var slotOwners = attackType == AttackType.Melee ? _meleeSlotOwners : _rangedSlotOwners;
            var candidates = attackType == AttackType.Melee ? _meleeSlotCandidates : _rangedSlotCandidates;
            var acquiredTimes = attackType == AttackType.Melee ? _meleeSlotAcquiredTimes : _rangedSlotAcquiredTimes;
            int maxSlots = GetDynamicSlotLimit(attackType);

            if ((IsInBreatherWindow || IsInPlayerBreatherWindow) && !slotOwners.ContainsKey(requester))
                return false;

            CleanupDeadSlotOwners(slotOwners, acquiredTimes);
            CleanupDeadCandidates(candidates);

            if (slotOwners.ContainsKey(requester))
                return true;

            if (slotOwners.Count < maxSlots)
                return TryOccupySlot(requester, priority, slotOwners, acquiredTimes, maxSlots);

            if (!candidates.ContainsKey(requester))
                candidates[requester] = new SlotCandidate(priority, Time.time);

            if (attackType == AttackType.Melee)
                ProcessAggroCandidates(candidates, slotOwners, acquiredTimes, maxSlots, ref _nextMeleeAggroDecisionTime);
            else
                ProcessAggroCandidates(candidates, slotOwners, acquiredTimes, maxSlots, ref _nextRangedAggroDecisionTime);

            return slotOwners.ContainsKey(requester);
        }

        public void ReleaseAttackSlot(MonsterActor releaser)
        {
            _meleeSlotOwners.Remove(releaser);
            _rangedSlotOwners.Remove(releaser);
            _meleeSlotAcquiredTimes.Remove(releaser);
            _rangedSlotAcquiredTimes.Remove(releaser);
            _meleeSlotCandidates.Remove(releaser);
            _rangedSlotCandidates.Remove(releaser);
        }

        public bool TryGetFormationSlotPosition(
            MonsterActor member,
            Vector3 targetPosition,
            Vector3 targetForward,
            float radius,
            out Vector3 position)
        {
            position = default;
            if (member == null || !_priorities.ContainsKey(member))
                return false;

            CleanupFormationOwners();
            var slot = RequestFormationSlotKey(member, targetPosition, targetForward);
            if (!slot.IsValid)
                return false;

            var resolvedRadius = slot.Ring == FormationRing.Ranged
                ? Mathf.Max(radius, GetMemberOptimalDistance(member))
                : radius;
            position = GetFormationSlotPosition(slot.Index, targetPosition, targetForward, resolvedRadius);
            return true;
        }

        public int RequestFormationSlot(MonsterActor member, Vector3 targetPosition, Vector3 targetForward)
            => RequestFormationSlotKey(member, targetPosition, targetForward).Index;

        private FormationSlotKey RequestFormationSlotKey(
            MonsterActor member,
            Vector3 targetPosition,
            Vector3 targetForward)
        {
            if (_formationSlots.TryGetValue(member, out var existingSlot)
                && IsFormationOwner(existingSlot, member))
                return existingSlot;

            ReleaseFormationSlot(member);

            var desiredSlot = ComputeDesiredFormationSlot(member.transform.position, targetPosition, targetForward);
            var ring = GetMemberAttackType(member) == AttackType.Ranged
                ? FormationRing.Ranged
                : FormationRing.Melee;
            var slot = FindAvailableFormationSlot(ring, desiredSlot);
            if (!slot.IsValid)
                return FormationSlotKey.Invalid;

            _formationOwners[slot] = member;
            _formationSlots[member] = slot;
            return slot;
        }

        public void ReleaseFormationSlot(MonsterActor member)
        {
            if (member == null)
                return;

            if (_formationSlots.TryGetValue(member, out var slot)
                && IsFormationOwner(slot, member))
                _formationOwners.Remove(slot);

            _formationSlots.Remove(member);
        }

        public Vector3 GetFormationSlotPosition(int slotIndex, Vector3 targetPosition, Vector3 targetForward, float radius)
        {
            var forward = targetForward.sqrMagnitude > 0.001f ? targetForward.normalized : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.001f)
                forward = Vector3.forward;

            var angleStep = 360f / Mathf.Max(1, _formationSlotCount);
            var angle = -180f + slotIndex * angleStep;
            var direction = Quaternion.Euler(0f, angle, 0f) * forward;
            var position = targetPosition + direction.normalized * Mathf.Max(0.1f, radius);
            position.y = targetPosition.y;
            return position;
        }

        /// <summary>
        /// 근접 그룹 멤버들로부터 밀려나는 분리(separation) 벡터를 계산한다.
        /// 여러 마리가 같은 지점으로 수렴해 서로 콜라이더로 막혀 멈추는 현상을 완화한다.
        /// 반환 벡터는 수평(XZ) 방향이며 크기는 0~1로 정규화된 밀어내기 강도.
        /// 근접한 동료가 없으면 Vector3.zero.
        /// </summary>
        public Vector3 ComputeSeparation(MonsterActor self, float radius)
        {
            if (self == null || radius <= 0.01f)
                return Vector3.zero;

            var selfPos = self.transform.position;
            var push = Vector3.zero;
            var radiusSq = radius * radius;

            for (int i = 0; i < _members.Count; i++)
            {
                var other = _members[i];
                if (other == null || ReferenceEquals(other, self) || !other.IsAlive())
                    continue;

                var delta = selfPos - other.transform.position;
                delta.y = 0f;
                var distSq = delta.sqrMagnitude;
                if (distSq >= radiusSq || distSq < 0.0001f)
                    continue;

                // 가까울수록 강하게 밀어낸다 (선형 폴오프).
                var dist = Mathf.Sqrt(distSq);
                push += (delta / dist) * (1f - dist / radius);
            }

            if (push.sqrMagnitude > 1f)
                push.Normalize();
            return push;
        }

        public void NotifyMemberAttackEnded(MonsterActor member)
        {
            if (member == null)
                return;

            var ownedSlot = _meleeSlotOwners.ContainsKey(member)
                            || _rangedSlotOwners.ContainsKey(member);
            ReleaseAttackSlot(member);

            var duration = ResolveBreatherDuration(member);
            if (ownedSlot && duration > 0f && _priorities.ContainsKey(member))
                _groupBreatherUntil = Mathf.Max(_groupBreatherUntil, Time.time + duration);
        }

        public void NotifyPlayerEnteredHitReaction()
        {
            if (_playerBreatherDuration > 0f)
                _playerBreatherUntil = Mathf.Max(
                    _playerBreatherUntil,
                    Time.time + _playerBreatherDuration);
        }

        public GroupIntentBias GetIntentBias(MonsterActor member, AttackType attackType)
        {
            if (member == null || !_priorities.ContainsKey(member))
                return GroupIntentBias.Neutral;

            CleanupDeadSlotOwners(_meleeSlotOwners, _meleeSlotAcquiredTimes);
            CleanupDeadSlotOwners(_rangedSlotOwners, _rangedSlotAcquiredTimes);

            var slotOwners = attackType == AttackType.Melee ? _meleeSlotOwners : _rangedSlotOwners;
            var maxSlots = GetDynamicSlotLimit(attackType);
            var ownsSlot = slotOwners.ContainsKey(member);
            var slotsFull = slotOwners.Count >= maxSlots;

            var attackMultiplier = 1f;
            var punishMultiplier = 1f;
            var counterMultiplier = 1f;
            var pressureBonus = 0f;
            var keepDistanceBonus = 0f;
            var retreatBonus = 0f;

            if ((IsInBreatherWindow || IsInPlayerBreatherWindow) && !ownsSlot)
            {
                attackMultiplier *= 0.3f;
                punishMultiplier *= 0.3f;
                counterMultiplier *= 0.3f;
                keepDistanceBonus += 0.15f;
            }

            if (!ownsSlot && slotsFull)
            {
                attackMultiplier *= 0.4f;
                punishMultiplier *= 0.4f;
                pressureBonus += 0.2f;
            }

            if (AliveCount == 1)
                retreatBonus += 0.15f;

            ApplyGroupPhaseBias(member, ref attackMultiplier, ref pressureBonus, ref keepDistanceBonus, ref retreatBonus);

            if (_memory != null
                && Time.time - _memory.LastHitOnGroupTime <= Mathf.Max(0f, _recentGroupHitResponseDuration))
            {
                keepDistanceBonus += Mathf.Max(0f, _recentGroupHitKeepDistanceBonus);
                if (!ownsSlot && attackType == AttackType.Melee)
                    attackMultiplier *= 0.7f;
            }

            var formationSlotIndex = _formationSlots.TryGetValue(member, out var formationSlot)
                ? formationSlot.Index
                : -1;
            var target = member.Detection != null && member.Detection.HasTarget
                ? member.Detection.CurrentTarget
                : null;
            var aggroFitness = target != null ? ComputeAggroFitness(member, target) : 0f;

            return new GroupIntentBias(
                attackMultiplier,
                punishMultiplier,
                counterMultiplier,
                pressureBonus,
                keepDistanceBonus,
                retreatBonus,
                BreatherRemainingTime,
                formationSlotIndex,
                aggroFitness);
        }

        private void CleanupDeadSlotOwners(
            Dictionary<MonsterActor, MemberPriority> slotOwners,
            Dictionary<MonsterActor, float> acquiredTimes)
        {
            _cleanupScratch.Clear();
            foreach (var kv in slotOwners)
                if (kv.Key == null || !kv.Key.IsAlive()) _cleanupScratch.Add(kv.Key);
            foreach (var d in _cleanupScratch)
            {
                slotOwners.Remove(d);
                acquiredTimes.Remove(d);
            }
        }

        private void CleanupDeadCandidates(Dictionary<MonsterActor, SlotCandidate> candidates)
        {
            _cleanupScratch.Clear();
            foreach (var kv in candidates)
                if (kv.Key == null || !kv.Key.IsAlive()) _cleanupScratch.Add(kv.Key);
            foreach (var d in _cleanupScratch) candidates.Remove(d);
        }

        private bool TryOccupySlot(
            MonsterActor requester,
            MemberPriority requesterPriority,
            Dictionary<MonsterActor, MemberPriority> slotOwners,
            Dictionary<MonsterActor, float> acquiredTimes,
            int maxSlots)
        {
            // 이미 점유 중 → 재요청 허용
            if (slotOwners.ContainsKey(requester)) return true;

            // 사망자 정리
            CleanupDeadSlotOwners(slotOwners, acquiredTimes);

            // 빈 슬롯 있으면 바로 점유
            if (slotOwners.Count < maxSlots)
            {
                OccupySlot(requester, requesterPriority, slotOwners, acquiredTimes);
                return true;
            }

            // 슬롯이 꽉 찼을 때 — 자신보다 낮은 우선순위 점유자 탐색
            MonsterActor lowestActor    = null;
            MemberPriority lowestPriority = requesterPriority; // 자신보다 낮아야 함

            foreach (var kv in slotOwners)
            {
                if (IsAttackSlotOwnerLocked(kv.Key, acquiredTimes))
                    continue;

                if (kv.Value < lowestPriority)
                {
                    lowestPriority = kv.Value;
                    lowestActor    = kv.Key;
                }
            }

            if (lowestActor == null
                && !TryFindFitnessTakeoverTarget(requester, requesterPriority, slotOwners, acquiredTimes, out lowestActor))
                return false; // 밀어낼 대상 없음 → 거절

            // 밀어내기: 낮은 우선순위 점유자가 슬롯을 잃음.
            // 해당 몬스터는 다음 BT 판단 주기에 슬롯 재요청 또는 CircleState 대기.
            slotOwners.Remove(lowestActor);
            acquiredTimes.Remove(lowestActor);
            OccupySlot(requester, requesterPriority, slotOwners, acquiredTimes);
            return true;
        }

        private void ProcessAggroCandidates(
            Dictionary<MonsterActor, SlotCandidate> candidates,
            Dictionary<MonsterActor, MemberPriority> slotOwners,
            Dictionary<MonsterActor, float> acquiredTimes,
            int maxSlots,
            ref float nextDecisionTime)
        {
            if (Time.time < nextDecisionTime)
                return;

            nextDecisionTime = Time.time + Mathf.Max(0.01f, _aggroDecisionInterval);
            CleanupDeadSlotOwners(slotOwners, acquiredTimes);
            CleanupDeadCandidates(candidates);

            if (candidates.Count == 0)
                return;

            if (slotOwners.Count < maxSlots)
            {
                while (slotOwners.Count < maxSlots && TryGetBestAggroCandidate(candidates, out var openRequester, out var openCandidate))
                {
                    candidates.Remove(openRequester);
                    OccupySlot(openRequester, openCandidate.Priority, slotOwners, acquiredTimes);
                }
                return;
            }

            if (!TryGetBestReplacementCandidate(candidates, slotOwners, acquiredTimes, out var requester, out var candidate, out var target))
                return;

            slotOwners.Remove(target);
            acquiredTimes.Remove(target);
            OccupySlot(requester, candidate.Priority, slotOwners, acquiredTimes);
            candidates.Remove(requester);
        }

        private bool TryGetBestAggroCandidate(
            Dictionary<MonsterActor, SlotCandidate> candidates,
            out MonsterActor bestRequester,
            out SlotCandidate bestCandidate)
        {
            bestRequester = null;
            bestCandidate = default;
            var bestScore = float.MinValue;

            foreach (var kv in candidates)
            {
                var target = kv.Key.Detection != null && kv.Key.Detection.HasTarget
                    ? kv.Key.Detection.CurrentTarget
                    : null;
                var score = ComputeCandidateScore(kv.Key, kv.Value, target);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestRequester = kv.Key;
                bestCandidate = kv.Value;
            }

            return bestRequester != null;
        }

        private bool TryGetBestReplacementCandidate(
            Dictionary<MonsterActor, SlotCandidate> candidates,
            Dictionary<MonsterActor, MemberPriority> slotOwners,
            Dictionary<MonsterActor, float> acquiredTimes,
            out MonsterActor bestRequester,
            out SlotCandidate bestCandidate,
            out MonsterActor bestTarget)
        {
            bestRequester = null;
            bestCandidate = default;
            bestTarget = null;
            var bestScore = float.MinValue;

            foreach (var kv in candidates)
            {
                if (!TryFindPriorityTakeoverTarget(kv.Value.Priority, slotOwners, acquiredTimes, out var target)
                    && !TryFindFitnessTakeoverTarget(kv.Key, kv.Value.Priority, slotOwners, acquiredTimes, out target))
                    continue;

                var targetTransform = kv.Key.Detection != null && kv.Key.Detection.HasTarget
                    ? kv.Key.Detection.CurrentTarget
                    : null;
                var score = ComputeCandidateScore(kv.Key, kv.Value, targetTransform);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestRequester = kv.Key;
                bestCandidate = kv.Value;
                bestTarget = target;
            }

            return bestRequester != null && bestTarget != null;
        }

        private bool TryFindPriorityTakeoverTarget(
            MemberPriority requesterPriority,
            Dictionary<MonsterActor, MemberPriority> slotOwners,
            Dictionary<MonsterActor, float> acquiredTimes,
            out MonsterActor takeoverTarget)
        {
            takeoverTarget = null;
            var lowestPriority = requesterPriority;

            foreach (var kv in slotOwners)
            {
                if (IsAttackSlotOwnerLocked(kv.Key, acquiredTimes))
                    continue;

                if (kv.Value >= lowestPriority)
                    continue;

                lowestPriority = kv.Value;
                takeoverTarget = kv.Key;
            }

            return takeoverTarget != null;
        }

        private bool TryFindFitnessTakeoverTarget(
            MonsterActor requester,
            MemberPriority requesterPriority,
            Dictionary<MonsterActor, MemberPriority> slotOwners,
            Dictionary<MonsterActor, float> acquiredTimes,
            out MonsterActor takeoverTarget)
        {
            takeoverTarget = null;

            var target = requester.Detection != null && requester.Detection.HasTarget
                ? requester.Detection.CurrentTarget
                : null;
            if (target == null)
                return false;

            var requesterFitness = ComputeAggroFitness(requester, target);
            var lowestFitness = float.MaxValue;

            foreach (var kv in slotOwners)
            {
                if (kv.Key == null || kv.Value > requesterPriority || IsAttackSlotOwnerLocked(kv.Key, acquiredTimes))
                    continue;

                var ownerFitness = ComputeAggroFitness(kv.Key, target);
                if (ownerFitness < lowestFitness)
                {
                    lowestFitness = ownerFitness;
                    takeoverTarget = kv.Key;
                }
            }

            if (takeoverTarget == null)
                return false;

            return MonsterGroupSlotPolicy.HasNormalizedTakeoverMargin(
                requesterFitness,
                lowestFitness,
                _aggroFitnessTakeoverMargin);
        }

        private bool IsAttackSlotOwnerLocked(
            MonsterActor owner,
            Dictionary<MonsterActor, float> acquiredTimes)
        {
            if (owner == null)
                return false;

            if (acquiredTimes.TryGetValue(owner, out var acquiredTime)
                && Time.time - acquiredTime < Mathf.Max(0f, _minSlotHoldDuration))
                return true;

            var stateId = owner.ActorController?.CurrentState?.StateId;
            return stateId is ActorStateId.Attack
                or ActorStateId.Flying_GroundAttack
                or ActorStateId.Flying_AirCircle
                or ActorStateId.Flying_Dive;
        }

        private float ComputeAggroFitness(MonsterActor member, Transform target)
        {
            if (member == null || target == null)
                return 0f;

            var toMember = member.transform.position - target.position;
            var distance = toMember.magnitude;
            var direction = distance > 0.001f ? toMember / distance : target.forward;
            var angle = Vector3.Angle(target.forward, direction);

            var optimalDistance = member.GroundAIController != null
                ? member.GroundAIController.OptimalCombatDistance
                : 2.5f;

            var distanceScore = Mathf.Clamp01(1f - Mathf.Abs(distance - optimalDistance) / 4f);
            var frontScore = Mathf.Clamp01(1f - angle / 180f);
            var hpScore = Mathf.Clamp01(member.GetHealthPercent());

            var fitness = distanceScore * 0.5f + frontScore * 0.3f + hpScore * 0.2f;
            if (!IsVisibleFromGameplayCamera(member.transform.position))
                fitness *= Mathf.Clamp01(_offscreenAggroMultiplier);

            return fitness;
        }

        private float ComputeCandidateScore(MonsterActor member, SlotCandidate candidate, Transform target)
        {
            var fitness = target != null ? ComputeAggroFitness(member, target) : 0f;
            var waitingBonus = Mathf.Clamp01(Time.time - candidate.RequestedTime) * 0.05f;
            return ((int)candidate.Priority * 10f) + fitness + waitingBonus;
        }

        private int ComputeDesiredFormationSlot(Vector3 memberPosition, Vector3 targetPosition, Vector3 targetForward)
        {
            var forward = targetForward.sqrMagnitude > 0.001f ? targetForward.normalized : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.001f)
                forward = Vector3.forward;

            var toMember = memberPosition - targetPosition;
            toMember.y = 0f;
            if (toMember.sqrMagnitude <= 0.001f)
                toMember = -forward;

            var angleStep = 360f / Mathf.Max(1, _formationSlotCount);
            var angle = Vector3.SignedAngle(forward, toMember.normalized, Vector3.up);
            return Mod(Mathf.RoundToInt((angle + 180f) / angleStep), _formationSlotCount);
        }

        private FormationSlotKey FindAvailableFormationSlot(FormationRing ring, int desiredSlot)
        {
            var count = Mathf.Max(1, _formationSlotCount);
            var desired = new FormationSlotKey(ring, desiredSlot);
            if (!_formationOwners.ContainsKey(desired))
                return desired;

            for (var offset = 1; offset < count; offset++)
            {
                var clockwise = new FormationSlotKey(ring, Mod(desiredSlot + offset, count));
                if (!_formationOwners.ContainsKey(clockwise))
                    return clockwise;

                var counterClockwise = new FormationSlotKey(ring, Mod(desiredSlot - offset, count));
                if (!_formationOwners.ContainsKey(counterClockwise))
                    return counterClockwise;
            }

            return FormationSlotKey.Invalid;
        }

        private bool IsFormationOwner(FormationSlotKey slot, MonsterActor member)
            => _formationOwners.TryGetValue(slot, out var owner) && owner == member;

        private void CleanupFormationOwners()
        {
            _deadFormationSlots.Clear();
            foreach (var kv in _formationOwners)
                if (kv.Value == null || !kv.Value.IsAlive())
                    _deadFormationSlots.Add(kv.Key);

            foreach (var slot in _deadFormationSlots)
            {
                if (_formationOwners.TryGetValue(slot, out var owner) && owner != null)
                    _formationSlots.Remove(owner);
                _formationOwners.Remove(slot);
            }
        }

        private static int Mod(int value, int count)
        {
            count = Mathf.Max(1, count);
            var result = value % count;
            return result < 0 ? result + count : result;
        }

        private int GetDynamicSlotLimit(AttackType attackType)
        {
            var ratio = attackType == AttackType.Melee ? _meleeSlotRatio : _rangedSlotRatio;
            var cap = attackType == AttackType.Melee ? _meleeSlotCap : _rangedSlotCap;
            var reduceForRecentHit = attackType == AttackType.Melee
                                     && _memory != null
                                     && Time.time - _memory.LastHitOnGroupTime
                                     <= Mathf.Max(0f, _recentGroupHitResponseDuration);
            return MonsterGroupSlotPolicy.CalculateLimit(AliveCount, ratio, cap, reduceForRecentHit);
        }

        private void OccupySlot(
            MonsterActor member,
            MemberPriority priority,
            Dictionary<MonsterActor, MemberPriority> slotOwners,
            Dictionary<MonsterActor, float> acquiredTimes)
        {
            slotOwners[member] = priority;
            acquiredTimes[member] = Time.time;
        }

        private float ResolveBreatherDuration(MonsterActor member)
        {
            var groundAI = member != null ? member.GroundAIController : null;
            var behavior = groundAI != null ? groundAI.BehaviorData : null;
            var phase = groundAI != null ? groundAI.CurrentPhase : null;
            var overrideDuration = phase != null && phase.breatherDurationOverride >= 0f
                ? phase.breatherDurationOverride
                : behavior != null ? behavior.breatherDurationOverride : -1f;
            var duration = overrideDuration >= 0f ? overrideDuration : _breatherDuration;
            var strategy = phase?.combatStrategyOverride ?? behavior?.combatStrategy;
            return duration * Mathf.Max(0f, strategy?.groupBreatherMultiplier ?? 1f);
        }

        private void ApplyGroupPhaseBias(
            MonsterActor member,
            ref float attackMultiplier,
            ref float pressureBonus,
            ref float keepDistanceBonus,
            ref float retreatBonus)
        {
            var aliveRatio = _peakAliveCount > 0 ? (float)AliveCount / _peakAliveCount : 1f;
            if (aliveRatio > 0.66f)
            {
                pressureBonus += 0.1f;
                return;
            }

            if (AliveCount > 2 && aliveRatio > 0.33f)
                return;

            var role = member?.GroundAIController?.BehaviorData?.aiRole ?? EnemyAIRole.Melee;
            if (role is EnemyAIRole.Melee or EnemyAIRole.RangedMain)
            {
                attackMultiplier *= 1.1f;
                pressureBonus += 0.1f;
            }
            else
            {
                keepDistanceBonus += 0.1f;
                retreatBonus += 0.15f;
            }
        }

        private AttackType GetMemberAttackType(MonsterActor member)
        {
            var combat = member != null ? member.Combat : null;
            return combat?.HasAttackType(AttackType.Ranged) == true
                   && !combat.HasAttackType(AttackType.Melee)
                ? AttackType.Ranged
                : AttackType.Melee;
        }

        private static float GetMemberOptimalDistance(MonsterActor member)
        {
            if (member?.GroundAIController != null)
                return member.GroundAIController.OptimalCombatDistance;
            if (member?.FlyingAIController != null)
                return member.FlyingAIController.OptimalCombatDistance;
            return 3f;
        }

        private bool IsVisibleFromGameplayCamera(Vector3 worldPosition)
        {
            var camera = _visibilityCamera != null ? _visibilityCamera : Camera.main;
            if (camera == null)
                return true;

            var viewport = camera.WorldToViewportPoint(worldPosition);
            return viewport.z > 0f
                   && viewport.x >= 0f && viewport.x <= 1f
                   && viewport.y >= 0f && viewport.y <= 1f;
        }

        private enum FormationRing : byte
        {
            Melee,
            Ranged,
        }

        private readonly struct FormationSlotKey : IEquatable<FormationSlotKey>
        {
            public static FormationSlotKey Invalid => new(FormationRing.Melee, -1);

            public FormationSlotKey(FormationRing ring, int index)
            {
                Ring = ring;
                Index = index;
            }

            public FormationRing Ring { get; }
            public int Index { get; }
            public bool IsValid => Index >= 0;

            public bool Equals(FormationSlotKey other) => Ring == other.Ring && Index == other.Index;
            public override bool Equals(object obj) => obj is FormationSlotKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine((int)Ring, Index);
        }

        private readonly struct SlotCandidate
        {
            public SlotCandidate(MemberPriority priority, float requestedTime)
            {
                Priority = priority;
                RequestedTime = requestedTime;
            }

            public MemberPriority Priority { get; }
            public float RequestedTime { get; }
        }

        #endregion

        #region 경보 전파

        public void AlertGroup(Transform target, MonsterActor source = null)
        {
            if (target == null)
                return;

            // AcquireTarget가 각 멤버의 OnTargetAcquiredExternally를 다시 발생시키므로
            // 동일 타겟의 전파 루프를 짧은 창으로 합친다.
            if (_lastAlertTarget == target
                && Time.time - _lastAlertTime < Mathf.Max(0f, _alertDedupeDuration))
                return;
            _lastAlertTarget = target;
            _lastAlertTime = Time.time;

            var origin = source != null ? source.transform.position : target.position;
            _alertOrderScratch.Clear();
            _alertOrderScratch.AddRange(_members);
            _alertDistanceComparer.Origin = origin;
            _alertOrderScratch.Sort(_alertDistanceComparer);

            var propagationIndex = 0;
            foreach (var member in _alertOrderScratch)
            {
                if (member == null || !member.IsAlive()) continue;
                if (member.Detection == null || member.Detection.HasTarget) continue;

                var distance = Vector3.Distance(member.transform.position, origin);
                if (_alertRadius > 0f && distance > _alertRadius) continue;
                if (_alertObstructionMask.value != 0
                    && Physics.Linecast(origin, member.transform.position, _alertObstructionMask, QueryTriggerInteraction.Ignore))
                    continue;

                var delay = Mathf.Max(0f, _alertPropagationDelay) * propagationIndex++;
                if (delay <= 0f)
                    member.Detection.AcquireTarget(target);
                else
                    StartCoroutine(AlertMemberAfterDelay(member, target, delay));
            }
        }

        private IEnumerator AlertMemberAfterDelay(MonsterActor member, Transform target, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (member != null && member.IsAlive() && member.Detection != null
                && !member.Detection.HasTarget && target != null)
                member.Detection.AcquireTarget(target);
        }

        private static float DistanceSquared(MonsterActor member, Vector3 position)
            => member == null ? float.MaxValue : (member.transform.position - position).sqrMagnitude;

        private sealed class DistanceFromPointComparer : IComparer<MonsterActor>
        {
            public Vector3 Origin { get; set; }

            public int Compare(MonsterActor x, MonsterActor y)
                => DistanceSquared(x, Origin).CompareTo(DistanceSquared(y, Origin));
        }

        #endregion

        #region 상태 질의

        public int AliveCount
        {
            get
            {
                int count = 0;
                foreach (var m in _members)
                    // 잠복 중이거나 개별 initiallyActive=false인 멤버는 전투 참여 전이므로 제외한다.
                    // 활성 멤버는 IsAlive까지 확인해 Unregister 누락에도 전멸 판정이 자가 치유되게 한다.
                    if (m != null && m.gameObject.activeInHierarchy && m.IsAlive()) count++;
                return count;
            }
        }

        public bool IsInBreatherWindow => Time.time < _groupBreatherUntil;
        public bool IsInPlayerBreatherWindow => Time.time < _playerBreatherUntil;
        public float BreatherRemainingTime => Mathf.Max(
            Mathf.Max(0f, _groupBreatherUntil - Time.time),
            Mathf.Max(0f, _playerBreatherUntil - Time.time));

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_members.Count == 0) return;
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
            foreach (var m in _members)
            {
                if (m == null) continue;
                Gizmos.DrawLine(transform.position, m.transform.position);
            }

            if (!Application.isPlaying || _formationOwners.Count == 0)
                return;

            Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.85f);
            foreach (var kv in _formationOwners)
            {
                var owner = kv.Value;
                if (owner == null || owner.Detection == null || !owner.Detection.HasTarget)
                    continue;

                var target = owner.Detection.CurrentTarget;
                var radius = kv.Key.Ring == FormationRing.Ranged
                    ? Mathf.Max(
                        owner.GroundAIController != null ? owner.GroundAIController.RetreatDistance : 3f,
                        GetMemberOptimalDistance(owner))
                    : owner.GroundAIController != null ? owner.GroundAIController.RetreatDistance : 3f;
                var slotPosition = GetFormationSlotPosition(
                    kv.Key.Index,
                    target.position,
                    target.forward,
                    radius);

                Gizmos.color = kv.Key.Ring == FormationRing.Ranged
                    ? new Color(0.8f, 0.4f, 1f, 0.85f)
                    : new Color(0.1f, 0.8f, 1f, 0.85f);
                Gizmos.DrawWireSphere(slotPosition, 0.25f);
                Gizmos.DrawLine(owner.transform.position, slotPosition);
            }
        }
#endif
    }
}
