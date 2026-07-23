using UnityEngine;

namespace UPlayGround.Ability.Core
{
    [CreateAssetMenu(fileName = "AbilityTaskGraph_", menuName = "UPlayGround/Ability/Task Graph")]
    public sealed class AbilityTaskGraphSO : ScriptableObject
    {
        [SerializeField] private AbilityTaskDefinitionSO _root;
        public AbilityTaskDefinitionSO Root => _root;
    }
}
