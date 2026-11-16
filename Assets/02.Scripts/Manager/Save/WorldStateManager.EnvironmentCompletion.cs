using System;
using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Data.Save;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.Manager
{
    public partial class WorldStateManager
    {
        private static bool ClearsEnvironment(EnvironmentBinding binding) => binding.definition != null && binding.definition.destruction != null
            && binding.definition.destruction.completionPolicy == EnvironmentCompletionPolicy.ClearAfterSettlement;

        private static bool HasEndedEnvironmentReaction(EnvironmentBinding binding) => ClearsEnvironment(binding)
            && binding.record.destruction.completion != null
            && binding.record.destruction.completion.phase >= EnvironmentCompletionPhase.Clearing;

        private static bool CanCommitEnvironmentReward(EnvironmentBinding binding) => !ClearsEnvironment(binding)
            || binding.record.destruction.completion?.phase >= EnvironmentCompletionPhase.Clearing;

        private static void PrepareEnvironmentCompletion(IEnvironmentReactionTarget target, ReactiveEnvironmentSaveEntry record, bool fresh)
        {
            var recipe = target.Definition.destruction;
            var pose = record.destruction;
            if (recipe == null || recipe.completionPolicy != EnvironmentCompletionPolicy.ClearAfterSettlement)
            {
                if (pose.completion != null) throw new ArgumentException("영속 잔해에 벌목 수명 기록이 있습니다.");
                return;
            }
            if (pose.completion == null)
            {
                if (pose.isFalling)
                    pose.direction = new SerializableVector3(Vector3.ProjectOnPlane(pose.direction.ToVector3(), Vector3.up).normalized);
                // 구버전 정착 잔해는 재연출하지 않고 기존 보상 원장만 이어받는다.
                pose.completion = new EnvironmentCompletionSaveState { version = 1,
                    phase = !record.state.isBroken ? EnvironmentCompletionPhase.Standing
                        : fresh || pose.isFalling ? EnvironmentCompletionPhase.Falling : EnvironmentCompletionPhase.Cleared,
                    reason = record.state.isBroken && !fresh && !pose.isFalling
                        ? EnvironmentSettlementReason.Completed : EnvironmentSettlementReason.None };
                if (pose.completion.phase == EnvironmentCompletionPhase.Cleared)
                {
                    pose.initialized = true;
                    FixEnvironmentRewardPosition(target, record.mapId, pose.completion);
                }
            }
            var state = pose.completion;
            Vector3 direction = pose.direction.ToVector3();
            bool validDirection = Mathf.Abs(direction.y) < 0.0001f && Mathf.Abs(direction.sqrMagnitude - 1) < 0.001f;
            float limit = state.phase == EnvironmentCompletionPhase.Falling ? (pose.usedFallback ? recipe.blockedCollapseSeconds : recipe.fallSeconds)
                : state.phase == EnvironmentCompletionPhase.Settled ? recipe.settlementSeconds
                : state.phase == EnvironmentCompletionPhase.Clearing ? recipe.clearingSeconds : 0;
            if (state.version != 1 || !Enum.IsDefined(typeof(EnvironmentCompletionPhase), state.phase)
                || !Enum.IsDefined(typeof(EnvironmentSettlementReason), state.reason)
                || !EnvironmentReaction.IsNonNegativeFinite(state.elapsed) || state.elapsed > limit
                || !IsFiniteDirection(state.rewardPosition.ToVector3())
                || record.state.isBroken != (state.phase != EnvironmentCompletionPhase.Standing)
                || state.phase == EnvironmentCompletionPhase.Falling && !pose.initialized
                || pose.usedFallback && (recipe.mode != EnvironmentDestructionMode.DirectionalFall
                    || pose.progress != 0 || pose.isFalling || direction.sqrMagnitude > 0.0001f)
                || pose.initialized && recipe.mode == EnvironmentDestructionMode.DirectionalFall
                    && !pose.usedFallback && state.phase != EnvironmentCompletionPhase.Cleared && !validDirection
                || recipe.mode == EnvironmentDestructionMode.Swap && (pose.isFalling || pose.progress != 0)
                || state.phase < EnvironmentCompletionPhase.Settled && (state.reason != EnvironmentSettlementReason.None || state.hasRewardPosition)
                || state.phase >= EnvironmentCompletionPhase.Settled && (state.reason == EnvironmentSettlementReason.None
                    || !state.hasRewardPosition || pose.isFalling || !pose.initialized)
                || pose.isFalling && (!pose.initialized || pose.usedFallback || pose.progress >= 1
                    || !validDirection)
                || !record.state.isBroken && (pose.initialized || pose.isFalling || pose.usedFallback || pose.progress != 0))
                throw new ArgumentException("저장된 벌목 수명 단계·시간·정착 포즈가 유효하지 않습니다.");
        }

        private static void FixEnvironmentRewardPosition(IEnvironmentReactionTarget target, string mapId, EnvironmentCompletionSaveState completion)
        {
            Vector3 position = target is ReactiveEnvironmentObject tree ? tree.RewardPosition : target.transform.position;
            completion.rewardPosition = new SerializableVector3(WorldCoordinates.ToSource(mapId, position));
            completion.hasRewardPosition = true;
        }

        private void ReleaseCompletedEnvironmentRain(EnvironmentBinding binding)
        {
            if (binding.target is ReactiveEnvironmentObject tree) tree.DetachRainPositions(_environmentRain);
            _environmentRainCapacity -= binding.rainSamples;
            binding.rainSamples = 0;
        }

        private void AdvanceEnvironmentCompletion(EnvironmentBinding binding, float seconds)
        {
            if (!ClearsEnvironment(binding)) return;
            var pose = binding.record.destruction;
            var state = pose.completion;
            var recipe = binding.definition.destruction;
            if (state == null || state.phase == EnvironmentCompletionPhase.Standing || state.phase == EnvironmentCompletionPhase.Cleared) return;
            state.elapsed += seconds;
            switch (state.phase)
            {
                case EnvironmentCompletionPhase.Falling:
                    if (!pose.initialized || pose.isFalling || pose.usedFallback && state.elapsed < recipe.blockedCollapseSeconds) return;
                    state.reason = pose.usedFallback ? EnvironmentSettlementReason.BlockedInitially
                        : pose.progress >= 1 || recipe.mode == EnvironmentDestructionMode.Swap
                            ? EnvironmentSettlementReason.Completed : EnvironmentSettlementReason.BlockedDuringFall;
                    FixEnvironmentRewardPosition(binding.target, binding.record.mapId, state);
                    state.phase = EnvironmentCompletionPhase.Settled;
                    break;
                case EnvironmentCompletionPhase.Settled:
                    if (state.elapsed < recipe.settlementSeconds) return;
                    state.phase = EnvironmentCompletionPhase.Clearing;
                    ReleaseCompletedEnvironmentRain(binding);
                    binding.reaction.RestorePendingInputs(0, 0);
                    CaptureEnvironment(binding);
                    // 보상 실패가 FX와 시각 정리의 진행을 막지 않는다.
                    CommitEnvironmentReward(binding);
                    break;
                case EnvironmentCompletionPhase.Clearing:
                    if (state.elapsed < recipe.clearingSeconds)
                    {
                        if (binding.target is ReactiveEnvironmentObject fading) fading.ApplyCompletion(pose, false);
                        return;
                    }
                    state.phase = EnvironmentCompletionPhase.Cleared;
                    break;
            }
            state.elapsed = 0;
            if (binding.target is ReactiveEnvironmentObject tree) tree.ApplyCompletion(pose, true);
            NotifyEnvironmentStructureChanged(binding.target, new Bounds(binding.target.transform.position, Vector3.one * recipe.trunkLength * 2));
            _environmentRewardsPending |= !binding.record.rewardCommitted && CanCommitEnvironmentReward(binding);
        }
    }
}
