using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UPlayGround.BehaviorTree;

namespace UPlayGround.Editor.BehaviorTree
{
    public class BehaviorTreeGraphView : GraphView
    {
        public Action<BTNodeView, bool> NodeSelectionChanged;

        private readonly Dictionary<BTNodeSO, BTNodeView> _nodeViews = new();
        private BehaviorTreeSO _currentTree;
        private bool _isPopulating;

        private const float NODE_WIDTH  = 170f;
        private const float NODE_HEIGHT = 90f;
        private const float H_SPACING   = 30f;
        private const float V_SPACING   = 60f;

        public BehaviorTreeGraphView()
        {
            SetupZoom(0.2f, 2.5f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            style.flexGrow = 1;

            graphViewChanged += OnGraphViewChanged;
        }

        // ── 트리 표시 ─────────────────────────────────
        public void PopulateView(BehaviorTreeSO tree)
        {
            _isPopulating = true;
            _currentTree  = tree;
            ClearAll();
            _isPopulating = false;

            if (tree?.rootNode == null) return;

            bool hasPositions = tree.rootNode.editorPosition != Vector2.zero;
            BuildNodeViews(tree.rootNode);
            ConnectEdges(tree.rootNode);

            if (hasPositions)
                ApplySavedPositions();
            else
            {
                AutoLayout(tree.rootNode, new Vector2(400f, 50f));
                PersistPositions();
            }
            FrameAll();
        }

        private void ApplySavedPositions()
        {
            foreach (var kv in _nodeViews)
                kv.Value.SetPosition(new Rect(kv.Key.editorPosition, new Vector2(NODE_WIDTH, NODE_HEIGHT)));
        }

        private void PersistPositions()
        {
            foreach (var kv in _nodeViews)
            {
                var pos = kv.Value.GetPosition().position;
                if (kv.Key.editorPosition != pos)
                {
                    kv.Key.editorPosition = pos;
                    EditorUtility.SetDirty(kv.Key);
                }
            }
        }

        private void BuildNodeViews(BTNodeSO so)
        {
            if (so == null || _nodeViews.ContainsKey(so)) return;
            CreateNodeView(so);
            foreach (var child in GetSOChildren(so))
                BuildNodeViews(child);
        }

        private BTNodeView CreateNodeView(BTNodeSO so)
        {
            var view = new BTNodeView(so);
            view.OnSelectionChanged += (v, selected) => NodeSelectionChanged?.Invoke(v, selected);
            _nodeViews[so] = view;
            AddElement(view);
            return view;
        }

        private void ConnectEdges(BTNodeSO so)
        {
            if (so == null) return;
            foreach (var child in GetSOChildren(so))
            {
                if (child == null) continue;
                ConnectEdges(child);

                if (!_nodeViews.TryGetValue(so,    out var parentView)) continue;
                if (!_nodeViews.TryGetValue(child,  out var childView))  continue;

                var edge = parentView.OutputPort.ConnectTo(childView.InputPort);
                AddElement(edge);
            }
        }

        // ── 자동 레이아웃 ─────────────────────────────
        private float AutoLayout(BTNodeSO so, Vector2 origin)
        {
            if (so == null || !_nodeViews.TryGetValue(so, out var view)) return 0f;

            var children = GetSOChildren(so);
            if (children.Count == 0)
            {
                view.SetPosition(new Rect(origin, new Vector2(NODE_WIDTH, NODE_HEIGHT)));
                return NODE_WIDTH;
            }

            float totalWidth = 0f;
            float childY     = origin.y + NODE_HEIGHT + V_SPACING;
            var   centerXs   = new List<float>();

            foreach (var child in children)
            {
                float w = AutoLayout(child, new Vector2(origin.x + totalWidth, childY));
                centerXs.Add(origin.x + totalWidth + w * 0.5f - NODE_WIDTH * 0.5f);
                totalWidth += w + H_SPACING;
            }
            totalWidth -= H_SPACING;

            float midX = (centerXs[0] + centerXs[centerXs.Count - 1]) * 0.5f;
            view.SetPosition(new Rect(new Vector2(midX, origin.y), new Vector2(NODE_WIDTH, NODE_HEIGHT)));
            return Mathf.Max(totalWidth, NODE_WIDTH);
        }

        // ── 컨텍스트 메뉴 (우클릭 노드 생성) ─────────
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (_currentTree == null) return;

            var mousePos = contentViewContainer.WorldToLocal(evt.mousePosition);

            foreach (var type in GetNodeTypes("BTAction_"))
            {
                var t = type; var mp = mousePos;
                evt.menu.AppendAction($"Create/Actions/{GetShortName(t)}", _ => CreateNode(t, mp));
            }
            foreach (var type in GetNodeTypes("BTCond_"))
            {
                var t = type; var mp = mousePos;
                evt.menu.AppendAction($"Create/Conditions/{GetShortName(t)}", _ => CreateNode(t, mp));
            }

            evt.menu.AppendAction("Create/Composite/Selector",       _ => CreateNode(typeof(BTSelectorSO),       mousePos));
            evt.menu.AppendAction("Create/Composite/Sequence",       _ => CreateNode(typeof(BTSequenceSO),       mousePos));
            evt.menu.AppendAction("Create/Composite/RandomSelector", _ => CreateNode(typeof(BTRandomSelectorSO), mousePos));
            evt.menu.AppendAction("Create/Decorator/Inverter",       _ => CreateNode(typeof(BTInverterSO),       mousePos));
            evt.menu.AppendAction("Create/Decorator/Cooldown",       _ => CreateNode(typeof(BTCooldownSO),       mousePos));

            if (selection.Count == 1 && selection[0] is BTNodeView sv)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Set as Root Node", _ => SetAsRoot(sv.NodeSO));
            }
        }

        private static IEnumerable<Type> GetNodeTypes(string prefix)
            => TypeCache.GetTypesDerivedFrom<BTNodeSO>()
                .Where(t => !t.IsAbstract && t.Name.Contains(prefix))
                .OrderBy(t => t.Name);

        private static string GetShortName(Type t)
            => t.Name.Replace("SO", "").Replace("BTAction_", "").Replace("BTCond_", "");

        private void CreateNode(Type type, Vector2 localPos)
        {
            if (_currentTree == null) return;

            var so = ScriptableObject.CreateInstance(type) as BTNodeSO;
            so.nodeName       = GetShortName(type);
            so.editorPosition = localPos;
            so.name           = so.nodeName;

            Undo.RecordObject(_currentTree, "Create BT Node");
            AssetDatabase.AddObjectToAsset(so, _currentTree);
            AssetDatabase.SaveAssets();

            if (_currentTree.rootNode == null)
            {
                _currentTree.rootNode = so;
                EditorUtility.SetDirty(_currentTree);
            }

            var view = CreateNodeView(so);
            view.SetPosition(new Rect(localPos, new Vector2(NODE_WIDTH, NODE_HEIGHT)));
        }

        private void SetAsRoot(BTNodeSO so)
        {
            if (_currentTree == null) return;
            Undo.RecordObject(_currentTree, "Set Root Node");
            _currentTree.rootNode = so;
            EditorUtility.SetDirty(_currentTree);
        }

        // ── GraphViewChanged — 연결/이동/삭제 ─────────
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isPopulating) return change;

            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    if (edge.output.node is BTNodeView parent && edge.input.node is BTNodeView child)
                        AddChildToSO(parent.NodeSO, child.NodeSO);
                }
            }

            if (change.elementsToRemove != null)
            {
                foreach (var elem in change.elementsToRemove)
                {
                    if (elem is Edge edge)
                    {
                        if (edge.output.node is BTNodeView parent && edge.input.node is BTNodeView child)
                            RemoveChildFromSO(parent.NodeSO, child.NodeSO);
                    }
                    else if (elem is BTNodeView nodeView)
                    {
                        DeleteNodeSO(nodeView.NodeSO);
                        _nodeViews.Remove(nodeView.NodeSO);
                    }
                }
            }

            if (change.movedElements != null)
            {
                foreach (var elem in change.movedElements)
                {
                    if (elem is BTNodeView nodeView)
                    {
                        nodeView.NodeSO.editorPosition = nodeView.GetPosition().position;
                        EditorUtility.SetDirty(nodeView.NodeSO);
                    }
                }
            }

            return change;
        }

        private void AddChildToSO(BTNodeSO parent, BTNodeSO child)
        {
            Undo.RecordObject(parent, "Add BT Child");
            switch (parent)
            {
                case BTSelectorSO s:
                    if (!s.children.Contains(child)) s.children.Add(child);
                    break;
                case BTSequenceSO s:
                    if (!s.children.Contains(child)) s.children.Add(child);
                    break;
                case BTRandomSelectorSO s:
                    if (!s.children.Contains(child)) { s.children.Add(child); s.weights.Add(1f); }
                    break;
                case BTInverterSO i:
                    i.child = child;
                    break;
                case BTCooldownSO c:
                    c.child = child;
                    break;
            }
            EditorUtility.SetDirty(parent);
        }

        private void RemoveChildFromSO(BTNodeSO parent, BTNodeSO child)
        {
            Undo.RecordObject(parent, "Remove BT Child");
            switch (parent)
            {
                case BTSelectorSO s:
                    s.children.Remove(child);
                    break;
                case BTSequenceSO s:
                    s.children.Remove(child);
                    break;
                case BTRandomSelectorSO s:
                    int idx = s.children.IndexOf(child);
                    if (idx >= 0) { s.children.RemoveAt(idx); if (idx < s.weights.Count) s.weights.RemoveAt(idx); }
                    break;
                case BTInverterSO i:
                    if (i.child == child) i.child = null;
                    break;
                case BTCooldownSO c:
                    if (c.child == child) c.child = null;
                    break;
            }
            EditorUtility.SetDirty(parent);
        }

        private void DeleteNodeSO(BTNodeSO so)
        {
            if (so == null || _currentTree == null) return;

            if (_currentTree.rootNode == so)
            {
                Undo.RecordObject(_currentTree, "Delete Root Node");
                _currentTree.rootNode = null;
                EditorUtility.SetDirty(_currentTree);
            }

            foreach (var kv in _nodeViews)
                if (kv.Key != so) RemoveChildFromSO(kv.Key, so);

            AssetDatabase.RemoveObjectFromAsset(so);
            UnityEngine.Object.DestroyImmediate(so, true);
            AssetDatabase.SaveAssets();
        }

        // ── 런타임 바인딩 ─────────────────────────────
        public void BindRuntimeTree(BTNode runtimeRoot)
        {
            foreach (var kv in _nodeViews) kv.Value.UnbindRuntimeNode();
            if (runtimeRoot == null) return;
            BindNodeRecursive(runtimeRoot);
        }

        private void BindNodeRecursive(BTNode node)
        {
            if (node?.SourceSO == null) return;
            if (_nodeViews.TryGetValue(node.SourceSO, out var view))
                view.BindRuntimeNode(node);
            foreach (var child in GetRuntimeChildren(node))
                BindNodeRecursive(child);
        }

        public void RefreshRuntimeStatus()
        {
            foreach (var kv in _nodeViews)
                kv.Value.RefreshRuntimeStatus();
        }

        // ── GraphView 오버라이드 ───────────────────────
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatible = new List<Port>();
            ports.ForEach(p =>
            {
                if (startPort != p && startPort.node != p.node && startPort.direction != p.direction)
                    compatible.Add(p);
            });
            return compatible;
        }

        // ── 내부 유틸 ─────────────────────────────────
        private void ClearAll()
        {
            _nodeViews.Clear();
            DeleteElements(graphElements.ToList());
        }

        private static List<BTNodeSO> GetSOChildren(BTNodeSO so)
        {
            var list = new List<BTNodeSO>();
            switch (so)
            {
                case BTSelectorSO       sel: list.AddRange(sel.children);                    break;
                case BTSequenceSO       seq: list.AddRange(seq.children);                    break;
                case BTRandomSelectorSO rnd: list.AddRange(rnd.children);                    break;
                case BTInverterSO       inv: if (inv.child != null) list.Add(inv.child);     break;
                case BTCooldownSO       cd:  if (cd.child  != null) list.Add(cd.child);      break;
            }
            return list;
        }

        private static List<BTNode> GetRuntimeChildren(BTNode node)
        {
            var list = new List<BTNode>();
            const System.Reflection.BindingFlags NP =
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            switch (node)
            {
                case BTSelector or BTSequence or BTRandomSelector:
                    if (node.GetType().GetField("_children", NP)?.GetValue(node) is List<BTNode> ch)
                        list.AddRange(ch);
                    break;
                case BTInverter or BTCooldown:
                    if (node.GetType().GetField("_child", NP)?.GetValue(node) is BTNode c)
                        list.Add(c);
                    break;
            }
            return list;
        }
    }
}
