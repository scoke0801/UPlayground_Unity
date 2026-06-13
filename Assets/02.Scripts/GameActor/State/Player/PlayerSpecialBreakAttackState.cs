using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Manager.Combat;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class PlayerSpecialBreakAttackState : PlayerActorState
    {
        public override string StateName => "SpecialBreakAttack";

        private const float DEFAULT_DURATION = 1.2f;
        private const float DEFAULT_DAMAGE_BY_MAX_HP_RATE = 0.2f;
        // 폴백은 임팩트 모션 이벤트가 끝내 발화하지 않을 때를 위한 백스톱이므로
        // duration에 근접한 늦은 시각으로 잡아 이벤트가 항상 우선되게 한다.
        private const float DEFAULT_FALLBACK_HIT_TIME = 1.0f;
        private const float DEFAULT_START_DISTANCE = 1.5f;
        private const float DEFAULT_SLIDE_DURATION = 0.25f;
        private const float DEFAULT_MAX_SLIDE_SPEED = 18f;

        // 처형(FinishAttack)과 동일하게, break 공격 중 주변 적의 AI를 정지시키는 반경
        private const float FREEZE_RADIUS = 15f;

        private readonly Transform _target;
        private MonsterActor _targetMonster;
        private SpecialBreakAttackAsset _attackData;
        private bool _applied;
        private float _remainingDuration;
        private float _elapsedTime;
        private Vector3 _targetPosition;
        private bool _isSliding;
        private bool _cameraSequenceStarted;
        private readonly List<IEnemyAIController> _frozenEnemyControllers = new List<IEnemyAIController>();

        public PlayerSpecialBreakAttackState(ActorMovementController controller, Transform target) : base(controller)
        {
            _target = target;
        }

        public override bool CanTransitionState(string stateName) => stateName is "Death" or "Hit" or "Grabbed";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _targetMonster = _target != null
                ? _target.GetComponent<MonsterActor>() ?? _target.GetComponentInParent<MonsterActor>()
                : null;

            if (_targetMonster == null
                || !_targetMonster.CanTakeDamage()
                || _targetMonster.BreakGauge == null
                || !_targetMonster.BreakGauge.IsExposed)
            {
                controller.TransitionToState(new PlayerIdleState(controller));
                return;
            }

            controller.MotionWarp?.ClearTarget();
            var combat = playerActor.GetCombat();
            combat?.RefreshCombatState();
            _attackData = combat?.SpecialBreakAttackData;
            combat?.SetupSpecialBreakAttackData(_attackData, _targetMonster);

            _elapsedTime = 0f;
            _remainingDuration = Duration;
            _applied = false;
            _cameraSequenceStarted = false;

            playerActor.GetPlayerEquipment()?.SetMainWeaponDrawn(true);
            FaceTarget();
            PrepareSlideTarget();
            PlayCameraSequence();

            var targetController = _targetMonster.GetComponent<ActorMovementController>();
            targetController?.TransitionToState(
                new EnemySpecialBreakVictimState(
                    targetController,
                    Duration,
                    playerActor.transform,
                    VictimKnockbackDistance,
                    VictimKnockbackDuration,
                    VictimMaxKnockbackSpeed));

            var animState = playerActor.Animator.PlayMotion(GetMotionKey(), 0.15f);
            if (animState != null)
                animState.OwnedEvents.OnEnd = Finish;

            FreezeSurroundingEnemies();
        }

        public override void OnExit(GameActorState toState)
        {
            if (_cameraSequenceStarted && _attackData?.cameraProfile != null)
                CameraManager.Instance.StopCameraSnapshotSequence(_attackData.cameraProfile);

            UnfreezeSurroundingEnemies();

            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            _elapsedTime += deltaTime;

            if (!_applied && _elapsedTime >= FallbackHitTime)
                ApplySpecialBreakAttack();

            _remainingDuration -= deltaTime;
            if (_remainingDuration <= 0f)
                Finish();
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_targetMonster == null)
            {
                currentRotation = currentRotation.normalized;
                return;
            }

            Vector3 dir = _targetMonster.transform.position - gameActor.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                currentRotation = Quaternion.LookRotation(dir.normalized).normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_isSliding && _targetMonster != null)
            {
                Vector3 toTarget = _targetPosition - gameActor.transform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude > 0.0025f && _elapsedTime < SlideDuration)
                {
                    float speed = Mathf.Clamp(toTarget.magnitude * 12f, 0f, MaxSlideSpeed);
                    currentVelocity = toTarget.normalized * speed;
                    return;
                }

                _isSliding = false;
            }

            if (!motor.GroundingStatus.IsStableOnGround) return;
            currentVelocity = Vector3.Lerp(
                currentVelocity,
                Vector3.zero,
                1f - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        public void ApplySpecialBreakAttackFromMotionEvent()
        {
            ApplySpecialBreakAttack();
        }

        private void ApplySpecialBreakAttack()
        {
            // 모션 이벤트와 폴백 타이머가 동일 임팩트를 중복 적용하는 것을 차단.
            if (_applied) return;
            _applied = true;
            if (_targetMonster == null || !_targetMonster.IsAlive()) return;

            _targetMonster.OnTakeSpecialBreakAttack(
                playerActor,
                DamageByMaxHpRate,
                FixedDamage,
                MinReferenceHealth);

            CombatFeedbackDispatcher.ApplyPlayerSpecialBreakImpactFeedback(
                playerActor,
                _targetMonster,
                _targetMonster.transform.position,
                HitStopDuration,
                HitStopScale,
                GlobalHitStopDuration,
                GlobalHitStopScale,
                CameraShakeKey,
                CameraPunchStrength,
                CameraPunchDuration);
        }

        private void FaceTarget()
        {
            if (_targetMonster == null) return;
            Vector3 dir = _targetMonster.transform.position - playerActor.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                playerActor.transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        private AnimKey GetMotionKey()
        {
            AnimKey configuredKey = _attackData != null ? _attackData.animKey : AnimKey.None;
            if (configuredKey != AnimKey.None && playerActor.Animator.HasMotion(configuredKey, true))
                return configuredKey;

            return playerActor.Animator.HasMotion(AnimKey.BreakAttack, true)
                ? AnimKey.BreakAttack
                : AnimKey.Attack_1;
        }

        private void PrepareSlideTarget()
        {
            if (_targetMonster == null) return;

            Vector3 dirFromTarget = playerActor.transform.position - _targetMonster.transform.position;
            dirFromTarget.y = 0f;
            if (dirFromTarget.sqrMagnitude <= 0.001f)
                dirFromTarget = -_targetMonster.transform.forward;

            _targetPosition = _targetMonster.transform.position + dirFromTarget.normalized * StartDistance;
            _isSliding = true;
        }

        private void PlayCameraSequence()
        {
            if (_attackData?.cameraProfile == null)
                return;

            _cameraSequenceStarted = CameraManager.Instance.PushCameraSnapshotSequence(
                _attackData.cameraProfile,
                CameraSnapshotActorReference.ActivePlayer(ActorSocketType.Center),
                new CameraSnapshotActorReference
                {
                    enabled = true,
                    useActivePlayerWhenEmpty = false,
                    actorIdType = ActorIdType.None,
                    actorId = _targetMonster != null ? _targetMonster.ActorId : string.Empty,
                    socketType = ActorSocketType.Center
                });
        }

        private void Finish()
        {
            if (playerController.HasMoveInput())
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            else
                controller.TransitionToState(new PlayerIdleState(controller));
        }

        /// <summary>
        /// 처형(FinishAttack)과 동일하게 주변 적의 AI를 정지시킨다.
        /// 단, break 타겟 몬스터는 EnemySpecialBreakVictimState로 별도 연출 중이므로 제외한다
        /// (Freeze 시 EnemyIdleState로 강제 전환되어 피격 연출이 덮어써짐).
        /// </summary>
        private void FreezeSurroundingEnemies()
        {
            var combat = playerActor.GetCombat();
            if (combat == null)
                return;

            combat.FillEnemyAIControllersInRadius(FREEZE_RADIUS, _frozenEnemyControllers);

            var targetController = _targetMonster != null ? _targetMonster.AIController : null;
            for (int i = _frozenEnemyControllers.Count - 1; i >= 0; i--)
            {
                var brain = _frozenEnemyControllers[i];
                if (!IsValidAIController(brain) || ReferenceEquals(brain, targetController))
                {
                    _frozenEnemyControllers.RemoveAt(i);
                    continue;
                }

                brain.Freeze();
            }
        }

        private void UnfreezeSurroundingEnemies()
        {
            foreach (var brain in _frozenEnemyControllers)
            {
                if (IsValidAIController(brain))
                    brain.Unfreeze();
            }
            _frozenEnemyControllers.Clear();
        }

        private static bool IsValidAIController(IEnemyAIController controller)
        {
            return controller is UnityEngine.Object unityObject && unityObject != null;
        }

        private float Duration => _attackData != null ? Mathf.Max(0.1f, _attackData.duration) : DEFAULT_DURATION;
        private float FallbackHitTime => _attackData != null ? Mathf.Max(0f, _attackData.fallbackHitTime) : DEFAULT_FALLBACK_HIT_TIME;
        private float DamageByMaxHpRate => _attackData != null ? Mathf.Max(0f, _attackData.damageByMaxHpRate) : DEFAULT_DAMAGE_BY_MAX_HP_RATE;
        private float FixedDamage => _attackData != null ? Mathf.Max(0f, _attackData.fixedDamage) : 0f;
        private float MinReferenceHealth => _attackData != null ? Mathf.Max(0f, _attackData.minReferenceHealth) : 0f;
        private float HitStopDuration => _attackData != null ? Mathf.Max(0f, _attackData.hitStopDuration) : 0.08f;
        private float StartDistance => _attackData != null ? Mathf.Max(0f, _attackData.startDistance) : DEFAULT_START_DISTANCE;
        private float SlideDuration => _attackData != null ? Mathf.Max(0f, _attackData.slideDuration) : DEFAULT_SLIDE_DURATION;
        private float MaxSlideSpeed => _attackData != null ? Mathf.Max(0f, _attackData.maxSlideSpeed) : DEFAULT_MAX_SLIDE_SPEED;
        private float VictimKnockbackDistance => _attackData != null ? Mathf.Max(0f, _attackData.victimKnockbackDistance) : 0.75f;
        private float VictimKnockbackDuration => _attackData != null ? Mathf.Max(0f, _attackData.victimKnockbackDuration) : 0.18f;
        private float VictimMaxKnockbackSpeed => _attackData != null ? Mathf.Max(0f, _attackData.victimMaxKnockbackSpeed) : 7f;
        private float HitStopScale => _attackData != null ? Mathf.Clamp(_attackData.hitStopScale, 0.001f, 1f) : 0.01f;
        private float GlobalHitStopDuration => _attackData != null ? Mathf.Max(0f, _attackData.globalHitStopDuration) : 0.055f;
        private float GlobalHitStopScale => _attackData != null ? Mathf.Clamp(_attackData.globalHitStopScale, 0.001f, 1f) : 0.02f;
        private UPlayGround.Data.Path.CameraShakeIdType CameraShakeKey => _attackData != null ? _attackData.cameraShakeKey : UPlayGround.Data.Path.CameraShakeIdType.CriticalHit;
        private float CameraPunchStrength => _attackData != null ? Mathf.Max(0f, _attackData.cameraPunchStrength) : 0.26f;
        private float CameraPunchDuration => _attackData != null ? Mathf.Max(0f, _attackData.cameraPunchDuration) : 0.16f;
    }
}
