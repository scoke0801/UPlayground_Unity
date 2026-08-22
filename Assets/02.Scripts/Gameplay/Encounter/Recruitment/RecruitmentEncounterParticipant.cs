using System;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Story;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Encounter
{
    /// <summary>영입 조우 참가자의 안정 ID, 전투 역할, 임시 진영과 영입 대상 생존 정책을 소유한다.</summary>
    public sealed class RecruitmentEncounterParticipant : MonoBehaviour, IMonsterFatalDamagePolicy
    {
        [SerializeField] private string _participantId;
        [SerializeField] private RecruitmentEncounterRole _role;
        [SerializeField] private MonsterActor _actor;

        private IRecruitmentEncounterService _service;
        private string _encounterId;
        private IDisposable _factionLease;
        private IDisposable _fatalDamageLease;
        private IDisposable _combatExclusionLease;
        private IDisposable _deathRemainsLease;
        private IDisposable _aggroLockLease;
        private RecruitmentIncapacitationRule _incapacitationRule;
        private bool _isIncapacitated;
        private bool _isBound;

        public string ParticipantId => _participantId;
        public RecruitmentEncounterRole Role => _role;
        public MonsterActor Actor => _actor;
        public bool IsIncapacitated => _isIncapacitated;

        private void Awake()
        {
            _actor ??= GetComponent<MonsterActor>();
        }

        private void OnValidate()
        {
            _participantId = _participantId?.Trim();
            _actor ??= GetComponent<MonsterActor>();
        }

        public bool Bind(
            IRecruitmentEncounterService service,
            string encounterId,
            RecruitmentIncapacitationRule incapacitationRule)
        {
            if (_isBound
                || service == null
                || _actor == null
                || string.IsNullOrWhiteSpace(_participantId)
                || string.IsNullOrWhiteSpace(encounterId))
            {
                return false;
            }

            _service = service;
            _encounterId = encounterId;
            _incapacitationRule = incapacitationRule;
            _actor.OnKilled += HandleActorKilled;
            if (IsRecruitActor)
            {
                _actor.SuppressRuntimePartyRecruitment();
                _fatalDamageLease = _actor.OverrideFatalDamagePolicy(this);
            }

            _isBound = true;
            return true;
        }

        public void Unbind()
        {
            if (_actor != null)
                _actor.OnKilled -= HandleActorKilled;
            ReleaseAggroLock();
            ReleaseDeathRemains();
            _factionLease?.Dispose();
            _factionLease = null;
            _fatalDamageLease?.Dispose();
            _fatalDamageLease = null;
            ReleaseCombatExclusion();
            _service = null;
            _encounterId = null;
            _isBound = false;
        }

        public bool ActivateCombat(CombatFactionSO allyFaction, IAggroLockSource aggroLock)
        {
            if (_actor == null)
                return false;

            IDisposable nextFactionLease = null;
            if (_role == RecruitmentEncounterRole.RequiredAlly)
            {
                if (allyFaction == null
                    || !Services.TryGet<ICombatRelationService>(out var relations))
                {
                    return false;
                }

                nextFactionLease = relations.OverrideAffiliation(
                    _actor,
                    allyFaction,
                    CombatCreditOwner.PlayerParty);
                if (nextFactionLease == null)
                    return false;

                IDisposable previousFactionLease = _factionLease;
                _factionLease = nextFactionLease;
                previousFactionLease?.Dispose();
            }

            ReleaseCombatExclusion();
            gameObject.SetActive(true);
            _actor.Detection?.ForceResetTarget();
            _actor.RestoreEncounterCombatState();
            _isIncapacitated = false;
            SetCombatComponentsEnabled(true);
            HoldAggroLock(aggroLock);
            return true;
        }

        /// <summary>
        /// 조우 전투 동안 어그로 해제를 막는다.
        /// 아군과 적은 씬 저작 위치·엄폐물 때문에 일반 이탈 규칙(거리·시야·앵커)에 쉽게 걸리는데,
        /// 그러면 연출 도중 서로를 놓고 멈춰 서서 조우가 성립하지 않는다.
        /// </summary>
        private void HoldAggroLock(IAggroLockSource aggroLock)
        {
            ReleaseAggroLock();
            if (aggroLock == null || _actor.Detection == null)
                return;

            _aggroLockLease = _actor.Detection.HoldAggroLock(aggroLock);
        }

        private void ReleaseAggroLock()
        {
            _aggroLockLease?.Dispose();
            _aggroLockLease = null;
        }

        /// <summary>진입 전에 참가자를 보여주되 락온·피해·AI에서 제외해 대치 장면으로 세운다.</summary>
        public void PrepareDormantPresentation()
        {
            if (_actor == null)
                return;

            ReleaseAggroLock();
            gameObject.SetActive(true);
            HoldCombatExclusion();
            _actor.RestoreEncounterCombatState();
            _actor.SetInvincible(true);
            SetCombatComponentsEnabled(false);
            _actor.StopStageApproach();
        }

        public void SetDormantOrHidden()
        {
            ReleaseAggroLock();
            ReleaseDeathRemains();
            if (_actor != null)
            {
                _actor.Detection?.ForceResetTarget();
                _actor.Abilities?.CancelAllAbilities();
                _actor.StopStageApproach();
            }

            // 사망한 참가자는 비활성화하지 않는다. 디졸브 진행이 멈춰 시체 오브젝트가 씬에 남는다.
            if (_actor == null || _actor.IsAlive())
                gameObject.SetActive(false);

            ReleaseCombatExclusion();
            _factionLease?.Dispose();
            _factionLease = null;
        }

        public void PrepareDialogue()
        {
            if (_actor == null || !IsRecruitActor)
                return;

            ReleaseAggroLock();
            gameObject.SetActive(true);
            HoldCombatExclusion();
            _actor.RestoreEncounterCombatState();
            _actor.SetInvincible(true);
            _isIncapacitated = false;
            SetCombatComponentsEnabled(false);
        }

        public MonsterFatalDamageResolution ResolveFatalDamage(
            MonsterActor victim,
            in HitRequest request,
            float requestedDamage,
            out float appliedDamage)
        {
            appliedDamage = requestedDamage;
            if (!IsRecruitActor
                || victim == null
                || victim != _actor
                || _isIncapacitated)
            {
                return MonsterFatalDamageResolution.Unhandled;
            }

            appliedDamage = Mathf.Max(0f, victim.CurrentHealth - 1f);
            if (!RecruitmentIncapacitationRuleEvaluator.IsSatisfied(
                    _incapacitationRule,
                    request.AttackKind,
                    request.IsSpecialBreak))
            {
                if (_incapacitationRule == RecruitmentIncapacitationRule.FinishAttack
                    && victim.TryExposeForFinishAttack())
                {
                    return MonsterFatalDamageResolution.Prevented;
                }

                // 잘못 저작된 대상 때문에 진행이 막히는 것보다 기존 치명 피해 제압으로 안전하게 수렴한다.
            }

            Incapacitate(victim);
            return MonsterFatalDamageResolution.Incapacitated;
        }

        private void Incapacitate(MonsterActor victim)
        {
            _isIncapacitated = true;
            ReleaseAggroLock();
            victim.SetInvincible(true);
            victim.Detection?.ForceResetTarget();
            victim.Abilities?.CancelAllAbilities();
            HoldCombatExclusion();
            SetCombatComponentsEnabled(false);
            victim.EnterEncounterIncapacitatedState();
            if (_role == RecruitmentEncounterRole.RecruitTarget)
                _service?.RecordHostileDefeated(_encounterId, _participantId);
        }

        private void HandleActorKilled(MonsterActor actor, CombatKillContext context)
        {
            if (_role != RecruitmentEncounterRole.Hostile)
                return;

            // 조우는 전투가 끝나면 곧바로 대화 연출로 이어진다. 시체가 먼저 사라지면
            // 방금 쓰러뜨린 상대가 없는 자리에서 대화가 시작돼 전투와 연출이 끊겨 보인다.
            HoldDeathRemains(actor);
            _service?.RecordHostileDefeated(_encounterId, _participantId);
        }

        private void HoldDeathRemains(MonsterActor actor)
        {
            if (_deathRemainsLease != null || actor == null)
                return;

            _deathRemainsLease = actor.HoldDeathRemains();
        }

        /// <summary>시체 잔존 홀드를 놓아 디졸브 정리를 시작시킨다.</summary>
        private void ReleaseDeathRemains()
        {
            _deathRemainsLease?.Dispose();
            _deathRemainsLease = null;
        }

        private void SetCombatComponentsEnabled(bool enabled)
        {
            if (_actor != null)
                _actor.SetCombatComponentsEnabled(enabled);
        }

        private void HoldCombatExclusion()
        {
            if (_combatExclusionLease == null && _actor != null)
                _combatExclusionLease = _actor.ExcludeFromCombat();
        }

        private void ReleaseCombatExclusion()
        {
            _combatExclusionLease?.Dispose();
            _combatExclusionLease = null;
        }

        private bool IsRecruitActor =>
            _role is RecruitmentEncounterRole.RequiredAlly
                or RecruitmentEncounterRole.RecruitTarget;
    }
}
