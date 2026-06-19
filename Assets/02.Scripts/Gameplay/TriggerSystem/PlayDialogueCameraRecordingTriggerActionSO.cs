using System.Collections;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data;
using UPlayGround.Manager;

namespace UPlayGround.TriggerSystem
{
    /// <summary>
    /// 대화 카메라 사전 녹화를 트리거로 재생한다(대화 밖 독립 컷신용).
    /// Main 대화 중에는 노드별 카메라 push가 덮으므로, 대화 통합은 DialogueNodeSO.cameraRecording을 사용한다.
    /// 구조는 PlayCameraSnapshotTriggerActionSO와 동일.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/트리거/액션/Play Dialogue Camera Recording")]
    public sealed class PlayDialogueCameraRecordingTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private DialogueCameraRecordingSO _recording;
        [SerializeField] private bool _overrideActorAnchor = false;
        [SerializeField] private CameraSnapshotActorReference _actorAnchor = CameraSnapshotActorReference.ActivePlayer();
        [SerializeField] private bool _waitForCompleted = false;
        [SerializeField] private bool _invokeSceneRelayEvents = false;

        public override bool CanExecute(TriggerContext context)
        {
            return _recording != null && _recording.SampleCount > 0 && CameraManager.Instance != null;
        }

        public override bool ConsumesTrigger(TriggerContext context)
        {
            return context != null && context.ActionConsumesTrigger;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            bool started = false;
            bool completed = false;
            var relay = _invokeSceneRelayEvents && context?.Composer != null
                ? context.Composer.GetComponent<TriggerUnityEventRelay>()
                : null;

            // 콜백이 Push 내부에서 동기 호출될 수 있다. started 이전이면 완료 플래그만 세우고
            // Completed 발화는 아래로 미뤄 Started → Completed 순서를 보장한다.
            bool played = CameraManager.Instance != null && CameraManager.Instance.PushDialogueCameraRecording(
                _recording,
                _overrideActorAnchor ? _actorAnchor : (CameraSnapshotActorReference?)null,
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

            if (completed)
                relay?.InvokeCompleted();

            if (!_waitForCompleted)
                yield break;

            while (!completed)
                yield return null;
        }
    }
}
