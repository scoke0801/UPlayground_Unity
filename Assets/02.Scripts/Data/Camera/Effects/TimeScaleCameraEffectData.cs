using UnityEngine;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "TimeScaleCameraEffect", menuName = "UPlayGround/카메라/이펙트/TimeScale")]
    public class TimeScaleCameraEffectData : CameraEffectData
    {
        [Header("TimeScale Settings")]
        [Tooltip("목표 타임스케일 (0.01 = 매우 느림, 1.0 = 정상)")]
        [Range(0.01f, 1f)]
        public float targetTimeScale = 0.1f;

        [Tooltip("true: GameCombatManager.HitStop에 위임, false: 직접 Time.timeScale 블렌딩")]
        public bool useHitStopManager = true;

        public override ICameraEffect CreateEffect() => new TimeScaleCameraEffect(this);
    }
}
