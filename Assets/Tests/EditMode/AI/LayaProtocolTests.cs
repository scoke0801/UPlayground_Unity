using System;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.AI.CombatDecision;
using UPlayGround.Data.AI;

namespace UPlayGround.AI.Tests
{
    public sealed class LayaProtocolTests
    {
        private LayaTrainingProfileSO _profile;
        private JObject _request, _response;
        [SetUp] public void Setup()
        {
            _profile = ScriptableObject.CreateInstance<LayaTrainingProfileSO>();
            _request = JObject.Parse("{\"protocol_version\":1,\"request_id\":\"r1\",\"session_id\":\"s1\",\"state_seq\":4,\"observation\":{},\"candidates\":[{\"id\":\"Attack\"},{\"id\":\"Chase\"}]}");
            _response = JObject.Parse("{\"protocol_version\":1,\"request_id\":\"r1\",\"session_id\":\"s1\",\"state_seq\":4,\"selected_action_id\":\"Attack\",\"probabilities\":{\"Attack\":0.7,\"Chase\":0.3},\"selected_probability\":0.7,\"decision_source\":\"laya\",\"inference_ms\":123,\"usage\":{\"truncated\":false,\"state_tokens_dropped\":0}}");
            _response["metadata"] = new JObject { ["backend"] = _profile.backend, ["model"] = _profile.model, ["revision"] = _profile.revision, ["config_sha256"] = _profile.configHash };
        }
        [TearDown] public void Teardown() => UnityEngine.Object.DestroyImmediate(_profile);
        [Test] public void 유효한_응답을_허용한다() => Assert.True(LayaProtocol.ValidateResponse(_request, _response, _profile, out _));
        [TestCase("request_id", "old")][TestCase("session_id", "old")][TestCase("selected_action_id", "Teleport")]
        public void 다른_식별자와_후보밖_행동을_거절한다(string key, string value)
        { _response[key] = value; Assert.False(LayaProtocol.ValidateResponse(_request, _response, _profile, out _)); }
        [Test] public void 관측번호_불일치를_거절한다()
        { _response["state_seq"] = 5; Assert.False(LayaProtocol.ValidateResponse(_request, _response, _profile, out _)); }
        [TestCase(-.1)][TestCase(1.1)][TestCase(double.NaN)]
        public void 잘못된_확률을_거절한다(double value)
        { _response["probabilities"]["Attack"] = value; Assert.False(LayaProtocol.ValidateResponse(_request, _response, _profile, out _)); }
        [Test] public void 설정불일치와_토큰손실을_거절한다()
        {
            _response["metadata"]["config_sha256"] = "other";
            Assert.False(LayaProtocol.ValidateResponse(_request, _response, _profile, out _));
            _response["metadata"]["config_sha256"] = _profile.configHash;
            _response["usage"]["truncated"] = true;
            Assert.False(LayaProtocol.ValidateResponse(_request, _response, _profile, out _));
        }
        [Test] public void TTL_경계와_중복소비_세대_대상_변경을_검사한다()
        {
            Assert.True(LayaProtocol.IsLive(10.75,10,.75,1,1,2,2,3,3,false));
            Assert.False(LayaProtocol.IsLive(10.751,10,.75,1,1,2,2,3,3,false));
            Assert.False(LayaProtocol.IsLive(10.2,10,.75,1,1,2,2,3,3,true));
            Assert.False(LayaProtocol.IsLive(10.2,10,.75,2,1,2,2,3,3,false));
            Assert.False(LayaProtocol.IsLive(10.2,10,.75,1,1,3,2,3,3,false));
            Assert.False(LayaProtocol.IsLive(10.2,10,.75,1,1,2,2,4,3,false));
        }
        [Test] public void 관측_예산을_넘으면_자르지_않고_거절한다()
        { _request["observation"]["text"] = new string('한', 4096); Assert.False(LayaProtocol.IsWithinBudget(_request)); }
        [Test] public void 기록_상한과_본문_만료를_보존한다()
        {
            var store = new LayaDiagnostics { Capacity = 2, BodyBudget = 100 };
            store.Add(new LayaDecisionRecord { requestJson = new string('x',150) });
            Assert.True(store.Records[0].bodyExpired);
            store.Add(new LayaDecisionRecord()); store.Add(new LayaDecisionRecord());
            Assert.AreEqual(2,store.Records.Count); Assert.AreEqual(1,store.Evicted);
        }
        [Test] public void 비밀정보를_주소에_허용하지_않는다()
        {
            Assert.False(LayaProtocol.TryEndpoint("http://user:secret@localhost:8765",out _));
            Assert.False(LayaProtocol.TryEndpoint("http://localhost:8765?token=secret",out _));
            Assert.True(LayaProtocol.TryEndpoint("http://127.0.0.1:8765",out _));
        }
    }
}
