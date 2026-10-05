using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 모드가 산출하는 기본 카메라 포즈.
    /// CameraEffectState는 이 포즈 위에 합성된다.
    /// </summary>
    public struct CameraPose
    {
        public Vector3 PivotPosition;
        public Vector3 CameraPosition;
        /// <summary>시선 보정과 연출을 포함한 최종 회전. 공전 각도로 역산해 누적하지 않는다.</summary>
        public Quaternion CameraRotation;
        /// <summary>위치 배치의 기준 공전각. 최종 시선과 독립적으로 유지한다.</summary>
        public float Yaw;
        public float Pitch;
        public float Distance;
        public float FieldOfView;

        public static CameraPose FromCamera(Camera camera, Transform pivot, float yaw, float pitch, float distance)
        {
            return new CameraPose
            {
                PivotPosition = pivot != null ? pivot.position : Vector3.zero,
                CameraPosition = camera != null ? camera.transform.position : Vector3.zero,
                CameraRotation = camera != null ? camera.transform.rotation : Quaternion.identity,
                Yaw = yaw,
                Pitch = pitch,
                Distance = distance,
                FieldOfView = camera != null ? camera.fieldOfView : 60f
            };
        }
    }
}
