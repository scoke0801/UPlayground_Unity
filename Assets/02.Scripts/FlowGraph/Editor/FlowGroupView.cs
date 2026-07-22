using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>FlowGraphGroup(데이터)과 동기화되는 캔버스 그룹. 멤버/타이틀 변경을 에셋에 즉시 반영한다.</summary>
    public sealed class FlowGroupView : Group
    {
        private readonly FlowGraphSO _graph;
        private bool _suppressSync;

        public FlowGroupView(FlowGraphSO graph, FlowGraphGroup data)
        {
            _graph = graph;
            Data = data;
            title = string.IsNullOrEmpty(data.title) ? "그룹" : data.title;
            SetPosition(new Rect(data.position, Vector2.zero));
        }

        public FlowGraphGroup Data { get; }

        /// <summary>로드 시 멤버 추가가 데이터에 역기록되지 않도록 억제한 채 실행한다.</summary>
        public void AddElementsWithoutSync(System.Collections.Generic.IEnumerable<GraphElement> elements)
        {
            _suppressSync = true;
            foreach (GraphElement element in elements)
                AddElement(element);
            _suppressSync = false;
        }

        protected override void OnElementsAdded(System.Collections.Generic.IEnumerable<GraphElement> elements)
        {
            base.OnElementsAdded(elements);
            if (_suppressSync)
                return;

            RecordUndo("그룹 멤버 추가");
            foreach (GraphElement element in elements)
            {
                if (element is FlowNodeView nodeView && !Data.nodeIds.Contains(nodeView.FlowNode.id))
                    Data.nodeIds.Add(nodeView.FlowNode.id);
            }
            EditorUtility.SetDirty(_graph);
        }

        protected override void OnElementsRemoved(System.Collections.Generic.IEnumerable<GraphElement> elements)
        {
            base.OnElementsRemoved(elements);
            if (_suppressSync)
                return;

            RecordUndo("그룹 멤버 제거");
            foreach (GraphElement element in elements)
            {
                if (element is FlowNodeView nodeView)
                    Data.nodeIds.Remove(nodeView.FlowNode.id);
            }
            EditorUtility.SetDirty(_graph);
        }

        protected override void OnGroupRenamed(string oldName, string newName)
        {
            base.OnGroupRenamed(oldName, newName);
            RecordUndo("그룹 이름 변경");
            Data.title = newName;
            EditorUtility.SetDirty(_graph);
        }

        public void SavePosition()
        {
            Data.position = GetPosition().position;
            EditorUtility.SetDirty(_graph);
        }

        private void RecordUndo(string label)
        {
            if (_graph != null)
                Undo.RegisterCompleteObjectUndo(_graph, label);
        }
    }
}
