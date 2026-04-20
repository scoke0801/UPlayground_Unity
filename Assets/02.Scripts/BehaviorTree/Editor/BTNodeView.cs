using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UPlayGround.BehaviorTree;

namespace UPlayGround.Editor.BehaviorTree
{
    public class BTNodeView : Node
    {
        public BTNodeSO NodeSO    { get; private set; }
        public Port     InputPort  { get; private set; }
        public Port     OutputPort { get; private set; }

        /// <summary> 선택/해제 시 에디터 창에 통지 </summary>
        public Action<BTNodeView, bool> OnSelectionChanged;

        private Label  _statusIcon;
        private BTNode _runtimeNode;

        public BTNodeView(BTNodeSO nodeSO) : base()
        {
            NodeSO      = nodeSO;
            title       = nodeSO.nodeName;
            viewDataKey = nodeSO.GetInstanceID().ToString();

            AddTypeLabel();
            AddStatusIcon();
            ApplyTypeStyle();
            CreatePorts();

            RefreshExpandedState();
            RefreshPorts();
        }

        // ── 선택 콜백 오버라이드 ──────────────────────
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

        // ── 포트 생성 ─────────────────────────────────
        private void CreatePorts()
        {
            InputPort = Port.Create<Edge>(
                Orientation.Vertical,
                Direction.Input,
                Port.Capacity.Single,
                typeof(bool));
            InputPort.portName = "";
            inputContainer.Add(InputPort);

            bool isComposite = NodeSO is BTSelectorSO || NodeSO is BTSequenceSO || NodeSO is BTRandomSelectorSO;
            OutputPort = Port.Create<Edge>(
                Orientation.Vertical,
                Direction.Output,
                isComposite ? Port.Capacity.Multi : Port.Capacity.Single,
                typeof(bool));
            OutputPort.portName = "";
            outputContainer.Add(OutputPort);
        }

        // ── 타입 / 설명 라벨 ──────────────────────────
        private void AddTypeLabel()
        {
            var typeLabel = new Label(GetTypeDisplayName(NodeSO));
            typeLabel.AddToClassList("bt-type-label");

            var descLabel = new Label(GetDescText(NodeSO));
            descLabel.AddToClassList("bt-desc-label");

            extensionContainer.Add(typeLabel);
            extensionContainer.Add(descLabel);
        }

        // ── 상태 아이콘 ───────────────────────────────
        private void AddStatusIcon()
        {
            _statusIcon = new Label("●");
            _statusIcon.AddToClassList("bt-status-icon");
            titleContainer.Add(_statusIcon);
        }

        // ── 타입별 스타일 ─────────────────────────────
        private void ApplyTypeStyle()
        {
            AddToClassList("bt-node");

            if (NodeSO is BTSelectorSO || NodeSO is BTSequenceSO || NodeSO is BTRandomSelectorSO)
                AddToClassList("bt-node--composite");
            else if (NodeSO is BTInverterSO || NodeSO is BTCooldownSO)
                AddToClassList("bt-node--decorator");
            else if (NodeSO.GetType().Name.Contains("Action_") || NodeSO.GetType().Name.StartsWith("BTAction"))
                AddToClassList("bt-node--action");
            else if (NodeSO.GetType().Name.Contains("Cond_") || NodeSO.GetType().Name.StartsWith("BTCond"))
                AddToClassList("bt-node--condition");
        }

        // ── 런타임 바인딩 ─────────────────────────────
        public void BindRuntimeNode(BTNode node)   => _runtimeNode = node;
        public void UnbindRuntimeNode()            { _runtimeNode = null; SetStatusStyle(null); }

        public void RefreshRuntimeStatus()
        {
            SetStatusStyle(_runtimeNode?.LastStatus);
        }

        private void SetStatusStyle(NodeStatus? status)
        {
            RemoveFromClassList("bt-node--running");
            RemoveFromClassList("bt-node--success");
            RemoveFromClassList("bt-node--failure");
            _statusIcon.RemoveFromClassList("bt-status-icon--running");
            _statusIcon.RemoveFromClassList("bt-status-icon--success");
            _statusIcon.RemoveFromClassList("bt-status-icon--failure");

            if (status == null) return;

            string cls      = status.Value == NodeStatus.Running ? "running"
                            : status.Value == NodeStatus.Success ? "success" : "failure";
            AddToClassList($"bt-node--{cls}");
            _statusIcon.AddToClassList($"bt-status-icon--{cls}");
        }

        // ── 유틸 ──────────────────────────────────────
        private static string GetTypeDisplayName(BTNodeSO so) => so switch
        {
            BTSelectorSO       => "Selector",
            BTSequenceSO       => "Sequence",
            BTRandomSelectorSO => "Random Selector",
            BTInverterSO       => "Inverter",
            BTCooldownSO c     => $"Cooldown ({c.cooldown:F1}s)",
            _                  => so.GetType().Name
                                    .Replace("SO", "")
                                    .Replace("BTCond_", "Cond: ")
                                    .Replace("BTAction_", "Action: ")
        };

        private static string GetDescText(BTNodeSO so) => so switch
        {
            BTSelectorSO       s => $"{s.children.Count} children",
            BTSequenceSO       s => $"{s.children.Count} children",
            BTRandomSelectorSO s => $"{s.children.Count} children",
            _                    => ""
        };
    }
}
