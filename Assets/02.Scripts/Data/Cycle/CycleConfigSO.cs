using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Cycle
{
    public enum CycleRewardGrade
    {
        Common,
        Rare,
        Heroic,
    }

    [Serializable]
    public sealed class CycleDifficultyEntry
    {
        [Min(1)] public int cycleIndex = 1;
        [Min(0.01f)] public float healthMultiplier = 1f;
        [Min(0.01f)] public float attackMultiplier = 1f;
        public CycleRewardGrade rewardGrade = CycleRewardGrade.Common;
    }

    /// <summary>P0 사이클 공통 튜닝값.</summary>
    [CreateAssetMenu(fileName = "CycleConfig", menuName = "UPlayGround/사이클/공통 설정")]
    public sealed class CycleConfigSO : ScriptableObject
    {
        [Min(1)] public int prototypeTargetMinutes = 20;
        [Min(1)] public int releaseMaxMinutes = 40;
        public List<CycleDifficultyEntry> difficultyByCycle = CreateDefaultDifficulties();
        [Range(0f, 1f)] public float expLossRate = 0.30f;
        public bool dropUnsettledMaterials = true;
        public bool enableEquipmentFragmentLoss;
        [Tooltip("사이클 중 영구 인벤토리 대신 미정산 원장으로 들어갈 재료 Item ID")]
        public List<int> unsettledMaterialItemIds = new();

        public bool IsUnsettledMaterial(int itemId) => unsettledMaterialItemIds != null && unsettledMaterialItemIds.Contains(itemId);

        public bool TryGetDifficulty(int cycleIndex, out CycleDifficultyEntry difficulty)
        {
            difficulty = null;
            if (difficultyByCycle == null)
                return false;

            for (int i = 0; i < difficultyByCycle.Count; i++)
            {
                CycleDifficultyEntry candidate = difficultyByCycle[i];
                if (candidate != null && candidate.cycleIndex == cycleIndex)
                {
                    difficulty = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool ValidateP0(out string error)
        {
            if (difficultyByCycle == null || difficultyByCycle.Count != 3)
            {
                error = "P0 난이도 설정은 사이클 1~3의 세 항목이어야 합니다.";
                return false;
            }

            for (int cycleIndex = 1; cycleIndex <= 3; cycleIndex++)
            {
                if (!TryGetDifficulty(cycleIndex, out CycleDifficultyEntry entry))
                {
                    error = $"사이클 {cycleIndex} 난이도 설정이 없습니다.";
                    return false;
                }

                if (entry.healthMultiplier <= 0f || entry.attackMultiplier <= 0f)
                {
                    error = $"사이클 {cycleIndex} 배율은 0보다 커야 합니다.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public static CycleConfigSO CreateRuntimeDefault()
        {
            CycleConfigSO config = CreateInstance<CycleConfigSO>();
            config.hideFlags = HideFlags.HideAndDontSave;
            config.difficultyByCycle = CreateDefaultDifficulties();
            return config;
        }

        private static List<CycleDifficultyEntry> CreateDefaultDifficulties()
        {
            return new List<CycleDifficultyEntry>
            {
                new() { cycleIndex = 1, healthMultiplier = 1.00f, attackMultiplier = 1.00f, rewardGrade = CycleRewardGrade.Common },
                new() { cycleIndex = 2, healthMultiplier = 1.35f, attackMultiplier = 1.18f, rewardGrade = CycleRewardGrade.Rare },
                new() { cycleIndex = 3, healthMultiplier = 1.75f, attackMultiplier = 1.38f, rewardGrade = CycleRewardGrade.Heroic },
            };
        }
    }
}
