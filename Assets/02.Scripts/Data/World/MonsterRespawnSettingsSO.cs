using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.World
{
    /// <summary>
    /// 필드 몬스터 재스폰 규칙(등급별 간격/레벨 성장/보상 스케일링).
    /// MonsterRespawnManager가 Addressables 키 "MonsterRespawnSettings"로 로드하며,
    /// 에셋이 없으면 코드 기본값(이 SO의 필드 기본값과 동일)으로 동작한다.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterRespawnSettings", menuName = "UPlayGround/월드/Monster Respawn Settings")]
    public class MonsterRespawnSettingsSO : ScriptableObject
    {
        [Header("등급별 최소 대기 시간 (인게임 분) — 이 시간이 지난 뒤 도래하는 첫 자정에 리스폰")]
        [Tooltip("약몹 최소 대기. 240 = 인게임 4시간 경과 후 첫 자정.")]
        [Min(1f)] public float weakIntervalMinutes = 4f * 60f;

        [Tooltip("일반 몬스터 최소 대기. 360 = 인게임 6시간 경과 후 첫 자정.")]
        [Min(1f)] public float normalIntervalMinutes = 6f * 60f;

        [Tooltip("엘리트 최소 대기. 720 = 인게임 12시간 경과 후 첫 자정.")]
        [Min(1f)] public float eliteIntervalMinutes = 12f * 60f;

        [Header("등급별 재스폰 허용")]
        public bool respawnWeak = true;
        public bool respawnNormal = true;
        public bool respawnElite = true;
        [Tooltip("보스는 재스폰하지 않는 것이 기본 설계다. 켜지 말 것.")]
        public bool respawnBoss = false;

        [Header("재스폰 레벨 성장")]
        [Tooltip("인게임 1일당 레벨 보너스. 0.5 = 2일마다 +1레벨.")]
        [Min(0f)] public float levelUpPerGameDay = 0.5f;

        [Tooltip("같은 포인트에서 N회 재스폰마다 +1레벨.")]
        [Min(1)] public int respawnCountPerLevel = 3;

        [Tooltip("기준 레벨 대비 최대 레벨 보너스.")]
        [Min(0)] public int maxRespawnLevelBonus = 20;

        [Min(1)] public int minRespawnLevel = 1;

        [Header("보상 스케일링")]
        [Tooltip("레벨 1당 경험치 증가율. runtimeExp = baseExp * gradeMult * (1+rate)^levelDelta")]
        [Min(0f)] public float expPerLevelRate = 0.08f;

        [Tooltip("레벨 1당 골드 증가율.")]
        [Min(0f)] public float goldPerLevelRate = 0.08f;

        [Tooltip("등급별 보상 배율.")]
        [Min(0f)] public float weakRewardMultiplier = 0.8f;
        [Min(0f)] public float normalRewardMultiplier = 1f;
        [Min(0f)] public float eliteRewardMultiplier = 1.35f;

        public bool IsGradeRespawnable(MonsterActorGrade grade) => grade switch
        {
            MonsterActorGrade.Weak => respawnWeak,
            MonsterActorGrade.Normal => respawnNormal,
            MonsterActorGrade.Elite => respawnElite,
            MonsterActorGrade.Boss => respawnBoss,
            _ => false,
        };

        public float GetIntervalMinutes(MonsterActorGrade grade) => grade switch
        {
            MonsterActorGrade.Weak => weakIntervalMinutes,
            MonsterActorGrade.Elite => eliteIntervalMinutes,
            _ => normalIntervalMinutes,
        };

        public float GetRewardMultiplier(MonsterActorGrade grade) => grade switch
        {
            MonsterActorGrade.Weak => weakRewardMultiplier,
            MonsterActorGrade.Elite => eliteRewardMultiplier,
            _ => normalRewardMultiplier,
        };
    }
}
