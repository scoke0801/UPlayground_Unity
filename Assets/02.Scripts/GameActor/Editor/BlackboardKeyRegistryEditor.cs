#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    [CustomEditor(typeof(BlackboardKeyRegistrySO))]
    internal sealed class BlackboardKeyRegistryEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, int> _usageCounts =
            new(StringComparer.Ordinal);
        private string _query = string.Empty;
        private VisualElement _listRoot;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.style.paddingLeft = 6;
            root.style.paddingRight = 6;

            var title = new Label("Blackboard Key Registry");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 15;
            title.style.marginBottom = 4;
            root.Add(title);

            var description = new HelpBox(
                "이름은 표시용 캐시이며 stable ID가 직렬화 정본입니다. "
                + "Key 추가·타입 변경은 Source JSON에서 수행한 뒤 생성/마이그레이션 도구를 실행하세요.",
                HelpBoxMessageType.Info);
            root.Add(description);

            var toolbar = new Toolbar();
            var search = new ToolbarSearchField();
            search.style.flexGrow = 1;
            search.RegisterValueChangedCallback(evt =>
            {
                _query = evt.newValue ?? string.Empty;
                RebuildList();
            });
            toolbar.Add(search);

            var validate = new ToolbarButton(
                BlackboardKeyRegistryGenerator.ValidateMenu)
            {
                text = "전체 검사"
            };
            toolbar.Add(validate);

            var refreshUsage = new ToolbarButton(() =>
            {
                CollectUsageCounts();
                RebuildList();
            })
            {
                text = "사용량"
            };
            toolbar.Add(refreshUsage);
            root.Add(toolbar);

            _listRoot = new ScrollView();
            _listRoot.style.maxHeight = 640;
            _listRoot.style.marginTop = 5;
            root.Add(_listRoot);

            CollectUsageCounts();
            RebuildList();
            return root;
        }

        private void RebuildList()
        {
            if (_listRoot == null)
                return;

            _listRoot.Clear();
            var registry = (BlackboardKeyRegistrySO)target;
            foreach (BlackboardKeyDefinition definition in registry.Definitions)
            {
                if (definition == null || !Matches(definition, _query))
                    continue;

                var row = new VisualElement();
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.25f, 0.25f, 0.25f);
                row.style.paddingTop = 4;
                row.style.paddingBottom = 4;

                var header = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row
                    }
                };
                var name = new Label(definition.DisplayName)
                {
                    tooltip = definition.Description
                };
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.flexGrow = 1;
                header.Add(name);

                _usageCounts.TryGetValue(definition.StableId, out int count);
                header.Add(new Label(
                    $"{definition.ValueType} · {definition.Scope} · 사용 {count}"));
                row.Add(header);

                var raw = new Label(
                    $"{definition.KeyName}  [{definition.StableId}]");
                raw.style.fontSize = 10;
                raw.style.color = new Color(0.65f, 0.65f, 0.65f);
                row.Add(raw);
                _listRoot.Add(row);
            }
        }

        private static bool Matches(
            BlackboardKeyDefinition definition,
            string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            return definition.KeyName.Contains(
                       query,
                       StringComparison.OrdinalIgnoreCase)
                   || definition.DisplayName.Contains(
                       query,
                       StringComparison.OrdinalIgnoreCase)
                   || definition.StableId.Contains(
                       query,
                       StringComparison.OrdinalIgnoreCase);
        }

        private void CollectUsageCounts()
        {
            _usageCounts.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:BehaviorTreeAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BehaviorTreeAsset tree =
                    AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(path);
                if (tree == null)
                    continue;

                foreach (BlackboardEntry entry in tree.Blackboard.Entries)
                    Increment(entry?.StableId);

                foreach (BTNode node in tree.Nodes)
                {
                    if (node == null)
                        continue;
                    var serializedNode = new SerializedObject(node);
                    SerializedProperty iterator = serializedNode.GetIterator();
                    while (iterator.NextVisible(true))
                    {
                        if (iterator.name == "_stableId"
                            && iterator.propertyType
                            == SerializedPropertyType.String)
                            Increment(iterator.stringValue);
                    }
                }
            }
        }

        private void Increment(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId))
                return;
            _usageCounts.TryGetValue(stableId, out int count);
            _usageCounts[stableId] = count + 1;
        }
    }
}
#endif
