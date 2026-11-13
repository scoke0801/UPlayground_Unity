using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace UPlayGround.Tests.AmbientFauna
{
    /// <summary>MCP 도메인 재로드와 별개로 관련 실행 전체의 NUnit 원본을 보존한다.</summary>
    [InitializeOnLoad]
    internal sealed class AmbientFaunaTestEvidence : ICallbacks
    {
        static AmbientFaunaTestEvidence() => TestRunnerApi.RegisterTestCallback(new AmbientFaunaTestEvidence());
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            if (!ContainsAssembly(result, "UPlayGround.AmbientFauna.")) return;
            const string root = "output/AmbientFauna/Completion";
            Directory.CreateDirectory(root);
            bool playMode = ContainsAssembly(result, "UPlayGround.AmbientFauna.PlayModeTests");
            TestRunnerApi.SaveResultToFile(result, root + (playMode ? "/playmode-final.xml" : "/editmode-final.xml"));
        }

        private static bool ContainsAssembly(ITestResultAdaptor result, string name)
        {
            if (result.Name.Contains(name)) return true;
            if (!result.HasChildren) return false;
            foreach (var child in result.Children)
                if (ContainsAssembly(child, name)) return true;
            return false;
        }
    }
}