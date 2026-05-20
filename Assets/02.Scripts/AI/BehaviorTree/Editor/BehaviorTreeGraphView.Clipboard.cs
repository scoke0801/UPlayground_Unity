#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public partial class BehaviorTreeGraphView
    {
        private string SerializeSelectionToClipboardData(IEnumerable<GraphElement> elements)
        {
            var clipboard = new BehaviorTreeClipboardData();
            var seenNodeGuids = new HashSet<string>();
            var seenGroupGuids = new HashSet<string>();

            foreach (var element in elements)
            {
                if (element is BehaviorTreeNodeView nodeView && nodeView.Node != null)
                {
                    nodeView.Node.EnsureGuid();
                    if (seenNodeGuids.Add(nodeView.Node.Guid))
                        clipboard.nodeGuids.Add(nodeView.Node.Guid);
                }
                else if (element is BehaviorTreeGroupView groupView && groupView.Group != null)
                {
                    if (seenGroupGuids.Add(groupView.Group.Guid))
                    {
                        clipboard.groups.Add(new BehaviorTreeClipboardGroup
                        {
                            title = groupView.Group.Title,
                            rect = groupView.Group.Rect,
                            color = groupView.Group.Color
                        });
                    }
                }
            }

            return clipboard.nodeGuids.Count == 0 && clipboard.groups.Count == 0
                ? string.Empty
                : ClipboardPrefix + JsonUtility.ToJson(clipboard);
        }

        private bool CanPasteClipboardData(string data)
        {
            return _tree != null && !string.IsNullOrWhiteSpace(data) && data.StartsWith(ClipboardPrefix, StringComparison.Ordinal);
        }

        private void UnserializeAndPaste(string operationName, string data)
        {
            if (!CanPasteClipboardData(data))
                return;

            var json = data.Substring(ClipboardPrefix.Length);
            var clipboard = JsonUtility.FromJson<BehaviorTreeClipboardData>(json);
            if (clipboard == null)
                return;

            PasteClipboardData(clipboard);
        }

        private void CopySelectionToClipboard()
        {
            var serialized = SerializeSelectionToClipboardData(selection.OfType<GraphElement>());
            if (!string.IsNullOrWhiteSpace(serialized))
                EditorGUIUtility.systemCopyBuffer = serialized;
        }

        private void PasteFromClipboard()
        {
            UnserializeAndPaste("Paste BT Selection", EditorGUIUtility.systemCopyBuffer);
        }

        private void PasteClipboardData(BehaviorTreeClipboardData clipboard)
        {
            if (_tree == null)
                return;

            var sourceNodes = ResolveClipboardNodes(clipboard);
            if (sourceNodes.Count == 0 && clipboard.groups.Count == 0)
                return;

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Paste BT Selection");
            Undo.RecordObject(_tree, "Paste BT Selection");
            var nodeMap = new Dictionary<BTNode, BTNode>();
            var sourceBounds = CalculateClipboardBounds(sourceNodes, clipboard.groups);
            var pasteOffset = GetVisibleContentBounds().center - sourceBounds.center + new Vector2(32f, 32f);

            foreach (var source in sourceNodes)
            {
                var clone = UnityEngine.Object.Instantiate(source);
                clone.name = source.name;
                clone.Guid = Guid.NewGuid().ToString("N");
                clone.DisplayName = source.DisplayName;
                clone.EditorPosition = source.EditorPosition + pasteOffset;
                clone.Children.Clear();
                CloneCompositeServices(source, clone, pasteOffset);

                Undo.RegisterCreatedObjectUndo(clone, "Paste BT Node");
                AssetDatabase.AddObjectToAsset(clone, _tree);
                _tree.Nodes.Add(clone);
                nodeMap[source] = clone;
            }

            foreach (var pair in nodeMap)
            {
                var source = pair.Key;
                var clone = pair.Value;
                var sourceWeighted = source as WeightedRandomSelectorNode;
                var cloneWeighted = clone as WeightedRandomSelectorNode;

                // Instantiate가 _weights 리스트도 복제하므로, 원본 자식 일부만 paste되면
                // 새 자식 인덱스와 가중치 인덱스가 어긋난다. SetWeight로 source 인덱스 기준 재매핑.
                var newIndex = 0;
                for (var i = 0; i < source.Children.Count; i++)
                {
                    var child = source.Children[i];
                    if (child == null || !nodeMap.TryGetValue(child, out var childClone))
                        continue;

                    clone.Children.Add(childClone);
                    if (sourceWeighted != null && cloneWeighted != null)
                        cloneWeighted.SetWeight(newIndex, sourceWeighted.GetWeight(i));

                    newIndex++;
                }

                EditorUtility.SetDirty(clone);
            }

            foreach (var groupData in clipboard.groups)
            {
                var group = new BehaviorTreeEditorGroup
                {
                    Guid = Guid.NewGuid().ToString("N"),
                    Title = groupData.title,
                    Rect = new Rect(groupData.rect.position + pasteOffset, groupData.rect.size),
                    Color = groupData.color
                };
                _tree.EditorGroups.Add(group);
            }

            SaveAsset();
            Undo.CollapseUndoOperations(undoGroup);
            PopulateView(_tree);

            ClearSelection();
            foreach (var clone in nodeMap.Values)
            {
                if (_nodeViews.TryGetValue(clone, out var nodeView))
                    AddToSelection(nodeView);
            }
            foreach (var group in _tree.EditorGroups.Where(group => clipboard.groups.Any(copied => group.Title == copied.title && group.Rect.position == copied.rect.position + pasteOffset)))
            {
                if (_groupViews.TryGetValue(group, out var groupView))
                    AddToSelection(groupView);
            }
        }

        private List<BTNode> ResolveClipboardNodes(BehaviorTreeClipboardData clipboard)
        {
            var result = new List<BTNode>();
            if (clipboard?.nodeGuids == null)
                return result;

            foreach (var guid in clipboard.nodeGuids)
            {
                var node = _tree.Nodes.FirstOrDefault(candidate => candidate != null && candidate.Guid == guid);
                if (node != null && node is not BTServiceNode)
                    result.Add(node);
            }

            return result;
        }

        private static Rect CalculateClipboardBounds(List<BTNode> nodes, List<BehaviorTreeClipboardGroup> groups)
        {
            var first = true;
            var bounds = new Rect();
            foreach (var rect in nodes.Select(node => new Rect(node.EditorPosition, new Vector2(160f, 120f)))
                         .Concat((groups ?? new List<BehaviorTreeClipboardGroup>()).Select(group => group.rect)))
            {
                if (first)
                {
                    bounds = rect;
                    first = false;
                }
                else
                {
                    bounds.xMin = Mathf.Min(bounds.xMin, rect.xMin);
                    bounds.yMin = Mathf.Min(bounds.yMin, rect.yMin);
                    bounds.xMax = Mathf.Max(bounds.xMax, rect.xMax);
                    bounds.yMax = Mathf.Max(bounds.yMax, rect.yMax);
                }
            }

            return first ? new Rect(Vector2.zero, Vector2.one) : bounds;
        }

        private void CloneCompositeServices(BTNode source, BTNode clone, Vector2 pasteOffset)
        {
            if (source is not BTCompositeNode sourceComposite || clone is not BTCompositeNode cloneComposite)
                return;

            cloneComposite.Services.Clear();
            foreach (var service in sourceComposite.Services)
            {
                if (service == null)
                    continue;

                var serviceClone = UnityEngine.Object.Instantiate(service);
                serviceClone.name = service.name;
                serviceClone.Guid = Guid.NewGuid().ToString("N");
                serviceClone.DisplayName = service.DisplayName;
                serviceClone.EditorPosition = service.EditorPosition + pasteOffset;
                serviceClone.Children.Clear();

                Undo.RegisterCreatedObjectUndo(serviceClone, "Paste BT Service");
                AssetDatabase.AddObjectToAsset(serviceClone, _tree);
                _tree.Nodes.Add(serviceClone);
                cloneComposite.Services.Add(serviceClone);
                EditorUtility.SetDirty(serviceClone);
            }
        }

        [Serializable]
        private sealed class BehaviorTreeClipboardData
        {
            public List<string> nodeGuids = new();
            public List<BehaviorTreeClipboardGroup> groups = new();
        }

        [Serializable]
        private sealed class BehaviorTreeClipboardGroup
        {
            public string title;
            public Rect rect;
            public Color color;
        }
    }
}
#endif
