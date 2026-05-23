using UnityEngine;
using UPlayGround.AI.CombatDecision;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// Intent별 점수 계산 가중치 프로파일.
    /// EnemyBehaviorSO.intentWeights 또는 BehaviorPhase.intentWeightsOverride로 주입한다.
    /// </summary>
    [CreateAssetMenu(fileName = "IW_Profile", menuName = "UPlayGround/Enemy/Intent Weights")]
    public class EnemyIntentWeightsSO : ScriptableObject
    {
        public IntentWeightEntry attack       = new();
        public IntentWeightEntry punish       = new();
        public IntentWeightEntry counter      = new();
        public IntentWeightEntry pressure     = new();
        public IntentWeightEntry chase        = new();
        public IntentWeightEntry retreat      = new();
        public IntentWeightEntry keepDistance = new();
        public IntentWeightEntry defend       = new();
        public IntentWeightEntry recover      = new();

        public IntentWeightEntry GetEntry(CombatIntent intent)
        {
            switch (intent)
            {
                case CombatIntent.Attack:       return attack;
                case CombatIntent.Punish:       return punish;
                case CombatIntent.Counter:      return counter;
                case CombatIntent.Pressure:     return pressure;
                case CombatIntent.Chase:        return chase;
                case CombatIntent.Retreat:      return retreat;
                case CombatIntent.KeepDistance: return keepDistance;
                case CombatIntent.Defend:       return defend;
                case CombatIntent.Recover:      return recover;
                default:                        return null;
            }
        }
    }
}
