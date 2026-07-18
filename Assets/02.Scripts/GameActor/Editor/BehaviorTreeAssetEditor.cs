#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    [CustomEditor(typeof(BehaviorTreeAsset))]
    public class BehaviorTreeAssetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var openButton = new Button(() => BehaviorTreeEditorWindow.Open(target as BehaviorTreeAsset))
            {
                text = "Behavior Tree Editor 열기"
            };
            openButton.style.marginTop = 8f;
            openButton.style.height = 26f;
            root.Add(openButton);

            return root;
        }
    }
}
#endif
