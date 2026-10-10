using System;
using UPlayGround.Manager;
using UPlayGround.State;

namespace UPlayGround
{
    /// <summary>공개 연출과 중단 후 재접근 동안 전투를 보류하는 액터 소유 홀드.</summary>
    public partial class MonsterActor : IStoryActorStaging
    {
        private int _storyCombatHolds;
        private bool _storyWasInvincible;
        private bool _storyDetectionEnabled;
        private bool _storyCombatEnabled;
        private bool _storyGroundAIEnabled;
        private bool _storyFlyingAIEnabled;
        private IDisposable _storyCombatExclusion;

        /// <summary>연출 준비 시 전투 상태를 보관하고 마지막 홀드 해제 시 복구한다.</summary>
        public IDisposable HoldStoryCombat()
        {
            if (_storyCombatHolds++ == 0)
            {
                _storyWasInvincible = _isInvincible;
                _storyDetectionEnabled = _detection != null && _detection.enabled;
                _storyCombatEnabled = _combat != null && _combat.enabled;
                _storyGroundAIEnabled = _groundAIController != null && _groundAIController.enabled;
                _storyFlyingAIEnabled = _flyingAIController != null && _flyingAIController.enabled;
                _storyCombatExclusion = ExcludeFromCombat();
                SetInvincible(true);
                Detection?.ForceResetTarget();
                Abilities?.CancelAllAbilities();
                SetCombatComponentsEnabled(false);
                MovementController?.TryTransitionToState(ActorStateId.Idle);
            }
            return new ActorRuntimeLease(ReleaseStoryCombat);
        }

        private void ReleaseStoryCombat()
        {
            _storyCombatHolds = Math.Max(0, _storyCombatHolds - 1);
            if (_storyCombatHolds > 0)
                return;
            _storyCombatExclusion?.Dispose();
            _storyCombatExclusion = null;
            if (this == null || !IsAlive())
                return;
            SetInvincible(_storyWasInvincible);
            if (_detection != null) _detection.enabled = _storyDetectionEnabled;
            if (_combat != null) _combat.enabled = _storyCombatEnabled;
            if (_groundAIController != null) _groundAIController.enabled = _storyGroundAIEnabled;
            if (_flyingAIController != null) _flyingAIController.enabled = _storyFlyingAIEnabled;
            SetBehaviorTreeRunning(IsAIControllerEnabled && !IsDialogueStaged);
        }
    }
}
