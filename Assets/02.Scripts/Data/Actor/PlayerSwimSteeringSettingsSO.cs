using UnityEngine;

namespace UPlayGround.Data.Actor
{
    /// <summary>시선으로 조절하는 수영의 수평 여유·수직 반응·잠수 진입 수치를 공유한다.</summary>
    [CreateAssetMenu(fileName = "PlayerSwimSteeringSettings", menuName = "UPlayGround/이동/수영 시선 조작")]
    public sealed class PlayerSwimSteeringSettingsSO : ScriptableObject
    {
        [Range(-30f, 60f), Tooltip("깊이를 유지하는 카메라 하향각(도).")]
        public float neutralCameraPitch = 18f;
        [Range(0f, 15f), Tooltip("기준 시선 주변에서 상승·하강하지 않는 여유 각도(도).")]
        public float pitchDeadZone = 4f;
        [Range(0.5f, 3f), Tooltip("여유 각도 밖의 시선 변화를 수중 이동 각도로 확대한다.")]
        public float pitchGain = 2.6f;
        [Range(5f, 80f), Tooltip("잠수를 시작하는 카메라 하향각(도). 수평 여유 밖에서만 진입한다.")]
        public float diveStartPitch = 22f;
        [Min(0f), Tooltip("아래를 보며 전진할 때 잠수 진입까지 유지할 시간(초).")]
        public float diveStartHoldTime = 0.1f;
        [Min(0f), Tooltip("수직 속도가 요청에 수렴하는 속도. 커질수록 상승·하강 전환과 정지가 빠르다.")]
        public float verticalDamping = 14f;
    }
}
