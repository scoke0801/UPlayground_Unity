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

        // ── Decorator 추적 ─────────────────────────────
        private readonly Dictionary<BTNodeSO, BTNodeSO>                              _parentMap      = new();
        private readonly Dictionary<BTNodeSO, List<BTNodeSO>>                        _decoratorMap   = new();
        private readonly Dictionary<Edge, (BTNodeSO topDecorator, BTNodeSO visible)> _edgeDecorators = new();

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

            if (tree?.rootNode == null)
            {
                _isPopulating = false;
                return;
            }

            BuildParentMap(tree.rootNode, null);

            bool hasPositions = tree.rootNode.editorPosition != Vector2.zero;
            BuildNodeViews(tree.rootNode);
            ConnectEdges(tree.rootNode);

            // ConnectEdges 완료 후 해제 — 그 사이 graphViewChanged가 AddChildToSO를 호출하지 않도록 보호
            _isPopulating = false;

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
            if (so == null) return;

            // 흡수 가능한 데코레이터는 별도 노드뷰 없이 자식으로 포워드
            if (IsAbsorbableDecorator(so))
            {
                BuildNodeViews(GetDecoratorChild(so));
                return;
            }

            if (_nodeViews.ContainsKey(so)) return;
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

            // 흡수된 데코레이터: 자식으로만 포워드
            if (IsAbsorbableDecorator(so))
            {
                ConnectEdges(GetDecoratorChild(so));
                return;
            }

            bool showIndex = so is BTSelectorSO or BTSequenceSO or BTRandomSelectorSO;
            var  soChildren = GetSOChildren(so);

            for (int idx = 0; idx < soChildren.Count; idx++)
            {
                var rawChild = soChildren[idx];
                if (rawChild == null) continue;

                // 데코레이터 체인을 해소해서 실제 노드(target)와 뱃지 목록(decorators)을 분리
                var decorators = new List<BTNodeSO>();
                var target = rawChild;
                while (IsAbsorbableDecorator(target))
                {
                    decorators.Add(target);
                    target = GetDecoratorChild(target);
                }

                if (target == null) continue;

                ConnectEdges(target);

                if (!_nodeViews.TryGetValue(so,     out var parentView)) continue;
                if (!_nodeViews.TryGetValue(target, out var targetView)) continue;

                // 뱃지 추가 (데코레이터가 있을 때)
                if (decorators.Count > 0)
                {
                    foreach (var dec in decorators)
                        targetView.AddDecoratorBadge(dec);

                    if (!_decoratorMap.ContainsKey(target))
                        _decoratorMap[target] = new List<BTNodeSO>();
                    _decoratorMap[target].AddRange(decorators);
                }

                // 실행 순서 인덱스 배지
                if (showIndex)
                    targetView.SetChildIndex(idx);

                // 에지: 부모 → target (데코레이터 바이패스)
                var edge = parentView.OutputPort.ConnectTo(targetView.InputPort);
                if (decorators.Count > 0)
                    _edgeDecorators[edge] = (rawChild, target);
                AddElement(edge);
            }
        }

        // ── 자동 레이아웃 ─────────────────────────────
        private float AutoLayout(BTNodeSO so, Vector2 origin)
        {
            if (so == null) return 0f;

            // 흡수된 데코레이터 → 자식 노드가 실제 레이아웃 대상
            if (IsAbsorbableDecorator(so))
                return AutoLayout(GetDecoratorChild(so), origin);

            if (!_nodeViews.TryGetValue(so, out var view)) return 0f;

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

            // 단일 노드 선택 시 노드별 액션
            if (selection.Count == 1 && selection[0] is BTNodeView sv)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Set as Root Node", _ => SetAsRoot(sv.NodeSO));

                // 부모가 있으면 데코레이터 추가 가능
                if (_parentMap.TryGetValue(sv.NodeSO, out var parentSO) && parentSO != null)
                {
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Add Decorator/! Inverter",      _ => AddDecorator(typeof(BTInverterSO),     sv));
                    evt.menu.AppendAction("Add Decorator/⏱ Cooldown",      _ => AddDecorator(typeof(BTCooldownSO),     sv));
                    evt.menu.AppendAction("Add Decorator/✓ ForceSuccess",  _ => AddDecorator(typeof(BTForceSuccessSO), sv));
                    evt.menu.AppendAction("Add Decorator/↺ Loop",           _ => AddDecorator(typeof(BTLoopSO),         sv));
                    evt.menu.AppendAction("Add Decorator/◉ Guard",         _ => AddDecorator(typeof(BTGuardSO),        sv));
                }

                // 데코레이터가 있으면 제거 가능
                if (_decoratorMap.TryGetValue(sv.NodeSO, out var decs) && decs.Count > 0)
                {
                    for (int i = 0; i < decs.Count; i++)
                    {
                        var dec   = decs[i];
                        string nm = dec switch
                        {
                            BTCooldownSO     cd => $"⏱ Cooldown ({cd.cooldown:F1}s)",
                            BTLoopSO         lp => $"↺ Loop ×{(lp.loopCount < 0 ? "∞" : lp.loopCount.ToString())}",
                            BTForceSuccessSO    => "✓ ForceSuccess",
                            BTGuardSO        g  => $"◉ Guard{(string.IsNullOrEmpty(g.observeKey) ? "" : $" ({g.observeKey})")}",
                            _                   => "! Inverter"
                        };
                        evt.menu.AppendAction($"Remove Decorator/{i + 1}. {nm}", _ => RemoveDecorator(dec, sv));
                    }
                }
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
                bool needsRepopulate = false;
                foreach (var elem in change.elementsToRemove)
                {
                    if (elem is Edge edge)
                    {
                        if (edge.output.node is BTNodeView parent && edge.input.node is BTNodeView child)
                        {
                            if (_edgeDecorators.TryGetValue(edge, out var decInfo))
                            {
                                // 흡수된 에지 제거: 데코레이터 체인 삭제 후 부모 → 자식 직결
                                Undo.RecordObject(parent.NodeSO, "Remove Decorator");
                                ReplaceChild(parent.NodeSO, decInfo.topDecorator, child.NodeSO);
                                DeleteDecoratorChain(decInfo.topDecorator, child.NodeSO);
                                EditorUtility.SetDirty(parent.NodeSO);
                                AssetDatabase.SaveAssets();
                                _edgeDecorators.Remove(edge);
                                needsRepopulate = true;
                            }
                            else
                            {
                                RemoveChildFromSO(parent.NodeSO, child.NodeSO);
                            }
                        }
                    }
                    else if (elem is BTNodeView nodeView)
                    {
                        DeleteNodeSO(nodeView.NodeSO);
                        _nodeViews.Remove(nodeView.NodeSO);
                    }
                }

                if (needsRepopulate)
                    EditorApplication.delayCall += () => { if (_currentTree != null) PopulateView(_currentTree); };
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
                case BTInverterSO     i:  i.child  = child; break;
                case BTCooldownSO     c:  c.child  = child; break;
                case BTForceSuccessSO fs: fs.child = child; break;
                case BTLoopSO         lp: lp.child = child; break;
                case BTGuardSO g:
                    if (g.condition == null) g.condition = child;
                    else                     g.child     = child;
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
                case BTInverterSO     i:  if (i.child  == child) i.child  = null; break;
                case BTCooldownSO     c:  if (c.child  == child) c.child  = null; break;
                case BTForceSuccessSO fs: if (fs.child    == child) fs.child    = null; break;
                case BTLoopSO         lp: if (lp.child    == child) lp.child    = null; break;
                case BTGuardSO        g:
                    if (g.condition == child) g.condition = null;
                    else if (g.child == child) g.child    = null;
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
            _parentMap.Clear();
            _decoratorMap.Clear();
            _edgeDecorators.Clear();
            DeleteElements(graphElements.ToList());
        }

        // ── 데코레이터 헬퍼 ──────────────────────────
        private static bool IsAbsorbableDecorator(BTNodeSO so) => so switch
        {
            BTInverterSO     inv => inv.child != null,
            BTCooldownSO     cd  => cd.child  != null,
            BTForceSuccessSO fs  => fs.child  != null,
            BTLoopSO         lp  => lp.child  != null,
            // BTGuardSO: condition이 없을 때(AlwaysTrue 용도)만 흡수. condition이 있으면 독립 노드로 표시
            BTGuardSO        g   => g.child != null && g.condition == null,
            _                    => false
        };

        private static BTNodeSO GetDecoratorChild(BTNodeSO so) => so switch
        {
            BTInverterSO     inv => inv.child,
            BTCooldownSO     cd  => cd.child,
            BTForceSuccessSO fs  => fs.child,
            BTLoopSO         lp  => lp.child,
            BTGuardSO        g   => g.child,
            _                    => null
        };

        private void BuildParentMap(BTNodeSO so, BTNodeSO parent)
        {
            if (so == null || _parentMap.ContainsKey(so)) return;
            _parentMap[so] = parent;
            foreach (var child in GetSOChildren(so))
                BuildParentMap(child, so);
        }

        private void AddDecorator(Type decoratorType, BTNodeView targetView)
        {
            var so = targetView.NodeSO;
            if (!_parentMap.TryGetValue(so, out var parentSO) || parentSO == null) return;

            var dec = ScriptableObject.CreateInstance(decoratorType) as BTNodeSO;
            dec.name = dec.nodeName = decoratorType switch
            {
                _ when decoratorType == typeof(BTInverterSO)     => "Inverter",
                _ when decoratorType == typeof(BTCooldownSO)     => "Cooldown",
                _ when decoratorType == typeof(BTForceSuccessSO) => "ForceSuccess",
                _ when decoratorType == typeof(BTLoopSO)         => "Loop",
                _                                                => decoratorType.Name.Replace("SO", "").Replace("BT", "")
            };

            Undo.RecordObjects(new UnityEngine.Object[] { parentSO, dec }, "Add Decorator");
            AssetDatabase.AddObjectToAsset(dec, _currentTree);

            if      (dec is BTInverterSO     inv) inv.child = so;
            else if (dec is BTCooldownSO     cd)  cd.child  = so;
            else if (dec is BTForceSuccessSO fs)  fs.child  = so;
            else if (dec is BTLoopSO         lp)  lp.child  = so;
            else if (dec is BTGuardSO        g)   g.child   = so;

            ReplaceChild(parentSO, so, dec);

            EditorUtility.SetDirty(parentSO);
            EditorUtility.SetDirty(dec);
            AssetDatabase.SaveAssets();

            PopulateView(_currentTree);
        }

        private void RemoveDecorator(BTNodeSO decorator, BTNodeView targetView)
        {
            if (!_parentMap.TryGetValue(decorator, out var grandParent) || grandParent == null) return;

            var so = targetView.NodeSO;
            Undo.RecordObjects(new UnityEngine.Object[] { grandParent, decorator }, "Remove Decorator");

            ReplaceChild(grandParent, decorator, so);

            AssetDatabase.RemoveObjectFromAsset(decorator);
            UnityEngine.Object.DestroyImmediate(decorator, true);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(grandParent);

            PopulateView(_currentTree);
        }

        private static void ReplaceChild(BTNodeSO parent, BTNodeSO oldChild, BTNodeSO newChild)
        {
            switch (parent)
            {
                case BTSelectorSO s:
                    for (int i = 0; i < s.children.Count; i++)
                        if (s.children[i] == oldChild) { s.children[i] = newChild; break; }
                    break;
                case BTSequenceSO s:
                    for (int i = 0; i < s.children.Count; i++)
                        if (s.children[i] == oldChild) { s.children[i] = newChild; break; }
                    break;
                case BTRandomSelectorSO s:
                    for (int i = 0; i < s.children.Count; i++)
                        if (s.children[i] == oldChild) { s.children[i] = newChild; break; }
                    break;
                case BTInverterSO     i:  if (i.child  == oldChild) i.child  = newChild; break;
                case BTCooldownSO     c:  if (c.child  == oldChild) c.child  = newChild; break;
                case BTForceSuccessSO fs: if (fs.child    == oldChild) fs.child    = newChild; break;
                case BTLoopSO         lp: if (lp.child    == oldChild) lp.child    = newChild; break;
                case BTGuardSO g:
                    if (g.condition == oldChild) g.condition = newChild;
                    else if (g.child == oldChild) g.child    = newChild;
                    break;
            }
        }

        private void DeleteDecoratorChain(BTNodeSO topDecorator, BTNodeSO stopAt)
        {
            var current = topDecorator;
            while (current != null && current != stopAt)
            {
                var next = GetDecoratorChild(current);
                AssetDatabase.RemoveObjectFromAsset(current);
                UnityEngine.Object.DestroyImmediate(current, true);
                current = next;
            }
        }

        private static List<BTNodeSO> GetSOChildren(BTNodeSO so)
        {
            var list = new List<BTNodeSO>();
            switch (so)
            {
                case BTSelectorSO       sel: list.AddRange(sel.children);                    break;
                case BTSequenceSO       seq: list.AddRange(seq.children);                    break;
                case BTRandomSelectorSO rnd: list.AddRange(rnd.children);                    break;
                case BTInverterSO       inv: if (inv.child  != null) list.Add(inv.child);  break;
                case BTCooldownSO       cd:  if (cd.child   != null) list.Add(cd.child);   break;
                case BTForceSuccessSO   fs:  if (fs.child   != null) list.Add(fs.child);   break;
                case BTLoopSO           lp:  if (lp.child   != null) list.Add(lp.child);   break;
                case BTGuardSO g:
                    // 흡수된 BTGuardSO(condition == null)는 child만 반환. 독립 노드는 양쪽 모두 반환
                    if (IsAbsorbableDecorator(g))
                    {
                        if (g.child != null) list.Add(g.child);
                    }
                    else
                    {
                        if (g.condition != null) list.Add(g.condition);
                        if (g.child     != null) list.Add(g.child);
                    }
                    break;
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

                // 단일 _child 노드 — 흡수 여부와 무관하게 런타임 탐색에 포함
                case BTInverter or BTCooldown or BTForceSuccess or BTLoop:
                    if (node.GetType().GetField("_child", NP)?.GetValue(node) is BTNode c)
                        list.Add(c);
                    break;

                // BTGuard: _condNode + _child 두 자식
                case BTGuard:
                    if (node.GetType().GetField("_condNode", NP)?.GetValue(node) is BTNode cond)
                        list.Add(cond);
                    if (node.GetType().GetField("_child", NP)?.GetValue(node) is BTNode guardChild)
                        list.Add(guardChild);
                    break;
            }
            return list;
        }
    }
}
