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

        /// <summary>
        /// 쉐이크 적용 방식. 기존 에셋 호환을 위해 Position이 0번(기본값).
        /// 3D 액션은 Rotation 권장(카메라가 벽을 뚫지 않고 멀미가 적음).
        /// </summary>
        public enum ShakeMode
        {
            Position,  // 카메라 로컬 위치 이동 (레거시)
            Rotation,  // 카메라 로컬 회전 (Pitch/Yaw/Roll) — 권장
        }

        /// <summary>노이즈 종류. 기존 호환을 위해 Random이 0번(기본값).</summary>
        public enum NoiseType
        {
            Random,  // 매 ShakesDelay마다 난수 (레거시, 계단식)
            Perlin,  // 연속 정합 노이즈 — 부드럽고 멀미가 적음 (권장)
        }

        public string key;

        public bool UseMainCamera = true;
        public List<Camera> Cameras = new List<Camera>();

        [Space]
        [Header("Mode")]
        [Tooltip("Position=위치 이동(레거시), Rotation=회전(권장)")]
        public ShakeMode Mode = ShakeMode.Position;

        [Tooltip("Random=난수(레거시), Perlin=정합 노이즈(권장)")]
        public NoiseType Noise = NoiseType.Random;

        [Space]
        [Tooltip("지속 시간 (초)")]
        public float Duration = 0.12f;

        [Tooltip("시작 지연 (초)")]
        public float Delay = 0f;

        [Space]
        [Header("Position Amplitude (Mode=Position)")]
        [Tooltip("X축 진폭 (좌우, 미터)")]
        public float AmplitudeX = 0.1f;

        [Tooltip("Y축 진폭 (상하, 미터)")]
        public float AmplitudeY = 0.06f;

        [Space]
        [Header("Rotation Amplitude (Mode=Rotation, 도 단위)")]
        [Tooltip("Pitch 진폭 (상하 끄덕임). 내려치기 계열에 강조")]
        public float PitchAmplitude = 1.2f;

        [Tooltip("Yaw 진폭 (좌우 흔들림). 횡베기 계열에 강조")]
        public float YawAmplitude = 0.9f;

        [Tooltip("Roll 진폭 (기울임). 멀미 유발 → 기본 0")]
        public float RollAmplitude = 0f;

        [Range(0f, 1f)]
        [Tooltip("타격 방향을 Pitch/Yaw 축 가중에 반영하는 정도 (Rotation 전용)")]
        public float DirectionalBias = 0.5f;

        [Space]
        [Header("Distance Attenuation (폭발 등 공간 발생원)")]
        [Tooltip("켜면 발생 위치-카메라 거리로 강도를 감쇠한다. 타격 월드 위치가 필요")]
        public bool AttenuateByDistance = false;

        [Tooltip("이 거리(m) 이상이면 강도 0. 0 이하면 감쇠 없음")]
        public float AttenuationRange = 25f;

        [Space]
        [Header("Frequency & Dampening")]
        [Tooltip("진동 주파수 (Hz). 높을수록 빠르게 떨림")]
        public float Frequency = 22f;

        [Tooltip("감쇠 방식")]
        public DampeningType Dampening = DampeningType.EaseOut;

        [Space]
        [Tooltip("Position 모드 전용: Screen=화면기준, World=월드기준")]
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

        /// <summary>회전 강도 벡터 (도): X=Pitch, Y=Yaw, Z=Roll</summary>
        public Vector3 RotationStrength => new Vector3(PitchAmplitude, YawAmplitude, RollAmplitude);

        /// <summary>캐싱된 감쇠 커브. 매 프레임 new 하지 않는다.</summary>
        public AnimationCurve ShakeCurve => _cachedCurve ??= BuildCurve();

        /// <summary>주파수 기반 진동 간격 (초)</summary>
        public float ShakesDelay => Frequency > 0f ? 1f / Frequency : 0f;
    }
}
