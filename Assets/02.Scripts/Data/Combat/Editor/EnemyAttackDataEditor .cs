using UnityEngine;
using UnityEditor;
using UPlayGround.Data.Combat;
using UPlayGround.Data;

namespace UPlayGround.Editor
{
    /// <summary>
    /// EnemyAttackDataSO 커스텀 에디터
    /// </summary>
    [CustomEditor(typeof(EnemyAttackDataSO))]
    public class EnemyAttackDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EnemyAttackDataSO attackData = (EnemyAttackDataSO)target;
            
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("스킬 조건 요약", EditorStyles.boldLabel);
            
            for (int i = 0; i < attackData.skills.Count; i++)
            {
                var skill = attackData.skills[i];
                
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"[{i}] {skill.baseInfo.animKey} ({skill.skillType})", EditorStyles.boldLabel);
                
                // 조건 요약
                string conditionSummary = GetConditionSummary(skill);
                EditorGUILayout.LabelField("조건:", conditionSummary);
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }
        
        private string GetConditionSummary(EnemyAttackInfo skill)
        {
            if (skill.conditionGroup.conditions == null || skill.conditionGroup.conditions.Count == 0)
                return "조건 없음";
            
            string[] conditionTexts = new string[skill.conditionGroup.conditions.Count];
            
            for (int i = 0; i < skill.conditionGroup.conditions.Count; i++)
            {
                var condition = skill.conditionGroup.conditions[i];
                conditionTexts[i] = GetConditionText(condition);
            }
            
            string operatorText = skill.conditionGroup.conditionOperator == ConditionOperator.And ? " AND " : " OR ";
            return string.Join(operatorText, conditionTexts);
        }
        
        private string GetConditionText(SkillCondition condition)
        {
            switch (condition.type)
            {
                case ConditionType.None:
                    return "없음";
                    
                case ConditionType.SelfHealthBased:
                    return $"자신 HP {condition.minHealthPercent*100:F0}~{condition.maxHealthPercent*100:F0}%";
                    
                case ConditionType.TargetHealthBased:
                    return $"타겟 HP {condition.minHealthPercent*100:F0}~{condition.maxHealthPercent*100:F0}%";
                    
                case ConditionType.RangeBased:
                    return $"거리 {condition.minRange:F1}~{condition.maxRange:F1}m";
                    
                case ConditionType.AllyCountBased:
                    return $"아군 {condition.minAllyCount}~{condition.maxAllyCount}마리";
                    
                case ConditionType.InjuredAllyNearby:
                    return $"부상 아군 {condition.maxRange:F0}m내 (HP {condition.maxHealthPercent*100:F0}%이하)";
                    
                default:
                    return "알 수 없음";
            }
        }
    }
}