using System.Collections;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.InputDefine;
using UPlayGround.Data.Combat;
using UPlayGround.Manager;
using UPlayGround.Manager.Handler;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 방어 상태
    /// </summary>
    public class PlayerGuardState : PlayerActorState
    {
        public override string StateName => "Guard";
        private PlayerCombat _combat;
        private float _guardStartTime;
        private const float PERFECT_GUARD_WINDOW = 0.8f;

        // 퍼펙트 가드 FOV 연출용 SO - CameraManager.SetPerfectGuardFOVData()로 주입받음
        private static FOVCameraEffectData _perfectGuardFOVData;

        /// <summary>
        /// CameraManager 초기화 시 Addressables로 로드한 SO를 주입.
        /// 씬 로드 전에 한 번만 호출하면 된다.
        /// </summary>
        public static void SetPerfectGuardFOVData(FOVCameraEffectData data)
        {
            _perfectGuardFOVData = data;
        }

        public PlayerGuardState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            // Guard 중에는 Hit 상태로 전환 불가 (Guard가 막아줌)
            if (stateName == "Hit")
                return false;
            return true;
        }
        
        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            if (playerActor.Animator.HasMotion(AnimKey.Guard, true) == false)
            {
                TransitionToIdleOrMove();
                return;
            }
            
            _combat = playerActor.GetCombat();
            _combat.IsGuarding = true;
            _guardStartTime = Time.time;

            playerActor.Animator.PlayMotion(AnimKey.Guard, 0.1f);
        }
        
        public override void OnExit(GameActorState toState)
        {
            _combat.IsGuarding = false;
            
            base.OnExit(toState);
        }
        
        public override void UpdateState(float deltaTime)
        {
            // Guard 입력을 떼면 Idle/Move로 복귀
            if (!playerController.HasGuardInput())
            {
                TransitionToIdleOrMove();
                return;
            }

            if (GameHitStopManager.Instance.IsHitStopping &&
                InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
            {
                playerController.TransitionToState(new PlayerAttackState(playerController));
                GameHitStopManager.Instance.Stop();
                return;
            }
            
            // 지면에서 떨어지면 Airborne 상태로 전환
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                playerController.TransitionToState(new PlayerAirborneState(playerController));
                return;
            }
        }
        
         public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // Lock-On 타겟이 있으면 타겟 방향으로 회전
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget != null)
            {
                Vector3 directionToTarget = (lockOnTarget.position - gameActor.transform.position).normalized;
                directionToTarget.y = 0f;
                
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    currentRotation = Quaternion.Slerp(currentRotation, targetRotation, deltaTime * 10f);
                }
            }
            else
            {
                // Guard 중에는 Look 방향으로 회전 (적을 바라보도록)
                Vector3 lookDirection = playerController.LookInputVector;
            
                if (lookDirection != Vector3.zero && controller.OrientationSharpness > 0f)
                {
                    Vector3 smoothedLookInputDirection = Vector3.Slerp(
                        motor.CharacterForward, 
                        lookDirection, 
                        1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime)).normalized;
                
                    currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, motor.CharacterUp);
                }
            }

            currentRotation = currentRotation.normalized;
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                // Guard 중에는 거의 정지 상태 유지
                Vector3 targetVelocity = Vector3.zero;
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
            }
        }
        
        /// <summary>
        /// 적의 공격이 Guard에 막혔을 때 호출 (PlayerActor.TakeDamage에서 호출)
        /// </summary>
        public void OnAttackBlocked(AttackData incomingAttack)
        {
            // 일반 가드 드롭 스폰 - 플레이어 전방 1m
            Vector3 guardDropPos = gameActor.transform.position + gameActor.transform.forward;
            VitalOrbManager.Instance.TrySpawn(VitalOrbTrigger.Guard, guardDropPos);

            // Guard Break 공격인지 확인
            if (_combat.IsGuardBreak(incomingAttack))
            {
                // Guard Break 애니메이션 재생 후 Hit 상태로
                var animState = playerActor.Animator.PlayMotion(AnimKey.Knockback, 0.1f, 0);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd = () =>
                    {
                        controller.TransitionToState(new PlayerIdleState(controller));
                    };
                }
                return;
            }
            
            // Just Guard (Perfect Guard) 타이밍 체크
            float timeSinceGuardStart = Time.time - _guardStartTime;
            bool isPerfectGuard = timeSinceGuardStart <= PERFECT_GUARD_WINDOW;
            
            var blockAnimState = playerActor.Animator.PlayMotion(AnimKey.Block, 0.05f, 0);
            
            playerController.AddVelocity(incomingAttack.attackDirection.normalized * 2.0f);
            
            blockAnimState.OwnedEvents.OnEnd = () =>
            {
                playerActor.Animator.PlayMotion(AnimKey.Guard, 0.1f, 0);
            };

            if (isPerfectGuard)
            {
                // 퍼펙트 가드 드롭 스폰 - 플레이어 전방 1m
                Vector3 spawnPos = gameActor.transform.position + gameActor.transform.forward;
                VitalOrbManager.Instance.TrySpawn(VitalOrbTrigger.PerfectGuard, spawnPos);
                GameHitStopManager.Instance.Execute(GameHitStopManager.HitStopIntensity.PlayerGuard);

                // 퍼펙트 가드 카메라 연출 (기획서 §7.2)
                // CriticalHit 쉐이크 + FOV -8° 줌인. 슬로우 종료 후 FOV 원복은 별도 이펙트 SO로 처리.
                CameraManager.Instance?.StartShake("CriticalHit");
                CameraManager.Instance?.PlayEffect(_perfectGuardFOVData);

                if (incomingAttack.attacker != null && incomingAttack.attacker.HasActorType(ActorType.Monster))
                {
                    var attackerController = incomingAttack.attacker.ActorController;
                    if (attackerController != null)
                    {
                        // 퍼펙트 가드 성공 시 공격자를 강제로 Hit 상태로 전환 (공격 끊기)
                        // attackerController.TransitionToState(new EnemyHitState(attackerController, new AttackData()
                        // {
                        //     attacker = playerActor,
                        //     reactionType = AttackReactionType.Hit,
                        //     attackDirection = -incomingAttack.attackDirection
                        // }));
                        Debug.Log($"[PerfectGuard] {incomingAttack.attacker.name}의 공격을 끊었습니다!");
                    }
                }
            }
            else
            {
                var socketTM = playerActor.GetSocket(ActorSocketType.GuardPosition);
                
                GameObjectManager.Instance.ShowFX("playerGuardFX", socketTM.position);
            }
        }
        
        /// <summary>
        /// Counter Attack (슈퍼 공격) 실행
        /// </summary>
        private void ExecuteCounterAttack()
        {
            Debug.Log("[PlayerGuardState] Counter Attack 실행!");
            
            // Counter Attack State로 전환하거나
            // 바로 공격 실행
            controller.TransitionToState(new PlayerAttackState(controller));
            
            // 또는 특별한 Counter 공격 실행
            // AttackData counterAttack = _combat.ExecuteCounterAttack();
        }
        
        private void TransitionToIdleOrMove()
        {
            if (playerController.HasMoveInput())
            {
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            }
            else
            {
                controller.TransitionToState(new PlayerIdleState(controller));
            }
        }
    }
}