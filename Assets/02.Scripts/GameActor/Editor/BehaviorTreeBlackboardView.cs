#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public class BehaviorTreeBlackboardView : VisualElement
    {
        private BehaviorTreeAsset _tree;
        private BehaviorTreeRunner _debugRunner;
        private SerializedObject _serializedTree;

        private readonly List<Action> _runtimeValueRefreshers = new();
        private IVisualElementScheduledItem _runtimePoll;
        private string _runtimeSignature = string.Empty;
        private bool _rebuildQueued;

        public BehaviorTreeBlackboardView()
        {
            style.flexGrow = 1;
            style.backgroundColor = BehaviorTreeEditorStyles.Panel;

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Undo.undoRedoPerformed += QueueRebuild;
                // 플레이 중 디버그 러너의 런타임 값을 주기적으로 반영한다.
                _runtimePoll = schedule.Execute(PollRuntime).Every(150);
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                Undo.undoRedoPerformed -= QueueRebuild;
                _runtimePoll?.Pause();
                _runtimePoll = null;
            });
        }

        public void Bind(BehaviorTreeAsset tree)
        {
            _tree = tree;
            _serializedTree = _tree != null ? new SerializedObject(_tree) : null;
            Redraw();
        }

        public void SetDebugRunner(BehaviorTreeRunner runner)
        {
            if (_debugRunner == runner)
                return;

            _debugRunner = runner;
            QueueRebuild();
        }

        /// <summary>런타임(디버그) 값 표시를 즉시 갱신한다. 디버그 틱 등 외부에서 호출.</summary>
        public void RefreshRuntimeValues()
        {
            foreach (var refresher in _runtimeValueRefreshers)
                refresher();
        }

        /// <summary>
        /// 구조 변화(추가/삭제/키·타입 변경/Undo)를 한 프레임에 한 번으로 합쳐 전체 재구성한다.
        /// 바인딩 콜백 도중 계층을 파괴하지 않도록 지연 실행한다.
        /// </summary>
        private void QueueRebuild()
        {
            if (_rebuildQueued)
                return;

            _rebuildQueued = true;
            schedule.Execute(() =>
            {
                _rebuildQueued = false;
                Redraw();
            });
        }

        public void Redraw()
        {
            Clear();
            _runtimeValueRefreshers.Clear();

            if (_tree == null || _serializedTree == null || _serializedTree.targetObject == null)
            {
                Add(new HelpBox("BT Asset을 선택하세요.", HelpBoxMessageType.Info));
                return;
            }

            _serializedTree.Update();
            var entries = _serializedTree.FindProperty("_blackboard").FindPropertyRelative("_entries");
            var runtimeBlackboard = ResolveRuntimeBlackboard();
            _runtimeSignature = BuildRuntimeSignature(runtimeBlackboard);

            Add(BuildToolbar(entries));

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.contentContainer.style.paddingLeft = 4f;
            scroll.contentContainer.style.paddingRight = 4f;

            for (var i = 0; i < entries.arraySize; i++)
                scroll.Add(BuildEntryCard(entries, i, runtimeBlackboard));

            var addButton = new Button(() => { AddNewEntry(entries); ApplyAndRebuild(); }) { text = "Key 추가" };
            addButton.style.marginTop = 6f;
            addButton.style.height = 24f;
            scroll.Add(addButton);

            BuildRuntimeOnlySection(scroll, runtimeBlackboard, entries);
            Add(scroll);
        }

        private VisualElement BuildToolbar(SerializedProperty entries)
        {
            var toolbar = new Toolbar();

            var addButton = new ToolbarButton(() => { AddNewEntry(entries); ApplyAndRebuild(); })
            {
                text = "Key 추가",
                tooltip = "새 Blackboard Key를 추가합니다."
            };
            toolbar.Add(addButton);

            var fillButton = new ToolbarButton(() =>
            {
                EnemyBlackboardDefaultEntryRegistry.AddMissingEntries(entries);
                ApplyAndRebuild();
            })
            {
                text = "Enemy 기본 Key 보강",
                tooltip = "몬스터 BT에서 자주 쓰는 기본 Blackboard Key 중 누락된 항목을 추가합니다."
            };
            toolbar.Add(fillButton);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            var countLabel = new Label($"{entries.arraySize} keys");
            countLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            countLabel.style.fontSize = 10f;
            countLabel.style.color = BehaviorTreeEditorStyles.TextMuted;
            countLabel.style.marginRight = 4f;
            toolbar.Add(countLabel);

            return toolbar;
        }

        private VisualElement BuildEntryCard(SerializedProperty entries, int index, Blackboard runtimeBlackboard)
        {
            var entry = entries.GetArrayElementAtIndex(index);
            var keyProp = entry.FindPropertyRelative("_key");
            var typeProp = entry.FindPropertyRelative("_valueType");
            var currentKey = keyProp.stringValue;

            var card = CreateCardBox();

            if (!string.IsNullOrWhiteSpace(currentKey))
            {
                var keyLabel = BehaviorTreeDisplayNameRegistry.GetBlackboardLabel(currentKey);
                var title = new Label(BehaviorTreeDisplayNameRegistry.FormatWithRawName(keyLabel, currentKey));
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.marginBottom = 2f;
                card.Add(title);
            }

            var keyRow = new VisualElement();
            keyRow.style.flexDirection = FlexDirection.Row;

            var keyField = new TextField { isDelayed = true, isReadOnly = true };
            keyField.style.flexGrow = 1;
            keyField.BindProperty(keyProp);
            keyRow.Add(keyField);

            var renameButton = new Button(() => PromptRename(keyProp.stringValue))
            {
                text = "Rename",
                tooltip = $"'{currentKey}' Key를 참조하는 모든 노드를 함께 변경합니다."
            };
            renameButton.style.width = 64f;
            renameButton.SetEnabled(false);
            renameButton.tooltip = "Key 이름은 BlackboardKeyRegistry에서 변경하고 alias로 이전 이름을 유지하세요.";
            keyRow.Add(renameButton);

            var capturedIndex = index;
            var deleteButton = new Button(() =>
            {
                entries.DeleteArrayElementAtIndex(capturedIndex);
                ApplyAndRebuild();
            })
            {
                text = "삭제"
            };
            deleteButton.style.width = 44f;
            keyRow.Add(deleteButton);
            card.Add(keyRow);

            var typeField = new PropertyField(typeProp, "Type");
            typeField.BindProperty(typeProp);
            card.Add(typeField);

            // 키/타입이 바뀌면 제목·값 필드 종류·런타임 매칭이 달라지므로 카드 전체를 재구성한다.
            card.TrackPropertyValue(keyProp, _ => QueueRebuild());
            card.TrackPropertyValue(typeProp, _ => QueueRebuild());

            card.Add(BuildValueRow(entry, typeProp, currentKey, runtimeBlackboard));
            return card;
        }

        private VisualElement BuildValueRow(
            SerializedProperty entry,
            SerializedProperty typeProp,
            string currentKey,
            Blackboard runtimeBlackboard)
        {
            var type = (BlackboardValueType)typeProp.enumValueIndex;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            var assetColumn = new VisualElement();
            assetColumn.style.flexGrow = 1;
            assetColumn.style.flexBasis = 0f;
            assetColumn.Add(CreateColumnLabel("Asset"));
            assetColumn.Add(CreateAssetValueField(entry, type));
            row.Add(assetColumn);

            var runtimeEntry = runtimeBlackboard != null && !string.IsNullOrWhiteSpace(currentKey)
                ? runtimeBlackboard.FindEntry(currentKey)
                : null;
            if (runtimeEntry != null)
            {
                var runtimeColumn = new VisualElement();
                runtimeColumn.style.flexGrow = 1;
                runtimeColumn.style.flexBasis = 0f;
                runtimeColumn.style.marginLeft = 6f;
                runtimeColumn.Add(CreateColumnLabel("Runtime"));
                runtimeColumn.Add(CreateRuntimeValueField(runtimeEntry));
                row.Add(runtimeColumn);
            }

            return row;
        }

        private static VisualElement CreateAssetValueField(SerializedProperty entry, BlackboardValueType type)
        {
            var valueProp = entry.FindPropertyRelative(GetValuePropertyName(type));
            var field = new PropertyField(valueProp, "Value");
            field.BindProperty(valueProp);
            return field;
        }

        private static string GetValuePropertyName(BlackboardValueType type)
        {
            return type switch
            {
                BlackboardValueType.Bool => "_boolValue",
                BlackboardValueType.Int => "_intValue",
                BlackboardValueType.Float => "_floatValue",
                BlackboardValueType.String => "_stringValue",
                BlackboardValueType.Vector3 => "_vector3Value",
                BlackboardValueType.Object => "_objectValue",
                _ => "_boolValue"
            };
        }

        /// <summary>
        /// 런타임 Blackboard 값을 읽기 전용으로 표시하는 필드를 만들고,
        /// 폴링 시 최신 값으로 갱신되도록 refresher를 등록한다.
        /// </summary>
        private VisualElement CreateRuntimeValueField(BlackboardEntry runtimeEntry)
        {
            VisualElement field;
            switch (runtimeEntry.ValueType)
            {
                case BlackboardValueType.Bool:
                {
                    var toggle = new Toggle("Value") { value = runtimeEntry.BoolValue };
                    _runtimeValueRefreshers.Add(() => toggle.SetValueWithoutNotify(runtimeEntry.BoolValue));
                    field = toggle;
                    break;
                }
                case BlackboardValueType.Int:
                {
                    var intField = new IntegerField("Value") { value = runtimeEntry.IntValue };
                    _runtimeValueRefreshers.Add(() => intField.SetValueWithoutNotify(runtimeEntry.IntValue));
                    field = intField;
                    break;
                }
                case BlackboardValueType.Float:
                {
                    var floatField = new FloatField("Value") { value = runtimeEntry.FloatValue };
                    _runtimeValueRefreshers.Add(() => floatField.SetValueWithoutNotify(runtimeEntry.FloatValue));
                    field = floatField;
                    break;
                }
                case BlackboardValueType.String:
                {
                    var textField = new TextField("Value") { value = runtimeEntry.StringValue ?? string.Empty };
                    _runtimeValueRefreshers.Add(() => textField.SetValueWithoutNotify(runtimeEntry.StringValue ?? string.Empty));
                    field = textField;
                    break;
                }
                case BlackboardValueType.Vector3:
                {
                    var vectorField = new Vector3Field("Value") { value = runtimeEntry.Vector3Value };
                    _runtimeValueRefreshers.Add(() => vectorField.SetValueWithoutNotify(runtimeEntry.Vector3Value));
                    field = vectorField;
                    break;
                }
                case BlackboardValueType.Object:
                {
                    var objectField = new ObjectField("Value")
                    {
                        objectType = typeof(UnityEngine.Object),
                        allowSceneObjects = true,
                        value = runtimeEntry.ObjectValue
                    };
                    _runtimeValueRefreshers.Add(() => objectField.SetValueWithoutNotify(runtimeEntry.ObjectValue));
                    field = objectField;
                    break;
                }
                default:
                    field = new Label("-");
                    break;
            }

            field.SetEnabled(false);
            return field;
        }

        private void BuildRuntimeOnlySection(VisualElement parent, Blackboard runtimeBlackboard, SerializedProperty entries)
        {
            if (runtimeBlackboard == null)
                return;

            VisualElement section = null;
            foreach (var runtimeEntry in runtimeBlackboard.Entries)
            {
                if (runtimeEntry == null || string.IsNullOrWhiteSpace(runtimeEntry.Key))
                    continue;

                if (HasAssetEntry(entries, runtimeEntry.Key))
                    continue;

                if (section == null)
                {
                    section = new VisualElement();
                    section.style.marginTop = 8f;

                    var header = new Label("Runtime Only");
                    header.style.unityFontStyleAndWeight = FontStyle.Bold;
                    header.style.marginBottom = 2f;
                    section.Add(header);
                }

                var card = CreateCardBox();
                card.Add(new Label($"{runtimeEntry.Key} ({runtimeEntry.ValueType})"));
                card.Add(CreateRuntimeValueField(runtimeEntry));
                card.SetEnabled(false);
                section.Add(card);
            }

            if (section != null)
                parent.Add(section);
        }

        private void PollRuntime()
        {
            var runtimeBlackboard = ResolveRuntimeBlackboard();
            var signature = BuildRuntimeSignature(runtimeBlackboard);
            if (!string.Equals(signature, _runtimeSignature, StringComparison.Ordinal))
            {
                // 러너 시작/중지 또는 런타임 키 구성 변화 → 병기 컬럼/Runtime Only 섹션 재구성
                QueueRebuild();
                return;
            }

            RefreshRuntimeValues();
        }

        private static string BuildRuntimeSignature(Blackboard runtimeBlackboard)
        {
            if (runtimeBlackboard == null)
                return string.Empty;

            var builder = new StringBuilder(128);
            foreach (var entry in runtimeBlackboard.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                    continue;

                builder.Append(entry.Key).Append(':').Append((int)entry.ValueType).Append('|');
            }

            return builder.ToString();
        }

        private void ApplyAndRebuild()
        {
            _serializedTree?.ApplyModifiedProperties();
            QueueRebuild();
        }

        private static VisualElement CreateCardBox()
        {
            var card = new VisualElement();
            card.style.marginTop = 4f;
            card.style.marginBottom = 2f;
            card.style.paddingLeft = 6f;
            card.style.paddingRight = 6f;
            card.style.paddingTop = 5f;
            card.style.paddingBottom = 5f;
            card.style.backgroundColor = BehaviorTreeEditorStyles.PanelAlt;
            card.style.borderTopColor = BehaviorTreeEditorStyles.Border;
            card.style.borderRightColor = BehaviorTreeEditorStyles.Border;
            card.style.borderBottomColor = BehaviorTreeEditorStyles.Border;
            card.style.borderLeftColor = BehaviorTreeEditorStyles.Border;
            card.style.borderTopWidth = 1f;
            card.style.borderRightWidth = 1f;
            card.style.borderBottomWidth = 1f;
            card.style.borderLeftWidth = 1f;
            card.style.borderTopLeftRadius = 5f;
            card.style.borderTopRightRadius = 5f;
            card.style.borderBottomLeftRadius = 5f;
            card.style.borderBottomRightRadius = 5f;
            return card;
        }

        private static Label CreateColumnLabel(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 10f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = BehaviorTreeEditorStyles.TextMuted;
            return label;
        }

        private static void AddNewEntry(SerializedProperty entries)
        {
            foreach (EnemyBlackboardDefaultEntry definition
                     in EnemyBlackboardDefaultEntryRegistry.Entries)
            {
                if (HasAssetEntry(entries, definition.Key))
                    continue;

                AddEntry(
                    entries,
                    definition.Key,
                    definition.Type,
                    definition.BoolValue,
                    definition.IntValue,
                    definition.FloatValue,
                    definition.StringValue);
                return;
            }
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
            entry.FindPropertyRelative("_stableId").stringValue =
                BlackboardKeyRegistry.TryResolve(
                    key,
                    out BlackboardKeyReference reference)
                    ? reference.StableId
                    : string.Empty;
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

        private static bool HasAssetEntry(SerializedProperty entries, string key)
        {
            for (var i = 0; i < entries.arraySize; i++)
            {
                var keyProp = entries.GetArrayElementAtIndex(i).FindPropertyRelative("_key");
                if (string.Equals(keyProp.stringValue, key, StringComparison.Ordinal))
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
            if (result.TotalFieldUpdates > 0 || !string.Equals(oldKey, newKey, StringComparison.Ordinal))
            {
                AssetDatabase.SaveAssets();
                _serializedTree = new SerializedObject(_tree);
                Redraw();
            }

            Debug.Log($"Blackboard Key 변경: '{oldKey}' → '{newKey}'. 노드 {result.TouchedNodes}개, Selector {result.UpdatedSelectorFields}개, Legacy Key {result.UpdatedLegacyFields}개 업데이트.");
        }
    }

    internal sealed class BehaviorTreeKeyRenameDialog : EditorWindow
    {
        private string _oldKey;
        private string _newKey;
        private int _references;
        private Action<string, string> _onConfirm;

        public static void Show(BehaviorTreeAsset tree, string oldKey, int references, Action<string, string> onConfirm)
        {
            var window = CreateInstance<BehaviorTreeKeyRenameDialog>();
            window.titleContent = new GUIContent("Rename Blackboard Key");
            window.minSize = new Vector2(360f, 150f);
            window.maxSize = new Vector2(420f, 180f);
            window._oldKey = oldKey;
            window._newKey = oldKey;
            window._references = references;
            window._onConfirm = onConfirm;
            window.BuildUI();
            window.ShowModal();
        }

        private void BuildUI()
        {
            var root = rootVisualElement;
            root.Clear();
            root.style.paddingLeft = 10f;
            root.style.paddingRight = 10f;
            root.style.paddingTop = 8f;

            var title = new Label("Blackboard Key 변경");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(title);
            root.Add(new Label($"기존 이름: {_oldKey}"));
            root.Add(new Label($"참조 노드 필드: {_references}개"));

            var keyField = new TextField("새 Key") { value = _newKey };
            keyField.style.marginTop = 6f;
            root.Add(keyField);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginTop = 8f;

            var cancelButton = new Button(Close) { text = "취소" };
            cancelButton.style.flexGrow = 1;
            buttonRow.Add(cancelButton);

            var confirmButton = new Button(() =>
            {
                _onConfirm?.Invoke(_oldKey, _newKey);
                Close();
            })
            {
                text = "변경"
            };
            confirmButton.style.flexGrow = 1;
            confirmButton.SetEnabled(false);
            buttonRow.Add(confirmButton);
            root.Add(buttonRow);

            keyField.RegisterValueChangedCallback(evt =>
            {
                _newKey = evt.newValue;
                confirmButton.SetEnabled(
                    !string.IsNullOrWhiteSpace(_newKey) &&
                    !string.Equals(_newKey, _oldKey, StringComparison.Ordinal));
            });
        }
    }

    internal readonly struct EnemyBlackboardDefaultEntry
    {
        public readonly string StableId;
        public readonly string Key;
        public readonly BlackboardValueType Type;
        public readonly string Label;
        public readonly bool BoolValue;
        public readonly int IntValue;
        public readonly float FloatValue;
        public readonly string StringValue;

        public EnemyBlackboardDefaultEntry(
            string stableId,
            string key,
            BlackboardValueType type,
            string label,
            bool boolValue = false,
            int intValue = 0,
            float floatValue = 0f,
            string stringValue = "")
        {
            StableId = stableId;
            Key = key;
            Type = type;
            Label = label;
            BoolValue = boolValue;
            IntValue = intValue;
            FloatValue = floatValue;
            StringValue = stringValue;
        }
    }

    internal static class EnemyBlackboardDefaultEntryRegistry
    {
        public static IReadOnlyList<EnemyBlackboardDefaultEntry> Entries => BehaviorTreeEditorRegistryData.BlackboardEntries;

        public static bool TryGetEntry(string key, out EnemyBlackboardDefaultEntry entry)
            => BehaviorTreeEditorRegistryData.TryGetBlackboardEntry(key, out entry);

        public static void ApplyDefaults(Blackboard blackboard)
        {
            if (blackboard == null)
                return;

            foreach (var entry in Entries)
                ApplyDefault(blackboard, entry);
        }

        public static void AddMissingEntries(SerializedProperty entries)
        {
            foreach (var entry in Entries)
            {
                if (HasAssetEntry(entries, entry.Key))
                    continue;

                AddEntry(entries, entry);
            }
        }

        private static void AddEntry(SerializedProperty entries, EnemyBlackboardDefaultEntry definition)
        {
            entries.InsertArrayElementAtIndex(entries.arraySize);
            var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            entry.FindPropertyRelative("_stableId").stringValue = definition.StableId;
            entry.FindPropertyRelative("_key").stringValue = definition.Key;
            entry.FindPropertyRelative("_valueType").enumValueIndex = (int)definition.Type;
            entry.FindPropertyRelative("_boolValue").boolValue = definition.BoolValue;
            entry.FindPropertyRelative("_intValue").intValue = definition.IntValue;
            entry.FindPropertyRelative("_floatValue").floatValue = definition.FloatValue;
            entry.FindPropertyRelative("_stringValue").stringValue = definition.StringValue;
            entry.FindPropertyRelative("_vector3Value").vector3Value = Vector3.zero;
            entry.FindPropertyRelative("_objectValue").objectReferenceValue = null;
        }

        private static void ApplyDefault(Blackboard blackboard, EnemyBlackboardDefaultEntry entry)
        {
            switch (entry.Type)
            {
                case BlackboardValueType.Bool:
                    blackboard.SetBool(entry.Key, entry.BoolValue);
                    break;
                case BlackboardValueType.Int:
                    blackboard.SetInt(entry.Key, entry.IntValue);
                    break;
                case BlackboardValueType.Float:
                    blackboard.SetFloat(entry.Key, entry.FloatValue);
                    break;
                case BlackboardValueType.String:
                    blackboard.SetString(entry.Key, entry.StringValue);
                    break;
                case BlackboardValueType.Vector3:
                    blackboard.SetVector3(entry.Key, Vector3.zero);
                    break;
                case BlackboardValueType.Object:
                    blackboard.SetObject(entry.Key, null);
                    break;
            }
        }

        private static bool HasAssetEntry(SerializedProperty entries, string key)
        {
            for (var i = 0; i < entries.arraySize; i++)
            {
                var keyProp = entries.GetArrayElementAtIndex(i).FindPropertyRelative("_key");
                if (string.Equals(keyProp.stringValue, key, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

    }
}
#endif
