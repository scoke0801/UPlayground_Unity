
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "CameraShakeData", menuName = "UPlayGround/SO/CameraShakeData")]
    public class CameraShakeData : ScriptableObject
    {
        public enum DampeningType
        {
            EaseOut,   // 점진 감쇠 (공격 히트 계열)
            Linear,    // 선형 감쇠 (피격 계열)
            Constant,  // 감쇠 없음 (킬캠 미진동 등 지속 진동)
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
        [Header("Amplitude (기획서 §4.1)")]
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

        // ── 하위 호환 ──────────────────────────────────────────────
        // CameraShaker.Update()는 ShakeStrength / ShakeCurve를 직접 참조하므로
        // AmplitudeX/Y → ShakeStrength, Dampening → ShakeCurve 로 변환하는 프로퍼티를 제공.
        // SO 에셋을 수동으로 다시 세팅할 필요 없이 런타임에서 자동 변환된다.

        /// <summary>CameraShaker가 사용하는 월드 강도 벡터 (Z는 0 고정)</summary>
        public Vector3 ShakeStrength => new Vector3(AmplitudeX, AmplitudeY, 0f);

        /// <summary>Dampening 타입에 맞는 감쇠 커브 (0=시작, 1=종료)</summary>
        public AnimationCurve ShakeCurve
        {
            get
            {
                return Dampening switch
                {
                    DampeningType.Linear   => AnimationCurve.Linear(0f, 1f, 1f, 0f),
                    DampeningType.Constant => AnimationCurve.Linear(0f, 1f, 1f, 1f),
                    _                      => AnimationCurve.EaseInOut(0f, 1f, 1f, 0f), // EaseOut
                };
            }
        }

        /// <summary>주파수 기반 진동 간격 (초). CameraShaker.ShakesDelay에 대응</summary>
        public float ShakesDelay => Frequency > 0f ? 1f / Frequency : 0f;
    }
}
