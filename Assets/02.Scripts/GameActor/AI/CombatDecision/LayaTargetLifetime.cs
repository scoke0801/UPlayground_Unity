#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
namespace UPlayGround.AI.CombatDecision
{
    /// <summary>풀에서 같은 Transform이 재사용되어도 이전 대상 관측을 구분한다.</summary>
    public sealed class LayaTargetLifetime : MonoBehaviour
    {
        public int Generation { get; private set; }
        private void OnEnable() => Generation++;
        private void OnDisable() => Generation++;
    }
}
#endif
