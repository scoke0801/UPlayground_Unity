#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static partial class MonsterBehaviorTreeJsonImporter
    {
        /// <summary>에셋을 생성하지 않고 importer와 같은 오류 검사를 수행한다.</summary>
        public static void ValidateJson(MonsterBehaviorTreeJson data)
        {
            Validate(data, data?.id);
        }

        /// <summary>쿨다운 등록 누락과 선택자 우선순위에서 가려진 규칙을 보고한다.</summary>
        public static IReadOnlyList<string> CollectWarnings(MonsterBehaviorTreeJson data)
        {
            var warnings = new List<string>();
            var registered = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in EnumerateJsonRules(data))
            {
                foreach (var action in rule.@do ?? new List<MonsterBehaviorActionJson>())
                    AddRegistration(action.action, action.cooldownId, action.cooldownDuration);
                foreach (var choice in rule.choices ?? new List<MonsterBehaviorChoiceJson>())
                    AddRegistration(choice.action, choice.cooldownId, choice.cooldownDuration);
            }

            if (data.groups != null && data.groups.Count > 0)
            {
                foreach (var group in data.groups)
                {
                    CheckConditions(group.when, group.name);
                    CheckRules(group.rules, group.name);
                }
            }
            else
                CheckRules(data.rules, data.id);

            return warnings;

            void AddRegistration(string action, string id, float duration)
            {
                if ((action == MonsterBehaviorJsonNodeKeys.Actions.RequestAction
                     || action == MonsterBehaviorJsonNodeKeys.Actions.Transition)
                    && !string.IsNullOrWhiteSpace(id) && duration > 0f)
                    registered.Add(id);
            }

            void CheckConditions(List<MonsterBehaviorConditionJson> conditions, string name)
            {
                if (conditions == null) return;
                foreach (var condition in conditions)
                    if (condition.condition == MonsterBehaviorJsonNodeKeys.Conditions.CooldownReady
                        && (string.IsNullOrWhiteSpace(condition.value) || !registered.Contains(condition.value)))
                        warnings.Add($"{name}: CooldownReady '{condition.value}'를 등록하는 RequestAction/Transition이 없습니다.");
            }

            void CheckRules(List<MonsterBehaviorRuleJson> rules, string groupName)
            {
                if (rules == null) return;
                string blocker = null;
                foreach (var rule in rules.OrderByDescending(rule => rule.priority))
                {
                    CheckConditions(rule.when, rule.name);
                    if (blocker != null)
                        warnings.Add($"{groupName}/{rule.name}: 앞선 무조건 성공 규칙 '{blocker}' 때문에 도달할 수 없습니다.");
                    // KeepCurrentState도 비차단 상태에서는 실패한다. 완료 후 성공하는 Wait만 확정 가림이다.
                    if ((rule.when == null || rule.when.Count == 0)
                        && rule.@do != null && rule.@do.Count == 1
                        && rule.@do[0].action == MonsterBehaviorJsonNodeKeys.Actions.Wait)
                        blocker ??= rule.name;
                }
            }
        }
    }
}
#endif
