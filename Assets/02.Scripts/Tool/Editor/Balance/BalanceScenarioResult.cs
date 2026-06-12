#if UNITY_EDITOR
using System.Collections.Generic;
using UPlayGround.Data.Actor;

namespace UPlayGround.Tool.Editor.Balance
{
    public enum BalanceCheckStatus
    {
        InvalidData,
        TooEasy,
        TooLethal,
        Stalled,
        Stable,
    }

    public enum BalanceValidationLevel
    {
        Info,
        Warning,
        Error,
    }

    public readonly struct BalanceValidationMessage
    {
        public BalanceValidationMessage(BalanceValidationLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        public BalanceValidationLevel Level { get; }
        public string Message { get; }
    }

    public sealed class BalanceSkillBreakdown
    {
        public string Name;
        public float Damage;
        public float PoiseDamage;
        public float Weight;
        public float SelectionChance;
        public float Cooldown;
        public float DpsContribution;
        public float PoiseContribution;
        public float DpsShare;
        public int HitPhaseCount;
        public string Category;
        public bool IsStrong;
        public bool UseDangerRing;
        public bool UseTelegraph;
        public float DangerRingDuration;
        public string DefenseType;
    }

    public sealed class BalanceScenarioResult
    {
        public ActorDefinitionSO Actor;
        public int MonsterLevel;
        public BalanceCheckStatus Status;
        public float TargetDuration;
        public float PlayerTimeToDeath;
        public float MonsterTimeToDeath;
        public float PlayerSurvivalRatio;
        public float MonsterKillRatio;
        public float BalanceScore;
        public float EnemyExpectedDps;
        public float EnemyPoiseDps;
        public float PlayerExpectedDps;
        public float PlayerEffectiveDpsWithBreak;
        public float PlayerExpectedBreakDps;
        public float PlayerAttackPower;
        public float PlayerHealth;
        public float MonsterHealth;
        public float MonsterBreakGauge;
        public float MonsterBreakResist;
        public float BreakExposedDuration;
        public float BreakDamageTakenMultiplier;
        public float EstimatedTimeToBreak;
        public float EstimatedBreaksPerFight;
        public float BreakExposedUptime;
        public float MonsterTimeToDeathWithBreak;
        public float PlayerMaxPoise;
        public float PlayerPoiseRecoveryRate;
        public float NetPoisePressure;
        public float EnemyAttackOpportunities;
        public float AvailableSkillCount;
        public int UnlockedSkillCount;
        public int LockedSkillCount;
        public float BasicAttackChance;
        public float HeavyAttackChance;
        public float SkillAttackChance;
        public float StrongAttackChance;
        public float TopAttackDpsShare;
        public string TopAttackName;
        public string RecommendedAction;
        public string Summary;
        public readonly List<BalanceValidationMessage> Messages = new();
        public readonly List<BalanceSkillBreakdown> SkillBreakdowns = new();

        public bool HasError
        {
            get
            {
                for (int i = 0; i < Messages.Count; i++)
                    if (Messages[i].Level == BalanceValidationLevel.Error)
                        return true;
                return false;
            }
        }
    }
}
#endif
