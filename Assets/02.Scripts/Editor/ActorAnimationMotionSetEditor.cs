using UnityEditor;
using UnityEngine;

namespace UPlayGround.Data.Actor.Animation.Editor
{
    [CustomEditor(typeof(ActorAnimationMotionSet), true)]
    public sealed class ActorAnimationMotionSetEditor : UnityEditor.Editor
    {
        private SerializedProperty _fallbackMotionSet;
        private SerializedProperty _motionSlots;

        private void OnEnable()
        {
            _fallbackMotionSet = serializedObject.FindProperty("fallbackMotionSet");
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
            EditorGUILayout.LabelField("Motion Slot 목록", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_motionSlots, true);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("애니메이션 에디터에서 열기", GUILayout.Height(26f)))
                UPlayGround.Animation.Editor.MotionSetEditorWindow.Open((ActorAnimationMotionSet)target);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
