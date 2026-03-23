using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 공중 공격 State.
    /// 기존 EnemyAttackState와 동일한 흐름이지만
    /// 공중 물리(GroundSolving OFF)를 유지한다.
    /// 완료 시 EnemyAerialState로 복귀.
    /// </summary>
    public class EnemyAerialAttackState : GameActorState
    {
        public override string StateName => "AerialAttack";

        private readonly AerialBehaviorLayer _aerialLayer;
        private readonly EnemyCombat         _combat;
        private readonly EnemyDetection      _detection;
        private readonly EnemyAttackInfo     _skill;

        private bool _done;

        public EnemyAerialAttackState(ActorMovementController controller,
            AerialBehaviorLayer aerialLayer, EnemyCombat combat,
            EnemyDetection detection, EnemyAttackInfo skill)
            : base(controller)
        {
            _aerialLayer = aerialLayer;
            _combat      = combat;
            _detection   = detection;
            _skill       = skill;
        }

        public override bool CanTransitionState(string stateName)
            => stateName is "Hit" or "Death";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _done = false;

            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(true);
            motor.SetGroundSolvingActivation(false);

            var anim = gameActor.Animator.PlayMotion(_skill.baseInfo.animKey, 0.1f);
            if (anim != null)
                gameActor.Animator.OnMotionSetCompleted += OnAttackEnd;
            else
                OnAttackEnd();
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            gameActor.Animator.OnMotionSetCompleted -= OnAttackEnd;
            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(false);
            _combat.ClearHitTargets();
            _aerialLayer.OnAerialAttackEnd();
        }

        public override void UpdateState(float deltaTime)
        {
            if (_done)
            {
                controller.TransitionToState(new EnemyAerialState(controller, _aerialLayer));
                return;
            }

            if (_combat.IsPossibleCollide)
                _combat.CheckMeleeAttackHit();
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 공격 중 제자리 — 수직 드리프트 방지
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, deltaTime * 8f);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_detection.HasTarget) return;
            Vector3 dir = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            currentRotation = Quaternion.Slerp(currentRotation,
                Quaternion.LookRotation(dir),
                1f - Mathf.Exp(-10f * deltaTime));
        }

        private void OnAttackEnd() => _done = true;
    }
}
