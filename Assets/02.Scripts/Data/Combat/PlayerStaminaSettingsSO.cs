using UnityEngine;

namespace UPlayGround.Data.Combat
{
    /// <summary>플레이어 이동 행동의 스태미나 소비와 회복 규칙을 정의한다.</summary>
    [CreateAssetMenu(
        fileName = "PlayerStaminaSettings",
        menuName = "UPlayGround/Combat/Player Stamina Settings")]
    public sealed class PlayerStaminaSettingsSO : ScriptableObject
    {
        public const string ResourcePath = "PlayerStaminaSettings";

        [Header("행동 비용")]
        [Min(0f)] public float dashCost = 20f;
        [Min(0f)] public float dodgeCost = 15f;
        [Min(0f)] public float dodgeCooldownSeconds = 0.35f;
        [Min(0f)] public float sprintCostPerSecond = 15f;
        [Min(0f)] public float minimumSprintStartStamina = 10f;

        [Header("회복")]
        [Min(0f)] public float recoveryDelaySeconds = 1.2f;
        [Min(0f)] public float recoveryPerSecond = 25f;

        /// <summary>Resources에 등록된 프로젝트 공용 스태미나 정책을 반환한다.</summary>
        public static PlayerStaminaSettingsSO Load() =>
            Resources.Load<PlayerStaminaSettingsSO>(ResourcePath);
    }
}
