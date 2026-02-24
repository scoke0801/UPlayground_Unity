using UnityEngine;

namespace UPlayGround.Data
{
    /// <summary>
    /// 킬캠 연출 파라미터
    /// ScriptableObject로 분리하여 에디터에서 튜닝 가능
    /// </summary>
    [CreateAssetMenu(fileName = "KillCamData", menuName = "UPlayGround/SO/KillCamData")]
    public class KillCamData : ScriptableObject
    {
        [Header("트리거 조건")]
        [Tooltip("킬캠 발동 확률 (0~1). 매 킬마다 연출이 나오면 지루하므로 확률 제어")]
        [Range(0f, 1f)]
        public float triggerChance = 0.5f;

        [Tooltip("킬캠 최소 재발동 간격 (초). 짧은 시간 내 연속 킬 시 중복 방지")]
        public float cooldown = 3f;

        [Header("슬로모션")]
        [Tooltip("슬로모션 TimeScale (낮을수록 느림)")]
        [Range(0.01f, 0.5f)]
        public float slowMotionTimeScale = 0.05f;

        [Tooltip("슬로모션 지속 시간 (실제 시간 기준, 초)")]
        public float slowMotionDuration = 0.6f;

        [Header("카메라 줌")]
        [Tooltip("줌인 목표 거리")]
        public float zoomDistance = 2.5f;

        [Tooltip("줌인 소요 시간 (실제 시간 기준, 초)")]
        public float zoomInDuration = 0.15f;

        [Tooltip("줌 유지 시간 (실제 시간 기준, 초)")]
        public float zoomHoldDuration = 0.3f;

        [Tooltip("줌아웃 복귀 시간 (실제 시간 기준, 초)")]
        public float zoomOutDuration = 0.4f;

        [Tooltip("줌인/아웃 이징 커브")]
        public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("카메라 오프셋")]
        [Tooltip("킬캠 시 카메라 피벗 오프셋 (숄더 뷰 등)")]
        public Vector3 killCamOffset = new Vector3(0.5f, 0.8f, 0f);

        [Header("카메라 쉐이크")]
        [Tooltip("킬캠 전용 카메라 쉐이크 키 (CameraShakeDatabase에 등록된 키)")]
        public string cameraShakeKey = "KillCam";

        [Header("포스트 프로세스 (선택)")]
        [Tooltip("Vignette 강도 (0이면 미사용)")]
        [Range(0f, 1f)]
        public float vignetteIntensity = 0.4f;
    }
}
