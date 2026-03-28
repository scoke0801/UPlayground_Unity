using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 보스 공중 선회 + WingBlast 투사체 발사.
    /// 플레이어 주변을 선회하면서 일정 간격으로 투사체를 발사한다.
    /// 발사 횟수 도달 시 Brain이 Dive로 전환.
    /// </summary>
    public class EnemyFlyingAirCircleState : GameActorState
    {
        public override string StateName => "Flying_AirCircle";
        public override bool AdjustGravity => false;

        private readonly EnemyFlyingBrain _brain;

        // 선회
        private float _orbitAngle;
        private float _orbitDirection; // +1 or -1

        // 투사체 타이밍
        private float _attackTimer;
        private int _shotsFired;
        private bool _isAttacking;

        // 투사체 간 대기
        private const float ShotInterval = 1.5f;  // 텔레그래핑 포함 간격
        private const float FirstShotDelay = 1.0f; // 선회 진입 후 첫 발사까지 대기

        // 고도 유지
        private float _verticalVelocity;

        public EnemyFlyingAirCircleState(ActorMovementController controller, EnemyFlyingBrain brain)
            : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(string stateName)
            => stateName is "Death" or "Flying_Dive";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _shotsFired = 0;
            _attackTimer = -FirstShotDelay; // 첫 발사 딜레이
            _isAttacking = false;
            _verticalVelocity = 0f;
            _orbitDirection = Random.value > 0.5f ? 1f : -1f;

            // 현재 위치에서 타겟 기준 각도 계산
            if (_brain.Detection.HasTarget)
            {
                Vector3 offset = motor.TransientPosition - _brain.Detection.CurrentTarget.position;
                offset.y = 0;
                _orbitAngle = Mathf.Atan2(offset.z, offset.x);
            }

            motor.SetGroundSolvingActivation(false);
            gameActor.Animator.PlayMotion(AnimKey.Fly_Move, 0.2f);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!_brain.Detection.HasTarget) return;

            _attackTimer += deltaTime;

            // 발사 타이밍 도달
            if (!_isAttacking && _attackTimer >= ShotInterval)
            {
                if (_shotsFired < _brain.AirAttackLimit)
                {
                    FireProjectile();
                    _attackTimer = 0f;
                }
            }

            // 모든 발사 완료 → Brain에 알림 (Brain이 Dive로 전환)
            if (_shotsFired >= _brain.AirAttackLimit && !_isAttacking)
            {
                _brain.OnAirAttackFinished();
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_brain.Detection.HasTarget) return;

            // 항상 플레이어를 바라본다
            Vector3 dir = (_brain.Detection.CurrentTarget.position - motor.TransientPosition);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(dir.normalized);
                currentRotation = Quaternion.Slerp(currentRotation, target,
                    1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
            }
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_brain.Detection.HasTarget)
            {
                currentVelocity = Vector3.zero;
                return;
            }

            Vector3 targetPos = _brain.Detection.CurrentTarget.position;
            float radius = _brain.AirCircleRadius;
            float speed = _brain.AirMoveSpeed;

            // 선회 각도 갱신
            float angularSpeed = speed / radius; // rad/s
            _orbitAngle += angularSpeed * _orbitDirection * deltaTime;

            // 목표 위치: 플레이어 위치 + 반경 오프셋 + 고도
            Vector3 desiredPos = targetPos + new Vector3(
                Mathf.Cos(_orbitAngle) * radius,
                0f,
                Mathf.Sin(_orbitAngle) * radius);
            desiredPos.y = targetPos.y + _brain.AirHoverHeight;

            // 현재 → 목표 벡터
            Vector3 toDesired = desiredPos - motor.TransientPosition;

            // 수평 이동
            Vector3 horizontalMove = new Vector3(toDesired.x, 0f, toDesired.z);
            float horizDist = horizontalMove.magnitude;

            Vector3 horizVel;
            if (horizDist > 0.5f)
                horizVel = horizontalMove.normalized * speed;
            else
                horizVel = horizontalMove * (speed / 0.5f); // 가까울수록 감속

            // 고도 유지 (스프링-댐퍼)
            float heightDiff = desiredPos.y - motor.TransientPosition.y;
            float springK = 6f;
            float damping = 4f;
            _verticalVelocity += (heightDiff * springK - _verticalVelocity * damping) * deltaTime;

            currentVelocity = new Vector3(horizVel.x, _verticalVelocity, horizVel.z);
        }

        private void FireProjectile()
        {
            _shotsFired++;

            // 공중 스킬 선택 (WingBlast — isAerialSkill=true인 스킬)
            float dist = _brain.Detection.DistanceToTarget;
            var aerialSkills = _brain.Combat.AttackData?.GetAvailableAerialSkills(dist);
            if (aerialSkills == null || aerialSkills.Count == 0)
            {
                Debug.LogWarning("[FlyingBossAirCircle] 사용 가능한 공중 스킬 없음");
                return;
            }

            var skill = _brain.Combat.AttackData.SelectRandomAerialSkill(aerialSkills);
            if (skill == null) return;

            // 스킬 세팅 후 공격 모션 재생
            _brain.Combat.SetCurrentSkill(skill);
            _isAttacking = true;

            var animState = gameActor.Animator.PlayMotion(skill.baseInfo.animKey, 0.1f);
            if (animState != null)
            {
                gameActor.Animator.OnMotionSetCompleted += OnAttackMotionEnd;
            }
            else
            {
                _isAttacking = false;
            }

            // 투사체 스폰은 애니메이션 이벤트(BeginCollisionEvent)에서 처리됨.
            // 만약 이벤트 없이 직접 스폰이 필요하면 여기서 SpawnProjectile 호출.
        }

        private void OnAttackMotionEnd()
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackMotionEnd;
            _isAttacking = false;

            // 공격 모션 후 선회 모션 복귀
            gameActor.Animator.PlayMotion(AnimKey.Fly_Move, 0.2f);

            // Brain에 알림 — 다음 발사 or Dive 판단
            _brain.OnAirAttackFinished();
        }
    }
}
