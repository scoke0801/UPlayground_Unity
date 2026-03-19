using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "CameraShakeData", menuName = "UPlayGround/SO/CameraShakeData")]
    public class CameraShakeData : ScriptableObject
    {
        public enum DampeningType
        {
            EaseOut,   // 점진 감쇠 — 공격 히트 계열
            Linear,    // 선형 감쇠 — 피격 계열
            Constant,  // 감쇠 없음 — 지속 진동
        }

        public string key;

        public bool UseMainCamera = true;
        public List<Camera> Cameras = new List<Camera>();

        [Space]
        [Tooltip("지속 시간 (초)")]
        public float Duration = 0.12f;

        [Tooltip("시작 지연 (초)")]
        public float Delay = 0f;

        [Space]
        [Header("Amplitude")]
        [Tooltip("X축 진폭 (좌우)")]
        public float AmplitudeX = 0.1f;

        [Tooltip("Y축 진폭 (상하)")]
        public float AmplitudeY = 0.06f;

        [Space]
        [Header("Frequency & Dampening")]
        [Tooltip("진동 주파수 (Hz). 높을수록 빠르게 떨림")]
        public float Frequency = 22f;

        [Tooltip("감쇠 방식")]
        public DampeningType Dampening = DampeningType.EaseOut;

        [Space]
        public CameraShaker.ShakeSpace ShakeSpace = CameraShaker.ShakeSpace.Screen;

        // ── 런타임 캐시 ───────────────────────────────────────────────
        // ShakeCurve는 프로퍼티 getter 호출마다 new 했던 것을 OnEnable에서 1회만 생성한다.
        // SO가 로드/재로드될 때마다 OnEnable이 호출되므로 항상 최신 Dampening을 반영한다.
        private AnimationCurve _cachedCurve;

        private void OnEnable()  => _cachedCurve = BuildCurve();
        private void OnValidate() => _cachedCurve = BuildCurve(); // 인스펙터에서 값 변경 시 즉시 갱신

        private AnimationCurve BuildCurve() => Dampening switch
        {
            DampeningType.Linear   => AnimationCurve.Linear(0f, 1f, 1f, 0f),
            DampeningType.Constant => AnimationCurve.Linear(0f, 1f, 1f, 1f),
            _                      => AnimationCurve.EaseInOut(0f, 1f, 1f, 0f), // EaseOut
        };

        /// <summary>CameraShaker가 사용하는 월드 강도 벡터 (Z 고정 0)</summary>
        public Vector3 ShakeStrength => new Vector3(AmplitudeX, AmplitudeY, 0f);

        /// <summary>캐싱된 감쇠 커브. 매 프레임 new 하지 않는다.</summary>
        public AnimationCurve ShakeCurve => _cachedCurve ??= BuildCurve();

        /// <summary>주파수 기반 진동 간격 (초)</summary>
        public float ShakesDelay => Frequency > 0f ? 1f / Frequency : 0f;
    }
}
