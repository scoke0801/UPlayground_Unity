using UnityEngine;

namespace UPlayGround.Data.Sound
{
    /// <summary>
    /// 단일 사운드 정의. SoundDatabaseSO가 이 에셋들을 모아 key 기반으로 관리한다.
    /// key는 비워 두면 OnValidate에서 에셋 이름으로 자동 채워진다.
    /// </summary>
    [CreateAssetMenu(fileName = "Sound_", menuName = "UPlayGround/오디오/Sound Entry")]
    public sealed class SoundEntrySO : ScriptableObject
    {
        public string key;
        public AudioClip clip;
        public SoundBusType bus = SoundBusType.SFX;
        public SoundDistanceMode distanceMode = SoundDistanceMode.Logarithmic3D;

        [Range(0f, 1f)] public float volume = 1f;
        public float pitchMin = 1f;
        public float pitchMax = 1f;

        public float minDistance = 1.5f;
        public float maxDistance = 24f;
        public AnimationCurve customRolloff;
        public bool preCullByMaxDistance = true;

        public float cooldown = 0f;
        public int maxSimultaneous = 4;
        [Range(0, 256)] public int priority = 128;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // key를 비워 두면 에셋 이름을 key로 사용한다(중복 입력 방지).
            if (string.IsNullOrWhiteSpace(key) && !string.IsNullOrEmpty(name))
                key = name;
        }
#endif
    }
}
