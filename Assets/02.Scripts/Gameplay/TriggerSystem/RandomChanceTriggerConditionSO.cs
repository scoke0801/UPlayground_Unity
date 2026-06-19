using UnityEngine;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/트리거/조건/Random Chance")]
    public sealed class RandomChanceTriggerConditionSO : TriggerConditionSO
    {
        [Range(0f, 1f)]
        [SerializeField] private float _chance = 0.5f;

        public override bool Evaluate(TriggerContext context)
        {
            return Random.value <= _chance;
        }
    }
}
