using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 몬스터 급강하.
    /// Approach(타겟 전방 상공으로 이동) → Telegraph(날개 접기) → Dive(대각선 돌진) → Recovery(후딜)
    /// </summary>
    public class EnemyFlyingDiveState : EnemyActorState
    {
        public override string StateName => "Flying_Dive";
        public override bool BlocksBehaviorTree => true;
        public override bool AdjustGravity => false;

        private readonly EnemyFlyingAIContext _brain;

        private enum Phase { Approach, Telegraph, Diving, Recovery, WaitGround }
        private Phase _phase;
        private float _phaseTimer;

        private Vector3 _approachTarget; // Approach 목표 (타겟 전방 상공)
        private Vector3 _diveTarget;     // Diving 착지 목표 (타겟 발밑 지면)
        private bool _impactApplied;
        private Collider _targetCollider;

        // 폴백 기본값
        private const float TelegraphDuration = 1.0f;
        private const float RecoveryDuration = 1.0f;
        private const float ApproachOffset = 5.0f;
        private const float ApproachArrivalDist = 2.5f;
        private const float ApproachTimeout = 3.0f;

        // SO 접근
        private float Cfg_Telegraph => _brain.FlyingSettings ? _brain.FlyingSettings.diveTelegraphDuration : TelegraphDuration;
        private float Cfg_Recovery => _brain.FlyingSettings ? _brain.FlyingSettings.diveRecoveryDuration : RecoveryDuration;
        private float Cfg_ApproachOffset => _brain.FlyingSettings ? _brain.FlyingSettings.diveApproachOffset : ApproachOffset;
        private float Cfg_ArrivalDist => _brain.FlyingSettings ? _brain.FlyingSettings.diveApproachArrivalDist : ApproachArrivalDist;
        private float Cfg_ApproachTimeout => _brain.FlyingSettings ? _brain.FlyingSettings.diveApproachTimeout : ApproachTimeout;

        public EnemyFlyingDiveState(ActorMovementController controller, EnemyFlyingAIContext brain)
            : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override bool CanPlayHitReaction(in HitContext hit)
        {
            return base.CanPlayHitReaction(hit)
                   && _phase is Phase.Approach or Phase.Telegraph or Phase.Recovery;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _phaseTimer = 0f;
            _impactApplied = false;
            _targetCollider = null;

            motor.SetGroundSolvingActivation(false);
            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(true);

            // 플레이어 Collider 충돌 무시
            if (_brain.Detection.HasTarget)
            {
                _targetCollider = _brain.Detection.CurrentTarget.GetComponent<Collider>();
                if (_targetCollider != null)
                    controller.AddIgnoreCollider(_targetCollider);
            }

            CalculateApproachTarget();

            // 현재 위치가 이미 Approach 목표 근처면 바로 Telegraph
            float horizDist = HorizDist(motor.TransientPosition, _approachTarget);
            if (horizDist <= Cfg_ArrivalDist)
            {
                EnterTelegraph();
            }
            else
            {
                _phase = Phase.Approach;
                gameActor.Animator.PlayMotion(AnimKey.Fly_Move, 0.15f);
            }
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            motor.SetGroundSolvingActivation(true);
            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(false);

            if (_targetCollider != null)
            {
                controller.RemoveIgnoreCollider(_targetCollider);
                _targetCollider = null;
            }
        }

        public override void UpdateState(float deltaTime)
        {
            _phaseTimer += deltaTime;

            switch (_phase)
            {
                case Phase.Approach:
                {
                    float horizDist = HorizDist(motor.TransientPosition, _approachTarget);
                    if (horizDist <= Cfg_ArrivalDist || _phaseTimer >= Cfg_ApproachTimeout)
                        EnterTelegraph();
                    break;
                }

                case Phase.Telegraph:
                    if (_phaseTimer >= Cfg_Telegraph)
                        EnterDiving();
                    break;

                case Phase.Diving:
                {
                    float groundY = GetGroundY(motor.TransientPosition);
                    bool nearGround = motor.TransientPosition.y <= groundY + 1.0f;
                    bool timeout = _phaseTimer > 5f;

                    if (nearGround || timeout)
                    {
                        OnImpact();
                        motor.SetGroundSolvingActivation(true);

                        // 지면에 확실히 붙을 때까지 대기 Phase로 전환
                        _phase = Phase.WaitGround;
                        _phaseTimer = 0f;
                    }
                    break;
                }

                case Phase.WaitGround:
                {
                    if (motor.GroundingStatus.IsStableOnGround)
                    {
                        EnterRecovery();
                        break;
                    }

                    if (_phaseTimer > 2f)
                    {
                        EnterRecovery();
                    }
                    break;
                }

                case Phase.Recovery:
                    if (_phaseTimer >= Cfg_Recovery)
                        _brain.OnDiveLanded();
                    break;
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Vector3 lookDir = Vector3.zero;

            switch (_phase)
            {
                case Phase.Approach:
                    // Approach 목표를 바라봄
                    lookDir = _approachTarget - motor.TransientPosition;
                    break;
                case Phase.Telegraph:
                    // 타겟(플레이어)을 바라봄
                    if (_brain.Detection.HasTarget)
                        lookDir = _brain.Detection.CurrentTarget.position - motor.TransientPosition;
                    break;
                case Phase.Diving:
                    // 착지 목표를 바라봄
                    lookDir = _diveTarget - motor.TransientPosition;
                    break;
            }

            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(lookDir.normalized);
                currentRotation = Quaternion.Slerp(currentRotation, target,
                    1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
            }
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            switch (_phase)
            {
                case Phase.Approach:
                {
                    // Approach 목표로 빠르게 수평 이동 (고도 유지)
                    Vector3 toApproach = _approachTarget - motor.TransientPosition;
                    Vector3 horizMove = new Vector3(toApproach.x, 0f, toApproach.z);
                    float speed = _brain.AirMoveSpeed * 2f;

                    Vector3 targetVel = horizMove.magnitude > 0.5f
                        ? horizMove.normalized * speed
                        : Vector3.zero;

                    // 고도 유지
                    float heightDiff = _approachTarget.y - motor.TransientPosition.y;
                    targetVel.y = heightDiff * 3f;

                    currentVelocity = Vector3.Lerp(currentVelocity, targetVel, deltaTime * 6f);
                    break;
                }

                case Phase.Telegraph:
                    // 정지 + 약간 상승 (위압감, 텔레그래핑)
                    currentVelocity = Vector3.Lerp(currentVelocity, Vector3.up * 1.5f, deltaTime * 5f);
                    break;

                case Phase.Diving:
                {
                    Vector3 toTarget = _diveTarget - motor.TransientPosition;
                    // 공격 Dive: 고속 돌진, 비공격: 부드러운 하강
                    float speed = IsAttackDive ? _brain.DiveSpeed : _brain.DiveSpeed * 0.4f;

                    if (toTarget.sqrMagnitude > 0.1f)
                        currentVelocity = toTarget.normalized * speed;
                    else
                        currentVelocity = Vector3.down * speed;
                    break;
                }

                case Phase.Recovery:
                    currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero,
                        1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                    if (motor.GroundingStatus.IsStableOnGround && currentVelocity.y < 0)
                        currentVelocity.y = -0.1f;
                    break;

                case Phase.WaitGround:
                    // 중력으로 자연 낙하하여 지면에 붙기
                    currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0f, deltaTime * 8f);
                    currentVelocity.z = Mathf.Lerp(currentVelocity.z, 0f, deltaTime * 8f);
                    currentVelocity += controller.Gravity * deltaTime;
                    break;
            }
        }

        public override void PostGroundingUpdate(float deltaTime)
        {
            if (_phase == Phase.Diving
                && motor.GroundingStatus.IsStableOnGround
                && !motor.LastGroundingStatus.IsStableOnGround)
            {
                OnImpact();
                motor.SetGroundSolvingActivation(true);
                _phase = Phase.WaitGround;
                _phaseTimer = 0f;
            }

            if (_phase == Phase.WaitGround
                && motor.GroundingStatus.IsStableOnGround)
            {
                EnterRecovery();
            }
        }

        #region 내부

        /// <summary>
        /// Approach 목표 = 타겟 전방(몬스터→타겟 방향의 반대) offset 거리, 현재 고도 유지.
        /// 내려찍기가 대각선으로 들어오도록 타겟에서 약간 떨어진 상공을 목표로 한다.
        /// </summary>
        private void CalculateApproachTarget()
        {
            if (!_brain.Detection.HasTarget)
            {
                _approachTarget = motor.TransientPosition;
                return;
            }

            Vector3 targetPos = _brain.Detection.CurrentTarget.position;

            // 몬스터에서 타겟으로의 방향 (수평)
            Vector3 fromMonster = (targetPos - motor.TransientPosition);
            fromMonster.y = 0;

            Vector3 approachDir;
            if (fromMonster.sqrMagnitude > 0.01f)
                approachDir = fromMonster.normalized;
            else
                approachDir = _brain.Detection.CurrentTarget.forward; // 폴백: 타겟 전방

            // 타겟에서 approachDir 반대 방향으로 offset만큼 떨어진 상공
            _approachTarget = targetPos - approachDir * Cfg_ApproachOffset;
            _approachTarget.y = motor.TransientPosition.y; // 현재 고도 유지
        }

        private bool IsAttackDive =>
            _brain.Combat.CurrentSkill != null && _brain.Combat.CurrentSkill.isDiveAttack;

        /// <summary>
        /// Recovery Phase 진입.
        /// 공격 Dive: Fall → Fly_Landing 전환 필요.
        /// 비공격 Dive: 이미 Fly_Landing 재생 중이므로 모션 재생 스킵.
        /// </summary>
        private void EnterRecovery()
        {
            _phase = Phase.Recovery;
            _phaseTimer = 0f;

            if (IsAttackDive)
                gameActor.Animator.PlayMotion(AnimKey.Fly_Landing, 0.1f);
        }

        private void EnterTelegraph()
        {
            _phase = Phase.Telegraph;
            _phaseTimer = 0f;

            if (IsAttackDive)
                gameActor.Animator.PlayMotion(AnimKey.Fly_Attack, 0.15f);
        }

        private void EnterDiving()
        {
            _phase = Phase.Diving;
            _phaseTimer = 0f;

            // Dive 목표: 타겟 발밑 지면
            if (_brain.Detection.HasTarget)
            {
                _diveTarget = _brain.Detection.CurrentTarget.position;
                _diveTarget.y = GetGroundY(_diveTarget);
            }
            else
            {
                _diveTarget = motor.TransientPosition;
                _diveTarget.y = GetGroundY(_diveTarget);
            }

            gameActor.Animator.PlayMotion(AnimKey.Fall, 0.1f);
        }

        private void OnImpact()
        {
            if (_impactApplied) return;
            _impactApplied = true;

            // 비공격 Dive는 착지 충격 판정 없음
            if (!IsAttackDive) return;

            float radius = _brain.DiveImpactRadius;
            Vector3 impactPos = motor.TransientPosition;
            impactPos.y = GetGroundY(impactPos);

            GameObjectManager.Instance.ShowFX(FXKeyType.GriffinDiveImpact, impactPos);

            LayerMask targetLayer = LayerMask.GetMask("Player");
            Collider[] hits = Physics.OverlapSphere(impactPos, radius, targetLayer);

            // Brain.TransitionToDescend에서 SetCurrentSkill한 Dive 스킬 사용
            var diveSkill = _brain.Combat.CurrentSkill;
            var phase = diveSkill?.baseInfo.GetHitPhase(0);

            foreach (var hit in hits)
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable == null || !damageable.CanTakeDamage()) continue;

                AttackData attackData = new AttackData
                {
                    damage = phase?.damage ?? 50f,
                    poiseDamage = phase?.poiseDamage ?? 100f,
                    breakDamage = phase?.breakDamage ?? 0f,
                    attackDirection = (hit.transform.position - impactPos).normalized,
                    hitPoint = hit.ClosestPoint(impactPos),
                    reactionType = phase?.reactionType ?? AttackReactionType.Airborne,
                    airborneForce = phase?.airborneForce ?? 12f,
                    knockbackForce = phase?.knockBackForce ?? 8f,
                    knockbackDrag = phase?.knockBackDrag ?? 5f,
                    hitParticleName = phase?.hitParticleName ?? "HeavyHit",
                    attacker = gameActor as MonsterActor,
                    reactionData = phase?.reactionProfile?.Resolve(),
                };

                damageable.ReceiveHit(HitRequest.FromAttackData(attackData));
            }
        }

        private static float HorizDist(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }

        private float GetGroundY(Vector3 pos)
        {
            int groundMask = LayerMask.GetMask("Default", "Ground", "InteractableObject");
            if (groundMask == 0) groundMask = ~LayerMask.GetMask("Player", "Monster", "Enemy");

            if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f,
                    groundMask, QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return 0f;
        }

        #endregion
    }
}
