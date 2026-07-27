#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UPlayGround.Tests.Editor
{
    /// <summary>KCC 이동 정책 EditMode 테스트를 메뉴 또는 Temp 요청 파일로 실행한다.</summary>
    [InitializeOnLoad]
    public static class MovementTestAutomation
    {
        public const string RequestPath = "Temp/KCCMovementTestRequest.txt";
        public const string ResultPath = "Temp/KCCMovementTestResults.xml";

        static MovementTestAutomation()
        {
            EditorApplication.update -= PollRequest;
            EditorApplication.update += PollRequest;
        }

        [MenuItem("Tools/Movement/KCC EditMode 테스트 실행")]
        public static void RunEditMode()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var writer = new ResultWriter(api);
            api.RegisterCallbacks(writer);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "UPlayGround.Movement.Tests" },
            }));
            Debug.Log("[MovementTests] UPlayGround.Movement.Tests 실행 요청");
        }

        private static void PollRequest()
        {
            if (EditorApplication.isCompiling || !File.Exists(RequestPath))
                return;

            File.Delete(RequestPath);
            RunEditMode();
        }

        private sealed class ResultWriter : ICallbacks
        {
            private readonly TestRunnerApi _api;

            public ResultWriter(TestRunnerApi api)
            {
                _api = api;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                File.WriteAllText(ResultPath, result.ToXml().OuterXml);
                Debug.Log(
                    $"[MovementTests] 완료: 성공 {result.PassCount}, 실패 {result.FailCount}, " +
                    $"건너뜀 {result.SkipCount}, 결과 {ResultPath}");
                _api.UnregisterCallbacks(this);
                UnityEngine.Object.DestroyImmediate(_api);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test.HasChildren
                    || !result.ResultState.StartsWith("Failed", StringComparison.Ordinal))
                    return;

                Debug.LogError(
                    $"[MovementTests] 실패: {result.FullName}\n" +
                    $"{result.Message}\n{result.StackTrace}");
            }
        }
    }
}
#endif
