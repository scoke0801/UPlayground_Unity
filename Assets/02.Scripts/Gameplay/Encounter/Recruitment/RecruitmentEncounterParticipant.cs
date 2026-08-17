using System;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Story;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Encounter
{
    /// <summary>영입 조우 참가자의 안정 ID, 역할, 임시 진영과 필수 아군 생존 정책을 소유한다.</summary>
    public sealed class RecruitmentEncounterParticipant : MonoBehaviour, IMonsterFatalDamagePolicy
    {
        [SerializeField] private string _participantId;
        [SerializeField] private RecruitmentEncounterRole _role;
        [SerializeField] private MonsterActor _actor;

        private IRecruitmentEncounterService _service;
        private string _encounterId;
        private IDisposable _factionLease;
        private IDisposable _fatalDamageLease;
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

        public bool Bind(IRecruitmentEncounterService service, string encounterId)
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
            _actor.OnKilled += HandleActorKilled;
            if (_role == RecruitmentEncounterRole.RequiredAlly)
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
            _factionLease?.Dispose();
            _factionLease = null;
            _fatalDamageLease?.Dispose();
            _fatalDamageLease = null;
            _service = null;
            _encounterId = null;
            _isBound = false;
        }

        public bool ActivateCombat(CombatFactionSO allyFaction)
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

            gameObject.SetActive(true);
            _actor.Detection?.ForceResetTarget();
            _actor.RestoreEncounterCombatState();
            _isIncapacitated = false;
            SetCombatComponentsEnabled(true);
            return true;
        }

        public void SetDormantOrHidden()
        {
            if (_actor != null)
            {
                _actor.Detection?.ForceResetTarget();
                _actor.Abilities?.CancelAllAbilities();
            }
            gameObject.SetActive(false);
            _factionLease?.Dispose();
            _factionLease = null;
        }

        public void PrepareDialogue()
        {
            if (_actor == null || _role != RecruitmentEncounterRole.RequiredAlly)
                return;

            gameObject.SetActive(true);
            _actor.RestoreEncounterCombatState();
            _actor.SetInvincible(true);
            _isIncapacitated = false;
            SetCombatComponentsEnabled(false);
        }

        public bool TryResolveFatalDamage(
            MonsterActor victim,
            in HitRequest request,
            float requestedDamage,
            out float appliedDamage)
        {
            appliedDamage = requestedDamage;
            if (_role != RecruitmentEncounterRole.RequiredAlly
                || victim == null
                || victim != _actor
                || _isIncapacitated)
            {
                return false;
            }

            appliedDamage = Mathf.Max(0f, victim.CurrentHealth - 1f);
            _isIncapacitated = true;
            victim.SetInvincible(true);
            victim.Detection?.ForceResetTarget();
            victim.Abilities?.CancelAllAbilities();
            SetCombatComponentsEnabled(false);
            return true;
        }

        private void HandleActorKilled(MonsterActor actor, CombatKillContext context)
        {
            if (_role == RecruitmentEncounterRole.Hostile)
                _service?.RecordHostileDefeated(_encounterId, _participantId);
        }

        private void SetCombatComponentsEnabled(bool enabled)
        {
            if (_actor == null)
                return;
            if (_actor.Detection != null)
                _actor.Detection.enabled = enabled;
            if (_actor.Combat != null)
                _actor.Combat.enabled = enabled;
            if (_actor.GroundAIController != null)
                _actor.GroundAIController.enabled = enabled;
            if (_actor.FlyingAIController != null)
                _actor.FlyingAIController.enabled = enabled;
        }
    }
}
