using UnityEngine;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "ZoomCameraEffect", menuName = "UPlayGround/SO/CameraEffect/Zoom")]
    public class ZoomCameraEffectData : CameraEffectData
    {
        [Header("Zoom Settings")]
        [Tooltip("거리 변화량 (음수=줌인, 양수=줌아웃)")]
        public float distanceDelta = -2f;

        [Tooltip("줌 중 카메라 오프셋 변화량 (선택)")]
        public Vector3 offsetDelta = Vector3.zero;

        [Tooltip("줌 진행 커브 (x=정규화 시간, y=적용 비율)")]
        public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public override ICameraEffect CreateEffect() => new ZoomCameraEffect(this);
    }
}
