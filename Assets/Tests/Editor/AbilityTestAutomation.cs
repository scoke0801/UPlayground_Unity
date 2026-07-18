#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UPlayGround.Tests.Editor
{
    [InitializeOnLoad]
    public static class AbilityTestAutomation
    {
        // Temp 요청 파일은 도메인 리로드 직후 한 번만 소비한다.
        private const string RequestPath = "Temp/AbilityTestRequest.txt";

        static AbilityTestAutomation()
        {
            EditorApplication.update += PollRequest;
        }

        [MenuItem("Tools/Ability/테스트/EditMode 실행")]
        public static void RunEditMode()
        {
            Run(TestMode.EditMode, "UPlayGround.Ability.Tests", "EditMode");
        }

        [MenuItem("Tools/Ability/테스트/PlayMode 수직 슬라이스 실행")]
        public static void RunPlayMode()
        {
            Run(
                TestMode.PlayMode,
                "UPlayGround.Ability.PlayModeTests",
                "PlayMode");
        }

        private static void RunRequestedTests()
        {
            if (!File.Exists(RequestPath))
                return;
            string request = File.ReadAllText(RequestPath).Trim();
            File.Delete(RequestPath);
            if (string.Equals(request, "PlayMode", StringComparison.OrdinalIgnoreCase))
                RunPlayMode();
            else
                RunEditMode();
        }

        private static void PollRequest()
        {
            if (EditorApplication.isCompiling || !File.Exists(RequestPath))
                return;
            RunRequestedTests();
        }

        private static void Run(
            TestMode mode,
            string assemblyName,
            string resultName)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultWriter(resultName));
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode,
                assemblyNames = new[] { assemblyName },
            }));
            Debug.Log($"[AbilityTestAutomation] {resultName} 테스트 실행 요청");
        }

        private sealed class ResultWriter : ICallbacks
        {
            private readonly string _resultName;

            public ResultWriter(string resultName)
            {
                _resultName = resultName;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                string path = $"Temp/AbilityTestResults-{_resultName}.xml";
                File.WriteAllText(path, result.ToXml().OuterXml);
                Debug.Log(
                    $"[AbilityTestAutomation] {_resultName} 완료: "
                    + $"성공 {result.PassCount}, 실패 {result.FailCount}, "
                    + $"건너뜀 {result.SkipCount}, 결과 {path}");
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test.HasChildren
                    || !result.ResultState.StartsWith(
                        "Failed",
                        StringComparison.Ordinal))
                    return;
                Debug.LogError(
                    $"[AbilityTestAutomation] 실패: {result.FullName}\n"
                    + $"{result.Message}\n{result.StackTrace}");
            }
        }
    }
}
#endif
