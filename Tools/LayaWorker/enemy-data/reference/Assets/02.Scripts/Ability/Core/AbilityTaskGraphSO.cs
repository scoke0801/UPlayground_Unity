using UnityEngine;
using System;

namespace UPlayGround.Ability.Core
{
    [CreateAssetMenu(fileName = "AbilityTaskGraph_", menuName = "UPlayGround/Ability/Task Graph")]
    public sealed class AbilityTaskGraphSO : ScriptableObject
    {
        [SerializeField] private AbilityTaskDefinitionSO _root;
        public AbilityTaskDefinitionSO Root => _root;

        public static AbilityTaskGraphSO CreateTransient(
            AbilityTaskDefinitionSO root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            var graph = CreateInstance<AbilityTaskGraphSO>();
            graph._root = root;
            return graph;
        }
    }
}
