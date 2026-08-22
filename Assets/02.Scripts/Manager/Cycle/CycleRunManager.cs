using System;
using System.Security.Cryptography;
using UnityEngine;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.Event;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Save;
using UPlayGround.Cycle;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 사이클의 생성, 진행, 완료, 포기와 저장 상태를 단일 지점에서 관리한다.
    /// 월드 배치와 정산의 구체 구현은 서비스 경계를 통해서만 호출한다.
    /// </summary>
    public sealed class CycleRunManager : BaseManager<CycleRunManager>,
        UPlayGround.UI.IUICycleRunService,
        IManager,
        IUpdatableManager,
        ISaveable,
        ICycleRunReaderService,
        ICycleExitService
    {
        private const int MinCycleIndex = 1;
        private const int MaxCycleIndex = 3;
        private const int StoryProgressOuterTrialsCleared = 10;
        private const int StoryProgressCentralEvaluationCleared = 20;
        private const int StoryProgressFirstReturnCompleted = 30;
        private const int StoryProgressSecondSettlementCompleted = 40;
        private const int StoryProgressFinalEvaluationCompleted = 50;
        private const string CycleQuestLineGraphId = "FLOW_CycleQuestLine";
        private const string OuterTrialsClearedEntryId = "outer_trials_cleared";
        private const string CentralEvaluationClearedEntryId = "central_evaluation_cleared";
        private const string FirstReturnCompletedEntryId = "first_return_completed";
        private const string SecondSettlementCompletedEntryId = "second_settlement_completed";
        private const string FinalEvaluationCompletedEntryId = "final_evaluation_completed";
        private const string FirstReturnStartedFlag = "cycle.story.first_return_started";
        private const string NextRouteUnlockedFlag = "cycle.story.next_route_unlocked";
        private const string FirstReturnArrivedEventId = "cycle.story.first_return_arrived";
        private const string MainQuestFirstCycleId = "quest_main_001";
        private const string MainQuestFirstReturnId = "quest_main_003";
        private const string SecondSettlementStoryFlag = "cycle.story.second_settlement_completed";
        private const string FinalEvaluationStoryFlag = "cycle.story.final_evaluation_completed";

        [SerializeField] private CycleConfigSO _config;

        private CycleRunState _current = CycleRunState.CreateInactive();
        private ICycleWorldSpawnService _worldSpawnService;
        private ICycleSettlementService _settlementService;
        private CycleLayoutState _layout;
        private bool _ownsRuntimeConfig;
        private CycleHistorySaveData _history = new();
        private bool _startRequestedForNextWorld;
        private int? _requestedStartSeed;
        private string _configuredWorldMapId;
        private IGlobalFlagService _boundFlags;
        private int _persistentStorySaveNotBeforeFrame = -1;

        /// <summary>외부에서 수정할 수 없도록 복사본을 반환한다.</summary>
        public CycleRunState Current => _current.Clone();

        public CycleConfigSO Config => _config;
        public CycleLayoutState CurrentLayout => _layout?.Clone();
        public CycleHistorySaveData History => _history;

        public bool IsActive => _current.phase is
            CycleRunPhase.Preparing or
            CycleRunPhase.Active or
            CycleRunPhase.BossDefeated or
            CycleRunPhase.Settling;
        public int CycleIndex => _current.cycleIndex;
        public int Seed => _current.seed;

        public event Action<CycleRunState> OnPhaseChanged;
        public event Action<int> OnCycleStarted;
        public event Action<int> OnCycleCompleted;
        public event Action<CycleBossPlacement> OnBossDiscovered;
        public event Action<CycleBossPlacement> OnBossDefeated;

        public void Init()
        {
            if (_config == null)
            {
                _config = CycleConfigSO.CreateRuntimeDefault();
                _ownsRuntimeConfig = true;
            }

            SaveManager.Instance.RegisterSaveable(this);
            _settlementService = new CycleSettlementService(this);
        }

        public void AfterInit()
        {
            BindPersistentStoryFlags();
            ApplyExitPortalState();
        }

        public void Dispose()
        {
            _worldSpawnService?.CleanupRunObjects();
            _worldSpawnService = null;
            _settlementService = null;
            if (_boundFlags != null)
                _boundFlags.OnFlagChanged -= OnPersistentStoryFlagChanged;
            _boundFlags = null;
            _persistentStorySaveNotBeforeFrame = -1;

            if (_ownsRuntimeConfig && _config != null)
                Destroy(_config);

            _config = null;
            _ownsRuntimeConfig = false;
            _startRequestedForNextWorld = false;
            _requestedStartSeed = null;
            _configuredWorldMapId = null;
            OnPhaseChanged = null;
            OnCycleStarted = null;
            OnCycleCompleted = null;
            OnBossDiscovered = null;
            OnBossDefeated = null;
            OnSettlementCommitted = null;
        }

        public void OnUpdate()
        {
            // 대화 액션에서 플래그가 바뀐 프레임의 FlowGraph/퀘스트 처리가 모두 끝난 뒤 저장한다.
            // 즉시 저장하면 구독자 호출 순서에 따라 플래그만 저장되고 SP/퀘스트 완료가 빠질 수 있다.
            if (_persistentStorySaveNotBeforeFrame >= 0
                && Time.frameCount >= _persistentStorySaveNotBeforeFrame)
            {
                _persistentStorySaveNotBeforeFrame = -1;
                RequestImmediateSave();
            }

            // unscaled 기반이라 히트스톱/슬로모에는 영향받지 않되,
            // 메뉴 등 명시적 일시정지(GameTimeManager.SetPause) 중에는 런 타이머를 멈춘다.
            if (GameTimeManager.Instance != null && GameTimeManager.Instance.IsPaused) return;

            if (_current.phase is CycleRunPhase.Active or CycleRunPhase.BossDefeated)
                _current.elapsedSeconds += Time.unscaledDeltaTime;
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType)
        {
            _worldSpawnService?.OnSceneChanged(sceneType);
            ApplyExitPortalState();
            TryStartRequestedCycle();
        }

        /// <summary>02 월드 배치 구현이 준비된 뒤 등록한다.</summary>
        public void SetWorldSpawnService(ICycleWorldSpawnService service)
        {
            _worldSpawnService = service;
        }

        public void ConfigureWorldContext(CycleWorldContext context)
        {
            if (context == null) return;
            _worldSpawnService = new CycleWorldSpawnService(context, this);
            _configuredWorldMapId = context.Config?.mapId;
            if (IsActive && _layout != null &&
                string.Equals(_current.mapId, SceneManager.Instance?.CurrentMapID, StringComparison.Ordinal))
            {
                if (!_worldSpawnService.TryRestore(_current.Clone(), _layout.Clone(), out string error))
                    Debug.LogError($"[CycleRunManager] 사이클 레이아웃 복원 실패: {error}");
                else
                    ApplyExitPortalState();
            }

            // CycleWorldContext.Start와 SceneContext 준비 통지의 실행 순서는 보장되지 않는다.
            // 여기와 OnSceneChanged 양쪽에서 시도해 두 조건이 모두 갖춰진 시점에 정확히 한 번 시작한다.
            TryStartRequestedCycle();
        }

        /// <summary>SceneContext가 지역 FlowGraph를 모두 무장한 뒤 호출한다.</summary>
        public void NotifyStoryFlowReady()
        {
            MigrateLegacyFirstReturnState();
            TryStartRequestedCycle();
        }

        /// <summary>
        /// 구버전은 첫 정산에서 main003의 단일 목표만 진행했고 first_return_started 플래그가 없었다.
        /// 완료 이력이 있는데 main003이 아직 활성이라면 새 첫 귀환 앵커의 도착 단계로 한 번만 옮긴다.
        /// 플래그 자체가 마이그레이션 완료 마커이므로 이후 로드에서는 반복되지 않는다.
        /// </summary>
        private void MigrateLegacyFirstReturnState()
        {
            if (_history == null || _history.completedCycleCount < 1)
                return;

            IGlobalFlagService flags = Svc.Flags;
            IQuestFlowService quest = Svc.QuestFlow;
            if (flags == null
                || quest == null
                || flags.GetFlag(FirstReturnStartedFlag)
                || quest.GetQuestStatus(MainQuestFirstReturnId) != QuestStatus.Active)
            {
                return;
            }

            flags.SetFlag(FirstReturnStartedFlag, true);
            quest.NotifyStoryEvent(FirstReturnArrivedEventId);
            _persistentStorySaveNotBeforeFrame = Mathf.Max(
                _persistentStorySaveNotBeforeFrame,
                Time.frameCount + 1);
            Debug.Log("[CycleRunManager] 구버전 첫 정산 세이브를 첫 귀환 앵커 시작 상태로 보정했습니다.");
        }

        /// <summary>06 정산 구현이 준비된 뒤 등록한다.</summary>
        public void SetSettlementService(ICycleSettlementService service)
        {
            _settlementService = service;
        }

        /// <summary>테스트 또는 부트스트랩에서 공통 설정을 교체한다.</summary>
        public bool SetConfig(CycleConfigSO config, bool allowActiveRestore = false)
        {
            if (IsActive && !allowActiveRestore)
            {
                Debug.LogWarning("[CycleRunManager] 실행 중에는 설정을 교체할 수 없습니다.");
                return false;
            }

            if (config == null)
            {
                Debug.LogError("[CycleRunManager] 설정 교체 실패: 설정이 null입니다.");
                return false;
            }

            if (!config.ValidateP0(out string error))
            {
                Debug.LogError($"[CycleRunManager] 설정 교체 실패: {error}");
                return false;
            }

            if (_ownsRuntimeConfig && _config != null)
                Destroy(_config);

            _config = config;
            _ownsRuntimeConfig = false;
            return true;
        }

        /// <summary>
        /// 첫 사이클 또는 직전 완료 사이클의 다음 번호로 시작한다.
        /// 결말 이후에는 최고 난이도(3회차)를 유지한 채 새 시드로 반복한다.
        /// </summary>
        public bool StartNewCycle(int? requestedSeed = null)
        {
            int cycleIndex = _current.phase == CycleRunPhase.Completed
                ? Mathf.Min(_current.cycleIndex + 1, MaxCycleIndex)
                : MinCycleIndex;
            return StartCycle(cycleIndex, requestedSeed);
        }

        /// <summary>
        /// 새 게임처럼 씬 로드 전에 들어온 사이클 시작 요청을 보존한다.
        /// 다음 CycleWorldContext와 SceneContext.MapID가 모두 준비되면 자동으로 한 번 실행한다.
        /// </summary>
        public void RequestStartNewCycleOnNextWorld(int? requestedSeed = null)
        {
            _startRequestedForNextWorld = true;
            _requestedStartSeed = requestedSeed;
            TryStartRequestedCycle();
        }

        /// <summary>개발 검증과 이어하기에서 명시한 P0 사이클 번호로 시작한다.</summary>
        public bool StartCycle(int cycleIndex, int? requestedSeed = null)
        {
            if (_current.phase is not (CycleRunPhase.Inactive or CycleRunPhase.Completed))
            {
                Debug.LogWarning($"[CycleRunManager] {_current.phase} 단계에서는 사이클을 시작할 수 없습니다.");
                return false;
            }

            if (cycleIndex < MinCycleIndex || cycleIndex > MaxCycleIndex)
            {
                Debug.LogWarning($"[CycleRunManager] P0 사이클 번호는 {MinCycleIndex}~{MaxCycleIndex}만 허용합니다: {cycleIndex}");
                return false;
            }

            if (_config == null)
            {
                Debug.LogError("[CycleRunManager] 사이클 설정이 없습니다.");
                return false;
            }

            if (!_config.ValidateP0(out string configError))
            {
                Debug.LogError($"[CycleRunManager] 유효한 사이클 설정이 없습니다: {configError}");
                return false;
            }

            if (_worldSpawnService == null)
            {
                Debug.LogError("[CycleRunManager] CycleWorldSpawnService가 등록되지 않아 사이클을 시작할 수 없습니다.");
                return false;
            }

            string mapId = SceneManager.Instance?.CurrentMapID;
            if (string.IsNullOrWhiteSpace(mapId))
            {
                Debug.LogError("[CycleRunManager] 현재 맵 ID가 없어 사이클을 시작할 수 없습니다.");
                return false;
            }

            CycleRunState previous = _current.Clone();
            CycleLayoutState previousLayout = _layout;
            int seed = requestedSeed ?? CreateSeed();

            _current = new CycleRunState
            {
                cycleIndex = cycleIndex,
                seed = seed,
                mapId = mapId,
                phase = CycleRunPhase.Preparing,
            };
            _layout = null;
            RaisePhaseChanged();

            try
            {
                if (!_worldSpawnService.TryBuildAndSpawn(
                        _current.Clone(),
                        CreateRandom,
                        out CycleLayoutState layout,
                        out string spawnError) ||
                    layout?.centralBoss == null ||
                    string.IsNullOrWhiteSpace(layout.centralBoss.spawnId))
                {
                    _worldSpawnService.CleanupRunObjects();
                    _current = previous;
                    _layout = previousLayout;
                    ApplyExitPortalState();
                    RaisePhaseChanged();
                    Debug.LogError($"[CycleRunManager] 사이클 월드 생성 실패: {spawnError ?? "중앙 보스 spawnId 누락"}");
                    return false;
                }

                _layout = layout;
                SetPhase(CycleRunPhase.Active);
                OnCycleStarted?.Invoke(cycleIndex);
                RequestImmediateSave();
                return true;
            }
            catch (Exception exception)
            {
                _worldSpawnService.CleanupRunObjects();
                _current = previous;
                _layout = previousLayout;
                ApplyExitPortalState();
                RaisePhaseChanged();
                Debug.LogException(exception);
                return false;
            }
        }

        public bool NotifyCentralBossDefeated(string spawnId)
        {
            if (_current.phase != CycleRunPhase.Active ||
                _current.centralBossDefeated ||
                string.IsNullOrWhiteSpace(spawnId) ||
                !string.Equals(spawnId, _layout?.centralBoss?.spawnId, StringComparison.Ordinal))
            {
                return false;
            }

            _current.centralBossDefeated = true;
            _current.exitPortalActivated = true;
            SetPhase(CycleRunPhase.BossDefeated);
            AdvanceCycleStoryProgress(StoryProgressOuterTrialsCleared);
            AdvanceCycleStoryProgress(StoryProgressCentralEvaluationCleared);
            ApplyExitPortalState();
            RequestImmediateSave();
            return true;
        }

        public bool DiscoverBoss(string spawnId)
        {
            if (_current.phase is not (CycleRunPhase.Active or CycleRunPhase.BossDefeated) || _layout == null)
                return false;

            CycleBossPlacement placement = _layout.FindBoss(spawnId);
            if (placement == null || placement.discovered || placement.defeated)
                return false;

            placement.discovered = true;
            OnBossDiscovered?.Invoke(placement.Clone());
            RequestImmediateSave();
            return true;
        }

        public bool ReportPlayerDamageDuringBossEncounter(string spawnId)
        {
            CycleBossPlacement placement = _layout?.FindBoss(spawnId);
            if (placement == null || !placement.discovered || placement.defeated ||
                placement.playerTookDamageAfterDiscovery)
                return false;

            placement.playerTookDamageAfterDiscovery = true;
            RequestImmediateSave();
            return true;
        }

        public void ReportBossDefeatContext(string spawnId, bool specialBreak, bool noHit)
        {
            CycleBossPlacement placement = _layout?.FindBoss(spawnId);
            if (placement == null || placement.defeated) return;
            placement.finishedBySpecialBreakAttack = specialBreak;
            placement.defeatedNoHit = noHit;
        }

        public bool NotifyBossDefeated(string spawnId)
        {
            CycleBossPlacement placement = _layout?.FindBoss(spawnId);
            if (placement == null || placement.defeated)
                return false;

            placement.discovered = true;
            placement.defeated = true;
            OnBossDefeated?.Invoke(placement.Clone());

            if (placement.isCentral)
                return NotifyCentralBossDefeated(spawnId);

            TryAdvanceOuterTrialStoryProgress();
            RequestImmediateSave();
            return true;
        }

        public bool RequestExit()
        {
            if (_current.phase != CycleRunPhase.BossDefeated || !_current.centralBossDefeated)
                return false;

            if (_settlementService == null)
            {
                Debug.LogError("[CycleRunManager] CycleSettlementService가 등록되지 않아 정산할 수 없습니다.");
                return false;
            }

            SetPhase(CycleRunPhase.Settling);

            try
            {
                if (!_settlementService.TrySettle(_current.Clone(), out string error))
                {
                    SetPhase(CycleRunPhase.BossDefeated);
                    Debug.LogError($"[CycleRunManager] 사이클 정산 실패: {error}");
                    return false;
                }
            }
            catch (Exception exception)
            {
                SetPhase(CycleRunPhase.BossDefeated);
                Debug.LogException(exception);
                return false;
            }

            int completedCycleIndex = _current.cycleIndex;
            _worldSpawnService?.CleanupRunObjects();
            _layout = null;
            _current.exitPortalActivated = false;
            SetPhase(CycleRunPhase.Completed);
            ApplyExitPortalState();
            AdvanceCycleStoryProgressForSettlement(_history.completedCycleCount);
            OnCycleCompleted?.Invoke(completedCycleIndex);
            RequestImmediateSave();
            return true;
        }

        public event Action<CycleSettlementPlan> OnSettlementCommitted;
        public void NotifySettlementCommitted(CycleSettlementPlan plan)
        {
            Delegate[] listeners = OnSettlementCommitted?.GetInvocationList();
            if (listeners == null) return;

            foreach (Delegate listener in listeners)
            {
                try { ((Action<CycleSettlementPlan>)listener).Invoke(plan); }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        /// <summary>에디터와 개발 빌드에서만 사용하는 무보상 포기 치트.</summary>
        public bool AbortCycle()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!IsActive)
                return false;

            _settlementService?.AbortRun();
            _worldSpawnService?.CleanupRunObjects();
            _layout = null;
            _current = CycleRunState.CreateInactive();
            ApplyExitPortalState();
            RaisePhaseChanged();
            RequestImmediateSave();
            return true;
#else
            Debug.LogWarning("[CycleRunManager] AbortCycle은 개발 빌드에서만 사용할 수 있습니다.");
            return false;
#endif
        }

        public CycleDifficultyEntry GetCurrentDifficulty()
        {
            return _config != null && _config.TryGetDifficulty(_current.cycleIndex, out CycleDifficultyEntry entry)
                ? entry
                : null;
        }

        /// <summary>스트림마다 독립적인 시작 상태를 가진 결정적 RNG를 만든다.</summary>
        public System.Random CreateRandom(CycleRandomStream stream)
        {
            return new System.Random(DeriveStreamSeed(_current.seed, stream));
        }

        public void ExportSaveData(GameSaveData saveData)
        {
            if (saveData == null)
                return;

            saveData.cycle ??= new CycleSaveData();
            CycleRunState runSnapshot = _current.Clone();
            if (runSnapshot.phase == CycleRunPhase.Settling)
                runSnapshot.phase = CycleRunPhase.BossDefeated;

            saveData.cycle.run = runSnapshot;
            saveData.cycle.layout = _layout?.Clone();
            saveData.cycle.history = _history;
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _startRequestedForNextWorld = false;
            _requestedStartSeed = null;
            CycleSaveData cycle = saveData?.cycle;
            _current = cycle?.run?.Clone() ?? CycleRunState.CreateInactive();
            _layout = cycle?.layout?.Clone();
            _history = cycle?.history ?? new CycleHistorySaveData();

            // Settling은 파일에 남기지 않는 단계다. 구버전/비정상 저장은 재정산 가능한 단계로 되돌린다.
            if (_current.phase == CycleRunPhase.Settling)
                _current.phase = CycleRunPhase.BossDefeated;

            if (_current.phase is CycleRunPhase.Inactive or CycleRunPhase.Completed)
            {
                _current.exitPortalActivated = false;
                _layout = null;
            }

            ApplyExitPortalState();
            RaisePhaseChanged();
        }

        public void ResetForNewGame()
        {
            _worldSpawnService?.CleanupRunObjects();
            _settlementService?.AbortRun();
            _layout = null;
            _history = new CycleHistorySaveData();
            _current = CycleRunState.CreateInactive();
            _startRequestedForNextWorld = false;
            _requestedStartSeed = null;
            _persistentStorySaveNotBeforeFrame = -1;
            ApplyExitPortalState();
            RaisePhaseChanged();
        }

        private void TryStartRequestedCycle()
        {
            if (!_startRequestedForNextWorld || _worldSpawnService == null || IsActive)
                return;

            string currentMapId = SceneManager.Instance?.CurrentMapID;
            if (string.IsNullOrWhiteSpace(currentMapId) ||
                string.IsNullOrWhiteSpace(_configuredWorldMapId) ||
                !string.Equals(currentMapId, _configuredWorldMapId, StringComparison.Ordinal))
            {
                return;
            }

            if (_history.completedCycleCount == 0)
            {
                IQuestFlowService quest = Svc.QuestFlow;
                if (quest == null)
                    return;

                QuestStatus status = quest.GetQuestStatus(MainQuestFirstCycleId);
                if (status is not (QuestStatus.Active or QuestStatus.Completed))
                {
                    if (!quest.AcceptQuest(MainQuestFirstCycleId))
                        return;
                    status = quest.GetQuestStatus(MainQuestFirstCycleId);
                }

                if (status == QuestStatus.Active)
                    quest.TrackQuest(MainQuestFirstCycleId);
            }

            int? seed = _requestedStartSeed;
            if (!StartNewCycle(seed))
            {
                Debug.LogError($"[CycleRunManager] 새 게임의 사이클 자동 시작에 실패했습니다: mapId={currentMapId}");
            }
            else
            {
                _startRequestedForNextWorld = false;
                _requestedStartSeed = null;
                Debug.Log($"[CycleRunManager] 새 게임 사이클 자동 시작 완료: mapId={currentMapId}, cycle={_current.cycleIndex}, seed={_current.seed}");
            }
        }

        private void SetPhase(CycleRunPhase phase)
        {
            if (_current.phase == phase)
                return;

            _current.phase = phase;
            RaisePhaseChanged();
        }

        private void RaisePhaseChanged()
        {
            OnPhaseChanged?.Invoke(_current.Clone());
        }

        private void ApplyExitPortalState()
        {
            bool active = _current.phase == CycleRunPhase.BossDefeated &&
                          _current.centralBossDefeated &&
                          _current.exitPortalActivated;
            PortalActor[] portals = FindObjectsByType<PortalActor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < portals.Length; i++)
            {
                if (portals[i] != null && portals[i].IsCycleExitPortal)
                    portals[i].SetPortalActive(active);
            }
        }

        private void TryAdvanceOuterTrialStoryProgress()
        {
            if (_layout?.outerBosses == null || _layout.outerBosses.Count == 0)
                return;

            int defeatedCount = 0;
            for (int i = 0; i < _layout.outerBosses.Count; i++)
            {
                CycleBossPlacement outerBoss = _layout.outerBosses[i];
                if (outerBoss != null && outerBoss.defeated)
                    defeatedCount++;
            }

            ActorSvc.QuestProgress?.NotifyCycleOuterBossProgress(defeatedCount);
            if (defeatedCount < _layout.outerBosses.Count)
                return;

            if (_layout.centralBoss != null
                && !_layout.centralBoss.defeated
                && !_worldSpawnService.TryActivateBoss(_layout.centralBoss.spawnId))
            {
                Debug.LogError($"[CycleRunManager] 중앙 평가자 활성화 실패: {_layout.centralBoss.spawnId}");
                return;
            }

            AdvanceCycleStoryProgress(StoryProgressOuterTrialsCleared);
        }

        private static void AdvanceCycleStoryProgressForSettlement(int completedCycleCount)
        {
            AdvanceCycleStoryProgress(StoryProgressOuterTrialsCleared);
            AdvanceCycleStoryProgress(StoryProgressCentralEvaluationCleared);

            if (completedCycleCount == 1)
            {
                Svc.Flags?.SetFlag(FirstReturnStartedFlag, true);
                CompleteFirstReturnQuest();
                return;
            }

            if (completedCycleCount >= 2)
            {
                AdvanceCycleStoryProgress(StoryProgressSecondSettlementCompleted);
                Svc.Flags?.SetFlag(SecondSettlementStoryFlag, true);
            }

            if (completedCycleCount < 3)
                return;

            AdvanceCycleStoryProgress(StoryProgressFinalEvaluationCompleted);
            Svc.Flags?.SetFlag(FinalEvaluationStoryFlag, true);
        }

        /// <summary>
        /// 첫 귀환 퀘스트의 도착 목표를 채우고 진행도를 올린 뒤 완료한다.
        /// 진행도를 먼저 올려야 다음 메인 퀘스트의 선행 조건이 완료 시점에 이미 충족된다.
        /// </summary>
        private static void CompleteFirstReturnQuest()
        {
            IQuestFlowService quest = Svc.QuestFlow;
            if (quest == null)
                return;

            if (quest.GetQuestStatus(MainQuestFirstReturnId) is not (QuestStatus.Active or QuestStatus.Completed))
                quest.AcceptQuest(MainQuestFirstReturnId);

            quest.NotifyStoryEvent(FirstReturnArrivedEventId);
            AdvanceCycleStoryProgress(StoryProgressFirstReturnCompleted);
            quest.CompleteQuest(MainQuestFirstReturnId);
        }

        private static void AdvanceCycleStoryProgress(int progress)
        {
            string entryId = progress switch
            {
                StoryProgressOuterTrialsCleared => OuterTrialsClearedEntryId,
                StoryProgressCentralEvaluationCleared => CentralEvaluationClearedEntryId,
                StoryProgressFirstReturnCompleted => FirstReturnCompletedEntryId,
                StoryProgressSecondSettlementCompleted => SecondSettlementCompletedEntryId,
                StoryProgressFinalEvaluationCompleted => FinalEvaluationCompletedEntryId,
                _ => null,
            };

            IFlowGraphService flow = Svc.FlowGraph;
            if (!string.IsNullOrEmpty(entryId)
                && flow != null
                && flow.IsGraphRegistered(CycleQuestLineGraphId)
                && flow.StartGraph(CycleQuestLineGraphId, entryId))
            {
                return;
            }

            // 테스트·비사이클 맵처럼 그래프가 적용되지 않은 환경에서도 기존 진행 경로를 보존한다.
            Svc.StoryFlow?.SetProgress(progress);
        }

        private void BindPersistentStoryFlags()
        {
            IGlobalFlagService flags = Svc.Flags;
            if (ReferenceEquals(_boundFlags, flags))
                return;

            if (_boundFlags != null)
                _boundFlags.OnFlagChanged -= OnPersistentStoryFlagChanged;

            _boundFlags = flags;
            if (_boundFlags != null)
                _boundFlags.OnFlagChanged += OnPersistentStoryFlagChanged;
        }

        private void OnPersistentStoryFlagChanged(string key, bool value)
        {
            if (!value)
                return;

            if (string.Equals(key, FirstReturnStartedFlag, StringComparison.Ordinal)
                || string.Equals(key, NextRouteUnlockedFlag, StringComparison.Ordinal))
            {
                _persistentStorySaveNotBeforeFrame = Mathf.Max(
                    _persistentStorySaveNotBeforeFrame,
                    Time.frameCount + 1);
            }
        }

        private void RequestImmediateSave()
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager != null && !saveManager.TrySaveActiveSlot())
                Debug.LogWarning("[CycleRunManager] 활성 세이브 슬롯이 없어 사이클 상태 자동 저장을 보류했습니다.");
        }

        private static int CreateSeed()
        {
            byte[] bytes = new byte[4];
            using RandomNumberGenerator generator = RandomNumberGenerator.Create();
            generator.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        private static int DeriveStreamSeed(int seed, CycleRandomStream stream)
        {
            unchecked
            {
                uint value = (uint)seed + 0x9E3779B9u * ((uint)stream + 1u);
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return (int)value;
            }
        }
    }
}
