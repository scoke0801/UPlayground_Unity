using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 몬스터 공중 선회 + 투사체 발사.
    /// 
    /// 카운트 관리:
    /// - Brain._airAttackCount가 유일한 카운터
    /// - 발사 모션 완료(또는 타임아웃) → Brain.OnAirAttackFinished()
    /// </summary>
    public class EnemyFlyingAirCircleState : GameActorState
    {
        public override string StateName => "Flying_AirCircle";
        public override bool AdjustGravity => false;

        private readonly EnemyFlyingAIContext _brain;

        // 선회
        private float _orbitAngle;
        private float _orbitDirection;
        private float _dirChangeTimer;
        private float _nextDirChangeTime;

        // 공격 타이밍
        private float _attackCooldown;
        private bool _isAttacking;

        // 고도 유지
        private float _verticalVelocity;
        private float _currentHoverHeight; // 매 진입마다 랜덤 결정
        private float _currentMaxStay;    // 매 진입마다 랜덤 결정

        // 전체 체류 타임아웃 — 어떤 이유로든 빠져나가지 못하는 상황 방지
        private float _totalTimer;

        private const float FirstShotDelay = 0.5f;       // SO 폴백용 기본값
        private const float ShotInterval = 0.8f;
        private const float AttackMotionTimeout = 3.0f;
        private const float MaxStayDuration = 8.0f;

        // SO에서 값을 읽되, SO 미할당 시 const 폴백
        private float Cfg_FirstShotDelay => _brain.FlyingSettings ? _brain.FlyingSettings.firstShotDelay : FirstShotDelay;
        private float Cfg_ShotInterval => _brain.FlyingSettings ? _brain.FlyingSettings.shotInterval : ShotInterval;
        private float Cfg_MotionTimeout => _brain.FlyingSettings ? _brain.FlyingSettings.attackMotionTimeout : AttackMotionTimeout;
        private float Cfg_DirChangeMin => _brain.FlyingSettings ? _brain.FlyingSettings.dirChangeTimeMin : 1.5f;
        private float Cfg_DirChangeMax => _brain.FlyingSettings ? _brain.FlyingSettings.dirChangeTimeMax : 3.5f;

        public EnemyFlyingAirCircleState(ActorMovementController controller, EnemyFlyingAIContext brain)
            : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _attackCooldown = Cfg_FirstShotDelay;
            _isAttacking = false;
            _verticalVelocity = 0f;
            _totalTimer = 0f;
            _orbitDirection = Random.value > 0.5f ? 1f : -1f;
            _dirChangeTimer = 0f;
            _nextDirChangeTime = Random.Range(Cfg_DirChangeMin, Cfg_DirChangeMax);

            // 고도 랜덤: Brain.AirHoverHeight ± SO.hoverHeightVariance
            float variance = _brain.FlyingSettings ? _brain.FlyingSettings.hoverHeightVariance : 1.5f;
            _currentHoverHeight = _brain.AirHoverHeight + Random.Range(-variance, variance);
            _currentHoverHeight = Mathf.Max(_currentHoverHeight, 2f); // 최소 2m

            // 체류 시간 랜덤
            float stayMin = _brain.FlyingSettings ? _brain.FlyingSettings.maxAirStayMin : 4f;
            float stayMax = _brain.FlyingSettings ? _brain.FlyingSettings.maxAirStayMax : MaxStayDuration;
            _currentMaxStay = Random.Range(stayMin, stayMax);

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
            gameActor.Animator.OnMotionSetCompleted -= OnAttackMotionEnd;
            _isAttacking = false;
        }

        public override void UpdateState(float deltaTime)
        {
            _totalTimer += deltaTime;

            // ── 최대 체류 시간 초과 → 강제 Dive ──
            if (_totalTimer >= _currentMaxStay)
            {
                ForceDescend();
                return;
            }

            if (!_brain.Detection.HasTarget) return;
            
            // ── 공격 횟수 도달 → Dive ──
            if (_brain.AirAttackCount >= _brain.AirAttackLimit)
            {
                ForceDescend();
                return;
            }

            // ── 쿨다운 후 공격 ──
            _attackCooldown -= deltaTime;
            if (_attackCooldown <= 0f)
            {
                TryAerialAttack();
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_brain.Detection.HasTarget) return;

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

            // ── 공격 모션 중: 선회 정지, 고도만 유지 ──
            if (_isAttacking)
            {
                // 수평 감속 → 정지
                currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0f, deltaTime * 8f);
                currentVelocity.z = Mathf.Lerp(currentVelocity.z, 0f, deltaTime * 8f);

                // 고도 유지
                float hoverY = targetPos.y + _currentHoverHeight;
                float hDiff = hoverY - motor.TransientPosition.y;
                _verticalVelocity += (hDiff * 6f - _verticalVelocity * 4f) * deltaTime;
                currentVelocity.y = _verticalVelocity;
                return;
            }

            // ── 선회 이동 ──
            float radius = _brain.AirCircleRadius;
            float speed = _brain.AirMoveSpeed;
            float angularSpeed = speed / radius;

            _dirChangeTimer += deltaTime;
            if (_dirChangeTimer >= _nextDirChangeTime)
            {
                _orbitDirection *= -1f;
                _dirChangeTimer = 0f;
                _nextDirChangeTime = Random.Range(Cfg_DirChangeMin, Cfg_DirChangeMax);
            }

            _orbitAngle += angularSpeed * _orbitDirection * deltaTime;

            Vector3 desiredPos = targetPos + new Vector3(
                Mathf.Cos(_orbitAngle) * radius,
                0f,
                Mathf.Sin(_orbitAngle) * radius);
            desiredPos.y = targetPos.y + _currentHoverHeight;

            Vector3 toDesired = desiredPos - motor.TransientPosition;
            Vector3 horizontalMove = new Vector3(toDesired.x, 0f, toDesired.z);
            float horizDist = horizontalMove.magnitude;

            Vector3 horizVel = horizDist > 0.5f
                ? horizontalMove.normalized * speed
                : horizontalMove * (speed / 0.5f);

            float heightDiff = desiredPos.y - motor.TransientPosition.y;
            _verticalVelocity += (heightDiff * 6f - _verticalVelocity * 4f) * deltaTime;

            currentVelocity = new Vector3(horizVel.x, _verticalVelocity, horizVel.z);
        }

        #region 공격

        /// <summary>
        /// 공중 공격 스킬 선택 + 모션 재생.
        /// 투사체 발사는 AnimEvent에서 처리 — 여기서는 모션만 제어한다.
        /// </summary>
        private void TryAerialAttack()
        {
            float dist = _brain.Detection.DistanceToTarget;
            var aerialSkills = _brain.Combat.AttackData?.GetAvailableAerialSkills(dist, _brain.Combat.CurrentLevel);
            // isDiveAttack 스킬 제외 (Dive는 Brain.TransitionToDescend에서 처리)
            aerialSkills?.RemoveAll(s => s.isDiveAttack);

            if (aerialSkills == null || aerialSkills.Count == 0)
            {
                _attackCooldown = 0.5f;
                return;
            }

            var skill = _brain.Combat.AttackData.SelectRandomAerialSkill(aerialSkills);
            if (skill == null) return;

            // Combat에 스킬 설정 — AnimEvent가 이 스킬을 참조하여 투사체 발사
            _brain.Combat.SetCurrentSkill(skill);
            _isAttacking = true;

            var animState = gameActor.Animator.PlayMotion(skill.baseInfo.animKey, 0.1f);
            if (animState != null)
            {
                gameActor.Animator.OnMotionSetCompleted += OnAttackMotionEnd;
            }
            else
            {
                // 모션 없으면 즉시 완료 처리
                _isAttacking = false;
                _attackCooldown = Cfg_ShotInterval;
                _brain.OnAirAttackFinished();
            }
        }

        private void OnAttackMotionEnd()
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackMotionEnd;

            if (!_isAttacking) return;
            _isAttacking = false;
            _attackCooldown = Cfg_ShotInterval;

            // 선회 모션 복귀 + 방향 반전
            gameActor.Animator.PlayMotion(AnimKey.Fly_Move, 0.2f);
            _orbitDirection *= -1f;
            _dirChangeTimer = 0f;
            _nextDirChangeTime = Random.Range(Cfg_DirChangeMin, Cfg_DirChangeMax);

            _brain.OnAirAttackFinished();
        }

        /// <summary>
        /// 안전장치 하강 — Brain.TransitionToDescend를 호출하여
        /// 데이터 기반(Dive/Land) 분기를 타게 한다.
        /// </summary>
        private void ForceDescend()
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackMotionEnd;
            _isAttacking = false;

            _brain.OnAirCircleForceDescend();
        }

        #endregion
    }
}
