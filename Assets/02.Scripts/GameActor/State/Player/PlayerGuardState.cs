using System.Collections;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.InputDefine;
using UPlayGround.Data.Combat;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Manager;
using UPlayGround.Manager.Handler;
using UPlayGround.Manager.Combat;
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
        private PlayerEquipment _equipment;
        private float _guardStartTime;
        private const float PERFECT_GUARD_WINDOW = 0.3f;
        private const float GuardBlockRecoilSpeed = 1.8f;
        private const float GuardBlockRecoilDuration = 0.16f;
        private const float HeavyGuardPushMultiplier = 1.2f;
        private Vector3 _guardBlockRecoilDirection;
        private float _guardBlockRecoilTimer;

        // 퍼펙트 가드 FOV 연출용 SO - CameraManager.SetPerfectGuardFOVData()로 주입받음
        public static FOVCameraEffectData PerfectGuardFOVData { get; private set; }

        /// <summary>
        /// CameraManager 초기화 시 Addressables로 로드한 SO를 주입.
        /// 씬 로드 전에 한 번만 호출하면 된다.
        /// </summary>
        public static void SetPerfectGuardFOVData(FOVCameraEffectData data)
        {
            PerfectGuardFOVData = data;
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

            Debug.Log("Player Guard Started");
            if (playerActor.Animator.HasMotion(AnimKey.Guard, true) == false)
            {
                TransitionToIdleOrMove();
                return;
            }

            _combat = playerActor.GetCombat();
            if (_combat == null)
            {
                TransitionToIdleOrMove();
                return;
            }
            
            // 가드 브레이크 쿨타임 중이면 가드 불가
            if (!_combat.CanGuard())
            {
                TransitionToIdleOrMove();
                return;
            }

            _combat.IsGuarding = true;
            _combat.OnGuardStart();
            _combat.RefreshCombatState();

            _equipment = playerActor.GetPlayerEquipment();
            _equipment?.SetMainWeaponDrawn(true);
            _guardStartTime = Time.time;
            _guardBlockRecoilDirection = Vector3.zero;
            _guardBlockRecoilTimer = 0f;

            playerActor.Animator.PlayMotion(AnimKey.Guard, 0.1f);
        }
        
        public override void OnExit(GameActorState toState)
        {
            if (_combat != null)
            {
                _combat.IsGuarding = false;
            }

            // Guard 모션의 InfiniteLoop(유지 구간) 해제 → 다음 모션으로 진행 허용
            playerActor.Animator.BreakAllInfiniteLoops();

            base.OnExit(toState);
        }
        
        public override void UpdateState(float deltaTime)
        {
            // Guard 유지 자체를 전투 의도로 본다.
            // 전투 타임아웃으로 예약된 납도 요청이 Guard 중 실행되는 것을 막는다.
            _combat?.RefreshCombatState();

            // 퍼펙트 가드 반격 창이 열려 있을 때 Attack 입력 → 반격 전환
            // 카운터 모션이 없으면 진입 자체를 막고 카운터 창/태그도 손대지 않는다.
            if (_combat.IsPerfectGuardCounterAvailable &&
                InputManager.Instance.InputBuffer.HasInput(PlayerAction.Attack))
            {
                playerActor.Tags?.AddTag(GameplayTagId.State_Combat_Counter);
                if (PlayerAttackState.TryEnter(playerController))
                {
                    InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack);
                    _combat.ClosePerfectGuardCounterWindow();
                    return;
                }
                // 진입 실패: 추가했던 카운터 태그를 원복.
                playerActor.Tags?.RemoveTag(GameplayTagId.State_Combat_Counter);
            }

            // Guard 입력을 떼면 Idle/Move로 복귀.
            // 카운터 입력 처리를 먼저 봐야 퍼펙트 가드 직후 가드를 놓고 공격해도 반격이 나간다.
            if (!playerController.HasGuardInput())
            {
                TransitionToIdleOrMove();
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

                if (_guardBlockRecoilTimer > 0f)
                {
                    float t = Mathf.Clamp01(_guardBlockRecoilTimer / GuardBlockRecoilDuration);
                    float speed = GuardBlockRecoilSpeed * t * t;
                    currentVelocity += _guardBlockRecoilDirection * speed;
                    _guardBlockRecoilTimer = Mathf.Max(0f, _guardBlockRecoilTimer - deltaTime);
                }
            }
        }
        
        /// <summary>
        /// 적의 공격이 Guard에 막혔을 때 호출 (PlayerActor.TakeDamage에서 호출)
        /// </summary>
        public void OnAttackBlocked(AttackData incomingAttack)
        {
            FaceIncomingAttack(incomingAttack);

            // 일반 가드 드롭 스폰
            Vector3 guardDropPos = gameActor.transform.position + gameActor.transform.forward;
            GameCombatManager.Instance.GameVitalOrb.TrySpawn(VitalOrbTrigger.Guard, guardDropPos);

            // 가드 브레이크 판정 (누적 횟수 초과 or 공격 자체가 GuardBreak)
            if (_combat.IsGuardBreak(incomingAttack))
            {
                TriggerGuardBreak();
                return;
            }
            
            // Just Guard (Perfect Guard) 타이밍 체크
            float timeSinceGuardStart = Time.time - _guardStartTime;
            bool isPerfectGuard = timeSinceGuardStart <= PERFECT_GUARD_WINDOW;
            
            var blockAnimState = playerActor.Animator.PlayMotion(AnimKey.Block, 0.05f, 0);
            BeginGuardBlockRecoil(incomingAttack, isPerfectGuard);
            
            blockAnimState.OwnedEvents.OnEnd = () =>
            {
                playerActor.Animator.PlayMotion(AnimKey.Guard, 0.1f, 0);
            };

            if (isPerfectGuard)
            {
                var defenseFeedback = GameCombatManager.Instance?.DefenseSuccessFeedback;

                // 공격자 경직 + 반격 창 열기 — Parryable 공격만 카운터 성립.
                // (GuardableOnly/Unblockable은 퍼펙트 가드 피드백은 받되 카운터는 열리지 않는다.)
                if (incomingAttack.defenseType == AttackDefenseType.Parryable)
                {
                    if (incomingAttack.attacker != null && incomingAttack.attacker.HasActorType(ActorType.Monster))
                    {
                        var monster = incomingAttack.attacker.GetComponent<MonsterActor>();
                        monster?.OnParried();
                    }

                    _combat.OpenPerfectGuardCounterWindow(
                        defenseFeedback?.GetCounterWindowDuration(DefenseSuccessType.PerfectGuard) ?? -1f);
                }

                Vector3 spawnPos = gameActor.transform.position + gameActor.transform.forward;
                defenseFeedback?.Play(
                    DefenseSuccessType.PerfectGuard,
                    new DefenseSuccessFeedbackContext(
                        playerActor,
                        incomingAttack?.attacker,
                        incomingAttack,
                        spawnPos));
            }
            else
            {
                var socketTM = playerActor.GetSocket(ActorSocketType.GuardPosition);
                GameObjectManager.Instance.ShowFX(FXKeyType.playerGuardFX, socketTM.position);
            }
        }

        private void BeginGuardBlockRecoil(AttackData incomingAttack, bool isPerfectGuard)
        {
            if (isPerfectGuard)
                return;

            Vector3 pushDirection = ResolveGuardPushDirection(incomingAttack);
            if (pushDirection.sqrMagnitude <= 0.0001f)
                return;

            float duration = GuardBlockRecoilDuration;
            if (IsHeavyGuardImpact(incomingAttack))
                duration *= HeavyGuardPushMultiplier;

            _guardBlockRecoilDirection = pushDirection;
            _guardBlockRecoilTimer = duration;
        }

        private Vector3 ResolveGuardPushDirection(AttackData incomingAttack)
        {
            Vector3 direction = Vector3.zero;

            if (incomingAttack?.attacker != null)
                direction = motor.TransientPosition - incomingAttack.attacker.transform.position;
            else if (incomingAttack != null && incomingAttack.attackDirection != Vector3.zero)
                direction = incomingAttack.attackDirection;
            else
                direction = -gameActor.transform.forward;

            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        }

        private static bool IsHeavyGuardImpact(AttackData incomingAttack)
        {
            if (incomingAttack == null)
                return false;

            return incomingAttack.reactionType is
                AttackReactionType.Heavy or
                AttackReactionType.KnockBack or
                AttackReactionType.Airborne or
                AttackReactionType.Knockdown or
                AttackReactionType.Stun or
                AttackReactionType.Grab;
        }

        /// <summary>
        /// 가드 브레이크 애니메이션 재생 후 Idle로 복귀
        /// </summary>
        private void TriggerGuardBreak()
        {
            _combat.OnGuardBreakConfirmed();
            _combat.ResetGuardCount();

            controller.TransitionToState(new PlayerGuardBreakState(controller));
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
