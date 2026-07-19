using System;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Codex
{
    public static class MonsterCodexCalculator
    {
        public const float DamageTakenSafetyFloor = 0f;

        public static float GetRecordRatio(long killCount, int fullRecordKillCount)
        {
            if (killCount <= 0)
                return 0f;

            return Mathf.Clamp01((float)killCount / Mathf.Max(1, fullRecordKillCount));
        }

        public static float GetExpMultiplier(float recordRatio, in MonsterCodexBonus bonus) =>
            1f + bonus.maxExpBonus * Mathf.Clamp01(recordRatio);

        public static float GetDamageDealtMultiplier(float recordRatio, in MonsterCodexBonus bonus) =>
            1f + bonus.maxDamageDealtBonus * Mathf.Clamp01(recordRatio);

        public static float GetDamageTakenMultiplier(float recordRatio, in MonsterCodexBonus bonus) =>
            Mathf.Max(
                DamageTakenSafetyFloor,
                1f - bonus.maxDamageTakenReduce * Mathf.Clamp01(recordRatio));
    }

    /// <summary>도감 UI가 구체 매니저나 ActorDefinitionSO를 조회하지 않도록 만든 스냅샷.</summary>
    [Serializable]
    public sealed class MonsterCodexEntryView
    {
        public string actorId;
        public string displayName;
        public string description;
        public Sprite portrait;
        public MonsterActorGrade grade;
        public CombatElementAssignmentMode elementAssignmentMode;
        public CombatElement element;
        public long killCount;
        public int fullRecordKillCount;
        public float recordRatio;
        public bool discovered;
        public MonsterCodexBonus bonus;

        public float ExpMultiplier =>
            MonsterCodexCalculator.GetExpMultiplier(recordRatio, bonus);
        public float DamageDealtMultiplier =>
            MonsterCodexCalculator.GetDamageDealtMultiplier(recordRatio, bonus);
        public float DamageTakenMultiplier =>
            MonsterCodexCalculator.GetDamageTakenMultiplier(recordRatio, bonus);
    }
}
