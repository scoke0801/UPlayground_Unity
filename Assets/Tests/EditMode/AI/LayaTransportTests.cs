using System.Collections;
using Cysharp.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UPlayGround.AI.CombatDecision;

namespace UPlayGround.AI.Tests
{
    /// <summary>명시적으로 실행한 로컬 모의 서버만 사용하는 HTTP 검사.</summary>
    public sealed class LayaTransportTests
    {
        [UnityTest] public IEnumerator 모의_HTTP_성공과_장애를_구분한다()
        {
            foreach (string fault in new[] { "none", "503", "timeout", "oversized", "malformed" })
            {
                var request = new JObject { ["protocol_version"] = 1, ["request_id"] = fault, ["session_id"] = "transport-test",
                    ["state_seq"] = 1, ["observation"] = new JObject { ["mock_fault"] = fault },
                    ["candidates"] = new JArray(new JObject { ["id"] = "Attack" }, new JObject { ["id"] = "Chase" }) };
                var task = LayaDecisionClient.Send("http://127.0.0.1:18765",request.ToString(),.4f,CancellationToken.None).AsTask();
                while (!task.IsCompleted) yield return null;
                var reply = task.Result;
                if (fault == "none") { Assert.AreEqual(200,reply.Status); Assert.IsNull(reply.Error); Assert.True((bool)JObject.Parse(reply.Body)["mock"]); }
                else if (fault == "malformed") Assert.Throws<Newtonsoft.Json.JsonReaderException>(() => JObject.Parse(reply.Body));
                else Assert.IsNotNull(reply.Error, fault);
            }
        }
        [UnityTest] public IEnumerator 요청_취소는_성공으로_기록하지_않는다()
        {
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            var task = LayaDecisionClient.Send("http://127.0.0.1:18765",null,1,cancellation.Token).AsTask();
            while (!task.IsCompleted) yield return null;
            Assert.IsNotNull(task.Result.Error);
        }
    }
}
