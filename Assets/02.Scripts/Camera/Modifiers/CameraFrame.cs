using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>Modifier들이 한 프레임의 카메라 상태와 포즈를 순서대로 계산하는 작업 단위.</summary>
    public struct CameraFrame
    {
        public CameraContext Context;
        public CameraState State;
        public CameraEffectState Effects;
        public CameraPose Pose;
        public float DeltaTime;

        /// <summary>락온 해제·정렬 중에도 피벗의 위치 보간을 유지한다.</summary>
        public bool KeepPositionSmoothing;

        /// <summary>지형 보정 전 추적 기준 위치. 벽에 밀린 피벗이 락온 회전에 역류하지 않게 한다.</summary>
        public Vector3 PivotBase;

        /// <summary>프레이밍에서 허용한 거리 상한. 충돌 단계는 이 범위 안에서 암을 접는다.</summary>
        public float DistanceCeiling;
    }
}
