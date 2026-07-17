using UnityEngine;

namespace UPlayGround.Data
{
    /// <summary>
    /// 카메라 이펙트 설정 ScriptableObject 베이스 클래스
    /// 각 구체 이펙트 데이터가 상속하여 고유 파라미터를 추가한다.
    /// </summary>
    public abstract class CameraEffectData : ScriptableObject
    {
        [Header("Base Effect Settings")]
        [Tooltip("이펙트 식별 키 (조회/중지용)")]
        public string effectKey;

        [Tooltip("우선순위 (높을수록 우선 적용)")]
        public int priority = 0;

        [Tooltip("총 지속 시간 (0 = 무한, 수동 Stop 필요)")]
        public float duration = 1.0f;

        [Tooltip("BlendIn 지속 시간 (초)")]
        public float blendInDuration = 0.1f;

        [Tooltip("BlendOut 지속 시간 (초)")]
        public float blendOutDuration = 0.2f;

        [Tooltip("BlendIn 커브 (x=정규화 시간, y=가중치)")]
        public AnimationCurve blendInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("BlendOut 커브 (x=정규화 시간, y=가중치)")]
        public AnimationCurve blendOutCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Tooltip("Time.timeScale 무관하게 동작 (unscaledDeltaTime 사용)")]
        public bool useUnscaledTime = false;

        /// <summary>
        /// 이 데이터에 맞는 이펙트 인스턴스를 생성하는 팩토리 메서드
        /// </summary>
        public abstract ICameraEffect CreateEffect();
    }
}
