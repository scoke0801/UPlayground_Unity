#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UPlayGround.AI.CombatDecision
{
    /// <summary>Unity 오브젝트를 보존하지 않는 요청별 진단. 미관측 시각은 -1이다.</summary>
    [Serializable]
    public sealed class LayaDecisionRecord
    {
        public string session, requestId, actor, target, state, localIntent, selectedIntent;
        public string status = "관측", reason = "", requestJson, responseJson, observationJson, candidatesJson;
        public string branch = "미실행", ability = "미확인", variant = "미확인", motionKey = "미확인";
        public double observed, sent = -1, received = -1, applied = -1, started = -1, ended = -1, inferenceMs = -1;
        public bool httpSuccess, validated, intentApplied, executed, preempted, bodyExpired;
        public int hits, generation, epoch, targetId, targetGeneration, sequence;
        public readonly List<string> events = new();
        public double RoundTripMs => received >= 0 && sent >= 0 ? (received - sent) * 1000 : -1;
        public double ApplyWaitMs => applied >= 0 && received >= 0 ? (applied - received) * 1000 : -1;
        public double TotalMs => applied >= 0 ? (applied - observed) * 1000 : -1;
        public double ActionMs => ended >= 0 && started >= 0 ? (ended - started) * 1000 : -1;
    }

    /// <summary>실험 소유의 유한 기록 저장소. 두 화면은 같은 목록을 읽는다.</summary>
    public sealed class LayaDiagnostics
    {
        public readonly List<LayaDecisionRecord> Records = new();
        public string Session = Guid.NewGuid().ToString("N"), Connection = "연결 미확인", Recording = "메모리 기록 중";
        public string TargetSummary = "훈련 상대 없음", Metadata = "", ExportPath = "";
        public int ProfileIndex, ModeIndex;
        public int Revision, Evicted, BodyEvicted;
        public int Capacity = 512, BodyBudget = 8388608;
        public bool IsExporting { get; private set; }

        public void Add(LayaDecisionRecord record)
        {
            if (Records.Count >= Capacity) { Records.RemoveAt(0); Evicted++; }
            Records.Add(record);
            Changed();
        }

        public void Changed()
        {
            long bytes = 0;
            for (int i = Records.Count - 1; i >= 0; i--)
            {
                var r = Records[i];
                bytes += Encoding.UTF8.GetByteCount(r.requestJson ?? "") + Encoding.UTF8.GetByteCount(r.responseJson ?? "")
                    + Encoding.UTF8.GetByteCount(r.observationJson ?? "") + Encoding.UTF8.GetByteCount(r.candidatesJson ?? "");
                if (bytes <= BodyBudget || r.bodyExpired) continue;
                r.requestJson = r.responseJson = r.observationJson = r.candidatesJson = null;
                r.bodyExpired = true;
                BodyEvicted++;
            }
            Revision++;
        }

        public void Event(LayaDecisionRecord record, string message, double now)
        {
            if (record == null) return;
            if (record.events.Count < 64) record.events.Add($"+{(now - record.observed) * 1000:0}ms  {LayaProtocol.Redact(message)}");
            Changed();
        }

        /// <summary>화면과 같은 DTO를 별도 스레드에서 저장하며, 기존 파일을 덮어쓰지 않는다.</summary>
        public async Task ExportAsync(string root)
        {
            if (IsExporting) return;
            IsExporting = true;
            try
            {
                var snapshot = new { schema_version = 1, session = Session, metadata = Metadata,
                    evicted = Evicted, body_evicted = BodyEvicted, records = Records.ToArray() };
                string json = LayaProtocol.Redact(JsonConvert.SerializeObject(snapshot, Formatting.Indented));
                var channels = new Dictionary<string, StringBuilder>();
                foreach (string channel in new[] { "observations", "requests", "responses", "decisions", "executions" })
                    channels[channel] = new StringBuilder();
                foreach (var record in snapshot.records)
                {
                    Append("observations", new { record.session, record.requestId, record.observed, record.observationJson });
                    Append("requests", new { record.session, record.requestId, record.sent, record.requestJson });
                    Append("responses", new { record.session, record.requestId, record.received, record.httpSuccess, record.responseJson });
                    Append("decisions", new { record.session, record.requestId, record.localIntent, record.selectedIntent,
                        record.validated, record.intentApplied, record.applied, record.status, record.reason });
                    Append("executions", new { record.session, record.requestId, record.executed, record.preempted,
                        record.branch, record.ability, record.variant, record.motionKey, record.started, record.ended, record.hits, record.events });
                }
                void Append(string channel, object entry) => channels[channel].AppendLine(LayaProtocol.Redact(JsonConvert.SerializeObject(entry)));
                string path = Path.Combine(root, Session, "session-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".json");
                await Task.Run(() => { Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                    using var writer = new StreamWriter(file, new UTF8Encoding(false)); writer.Write(json);
                    string channelDirectory = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
                    Directory.CreateDirectory(channelDirectory);
                    foreach (var channel in channels) File.WriteAllText(Path.Combine(channelDirectory, channel.Key + ".jsonl"), channel.Value.ToString(), new UTF8Encoding(false));
                });
                ExportPath = path;
                Recording = "저장 성공: " + path;
            }
            catch (Exception error) { Recording = "저장 실패: " + LayaProtocol.Redact(error.Message); }
            finally { IsExporting = false; Revision++; }
        }

        public static LayaDiagnostics Import(string path)
        {
            if (new FileInfo(path).Length > 32 * 1024 * 1024) throw new InvalidDataException("로그가 32MiB를 초과합니다.");
            var data = Newtonsoft.Json.Linq.JObject.Parse(LayaProtocol.Redact(File.ReadAllText(path)));
            if ((int?)data["schema_version"] != 1) throw new InvalidDataException("지원하지 않는 로그 버전입니다.");
            var store = new LayaDiagnostics { Session = (string)data["session"], Metadata = (string)data["metadata"],
                Connection = "저장 로그 · 실시간 연결 아님", Recording = "읽기 전용: " + path };
            foreach (var r in data["records"].ToObject<List<LayaDecisionRecord>>()) store.Add(r);
            return store;
        }
    }
}
#endif
