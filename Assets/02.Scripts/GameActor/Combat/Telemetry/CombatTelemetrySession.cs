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
        public sealed class AbilityUsageRecord
        {
            public string sourceKey;
            public string side; // player / monster / ally
            public string abilityId;
            public string variantId;
            public string motionKey;
            public string motionId;
            public string attackKind;
            public int attemptCount;
            public int resolvedCount;
            public int damageHitCount;
            public int counterHitCount;
            public int guardedCount;
            public int parriedCount;
            public int dodgedCount;
            public float totalDamage;
        }

        [Serializable]
        public sealed class IntentUsageRecord
        {
            public string intent;
            public int evaluationCount;
            public int selectionCount;
        }

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
            public int counterHitsOnMonster;
            public int monsterAbilityStarts;
            public float longestMonsterActionGap;
            public List<AbilityUsageRecord> abilities = new();
            public List<IntentUsageRecord> intents = new();
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
            public float LastMonsterAbilityStartTime = -1f;
            public string LastIntent;
            public readonly Dictionary<string, AbilityUsageRecord> Abilities = new();
            public readonly Dictionary<string, IntentUsageRecord> Intents = new();
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
                if (result.Hit.IsCounterAttack)
                    encounter.Record.counterHitsOnMonster++;
                RecordAbilityResult(
                    encounter,
                    result,
                    result.Attacker is PlayerActor ? "player" : "ally");
                encounter.LastEventTime = now;
                return;
            }

            if (result.Attacker is MonsterActor attackerMonster && result.Victim is PlayerActor)
            {
                ActiveEncounter encounter = GetOrCreate(attackerMonster, now);
                encounter.LastEventTime = now;
                RecordAbilityResult(encounter, result, "monster");

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

        /// <summary>
        /// 몬스터가 Ability를 실제 커밋해 행동을 시작한 시점. 적중하지 않은 시도와 행동 사이 공백도
        /// 결과 기반 로그에서 유실되지 않도록 EnemyCombat에서 호출한다.
        /// </summary>
        public static void NotifyMonsterAbilityStarted(
            MonsterActor monster,
            string abilityId,
            string variantId,
            string motionKey,
            string motionId)
        {
            if (!Enabled || monster == null)
                return;

            float now = Time.time;
            SweepTimeouts(now);
            ActiveEncounter encounter = GetOrCreate(monster, now);
            encounter.Record.monsterAbilityStarts++;

            if (encounter.LastMonsterAbilityStartTime >= 0f)
            {
                float gap = Mathf.Max(0f, now - encounter.LastMonsterAbilityStartTime);
                encounter.Record.longestMonsterActionGap = Mathf.Max(
                    encounter.Record.longestMonsterActionGap,
                    gap);
            }

            encounter.LastMonsterAbilityStartTime = now;
            encounter.LastEventTime = now;
            AbilityUsageRecord usage = GetOrCreateAbility(
                encounter,
                "monster",
                abilityId,
                variantId,
                motionKey,
                motionId,
                "SkillAttack");
            usage.attemptCount++;
        }

        /// <summary>
        /// 이미 열린 인카운터의 Intent 평가 분포를 집계한다. 서비스 틱 자체는 인카운터 수명 연장
        /// 이벤트로 보지 않아, 전투가 끝난 AI가 텔레메트리 세션을 계속 붙잡지 않게 한다.
        /// </summary>
        public static void NotifyIntentEvaluated(MonsterActor monster, string intent)
        {
            if (!Enabled || monster == null || string.IsNullOrWhiteSpace(intent))
                return;

            if (!_active.TryGetValue(monster.GetInstanceID(), out ActiveEncounter encounter))
                return;

            string normalized = intent.Trim();
            if (!encounter.Intents.TryGetValue(normalized, out IntentUsageRecord usage))
            {
                usage = new IntentUsageRecord { intent = normalized };
                encounter.Intents.Add(normalized, usage);
            }

            usage.evaluationCount++;
            if (!string.Equals(encounter.LastIntent, normalized, StringComparison.Ordinal))
            {
                usage.selectionCount++;
                encounter.LastIntent = normalized;
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

            encounter.Record.abilities.Clear();
            encounter.Record.abilities.AddRange(encounter.Abilities.Values);
            encounter.Record.abilities.Sort((left, right) =>
                string.CompareOrdinal(left.sourceKey, right.sourceKey));
            encounter.Record.intents.Clear();
            encounter.Record.intents.AddRange(encounter.Intents.Values);
            encounter.Record.intents.Sort((left, right) =>
                string.CompareOrdinal(left.intent, right.intent));

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

        private static void RecordAbilityResult(
            ActiveEncounter encounter,
            CombatResult result,
            string side)
        {
            string motionId = result.Hit.MotionAsset != null
                ? result.Hit.MotionAsset.name
                : null;
            AbilityUsageRecord usage = GetOrCreateAbility(
                encounter,
                side,
                result.Hit.AbilityId,
                result.Hit.AbilityVariantId,
                result.Hit.MotionKey,
                motionId,
                result.Hit.AttackKind.ToString());

            usage.resolvedCount++;
            if (result.DamageApplied && result.FinalDamage > 0f)
            {
                usage.damageHitCount++;
                usage.totalDamage += result.FinalDamage;
            }
            if (result.Hit.IsCounterAttack)
                usage.counterHitCount++;

            switch (result.DefenseOutcome)
            {
                case DefenseOutcome.Guarded:
                case DefenseOutcome.GuardBreak:
                    usage.guardedCount++;
                    break;
                case DefenseOutcome.Parried:
                    usage.parriedCount++;
                    break;
                case DefenseOutcome.PerfectDodged:
                case DefenseOutcome.Invincible:
                    usage.dodgedCount++;
                    break;
            }
        }

        private static AbilityUsageRecord GetOrCreateAbility(
            ActiveEncounter encounter,
            string side,
            string abilityId,
            string variantId,
            string motionKey,
            string motionId,
            string attackKind)
        {
            string sourceKey = BuildAbilitySourceKey(
                side,
                abilityId,
                variantId,
                motionKey,
                motionId,
                attackKind);
            if (encounter.Abilities.TryGetValue(sourceKey, out AbilityUsageRecord existing))
                return existing;

            var usage = new AbilityUsageRecord
            {
                sourceKey = sourceKey,
                side = side,
                abilityId = abilityId,
                variantId = variantId,
                motionKey = motionKey,
                motionId = motionId,
                attackKind = attackKind,
            };
            encounter.Abilities.Add(sourceKey, usage);
            return usage;
        }

        private static string BuildAbilitySourceKey(
            string side,
            string abilityId,
            string variantId,
            string motionKey,
            string motionId,
            string attackKind)
        {
            static string Normalize(string value) =>
                string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

            return string.Join(
                "|",
                Normalize(side),
                Normalize(abilityId),
                Normalize(variantId),
                Normalize(motionKey),
                Normalize(motionId),
                Normalize(attackKind));
        }
    }
}
