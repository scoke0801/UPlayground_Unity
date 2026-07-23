using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    public interface IBossAssistEffectExecutor
    {
        void Execute(BossAssistDefinitionSO definition, PlayerActor player, MonsterActor target, Action completed);
        void Cancel();
    }

    public sealed class BossAssistManager : BaseManager<BossAssistManager>, IManager, IUpdatableManager, ISaveable
    {
        private const int MaxRosterSize = 4;
        private readonly UPlayGround.Cycle.AssistRosterService _roster = new();
        private readonly UPlayGround.Cycle.BossRecruitmentService _recruitment = new();
        private readonly Dictionary<string, float> _cooldowns = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (bool specialBreak, bool noHit)> _defeatContexts = new(StringComparer.Ordinal);
        private BossAssistDatabaseSO _database;
        private GameObject _activeModel;
        private IBossAssistEffectExecutor _activeExecutor;
        private float _executionRemaining;
        private string _activeAssistId;
        private float _pendingCooldownSeconds;
        private bool _inputRegistered;

        public UPlayGround.Cycle.AssistRosterService Roster => _roster;
        public UPlayGround.Cycle.BossRecruitmentService Recruitment => _recruitment;
        public BossAssistDefinitionSO EquippedDefinition => _database?.FindByAssistId(_roster.EquippedAssistId);
        /// <summary>UI 등 외부에서 어시스트 정의를 조회한다. DB 미구성/미등록이면 null.</summary>
        public BossAssistDefinitionSO FindDefinition(string assistId) => _database?.FindByAssistId(assistId);
        public bool IsExecuting => _activeModel != null;
        public event Action<string> OnAssistStarted;
        public event Action<string> OnAssistCompleted;
        public event Action<UPlayGround.Cycle.BossRecruitmentResult> OnRecruitmentResolved;
        public event Action<string> OnDuplicateRecruitRewardRequested;

        public void Init() => SaveManager.Instance.RegisterSaveable(this);
        public void AfterInit()
        {
            RegisterInput();
            CycleRunManager.Instance.OnBossDefeated += OnCycleBossDefeated;
            CycleRunManager.Instance.OnCycleCompleted += OnCycleCompleted;
            CycleRunManager.Instance.OnCycleStarted += OnCycleStarted;
        }

        public void Configure(BossAssistDatabaseSO database)
        {
            _database = database;
            if (_database == null) return;

            var validRoster = new List<string>();
            foreach (string id in _roster.Roster)
                if (_database.FindByAssistId(id) != null) validRoster.Add(id);

            string equipped = _database.FindByAssistId(_roster.EquippedAssistId) != null
                ? _roster.EquippedAssistId
                : null;
            string pending = _database.FindByAssistId(_roster.PendingRecruitAssistId) != null
                ? _roster.PendingRecruitAssistId
                : null;
            _roster.Restore(validRoster, equipped, pending);
        }

        public void Dispose()
        {
            CleanupExecution(false);
            UnregisterInput();
            if (CycleRunManager.Instance != null)
            {
                CycleRunManager.Instance.OnBossDefeated -= OnCycleBossDefeated;
                CycleRunManager.Instance.OnCycleCompleted -= OnCycleCompleted;
                CycleRunManager.Instance.OnCycleStarted -= OnCycleStarted;
            }
        }

        public void OnUpdate()
        {
            // 명시적 일시정지 중에는 쿨다운 감소·어시스트 실행 타이머를 멈춘다 (사이클 런 타이머와 동일 계약).
            if (GameTimeManager.Instance != null && GameTimeManager.Instance.IsPaused) return;

            if (_cooldowns.Count > 0)
            {
                string[] ids = new string[_cooldowns.Count];
                _cooldowns.Keys.CopyTo(ids, 0);
                foreach (string id in ids) _cooldowns[id] = Mathf.Max(0f, _cooldowns[id] - Time.unscaledDeltaTime);
            }
            if (_activeModel != null)
            {
                _executionRemaining -= Time.unscaledDeltaTime;
                if (_executionRemaining <= 0f) CleanupExecution(true);
            }
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType) => CleanupExecution(false);

        public bool RequestAssist()
        {
            if (_activeModel != null || _database == null || !(CycleRunManager.Instance?.IsActive ?? false)) return false;
            string assistId = _roster.EquippedAssistId;
            BossAssistDefinitionSO definition = _database.FindByAssistId(assistId);
            if (definition == null || GetCooldownRemaining(assistId) > 0f) return false;
            PlayerActor player = GameObjectManager.Instance?.Player;
            if (player == null || !player.IsAlive()) return false;
            MonsterActor target = FindNearestTarget(player.transform.position, 30f);
            if (definition.requiresTarget && target == null) return false;
            if (!TryResolvePosition(definition, player, target, out Vector3 position, out Quaternion rotation)) return false;

            _activeModel = definition.assistPrefab != null
                ? Instantiate(definition.assistPrefab, position, rotation)
                : new GameObject($"Assist_{definition.assistId}");
            _activeModel.transform.SetPositionAndRotation(position, rotation);
            DisableCollision(_activeModel);
            _activeExecutor = _activeModel.GetComponentInChildren<IBossAssistEffectExecutor>();
            _executionRemaining = Mathf.Max(0.1f, definition.maxExecutionSeconds);
            _activeAssistId = assistId;
            _pendingCooldownSeconds = Mathf.Max(0f, definition.cooldownSeconds);
            OnAssistStarted?.Invoke(assistId);

            try
            {
                if (_activeExecutor != null) _activeExecutor.Execute(definition, player, target, CompleteExecution);
                else if (definition.healAmount > 0f)
                    player.ApplyHealingEffect(definition.healAmount);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                CleanupExecution(false, applyCooldown: false);
                return false;
            }
            return true;
        }

        public void ReportBossDefeatContext(string spawnId, bool specialBreak, bool noHit)
        {
            if (!string.IsNullOrWhiteSpace(spawnId)) _defeatContexts[spawnId] = (specialBreak, noHit);
        }

        public void CompleteExecution() => CleanupExecution(true);
        public float GetCooldownRemaining(string assistId) => !string.IsNullOrEmpty(assistId) && _cooldowns.TryGetValue(assistId, out float value) ? value : 0f;
        public (float remaining, float duration) SampleCooldown()
        {
            BossAssistDefinitionSO definition = _database?.FindByAssistId(_roster.EquippedAssistId);
            return definition == null ? (0f, 0f) : (GetCooldownRemaining(definition.assistId), definition.cooldownSeconds);
        }

        public Dictionary<string, float> GetCooldownSnapshot() => new(_cooldowns, StringComparer.Ordinal);
        public void RestoreCooldowns(IEnumerable<KeyValuePair<string, float>> values)
        {
            _cooldowns.Clear();
            if (values == null) return;
            foreach (KeyValuePair<string, float> value in values) if (_database?.FindByAssistId(value.Key) != null) _cooldowns[value.Key] = Mathf.Max(0f, value.Value);
        }

        private void OnCycleBossDefeated(CycleBossPlacement placement)
        {
            if (placement == null) return;
            BossAssistDefinitionSO definition = _database?.FindByBossActorId(placement.actorId);
            if (definition == null) return;
            if (placement.isCentral && !definition.recruitableFromCentralBoss) return;
            CycleRunState run = CycleRunManager.Instance.Current;
            int derived = DeriveRecruitSeed(run.seed, placement.spawnId);
            _defeatContexts.TryGetValue(placement.spawnId, out var flags);
            _defeatContexts.Remove(placement.spawnId);
            var context = new UPlayGround.Cycle.BossDefeatContext(placement.actorId, placement.spawnId, flags.specialBreak, flags.noHit);
            var result = _recruitment.Roll(definition.assistId, context, new System.Random(derived), _roster, MaxRosterSize);
            OnRecruitmentResolved?.Invoke(result);
            if (result.rosterResult?.status == UPlayGround.Cycle.AssistRecruitStatus.Duplicate)
                OnDuplicateRecruitRewardRequested?.Invoke(definition.assistId);
            SaveManager.Instance?.TrySaveActiveSlot();
        }

        private void OnCycleStarted(int _) => _cooldowns.Clear();
        private void OnCycleCompleted(int _) { CleanupExecution(false); _cooldowns.Clear(); }

        private void CleanupExecution(bool notify, bool applyCooldown = true)
        {
            if (_activeModel == null) return;
            string assistId = _activeAssistId;
            try { _activeExecutor?.Cancel(); }
            catch (Exception exception) { Debug.LogException(exception); }
            Destroy(_activeModel);
            _activeModel = null;
            _activeExecutor = null;
            _executionRemaining = 0f;
            if (applyCooldown && !string.IsNullOrEmpty(assistId))
                _cooldowns[assistId] = _pendingCooldownSeconds;
            _activeAssistId = null;
            _pendingCooldownSeconds = 0f;
            if (notify) OnAssistCompleted?.Invoke(assistId);
        }

        private static MonsterActor FindNearestTarget(Vector3 origin, float radius)
        {
            MonsterActor[] monsters = FindObjectsByType<MonsterActor>(FindObjectsSortMode.None);
            MonsterActor best = null; float bestSqr = radius * radius;
            foreach (MonsterActor monster in monsters)
            {
                if (monster == null || !monster.CanTakeDamage()) continue;
                float sqr = (monster.transform.position - origin).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = monster; }
            }
            return best;
        }

        private static bool TryResolvePosition(BossAssistDefinitionSO definition, PlayerActor player, MonsterActor target, out Vector3 position, out Quaternion rotation)
        {
            Transform anchor = definition.placementPolicy == AssistPlacementPolicy.NearTarget && target != null ? target.transform : player.transform;
            position = definition.placementPolicy == AssistPlacementPolicy.PlayerForwardFixed
                ? player.transform.position + player.transform.forward * Mathf.Max(1f, definition.placementOffset.z)
                : anchor.TransformPoint(definition.placementOffset);
            rotation = Quaternion.LookRotation(target != null ? (target.transform.position - position).normalized : player.transform.forward, Vector3.up);
            if (Physics.Raycast(position + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Ignore)) position = hit.point;
            return Physics.CheckSphere(position + Vector3.up, 0.5f, ~0, QueryTriggerInteraction.Ignore) == false;
        }

        private static void DisableCollision(GameObject model)
        {
            foreach (Collider value in model.GetComponentsInChildren<Collider>(true)) value.enabled = false;
            foreach (Rigidbody value in model.GetComponentsInChildren<Rigidbody>(true)) { value.isKinematic = true; value.detectCollisions = false; }
        }

        private void RegisterInput()
        {
            if (_inputRegistered || InputManager.Instance == null) return;
            InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.BossAssist, null, OnAssistInput, null, null, null, InputLayer.Level_0);
            _inputRegistered = true;
        }

        private void UnregisterInput()
        {
            if (!_inputRegistered || InputManager.Instance == null) return;
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.BossAssist, null, OnAssistInput, null);
            _inputRegistered = false;
        }

        private void OnAssistInput(InputAction.CallbackContext _) => RequestAssist();
        private static int DeriveRecruitSeed(int seed, string spawnId)
        {
            unchecked { int hash = seed; foreach (char c in spawnId ?? string.Empty) hash = hash * 31 + c; return hash; }
        }

        public void ExportSaveData(UPlayGround.Data.Save.GameSaveData saveData)
        {
            saveData.cycle ??= new UPlayGround.Data.Save.CycleSaveData();
            AssistProgressSaveData data = new()
            {
                roster = new List<string>(_roster.Roster),
                equippedAssistId = _roster.EquippedAssistId,
                pity = _recruitment.Export(),
                pendingRecruitAssistId = _roster.PendingRecruitAssistId,
            };
            foreach ((string id, float remaining) in _cooldowns)
                data.cooldowns.Add(new AssistCooldownEntry { assistId = id, remainingSeconds = remaining });
            saveData.cycle.assists = data;
        }

        public void ImportSaveData(UPlayGround.Data.Save.GameSaveData saveData)
        {
            AssistProgressSaveData data = saveData?.cycle?.assists ?? new AssistProgressSaveData();
            IEnumerable<string> validRoster = data.roster;
            if (_database != null) validRoster = data.roster.FindAll(id => _database.FindByAssistId(id) != null);
            _roster.Restore(validRoster, data.equippedAssistId, data.pendingRecruitAssistId);
            _recruitment.Restore(data.pity);
            _cooldowns.Clear();
            _defeatContexts.Clear();
            bool keepCooldowns = saveData?.cycle?.run?.phase is CycleRunPhase.Active or CycleRunPhase.BossDefeated;
            if (keepCooldowns && data.cooldowns != null)
                foreach (AssistCooldownEntry value in data.cooldowns)
                    if (value != null && (_database == null || _database.FindByAssistId(value.assistId) != null))
                        _cooldowns[value.assistId] = Mathf.Max(0f, value.remainingSeconds);
        }

        public void ResetForNewGame()
        {
            CleanupExecution(false);
            _roster.Clear();
            _recruitment.Restore(null);
            _cooldowns.Clear();
        }
    }
}
