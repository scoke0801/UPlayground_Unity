using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 공중 체공 허브 State.
    ///
    /// [수직 제어]
    /// 단순 P제어만 사용. PD의 D항은 매 프레임 오차 부호가 바뀌며 진동을 유발하므로 제거.
    ///
    /// [수평 행동]
    /// Idle : 플레이어 주변 hoverIdleRadius 반경의 목표 지점으로 직접 이동 (선회)
    ///        매 프레임 선회 각도를 일정 속도로 증가 → 목표 XZ를 직접 계산 → 그쪽으로 이동
    /// Move : 플레이어 방향으로 직선 접근
    /// </summary>
    public class EnemyAerialState : GameActorState
    {
        public override string StateName => "Aerial";

        private readonly AerialBehaviorLayer _aerialLayer;
        private EnemyDetection _detection;

        private enum Sub { Idle, Move, Attack }
        private Sub _sub = Sub.Idle;

        private float _targetHoverY;
        private float _orbitAngle;         // 현재 선회 각도 (도)
        private bool  _attackInProgress;
        private bool  _isPlayingMoveAnim;

        public EnemyAerialState(ActorMovementController controller, AerialBehaviorLayer aerialLayer)
            : base(controller)
        {
            _aerialLayer = aerialLayer;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _detection = gameActor.GetComponent<EnemyDetection>();

            motor.SetGroundSolvingActivation(false);

            var data = _aerialLayer.Data;
            _targetHoverY      = Mathf.Clamp(motor.TransientPosition.y,
                                     _spawnY + data.minHoverHeight,
                                     _spawnY + data.maxHoverHeight);
            _orbitAngle        = 0f;
            _attackInProgress  = false;
            _isPlayingMoveAnim = false;

            _aerialLayer.OnEnterAerial();
            PlaySubAnim(Sub.Idle);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (_aerialLayer.ShouldLand())
            {
                controller.TransitionToState(new EnemyLandState(controller, _aerialLayer));
                return;
            }

            _aerialLayer.Tick(deltaTime);

            if (_attackInProgress) return;

            UpdateSubState();
        }

        private void UpdateSubState()
        {
            if (_detection == null || !_detection.HasTarget)
            {
                TransitionSub(Sub.Idle);
                return;
            }

            float dist = _detection.DistanceToTarget;

            if (_aerialLayer.CanAttack(dist))
            {
                TryLaunchAttack(dist);
                return;
            }

            // 사거리 밖이면 접근, 사거리 안이면 선회 대기
            TransitionSub(dist > _aerialLayer.MaxAerialRange ? Sub.Move : Sub.Idle);
        }

        // ── 속도 제어 ─────────────────────────────────────────────────

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 horizontal = CalcHorizontalVelocity(deltaTime);
            float   vertical   = CalcVerticalVelocity(deltaTime);

            currentVelocity.x = horizontal.x;
            currentVelocity.z = horizontal.z;
            currentVelocity.y = vertical;
        }

        private Vector3 CalcHorizontalVelocity(float deltaTime)
        {
            var data = _aerialLayer.Data;

            if (_sub == Sub.Move && _detection != null && _detection.HasTarget)
            {
                Vector3 toTarget = _detection.CurrentTarget.position - motor.TransientPosition;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.01f) return Vector3.zero;
                return toTarget.normalized * data.hoverMoveSpeed;
            }

            if (_sub == Sub.Idle && _detection != null && _detection.HasTarget)
            {
                // 선회: 매 프레임 각도 증가 → 플레이어 주변 목표 XZ 직접 계산 → 그 방향으로 이동
                // "desired 방향으로 조금씩" 방식이 아닌 "목표 XZ로 직접" 방식 — 반경 유지되고 진동 없음
                float orbitSpeed = data.hoverMoveSpeed / Mathf.Max(data.hoverIdleRadius, 0.1f)
                                   * Mathf.Rad2Deg;
                _orbitAngle += orbitSpeed * deltaTime;

                Vector3 orbitTarget = _detection.CurrentTarget.position + new Vector3(
                    Mathf.Cos(_orbitAngle * Mathf.Deg2Rad),
                    0f,
                    Mathf.Sin(_orbitAngle * Mathf.Deg2Rad)) * data.hoverIdleRadius;

                Vector3 toOrbit = orbitTarget - motor.TransientPosition;
                toOrbit.y = 0f;

                // 목표에 가까울수록 속도 감소 → 오버슈트 방지
                float dist2d = toOrbit.magnitude;
                float speed  = Mathf.Clamp(dist2d * 3f, 0f, data.hoverMoveSpeed);
                return dist2d > 0.05f ? toOrbit.normalized * speed : Vector3.zero;
            }

            return Vector3.zero;
        }

        /// <summary>
        /// 수직 단순 P제어.
        /// D항 제거 — 매 프레임 오차 부호 반전으로 인한 진동의 원인이었음.
        /// </summary>
        private float CalcVerticalVelocity(float deltaTime)
        {
            var   data = _aerialLayer.Data;
            float curY = motor.TransientPosition.y;
            float minY = _spawnY + data.minHoverHeight;
            float maxY = _spawnY + data.maxHoverHeight;

            if (curY < minY) return data.hoverAscentSpeed;
            if (curY > maxY) return -data.hoverDescentSpeed;

            // springK를 P 게인으로 사용. 오차 작아질수록 속도도 줄어 자연스럽게 수렴
            float error = _targetHoverY - curY;
            float vY    = data.springK * error;
            return Mathf.Clamp(vY, -data.hoverDescentSpeed, data.hoverAscentSpeed);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_detection == null || !_detection.HasTarget) return;

            Vector3 dir = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;

            currentRotation = Quaternion.Slerp(currentRotation,
                Quaternion.LookRotation(dir),
                1f - Mathf.Exp(-8f * deltaTime));
        }

        // ── 공격 ──────────────────────────────────────────────────────

        private void TryLaunchAttack(float dist)
        {
            var skill = _aerialLayer.SelectAerialSkill(dist);
            if (skill == null) return;

            _attackInProgress = true;
            TransitionSub(Sub.Attack);

            if (skill.isDiveAttack)
            {
                _aerialLayer.SetPendingDiveAttack(skill);
                controller.TransitionToState(new EnemyLandState(controller, _aerialLayer));
            }
            else
            {
                var combat = gameActor.GetComponent<EnemyCombat>();
                controller.TransitionToState(
                    new EnemyAerialAttackState(controller, _aerialLayer, combat, _detection, skill));
            }
        }

        // ── 서브 전환 ─────────────────────────────────────────────────

        private void TransitionSub(Sub next)
        {
            if (_sub == next) return;
            _sub = next;
            PlaySubAnim(next);
        }

        private void PlaySubAnim(Sub sub)
        {
            bool wantMove = sub == Sub.Move;
            if (wantMove == _isPlayingMoveAnim) return;

            _isPlayingMoveAnim = wantMove;
            gameActor.Animator.PlayMotion(
                wantMove ? AnimKey.Fly_Move : AnimKey.Fly_Idle, 0.15f);
        }

        private float _spawnY => _aerialLayer.SpawnY;
    }
}
