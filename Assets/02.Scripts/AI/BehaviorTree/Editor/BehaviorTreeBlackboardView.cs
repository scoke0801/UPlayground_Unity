#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public class BehaviorTreeBlackboardView : VisualElement
    {
        private BehaviorTreeAsset _tree;
        private BehaviorTreeRunner _debugRunner;
        private SerializedObject _serializedTree;

        public BehaviorTreeBlackboardView()
        {
            style.flexGrow = 1;
            style.backgroundColor = BehaviorTreeEditorStyles.Panel;
        }

        public void Bind(BehaviorTreeAsset tree)
        {
            _tree = tree;
            _serializedTree = _tree != null ? new SerializedObject(_tree) : null;
            Redraw();
        }

        public void SetDebugRunner(BehaviorTreeRunner runner)
        {
            _debugRunner = runner;
            MarkDirtyRepaint();
        }

        public void Redraw()
        {
            Clear();
            Add(new IMGUIContainer(DrawBlackboard));
        }

        private void DrawBlackboard()
        {
            if (_tree == null || _serializedTree == null)
            {
                EditorGUILayout.HelpBox("BT Asset을 선택하세요.", MessageType.Info);
                return;
            }

            _serializedTree.Update();
            var blackboard = _serializedTree.FindProperty("_blackboard");
            var entries = blackboard.FindPropertyRelative("_entries");

            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("_key"), GUIContent.none);
                if (GUILayout.Button("삭제", GUILayout.Width(44f)))
                {
                    entries.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(entry.FindPropertyRelative("_valueType"), new GUIContent("Type"));
                DrawValueField(entry);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Key 추가"))
            {
                entries.InsertArrayElementAtIndex(entries.arraySize);
                var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                entry.FindPropertyRelative("_key").stringValue = "NewKey";
                entry.FindPropertyRelative("_valueType").enumValueIndex = (int)BlackboardValueType.Bool;
            }

            _serializedTree.ApplyModifiedProperties();
            DrawRuntimeBlackboard();
        }

        private void DrawRuntimeBlackboard()
        {
            if (!Application.isPlaying || _debugRunner == null || !_debugRunner.DebugMode)
                return;

            var runtimeBlackboard = _debugRunner.RuntimeTree?.Blackboard;
            if (runtimeBlackboard == null)
                return;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Runtime Values", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            foreach (var entry in runtimeBlackboard.Entries)
            {
                if (entry == null)
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(entry.Key, entry.ValueType.ToString());
                DrawRuntimeValue(entry);
                EditorGUILayout.EndVertical();
            }
            EditorGUI.EndDisabledGroup();
        }

        private static void DrawRuntimeValue(BlackboardEntry entry)
        {
            switch (entry.ValueType)
            {
                case BlackboardValueType.Bool:
                    EditorGUILayout.Toggle("Value", entry.BoolValue);
                    break;
                case BlackboardValueType.Int:
                    EditorGUILayout.IntField("Value", entry.IntValue);
                    break;
                case BlackboardValueType.Float:
                    EditorGUILayout.FloatField("Value", entry.FloatValue);
                    break;
                case BlackboardValueType.String:
                    EditorGUILayout.TextField("Value", entry.StringValue);
                    break;
                case BlackboardValueType.Vector3:
                    EditorGUILayout.Vector3Field("Value", entry.Vector3Value);
                    break;
                case BlackboardValueType.Object:
                    EditorGUILayout.ObjectField("Value", entry.ObjectValue, typeof(UnityEngine.Object), true);
                    break;
            }
        }

        private static void DrawValueField(SerializedProperty entry)
        {
            var type = (BlackboardValueType)entry.FindPropertyRelative("_valueType").enumValueIndex;
            switch (type)
            {
                case BlackboardValueType.Bool:
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_boolValue"), new GUIContent("Value"));
                    break;
                case BlackboardValueType.Int:
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_intValue"), new GUIContent("Value"));
                    break;
                case BlackboardValueType.Float:
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_floatValue"), new GUIContent("Value"));
                    break;
                case BlackboardValueType.String:
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_stringValue"), new GUIContent("Value"));
                    break;
                case BlackboardValueType.Vector3:
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_vector3Value"), new GUIContent("Value"));
                    break;
                case BlackboardValueType.Object:
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_objectValue"), new GUIContent("Value"));
                    break;
            }
        }
    }
}
#endif
