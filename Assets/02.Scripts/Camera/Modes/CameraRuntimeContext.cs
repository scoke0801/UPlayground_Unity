using System;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 카메라 모드가 참조하는 런타임 의존성 묶음.
    /// CameraManager 내부 필드를 직접 노출하지 않기 위한 중간 계층이다.
    /// </summary>
    public sealed class CameraRuntimeContext
    {
        public Camera MainCamera { get; set; }
        public Transform Target { get; set; }
        public Transform CameraPivot { get; set; }
        public CameraSettings Settings { get; set; }
        public DialogueCameraSettingsSO DialogueSettings { get; set; }
        public CameraRigState State { get; }
        public CameraLockOn LockOn { get; set; }
        public CameraCollision Collision { get; set; }
        public CameraDistanceController DistanceController { get; set; }
        public CameraRotationTransition RotationTransition { get; set; }
        public Func<bool> CombatStateProvider { get; set; }
        public Func<float> ComputeSlopePitchOffset { get; set; }
        public Action StartCameraAlign { get; set; }
        public Transform LookAtOverride { get; set; }
        public Vector3 LookAtOverrideOffset { get; set; }
        public LayerMask CollisionLayers { get; set; }
        public CapsuleCollider CharacterCapsule { get; set; }
        public bool IsInputLocked { get; set; }
        public bool IsAligning { get; set; }
        public float AlignTimer { get; set; }
        public bool HasActiveEffects { get; set; }

        public CameraRuntimeContext(CameraRigState state)
        {
            State = state;
        }
    }
}
