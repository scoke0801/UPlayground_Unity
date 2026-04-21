using UnityEngine.UIElements;
using UPlayGround.BehaviorTree;

namespace UPlayGround.Editor.BehaviorTree
{
    public class BTBlackboardView : VisualElement
    {
        private RuntimeBlackboard _bb;
        private BTBlackboardSO    _so;
        private VisualElement     _rows;
        private Label             _headerLabel;

        private static readonly string[] TypeColors = { "#7ec8e3", "#f5a623", "#b8f5a0", "#e3b8f5" };

        public BTBlackboardView()
        {
            AddToClassList("bt-blackboard");

            _headerLabel = new Label("Blackboard (편집 모드)");
            _headerLabel.AddToClassList("bt-blackboard-title");
            Add(_headerLabel);

            _rows = new VisualElement();
            Add(_rows);
        }

        /// <summary> 편집 모드: SO 키 목록 표시 </summary>
        public void SetBlackboardSO(BTBlackboardSO so)
        {
            _so = so;
            _bb = null;
            _headerLabel.text = so != null ? $"Blackboard: {so.name}" : "Blackboard (편집 모드)";
            Refresh();
        }

        /// <summary> 런타임 모드: 실제 값 표시 </summary>
        public void SetBlackboard(RuntimeBlackboard bb)
        {
            _bb = bb;
            _so = null;
            _headerLabel.text = bb != null ? "Blackboard (런타임)" : "Blackboard (편집 모드)";
            Refresh();
        }

        public void Refresh()
        {
            _rows.Clear();

            if (_bb != null)
                RefreshRuntime();
            else if (_so != null)
                RefreshEditMode();
            else
                AddRow("──", "BT 미실행", "");
        }

        private void RefreshRuntime()
        {
            foreach (var kv in _bb.Bools)
                AddRow("[BOOL]",  kv.Key, kv.Value.ToString());
            foreach (var kv in _bb.Floats)
                AddRow("[FLOAT]", kv.Key, $"{kv.Value:F2}");
            foreach (var kv in _bb.Ints)
                AddRow("[INT]",   kv.Key, kv.Value.ToString());
            foreach (var kv in _bb.Strings)
                AddRow("[STR]",   kv.Key, kv.Value);

            AddRow("──", "IsActionReady", _bb.IsActionReady.ToString());
        }

        private void RefreshEditMode()
        {
            if (_so.keys == null || _so.keys.Count == 0)
            {
                AddRow("──", "키 없음", "");
                return;
            }

            foreach (var key in _so.keys)
            {
                string tag = key.keyType switch
                {
                    BlackboardKeyType.Bool   => "[BOOL]",
                    BlackboardKeyType.Float  => "[FLOAT]",
                    BlackboardKeyType.Int    => "[INT]",
                    BlackboardKeyType.String => "[STR]",
                    _                        => "[?]"
                };
                AddRow(tag, key.keyName, "");
            }
        }

        private void AddRow(string typeTag, string key, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("bt-blackboard-row");

            var tagLabel = new Label(typeTag);
            tagLabel.AddToClassList("bt-blackboard-type");

            var keyLabel = new Label(key);
            keyLabel.AddToClassList("bt-blackboard-key");

            var valLabel = new Label(value);
            valLabel.AddToClassList("bt-blackboard-value");

            row.Add(tagLabel);
            row.Add(keyLabel);
            row.Add(valLabel);
            _rows.Add(row);
        }
    }
}
