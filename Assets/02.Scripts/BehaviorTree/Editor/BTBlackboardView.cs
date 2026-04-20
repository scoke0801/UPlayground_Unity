using UnityEngine.UIElements;
using UPlayGround.BehaviorTree;

namespace UPlayGround.Editor.BehaviorTree
{
    public class BTBlackboardView : VisualElement
    {
        private EnemyBlackboard _bb;
        private VisualElement   _rows;
        private Label           _headerLabel;

        public BTBlackboardView()
        {
            AddToClassList("bt-blackboard");

            _headerLabel = new Label("Blackboard (편집 모드)");
            _headerLabel.AddToClassList("bt-blackboard-title");
            Add(_headerLabel);

            _rows = new VisualElement();
            Add(_rows);
        }

        public void SetBlackboard(EnemyBlackboard bb)
        {
            _bb = bb;
            _headerLabel.text = bb != null ? "Blackboard (런타임)" : "Blackboard (편집 모드)";
            Refresh();
        }

        public void Refresh()
        {
            _rows.Clear();

            if (_bb == null)
            {
                AddRow("상태", "BT 미실행 (런타임 전용)");
                return;
            }

            AddRow("HasTarget",         _bb.HasTarget.ToString());
            AddRow("DistanceToTarget",  $"{_bb.DistanceToTarget:F2}");
            AddRow("CurrentState",      _bb.CurrentStateName ?? "-");
            AddRow("ActionReady",       _bb.IsActionReady.ToString());
            AddRow("LastActionTime",    $"{_bb.LastActionTime:F2}");
            AddRow("NextActionDelay",   $"{_bb.NextActionDelay:F2}");
            AddRow("─────────────", "");
            AddRow("PhaseAllowCharge",  _bb.PhaseAllowCharge.ToString());
            AddRow("PhaseAllowFlank",   _bb.PhaseAllowFlank.ToString());
            AddRow("ChargeChance",      $"{_bb.PhaseChargeChance:F2}");
            AddRow("FlankChance",       $"{_bb.PhaseFlankChance:F2}");
            AddRow("MaxConsecAttacks",  _bb.PhaseMaxConsecutiveAttacks.ToString());
            AddRow("─────────────", "");
            AddRow("OptimalDist",       $"{_bb.OptimalCombatDistance:F2}");
            AddRow("MaxAttackRange",    $"{_bb.MaxAttackRange:F2}");
            AddRow("PersonalSpace",     $"{_bb.PersonalSpaceDistance:F2}");
            AddRow("HasGuardMotion",    _bb.HasGuardMotion.ToString());
            AddRow("ConsecDefense",     _bb.ConsecutiveDefensiveCount.ToString());
        }

        private void AddRow(string key, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("bt-blackboard-row");

            var keyLabel = new Label(key);
            keyLabel.AddToClassList("bt-blackboard-key");

            var valLabel = new Label(value);
            valLabel.AddToClassList("bt-blackboard-value");

            row.Add(keyLabel);
            row.Add(valLabel);
            _rows.Add(row);
        }
    }
}
