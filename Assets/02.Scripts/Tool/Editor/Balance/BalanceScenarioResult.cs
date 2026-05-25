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
        public float Weight;
        public float SelectionChance;
        public float Cooldown;
        public float DpsContribution;
        public int HitPhaseCount;
        public string Category;
    }

    public sealed class BalanceScenarioResult
    {
        public ActorDefinitionSO Actor;
        public int MonsterLevel;
        public BalanceCheckStatus Status;
        public float TargetDuration;
        public float PlayerTimeToDeath;
        public float MonsterTimeToDeath;
        public float EnemyExpectedDps;
        public float PlayerExpectedDps;
        public float PlayerAttackPower;
        public float MonsterHealth;
        public float EnemyAttackOpportunities;
        public float AvailableSkillCount;
        public float BasicAttackChance;
        public float HeavyAttackChance;
        public float SkillAttackChance;
        public float StrongAttackChance;
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
