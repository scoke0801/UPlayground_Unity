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

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.Add(new IMGUIContainer(DrawBlackboard));
            Add(scroll);
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

            var runtimeBlackboard = ResolveRuntimeBlackboard();
            DrawEntryToolbar(entries);

            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var keyProp = entry.FindPropertyRelative("_key");
                var typeProp = entry.FindPropertyRelative("_valueType");
                var currentKey = keyProp.stringValue;
                var keyLabel = BehaviorTreeDisplayNameRegistry.GetBlackboardLabel(currentKey);
                var runtimeEntry = runtimeBlackboard != null && !string.IsNullOrWhiteSpace(currentKey)
                    ? runtimeBlackboard.FindEntry(currentKey)
                    : null;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (!string.IsNullOrWhiteSpace(currentKey))
                    EditorGUILayout.LabelField(BehaviorTreeDisplayNameRegistry.FormatWithRawName(keyLabel, currentKey), EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(keyProp, GUIContent.none);
                if (GUILayout.Button(new GUIContent("Rename", $"'{currentKey}' Key를 참조하는 모든 노드를 함께 변경합니다."), GUILayout.Width(64f)))
                {
                    PromptRename(currentKey);
                }
                if (GUILayout.Button(new GUIContent("삭제"), GUILayout.Width(44f)))
                {
                    entries.DeleteArrayElementAtIndex(i);
                    _serializedTree.ApplyModifiedProperties();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(typeProp, new GUIContent("Type"));
                DrawSideBySideValue(entry, runtimeEntry);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("Key 추가"))
                AddNewEntry(entries);

            _serializedTree.ApplyModifiedProperties();
            DrawRuntimeOnlyEntries(runtimeBlackboard, entries);
        }

        private void DrawEntryToolbar(SerializedProperty entries)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(new GUIContent("Key 추가", "새 Blackboard Key를 추가합니다."), EditorStyles.toolbarButton))
                AddNewEntry(entries);

            if (GUILayout.Button(new GUIContent("Enemy 기본 Key 보강", "몬스터 BT에서 자주 쓰는 기본 Blackboard Key 중 누락된 항목을 추가합니다."), EditorStyles.toolbarButton))
                AddMissingEnemyDefaultEntries(entries);

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"{entries.arraySize} keys", EditorStyles.miniLabel, GUILayout.Width(54f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        private static void AddNewEntry(SerializedProperty entries)
        {
            AddEntry(entries, "NewKey", BlackboardValueType.Bool);
        }

        private static void AddMissingEnemyDefaultEntries(SerializedProperty entries)
        {
            AddEntryIfMissing(entries, EnemyBlackboardKeys.HasTarget, BlackboardValueType.Bool);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.Target, BlackboardValueType.Object);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.DistanceToTarget, BlackboardValueType.Float, floatValue: float.MaxValue);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.CurrentState, BlackboardValueType.String);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.HpPercent, BlackboardValueType.Float, floatValue: 1f);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.CurrentPhaseName, BlackboardValueType.String);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.PhaseIndex, BlackboardValueType.Int, intValue: -1);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.AllowCharge, BlackboardValueType.Bool);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.AllowFlank, BlackboardValueType.Bool);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.MaxConsecutiveAttacks, BlackboardValueType.Int, intValue: 3);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.ContinueAttackChance, BlackboardValueType.Float, floatValue: 0.3f);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.GuardChance, BlackboardValueType.Float, floatValue: 0.25f);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.RetreatChance, BlackboardValueType.Float, floatValue: 0.2f);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.IsPlayerAttacking, BlackboardValueType.Bool);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.IsPlayerGuarding, BlackboardValueType.Bool);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.IsPlayerStaggered, BlackboardValueType.Bool);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.IsPlayerRecovering, BlackboardValueType.Bool);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.IsPlayerDodgingFrequently, BlackboardValueType.Bool);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.CanUseSkill, BlackboardValueType.Bool);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.HasAttackSlot, BlackboardValueType.Bool);
            AddEntryIfMissing(entries, EnemyBlackboardKeys.NextActionAllowedTime, BlackboardValueType.Float);
        }

        private static void AddEntryIfMissing(
            SerializedProperty entries,
            string key,
            BlackboardValueType type,
            bool boolValue = false,
            int intValue = 0,
            float floatValue = 0f,
            string stringValue = "")
        {
            if (HasAssetEntry(entries, key))
                return;

            AddEntry(entries, key, type, boolValue, intValue, floatValue, stringValue);
        }

        private static void AddEntry(
            SerializedProperty entries,
            string key,
            BlackboardValueType type,
            bool boolValue = false,
            int intValue = 0,
            float floatValue = 0f,
            string stringValue = "")
        {
            entries.InsertArrayElementAtIndex(entries.arraySize);
            var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            entry.FindPropertyRelative("_key").stringValue = key;
            entry.FindPropertyRelative("_valueType").enumValueIndex = (int)type;
            entry.FindPropertyRelative("_boolValue").boolValue = boolValue;
            entry.FindPropertyRelative("_intValue").intValue = intValue;
            entry.FindPropertyRelative("_floatValue").floatValue = floatValue;
            entry.FindPropertyRelative("_stringValue").stringValue = stringValue;
            entry.FindPropertyRelative("_vector3Value").vector3Value = Vector3.zero;
            entry.FindPropertyRelative("_objectValue").objectReferenceValue = null;
        }

        private Blackboard ResolveRuntimeBlackboard()
        {
            if (!Application.isPlaying || _debugRunner == null || !_debugRunner.DebugMode)
                return null;

            return _debugRunner.RuntimeTree?.Blackboard;
        }

        private void DrawSideBySideValue(SerializedProperty entry, BlackboardEntry runtimeEntry)
        {
            var type = (BlackboardValueType)entry.FindPropertyRelative("_valueType").enumValueIndex;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Asset", EditorStyles.miniBoldLabel);
            DrawValueField(entry, type);
            EditorGUILayout.EndVertical();

            if (runtimeEntry != null)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Runtime", EditorStyles.miniBoldLabel);
                EditorGUI.BeginDisabledGroup(true);
                DrawRuntimeValue(runtimeEntry);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRuntimeOnlyEntries(Blackboard runtimeBlackboard, SerializedProperty entries)
        {
            if (runtimeBlackboard == null)
                return;

            var hasRuntimeOnly = false;
            foreach (var runtimeEntry in runtimeBlackboard.Entries)
            {
                if (runtimeEntry == null || string.IsNullOrWhiteSpace(runtimeEntry.Key))
                    continue;

                if (HasAssetEntry(entries, runtimeEntry.Key))
                    continue;

                if (!hasRuntimeOnly)
                {
                    EditorGUILayout.Space(8f);
                    EditorGUILayout.LabelField("Runtime Only", EditorStyles.boldLabel);
                    hasRuntimeOnly = true;
                }

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{runtimeEntry.Key} ({runtimeEntry.ValueType})");
                DrawRuntimeValue(runtimeEntry);
                EditorGUILayout.EndVertical();
                EditorGUI.EndDisabledGroup();
            }
        }

        private static bool HasAssetEntry(SerializedProperty entries, string key)
        {
            for (var i = 0; i < entries.arraySize; i++)
            {
                var keyProp = entries.GetArrayElementAtIndex(i).FindPropertyRelative("_key");
                if (string.Equals(keyProp.stringValue, key, System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void PromptRename(string currentKey)
        {
            if (_tree == null || string.IsNullOrWhiteSpace(currentKey))
                return;

            var references = BehaviorTreeBlackboardKeyRenamer.CountReferences(_tree, currentKey);
            BehaviorTreeKeyRenameDialog.Show(_tree, currentKey, references, OnRenameConfirmed);
        }

        private void OnRenameConfirmed(string oldKey, string newKey)
        {
            if (_tree == null)
                return;

            var result = BehaviorTreeBlackboardKeyRenamer.RenameKey(_tree, oldKey, newKey);
            if (result.TotalFieldUpdates > 0 || !string.Equals(oldKey, newKey, System.StringComparison.Ordinal))
            {
                AssetDatabase.SaveAssets();
                _serializedTree = new SerializedObject(_tree);
                Redraw();
            }

            Debug.Log($"Blackboard Key 변경: '{oldKey}' → '{newKey}'. 노드 {result.TouchedNodes}개, Selector {result.UpdatedSelectorFields}개, Legacy Key {result.UpdatedLegacyFields}개 업데이트.");
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

        private static void DrawValueField(SerializedProperty entry, BlackboardValueType type)
        {
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

    internal sealed class BehaviorTreeKeyRenameDialog : EditorWindow
    {
        private string _oldKey;
        private string _newKey;
        private int _references;
        private System.Action<string, string> _onConfirm;

        public static void Show(BehaviorTreeAsset tree, string oldKey, int references, System.Action<string, string> onConfirm)
        {
            var window = CreateInstance<BehaviorTreeKeyRenameDialog>();
            window.titleContent = new GUIContent("Rename Blackboard Key");
            window.minSize = new Vector2(360f, 150f);
            window.maxSize = new Vector2(420f, 180f);
            window._oldKey = oldKey;
            window._newKey = oldKey;
            window._references = references;
            window._onConfirm = onConfirm;
            window.ShowModal();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Blackboard Key 변경", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"기존 이름: {_oldKey}");
            EditorGUILayout.LabelField($"참조 노드 필드: {_references}개");
            EditorGUILayout.Space(6f);

            _newKey = EditorGUILayout.TextField("새 Key", _newKey);

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("취소"))
            {
                Close();
            }

            GUI.enabled = !string.IsNullOrWhiteSpace(_newKey) && !string.Equals(_newKey, _oldKey, System.StringComparison.Ordinal);
            if (GUILayout.Button("변경"))
            {
                _onConfirm?.Invoke(_oldKey, _newKey);
                Close();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
