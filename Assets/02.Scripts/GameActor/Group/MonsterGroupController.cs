using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Group
{
    /// <summary>
    /// 그룹 내 멤버 우선순위.
    /// 슬롯 경쟁 시 Summoner > Normal > Summon 순으로 밀려난다.
    /// </summary>
    public enum MemberPriority { Summon, Normal, Summoner }

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

        // 슬롯 점유 추적 — (actor, priority) 쌍으로 저장
        private readonly Dictionary<MonsterActor, MemberPriority> _meleeSlotOwners  = new();
        private readonly Dictionary<MonsterActor, MemberPriority> _rangedSlotOwners = new();

        // 멤버 레지스트리
        private readonly List<MonsterActor>                        _members    = new();
        private readonly Dictionary<MonsterActor, MemberPriority>  _priorities = new();
        private readonly List<MonsterActor>                        _deadSlotOwners = new();

        private bool _isActivated = false;

    /// <summary>
    /// 그룹 내 모든 멤버가 사망했을 때 1회 발동.
    /// GroupStoryTrigger 등 외부에서 구독해 후속 처리를 연결한다.
    /// </summary>
    public event Action OnGroupDefeated;

        #region 초기화

        private void Start()
        {
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
            ReleaseAttackSlot(actor);

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
            int maxSlots   = attackType == AttackType.Melee ? _maxMeleeAttackers : _maxRangedAttackers;

            return TryOccupySlot(requester, priority, slotOwners, maxSlots);
        }

        public void ReleaseAttackSlot(MonsterActor releaser)
        {
            _meleeSlotOwners.Remove(releaser);
            _rangedSlotOwners.Remove(releaser);
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
            _deadSlotOwners.Clear();
            foreach (var kv in slotOwners)
                if (kv.Key == null || !kv.Key.IsAlive()) _deadSlotOwners.Add(kv.Key);
            foreach (var d in _deadSlotOwners) slotOwners.Remove(d);

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
                if (kv.Value < lowestPriority)
                {
                    lowestPriority = kv.Value;
                    lowestActor    = kv.Key;
                }
            }

            if (lowestActor == null) return false; // 밀어낼 대상 없음 → 거절

            // 밀어내기: 낮은 우선순위 점유자가 슬롯을 잃음
            // 해당 몬스터는 다음 Brain 판단 주기(0.1s)에 슬롯 재요청 or CircleState 대기
            slotOwners.Remove(lowestActor);
            slotOwners[requester] = requesterPriority;
            return true;
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
        }
#endif
    }
}
