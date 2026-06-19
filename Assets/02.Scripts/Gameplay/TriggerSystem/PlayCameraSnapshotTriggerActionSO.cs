using System.Collections;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data;
using UPlayGround.Manager;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/트리거/액션/Play Camera Snapshot Sequence")]
    public sealed class PlayCameraSnapshotTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private CameraSnapshotProfile _profile;
        [SerializeField] private bool _overrideActorAnchor = false;
        [SerializeField] private CameraSnapshotActorReference _actorAnchor = CameraSnapshotActorReference.ActivePlayer();
        [SerializeField] private bool _overrideLookAtTarget = false;
        [SerializeField] private CameraSnapshotActorReference _lookAtTarget = CameraSnapshotActorReference.None();
        [SerializeField] private bool _waitForCompleted = false;
        [SerializeField] private bool _invokeSceneRelayEvents = false;

        public override bool CanExecute(TriggerContext context)
        {
            return _profile != null && CameraManager.Instance != null;
        }

        // 재생에 성공했을 때만 발화를 소모(Once에서 잠금)한다. 결과는 컨텍스트에 실어 SO 공유 상태를 피한다.
        public override bool ConsumesTrigger(TriggerContext context)
        {
            return context != null && context.ActionConsumesTrigger;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            bool started   = false;
            bool completed = false;
            var relay = _invokeSceneRelayEvents && context?.Composer != null
                ? context.Composer.GetComponent<TriggerUnityEventRelay>()
                : null;

            // 콜백은 Push 내부에서 동기 호출될 수 있다. started 이전이면 완료 플래그만 세우고
            // Completed 발화는 아래로 미뤄 Started → Completed 순서를 보장한다.
            bool played = CameraManager.Instance != null && CameraManager.Instance.PushCameraSnapshotSequence(
                _profile,
                _overrideActorAnchor ? _actorAnchor : null,
                _overrideLookAtTarget ? _lookAtTarget : null,
                () =>
                {
                    completed = true;
                    if (started)
                        relay?.InvokeCompleted();
                });

            if (context != null)
                context.ActionConsumesTrigger = played;

            if (!played)
                yield break;

            started = true;
            relay?.InvokeStarted();

            // 동기 완료(콜백이 Push 도중 실행됨) 케이스: Started 직후 Completed를 발화한다.
            if (completed)
                relay?.InvokeCompleted();

            if (!_waitForCompleted)
                yield break;

            while (!completed)
                yield return null;
        }
    }
}
