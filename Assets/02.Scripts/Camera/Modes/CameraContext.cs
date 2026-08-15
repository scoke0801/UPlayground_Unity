using System;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 카메라 모드가 참조하는 런타임 의존성 묶음.
    /// CameraManager 내부 필드를 직접 노출하지 않기 위한 중간 계층이다.
    /// </summary>
    public sealed class CameraContext
    {
        public Camera MainCamera { get; set; }
        public Transform Target { get; set; }
        public Transform CameraPivot { get; set; }
        public CameraSettings Settings { get; set; }
        public DialogueCameraSettingsSO DialogueSettings { get; set; }

        /// <summary>진행 중인 대화 세션의 연출 상태(가상선·인트로 소진·직전 샷). 대화 중이 아니면 null.</summary>
        public DialogueShotSession DialogueSession { get; set; }
        public CameraState State { get; }
        public CameraLockOn LockOn { get; set; }
        public CameraCollision Collision { get; set; }
        public CameraDistanceController DistanceController { get; set; }
        public CameraRotationTransition RotationTransition { get; set; }
        public Func<bool> CombatStateProvider { get; set; }
        public CameraMotionContext Motion { get; set; }
        public float LastManualInputTime { get; set; }
        public Action StartCameraAlign { get; set; }
        public Action NotifyManualCameraInput { get; set; }
        public Func<CameraModeEnterParams, bool> PopCameraMode { get; set; }
        public Transform LookAtOverride { get; set; }
        public Vector3 LookAtOverrideOffset { get; set; }
        public LayerMask CollisionLayers { get; set; }
        public bool IsInputLocked { get; set; }
        public bool IsAligning { get; set; }
        public float AlignTimer { get; set; }
        public bool HasActiveEffects { get; set; }
        public CameraModeEnterParams ActiveEnterParams { get; set; }

        public CameraContext(CameraState state)
        {
            State = state;
        }
    }
}
