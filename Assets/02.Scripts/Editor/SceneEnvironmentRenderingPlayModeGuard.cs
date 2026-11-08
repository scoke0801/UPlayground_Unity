using UnityEditor;
using UnityEngine;
using UPlayGround.Gameplay.World;

namespace UPlayGround.EditorTools
{
    /// <summary>도메인 리로드를 끈 에디터에서도 Play 종료 전에 전역 RP를 복원한다.</summary>
    [InitializeOnLoad]
    internal static class SceneEnvironmentRenderingPlayModeGuard
    {
        static SceneEnvironmentRenderingPlayModeGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += RestoreRendering;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                RestoreRendering();
        }

        private static void RestoreRendering()
        {
            foreach (var binding in UnityEngine.Object.FindObjectsByType<SceneEnvironmentRendering>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                binding.RestoreRendering();
        }
    }
}
