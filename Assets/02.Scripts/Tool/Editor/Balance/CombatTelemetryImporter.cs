#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UPlayGround.Combat;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// CombatTelemetrySession이 저장한 세션 JSON들을 읽어 actorId별 실측 지표로 집계한다.
    /// Balance Designer의 "추정 vs 실측" 비교에 사용한다.
    /// </summary>
    public static class CombatTelemetryImporter
    {
        public sealed class ActorTelemetry
        {
            public string ActorId;
            public int EncounterCount;
            public int KillCount;
            public int PlayerDeathCount;
            public readonly List<float> KillDurations = new();
            public float MedianKillTime;
            public float AvgKillTime;
            public float AvgDamageToPlayer;
            public float AvgHitsOnPlayer;
            /// <summary>적 공격 판정 중 실제 피격 비율 (피격 / (피격+가드+패리+회피)).</summary>
            public float HitReceiveRate;
            public float GuardRate;
            public float ParryRate;
            public float DodgeRate;
        }

        private static readonly Dictionary<string, ActorTelemetry> _byActor = new();

        public static int SessionCount { get; private set; }
        public static int TotalEncounters { get; private set; }
        public static string LoadedAt { get; private set; }
        public static bool HasData => TotalEncounters > 0;
        public static string DirectoryPath => CombatTelemetrySession.OutputDirectory;

        public static bool TryGet(string actorId, out ActorTelemetry telemetry)
            => _byActor.TryGetValue(actorId ?? string.Empty, out telemetry);

        public static void Reload()
        {
            _byActor.Clear();
            SessionCount = 0;
            TotalEncounters = 0;
            LoadedAt = DateTime.Now.ToString("HH:mm:ss");

            if (!Directory.Exists(DirectoryPath))
                return;

            // 집계 중간값: actorId별 합산용 임시 버킷
            var damageSums = new Dictionary<string, float>();
            var hitSums = new Dictionary<string, float>();
            var hitEvents = new Dictionary<string, (float hit, float guard, float parry, float dodge)>();

            foreach (string file in Directory.GetFiles(DirectoryPath, "session_*.json"))
            {
                CombatTelemetrySession.SessionData session;
                try
                {
                    session = JsonUtility.FromJson<CombatTelemetrySession.SessionData>(File.ReadAllText(file));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CombatTelemetry] 세션 파일 파싱 실패 ({Path.GetFileName(file)}): {e.Message}");
                    continue;
                }

                if (session?.encounters == null || session.encounters.Count == 0)
                    continue;

                SessionCount++;
                for (int i = 0; i < session.encounters.Count; i++)
                {
                    CombatTelemetrySession.EncounterRecord record = session.encounters[i];
                    if (record == null || string.IsNullOrEmpty(record.actorId))
                        continue;

                    TotalEncounters++;
                    ActorTelemetry telemetry = GetOrCreate(record.actorId);
                    telemetry.EncounterCount++;
                    if (record.monsterKilled)
                    {
                        telemetry.KillCount++;
                        telemetry.KillDurations.Add(record.duration);
                    }
                    if (record.playerDied)
                        telemetry.PlayerDeathCount++;

                    Accumulate(damageSums, record.actorId, record.damageToPlayer);
                    Accumulate(hitSums, record.actorId, record.hitsOnPlayer);

                    (float hit, float guard, float parry, float dodge) events = hitEvents.TryGetValue(record.actorId, out var existing) ? existing : default;
                    events.hit += record.hitsOnPlayer;
                    events.guard += record.guardedCount;
                    events.parry += record.parriedCount;
                    events.dodge += record.dodgedCount;
                    hitEvents[record.actorId] = events;
                }
            }

            foreach (ActorTelemetry telemetry in _byActor.Values)
            {
                telemetry.KillDurations.Sort();
                if (telemetry.KillDurations.Count > 0)
                {
                    telemetry.MedianKillTime = telemetry.KillDurations[telemetry.KillDurations.Count / 2];
                    float sum = 0f;
                    for (int i = 0; i < telemetry.KillDurations.Count; i++)
                        sum += telemetry.KillDurations[i];
                    telemetry.AvgKillTime = sum / telemetry.KillDurations.Count;
                }

                if (telemetry.EncounterCount > 0)
                {
                    telemetry.AvgDamageToPlayer = damageSums.TryGetValue(telemetry.ActorId, out float damage) ? damage / telemetry.EncounterCount : 0f;
                    telemetry.AvgHitsOnPlayer = hitSums.TryGetValue(telemetry.ActorId, out float hits) ? hits / telemetry.EncounterCount : 0f;
                }

                if (hitEvents.TryGetValue(telemetry.ActorId, out var ev))
                {
                    float total = ev.hit + ev.guard + ev.parry + ev.dodge;
                    if (total > 0f)
                    {
                        telemetry.HitReceiveRate = ev.hit / total;
                        telemetry.GuardRate = ev.guard / total;
                        telemetry.ParryRate = ev.parry / total;
                        telemetry.DodgeRate = ev.dodge / total;
                    }
                }
            }
        }

        private static ActorTelemetry GetOrCreate(string actorId)
        {
            if (_byActor.TryGetValue(actorId, out ActorTelemetry existing))
                return existing;

            var telemetry = new ActorTelemetry { ActorId = actorId };
            _byActor[actorId] = telemetry;
            return telemetry;
        }

        private static void Accumulate(Dictionary<string, float> map, string key, float value)
        {
            map.TryGetValue(key, out float current);
            map[key] = current + value;
        }
    }
}
#endif
