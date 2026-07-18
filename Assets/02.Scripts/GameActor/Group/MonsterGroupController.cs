using System;
using System.Collections.Generic;
using UnityEngine;
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

    /// <summary>
    /// 몬스터 그룹 전체를 조율하는 컨트롤러.
    /// 씬의 MonsterGroup GameObject에 배치한다.
    ///
    /// 핵심 역할
    ///   1. Attack Slot 관리 - 근접/원거리 슬롯 분리
    ///   2. 우선순위 기반 슬롯 경쟁 (Summoner > Normal > Summon)
    ///   3. 경보 전파 - 한 명이 발견하면 그룹 전체 각성
    /// </summary>
    public class MonsterGroupController : MonoBehaviour
    {
        [Header("Attack Slots")]
        [Tooltip("동시에 근접 공격 가능한 최대 인원")]
        [SerializeField] private int _maxMeleeAttackers  = 2;
        [Tooltip("동시에 원거리 공격 가능한 최대 인원")]
        [SerializeField] private int _maxRangedAttackers = 2;

        [Header("Tempo")]
        [Tooltip("멤버 공격 종료 후 그룹 전체가 새 공격 슬롯을 잡지 못하는 시간(초)")]
        [SerializeField] private float _breatherDuration = 0.6f;

        [Header("Aggro Fitness")]
        [Tooltip("슬롯이 가득 찼을 때 더 적합한 멤버가 기존 점유자를 밀어내기 위해 필요한 최소 점수 차이")]
        [SerializeField] private float _aggroFitnessTakeoverMargin = 0.08f;
        [Tooltip("포화된 공격 슬롯 후보를 다시 평가하는 간격(초)")]
        [SerializeField] private float _aggroDecisionInterval = 0.1f;

        [Header("Formation")]
        [Tooltip("플레이어 주변을 나누는 공간 슬롯 개수")]
        [SerializeField] private int _formationSlotCount = 8;

        // 슬롯 점유 추적 — (actor, priority) 쌍으로 저장
        private readonly Dictionary<MonsterActor, MemberPriority> _meleeSlotOwners  = new();
        private readonly Dictionary<MonsterActor, MemberPriority> _rangedSlotOwners = new();
        private readonly Dictionary<MonsterActor, SlotCandidate> _meleeSlotCandidates = new();
        private readonly Dictionary<MonsterActor, SlotCandidate> _rangedSlotCandidates = new();
        private readonly Dictionary<int, MonsterActor> _formationOwners = new();
        private readonly Dictionary<MonsterActor, int> _formationSlots = new();

        // 멤버 레지스트리
        private readonly List<MonsterActor>                        _members    = new();
        private readonly Dictionary<MonsterActor, MemberPriority>  _priorities = new();
        // 사망/무효 멤버 정리를 위한 공용 스크래치. CleanupDeadSlotOwners/CleanupDeadCandidates 양쪽이 사용.
        private readonly List<MonsterActor>                        _cleanupScratch = new();
        private readonly List<int>                                 _deadFormationSlots = new();

        private bool _isActivated = false;
        private float _groupBreatherUntil = -999f;
        private float _nextMeleeAggroDecisionTime = -999f;
        private float _nextRangedAggroDecisionTime = -999f;
        private MonsterGroupMemory _memory;

        public MonsterGroupMemory Memory => _memory;

    /// <summary>
    /// 그룹 내 모든 멤버가 사망했을 때 1회 발동.
    /// TriggerComposer 등 외부에서 구독해 후속 처리를 연결한다.
    /// </summary>
    public event Action OnGroupDefeated;

        #region 초기화

        private void Start()
        {
            _memory ??= GetComponent<MonsterGroupMemory>();
            _memory ??= gameObject.AddComponent<MonsterGroupMemory>();

            // Awake 타이밍에 호출하면 MonsterActor.AIController가 아직 null일 수 있음
            // Start에서 수집하면 모든 컴포넌트 Awake 완료 후 보장됨
            var actors = GetComponentsInChildren<MonsterActor>(includeInactive: true);
            foreach (var actor in actors)
                RegisterMember(actor, MemberPriority.Normal);

            if (actors.Length != 0)
                _isActivated = true;
        }

        public void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;

            foreach (var member in _members)
                member?.gameObject.SetActive(true);
        }

        #endregion

        #region 멤버 등록/해제

        public void RegisterMember(MonsterActor actor, MemberPriority priority)
        {
            if (actor == null || _priorities.ContainsKey(actor)) return;
            if (actor.AIController == null)
            {
                Debug.LogWarning($"[MonsterGroupController] {actor.name}의 AIController가 null입니다. 등록 건너뜀.");
                return;
            }

            _members.Add(actor);
            _priorities[actor] = priority;
            actor.AIController.SetGroup(this, priority);
        }

        public void UnregisterMember(MonsterActor actor)
        {
            if (actor == null) return;

            _members.Remove(actor);
            _priorities.Remove(actor);
            _meleeSlotCandidates.Remove(actor);
            _rangedSlotCandidates.Remove(actor);
            ReleaseAttackSlot(actor);
            ReleaseFormationSlot(actor);

            // 전멸 감지 — 활성화된 그룹이 전부 사망했을 때만 발동
            if (_isActivated && AliveCount == 0)
                OnGroupDefeated?.Invoke();
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
            int maxSlots   = attackType == AttackType.Melee ? _maxMeleeAttackers : _maxRangedAttackers;

            if (IsInBreatherWindow && !slotOwners.ContainsKey(requester))
                return false;

            CleanupDeadSlotOwners(slotOwners);
            CleanupDeadCandidates(candidates);

            if (slotOwners.ContainsKey(requester))
                return true;

            if (slotOwners.Count < maxSlots)
                return TryOccupySlot(requester, priority, slotOwners, maxSlots);

            if (!candidates.ContainsKey(requester))
                candidates[requester] = new SlotCandidate(priority, Time.time);

            if (attackType == AttackType.Melee)
                ProcessAggroCandidates(candidates, slotOwners, maxSlots, ref _nextMeleeAggroDecisionTime);
            else
                ProcessAggroCandidates(candidates, slotOwners, maxSlots, ref _nextRangedAggroDecisionTime);

            return slotOwners.ContainsKey(requester);
        }

        public void ReleaseAttackSlot(MonsterActor releaser)
        {
            _meleeSlotOwners.Remove(releaser);
            _rangedSlotOwners.Remove(releaser);
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
            var slot = RequestFormationSlot(member, targetPosition, targetForward);
            if (slot < 0)
                return false;

            position = GetFormationSlotPosition(slot, targetPosition, targetForward, radius);
            return true;
        }

        public int RequestFormationSlot(MonsterActor member, Vector3 targetPosition, Vector3 targetForward)
        {
            if (_formationSlots.TryGetValue(member, out var existingSlot)
                && IsFormationOwner(existingSlot, member))
                return existingSlot;

            ReleaseFormationSlot(member);

            var desiredSlot = ComputeDesiredFormationSlot(member.transform.position, targetPosition, targetForward);
            var slot = FindAvailableFormationSlot(desiredSlot);
            if (slot < 0)
                return -1;

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

            ReleaseAttackSlot(member);

            if (_breatherDuration > 0f && _priorities.ContainsKey(member))
                _groupBreatherUntil = Mathf.Max(_groupBreatherUntil, Time.time + _breatherDuration);
        }

        public GroupIntentBias GetIntentBias(MonsterActor member, AttackType attackType)
        {
            if (member == null || !_priorities.ContainsKey(member))
                return GroupIntentBias.Neutral;

            CleanupDeadSlotOwners(_meleeSlotOwners);
            CleanupDeadSlotOwners(_rangedSlotOwners);

            var slotOwners = attackType == AttackType.Melee ? _meleeSlotOwners : _rangedSlotOwners;
            var maxSlots = attackType == AttackType.Melee ? _maxMeleeAttackers : _maxRangedAttackers;
            var ownsSlot = slotOwners.ContainsKey(member);
            var slotsFull = slotOwners.Count >= maxSlots;

            var attackMultiplier = 1f;
            var punishMultiplier = 1f;
            var counterMultiplier = 1f;
            var pressureBonus = 0f;
            var keepDistanceBonus = 0f;
            var retreatBonus = 0f;

            if (IsInBreatherWindow && !ownsSlot)
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

            var formationSlotIndex = _formationSlots.TryGetValue(member, out var formationSlot) ? formationSlot : -1;
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

        private void CleanupDeadSlotOwners(Dictionary<MonsterActor, MemberPriority> slotOwners)
        {
            _cleanupScratch.Clear();
            foreach (var kv in slotOwners)
                if (kv.Key == null || !kv.Key.IsAlive()) _cleanupScratch.Add(kv.Key);
            foreach (var d in _cleanupScratch) slotOwners.Remove(d);
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
            int maxSlots)
        {
            // 이미 점유 중 → 재요청 허용
            if (slotOwners.ContainsKey(requester)) return true;

            // 사망자 정리
            CleanupDeadSlotOwners(slotOwners);

            // 빈 슬롯 있으면 바로 점유
            if (slotOwners.Count < maxSlots)
            {
                slotOwners[requester] = requesterPriority;
                return true;
            }

            // 슬롯이 꽉 찼을 때 — 자신보다 낮은 우선순위 점유자 탐색
            MonsterActor lowestActor    = null;
            MemberPriority lowestPriority = requesterPriority; // 자신보다 낮아야 함

            foreach (var kv in slotOwners)
            {
                if (IsAttackSlotOwnerLocked(kv.Key))
                    continue;

                if (kv.Value < lowestPriority)
                {
                    lowestPriority = kv.Value;
                    lowestActor    = kv.Key;
                }
            }

            if (lowestActor == null
                && !TryFindFitnessTakeoverTarget(requester, requesterPriority, slotOwners, out lowestActor))
                return false; // 밀어낼 대상 없음 → 거절

            // 밀어내기: 낮은 우선순위 점유자가 슬롯을 잃음.
            // 해당 몬스터는 다음 BT 판단 주기에 슬롯 재요청 또는 CircleState 대기.
            slotOwners.Remove(lowestActor);
            slotOwners[requester] = requesterPriority;
            return true;
        }

        private void ProcessAggroCandidates(
            Dictionary<MonsterActor, SlotCandidate> candidates,
            Dictionary<MonsterActor, MemberPriority> slotOwners,
            int maxSlots,
            ref float nextDecisionTime)
        {
            if (Time.time < nextDecisionTime)
                return;

            nextDecisionTime = Time.time + Mathf.Max(0.01f, _aggroDecisionInterval);
            CleanupDeadSlotOwners(slotOwners);
            CleanupDeadCandidates(candidates);

            if (candidates.Count == 0)
                return;

            if (slotOwners.Count < maxSlots)
            {
                while (slotOwners.Count < maxSlots && TryGetBestAggroCandidate(candidates, out var openRequester, out var openCandidate))
                {
                    candidates.Remove(openRequester);
                    slotOwners[openRequester] = openCandidate.Priority;
                }
                return;
            }

            if (!TryGetBestReplacementCandidate(candidates, slotOwners, out var requester, out var candidate, out var target))
                return;

            slotOwners.Remove(target);
            slotOwners[requester] = candidate.Priority;
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
                if (!TryFindPriorityTakeoverTarget(kv.Value.Priority, slotOwners, out var target)
                    && !TryFindFitnessTakeoverTarget(kv.Key, kv.Value.Priority, slotOwners, out target))
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
            out MonsterActor takeoverTarget)
        {
            takeoverTarget = null;
            var lowestPriority = requesterPriority;

            foreach (var kv in slotOwners)
            {
                if (IsAttackSlotOwnerLocked(kv.Key))
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
                if (kv.Key == null || kv.Value > requesterPriority || IsAttackSlotOwnerLocked(kv.Key))
                    continue;

                var ownerFitness = ComputeAggroFitness(kv.Key, target);
                if (ownerFitness < lowestFitness)
                {
                    lowestFitness = ownerFitness;
                    takeoverTarget = kv.Key;
                }
            }

            return takeoverTarget != null
                   && requesterFitness > lowestFitness + Mathf.Max(0f, _aggroFitnessTakeoverMargin);
        }

        private static bool IsAttackSlotOwnerLocked(MonsterActor owner)
        {
            return owner != null
                   && owner.ActorController != null
                   && owner.ActorController.CurrentState?.StateName == EnemyAttackState.StateNameValue;
        }

        private static float ComputeAggroFitness(MonsterActor member, Transform target)
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

            return distanceScore * 0.5f + frontScore * 0.3f + hpScore * 0.2f;
        }

        private static float ComputeCandidateScore(MonsterActor member, SlotCandidate candidate, Transform target)
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

        private int FindAvailableFormationSlot(int desiredSlot)
        {
            var count = Mathf.Max(1, _formationSlotCount);
            if (!_formationOwners.ContainsKey(desiredSlot))
                return desiredSlot;

            for (var offset = 1; offset < count; offset++)
            {
                var clockwise = Mod(desiredSlot + offset, count);
                if (!_formationOwners.ContainsKey(clockwise))
                    return clockwise;

                var counterClockwise = Mod(desiredSlot - offset, count);
                if (!_formationOwners.ContainsKey(counterClockwise))
                    return counterClockwise;
            }

            return -1;
        }

        private bool IsFormationOwner(int slot, MonsterActor member)
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

        public void AlertGroup(Transform target)
        {
            foreach (var member in _members)
            {
                if (member == null || !member.IsAlive()) continue;
                if (member.Detection.HasTarget) continue;
                member.Detection.AcquireTarget(target);
            }
        }

        #endregion

        #region 상태 질의

        public int AliveCount
        {
            get
            {
                int count = 0;
                foreach (var m in _members)
                    if (m != null && m.IsAlive()) count++;
                return count;
            }
        }

        public bool IsInBreatherWindow => Time.time < _groupBreatherUntil;
        public float BreatherRemainingTime => Mathf.Max(0f, _groupBreatherUntil - Time.time);

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
                var slotPosition = GetFormationSlotPosition(
                    kv.Key,
                    target.position,
                    target.forward,
                    owner.GroundAIController != null ? owner.GroundAIController.RetreatDistance : 3f);

                Gizmos.DrawWireSphere(slotPosition, 0.25f);
                Gizmos.DrawLine(owner.transform.position, slotPosition);
            }
        }
#endif
    }
}
