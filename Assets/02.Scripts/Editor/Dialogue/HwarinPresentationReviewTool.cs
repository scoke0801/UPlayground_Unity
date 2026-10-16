using UnityEditor;
using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.EditorTools
{
    /// <summary>현재 플레이 씬에서 보상 없는 화린 기능 검수 대화를 실행한다.</summary>
    public static class HwarinPresentationReviewTool
    {
        public const string GraphPath = "Assets/10.Datas/Dialogue/Presentation/Hwarin/DLG_HwarinPresentationReview.asset";

        [UPlaygroundTool("UPlayGround/내러티브/대화/화린 연출 검수")]
        public static void Run()
        {
            var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(GraphPath);
            Selection.activeObject = graph;
            if (!EditorApplication.isPlaying)
            {
                Debug.Log("Boot에서 Play 후 호수 시나리오에 진입하고 같은 도구를 실행하세요. 검수 그래프를 선택했습니다.");
                return;
            }
            var manager = DialogueManager.Instance;
            if (manager == null || manager.IsDialogueActive)
            {
                Debug.LogWarning("게임 초기화와 현재 대화 종료 후 다시 실행하세요.");
                return;
            }
            manager.StartDialogue(graph);
        }
    }
}
