#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Editor.Actor;

namespace UPlayGround.Tool.Editor.Actor
{
    /// <summary>
    /// ActorDefinitionSO Inspector.
    /// 섹션 구성과 디자인은 <see cref="ActorDefinitionDetailView"/>가 단일 소스이며,
    /// 데이터 저작 허브·액터 데이터베이스 에디터와 동일한 화면을 사용한다.
    /// </summary>
    [CustomEditor(typeof(ActorDefinitionSO))]
    public class ActorDefinitionSOEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return ActorDefinitionDetailView.Build(serializedObject, new ActorDefinitionDetailOptions
            {
                ShowOpenHubButton = true,
                ShowAssetHeader   = false,
                ShowHubLinks      = true,
            });
        }
    }
}
#endif
