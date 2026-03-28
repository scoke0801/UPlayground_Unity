using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 보스 급강하.
    /// 텔레그래핑(날개 접기) → 타겟 방향으로 고속 하강 → 착지 순간 범위 판정 → 후딜.
    /// </summary>
    public class EnemyFlyingDiveState : GameActorState
    {
        public override string StateName => "Flying_Dive";
        public override bool AdjustGravity => false;

        private readonly EnemyFlyingBrain _brain;

        private enum Phase { Telegraph, Diving, Recovery }
        private Phase _phase;
        private float _phaseTimer;

        private Vector3 _diveTarget;
        private bool _impactApplied;
        private Collider _targetCollider; // Diving 중 충돌 무시용

        // 타이밍
        private const float TelegraphDuration = 0.7f;
        private const float RecoveryDuration = 1.0f; // 착지 후딜 = 반격 창

        public EnemyFlyingDiveState(ActorMovementController controller, EnemyFlyingBrain brain)
            : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(string stateName)
            => stateName is "Death";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _phase = Phase.Telegraph;
            _phaseTimer = 0f;
            _impactApplied = false;
            _targetCollider = null;

            motor.SetGroundSolvingActivation(false);
            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(true);

            gameActor.Animator.PlayMotion(AnimKey.Fly_Attack, 0.15f);

            if (_brain.Detection.HasTarget)
            {
                Transform target = _brain.Detection.CurrentTarget;
                _diveTarget = target.position;
                _diveTarget.y = GetGroundY(_diveTarget);

                // 플레이어 Collider와의 KCC 충돌 무시 — 머리 위에 올라타는 것 방지
                _targetCollider = target.GetComponent<Collider>();
                if (_targetCollider != null)
                    controller.AddIgnoreCollider(_targetCollider);
            }
            else
            {
                _diveTarget = motor.TransientPosition;
                _diveTarget.y = 0f;
            }
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            motor.SetGroundSolvingActivation(true);
            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(false);

            // 충돌 무시 해제
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
                case Phase.Telegraph:
                    if (_phaseTimer >= TelegraphDuration)
                    {
                        _phase = Phase.Diving;
                        _phaseTimer = 0f;

                        // 다이브 시작 시 타겟 위치 최종 갱신
                        if (_brain.Detection.HasTarget)
                        {
                            _diveTarget = _brain.Detection.CurrentTarget.position;
                            _diveTarget.y = GetGroundY(_diveTarget);
                        }
                    }
                    break;

                case Phase.Diving:
                {
                    // 고도 기반 착지 판정 — GroundSolving이 꺼져있으므로 직접 체크
                    bool reachedGround = motor.TransientPosition.y <= _diveTarget.y + 0.8f;
                    bool timeout = _phaseTimer > 5f;

                    if (reachedGround || timeout)
                    {
                        OnImpact();

                        // GroundSolving을 먼저 복구해야 IsStableOnGround가 작동한다
                        motor.SetGroundSolvingActivation(true);

                        _phase = Phase.Recovery;
                        _phaseTimer = 0f;

                        gameActor.Animator.PlayMotion(AnimKey.Fly_Landing, 0.1f);
                        Debug.Log("[FlyingBoss] Dive → Recovery (후딜 시작)");
                    }
                    break;
                }

                case Phase.Recovery:
                    if (_phaseTimer >= RecoveryDuration)
                    {
                        _brain.OnDiveLanded();
                    }
                    break;
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 텔레그래핑 중 타겟 방향으로 회전
            if (_phase == Phase.Telegraph && _brain.Detection.HasTarget)
            {
                Vector3 dir = (_diveTarget - motor.TransientPosition);
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion target = Quaternion.LookRotation(dir.normalized);
                    currentRotation = Quaternion.Slerp(currentRotation, target,
                        1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
                }
            }
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            switch (_phase)
            {
                case Phase.Telegraph:
                    // 제자리 정지 (약간 위로 떠오르는 느낌)
                    currentVelocity = Vector3.Lerp(currentVelocity, Vector3.up * 0.5f, deltaTime * 3f);
                    break;

                case Phase.Diving:
                    // 타겟 방향 + 하방으로 고속 이동
                    Vector3 toTarget = _diveTarget - motor.TransientPosition;
                    Vector3 diveDir = toTarget.normalized;

                    float speed = _brain.DiveSpeed;
                    currentVelocity = diveDir * speed;

                    // 최소 하강 속도 보장
                    if (currentVelocity.y > -speed * 0.5f)
                        currentVelocity.y = -speed * 0.5f;
                    break;

                case Phase.Recovery:
                    // 정지
                    currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero,
                        1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));

                    // Recovery 중 지면 고정
                    if (motor.GroundingStatus.IsStableOnGround && currentVelocity.y < 0)
                        currentVelocity.y = -0.1f;
                    break;
            }
        }

        public override void PostGroundingUpdate(float deltaTime)
        {
            // Diving 중 KCC가 지면 접촉을 감지하면 즉시 전환
            // (UpdateState의 고도 체크보다 정확한 물리 기반 감지)
            if (_phase == Phase.Diving
                && motor.GroundingStatus.IsStableOnGround
                && !motor.LastGroundingStatus.IsStableOnGround)
            {
                OnImpact();
                motor.SetGroundSolvingActivation(true);
                _phase = Phase.Recovery;
                _phaseTimer = 0f;
                gameActor.Animator.PlayMotion(AnimKey.Fly_Landing, 0.1f);
            }
        }

        /// <summary>
        /// 착지 충격 판정 — 범위 내 IDamageable에 데미지.
        /// </summary>
        private void OnImpact()
        {
            if (_impactApplied) return;
            _impactApplied = true;

            float radius = _brain.DiveImpactRadius;
            Vector3 impactPos = motor.TransientPosition;
            LayerMask targetLayer = LayerMask.GetMask("Player"); // 프로젝트에 맞게 조정

            Collider[] hits = Physics.OverlapSphere(impactPos, radius, targetLayer);
            foreach (var hit in hits)
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable == null || !damageable.CanTakeDamage()) continue;

                // Dive 스킬 데이터에서 수치 가져오기
                // EnemyAttackDataSO에 isDiveAttack=true인 스킬이 있으면 그 수치 사용
                var diveSkill = FindDiveSkill();
                var phase = diveSkill?.baseInfo.GetHitPhase(0);

                AttackData attackData = new AttackData
                {
                    damage = phase?.damage ?? 50f,
                    poiseDamage = phase?.poiseDamage ?? 100f,
                    attackDirection = (hit.transform.position - impactPos).normalized,
                    hitPoint = hit.ClosestPoint(impactPos),
                    reactionType = phase?.reactionType ?? AttackReactionType.Airborne,
                    airborneForce = phase?.airborneForce ?? 12f,
                    knockbackForce = phase?.knockBackForce ?? 8f,
                    knockbackDrag = phase?.knockBackDrag ?? 5f,
                    hitParticleName = phase?.hitParticleName ?? "HeavyHit",
                    attacker = gameActor as MonsterActor,
                };

                damageable.TakeDamage(attackData);
            }

            // 착지 이펙트, 카메라 쉐이크 등은 여기서 호출
            // CameraManager.Instance.StartShake("HeavyHit");
            Debug.Log($"[FlyingBoss] Dive Impact! 반경 {radius}m, 히트 {hits.Length}개");
        }

        private EnemyAttackInfo FindDiveSkill()
        {
            if (_brain.Combat.AttackData == null) return null;
            foreach (var skill in _brain.Combat.AttackData.skills)
            {
                if (skill.isDiveAttack) return skill;
            }
            return null;
        }

        /// <summary>
        /// 지면 높이 계산. 플레이어/몬스터 Collider를 무시하고 순수 지형만 감지.
        /// </summary>
        private float GetGroundY(Vector3 pos)
        {
            // Ground/Environment 레이어만 대상. Player/Monster Collider 위에 착지하는 것을 방지.
            // 프로젝트 레이어 구성에 맞게 조정 필요.
            int groundMask = LayerMask.GetMask("Default", "Ground", "InteractableObject");
            if (groundMask == 0) groundMask = ~LayerMask.GetMask("Player", "Monster", "Enemy");

            if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f,
                    groundMask, QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return 0f;
        }
    }
}
