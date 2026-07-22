#if UNITY_EDITOR
using UnityEditor;
using UPlayGround.Data.Crafting.Editor;
using UPlayGround.Data.Editor.Authoring;
using UPlayGround.Data.Item.Editor;
using UPlayGround.Tool.Editor.Actor;
using UPlayGround.Tool.Editor.Stat;
using UPlayGround.Actor.Editor;

namespace UPlayGround.Editor.Authoring
{
    /// <summary>
    /// 통합 데이터 저작 허브의 보조 도구 액션을 실제 프로젝트 에디터 구현과 연결합니다.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectDataAuthoringToolRegistration
    {
        static ProjectDataAuthoringToolRegistration()
        {
            DataAuthoringToolBridge.Register(DataAuthoringToolBridge.ItemGenerator, ItemDataGeneratorWindow.Open);
            DataAuthoringToolBridge.Register(DataAuthoringToolBridge.RecipeGenerator, RecipeDataGeneratorWindow.Open);
            DataAuthoringToolBridge.Register(DataAuthoringToolBridge.StatGenerator, StatDataGeneratorWindow.Open);
            DataAuthoringToolBridge.Register(DataAuthoringToolBridge.StatCoverage, StatDataGeneratorWindow.ValidateStatDataCoverageMenu);
            DataAuthoringToolBridge.Register(DataAuthoringToolBridge.NpcGenerator, NpcDataGeneratorWindow.Open);
            DataAuthoringToolBridge.Register(DataAuthoringToolBridge.ActorDatabaseEditor, ActorDatabaseEditorWindow.Open);
        }
    }
}
#endif
