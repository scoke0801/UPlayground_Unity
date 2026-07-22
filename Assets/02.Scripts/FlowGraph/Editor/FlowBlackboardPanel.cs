using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>
    /// 그래프 블랙보드 변수 선언 패널.
    /// 이름/타입/기본값 저작과 검색, 사용처 이동, 런타임 값 확인을 한 곳에서 제공한다.
    /// </summary>
    public sealed class FlowBlackboardPanel : VisualElement
    {
        private readonly struct VariableUsage
        {
            public VariableUsage(FlowNode node, FlowVariableValue value)
            {
                Node = node;
                Value = value;
            }

            public FlowNode Node { get; }
            public FlowVariableValue Value { get; }
        }

        private static readonly Color DividerColor = new(0.1f, 0.1f, 0.1f);
        private static readonly Color CardColor = new(0.19f, 0.19f, 0.19f);
        private static readonly Color RuntimeColor = new(0.45f, 0.85f, 0.55f);
        private static readonly Color ErrorColor = new(0.94f, 0.33f, 0.31f);

        private readonly Action _onChanged;
        private readonly Action<string> _focusNode;
        private readonly ScrollView _rows;
        private readonly TextField _searchField;
        private readonly Label _countLabel;
        private readonly Label _runtimeStatus;
        private readonly Dictionary<string, Label> _runtimeLabels = new();
        private FlowGraphSO _graph;

        public FlowBlackboardPanel(Action onChanged, Action<string> focusNode = null)
        {
            _onChanged = onChanged;
            _focusNode = focusNode;

            style.height = 220;
            style.flexShrink = 0;
            style.borderTopWidth = 1;
            style.borderTopColor = DividerColor;

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 28,
                    paddingLeft = 8,
                    paddingRight = 4,
                },
            };
            header.Add(new Label("Blackboard")
            {
                tooltip = "그래프가 실행될 때 컨텍스트마다 별도로 생성되는 변수",
                style = { unityFontStyleAndWeight = FontStyle.Bold },
            });
            _countLabel = new Label
            {
                style = { marginLeft = 5, opacity = 0.55f, flexGrow = 1, fontSize = 10 },
            };
            header.Add(_countLabel);
            var addButton = new Button(AddVariable)
            {
                text = "+ 변수",
                tooltip = "블랙보드 변수 추가",
                style = { height = 22 },
            };
            header.Add(addButton);
            Add(header);

            var searchRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 5,
                    paddingRight = 5,
                    paddingBottom = 3,
                },
            };
            _searchField = new TextField
            {
                tooltip = "변수 이름 또는 타입으로 필터링",
                style = { flexGrow = 1 },
            };
            _searchField.RegisterValueChangedCallback(_ => Rebuild());
            searchRow.Add(_searchField);
            var clearSearch = new Button(() => _searchField.value = string.Empty)
            {
                text = "×",
                tooltip = "검색 지우기",
                style = { width = 22, height = 20, marginLeft = 2 },
            };
            searchRow.Add(clearSearch);
            Add(searchRow);

            _runtimeStatus = new Label
            {
                style =
                {
                    display = DisplayStyle.None,
                    color = RuntimeColor,
                    fontSize = 10,
                    paddingLeft = 8,
                    paddingBottom = 2,
                },
            };
            Add(_runtimeStatus);

            _rows = new ScrollView { style = { flexGrow = 1 } };
            Add(_rows);

            Rebuild();
        }

        public void SetGraph(FlowGraphSO graph)
        {
            _graph = graph;
            Rebuild();
        }

        private void AddVariable()
        {
            if (_graph == null)
                return;

            RecordUndo("Blackboard 변수 추가");
            string baseName = "variable";
            string name = baseName;
            int suffix = 1;
            while (_graph.HasVariable(name))
                name = $"{baseName}{suffix++}";

            var added = new FlowVariableDef { name = name };
            _graph.variables.Add(added);
            MarkChanged();
            _searchField.SetValueWithoutNotify(string.Empty);
            Rebuild(added);
        }

        /// <summary>Play Mode에서 첫 실행 컨텍스트의 값을 선언 옆에 표시한다.</summary>
        public void UpdateRuntimeValues(FlowGraphRunner runner)
        {
            int contextCount = Application.isPlaying && runner != null ? runner.ActiveContexts.Count : 0;
            FlowContext context = contextCount > 0 ? runner.ActiveContexts[0] : null;

            _runtimeStatus.style.display = contextCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _runtimeStatus.text = contextCount > 0
                ? $"● 실행값 · 컨텍스트 {contextCount}개 중 첫 번째"
                : string.Empty;

            foreach (KeyValuePair<string, Label> pair in _runtimeLabels)
            {
                string text = string.Empty;
                if (context != null && context.TryGet(pair.Key, out object value))
                    text = $"실행값  {FormatValue(value)}";
                if (pair.Value.text != text)
                    pair.Value.text = text;
            }
        }

        private void Rebuild(FlowVariableDef focus = null)
        {
            _rows.Clear();
            _runtimeLabels.Clear();
            int variableCount = _graph?.variables?.Count ?? 0;
            _countLabel.text = variableCount > 0 ? variableCount.ToString() : string.Empty;

            if (_graph == null)
            {
                AddEmptyState("그래프를 열면 변수를 선언할 수 있습니다.");
                return;
            }

            string query = _searchField.value?.Trim();
            int visibleCount = 0;
            for (int i = 0; i < _graph.variables.Count; i++)
            {
                FlowVariableDef def = _graph.variables[i];
                if (def == null || !MatchesSearch(def, query))
                    continue;

                visibleCount++;
                _rows.Add(CreateRow(def, def == focus));
            }

            if (variableCount == 0)
                AddEmptyState("아직 변수가 없습니다. ‘+ 변수’로 추가하세요.");
            else if (visibleCount == 0)
                AddEmptyState("검색 결과가 없습니다.");
        }

        private void AddEmptyState(string message)
        {
            _rows.Add(new Label(message)
            {
                style =
                {
                    paddingLeft = 10,
                    paddingRight = 8,
                    paddingTop = 10,
                    whiteSpace = WhiteSpace.Normal,
                    opacity = 0.55f,
                },
            });
        }

        private VisualElement CreateRow(FlowVariableDef def, bool focusName)
        {
            List<VariableUsage> usages = FindUsages(def.name);
            var card = new VisualElement
            {
                style =
                {
                    marginLeft = 5,
                    marginRight = 5,
                    marginBottom = 4,
                    paddingLeft = 5,
                    paddingRight = 4,
                    paddingTop = 3,
                    paddingBottom = 3,
                    backgroundColor = CardColor,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3,
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                },
            };

            var top = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            string originalName = def.name ?? string.Empty;
            var nameField = new TextField
            {
                value = originalName,
                tooltip = "변수 이름. 변경하면 기존 노드 참조도 함께 갱신됩니다.",
                style = { flexGrow = 1, minWidth = 70 },
            };
            bool invalidName = string.IsNullOrWhiteSpace(originalName) || IsDuplicateName(def, originalName);
            SetNameFieldValidity(nameField, invalidName);
            nameField.RegisterCallback<FocusOutEvent>(_ => CommitRename(def, originalName, nameField));
            nameField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    nameField.Blur();
            });
            top.Add(nameField);

            var usageButton = new Button(() => ShowUsages(def.name))
            {
                text = usages.Count > 0 ? $"사용 {usages.Count}" : "미사용",
                tooltip = usages.Count > 0 ? "클릭하여 사용 노드로 이동" : "이 변수를 참조하는 노드가 없습니다.",
                style = { height = 20, marginLeft = 2, fontSize = 9 },
            };
            usageButton.SetEnabled(usages.Count > 0);
            top.Add(usageButton);

            var menuButton = new Button(() => ShowVariableMenu(def))
            {
                text = "⋮",
                tooltip = "변수 작업",
                style = { width = 22, height = 20, marginLeft = 2 },
            };
            top.Add(menuButton);
            card.Add(top);

            var valueRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 2 },
            };
            var typeField = new EnumField(def.type)
            {
                tooltip = "변수 타입",
                style = { width = 72 },
            };
            typeField.RegisterValueChangedCallback(evt => ChangeType(def, (FlowVariableType)evt.newValue));
            valueRow.Add(typeField);

            VisualElement valueField = CreateValueField(def);
            valueField.tooltip = "컨텍스트가 생성될 때 복사되는 기본값";
            valueField.style.flexGrow = 1;
            valueField.style.marginLeft = 3;
            valueRow.Add(valueField);
            card.Add(valueRow);

            var runtimeLabel = new Label
            {
                style =
                {
                    color = RuntimeColor,
                    fontSize = 10,
                    paddingLeft = 3,
                    paddingTop = 2,
                },
            };
            if (!string.IsNullOrEmpty(def.name) && !_runtimeLabels.ContainsKey(def.name))
                _runtimeLabels.Add(def.name, runtimeLabel);
            card.Add(runtimeLabel);

            if (focusName)
                nameField.schedule.Execute(() => { nameField.Focus(); nameField.SelectAll(); });

            return card;
        }

        private void CommitRename(FlowVariableDef def, string oldName, TextField field)
        {
            string newName = field.value?.Trim() ?? string.Empty;
            if (newName == oldName)
            {
                field.SetValueWithoutNotify(newName);
                return;
            }

            if (string.IsNullOrEmpty(newName) || IsDuplicateName(def, newName))
            {
                field.SetValueWithoutNotify(oldName);
                SetNameFieldValidity(field, true);
                field.tooltip = string.IsNullOrEmpty(newName)
                    ? "변수 이름은 비워둘 수 없습니다."
                    : $"'{newName}' 변수는 이미 존재합니다.";
                return;
            }

            RecordUndo("Blackboard 변수 이름 변경");
            RenameUsages(oldName, newName);
            def.name = newName;
            MarkChanged();
            Rebuild();
        }

        private void ChangeType(FlowVariableDef def, FlowVariableType newType)
        {
            if (def.type == newType)
                return;

            RecordUndo("Blackboard 변수 타입 변경");
            def.type = newType;
            foreach (VariableUsage usage in FindUsages(def.name))
            {
                if (usage.Value != null)
                    usage.Value.type = newType;
            }
            MarkChanged();
            Rebuild();
        }

        private VisualElement CreateValueField(FlowVariableDef def)
        {
            switch (def.type)
            {
                case FlowVariableType.Bool:
                {
                    var field = new Toggle { value = def.boolValue };
                    field.RegisterValueChangedCallback(evt => ChangeDefaultValue(() => def.boolValue = evt.newValue));
                    return field;
                }
                case FlowVariableType.Int:
                {
                    var field = new IntegerField { value = def.intValue };
                    field.RegisterValueChangedCallback(evt => ChangeDefaultValue(() => def.intValue = evt.newValue));
                    return field;
                }
                case FlowVariableType.Float:
                {
                    var field = new FloatField { value = def.floatValue };
                    field.RegisterValueChangedCallback(evt => ChangeDefaultValue(() => def.floatValue = evt.newValue));
                    return field;
                }
                default:
                {
                    var field = new TextField { value = def.stringValue };
                    field.RegisterValueChangedCallback(evt => ChangeDefaultValue(() => def.stringValue = evt.newValue));
                    return field;
                }
            }
        }

        private void ChangeDefaultValue(Action apply)
        {
            RecordUndo("Blackboard 기본값 변경");
            apply();
            MarkChanged();
        }

        private void ShowUsages(string variableName)
        {
            List<VariableUsage> usages = FindUsages(variableName);
            if (usages.Count == 1)
            {
                _focusNode?.Invoke(usages[0].Node.id);
                return;
            }

            var menu = new GenericMenu();
            foreach (VariableUsage usage in usages)
            {
                FlowNode node = usage.Node;
                menu.AddItem(new GUIContent(node.DisplayName), false, () => _focusNode?.Invoke(node.id));
            }
            menu.ShowAsContext();
        }

        private void ShowVariableMenu(FlowVariableDef def)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("복제"), false, () => DuplicateVariable(def));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("삭제"), false, () => DeleteVariable(def));
            menu.ShowAsContext();
        }

        private void DuplicateVariable(FlowVariableDef source)
        {
            string baseName = string.IsNullOrWhiteSpace(source.name) ? "variable" : $"{source.name}_copy";
            string name = baseName;
            int suffix = 2;
            while (_graph.HasVariable(name))
                name = $"{baseName}{suffix++}";

            RecordUndo("Blackboard 변수 복제");
            var copy = new FlowVariableDef
            {
                name = name,
                type = source.type,
                boolValue = source.boolValue,
                intValue = source.intValue,
                floatValue = source.floatValue,
                stringValue = source.stringValue,
            };
            int index = _graph.variables.IndexOf(source);
            _graph.variables.Insert(index + 1, copy);
            MarkChanged();
            _searchField.SetValueWithoutNotify(string.Empty);
            Rebuild(copy);
        }

        private void DeleteVariable(FlowVariableDef def)
        {
            int usageCount = FindUsages(def.name).Count;
            if (usageCount > 0 && !EditorUtility.DisplayDialog(
                    "사용 중인 변수 삭제",
                    $"'{def.name}' 변수를 {usageCount}개 노드가 사용 중입니다.\n삭제하면 해당 노드가 검증 경고 상태가 됩니다.",
                    "삭제", "취소"))
            {
                return;
            }

            RecordUndo("Blackboard 변수 삭제");
            _graph.variables.Remove(def);
            MarkChanged();
            Rebuild();
        }

        private List<VariableUsage> FindUsages(string variableName)
        {
            var results = new List<VariableUsage>();
            if (_graph == null || string.IsNullOrEmpty(variableName))
                return results;

            foreach (FlowNode node in _graph.nodes)
            {
                switch (node)
                {
                    case SetVariableNode set when set.variableName == variableName:
                        results.Add(new VariableUsage(node, set.value));
                        break;
                    case CheckVariableNode check when check.variableName == variableName:
                        results.Add(new VariableUsage(node, check.expected));
                        break;
                }

                FlowCondition condition = node switch
                {
                    BranchNode branch => branch.condition,
                    WaitConditionNode wait => wait.condition,
                    _ => null,
                };
                if (condition is VariableCondition variable && variable.variableName == variableName)
                    results.Add(new VariableUsage(node, variable.expected));
            }
            return results;
        }

        private void RenameUsages(string oldName, string newName)
        {
            foreach (VariableUsage usage in FindUsages(oldName))
            {
                switch (usage.Node)
                {
                    case SetVariableNode set when set.variableName == oldName:
                        set.variableName = newName;
                        break;
                    case CheckVariableNode check when check.variableName == oldName:
                        check.variableName = newName;
                        break;
                }

                FlowCondition condition = usage.Node switch
                {
                    BranchNode branch => branch.condition,
                    WaitConditionNode wait => wait.condition,
                    _ => null,
                };
                if (condition is VariableCondition variable && variable.variableName == oldName)
                    variable.variableName = newName;
            }
        }

        private bool IsDuplicateName(FlowVariableDef self, string name)
        {
            foreach (FlowVariableDef candidate in _graph.variables)
            {
                if (candidate != null && candidate != self && candidate.name == name)
                    return true;
            }
            return false;
        }

        private static bool MatchesSearch(FlowVariableDef def, string query)
        {
            return string.IsNullOrEmpty(query)
                || (def.name?.IndexOf(query, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || def.type.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void SetNameFieldValidity(TextField field, bool invalid)
        {
            Color color = invalid ? ErrorColor : Color.clear;
            float width = invalid ? 1f : 0f;
            field.style.borderBottomColor = color;
            field.style.borderTopColor = color;
            field.style.borderLeftColor = color;
            field.style.borderRightColor = color;
            field.style.borderBottomWidth = width;
            field.style.borderTopWidth = width;
            field.style.borderLeftWidth = width;
            field.style.borderRightWidth = width;
        }

        private static string FormatValue(object value)
        {
            return value switch
            {
                null => "null",
                bool boolean => boolean ? "True" : "False",
                float number => number.ToString("0.###"),
                string text => $"\"{text}\"",
                _ => value.ToString(),
            };
        }

        private void RecordUndo(string label)
        {
            if (_graph != null)
                Undo.RegisterCompleteObjectUndo(_graph, label);
        }

        private void MarkChanged()
        {
            if (_graph != null)
                EditorUtility.SetDirty(_graph);
            _onChanged?.Invoke();
        }
    }
}
