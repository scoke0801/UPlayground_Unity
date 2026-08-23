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
        public override ActorStateId StateId => ActorStateId.Death;

        public override bool BlocksExitTo(GameActorState newState)
            => playerActor != null
               && !playerActor.IsAlive()
               && newState?.StateId != ActorStateId.Death;
        
        public PlayerDeathState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            return true;
        }

        private Vector3    _deathPosition;
        private Quaternion _deathRotation;
        private Coroutine  _autoSwitchDelayRoutine;
        private bool       _completionSubscribed;
        private bool       _respawnFlowStarted;

        private const float AutoSwitchDelayAfterDeathMotion = 0.45f;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Abilities?.HandleOwnerDeath();

            // 워프 진행 중이면 즉시 clear (사망 모션이 우선).
            controller.MotionWarp?.ClearTarget();

            _deathPosition = gameActor.transform.position;
            _deathRotation = gameActor.transform.rotation;
            _respawnFlowStarted = false;

            var state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Die, 0.25f);
            if (state != null)
            {
                // MotionSet 타임라인은 Animancer OnEnd를 쓰지 않는다(ActorAnimator가 매 클립 전환마다 null로 지운다).
                // 종료 신호는 다른 상태와 동일하게 OnMotionSetCompleted로 받는다.
                gameActor.Animator.OnMotionSetCompleted += OnDeathMotionEnd;
                _completionSubscribed = true;
            }
            else
            {
                // Die 모션 미등록 → 사망 플로우가 영구 정지하지 않도록 즉시 진행한다.
                OnDeathMotionEnd();
            }
        }

        private void OnDeathMotionEnd()
        {
            UnsubscribeMotionCompletion();

            if (_respawnFlowStarted) return;
            _respawnFlowStarted = true;

            // 완료된 MotionSet은 마지막 Base 포즈를 유지하므로 별도 정지 처리가 필요 없다.
            _autoSwitchDelayRoutine = controller.StartCoroutine(SwitchOrShowRespawnAfterDelay());
        }

        private void UnsubscribeMotionCompletion()
        {
            if (!_completionSubscribed) return;
            _completionSubscribed = false;

            if (gameActor != null && gameActor.Animator != null)
                gameActor.Animator.OnMotionSetCompleted -= OnDeathMotionEnd;
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

            ShowRespawnUI();
        }

        public override void OnExit(GameActorState toState)
        {
            UnsubscribeMotionCompletion();

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
        }
    }
}
