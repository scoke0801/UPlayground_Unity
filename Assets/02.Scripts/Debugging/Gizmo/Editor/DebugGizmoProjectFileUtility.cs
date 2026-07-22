using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Debugging.Editor
{
    public static class DebugGizmoProjectFileUtility
    {
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/Debug/Regenerate C# Project Files")]
        public static void RegenerateProjectFiles()
        {
            AssetDatabase.Refresh();

            if (TryInvokeSyncVs())
            {
                Debug.Log("[DebugGizmo] C# 프로젝트 파일 재생성을 요청했습니다. (SyncVS)");
                return;
            }

            if (TryInvokeCodeEditorSync())
            {
                Debug.Log("[DebugGizmo] C# 프로젝트 파일 재생성을 요청했습니다. (CodeEditor)");
                return;
            }

            Debug.LogWarning("[DebugGizmo] C# 프로젝트 파일 재생성 API를 찾지 못했습니다.");
        }

        private static bool TryInvokeSyncVs()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type syncType = assembly.GetType("UnityEditor.SyncVS");
                MethodInfo syncMethod = syncType?.GetMethod(
                    "SyncSolution",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                if (syncMethod == null)
                {
                    continue;
                }

                syncMethod.Invoke(null, null);
                return true;
            }

            return false;
        }

        private static bool TryInvokeCodeEditorSync()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type codeEditorType = assembly.GetType("Unity.CodeEditor.CodeEditor");
                PropertyInfo currentEditorProperty = codeEditorType?.GetProperty(
                    "CurrentEditor",
                    BindingFlags.Public | BindingFlags.Static);
                object currentEditor = currentEditorProperty?.GetValue(null);
                MethodInfo syncAllMethod = currentEditor?.GetType().GetMethod(
                    "SyncAll",
                    BindingFlags.Public | BindingFlags.Instance);

                if (syncAllMethod == null)
                {
                    continue;
                }

                syncAllMethod.Invoke(currentEditor, null);
                return true;
            }

            return false;
        }
    }
}
