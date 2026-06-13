using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 플레이 세션 동안 전투 인카운터를 몬스터 단위로 집계해 로컬 JSON으로 저장하는 밸런스 텔레메트리.
    /// <see cref="CombatLogRecorder.ResultObserved"/>를 구독해 피해/방어 결과를 집계하고,
    /// 사망 훅(NotifyMonsterDeath/NotifyPlayerDeath)으로 인카운터를 종결한다.
    /// 저장 위치: persistentDataPath/CombatTelemetry/session_*.json — Balance Designer가 실측 비교에 사용한다.
    /// 프레임 단위 기록이 아닌 인카운터 합산만 저장하므로 런타임 비용은 무시 가능한 수준이다.
    /// </summary>
    public static class CombatTelemetrySession
    {
        /// <summary>집계 on/off. 에디터/개발 빌드에서 기본 on.</summary>
        public static bool Enabled = true;

        /// <summary>이 시간(초) 동안 전투 이벤트가 없으면 인카운터를 이탈로 종결한다.</summary>
        public const float EncounterTimeoutSeconds = 25f;

        public static string OutputDirectory
            => Path.Combine(Application.persistentDataPath, "CombatTelemetry");

        [Serializable]
        public sealed class EncounterRecord
        {
            public string actorId;
            public int monsterLevel;
            public string grade;
            public float startTime;
            public float duration;
            public bool monsterKilled;
            public bool playerDied;
            public string endReason; // kill / player_death / timeout / session_end
            public float damageToMonster;
            public float damageToPlayer;
            public int hitsOnMonster;
            public int hitsOnPlayer;
            public int guardedCount;
            public int parriedCount;
            public int dodgedCount;
            public float maxSingleHitOnPlayer;
        }

        [Serializable]
        public sealed class SessionData
        {
            public string startedAt;
            public List<EncounterRecord> encounters = new();
        }

        private sealed class ActiveEncounter
        {
            public EncounterRecord Record;
            public float LastEventTime;
        }

        private static readonly Dictionary<int, ActiveEncounter> _active = new();
        private static SessionData _session;
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            // 도메인 리로드 비활성 환경에서도 중복 구독되지 않도록 항상 해제 후 재구독한다.
            CombatLogRecorder.ResultObserved -= HandleResult;
            CombatLogRecorder.ResultObserved += HandleResult;
            if (!_initialized)
            {
                Application.quitting += Flush;
                _initialized = true;
            }

            _active.Clear();
            _session = null;
            Enabled = Application.isEditor || Debug.isDebugBuild;
        }

        private static void HandleResult(CombatResult result)
        {
            if (!Enabled)
                return;

            float now = Time.time;
            SweepTimeouts(now);

            if (result.Victim is MonsterActor victimMonster)
            {
                if (!result.DamageApplied || result.FinalDamage <= 0f)
                    return;

                ActiveEncounter encounter = GetOrCreate(victimMonster, now);
                encounter.Record.damageToMonster += result.FinalDamage;
                encounter.Record.hitsOnMonster++;
                encounter.LastEventTime = now;
                return;
            }

            if (result.Attacker is MonsterActor attackerMonster && result.Victim is PlayerActor)
            {
                ActiveEncounter encounter = GetOrCreate(attackerMonster, now);
                encounter.LastEventTime = now;

                if (result.DamageApplied && result.FinalDamage > 0f)
                {
                    encounter.Record.damageToPlayer += result.FinalDamage;
                    encounter.Record.hitsOnPlayer++;
                    if (result.FinalDamage > encounter.Record.maxSingleHitOnPlayer)
                        encounter.Record.maxSingleHitOnPlayer = result.FinalDamage;
                    return;
                }

                switch (result.DefenseOutcome)
                {
                    case DefenseOutcome.Guarded:
                    case DefenseOutcome.GuardBreak:
                        encounter.Record.guardedCount++;
                        break;
                    case DefenseOutcome.Parried:
                        encounter.Record.parriedCount++;
                        break;
                    case DefenseOutcome.PerfectDodged:
                    case DefenseOutcome.Invincible:
                        encounter.Record.dodgedCount++;
                        break;
                }
            }
        }

        /// <summary>몬스터 사망 시 MonsterActor.OnDeath에서 호출 — 인카운터를 처치로 종결한다.</summary>
        public static void NotifyMonsterDeath(MonsterActor monster)
        {
            if (!Enabled || monster == null)
                return;

            int key = monster.GetInstanceID();
            if (!_active.TryGetValue(key, out ActiveEncounter encounter))
                return;

            encounter.Record.monsterKilled = true;
            encounter.Record.endReason = "kill";
            CloseEncounter(key, Time.time);
        }

        /// <summary>플레이어 사망 시 PlayerActor.OnDeath에서 호출 — 진행 중인 모든 인카운터를 종결한다.</summary>
        public static void NotifyPlayerDeath(PlayerActor player)
        {
            if (!Enabled)
                return;

            float now = Time.time;
            var keys = new List<int>(_active.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                ActiveEncounter encounter = _active[keys[i]];
                encounter.Record.playerDied = true;
                encounter.Record.endReason = "player_death";
                CloseEncounter(keys[i], now);
            }
        }

        /// <summary>세션을 파일로 저장하고 집계 상태를 초기화한다. Application.quitting에서 자동 호출.</summary>
        public static void Flush()
        {
            float now = Time.time;
            if (_active.Count > 0)
            {
                var keys = new List<int>(_active.Keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    _active[keys[i]].Record.endReason = "session_end";
                    CloseEncounter(keys[i], _active[keys[i]].LastEventTime > 0f ? _active[keys[i]].LastEventTime : now);
                }
            }

            if (_session == null || _session.encounters.Count == 0)
                return;

            try
            {
                Directory.CreateDirectory(OutputDirectory);
                string path = Path.Combine(OutputDirectory, $"session_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.WriteAllText(path, JsonUtility.ToJson(_session, true));
                Debug.Log($"[CombatTelemetry] 세션 저장 — 인카운터 {_session.encounters.Count}건\n{path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CombatTelemetry] 세션 저장 실패: {e.Message}");
            }
            finally
            {
                _session = null;
            }
        }

        private static ActiveEncounter GetOrCreate(MonsterActor monster, float now)
        {
            int key = monster.GetInstanceID();
            if (_active.TryGetValue(key, out ActiveEncounter existing))
                return existing;

            _session ??= new SessionData { startedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };

            string actorId = monster.Definition != null && !string.IsNullOrEmpty(monster.Definition.actorId)
                ? monster.Definition.actorId
                : (!string.IsNullOrEmpty(monster.ActorId) ? monster.ActorId : monster.gameObject.name);

            var encounter = new ActiveEncounter
            {
                Record = new EncounterRecord
                {
                    actorId = actorId,
                    monsterLevel = monster.Level,
                    grade = monster.Grade.ToString(),
                    startTime = now,
                },
                LastEventTime = now,
            };
            _active[key] = encounter;
            return encounter;
        }

        private static void CloseEncounter(int key, float endTime)
        {
            if (!_active.TryGetValue(key, out ActiveEncounter encounter))
                return;

            encounter.Record.duration = Mathf.Max(0f, endTime - encounter.Record.startTime);
            if (string.IsNullOrEmpty(encounter.Record.endReason))
                encounter.Record.endReason = "timeout";

            _session?.encounters.Add(encounter.Record);
            _active.Remove(key);
        }

        private static void SweepTimeouts(float now)
        {
            if (_active.Count == 0)
                return;

            List<int> stale = null;
            foreach (KeyValuePair<int, ActiveEncounter> pair in _active)
            {
                if (now - pair.Value.LastEventTime >= EncounterTimeoutSeconds)
                {
                    stale ??= new List<int>();
                    stale.Add(pair.Key);
                }
            }

            if (stale == null)
                return;

            for (int i = 0; i < stale.Count; i++)
            {
                ActiveEncounter encounter = _active[stale[i]];
                encounter.Record.endReason = "timeout";
                CloseEncounter(stale[i], encounter.LastEventTime);
            }
        }
    }
}
