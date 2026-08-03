using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Cycle;
using UPlayGround.Components;
using UPlayGround.Manager;

namespace UPlayGround.Cycle
{
    [Serializable]
    public sealed class CycleEncounterRecord
    {
        public string spawnId;
        public string actorId;
        public bool isCentral;
        public float discoveredAt = -1f;
        public float defeatedAt = -1f;
        public bool playerTookDamage;
        public bool finishedBySpecialBreak;
        public bool noHit;
    }

    [Serializable]
    public sealed class CharacterCycleCombatRecord
    {
        public string characterType;
        public float activeSeconds;
        public float damageDealt;
        public float breakDamage;
        public int swaps;
    }

    [Serializable]
    public sealed class AssistUseRecord
    {
        public string assistId;
        public float usedAt;
        public float cooldownSeconds;
    }

    [Serializable]
    public sealed class AssistRecruitmentRecord
    {
        public string assistId;
        public float occurredAt;
        public bool success;
        public string trigger;
        public int defeatCountBefore;
        public int defeatCountAfter;
        public int requiredDefeatCount;
        public string rosterStatus;
    }

    [Serializable]
    public sealed class RemainsRecord
    {
        public string eventType;
        public float occurredAt;
        public long lostExp;
        public int materialCount;
        public Vector3 position;
    }

    [Serializable]
    public sealed class MarkerSelectionRecord
    {
        public string spawnId;
        public float selectedAt;
        public Vector3 playerPosition;
        public float distance;
    }

    [Serializable]
    public sealed class CycleSettlementTelemetryRecord
    {
        public string settlementId;
        public float occurredAt;
        public int committedMaterialCount;
        public bool discardedRemains;
    }

    [Serializable]
    public sealed class CycleTelemetryRecord
    {
        public string sessionId;
        public int seed;
        public int cycleIndex;
        public float totalSeconds;
        public string playerSpawnId;
        public string equippedAssistId;
        public List<string> startingParty = new();
        public List<CycleEncounterRecord> encounters = new();
        public List<CharacterCycleCombatRecord> characters = new();
        public List<AssistUseRecord> assistUses = new();
        public List<AssistRecruitmentRecord> recruitments = new();
        public List<RemainsRecord> remainsEvents = new();
        public List<MarkerSelectionRecord> markerSelections = new();
        public List<CycleSettlementTelemetryRecord> settlements = new();
        public int projectileSpawnCount;
        public int projectileHitCount;
        public int projectileExpireCount;
        public int projectilePeakActive;
        public float projectileFlightSeconds;
    }

    public sealed class CycleTelemetrySession : BaseManager<CycleTelemetrySession>, IManager, IUpdatableManager
    {
        private CycleTelemetryRecord _record;
        private PlayerCombat _subscribedCombat;
        public CycleTelemetryRecord Current => _record;

        public void Init() { }

        public void AfterInit()
        {
            CycleRunManager run = CycleRunManager.Instance;
            run.OnCycleStarted += StartSession;
            run.OnPhaseChanged += OnPhaseChanged;
            run.OnBossDiscovered += OnDiscovered;
            run.OnBossDefeated += OnDefeated;
            run.OnSettlementCommitted += OnSettlement;
            run.OnCycleCompleted += CompleteSession;

            BossAssistManager.Instance.OnAssistStarted += OnAssistUsed;
            BossAssistManager.Instance.OnRecruitmentResolved += OnRecruitment;
            PartyManager.Instance.OnSwapCompleted += OnSwapCompleted;
            CycleRemainsManager.Instance.OnRemainsCreated += OnRemainsCreated;
            CycleRemainsManager.Instance.OnRemainsRecovered += OnRemainsRecovered;
            CycleRemainsManager.Instance.OnRemainsDiscarded += OnRemainsDiscarded;
            EnsureSessionForActiveRun();
        }

        public void Dispose()
        {
            UnbindCombat();
            if (CycleRunManager.Instance != null)
            {
                CycleRunManager.Instance.OnCycleStarted -= StartSession;
                CycleRunManager.Instance.OnPhaseChanged -= OnPhaseChanged;
                CycleRunManager.Instance.OnBossDiscovered -= OnDiscovered;
                CycleRunManager.Instance.OnBossDefeated -= OnDefeated;
                CycleRunManager.Instance.OnSettlementCommitted -= OnSettlement;
                CycleRunManager.Instance.OnCycleCompleted -= CompleteSession;
            }
            if (BossAssistManager.Instance != null)
            {
                BossAssistManager.Instance.OnAssistStarted -= OnAssistUsed;
                BossAssistManager.Instance.OnRecruitmentResolved -= OnRecruitment;
            }
            if (PartyManager.Instance != null) PartyManager.Instance.OnSwapCompleted -= OnSwapCompleted;
            if (CycleRemainsManager.Instance != null)
            {
                CycleRemainsManager.Instance.OnRemainsCreated -= OnRemainsCreated;
                CycleRemainsManager.Instance.OnRemainsRecovered -= OnRemainsRecovered;
                CycleRemainsManager.Instance.OnRemainsDiscarded -= OnRemainsDiscarded;
            }
        }

        public void OnUpdate()
        {
            if (_record == null) return;
            float delta = Time.unscaledDeltaTime;
            _record.totalSeconds += delta;
            string activeType = PartyManager.Instance?.ActiveCharacterType.ToString();
            if (!string.IsNullOrEmpty(activeType) && activeType != "None")
                FindOrCreateCharacter(activeType).activeSeconds += delta;
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType) => EnsureSessionForActiveRun();

        /// <summary>미니맵/나침반 선택 UI가 `?` 목적지를 선택했을 때 호출한다.</summary>
        public void RecordMarkerSelected(string spawnId, Vector3 worldPosition)
        {
            if (_record == null || string.IsNullOrWhiteSpace(spawnId)) return;
            Vector3 playerPosition = GameObjectManager.Instance?.Player != null
                ? GameObjectManager.Instance.Player.transform.position
                : Vector3.zero;
            _record.markerSelections.Add(new MarkerSelectionRecord
            {
                spawnId = spawnId,
                selectedAt = _record.totalSeconds,
                playerPosition = playerPosition,
                distance = Vector3.Distance(playerPosition, worldPosition),
            });
        }

        public void RecordProjectileSpawn(int activeCount)
        {
            if (_record == null) return;
            _record.projectileSpawnCount++;
            _record.projectilePeakActive = Mathf.Max(_record.projectilePeakActive, activeCount);
        }

        public void RecordProjectileHit()
        {
            if (_record != null)
                _record.projectileHitCount++;
        }

        public void RecordProjectileExpire(float flightSeconds)
        {
            if (_record == null) return;
            _record.projectileExpireCount++;
            _record.projectileFlightSeconds += Mathf.Max(0f, flightSeconds);
        }

        private void StartSession(int _) => CreateSession(CycleRunManager.Instance.Current);

        private void OnPhaseChanged(CycleRunState run)
        {
            if (run != null && run.phase is CycleRunPhase.Active or CycleRunPhase.BossDefeated)
                EnsureSessionForActiveRun();
        }

        private void EnsureSessionForActiveRun()
        {
            CycleRunState run = CycleRunManager.Instance?.Current;
            if (run == null || run.phase is not (CycleRunPhase.Active or CycleRunPhase.BossDefeated)) return;
            if (_record != null && _record.seed == run.seed && _record.cycleIndex == run.cycleIndex) return;
            CreateSession(run);
        }

        private void CreateSession(CycleRunState run)
        {
            _record = new CycleTelemetryRecord
            {
                sessionId = Guid.NewGuid().ToString("N"),
                seed = run.seed,
                cycleIndex = run.cycleIndex,
                totalSeconds = run.elapsedSeconds,
                playerSpawnId = CycleRunManager.Instance.CurrentLayout?.playerSpawnId,
                equippedAssistId = BossAssistManager.Instance?.Roster.EquippedAssistId,
            };
            if (PartyManager.Instance?.BattleOrder != null)
                foreach (var type in PartyManager.Instance.BattleOrder) _record.startingParty.Add(type.ToString());

            CycleLayoutState layout = CycleRunManager.Instance.CurrentLayout;
            if (layout != null)
            {
                if (layout.outerBosses != null)
                    foreach (CycleBossPlacement boss in layout.outerBosses) AddRestoredEncounter(boss);
                AddRestoredEncounter(layout.centralBoss);
            }
            BindCombat();
        }

        private void AddRestoredEncounter(CycleBossPlacement boss)
        {
            if (boss == null || !boss.discovered) return;
            _record.encounters.Add(new CycleEncounterRecord
            {
                spawnId = boss.spawnId,
                actorId = boss.actorId,
                isCentral = boss.isCentral,
                playerTookDamage = boss.playerTookDamageAfterDiscovery,
                finishedBySpecialBreak = boss.finishedBySpecialBreakAttack,
                noHit = boss.defeated && boss.defeatedNoHit,
            });
        }

        private void OnDiscovered(CycleBossPlacement boss)
        {
            if (_record == null || boss == null) return;
            if (!_record.markerSelections.Exists(value => value.spawnId == boss.spawnId) &&
                CycleBossMarkerRegistry.TryGet(boss.spawnId, out CycleBossMarkerData marker))
                RecordMarkerSelected(boss.spawnId, marker.worldPosition);
            CycleEncounterRecord existing = _record.encounters.Find(value => value.spawnId == boss.spawnId);
            if (existing != null) { if (existing.discoveredAt < 0f) existing.discoveredAt = _record.totalSeconds; return; }
            _record.encounters.Add(new CycleEncounterRecord
            {
                spawnId = boss.spawnId,
                actorId = boss.actorId,
                isCentral = boss.isCentral,
                discoveredAt = _record.totalSeconds,
            });
        }

        private void OnDefeated(CycleBossPlacement boss)
        {
            if (_record == null || boss == null) return;
            CycleEncounterRecord value = _record.encounters.Find(entry => entry.spawnId == boss.spawnId);
            if (value == null)
            {
                OnDiscovered(boss);
                value = _record.encounters.Find(entry => entry.spawnId == boss.spawnId);
            }
            if (value == null) return;
            value.defeatedAt = _record.totalSeconds;
            value.playerTookDamage = boss.playerTookDamageAfterDiscovery;
            value.finishedBySpecialBreak = boss.finishedBySpecialBreakAttack;
            value.noHit = boss.defeatedNoHit;
        }

        private void OnAssistUsed(string id)
        {
            if (_record == null) return;
            _record.assistUses.Add(new AssistUseRecord
            {
                assistId = id,
                usedAt = _record.totalSeconds,
                cooldownSeconds = BossAssistManager.Instance.SampleCooldown().duration,
            });
        }

        private void OnRecruitment(BossRecruitmentResult result)
        {
            if (_record == null) return;
            _record.recruitments.Add(new AssistRecruitmentRecord
            {
                assistId = result.assistId,
                occurredAt = _record.totalSeconds,
                success = result.success,
                trigger = result.trigger.ToString(),
                defeatCountBefore = result.defeatCountBefore,
                defeatCountAfter = result.defeatCountAfter,
                requiredDefeatCount = result.requiredDefeatCount,
                rosterStatus = result.rosterResult?.status.ToString(),
            });
        }

        private void OnSwapCompleted(PlayerActor _)
        {
            if (_record == null) return;
            string activeType = PartyManager.Instance.ActiveCharacterType.ToString();
            FindOrCreateCharacter(activeType).swaps++;
            BindCombat();
        }

        private void BindCombat()
        {
            PlayerCombat combat = PartyManager.Instance?.ActiveCharacter?.GetCombat();
            if (_subscribedCombat == combat) return;
            UnbindCombat();
            _subscribedCombat = combat;
            if (_subscribedCombat != null) _subscribedCombat.OnAttackHit += OnAttackHit;
        }

        private void UnbindCombat()
        {
            if (_subscribedCombat != null) _subscribedCombat.OnAttackHit -= OnAttackHit;
            _subscribedCombat = null;
        }

        private void OnAttackHit(AttackData attack)
        {
            if (_record == null || attack == null) return;
            string activeType = PartyManager.Instance.ActiveCharacterType.ToString();
            CharacterCycleCombatRecord character = FindOrCreateCharacter(activeType);
            character.damageDealt += Mathf.Max(0f, attack.damage);
            character.breakDamage += Mathf.Max(0f, attack.breakDamage);
        }

        private CharacterCycleCombatRecord FindOrCreateCharacter(string type)
        {
            CharacterCycleCombatRecord record = _record.characters.Find(value => value.characterType == type);
            if (record != null) return record;
            record = new CharacterCycleCombatRecord { characterType = type };
            _record.characters.Add(record);
            return record;
        }

        private void OnRemainsCreated(RemainsState state) => AddRemains("created", state);
        private void OnRemainsRecovered(RemainsState state) => AddRemains("recovered", state);
        private void OnRemainsDiscarded(RemainsState state) => AddRemains("discarded", state);

        private void AddRemains(string type, RemainsState state)
        {
            if (_record == null || state == null) return;
            long exp = 0;
            foreach (LostExpEntry entry in state.lostExp) if (entry != null) exp += entry.amount;
            int count = 0;
            foreach (CycleItemStack item in state.materials) if (item != null) count += item.count;
            _record.remainsEvents.Add(new RemainsRecord
            {
                eventType = type,
                occurredAt = _record.totalSeconds,
                lostExp = exp,
                materialCount = count,
                position = state.position.ToVector3(),
            });
        }

        private void OnSettlement(CycleSettlementPlan plan)
        {
            if (_record == null || plan == null) return;
            int materialCount = 0;
            foreach (CycleItemStack item in plan.materialRewards) if (item != null) materialCount += item.count;
            _record.settlements.Add(new CycleSettlementTelemetryRecord
            {
                settlementId = plan.settlementId,
                occurredAt = _record.totalSeconds,
                committedMaterialCount = materialCount,
                discardedRemains = plan.discardRemains,
            });
        }

        private void CompleteSession(int _)
        {
            if (_record == null) return;
            WriteLocal(_record);
            _record = null;
            UnbindCombat();
        }

        private static void WriteLocal(CycleTelemetryRecord record)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "cycle_telemetry");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, $"cycle_{record.sessionId}.json"), JsonUtility.ToJson(record, true));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
#endif
        }
    }
}
