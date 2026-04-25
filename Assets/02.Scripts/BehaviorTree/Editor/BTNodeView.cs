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

            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private void OnMouseDown(MouseDownEvent e)
        {
            if (e.clickCount != 2 || _runtimeNode == null) return;
            _runtimeNode.BreakpointEnabled = !_runtimeNode.BreakpointEnabled;
            EnableInClassList("bt-breakpoint", _runtimeNode.BreakpointEnabled);
            e.StopPropagation();
        }

        // ── 포트 생성 ─────────────────────────────────
        private void CreatePorts()
        {
            bool isMultiOutput = NodeSO is BTSelectorSO or BTSequenceSO or BTRandomSelectorSO or BTGuardSO;
            bool hasOutput     = isMultiOutput || NodeSO is BTInverterSO or BTCooldownSO or BTForceSuccessSO or BTLoopSO;

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

            // 서비스 스트립 (Selector / Sequence에 서비스가 있을 때)
            var svcList = NodeSO switch
            {
                BTSelectorSO sel => sel.services,
                BTSequenceSO seq => seq.services,
                _                => null
            };
            if (svcList != null)
                foreach (var svc in svcList)
                    if (svc != null) BuildServiceStrip(svc);
        }

        private void BuildServiceStrip(BTServiceSO svc)
        {
            var strip = new VisualElement();
            strip.AddToClassList("bt-service-strip");

            var icon = new Label("★");
            icon.AddToClassList("bt-svc-icon");
            strip.Add(icon);

            var lbl = new Label($"{svc.serviceName}  {svc.tickInterval:F2}s");
            lbl.AddToClassList("bt-svc-label");
            strip.Add(lbl);

            extensionContainer.Add(strip);
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

        // ── 실행 순서 인덱스 배지 ──────────────────────
        public void SetChildIndex(int index)
        {
            var existing = inputContainer.Q<Label>("bt-child-index");
            existing?.RemoveFromHierarchy();

            var badge = new Label(index.ToString());
            badge.name = "bt-child-index";
            badge.AddToClassList("bt-child-index");
            inputContainer.Add(badge);
        }

        // ── 런타임 상태 ───────────────────────────────
        public void BindRuntimeNode(BTNode node)
        {
            _runtimeNode = node;
            EnableInClassList("bt-breakpoint", node?.BreakpointEnabled ?? false);
        }
        public void UnbindRuntimeNode()            { _runtimeNode = null; ApplyStatus(null); RemoveFromClassList("bt-breakpoint"); }
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

            // 타입별 색상 클래스
            badge.AddToClassList(decorator switch
            {
                BTGuardSO        => "bt-dec-guard",
                BTCooldownSO     => "bt-dec-cooldown",
                BTForceSuccessSO => "bt-dec-forcesuccess",
                BTLoopSO         => "bt-dec-loop",
                _                => "bt-dec-inverter"
            });

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;

            string icon = decorator switch
            {
                BTGuardSO        => "◉",
                BTCooldownSO     => "⏱",
                BTForceSuccessSO => "✓",
                BTLoopSO         => "↺",
                _                => "!"
            };
            var iconLbl = new Label(icon);
            iconLbl.AddToClassList("bt-dec-icon");
            row.Add(iconLbl);

            string text = decorator switch
            {
                BTGuardSO        g  => BuildGuardText(g),
                BTCooldownSO     cd => $"Cooldown  {cd.cooldown:F1}s",
                BTLoopSO         lp => $"Loop  ×{(lp.loopCount < 0 ? "∞" : lp.loopCount.ToString())}",
                BTForceSuccessSO    => "ForceSuccess",
                _                   => "Inverter"
            };
            var lbl = new Label(text);
            lbl.AddToClassList("bt-dec-label");
            row.Add(lbl);

            badge.Add(row);

            // Guard: abortType 표시
            if (decorator is BTGuardSO grd && grd.abortType != AbortType.None)
            {
                var abortRow = new VisualElement();
                abortRow.style.flexDirection = FlexDirection.Row;
                var abortLbl = new Label($"aborts: {grd.abortType.ToString().ToLower()}");
                abortLbl.AddToClassList("bt-dec-abort");
                abortRow.Add(abortLbl);
                badge.Add(abortRow);
            }

            extensionContainer.Insert(0, badge);
            RefreshExpandedState();
        }

        private static string BuildGuardText(BTGuardSO g)
        {
            string key = string.IsNullOrEmpty(g.observeKey) ? "" : $": {g.observeKey}";
            return string.IsNullOrEmpty(g.nodeName) ? $"Guard{key}" : $"{g.nodeName}{key}";
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
            BTForceSuccessSO                                      => "bt-type-forcesuccess",
            BTLoopSO                                              => "bt-type-loop",
            BTGuardSO                                             => "bt-type-guard",
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
            BTForceSuccessSO   => "✓",
            BTLoopSO           => "↺",
            BTGuardSO          => "◉",
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
            BTForceSuccessSO   => "ForceSuccess",
            BTLoopSO           => "Loop",
            BTGuardSO          => "Guard",
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
            BTCooldownSO       c  => $"Cooldown  {c.cooldown:F1}s",
            BTLoopSO           lp => lp.loopCount < 0 ? "∞  반복" : $"×{lp.loopCount}  반복",
            BTGuardSO           g => g.abortType == AbortType.None
                                        ? (g.condition != null ? $"if  {g.condition.nodeName}" : "no condition")
                                        : (g.condition != null ? $"if  {g.condition.nodeName}  [{g.abortType}]" : $"[{g.abortType}]"),
            BTAction_WaitSO    w  => w.randomDeviation > 0f
                                        ? $"{w.duration:F1}s ± {w.randomDeviation:F1}s"
                                        : $"{w.duration:F1}s",

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
