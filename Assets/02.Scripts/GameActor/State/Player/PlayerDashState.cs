using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Combat;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.State
{
    /// <summary>
    /// 전방 직선 대시 상태
    /// - 대시 중 Enemy 레이어를 CollidableLayers에서 제거해 충돌 무시
    /// - 대시 종료 시 ComputePenetration으로 겹침 해소 후 레이어 복구
    /// </summary>
    public class PlayerDashState : PlayerActorState
    {
        public override ActorStateId StateId => ActorStateId.Dash;
        public override bool GrantsInvincibility => true;

        // 대시 회피 위협 스캔 기본값. CombatDefensePolicySO에서 오버라이드할 수 있다.
        private const float DefaultEvadeSearchRange     = 6f;
        private const float DefaultEvadeWindowBeforeHit = 0.25f;
        private const float DefaultEvadeGraceAfterHit   = 0.08f;
        private const float DefaultEvadeRadiusPadding   = 0.5f;

        private Vector3 _dashDirection;

        // 대시 1회당 회피 타임스케일 피드백을 한 번만 발동하기 위한 가드.
        // (단일 대시가 여러 히트 페이즈와 겹쳐도 중복 연출되지 않도록 함)
        private bool _evadeFeedbackFired;

        private readonly List<Collider> _ignoredOnDodge = new();
        private readonly List<EnemyMovementController> _enemyControllers = new();

        // 위협 스캔용 재사용 버퍼.
        // 대시 상태는 전환마다 new로 생성되므로 인스턴스 필드로 두면 대시마다 할당이 발생한다.
        // 플레이어는 동시에 하나만 대시하므로 static 공유로 충분하다.
        private static readonly Collider[] ThreatOverlapBuffer = new Collider[64];
        private static readonly HashSet<MonsterActor> EvaluatedThreatMonsters = new();

        // 위협 스캔은 매 프레임 돌므로 문자열 레이어 조회를 캐시한다.
        private static int _enemyLayerMask = -1;
        private static int EnemyLayerMask
        {
            get
            {
                if (_enemyLayerMask < 0)
                    _enemyLayerMask = LayerMask.GetMask("Enemy");
                return _enemyLayerMask;
            }
        }

        public PlayerDashState(ActorMovementController controller) : base(controller) { }

        /// <summary>
        /// 대시 중 적 공격을 회피했을 때 호출. 대시당 최초 1회만 true를 반환한다.
        /// </summary>
        public bool TryConsumeEvadeFeedback()
        {
            if (_evadeFeedbackFired) return false;
            _evadeFeedbackFired = true;
            return true;
        }

        // 상태 전환 제한
        public override bool CanTransitionState(ActorStateId fromState)
        {
            if (playerController != null && playerController.IsDashReady == false)
                return false;
            return playerActor?.Stamina?.CanDash != false;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            if (playerActor?.Stamina?.TrySpendDash() == false)
            {
                FinishDash();
                return;
            }
            gameActor.Tags?.AddTag(GameplayTags.State_Dash);
            playerActor?.ComboInputTracker.Push(ComboInputToken.Dash);
            playerController.StartDashCooldown();
            playerActor?.PrepareEvadeAfterimage();

            _dashDirection = playerController.HasMoveInput()
                ? playerController.MoveInputVector.normalized
                : motor.CharacterForward;

            IgnoreMonsterColliders();

            UPlayGround.Gameplay.Tag.GameplayTag dashKey = ResolveDirectionalMotionKey(
                UPlayGround.Data.Actor.Animation.MotionTags.Dash_F, UPlayGround.Data.Actor.Animation.MotionTags.Dash_B, UPlayGround.Data.Actor.Animation.MotionTags.Dash_L, UPlayGround.Data.Actor.Animation.MotionTags.Dash_R, UPlayGround.Data.Actor.Animation.MotionTags.Dash);
            var animState = gameActor.Animator.PlayMotion(dashKey, 0.1f);
            if (animState != null)
            {
                gameActor.Animator.OnMotionSetCompleted += OnAnimationEnd;
            }
            else
                FinishDash();
        }

        public override void OnExit(GameActorState toState)
        {
            playerActor?.CancelEvadeAfterimage();
            gameActor.Tags?.RemoveTag(GameplayTags.State_Dash);
            RestoreAndResolvePenetration();

            gameActor.Animator.OnMotionSetCompleted -= OnAnimationEnd;

            gameActor.MoveAnimType = playerActor?.Stamina?.CanStartSprint != false
                ? BaseMoveAnimType.Sprint
                : BaseMoveAnimType.Run;
            base.OnExit(toState);
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity = _dashDirection * controller.DashSpeed;
            currentVelocity.y = 0f;
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround &&
                Svc.Input.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
            {
                // 대시 공격으로 전환됐으면 더 이상 대시 상태가 아니므로 위협 스캔을 돌리지 않는다.
                if (playerController.TryTransitionToState(new PlayerJumpDashAttackState(playerController)))
                    return;
            }

            PollEvadeThreat();
        }

        /// <summary>
        /// 대시 중 주변 적의 활성/임박 공격을 스캔해 회피 성립을 판정한다.
        ///
        /// 대시는 무적 상태로 히트박스를 빠르게 통과하므로 피격 이벤트 자체가 발생하지 않는 경우가 많고,
        /// 그때 DefenseOutcome.Invincible 경로가 타지 않아 회피가 성립하지 않았다.
        /// 여기서는 겹침이 아니라 위협 반경/텔레그래프 시간으로 판정해 "스쳐 지나간" 회피를 잡는다.
        /// (피격 기반 경로는 그대로 유지되며, TryConsumeEvadeFeedback이 중복을 막는다)
        /// </summary>
        private void PollEvadeThreat()
        {
            if (_evadeFeedbackFired || playerActor == null) return;

            var policy = playerActor.Definition != null
                ? playerActor.Definition.EffectiveCombatDefensePolicy
                : null;
            if (policy != null && !policy.enableDashEvadeThreatScan) return;

            float range = policy != null
                ? policy.ResolveDashEvadeSearchRange(DefaultEvadeSearchRange)
                : DefaultEvadeSearchRange;
            float beforeHit = policy != null
                ? policy.ResolveDashEvadeWindowBeforeHit(DefaultEvadeWindowBeforeHit)
                : DefaultEvadeWindowBeforeHit;
            float afterHit = policy != null
                ? policy.ResolveDashEvadeGraceAfterHitStart(DefaultEvadeGraceAfterHit)
                : DefaultEvadeGraceAfterHit;
            float padding = policy != null
                ? policy.ResolveDashEvadeRadiusPadding(DefaultEvadeRadiusPadding)
                : DefaultEvadeRadiusPadding;

            if (!EnemyThreatScanner.TryFindBestThreat(
                    motor.TransientPosition,
                    range,
                    EnemyLayerMask,
                    beforeHit,
                    afterHit,
                    padding,
                    ThreatOverlapBuffer,
                    EvaluatedThreatMonsters,
                    out EnemyAttackThreat threat))
                return;

            playerActor.TryDashEvadeFeedback(threat);
        }

        private void IgnoreMonsterColliders()
        {
            _ignoredOnDodge.Clear();
            _enemyControllers.Clear();

            int monsterLayer = LayerMask.GetMask("Enemy");

            Vector3 capsuleBottom = motor.TransientPosition + motor.CharacterUp * motor.Capsule.radius;
            Vector3 capsuleTop    = motor.TransientPosition + motor.CharacterUp * (motor.Capsule.height - motor.Capsule.radius);

            var hits = Physics.OverlapCapsule(capsuleBottom, capsuleTop, 5f, monsterLayer);

            foreach (var col in hits)
            {
                _ignoredOnDodge.Add(col);

                // 플레이어 컨트롤러에서 몬스터 콜라이더 무시
                controller.AddIgnoreCollider(col);

                // 몬스터 컨트롤러에서 플레이어 캡슐 무시 (양방향)
                var enemyController = col.GetComponentInParent<EnemyMovementController>();
                if (enemyController != null)
                {
                    enemyController.AddIgnoreCollider(motor.Capsule);
                    _enemyControllers.Add(enemyController);
                }
            }
        }

        private void RestoreAndResolvePenetration()
        {
            Vector3 resolvedPosition = motor.TransientPosition;

            // ComputePenetration으로 겹침 해소
            foreach (var col in _ignoredOnDodge)
            {
                if (col == null) continue;

                bool overlapping = Physics.ComputePenetration(
                    motor.Capsule,      resolvedPosition,        motor.TransientRotation,
                    col,                col.transform.position,  col.transform.rotation,
                    out Vector3 dir,    out float dist
                );

                if (overlapping)
                    resolvedPosition += dir * (dist + 0.01f);
            }

            if (resolvedPosition != motor.TransientPosition)
                motor.SetPosition(resolvedPosition);

            // 플레이어 쪽 무시 목록 해제
            foreach (var col in _ignoredOnDodge)
            {
                if (col != null)
                    controller.RemoveIgnoreCollider(col);
            }

            // 몬스터 쪽 무시 목록 해제 (양방향)
            foreach (var enemyController in _enemyControllers)
            {
                if (enemyController != null)
                    enemyController.RemoveIgnoreCollider(motor.Capsule);
            }

            _ignoredOnDodge.Clear();
            _enemyControllers.Clear();
        }
        
        private void FinishDash()
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                playerController.TransitionToState(ActorStateId.Airborne);
                return;
            }

            if (playerController.HasMoveInput())
                controller.TransitionToState(ActorStateId.GroundMove);
            else
                controller.TransitionToState(ActorStateId.Idle);
        }

        private void OnAnimationEnd()
        {
            if (controller.CurrentState == this)
                FinishDash();
        }
    }
}
