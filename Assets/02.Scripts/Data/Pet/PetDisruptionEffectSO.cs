using UnityEngine;
using UPlayGround.Data.Ability;

namespace UPlayGround.Data.Pet
{
    /// <summary>교란 수치는 GAS 효과가 소유하며 공격 지연은 AI가 안전한 결정 지점에서 소비한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Pet/Disruption Effect")]
    public sealed class PetDisruptionEffectSO : GameplayEffectSO
    {
        [Min(0f)] public float distractionSeconds = 1f;
        [Min(0f)] public float facingSharpness = 8f;
    }
}
