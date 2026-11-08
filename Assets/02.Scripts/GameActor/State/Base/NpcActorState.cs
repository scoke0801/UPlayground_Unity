using UPlayGround.MovementController;

using UnityEngine;

namespace UPlayGround.State
{
    /// <summary>
    /// NPC 전용 State 베이스.
    /// PlayerActorState / EnemyActorState 와 동일한 레이어 역할.
    /// NpcMovementController와 NpcActor 참조를 미리 캐싱합니다.
    /// </summary>
    public abstract class NpcActorState : GameActorState
    {
        protected NpcMovementController npcController;
        protected NpcActor npcActor;
        private bool _isGesturing;
        private float _gestureRemaining;

        protected NpcActorState(NpcMovementController controller) : base(controller)
        {
            npcController = controller;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            npcActor = gameActor as NpcActor;
            _isGesturing = false;
        }

        /// <summary>대기와 배회 휴식이 같은 생활 동작을 끝까지 재생하고 기본 대기로 복귀한다.</summary>
        protected bool TickLifeGesture(UPlayGround.Components.NpcBrain brain, float deltaTime)
        {
            if (_isGesturing)
            {
                _gestureRemaining -= deltaTime;
                if (_gestureRemaining > 0f) return true;
                _isGesturing = false;
                brain.SetLifeGesturePlaying(false);
                gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, 0.3f);
                return false;
            }

            if (!brain.TryTakeLifeGesture(out var motion)) return false;
            var state = gameActor.Animator.PlayMotion(motion, 0.25f);
            if (state == null) return false;
            _gestureRemaining = Mathf.Max(0.5f, motion.motionSet.TotalDuration);
            _isGesturing = true;
            brain.SetLifeGesturePlaying(true);
            return true;
        }
    }
}
