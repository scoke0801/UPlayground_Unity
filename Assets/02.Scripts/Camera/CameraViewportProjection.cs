using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>렌더 결과나 추적 상태 없이 지정한 포즈의 화면 좌표를 계산한다.</summary>
    public static class CameraViewportProjection
    {
        /// <summary>카메라 포즈에서 월드 위치를 뷰포트로 투영한다.</summary>
        public static bool TryProject(in CameraPose pose, Vector3 focus, float aspect, out Vector2 viewport)
        {
            Vector3 local = Quaternion.Inverse(pose.CameraRotation) * (focus - pose.CameraPosition);
            viewport = Vector2.zero;
            if (local.z <= 0.001f) return false;
            float tangent = Mathf.Tan(Mathf.Clamp(pose.FieldOfView, 1f, 179f) * 0.5f * Mathf.Deg2Rad);
            viewport = new Vector2(0.5f + local.x / (2f * local.z * tangent * Mathf.Max(0.01f, aspect)),
                0.5f + local.y / (2f * local.z * tangent));
            return true;
        }
    }
}
