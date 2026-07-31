using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Tool.Editor.Combat;

namespace UPlayGround.Data.Actor.Animation.Editor
{
    [CustomEditor(typeof(ActorAnimationMotionSet), true)]
    public sealed class ActorAnimationMotionSetEditor : UnityEditor.Editor
    {
        private SerializedProperty _fallbackMotionSet;
        private SerializedProperty _attackWeaponType;
        private SerializedProperty _attackAbilitySet;
        private SerializedProperty _abilityMotions;
        private SerializedProperty _motionSlots;

        private void OnEnable()
        {
            _fallbackMotionSet = serializedObject.FindProperty("fallbackMotionSet");
            _attackWeaponType = serializedObject.FindProperty("attackWeaponType");
            _attackAbilitySet = serializedObject.FindProperty("attackAbilitySet");
            _abilityMotions = serializedObject.FindProperty("abilityMotions");
            _motionSlots = serializedObject.FindProperty("motionSlots");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("공용 모션", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _fallbackMotionSet,
                new GUIContent("Fallback MotionSet", "현재 SO에 없는 Motion Slot을 Fallback 체인에서 찾습니다."));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("공격 모션", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _attackWeaponType,
                new GUIContent(
                    "Attack Weapon Type",
                    "이 Actor MotionSet이 담당하는 공격 무기 타입입니다."));
            EditorGUILayout.PropertyField(
                _attackAbilitySet,
                new GUIContent(
                    "Attack Ability Set",
                    "애니메이션 에디터에서 함께 표시할 공격 Ability 모음입니다."));
            EditorGUILayout.PropertyField(
                _abilityMotions,
                new GUIContent(
                    "Ability Motions",
                    "Ability/Variant Key를 실제 MotionSetAsset으로 해석하는 액터 소유 매핑입니다."),
                true);

            if (_attackAbilitySet.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "공격 AbilitySet을 연결하면 애니메이션 에디터의 공격 그룹에서 모든 공격 MotionSet을 바로 편집할 수 있습니다.",
                    MessageType.Info);

                if (GUILayout.Button("몬스터 ActorDefinition에서 자동 연결"))
                {
                    var actorSet = (ActorAnimationMotionSet)target;
                    AbilitySetSO found =
                        CombatTimelineUtility.FindAbilitySetForMotionSet(
                            actorSet,
                            out var owner,
                            out bool ambiguous,
                            out string candidateSummary);
                    if (found != null)
                    {
                        Undo.RecordObject(actorSet, "Connect Attack Ability Set");
                        actorSet.attackAbilitySet = found;
                        EditorUtility.SetDirty(actorSet);
                        AssetDatabase.SaveAssetIfDirty(actorSet);
                        serializedObject.Update();
                        Debug.Log(
                            $"[ActorAnimationMotionSet] {actorSet.name}: "
                            + $"{owner.name}의 {found.name} 연결");
                    }
                    else if (ambiguous)
                    {
                        Debug.LogWarning(
                            $"[ActorAnimationMotionSet] {actorSet.name}의 "
                            + $"AbilitySet 자동 연결 후보가 모호합니다: {candidateSummary}. "
                            + "Attack Ability Set을 직접 선택해 주세요.");
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[ActorAnimationMotionSet] {actorSet.name}에 대응하는 "
                            + "몬스터 ActorDefinition/AbilitySet을 찾지 못했습니다.");
                    }
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Motion Slot 목록", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_motionSlots, true);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("애니메이션 에디터에서 열기", GUILayout.Height(26f)))
                UPlayGround.Animation.Editor.MotionEditorProjectEntry.Open(
                    (ActorAnimationMotionSet)target);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
