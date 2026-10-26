using UnityEditor;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Editor
{
    /// <summary>런타임 초기화 상태를 잃은 Play 세션이 도메인 리로드 뒤 계속 실행되는 것을 막는다.</summary>
    [InitializeOnLoad]
    internal static class GameplayAssemblyReloadGuard
    {
        private const string InterruptedPlayKey = "UPlayGround.GameplayAssemblyReloadGuard.InterruptedPlay";

        static GameplayAssemblyReloadGuard()
        {
            AssemblyReloadEvents.beforeAssemblyReload += RecordInterruptedPlay;

            if (!SessionState.GetBool(InterruptedPlayKey, false))
                return;

            SessionState.EraseBool(InterruptedPlayKey);
            EditorApplication.delayCall += StopInterruptedPlay;
        }

        private static void RecordInterruptedPlay()
        {
            // 비동기 continuation과 서비스/핸들 목록은 핫 리로드로 복구되지 않는다.
            // Instance 접근으로 매니저를 생성하지 않고 실제 게임 세션만 판별한다.
            bool hasGameSession = EditorApplication.isPlaying
                && UnityEngine.Object.FindFirstObjectByType<GameManager>() != null;
            SessionState.SetBool(InterruptedPlayKey, hasGameSession);
        }

        private static void StopInterruptedPlay()
        {
            if (!EditorApplication.isPlaying)
                return;

            Debug.LogWarning("[GameplayAssemblyReloadGuard] 스크립트 리로드로 런타임 초기화 상태가 유실되어 Play를 종료합니다. 컴파일 완료 후 다시 Play하세요.");
            EditorApplication.isPlaying = false;
        }
    }
}
