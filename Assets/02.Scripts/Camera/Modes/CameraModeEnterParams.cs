using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 카메라 모드 진입 시 전달하는 선택 파라미터.
    /// 모드별로 필요한 값만 사용한다.
    /// </summary>
    public class CameraModeEnterParams
    {
        public Transform PrimaryTarget;
        public Transform SecondaryTarget;
        public Vector3 WorldPosition;
        public Vector3 Offset;
        public float Duration;
        public AnimationCurve BlendCurve;
        public bool RestorePreviousOnExit = true;
        public CameraSnapshotProfile SnapshotProfile;
        public DialogueCameraRecordingSO DialogueRecording;

        /// <summary>대화 샷 요청. HasDialogueShot이 true일 때만 유효하다.</summary>
        public DialogueShotRequest DialogueShot;
        public bool HasDialogueShot;
        public bool HasSnapshotActorAnchorOverride;
        public CameraSnapshotActorReference SnapshotActorAnchor;
        public bool HasSnapshotLookAtTargetOverride;
        public CameraSnapshotActorReference SnapshotLookAtTarget;
        public System.Action OnComplete;
        public float FreeCameraMoveSpeed;
        public float FreeCameraLookSensitivity;

        public static readonly CameraModeEnterParams Empty = new CameraModeEnterParams();
    }
}
