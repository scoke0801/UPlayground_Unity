#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UPlayGround.Data.AI;

namespace UPlayGround.AI.CombatDecision
{
    /// <summary>HTTP v1의 신뢰 경계. 전투 상태와 별개로 응답 원문을 검증한다.</summary>
    public static class LayaProtocol
    {
        public const int MaxBodyBytes = 65536;
        public const int MaxObservationBytes = 4096;
        private static readonly Regex SecretField = new Regex("(\"(?:authorization|access_token|refresh_token|token|api_key|password)\"\\s*:\\s*\")([^\"]*)(\")", RegexOptions.IgnoreCase);
        private static readonly Regex BearerValue = new Regex("Bearer\\s+[A-Za-z0-9._~+/=-]+", RegexOptions.IgnoreCase);

        public static bool ValidateHealth(JObject health, LayaTrainingProfileSO profile) =>
            (bool?)health["ready"] == true && (int?)health["protocol_version"] == 1
            && (string)health["metadata"]?["backend"] == profile.backend
            && (string)health["metadata"]?["model"] == profile.model
            && (string)health["metadata"]?["revision"] == profile.revision
            && (string)health["metadata"]?["config_sha256"] == profile.configHash;

        public static bool ValidateResponse(JObject request, JObject response, LayaTrainingProfileSO profile, out string reason)
        {
            reason = "응답 계약 위반";
            if ((int?)response["protocol_version"] != 1 || !JToken.DeepEquals(request["request_id"], response["request_id"])
                || !JToken.DeepEquals(request["session_id"], response["session_id"])
                || !JToken.DeepEquals(request["state_seq"], response["state_seq"])) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var candidate in (JArray)request["candidates"]) ids.Add((string)candidate["id"]);
            if (response["selected_action_id"]?.Type != JTokenType.String
                || !ids.Contains((string)response["selected_action_id"]) || (string)response["decision_source"] != "laya") return false;
            if (!(response["probabilities"] is JObject probabilities) || probabilities.Count != ids.Count) return false;
            double sum = 0;
            foreach (var pair in probabilities)
            {
                if (!ids.Contains(pair.Key) || !TryNumber(pair.Value, out double value) || value < 0 || value > 1) return false;
                sum += value;
            }
            if (Math.Abs(sum - 1) > 0.015 || !TryNumber(response["selected_probability"], out double selected)
                || Math.Abs(selected - (double)probabilities[(string)response["selected_action_id"]]) > 0.001
                || !TryNumber(response["inference_ms"], out double inference) || inference < 0) return false;
            if ((string)response["metadata"]?["backend"] != profile.backend
                || (string)response["metadata"]?["model"] != profile.model
                || (string)response["metadata"]?["revision"] != profile.revision
                || (string)response["metadata"]?["config_sha256"] != profile.configHash)
            { reason = "모델·revision·몬스터 설정 hash 불일치"; return false; }
            if ((bool?)response["usage"]?["truncated"] != false
                || (int?)response["usage"]?["state_tokens_dropped"] != 0)
            { reason = "관측 또는 후보 토큰 잘림"; return false; }
            reason = "검증 통과";
            return true;
        }

        public static bool IsWithinBudget(JObject request) =>
            Encoding.UTF8.GetByteCount(request.ToString(Formatting.None)) <= MaxBodyBytes
            && Encoding.UTF8.GetByteCount(request["observation"].ToString(Formatting.None)) <= MaxObservationBytes;

        public static bool IsLive(double now, double observed, double ttl, int generation, int expectedGeneration,
            int epoch, int expectedEpoch, int target, int expectedTarget, bool consumed) =>
            !consumed && now >= observed && now - observed <= ttl && generation == expectedGeneration
            && epoch == expectedEpoch && target != 0 && target == expectedTarget;

        public static string Redact(string text)
        {
            if (text == null) return null;
            string token = Environment.GetEnvironmentVariable("LAYA_WORKER_TOKEN");
            if (!string.IsNullOrEmpty(token)) text = text?.Replace(token, "[비밀정보 제외]");
            return BearerValue.Replace(SecretField.Replace(text, "$1[비밀정보 제외]$3"), "Bearer [비밀정보 제외]");
        }

        public static bool TryEndpoint(string value, out Uri uri) => Uri.TryCreate(value, UriKind.Absolute, out uri)
            && (uri.Scheme == "http" || uri.Scheme == "https") && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) && uri.AbsolutePath == "/";

        private static bool TryNumber(JToken token, out double value)
        {
            value = 0;
            if (token == null || (token.Type != JTokenType.Float && token.Type != JTokenType.Integer)) return false;
            value = (double)token;
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
#endif
