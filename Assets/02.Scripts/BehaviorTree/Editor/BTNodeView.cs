using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UPlayGround.BehaviorTree;

namespace UPlayGround.Editor.BehaviorTree
{
    public class BTNodeView : Node
    {
        public BTNodeSO NodeSO     { get; private set; }
        public Port     InputPort  { get; private set; }
        public Port     OutputPort { get; private set; }

        public Action<BTNodeView, bool> OnSelectionChanged;

        private BTNode _runtimeNode;

        public BTNodeView(BTNodeSO nodeSO) : base()
        {
            NodeSO      = nodeSO;
            viewDataKey = nodeSO.GetInstanceID().ToString();

            AddToClassList("bt-ue-node");
            AddToClassList(GetTypeClass(nodeSO));

            CreatePorts();
            BuildHeader();
            BuildBody();
            RearrangeLayout();
        }

        // ── 포트 생성 ─────────────────────────────────
        private void CreatePorts()
        {
            bool isMultiOutput = NodeSO is BTSelectorSO or BTSequenceSO or BTRandomSelectorSO;
            bool hasOutput     = isMultiOutput || NodeSO is BTInverterSO or BTCooldownSO;

            InputPort = Port.Create<Edge>(
                Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            InputPort.portName = "";
            inputContainer.Add(InputPort);

            if (hasOutput)
            {
                OutputPort = Port.Create<Edge>(
                    Orientation.Vertical, Direction.Output,
                    isMultiOutput ? Port.Capacity.Multi : Port.Capacity.Single,
                    typeof(bool));
                OutputPort.portName = "";
                outputContainer.Add(OutputPort);
            }
        }

        // ── 헤더 (색상 바) ─────────────────────────────
        private void BuildHeader()
        {
            titleContainer.Clear();

            var row = new VisualElement();
            row.AddToClassList("bt-ue-header-row");

            var icon = new Label(GetTypeIcon(NodeSO));
            icon.AddToClassList("bt-ue-icon");
            row.Add(icon);

            var typeLbl = new Label(GetTypeName(NodeSO));
            typeLbl.AddToClassList("bt-ue-typename");
            row.Add(typeLbl);

            var spacer = new VisualElement();
            spacer.AddToClassList("bt-ue-spacer");
            row.Add(spacer);

            var statusDot = new Label("●");
            statusDot.name = "bt-status-dot";
            statusDot.AddToClassList("bt-ue-status-dot");
            row.Add(statusDot);

            titleContainer.Add(row);
        }

        // ── 바디 (노드 이름) ────────────────────────────
        private void BuildBody()
        {
            extensionContainer.Clear();
            extensionContainer.AddToClassList("bt-ue-body");

            var nameLabel = new Label(NodeSO.nodeName);
            nameLabel.AddToClassList("bt-ue-nodename");
            extensionContainer.Add(nameLabel);

            var desc = GetDescText(NodeSO);
            if (!string.IsNullOrEmpty(desc))
            {
                var descLabel = new Label(desc);
                descLabel.AddToClassList("bt-ue-desc");
                extensionContainer.Add(descLabel);
            }
        }

        // ── UE 레이아웃 재배치 ─────────────────────────
        // 목표 순서: [inputContainer] → [titleContainer] → [extensionContainer] → [outputContainer]
        private void RearrangeLayout()
        {
            var nodeBorder    = this.Q("node-border");
            var titleCont     = this.Q("title");
            var extensionCont = this.Q("extension");
            var inputCont     = this.Q("input");
            var outputCont    = this.Q("output");

            if (nodeBorder == null) return;

            // 필요 요소 분리
            titleCont?.RemoveFromHierarchy();
            extensionCont?.RemoveFromHierarchy();
            inputCont?.RemoveFromHierarchy();
            outputCont?.RemoveFromHierarchy();

            // nodeBorder의 나머지 래퍼 요소들(#contents 등) 제거
            // 단, nodeBorder 자체의 클래스/이벤트는 유지되므로 VisualElement 교체는 안전
            var remaining = new System.Collections.Generic.List<VisualElement>();
            foreach (var child in nodeBorder.Children())
                remaining.Add(child);
            foreach (var child in remaining)
                child.RemoveFromHierarchy();

            // UE 순서로 재삽입
            if (inputCont != null)
            {
                inputCont.AddToClassList("bt-ue-portrow-top");
                nodeBorder.Add(inputCont);
            }
            if (titleCont != null)
                nodeBorder.Add(titleCont);
            if (extensionCont != null)
            {
                extensionCont.style.display = DisplayStyle.Flex;
                nodeBorder.Add(extensionCont);
            }
            if (outputCont != null)
            {
                outputCont.AddToClassList("bt-ue-portrow-bottom");
                nodeBorder.Add(outputCont);
            }
        }

        // ── 선택 콜백 ─────────────────────────────────
        public override void OnSelected()
        {
            base.OnSelected();
            OnSelectionChanged?.Invoke(this, true);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            OnSelectionChanged?.Invoke(this, false);
        }

        // ── 런타임 상태 ───────────────────────────────
        public void BindRuntimeNode(BTNode node)   => _runtimeNode = node;
        public void UnbindRuntimeNode()            { _runtimeNode = null; ApplyStatus(null); }
        public void RefreshRuntimeStatus()         => ApplyStatus(_runtimeNode?.LastStatus);

        private void ApplyStatus(NodeStatus? status)
        {
            RemoveFromClassList("bt-running");
            RemoveFromClassList("bt-success");
            RemoveFromClassList("bt-failure");

            var dot = this.Q<Label>("bt-status-dot");
            dot?.RemoveFromClassList("bt-dot-running");
            dot?.RemoveFromClassList("bt-dot-success");
            dot?.RemoveFromClassList("bt-dot-failure");

            if (status == null) return;

            switch (status.Value)
            {
                case NodeStatus.Running: AddToClassList("bt-running"); dot?.AddToClassList("bt-dot-running"); break;
                case NodeStatus.Success: AddToClassList("bt-success"); dot?.AddToClassList("bt-dot-success"); break;
                default:                 AddToClassList("bt-failure"); dot?.AddToClassList("bt-dot-failure"); break;
            }
        }

        // ── 데코레이터 뱃지 (Unreal식 인라인 표시) ────
        public void AddDecoratorBadge(BTNodeSO decorator)
        {
            var badge = new VisualElement();
            badge.AddToClassList("bt-decorator-badge");

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;

            var icon = new Label(decorator is BTCooldownSO ? "⏱" : "!");
            icon.AddToClassList("bt-dec-icon");
            row.Add(icon);

            string text = decorator switch
            {
                BTCooldownSO cd => $"Cooldown  {cd.cooldown:F1}s",
                _               => "Inverter"
            };
            var lbl = new Label(text);
            lbl.AddToClassList("bt-dec-label");
            row.Add(lbl);

            badge.Add(row);
            extensionContainer.Insert(0, badge);
        }

        public void ClearDecoratorBadges()
        {
            var toRemove = new System.Collections.Generic.List<VisualElement>();
            foreach (var child in extensionContainer.Children())
                if (child.ClassListContains("bt-decorator-badge"))
                    toRemove.Add(child);
            foreach (var e in toRemove)
                e.RemoveFromHierarchy();
        }

        // ── 유틸 ──────────────────────────────────────
        private static string GetTypeClass(BTNodeSO so) => so switch
        {
            BTSelectorSO                                          => "bt-type-selector",
            BTSequenceSO                                          => "bt-type-sequence",
            BTRandomSelectorSO                                    => "bt-type-random",
            BTInverterSO                                          => "bt-type-inverter",
            BTCooldownSO                                          => "bt-type-cooldown",
            _ when so.GetType().Name.Contains("Action")          => "bt-type-action",
            _ when so.GetType().Name.Contains("Cond")            => "bt-type-condition",
            _                                                     => "bt-type-action"
        };

        private static string GetTypeIcon(BTNodeSO so) => so switch
        {
            BTSelectorSO       => "?",
            BTSequenceSO       => "→",
            BTRandomSelectorSO => "⟳",
            BTInverterSO       => "!",
            BTCooldownSO       => "⏱",
            _ when so.GetType().Name.Contains("Action") => "▶",
            _                                            => "◆"
        };

        private static string GetTypeName(BTNodeSO so) => so switch
        {
            BTSelectorSO       => "Selector",
            BTSequenceSO       => "Sequence",
            BTRandomSelectorSO => "Random Selector",
            BTInverterSO       => "Inverter",
            BTCooldownSO       => "Cooldown",
            _ => so.GetType().Name
                    .Replace("SO", "")
                    .Replace("BTAction_", "")
                    .Replace("BTCond_", "")
        };

        private static string GetDescText(BTNodeSO so) => so switch
        {
            // ── 컴포짓/데코레이터 ───────────────────────
            BTSelectorSO       s => $"{s.children.Count} children",
            BTSequenceSO       s => $"{s.children.Count} children",
            BTRandomSelectorSO s => $"{s.children.Count} children",
            BTCooldownSO       c => $"Cooldown  {c.cooldown:F1}s",

            // ── 조건 노드 ────────────────────────────────
            BTCond_DistanceSO d => d.check switch {
                DistanceCheck.LessThan    => $"dist < {d.maxDistance:F1}",
                DistanceCheck.GreaterThan => $"dist > {d.minDistance:F1}",
                _                         => $"{d.minDistance:F1} ≤ dist ≤ {d.maxDistance:F1}",
            },
            BTCond_PlayerStateSO  p => p.query.ToString().Replace("Is", ""),
            BTCond_CurrentStateSO c => c.invert ? $"NOT {c.stateName}" : c.stateName,
            BTCond_RandomChanceSO r => $"{r.probability * 100f:F0}%",
            BTCond_HPPercentSO    h => h.check == HPCheck.LessThan
                ? $"HP < {h.threshold * 100f:F0}%"
                : $"HP > {h.threshold * 100f:F0}%",

            // ── 액션 노드 ────────────────────────────────
            BTAction_CircleSO c => $"{c.minDuration:F1} ~ {c.maxDuration:F1}s",
            BTAction_GuardSO  g => $"{g.minDuration:F1} ~ {g.maxDuration:F1}s",

            _ => ""
        };
    }
}
