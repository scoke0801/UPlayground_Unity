using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Contracts;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Codex;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Save;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 새 게임 범위의 몬스터 종별 발견/처치 기록을 저장하고 상대 종 전용 보정을 제공한다.
    /// </summary>
    public sealed class MonsterCodexManager : BaseManager<MonsterCodexManager>,
        IManager,
        IAsyncInitializableManager,
        ISaveable,
        IMonsterCodexService
    {
        private const string DatabaseKey = "MonsterCodexDatabase";

        private readonly Dictionary<string, MonsterCodexEntrySave> _records = new();
        private readonly List<MonsterCodexEntryView> _viewBuffer = new();
        private MonsterCodexDatabaseSO _database;

        public bool IsDatabaseLoaded => _database != null;

        public void Init()
        {
            _records.Clear();
            SaveManager.Instance.RegisterSaveable(this);
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                _database = await AssetManager.Instance.LoadGlobalAsync<MonsterCodexDatabaseSO>(
                    DatabaseKey,
                    nameof(MonsterCodexManager),
                    cancellationToken);

                if (_database != null)
                    _database.Initialize();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // 도감 데이터 누락이 게임 전체 부팅을 막지는 않는다. 보정은 기본 배율 1로 동작한다.
                _database = null;
                Debug.LogWarning(
                    $"[MonsterCodexManager] Database 로드 실패. 도감 보정을 비활성화합니다: " +
                    exception.Message);
            }
        }

        public void AfterInit()
        {
            if (_database == null)
                BuildRuntimeFallbackDatabase();
        }
        public void Dispose()
        {
            _records.Clear();
            _viewBuffer.Clear();
            _database = null;
        }
        public void OnUpdate() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType) { }

        public void RecordKill(string actorId, CombatElement element)
        {
            if (string.IsNullOrWhiteSpace(actorId))
                return;
            if (!TryGetDefinition(actorId, out _))
                return;

            MonsterCodexEntrySave record = GetOrCreateRecord(actorId);
            if (record.killCount < long.MaxValue)
                record.killCount++;
            record.discovered = true;

            ActorDefinitionSO definition =
                ActorSpawnManager.Instance?.Database?.GetDefinition(actorId);
            if (definition != null &&
                definition.elementAssignmentMode == CombatElementAssignmentMode.RandomPerNewGame)
            {
                record.discoveredElement = (int)element;
            }
        }

        public float GetRecordRatio(string actorId)
        {
            if (!TryGetDefinition(actorId, out MonsterCodexEntrySO entry))
                return 0f;

            return entry.GetRecordRatio(GetKillCount(actorId));
        }

        public bool IsDiscovered(string actorId) =>
            !string.IsNullOrEmpty(actorId) &&
            _records.TryGetValue(actorId, out MonsterCodexEntrySave record) &&
            record.discovered;

        public CombatElement GetDiscoveredElement(string actorId)
        {
            if (!_records.TryGetValue(actorId, out MonsterCodexEntrySave record) ||
                !record.discovered)
            {
                return CombatElement.None;
            }

            return Enum.IsDefined(typeof(CombatElement), record.discoveredElement)
                ? (CombatElement)record.discoveredElement
                : CombatElement.None;
        }

        public float GetExpMultiplier(string actorId)
        {
            if (!TryGetDefinition(actorId, out MonsterCodexEntrySO entry))
                return 1f;

            return MonsterCodexCalculator.GetExpMultiplier(
                entry.GetRecordRatio(GetKillCount(actorId)),
                entry.bonus);
        }

        public float GetDamageDealtMultiplier(string actorId)
        {
            if (!TryGetDefinition(actorId, out MonsterCodexEntrySO entry))
                return 1f;

            return MonsterCodexCalculator.GetDamageDealtMultiplier(
                entry.GetRecordRatio(GetKillCount(actorId)),
                entry.bonus);
        }

        public float GetDamageTakenMultiplier(string actorId)
        {
            if (!TryGetDefinition(actorId, out MonsterCodexEntrySO entry))
                return 1f;

            return MonsterCodexCalculator.GetDamageTakenMultiplier(
                entry.GetRecordRatio(GetKillCount(actorId)),
                entry.bonus);
        }

        public IReadOnlyList<MonsterCodexEntryView> GetAllEntries()
        {
            _viewBuffer.Clear();
            if (_database == null)
                return _viewBuffer;

            ActorDatabase actorDatabase = ActorSpawnManager.Instance?.Database;
            foreach (MonsterCodexEntrySO entry in _database.Entries)
            {
                if (entry == null || !entry.includeInCodex)
                    continue;

                ActorDefinitionSO definition = actorDatabase?.GetDefinition(entry.actorId);
                if (definition == null)
                    continue;

                bool discovered = IsDiscovered(entry.actorId);
                CombatElement element =
                    definition.elementAssignmentMode == CombatElementAssignmentMode.RandomPerNewGame
                        ? GetDiscoveredElement(entry.actorId)
                        : definition.combatElement;
                long killCount = GetKillCount(entry.actorId);

                _viewBuffer.Add(new MonsterCodexEntryView
                {
                    actorId = entry.actorId,
                    displayName = string.IsNullOrWhiteSpace(entry.displayNameOverride)
                        ? definition.displayName
                        : entry.displayNameOverride,
                    description = string.IsNullOrWhiteSpace(entry.descriptionOverride)
                        ? definition.description
                        : entry.descriptionOverride,
                    portrait = entry.portrait,
                    grade = definition.EffectiveGrade,
                    elementAssignmentMode = definition.elementAssignmentMode,
                    element = element,
                    killCount = killCount,
                    fullRecordKillCount = Mathf.Max(1, entry.fullRecordKillCount),
                    recordRatio = entry.GetRecordRatio(killCount),
                    discovered = discovered,
                    bonus = entry.bonus,
                });
            }

            return _viewBuffer;
        }

        public void ExportSaveData(GameSaveData saveData)
        {
            if (saveData == null)
                return;

            saveData.monsterCodex = new List<MonsterCodexEntrySave>(_records.Count);
            foreach (MonsterCodexEntrySave record in _records.Values)
            {
                saveData.monsterCodex.Add(new MonsterCodexEntrySave
                {
                    actorId = record.actorId,
                    killCount = Math.Max(0L, record.killCount),
                    discovered = record.discovered,
                    discoveredElement = record.discoveredElement,
                });
            }

            saveData.monsterCodex.Sort(
                (left, right) => string.CompareOrdinal(left.actorId, right.actorId));
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _records.Clear();
            List<MonsterCodexEntrySave> records = saveData?.monsterCodex;
            if (records == null)
                return;

            foreach (MonsterCodexEntrySave source in records)
            {
                if (source == null || string.IsNullOrWhiteSpace(source.actorId))
                    continue;

                _records[source.actorId] = new MonsterCodexEntrySave
                {
                    actorId = source.actorId,
                    killCount = Math.Max(0L, source.killCount),
                    discovered = source.discovered || source.killCount > 0,
                    discoveredElement = source.discoveredElement,
                };
            }
        }

        public void ResetForNewGame()
        {
            _records.Clear();
            _viewBuffer.Clear();
        }

        private bool TryGetDefinition(string actorId, out MonsterCodexEntrySO entry)
        {
            entry = null;
            return _database != null &&
                   _database.TryGetEntry(actorId, out entry) &&
                   entry != null &&
                   entry.includeInCodex;
        }

        private long GetKillCount(string actorId) =>
            !string.IsNullOrEmpty(actorId) &&
            _records.TryGetValue(actorId, out MonsterCodexEntrySave record)
                ? Math.Max(0L, record.killCount)
                : 0L;

        private MonsterCodexEntrySave GetOrCreateRecord(string actorId)
        {
            if (_records.TryGetValue(actorId, out MonsterCodexEntrySave record))
                return record;

            record = new MonsterCodexEntrySave { actorId = actorId };
            _records.Add(actorId, record);
            return record;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>치트: 도감 대상을 즉시 100% 기록(발견) 상태로 등록한다. 성공하면 true.</summary>
        public bool CheatRegisterFull(string actorId)
        {
            if (!TryGetDefinition(actorId, out MonsterCodexEntrySO entry))
                return false;

            MonsterCodexEntrySave record = GetOrCreateRecord(actorId);
            record.killCount = Math.Max(1, entry.fullRecordKillCount);
            record.discovered = true;

            // 랜덤 속성 몬스터는 현재 새 게임 시드로 확정 속성을 발견 처리한다.
            ActorDefinitionSO definition =
                ActorSpawnManager.Instance?.Database?.GetDefinition(actorId);
            if (definition != null &&
                definition.elementAssignmentMode == CombatElementAssignmentMode.RandomPerNewGame)
            {
                record.discoveredElement =
                    (int)definition.ResolveCombatElement(Svc.ElementRandom?.NewGameElementSeed ?? 0);
            }

            return true;
        }

        /// <summary>치트: 도감 대상의 기록을 완전히 제거해 미발견 상태로 되돌린다. 실제 제거 시 true.</summary>
        public bool CheatRemove(string actorId) =>
            !string.IsNullOrWhiteSpace(actorId) && _records.Remove(actorId);
#endif

        private void BuildRuntimeFallbackDatabase()
        {
            ActorDatabase actors = ActorSpawnManager.Instance?.Database;
            if (actors == null)
                return;

            _database = ScriptableObject.CreateInstance<MonsterCodexDatabaseSO>();
            _database.name = "MonsterCodexDatabase_RuntimeFallback";
            foreach (ActorDefinitionSO definition in actors.All)
            {
                if (definition == null ||
                    (definition.actorType & ActorType.Monster) == 0 ||
                    string.IsNullOrWhiteSpace(definition.actorId))
                {
                    continue;
                }

                MonsterCodexEntrySO entry =
                    ScriptableObject.CreateInstance<MonsterCodexEntrySO>();
                entry.name = $"MonsterCodexEntry_{definition.actorId}_RuntimeFallback";
                entry.actorId = definition.actorId;
                entry.fullRecordKillCount = 10;
                entry.bonus = new MonsterCodexBonus
                {
                    maxExpBonus = 0.2f,
                    maxDamageDealtBonus = 0.1f,
                    maxDamageTakenReduce = 0.1f,
                };
                _database.AddEntry(entry);
            }

            _database.Initialize();
            Debug.LogWarning(
                "[MonsterCodexManager] 저장된 도감 Database가 없어 런타임 초안값을 사용합니다. " +
                "에디터 메뉴에서 도감 데이터를 생성하세요.");
        }
    }
}
