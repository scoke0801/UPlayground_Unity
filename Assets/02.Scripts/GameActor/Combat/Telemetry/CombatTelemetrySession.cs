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
        public sealed class AbilityFailureRecord
        {
            public string reason;
            public int count;
        }

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
            public int completedCount;
            public int cancelledCount;
            public int missedAttemptCount;
            public int activationFailureCount;
            public int resolvedCount;
            public int damageHitCount;
            public int counterHitCount;
            public int guardedCount;
            public int parriedCount;
            public int dodgedCount;
            public float totalDamage;
            public List<AbilityFailureRecord> activationFailures = new();
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

        private sealed class PendingPlayerAbility
        {
            public int EncounterKey;
            public int PlayerInstanceId;
            public string AbilityId;
            public string VariantId;
            public string MotionKey;
            public bool Resolved;
            public AbilityUsageRecord Usage;
        }

        private static readonly Dictionary<int, ActiveEncounter> _active = new();
        private static readonly Dictionary<string, PendingPlayerAbility> _pendingPlayerAbilities = new();
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
            _pendingPlayerAbilities.Clear();
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
                ActiveEncounter encounter = GetOrCreate(victimMonster, now);
                string side = result.Attacker is PlayerActor ? "player" : "ally";
                RecordAbilityResult(encounter, result, side);
                if (result.Attacker is PlayerActor playerAttacker)
                    MarkPlayerAbilityResolved(playerAttacker, victimMonster, result.Hit);
                encounter.LastEventTime = now;

                if (!result.DamageApplied || result.FinalDamage <= 0f)
                    return;

                encounter.Record.damageToMonster += result.FinalDamage;
                encounter.Record.hitsOnMonster++;
                if (result.Hit.IsCounterAttack)
                    encounter.Record.counterHitsOnMonster++;
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

        /// <summary>플레이어 Ability가 비용·쿨다운 커밋까지 성공해 실제 행동을 시작한 시점.</summary>
        public static void NotifyPlayerAbilityStarted(
            PlayerActor player,
            MonsterActor target,
            ulong executionHandle,
            string abilityId,
            string variantId,
            string motionKey)
        {
            if (!Enabled || player == null || target == null || executionHandle == 0)
                return;

            float now = Time.time;
            SweepTimeouts(now);
            ActiveEncounter encounter = GetOrCreate(target, now);
            AbilityUsageRecord usage = GetOrCreateAbility(
                encounter,
                "player",
                abilityId,
                variantId,
                motionKey,
                null,
                "Ability");
            usage.attemptCount++;
            encounter.LastEventTime = now;
            _pendingPlayerAbilities[BuildPlayerExecutionKey(player, executionHandle)] =
                new PendingPlayerAbility
                {
                    EncounterKey = target.GetInstanceID(),
                    PlayerInstanceId = player.GetInstanceID(),
                    AbilityId = abilityId,
                    VariantId = variantId,
                    MotionKey = motionKey,
                    Usage = usage,
                };
        }

        /// <summary>실제 활성화 요청이 Prepare/Commit 조건에서 거절된 원인을 인카운터에 누적한다.</summary>
        public static void NotifyPlayerAbilityActivationFailed(
            PlayerActor player,
            MonsterActor target,
            string abilityId,
            string reason)
        {
            if (!Enabled || player == null || target == null)
                return;

            float now = Time.time;
            SweepTimeouts(now);
            ActiveEncounter encounter = GetOrCreate(target, now);
            AbilityUsageRecord usage = GetOrCreateAbility(
                encounter,
                "player",
                abilityId,
                null,
                null,
                null,
                "Ability");
            usage.activationFailureCount++;
            AddFailureReason(usage, reason);
            encounter.LastEventTime = now;
        }

        /// <summary>플레이어 Ability 종료 시 완료/취소와 한 번도 판정되지 않은 미적중 시도를 확정한다.</summary>
        public static void NotifyPlayerAbilityEnded(
            PlayerActor player,
            ulong executionHandle,
            bool completed,
            string reason)
        {
            if (player == null || executionHandle == 0)
                return;

            string key = BuildPlayerExecutionKey(player, executionHandle);
            if (!_pendingPlayerAbilities.Remove(key, out PendingPlayerAbility pending))
                return;

            if (!_active.TryGetValue(pending.EncounterKey, out ActiveEncounter encounter))
                return;

            if (completed)
                pending.Usage.completedCount++;
            else
                pending.Usage.cancelledCount++;

            if (!pending.Resolved)
                pending.Usage.missedAttemptCount++;

            if (!completed)
                AddFailureReason(pending.Usage, reason);
            encounter.LastEventTime = Time.time;
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

            FinalizePendingForEncounter(key, encounter.Record.endReason);

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
            {
                if (string.IsNullOrWhiteSpace(existing.motionId)
                    && !string.IsNullOrWhiteSpace(motionId))
                    existing.motionId = motionId;
                if ((string.IsNullOrWhiteSpace(existing.attackKind)
                     || string.Equals(existing.attackKind, "Ability", StringComparison.Ordinal))
                    && !string.IsNullOrWhiteSpace(attackKind)
                    && !string.Equals(attackKind, "Ability", StringComparison.Ordinal))
                    existing.attackKind = attackKind;
                return existing;
            }

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

            // GAS 실행은 시작 시 MotionAsset/AttackKind를 아직 모를 수 있다. Ability 식별자가
            // 존재하면 그 계층만으로 키를 고정해 시작·적중·방어 결과를 같은 레코드에 합친다.
            if (!string.IsNullOrWhiteSpace(abilityId)
                || !string.IsNullOrWhiteSpace(motionKey))
            {
                return string.Join(
                    "|",
                    Normalize(side),
                    Normalize(abilityId),
                    Normalize(variantId),
                    Normalize(motionKey));
            }

            return string.Join(
                "|",
                Normalize(side),
                Normalize(abilityId),
                Normalize(variantId),
                Normalize(motionKey),
                Normalize(motionId),
                Normalize(attackKind));
        }

        private static void FinalizePendingForEncounter(int encounterKey, string endReason)
        {
            List<string> staleExecutions = null;
            foreach (KeyValuePair<string, PendingPlayerAbility> pair in _pendingPlayerAbilities)
            {
                PendingPlayerAbility pending = pair.Value;
                if (pending.EncounterKey != encounterKey)
                    continue;

                bool completedByKill = pending.Resolved
                                       && string.Equals(
                                           endReason,
                                           "kill",
                                           StringComparison.Ordinal);
                if (completedByKill)
                {
                    pending.Usage.completedCount++;
                }
                else
                {
                    pending.Usage.cancelledCount++;
                    if (!pending.Resolved)
                        pending.Usage.missedAttemptCount++;
                    AddFailureReason(
                        pending.Usage,
                        $"EncounterClosed:{(string.IsNullOrWhiteSpace(endReason) ? "Unknown" : endReason)}");
                }

                staleExecutions ??= new List<string>();
                staleExecutions.Add(pair.Key);
            }

            if (staleExecutions == null)
                return;
            for (int i = 0; i < staleExecutions.Count; i++)
                _pendingPlayerAbilities.Remove(staleExecutions[i]);
        }

        private static string BuildPlayerExecutionKey(PlayerActor player, ulong executionHandle)
            => $"{player.GetInstanceID()}:{executionHandle}";

        private static void MarkPlayerAbilityResolved(
            PlayerActor player,
            MonsterActor target,
            in HitContext hit)
        {
            int playerId = player.GetInstanceID();
            int encounterKey = target.GetInstanceID();
            PendingPlayerAbility fallback = null;
            int candidateCount = 0;

            foreach (PendingPlayerAbility pending in _pendingPlayerAbilities.Values)
            {
                if (pending.PlayerInstanceId != playerId
                    || pending.EncounterKey != encounterKey
                    || pending.Resolved)
                    continue;

                candidateCount++;
                fallback = pending;
                if (MatchesAbilityIdentity(pending, hit))
                {
                    pending.Resolved = true;
                    return;
                }
            }

            // 레거시 HitRequest에 Ability/Motion 식별자가 모두 없을 때만 단일 실행을 안전하게 귀속한다.
            if (candidateCount == 1
                && string.IsNullOrWhiteSpace(hit.AbilityId)
                && string.IsNullOrWhiteSpace(hit.MotionKey))
                fallback.Resolved = true;
        }

        private static bool MatchesAbilityIdentity(
            PendingPlayerAbility pending,
            in HitContext hit)
        {
            if (!string.IsNullOrWhiteSpace(pending.AbilityId)
                && !string.IsNullOrWhiteSpace(hit.AbilityId)
                && string.Equals(pending.AbilityId, hit.AbilityId, StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(pending.VariantId)
                       || string.IsNullOrWhiteSpace(hit.AbilityVariantId)
                       || string.Equals(
                           pending.VariantId,
                           hit.AbilityVariantId,
                           StringComparison.Ordinal);
            }

            return !string.IsNullOrWhiteSpace(pending.MotionKey)
                   && !string.IsNullOrWhiteSpace(hit.MotionKey)
                   && string.Equals(pending.MotionKey, hit.MotionKey, StringComparison.Ordinal);
        }

        private static void AddFailureReason(AbilityUsageRecord usage, string reason)
        {
            string normalized = string.IsNullOrWhiteSpace(reason) ? "Unknown" : reason.Trim();
            for (int i = 0; i < usage.activationFailures.Count; i++)
            {
                AbilityFailureRecord existing = usage.activationFailures[i];
                if (!string.Equals(existing.reason, normalized, StringComparison.Ordinal))
                    continue;
                existing.count++;
                return;
            }

            usage.activationFailures.Add(new AbilityFailureRecord
            {
                reason = normalized,
                count = 1,
            });
        }
    }
}
