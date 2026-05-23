using System;
using System.Collections.Generic;

namespace UPlayGround.AI.CombatDecision
{
    /// <summary>
    /// 조건 항들의 결합 방식.
    /// </summary>
    public enum ConditionMode
    {
        /// <summary> 모든 항이 참이어야 적용 (AND) </summary>
        All = 0,
        /// <summary> 항 중 하나라도 참이면 적용 (OR) </summary>
        Any = 1
    }

    /// <summary>
    /// 단일 조건 항. negate가 true면 평가 결과를 뒤집는다.
    /// </summary>
    [Serializable]
    public struct ConditionTerm
    {
        public IntentConditionId conditionId;
        public bool negate;

        public ConditionTerm(IntentConditionId id, bool negate = false)
        {
            this.conditionId = id;
            this.negate = negate;
        }
    }

    /// <summary>
    /// 연속값에 계수를 곱해 점수에 가산하는 항.
    /// 예: Aggression 0.4f, coefficient 0.42f → 0.168 가산.
    /// </summary>
    [Serializable]
    public struct ContinuousContribution
    {
        public ContinuousValueId valueId;
        public float coefficient;

        public ContinuousContribution(ContinuousValueId id, float coefficient)
        {
            this.valueId = id;
            this.coefficient = coefficient;
        }
    }

    /// <summary>
    /// 조건이 만족될 때 점수에 가산되는 보너스.
    /// 가산량 = amount + Σ(continuous[i].coefficient × value[continuous[i].id]).
    /// </summary>
    [Serializable]
    public class ConditionBonus
    {
        [UnityEngine.Tooltip("인스펙터/디버그용 라벨")]
        public string label;

        public ConditionMode mode = ConditionMode.All;
        public List<ConditionTerm> terms = new();

        [UnityEngine.Tooltip("고정 가산량")]
        [UnityEngine.Range(-0.6f, 0.6f)]
        public float amount;

        [UnityEngine.Tooltip("연속값 기반 추가 가산")]
        public List<ContinuousContribution> continuous = new();
    }

    /// <summary>
    /// 조건이 만족될 때 점수에 곱해지는 배수.
    /// 모든 bonus 가산 이후, Phase/Role 가중치 이전에 적용된다.
    /// </summary>
    [Serializable]
    public class ConditionMultiplier
    {
        [UnityEngine.Tooltip("인스펙터/디버그용 라벨")]
        public string label;

        public ConditionMode mode = ConditionMode.All;
        public List<ConditionTerm> terms = new();

        [UnityEngine.Tooltip("적용 배수")]
        [UnityEngine.Range(0f, 2f)]
        public float factor = 1f;
    }

    /// <summary>
    /// 한 Intent의 점수 계산 정의.
    /// 점수 = baseScore + Σ(baseContinuous) + Σ(만족된 bonuses) → Π(만족된 multipliers).
    /// </summary>
    [Serializable]
    public class IntentWeightEntry
    {
        [UnityEngine.Tooltip("기본 점수")]
        [UnityEngine.Range(0f, 1f)]
        public float baseScore;

        [UnityEngine.Tooltip("기본 점수에 가산되는 연속값 기여")]
        public List<ContinuousContribution> baseContinuous = new();

        public List<ConditionBonus> bonuses = new();
        public List<ConditionMultiplier> multipliers = new();
    }
}
