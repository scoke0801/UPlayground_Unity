using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
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

        private const float AutoSwitchDelayAfterDeathMotion = 0.45f;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            // 워프 진행 중이면 즉시 clear (사망 모션이 우선).
            controller.MotionWarp?.ClearTarget();

            _deathPosition = gameActor.transform.position;
            _deathRotation = gameActor.transform.rotation;

            var state = gameActor.Animator.PlayMotion(AnimKey.Die, 0.25f);
            if (state != null)
            {
                state.OwnedEvents.OnEnd = () => OnDeathMotionEnd(state);
            }
        }

        private void OnDeathMotionEnd(Animancer.AnimancerState deathState)
        {
            if (deathState != null)
                deathState.Speed = 0f;

            _autoSwitchDelayRoutine = controller.StartCoroutine(SwitchOrShowRespawnAfterDelay());
        }

        private IEnumerator SwitchOrShowRespawnAfterDelay()
        {
            yield return new WaitForSeconds(AutoSwitchDelayAfterDeathMotion);
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
            if (motor.GroundingStatus.IsStableOnGround)
            {
                // Gravity
                currentVelocity += controller.Gravity * deltaTime;
            }
        }
    }
}
