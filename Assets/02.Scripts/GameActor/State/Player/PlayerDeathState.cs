using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 몬스터 사망 상태
    /// </summary>
    public class PlayerDeathState : PlayerActorState
    {
        public override string StateName => "Death";
        
        public PlayerDeathState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        private Vector3    _deathPosition;
        private Quaternion _deathRotation;
        private Coroutine  _autoSwitchDelayRoutine;
        private MotionSet  _playedMotionSet;
        private float      _deathMotionTimeout;
        private float      _elapsedUnscaled;
        private bool       _deathSequenceStarted;

        private const float AutoSwitchDelayAfterDeathMotion = 0.45f;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Abilities?.HandleOwnerDeath();

            // 워프 진행 중이면 즉시 clear (사망 모션이 우선).
            controller.MotionWarp?.ClearTarget();

            _deathPosition = gameActor.transform.position;
            _deathRotation = gameActor.transform.rotation;

            var state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Die, 0.25f);
            if (state != null)
            {
                _playedMotionSet = gameActor.Animator.CurrentMotionSet;
                _deathMotionTimeout = Mathf.Max(
                    0.5f,
                    (_playedMotionSet?.TotalDuration ?? 2.5f) + 0.25f);
                gameActor.Animator.OnMotionSetEndedWithReason += OnMotionSetEnded;
                state.OwnedEvents.OnEnd = () => BeginDeathSequence(state);
            }
            else
            {
                BeginDeathSequence(null);
            }
        }

        private void OnMotionSetEnded(MotionSet motionSet, MotionSetEndReason reason)
        {
            if (!ReferenceEquals(motionSet, _playedMotionSet))
                return;

            BeginDeathSequence(null);
        }

        private void BeginDeathSequence(Animancer.AnimancerState deathState)
        {
            if (_deathSequenceStarted)
                return;

            _deathSequenceStarted = true;
            gameActor.Animator.OnMotionSetEndedWithReason -= OnMotionSetEnded;

            if (deathState != null)
                deathState.Speed = 0f;

            _autoSwitchDelayRoutine = controller.StartCoroutine(SwitchOrShowRespawnAfterDelay());
        }

        private IEnumerator SwitchOrShowRespawnAfterDelay()
        {
            yield return new WaitForSecondsRealtime(AutoSwitchDelayAfterDeathMotion);
            _autoSwitchDelayRoutine = null;

            if (controller.CurrentState != this)
                yield break;

            TrySwitchOrShowRespawn();
        }

        private void TrySwitchOrShowRespawn()
        {
            var partyManager = Svc.Party;
            if (partyManager != null && partyManager.TrySwitchToNextAliveAfterActiveDeath())
            {
                return;
            }

            if (ActorSvc.CycleRemains?.HandlePartyWipe(_deathPosition, _deathRotation) == true)
                return;

            ShowRespawnUI();
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetEndedWithReason -= OnMotionSetEnded;

            if (_autoSwitchDelayRoutine != null)
            {
                controller.StopCoroutine(_autoSwitchDelayRoutine);
                _autoSwitchDelayRoutine = null;
            }

            base.OnExit(toState);
        }

        private void ShowRespawnUI()
        {
            // 가장 가까운 포탈 위치 계산
            var portals    = UnityEngine.Object.FindObjectsByType<PortalActor>(FindObjectsSortMode.None);
            var portalPos  = _deathPosition;
            var portalRot  = _deathRotation;

            if (portals.Length > 0)
            {
                PortalActor nearest = null;
                float       minDist = float.MaxValue;

                foreach (var portal in portals)
                {
                    float dist = Vector3.Distance(_deathPosition, portal.transform.position);
                    if (dist < minDist) { minDist = dist; nearest = portal; }
                }

                if (nearest != null)
                    (portalPos, portalRot) = nearest.GetArrivalPoint();
            }

            var actor = playerActor ?? gameActor as PlayerActor;

            if (ActorSvc.UI == null)
            {
                actor?.Respawn(portalPos, portalRot, 1f);
                return;
            }

            ActorSvc.UI.ShowRespawn(
                spotHealPercent => actor?.Respawn(_deathPosition, _deathRotation, spotHealPercent),
                () => actor?.Respawn(portalPos, portalRot, 1f));
        }

        public override void UpdateState(float deltaTime)
        {
            if (_deathSequenceStarted)
                return;

            // 모션 종료 콜백이 교체/종료 순서 때문에 유실돼도 부활 흐름은 반드시 진행한다.
            _elapsedUnscaled += Time.unscaledDeltaTime;
            if (_elapsedUnscaled >= _deathMotionTimeout)
                BeginDeathSequence(null);
        }
        
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // Idle 상태에서는 회전 유지 (또는 부드럽게 정면으로)
            currentRotation = currentRotation.normalized;
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity.x = 0;
            currentVelocity.z = 0;
        }
    }
}
