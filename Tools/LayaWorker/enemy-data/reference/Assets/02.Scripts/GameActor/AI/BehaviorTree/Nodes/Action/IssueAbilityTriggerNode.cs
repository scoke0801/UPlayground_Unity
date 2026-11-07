using UPlayGround.Ability.Core;
using UPlayGround.Components;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Ability;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 예약된 몬스터 공격 슬롯을 카테고리 GameplayEvent로 실행한다.
    /// 상태명이 아니라 이 요청으로 수락된 실행 핸들만 추적한다.
    /// </summary>
    public class IssueAbilityTriggerNode : BTActionNode
    {
        [SerializeField] private AbilityAttackCategory _attackCategory =
            AbilityAttackCategory.None;
        [SerializeField] private AbilityAIRole _abilityRole =
            AbilityAIRole.None;

        private readonly EnemyAttackTriggerRequest _request = new();

        public AbilityAttackCategory AttackCategory
        {
            get => _attackCategory;
            set => _attackCategory = value;
        }

        public AbilityAIRole AbilityRole
        {
            get => _abilityRole;
            set => _abilityRole = value;
        }

        protected override void OnStart() => _request.Reset();

        protected override BTStatus OnUpdate()
        {
            EnemyCombat combat = Context?.GetComponentCached<EnemyCombat>();
            EnemyAIContext aiContext = Context?.GetComponentCached<EnemyAIContext>();
            if (combat == null || aiContext == null)
                return BTStatus.Failure;

            if (!_request.TriggerIssued)
            {
                combat.ReserveAttackSelection(_attackCategory, _abilityRole);
                aiContext.NotifyBTAttackStarted();
                _request.Issue(combat, aiContext, _attackCategory);
            }

            BTStatus status = _request.Update();
            if (status == BTStatus.Failure)
                Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, false);
            return status;
        }

        protected override void OnStop() => _request.Stop();
    }

    /// <summary>두 공격 개시 노드가 공유하는 트리거 발급/수락/거부 수명주기.</summary>
    internal sealed class EnemyAttackTriggerRequest
    {
        private EnemyCombat _combat;
        private ActorAbilitySystem _abilitySystem;
        private EnemyAIContext _aiContext;
        private GameplayAbilitySO _triggerAbility;
        private AbilityAttackCategory _category;
        private AbilityExecutionHandle _acceptedHandle;
        private AbilityActivationResult? _rejection;
        private bool _acceptedSignalPending;
        private bool _slotReleased;

        public bool TriggerIssued { get; private set; }
        public int TriggerFrame { get; private set; } = -1;

        public void Issue(
            EnemyCombat combat,
            EnemyAIContext aiContext,
            AbilityAttackCategory category)
        {
            if (TriggerIssued)
                return;

            TriggerIssued = true;
            TriggerFrame = Time.frameCount;
            _combat = combat;
            _aiContext = aiContext;
            _abilitySystem = combat?.AbilitySystem;

            if (_abilitySystem == null
                || !EnemyAbilityTriggerTags.TryResolveAttackTrigger(
                    combat?.AbilitySet,
                    category,
                    out _category,
                    out _triggerAbility,
                    out var tag))
            {
                Reject(AbilityActivationResult.NotGranted);
                return;
            }

            _abilitySystem.AbilityTriggerAccepted += OnTriggerAccepted;
            _abilitySystem.AbilityTriggerRejected += OnTriggerRejected;
            _abilitySystem.IssueTriggerEvent(tag);
        }

        public BTStatus Update()
        {
            if (_rejection.HasValue)
                return BTStatus.Failure;

            if (_acceptedHandle.IsValid)
                return _abilitySystem != null
                       && _abilitySystem.IsExecutionActive(_acceptedHandle)
                    ? BTStatus.Running
                    : BTStatus.Success;

            if (TriggerIssued && Time.frameCount > TriggerFrame + 1)
            {
                Reject(AbilityActivationResult.StateTransitionRejected);
                return BTStatus.Failure;
            }

            return BTStatus.Running;
        }

        public bool ConsumeAcceptedSignal()
        {
            if (!_acceptedSignalPending)
                return false;
            _acceptedSignalPending = false;
            return true;
        }

        public void Stop()
        {
            Unsubscribe();
            if (TriggerIssued
                && !_acceptedHandle.IsValid
                && !_rejection.HasValue)
            {
                _combat?.ClearReservedAttackSelection();
                ReleaseSlot();
            }
        }

        public void Reset()
        {
            Stop();
            _combat = null;
            _abilitySystem = null;
            _aiContext = null;
            _triggerAbility = null;
            _category = AbilityAttackCategory.None;
            _acceptedHandle = default;
            _rejection = null;
            _acceptedSignalPending = false;
            _slotReleased = false;
            TriggerIssued = false;
            TriggerFrame = -1;
        }

        private void OnTriggerAccepted(
            AbilityTriggerRequest request,
            AbilityExecutionHandle handle)
        {
            if (request.Ability != _triggerAbility
                || !EnemyAbilityTriggerTags.TryGetAttackCategory(
                    request.TriggerTag,
                    out AbilityAttackCategory category)
                || category != _category)
                return;

            _acceptedHandle = handle;
            _acceptedSignalPending = true;
            Unsubscribe();
        }

        private void OnTriggerRejected(
            GameplayAbilitySO ability,
            AbilityActivationResult reason)
        {
            if (ability != _triggerAbility)
                return;
            Reject(reason);
        }

        private void Reject(AbilityActivationResult reason)
        {
            _rejection = reason;
            Unsubscribe();
            _combat?.ClearReservedAttackSelection();
            ReleaseSlot();
        }

        private void ReleaseSlot()
        {
            if (_slotReleased)
                return;
            _slotReleased = true;
            _aiContext?.ReleaseGroupSlot();
        }

        private void Unsubscribe()
        {
            if (_abilitySystem == null)
                return;
            _abilitySystem.AbilityTriggerAccepted -= OnTriggerAccepted;
            _abilitySystem.AbilityTriggerRejected -= OnTriggerRejected;
        }
    }
}
