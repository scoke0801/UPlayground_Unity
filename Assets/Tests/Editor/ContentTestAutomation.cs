#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UPlayGround.EditorTools;

namespace UPlayGround.Tests.Editor
{
    /// <summary>대화·진행도·FlowGraph 테스트를 열린 에디터에서 실행하고 결과를 기록한다.</summary>
    [InitializeOnLoad]
    public static class ContentTestAutomation
    {
        private const string RequestPath = "Temp/ContentTestRequest.txt";
        private static TestRunnerApi _api;

        static ContentTestAutomation()
        {
            EditorApplication.update += PollRequest;
        }

        /// <summary>콘텐츠 진행과 호수 스토리 에셋의 EditMode 검증을 실행한다.</summary>
        [UPlaygroundTool("UPlayGround/검증/스토리 EditMode 테스트")]
        public static void RunEditMode() => Run(TestMode.EditMode);

        /// <summary>FlowGraph 실행과 상호작용 수명주기의 PlayMode 검증을 실행한다.</summary>
        [UPlaygroundTool("UPlayGround/검증/스토리 PlayMode 테스트")]
        public static void RunPlayMode() => Run(TestMode.PlayMode);

        private static void PollRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode || _api != null
                || !File.Exists(RequestPath)) return;
            string mode = File.ReadAllText(RequestPath).Trim();
            File.Delete(RequestPath);
            Run(mode == "PlayMode" ? TestMode.PlayMode : TestMode.EditMode);
        }

        private static void Run(TestMode mode)
        {
            if (_api != null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _api.RegisterCallbacks(new ResultWriter(mode));
            _api.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode,
                assemblyNames = mode == TestMode.EditMode
                    ? new[] { "UPlayGround.Content.Tests", "UPlayGround.FlowGraph.Tests" }
                    : new[] { "UPlayGround.FlowGraph.PlayModeTests" },
            }));
        }

        private sealed class ResultWriter : ICallbacks
        {
            private readonly string _resultPath;

            public ResultWriter(TestMode mode) => _resultPath = $"Temp/ContentTests-{mode}.xml";
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                File.WriteAllText(_resultPath, result.ToXml().OuterXml);
                Debug.Log($"[ContentTests] 성공 {result.PassCount}, 실패 {result.FailCount}, 결과 {_resultPath}");
                TestRunnerApi.UnregisterTestCallback(this);
                if (_api == null) return;
                UnityEngine.Object.DestroyImmediate(_api);
                _api = null;
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.Test.HasChildren && result.ResultState.StartsWith("Failed", StringComparison.Ordinal))
                    Debug.LogError($"[ContentTests] {result.FullName}\n{result.Message}");
            }
        }
    }
}
#endif
