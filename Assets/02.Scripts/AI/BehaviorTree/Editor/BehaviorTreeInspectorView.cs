#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public class BehaviorTreeInspectorView : VisualElement
    {
        private UnityEditor.Editor _editor;

        public BehaviorTreeInspectorView()
        {
            style.flexGrow = 1;
        }

        public void UpdateSelection(BTNode node)
        {
            Clear();
            if (_editor != null)
                UnityEngine.Object.DestroyImmediate(_editor);

            if (node == null)
            {
                Add(new Label("노드를 선택하세요."));
                return;
            }

            _editor = UnityEditor.Editor.CreateEditor(node);
            Add(new IMGUIContainer(() =>
            {
                if (_editor == null)
                    return;

                _editor.OnInspectorGUI();
            }));
        }
    }
}
#endif
