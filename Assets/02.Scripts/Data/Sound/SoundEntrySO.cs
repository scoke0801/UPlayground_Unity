using UnityEngine;

namespace UPlayGround.Data.Sound
{
    /// <summary>
    /// SoundDatabaseSO에서 사용하는 게임플레이 공통 사운드 키.
    /// 실제 AudioClip과 버스/거리 설정은 SoundEntrySO에서 관리한다.
    /// </summary>
    public static class GameSoundKey
    {
        public const string UiClick = "UI_Click";

        public const string CombatHitLight = "Combat_Hit_Light";
        public const string CombatHitHeavy = "Combat_Hit_Heavy";

        // 임팩트 티어 확장 키. 미등록이면 CombatFeedbackDispatcher가 Light/Heavy로 폴백하므로
        // 사운드 엔트리를 저작하기 전에도 안전하다.
        public const string CombatHitCritical = "Combat_Hit_Critical";
        public const string CombatHitBreak = "Combat_Hit_Break";
        public const string CombatWallImpact = "Combat_WallImpact";
        public const string CombatGuard = "Combat_Guard";
        public const string CombatPerfectGuard = "Combat_PerfectGuard";
        public const string CombatParry = "Combat_Parry";
        public const string CombatPerfectDodge = "Combat_PerfectDodge";
        public const string CombatSpecialBreak = "Combat_SpecialBreak";

        public const string PlayerDash = "Player_Dash";
        public const string PlayerDashEvade = "Player_DashEvade";

        public const string LevelUp = "LevelUp";
        public const string Heal = "Heal";
        public const string RestPointHeal = "RestPoint_Heal";
        public const string QuestClear = "QuestClear";
        public const string GetItem = "GetItem";
    }

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
