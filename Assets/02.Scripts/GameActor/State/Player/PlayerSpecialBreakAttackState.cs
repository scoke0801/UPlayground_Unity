using UnityEngine;
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
        private const float DEFAULT_FALLBACK_HIT_TIME = 0.15f;
        private const float DEFAULT_START_DISTANCE = 1.5f;
        private const float DEFAULT_SLIDE_DURATION = 0.25f;
        private const float DEFAULT_MAX_SLIDE_SPEED = 18f;

        private readonly Transform _target;
        private MonsterActor _targetMonster;
        private SpecialBreakAttackAsset _attackData;
        private bool _applied;
        private float _remainingDuration;
        private float _elapsedTime;
        private Vector3 _targetPosition;
        private bool _isSliding;
        private bool _cameraSequenceStarted;

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

            if (_targetMonster == null || _targetMonster.BreakGauge == null || !_targetMonster.BreakGauge.IsExposed)
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
                new EnemySpecialBreakVictimState(targetController, Duration));

            var animState = playerActor.Animator.PlayMotion(GetMotionKey(), 0.15f);
            if (animState != null)
                animState.OwnedEvents.OnEnd = Finish;
        }

        public override void OnExit(GameActorState toState)
        {
            if (_cameraSequenceStarted && _attackData?.cameraProfile != null)
                CameraManager.Instance.StopCameraSnapshotSequence(_attackData.cameraProfile);

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
            _applied = true;
            if (_targetMonster == null || !_targetMonster.IsAlive()) return;

            _targetMonster.OnTakeSpecialBreakAttack(
                playerActor,
                DamageByMaxHpRate,
                FixedDamage);

            if (HitStopDuration > 0f)
                GameCombatManager.Instance.GameHitStop.Execute(HitStopDuration, 0.02f);
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

            return playerActor.Animator.HasMotion(AnimKey.FinishAttack, true)
                ? AnimKey.FinishAttack
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

        private float Duration => _attackData != null ? Mathf.Max(0.1f, _attackData.duration) : DEFAULT_DURATION;
        private float FallbackHitTime => _attackData != null ? Mathf.Max(0f, _attackData.fallbackHitTime) : DEFAULT_FALLBACK_HIT_TIME;
        private float DamageByMaxHpRate => _attackData != null ? Mathf.Max(0f, _attackData.damageByMaxHpRate) : DEFAULT_DAMAGE_BY_MAX_HP_RATE;
        private float FixedDamage => _attackData != null ? Mathf.Max(0f, _attackData.fixedDamage) : 0f;
        private float HitStopDuration => _attackData != null ? Mathf.Max(0f, _attackData.hitStopDuration) : 0.08f;
        private float StartDistance => _attackData != null ? Mathf.Max(0f, _attackData.startDistance) : DEFAULT_START_DISTANCE;
        private float SlideDuration => _attackData != null ? Mathf.Max(0f, _attackData.slideDuration) : DEFAULT_SLIDE_DURATION;
        private float MaxSlideSpeed => _attackData != null ? Mathf.Max(0f, _attackData.maxSlideSpeed) : DEFAULT_MAX_SLIDE_SPEED;
    }
}
