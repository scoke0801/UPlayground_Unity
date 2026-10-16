using UnityEditor;
using UnityEditor.SceneManagement;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor
{
    /// <summary>전체 동료를 사용할 수 있는 독립 전투 시험장의 진입점.</summary>
    public static class CombatTestMapTool
    {
        internal const string ScenePath = "Assets/01.Scenes/Test/CombatTestMap.unity";

        /// <summary>빈 전투 시험장을 열고 현재 씬에서 플레이하도록 준비한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/전투 테스트 맵 열기")]
        public static void Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            // Boot의 새 게임 선택을 거치지 않아야 전체 카탈로그가 테스트 로스터로 준비된다.
            EditorSceneManager.playModeStartScene = null;
            CombatTestSessionWindow.Open();
        }
    }
}
