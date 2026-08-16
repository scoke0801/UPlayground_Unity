using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Save;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Sound;
using UPlayGround.Data.UI;
using UPlayGround.Story;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 퀘스트 시스템 매니저.
    ///
    /// ── 공개 API (QuestIdType enum 사용) ──────────────────────────
    ///   퀘스트 수락     → AcceptQuest(QuestIdType.퀘스트이름)
    ///   퀘스트 완료     → CompleteQuest(QuestIdType.퀘스트이름)
    ///   퀘스트 포기     → AbandonQuest(QuestIdType.퀘스트이름)
    ///   상태 조회       → GetQuestStatus(QuestIdType.퀘스트이름)
    ///   활성 목록       → GetActiveQuests()
    ///
    /// ── 목표 타입별 알림 (int/string 파라미터) ────────────────────
    ///   ItemCollect   → NotifyItemCollected(itemId, count)
    ///   ItemDeliver   → NotifyItemDelivered(npcId, itemId, count)
    ///   ItemUse       → NotifyItemUsed(itemId, count)
    ///   MonsterKill   → NotifyMonsterKill(actorId)
    ///   StoryProgress → NotifyStoryProgress(progress)
    ///   CycleOuterBoss→ NotifyCycleOuterBossProgress(defeatedCount)
    ///   ItemCraft     → NotifyItemCrafted(recipeId, quantity)
    ///   ItemEnhance   → NotifyItemEnhanced(itemId)
    ///   ReachLocation → NotifyLocationReached(locationId)
    ///
    /// ── 이벤트 구독 ─────────────────────────────────────────────────
    ///   EventManager.Subscribe&lt;QuestEvent, QuestStateEventData&gt;(QuestEvent.QuestAccepted, ...)
    ///   EventManager.Subscribe&lt;QuestEvent, QuestStateEventData&gt;(QuestEvent.QuestCompleted, ...)
    ///   EventManager.Subscribe&lt;QuestEvent, QuestObjectiveEventData&gt;(QuestEvent.QuestObjectiveUpdated, ...)
    ///
    /// ── QuestIdType enum 재생성 방법 ────────────────────────────────
    ///   메뉴: UPlayGround / ID Enum Generator → Quest 행 [생성] 버튼
    ///   또는: Quest Editor 창 툴바 → [Enum 생성] 버튼
    /// </summary>
    public class QuestManager : BaseManager<QuestManager>, IManager, ISaveable, IAsyncInitializableManager,
        UPlayGround.UI.IUIQuestService,
        IQuestProgressService,
        IQuestFlowService
    {
        private const string QUEST_DATABASE_KEY = "QuestDatabase";
        private const int MAX_AUTO_ACCEPT_CHAIN_DEPTH = 32;

        // ──── 데이터 ────
        private QuestDatabase _db;
        private readonly Dictionary<string, QuestSO> _runtimeQuestDefinitions = new(StringComparer.Ordinal);

        // ──── 런타임 상태 (내부는 string 키로 관리) ────
        private readonly Dictionary<string, QuestRuntimeData> _activeQuests     = new();
        private readonly HashSet<string>                      _completedQuestIds = new();
        private readonly HashSet<string>                      _failedQuestIds = new();
        private readonly HashSet<string>                      _pendingAcceptQuestIds = new();
        private readonly HashSet<string>                      _pendingReachedLocationIds = new();
        private readonly HashSet<string>                      _pendingStoryEventIds = new();
        private string _trackedQuestId;
        private bool _isQuestTrackingSuppressed;

        // DB 로드 전에 LoadGame()이 호출될 경우 보관
        private QuestSaveData _pendingLoad;
        private int _autoAcceptChainDepth;
        private bool _pendingNewGameAutoAccept;

        public bool IsDBLoaded { get; private set; } = false;
        public FirstTimeGuideConfigSO FirstTimeGuideConfig => _db?.FirstTimeGuideConfig;

        // ──────────────────────────────────────────────────────────
        #region IManager

        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
        }

        public UniTask InitializeAsync(CancellationToken cancellationToken) =>
            LoadDatabaseAsync(cancellationToken);

        public void AfterInit() { }

        public void Dispose()
        {
            _activeQuests.Clear();
            _completedQuestIds.Clear();
            _failedQuestIds.Clear();
            _pendingAcceptQuestIds.Clear();
            _pendingReachedLocationIds.Clear();
            _pendingStoryEventIds.Clear();
            _trackedQuestId = null;
            _isQuestTrackingSuppressed = false;
            DestroyRuntimeQuestDefinitions();
            _db = null;
            IsDBLoaded = false;
        }

        public void OnUpdate()      { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }
        public void OnSceneChanged(string sceneType)
        {
            TryAutoAcceptNewGameQuests();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region DB 로드

        private async UniTask LoadDatabaseAsync(CancellationToken cancellationToken)
        {
            try
            {
                _db = await AssetManager.Instance.LoadGlobalAsync<QuestDatabase>(
                    QUEST_DATABASE_KEY,
                    nameof(QuestManager),
                    cancellationToken);

                _db.Initialize();
                IsDBLoaded = true;

                if (_pendingLoad != null)
                    ApplyPendingLoad();

                FlushPendingQuestRequests();
                TryAutoAcceptNewGameQuests();

                Debug.Log("[QuestManager] QuestDatabase 로드 완료");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuestManager] QuestDatabase 로드 실패: {e.Message}");
                throw;
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 공개 API — QuestIdType enum 사용

        /// <summary>
        /// 퀘스트를 수락한다. QuestIdType enum으로 타입 안전하게 지정.
        /// 선행 퀘스트 완료 여부 및 스토리 진행도 조건을 검사한다.
        /// </summary>
        public bool AcceptQuest(QuestIdType questId) => AcceptQuestById(questId.ToQuestId());

        /// <summary>퀘스트를 수락한다. (문자열 ID 오버로드 — UI/치트용)</summary>
        public bool AcceptQuest(string questId) => AcceptQuestById(questId);

        /// <summary>
        /// 퀘스트를 완료 처리한다.
        /// autoComplete=false 퀘스트는 외부(UI 등)에서 명시적으로 호출해야 한다.
        /// </summary>
        public bool CompleteQuest(QuestIdType questId) => CompleteQuestById(questId.ToQuestId());

        /// <summary>퀘스트를 완료 처리한다. (문자열 ID 오버로드 — UI용)</summary>
        public bool CompleteQuest(string questId) => CompleteQuestById(questId);

        /// <summary>진행 중인 퀘스트를 포기한다.</summary>
        public bool AbandonQuest(QuestIdType questId) => AbandonQuestById(questId.ToQuestId());

        /// <summary>진행 중인 퀘스트를 포기한다. (문자열 ID 오버로드 — UI용)</summary>
        public bool AbandonQuest(string questId) => AbandonQuestById(questId);

        /// <summary>ID로 QuestSO 정의를 조회한다. DB 미로드/없음이면 null. (UI 상세 표시용)</summary>
        public QuestSO GetQuestData(string questId) => ResolveQuestDefinition(questId);

        /// <summary>진행 중인 퀘스트를 실패 처리한다.</summary>
        public bool FailQuest(QuestIdType questId) => FailQuestById(questId.ToQuestId());

        /// <summary>진행 중인 퀘스트를 실패 처리한다. (문자열 ID 오버로드 — UI/치트용)</summary>
        public bool FailQuest(string questId) => FailQuestById(questId);

        /// <summary>퀘스트 현재 상태를 반환한다.</summary>
        public QuestStatus GetQuestStatus(QuestIdType questId) => GetQuestStatusById(questId.ToQuestId());

        /// <summary>퀘스트 현재 상태를 반환한다. (문자열 ID 오버로드 — UI/치트용)</summary>
        public QuestStatus GetQuestStatus(string questId) => GetQuestStatusById(questId);

        /// <summary>DB에 정의된 모든 퀘스트를 열거한다. (치트/디버그 도구용)</summary>
        public IEnumerable<QuestSO> GetAllQuestDefinitions()
        {
            if (IsDBLoaded && _db != null)
                foreach (QuestSO quest in _db.GetAllQuests())
                    if (quest != null) yield return quest;
            foreach (QuestSO quest in _runtimeQuestDefinitions.Values)
                if (quest != null) yield return quest;
        }

        /// <summary>
        /// 현재 실행에만 존재하는 퀘스트 정의를 등록한다. 저장 파일에는 기록하지 않으며,
        /// 호출자가 필요할 때 같은 ID로 다시 생성한다.
        /// </summary>
        public bool RegisterRuntimeQuest(QuestSO quest, bool acceptImmediately = true)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.questId))
            {
                Debug.LogWarning("[QuestManager] 런타임 퀘스트 ID가 비어 있습니다.");
                return false;
            }

            if (_db?.GetQuest(quest.questId) != null)
            {
                Debug.LogError($"[QuestManager] DB 퀘스트와 런타임 퀘스트 ID가 중복됩니다: {quest.questId}");
                return false;
            }

            if (_runtimeQuestDefinitions.TryGetValue(quest.questId, out QuestSO previous) && previous != quest)
            {
                string previousTrackedQuestId = _trackedQuestId;
                bool refreshTrackedQuest = string.Equals(
                    previousTrackedQuestId, quest.questId, StringComparison.Ordinal);
                RemoveRuntimeQuestState(quest.questId);
                DestroyRuntimeQuestDefinition(previous);
                _runtimeQuestDefinitions[quest.questId] = quest;
                bool registered = !acceptImmediately
                                  || _activeQuests.ContainsKey(quest.questId)
                                  || AcceptQuestById(quest.questId);
                PublishRuntimeTrackingChange(previousTrackedQuestId, refreshTrackedQuest);
                return registered;
            }

            _runtimeQuestDefinitions[quest.questId] = quest;
            return !acceptImmediately || _activeQuests.ContainsKey(quest.questId) || AcceptQuestById(quest.questId);
        }

        public void UnregisterRuntimeQuests(string idPrefix)
        {
            if (string.IsNullOrEmpty(idPrefix)) return;
            string previousTrackedQuestId = _trackedQuestId;
            List<string> ids = new();
            foreach (string id in _runtimeQuestDefinitions.Keys)
                if (id.StartsWith(idPrefix, StringComparison.Ordinal)) ids.Add(id);
            foreach (string id in ids)
            {
                QuestSO definition = _runtimeQuestDefinitions[id];
                _runtimeQuestDefinitions.Remove(id);
                RemoveRuntimeQuestState(id);
                DestroyRuntimeQuestDefinition(definition);
            }
            PublishRuntimeTrackingChange(previousTrackedQuestId);
        }

        /// <summary>퀘스트 완료 여부를 반환한다.</summary>
        public bool IsQuestCompleted(QuestIdType questId) => _completedQuestIds.Contains(questId.ToQuestId());

        /// <summary>퀘스트 실패 여부를 반환한다.</summary>
        public bool IsQuestFailed(QuestIdType questId) => _failedQuestIds.Contains(questId.ToQuestId());

        /// <summary>퀘스트가 현재 진행 중인지 반환한다.</summary>
        public bool IsQuestActive(QuestIdType questId) => _activeQuests.ContainsKey(questId.ToQuestId());

        /// <summary>진행 중인 퀘스트의 런타임 데이터를 반환한다. 없으면 null.</summary>
        public QuestRuntimeData GetActiveQuestRuntime(QuestIdType questId)
        {
            _activeQuests.TryGetValue(questId.ToQuestId(), out var r);
            return r;
        }

        /// <summary>진행 중인 퀘스트의 런타임 데이터를 반환한다. (문자열 ID 오버로드 — UI용)</summary>
        public QuestRuntimeData GetActiveQuestRuntime(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            _activeQuests.TryGetValue(questId, out var r);
            return r;
        }

        /// <summary> 진행 중인 모든 퀘스트 런타임 데이터 </summary>
        public IEnumerable<QuestRuntimeData> GetActiveQuests() => _activeQuests.Values;

        /// <summary>
        /// 세이브 데이터의 진행 중 퀘스트 목록에서 메인 퀘스트 표시 이름을 해석한다.
        /// 세이브 슬롯 미리보기용(런타임 상태가 아닌 파일 내용 기준). DB가 로드돼 있어야 하며,
        /// 메인 퀘스트가 없거나 DB 미로드면 null. (여러 개면 첫 번째. questName이 비면 questId로 대체.)
        /// </summary>
        public string ResolveMainQuestName(QuestSaveData questSave)
        {
            if (questSave?.activeQuests == null) return null;
            foreach (var entry in questSave.activeQuests)
            {
                if (entry == null) continue;
                var so = GetQuestData(entry.questId);
                if (so != null && so.questType == QuestType.Main)
                    return string.IsNullOrEmpty(so.questName) ? so.questId : so.questName;
            }
            return null;
        }

        /// <summary>현재 HUD에서 추적 중인 퀘스트 ID. 없으면 null.</summary>
        public string TrackedQuestId => _trackedQuestId;

        /// <summary>플레이어가 HUD 퀘스트 추적을 수동 해제했는지 여부.</summary>
        public bool IsQuestTrackingSuppressed => _isQuestTrackingSuppressed;

        /// <summary>현재 HUD에서 추적 중인 퀘스트 런타임 데이터. 없으면 null.</summary>
        public QuestRuntimeData GetTrackedQuestRuntime()
        {
            if (string.IsNullOrEmpty(_trackedQuestId))
            {
                return null;
            }

            _activeQuests.TryGetValue(_trackedQuestId, out var runtime);
            return runtime;
        }

        /// <summary>퀘스트를 HUD 추적 대상으로 지정한다.</summary>
        public bool TrackQuest(QuestIdType questId) => TrackQuestById(questId.ToQuestId());

        /// <summary>퀘스트를 HUD 추적 대상으로 지정한다.</summary>
        public bool TrackQuest(string questId) => TrackQuestById(questId);

        /// <summary>현재 HUD 추적 대상을 해제한다.</summary>
        public bool UntrackQuest()
        {
            if (string.IsNullOrEmpty(_trackedQuestId) && _isQuestTrackingSuppressed)
            {
                return false;
            }

            string previousQuestId = _trackedQuestId;
            _trackedQuestId = null;
            _isQuestTrackingSuppressed = true;
            SendQuestEvent(QuestEvent.QuestUntracked, previousQuestId);
            return true;
        }

        /// <summary>지정한 퀘스트가 현재 HUD 추적 대상인지 반환한다.</summary>
        public bool IsQuestTracked(QuestIdType questId) => IsQuestTracked(questId.ToQuestId());

        /// <summary>지정한 퀘스트가 현재 HUD 추적 대상인지 반환한다.</summary>
        public bool IsQuestTracked(string questId) => !string.IsNullOrEmpty(questId) && _trackedQuestId == questId;

        /// <summary>
        /// 수락 가능한 퀘스트 목록 (선행 조건 충족, 완료/진행 중 아닌 것)
        /// </summary>
        public List<QuestSO> GetAvailableQuests()
        {
            if (!IsDBLoaded) return new List<QuestSO>();

            var result = new List<QuestSO>();
            foreach (var q in _db.GetAllQuests())
            {
                if (q == null) continue;
                if (_activeQuests.ContainsKey(q.questId)) continue;
                if (!q.isRepeatable && _completedQuestIds.Contains(q.questId)) continue;
                if (!q.isRepeatable && _failedQuestIds.Contains(q.questId)) continue;
                if (!CheckPrerequisites(q)) continue;
                result.Add(q);
            }
            return result;
        }

        /// <summary>
        /// 완료한 퀘스트 목록 (UI 완료 탭용). DB에 존재하는 QuestSO만 반환한다.
        /// </summary>
        public List<QuestSO> GetCompletedQuests() => ResolveQuests(_completedQuestIds);

        /// <summary>
        /// 실패한 퀘스트 목록 (UI 실패 탭용). DB에 존재하는 QuestSO만 반환한다.
        /// </summary>
        public List<QuestSO> GetFailedQuests() => ResolveQuests(_failedQuestIds);

        private List<QuestSO> ResolveQuests(IEnumerable<string> questIds)
        {
            var result = new List<QuestSO>();
            foreach (var id in questIds)
            {
                var q = ResolveQuestDefinition(id);
                if (q != null) result.Add(q);
            }
            return result;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 내부 구현 — string ID 기반

        private bool AcceptQuestById(string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                Debug.LogWarning("[QuestManager] AcceptQuest: questId가 비어있습니다. QuestIdType.None을 사용했나요?");
                return false;
            }

            QuestSO questSO = ResolveQuestDefinition(questId);
            if (questSO == null && !IsDBLoaded)
            {
                _pendingAcceptQuestIds.Add(questId);
                Debug.Log($"[QuestManager] DB 로드 전 퀘스트 수락 요청 보류: {questId}");
                return true;
            }

            if (questSO == null)
            {
                Debug.LogWarning($"[QuestManager] 존재하지 않는 퀘스트: {questId}");
                return false;
            }

            if (_activeQuests.ContainsKey(questId))
            {
                Debug.LogWarning($"[QuestManager] 이미 진행 중인 퀘스트: {questId}");
                return false;
            }

            if (!questSO.isRepeatable && _completedQuestIds.Contains(questId))
            {
                Debug.LogWarning($"[QuestManager] 이미 완료한 퀘스트: {questId}");
                return false;
            }

            if (!questSO.isRepeatable && _failedQuestIds.Contains(questId))
            {
                Debug.LogWarning($"[QuestManager] 이미 실패한 퀘스트: {questId}");
                return false;
            }

            if (!CheckPrerequisites(questSO))
            {
                Debug.LogWarning($"[QuestManager] 선행 조건 미충족: {questId}");
                return false;
            }

            var runtime = new QuestRuntimeData(questSO) { Status = QuestStatus.Active };
            _activeQuests[questId] = runtime;
            _failedQuestIds.Remove(questId);

            // ItemCollect 목표는 수락 시 인벤토리 현황으로 즉시 갱신
            RefreshItemCollectObjectives(runtime);

            if (string.IsNullOrEmpty(_trackedQuestId) && !_isQuestTrackingSuppressed)
            {
                TrackQuestById(questId, false);
            }

            SendQuestEvent(QuestEvent.QuestAccepted, questId, questSO.questName);
            Debug.Log($"[QuestManager] 퀘스트 수락: {questSO.questName}");
            return true;
        }

        private bool CompleteQuestById(string questId)
        {
            if (!_activeQuests.TryGetValue(questId, out var runtime)) return false;

            if (!runtime.AreAllObjectivesComplete())
            {
                Debug.LogWarning($"[QuestManager] 목표 미달성 상태에서 완료 시도: {questId}");
                return false;
            }

            runtime.Status = QuestStatus.Completed;
            _activeQuests.Remove(questId);
            _completedQuestIds.Add(questId);
            _failedQuestIds.Remove(questId);

            if (_trackedQuestId == questId)
            {
                _trackedQuestId = null;
                _isQuestTrackingSuppressed = false;
                TrackFirstActiveQuest(false);
            }

            GiveRewards(runtime.QuestSO.reward);
            SendQuestEvent(QuestEvent.QuestCompleted, questId, runtime.QuestSO.questName);
            SoundManager.Instance?.PlayUi(GameSoundKey.QuestClear);
            Debug.Log($"[QuestManager] 퀘스트 완료: {runtime.QuestSO.questName}");

            AutoAcceptNextQuests(runtime.QuestSO);
            return true;
        }

        private bool AbandonQuestById(string questId)
        {
            if (!_activeQuests.TryGetValue(questId, out var runtime)) return false;
            _activeQuests.Remove(questId);
            if (_trackedQuestId == questId)
            {
                _trackedQuestId = null;
                _isQuestTrackingSuppressed = false;
                TrackFirstActiveQuest();
            }
            Debug.Log($"[QuestManager] 퀘스트 포기: {runtime.QuestSO.questName}");
            return true;
        }

        private bool FailQuestById(string questId)
        {
            if (!_activeQuests.TryGetValue(questId, out var runtime)) return false;

            runtime.Status = QuestStatus.Failed;
            _activeQuests.Remove(questId);
            _failedQuestIds.Add(questId);

            if (_trackedQuestId == questId)
            {
                _trackedQuestId = null;
                _isQuestTrackingSuppressed = false;
                TrackFirstActiveQuest(false);
            }

            SendQuestEvent(QuestEvent.QuestFailed, questId, runtime.QuestSO.questName);
            Debug.Log($"[QuestManager] 퀘스트 실패: {runtime.QuestSO.questName}");
            return true;
        }

        private QuestStatus GetQuestStatusById(string questId)
        {
            if (_activeQuests.TryGetValue(questId, out var runtime))
                return runtime.Status;
            if (_completedQuestIds.Contains(questId))
                return QuestStatus.Completed;
            if (_failedQuestIds.Contains(questId))
                return QuestStatus.Failed;
            if (IsDBLoaded)
            {
                var questSO = ResolveQuestDefinition(questId);
                if (questSO != null && !CheckPrerequisites(questSO))
                    return QuestStatus.Locked;
            }
            return QuestStatus.Available;
        }

        private bool TrackQuestById(string questId, bool sendEvent = true)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return false;
            }

            if (!_activeQuests.TryGetValue(questId, out var runtime))
            {
                Debug.LogWarning($"[QuestManager] 진행 중인 퀘스트만 추적할 수 있습니다: {questId}");
                return false;
            }

            _trackedQuestId = questId;
            _isQuestTrackingSuppressed = false;

            if (sendEvent)
            {
                SendQuestEvent(QuestEvent.QuestTracked, questId, runtime.QuestSO.questName);
            }

            return true;
        }

        private void TrackFirstActiveQuest(bool sendEvent = true)
        {
            foreach (var runtime in _activeQuests.Values)
            {
                if (runtime?.QuestSO == null)
                {
                    continue;
                }

                TrackQuestById(runtime.QuestSO.questId, sendEvent);
                return;
            }

            _isQuestTrackingSuppressed = false;
            if (sendEvent)
            {
                SendQuestEvent(QuestEvent.QuestUntracked, null);
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region Notify — 외부 시스템에서 호출

        /// <summary>
        /// 아이템 수집 시 호출.
        /// 연결 위치: InventoryManager.AddItem() 이후 또는 아이템 픽업 액터
        /// </summary>
        public void NotifyItemCollected(int itemId, int count = 1)
        {
            UpdateObjectives(QuestObjectiveType.ItemCollect, itemId, count,
                (obj, runtime) =>
                {
                    int have    = InventoryManager.Instance.GetItemCount(itemId);
                    int clamped = Mathf.Min(have, obj.requiredCount);
                    runtime.SetProgress(obj.objectiveId, clamped);
                    return clamped;
                });
        }

        /// <summary>
        /// 아이템을 NPC에게 전달할 때 호출.
        /// 연결 위치: NPC 상호작용 핸들러 또는 대화 액션
        /// </summary>
        public void NotifyItemDelivered(int npcId, int itemId, int count = 1)
        {
            UpdateObjectives(QuestObjectiveType.ItemDeliver, itemId, count,
                (obj, runtime) =>
                {
                    if (obj.npcId != npcId) return -1;
                    return runtime.AddProgress(obj.objectiveId, count);
                });
        }

        /// <summary>
        /// 아이템 사용 시 호출.
        /// 연결 위치: 아이템 사용 처리 코드
        /// </summary>
        public void NotifyItemUsed(int itemId, int count = 1)
        {
            UpdateObjectives(QuestObjectiveType.ItemUse, itemId, count);
        }

        /// <summary>
        /// 몬스터 처치 시 호출. 레거시 숫자 ID 퀘스트 호환용.
        /// </summary>
        public void NotifyMonsterKill(int monsterId)
        {
            UpdateObjectives(QuestObjectiveType.MonsterKill, monsterId, 1);
        }

        /// <summary>
        /// 몬스터 처치 시 호출. MonsterActor.ActorId 문자열을 기준으로 MonsterKill 목표를 갱신한다.
        /// </summary>
        public void NotifyMonsterKill(string actorId)
        {
            if (string.IsNullOrEmpty(actorId)) return;

            var runtimes = new List<QuestRuntimeData>(_activeQuests.Values);
            foreach (var runtime in runtimes)
            {
                foreach (var obj in runtime.QuestSO.objectives)
                {
                    if (obj.type != QuestObjectiveType.MonsterKill) continue;
                    if (!IsMonsterKillTargetMatch(obj, actorId)) continue;
                    if (runtime.IsObjectiveComplete(obj)) continue;

                    runtime.AddProgress(obj.objectiveId, 1);
                    SendObjectiveEvent(runtime, obj);
                    TryAutoComplete(runtime);
                }
            }
        }

        /// <summary>
        /// 스토리 진행도 변경 시 호출.
        /// 연결 위치: StoryManager.SetProgress()
        /// </summary>
        public void NotifyStoryProgress(int progress)
        {
            var runtimes = new List<QuestRuntimeData>(_activeQuests.Values);
            foreach (var runtime in runtimes)
            {
                foreach (var obj in runtime.QuestSO.objectives)
                {
                    if (obj.type != QuestObjectiveType.StoryProgress) continue;
                    if (runtime.IsObjectiveComplete(obj)) continue;
                    if (progress < obj.targetId) continue;

                    runtime.SetProgress(obj.objectiveId, obj.requiredCount);
                    SendObjectiveEvent(runtime, obj);
                    TryAutoComplete(runtime);
                }
            }
        }

        /// <summary>
        /// 아이템 제작 완료 시 호출.
        /// 연결 위치: RecipeManager.OnCraftingCompleted 이벤트 구독
        /// </summary>
        public void NotifyItemCrafted(int recipeId, int quantity = 1)
        {
            UpdateObjectives(QuestObjectiveType.ItemCraft, recipeId, quantity);
        }

        /// <summary>
        /// 아이템 강화 완료 시 호출.
        /// 연결 위치: 강화 시스템 완료 처리
        /// </summary>
        public void NotifyItemEnhanced(int itemId)
        {
            UpdateObjectives(QuestObjectiveType.ItemEnhance, itemId, 1);
        }

        /// <summary>
        /// 목표 지점 도달 시 호출.
        /// 연결 위치: 트리거 존, PortalActor 등
        /// </summary>
        public void NotifyLocationReached(string locationId)
        {
            if (string.IsNullOrEmpty(locationId)) return;

            if (!IsDBLoaded)
            {
                _pendingReachedLocationIds.Add(locationId);
                Debug.Log($"[QuestManager] DB 로드 전 위치 도달 알림 보류: {locationId}");
                return;
            }

            var runtimes = new List<QuestRuntimeData>(_activeQuests.Values);
            foreach (var runtime in runtimes)
            {
                foreach (var obj in runtime.QuestSO.objectives)
                {
                    if (obj.type != QuestObjectiveType.ReachLocation) continue;
                    if (obj.targetStringId != locationId) continue;
                    if (runtime.IsObjectiveComplete(obj)) continue;

                    runtime.SetProgress(obj.objectiveId, obj.requiredCount);
                    SendObjectiveEvent(runtime, obj);
                    TryAutoComplete(runtime);
                }
            }
        }

        /// <summary>
        /// 서사 흐름에서 발생한 문자열 이벤트를 활성 퀘스트 목표에 반영한다.
        /// 동일 이벤트를 다시 알려도 목표 요구량을 넘겨 누적하지 않는다.
        /// </summary>
        public void NotifyStoryEvent(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                return;

            string normalizedId = eventId.Trim();
            if (!IsDBLoaded)
            {
                _pendingStoryEventIds.Add(normalizedId);
                Debug.Log($"[QuestManager] DB 로드 전 스토리 이벤트 알림 보류: {normalizedId}");
                return;
            }

            var runtimes = new List<QuestRuntimeData>(_activeQuests.Values);
            foreach (QuestRuntimeData runtime in runtimes)
            {
                foreach (QuestObjectiveData obj in runtime.QuestSO.objectives)
                {
                    if (obj.type != QuestObjectiveType.StoryEvent
                        || obj.targetStringId != normalizedId
                        || runtime.IsObjectiveComplete(obj))
                        continue;

                    runtime.SetProgress(obj.objectiveId, obj.requiredCount);
                    SendObjectiveEvent(runtime, obj);
                    TryAutoComplete(runtime);
                }
            }
        }

        /// <summary>
        /// 현재 사이클에서 처치한 외곽 수호자 수를 퀘스트 목표에 반영한다.
        /// 사이클마다 새 메인 퀘스트가 활성화되므로 절대 누적값이 아니라 현재 런의 수를 사용한다.
        /// </summary>
        public void NotifyCycleOuterBossProgress(int defeatedCount)
        {
            int safeCount = Mathf.Max(0, defeatedCount);
            var runtimes = new List<QuestRuntimeData>(_activeQuests.Values);
            foreach (QuestRuntimeData runtime in runtimes)
            {
                foreach (QuestObjectiveData obj in runtime.QuestSO.objectives)
                {
                    if (obj.type != QuestObjectiveType.CycleOuterBoss || runtime.IsObjectiveComplete(obj))
                        continue;

                    runtime.SetProgress(obj.objectiveId, Mathf.Min(safeCount, obj.requiredCount));
                    SendObjectiveEvent(runtime, obj);
                    TryAutoComplete(runtime);
                }
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 내부 유틸

        private void UpdateObjectives(
            QuestObjectiveType type,
            int targetId,
            int count,
            Func<QuestObjectiveData, QuestRuntimeData, int> customUpdate = null)
        {
            // _activeQuests 스냅샷으로 반복: TryAutoComplete → CompleteQuestById 가
            // _activeQuests에서 항목을 제거해도 InvalidOperationException이 발생하지 않도록 함
            var runtimes = new List<QuestRuntimeData>(_activeQuests.Values);
            foreach (var runtime in runtimes)
            {
                foreach (var obj in runtime.QuestSO.objectives)
                {
                    if (obj.type != type) continue;
                    if (obj.targetId != targetId) continue;
                    if (runtime.IsObjectiveComplete(obj)) continue;

                    int newCount;
                    if (customUpdate != null)
                    {
                        newCount = customUpdate(obj, runtime);
                        if (newCount == -1) continue;
                    }
                    else
                    {
                        newCount = runtime.AddProgress(obj.objectiveId, count);
                    }

                    SendObjectiveEvent(runtime, obj);
                    TryAutoComplete(runtime);
                }
            }
        }

        private QuestSO ResolveQuestDefinition(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            if (_runtimeQuestDefinitions.TryGetValue(questId, out QuestSO runtimeQuest))
                return runtimeQuest;
            return IsDBLoaded && _db != null ? _db.GetQuest(questId) : null;
        }

        private void RemoveRuntimeQuestState(string questId)
        {
            _activeQuests.Remove(questId);
            _completedQuestIds.Remove(questId);
            _failedQuestIds.Remove(questId);
            _pendingAcceptQuestIds.Remove(questId);
            if (!string.Equals(_trackedQuestId, questId, StringComparison.Ordinal)) return;
            _trackedQuestId = null;
        }

        private void PublishRuntimeTrackingChange(
            string previousTrackedQuestId,
            bool forceRefresh = false)
        {
            // 사용자가 명시적으로 추적을 끈 상태라면 런타임 정의 정리가
            // 임의로 다른 퀘스트 추적을 다시 켜서는 안 된다.
            if (string.IsNullOrEmpty(previousTrackedQuestId)
                && _isQuestTrackingSuppressed)
            {
                return;
            }

            if (string.IsNullOrEmpty(_trackedQuestId))
                TrackFirstActiveQuest(false);
            if (!forceRefresh
                && string.Equals(
                    previousTrackedQuestId,
                    _trackedQuestId,
                    StringComparison.Ordinal))
            {
                return;
            }

            if (!string.IsNullOrEmpty(_trackedQuestId)
                && _activeQuests.TryGetValue(
                    _trackedQuestId,
                    out QuestRuntimeData runtime)
                && runtime?.QuestSO != null)
            {
                SendQuestEvent(
                    QuestEvent.QuestTracked,
                    _trackedQuestId,
                    runtime.QuestSO.questName);
                return;
            }

            if (!string.IsNullOrEmpty(previousTrackedQuestId))
                SendQuestEvent(QuestEvent.QuestUntracked, previousTrackedQuestId);
        }

        private void DestroyRuntimeQuestDefinitions()
        {
            foreach (QuestSO definition in _runtimeQuestDefinitions.Values)
                DestroyRuntimeQuestDefinition(definition);
            _runtimeQuestDefinitions.Clear();
        }

        private static void DestroyRuntimeQuestDefinition(QuestSO definition)
        {
            if (definition == null) return;
            if (Application.isPlaying) Destroy(definition);
            else DestroyImmediate(definition);
        }

        private void RefreshItemCollectObjectives(QuestRuntimeData runtime)
        {
            foreach (var obj in runtime.QuestSO.objectives)
            {
                if (obj.type != QuestObjectiveType.ItemCollect) continue;
                int have    = InventoryManager.Instance.GetItemCount(obj.targetId);
                int clamped = Mathf.Min(have, obj.requiredCount);
                if (clamped > 0)
                {
                    runtime.SetProgress(obj.objectiveId, clamped);
                    SendObjectiveEvent(runtime, obj);
                }
            }
            TryAutoComplete(runtime);
        }

        private static bool IsMonsterKillTargetMatch(QuestObjectiveData obj, string actorId)
        {
            if (!string.IsNullOrEmpty(obj.targetStringId))
            {
                return obj.targetStringId == actorId;
            }

            return int.TryParse(actorId, out int numericActorId) && obj.targetId == numericActorId;
        }

        private bool CheckPrerequisites(QuestSO questSO)
        {
            foreach (var reqId in questSO.requiredQuestIds)
                if (!_completedQuestIds.Contains(reqId))
                    return false;

            if (questSO.requiredStoryProgress > 0 &&
                StoryManager.Instance.CurrentProgress < questSO.requiredStoryProgress)
                return false;

            return true;
        }

        private void GiveRewards(QuestRewardData reward)
        {
            if (reward == null) return;
            if (reward.gold > 0 && !InventoryManager.Instance.TryAddGold(reward.gold))
                Debug.LogWarning($"[QuestManager] 골드 보상 지급 실패: amount={reward.gold}");
            if (reward.exp > 0)
                PartyManager.Instance?.AwardBattleExp(reward.exp);
            foreach (var itemReward in reward.items)
                if (itemReward.count > 0)
                    InventoryManager.Instance.AddItem(itemReward.itemId, itemReward.count);
        }

        // TryAutoComplete는 string 기반 내부 완료 처리를 호출
        private void TryAutoComplete(QuestRuntimeData runtime)
        {
            if (!runtime.QuestSO.autoComplete) return;
            if (!runtime.AreAllObjectivesComplete()) return;
            CompleteQuestById(runtime.QuestSO.questId);
        }

        private void AutoAcceptNextQuests(QuestSO completedQuest)
        {
            if (completedQuest?.autoAcceptNextQuestIds == null ||
                completedQuest.autoAcceptNextQuestIds.Count == 0)
            {
                Debug.Log($"[QuestManager] 자동 연계 퀘스트 없음: {completedQuest?.questId}");
                return;
            }

            if (_autoAcceptChainDepth >= MAX_AUTO_ACCEPT_CHAIN_DEPTH)
            {
                Debug.LogWarning($"[QuestManager] 자동 연계 깊이 제한 초과: {completedQuest.questId}");
                return;
            }

            _autoAcceptChainDepth++;
            try
            {
                foreach (var nextQuestId in completedQuest.autoAcceptNextQuestIds)
                {
                    if (string.IsNullOrEmpty(nextQuestId))
                    {
                        continue;
                    }

                    if (nextQuestId == completedQuest.questId)
                    {
                        Debug.LogWarning($"[QuestManager] 자기 자신으로 자동 연계할 수 없습니다: {completedQuest.questId}");
                        continue;
                    }

                    Debug.Log($"[QuestManager] 자동 연계 퀘스트 수락 시도: {completedQuest.questId} -> {nextQuestId}");
                    if (!AcceptQuestById(nextQuestId))
                    {
                        Debug.LogWarning($"[QuestManager] 자동 연계 퀘스트 수락 실패: {completedQuest.questId} -> {nextQuestId}");
                    }
                }
            }
            finally
            {
                _autoAcceptChainDepth--;
            }
        }

        private void TryAutoAcceptNewGameQuests()
        {
            if (!_pendingNewGameAutoAccept || !IsDBLoaded || _db == null)
                return;

            _pendingNewGameAutoAccept = false;
            var pending = new List<QuestSO>();
            foreach (var quest in _db.GetAllQuests())
            {
                if (quest == null || !quest.autoAcceptOnNewGame)
                    continue;

                pending.Add(quest);
            }

            bool acceptedAny;
            do
            {
                acceptedAny = false;
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    QuestSO quest = pending[i];
                    if (!CheckPrerequisites(quest))
                        continue;

                    if (AcceptQuestById(quest.questId))
                    {
                        pending.RemoveAt(i);
                        acceptedAny = true;
                    }
                }
            }
            while (acceptedAny && pending.Count > 0);

            for (int i = 0; i < pending.Count; i++)
                Debug.LogWarning($"[QuestManager] 새 게임 자동 퀘스트 수락 실패: {pending[i].questId} (선행 조건 미충족)");
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 이벤트 발송

        private void SendQuestEvent(QuestEvent eventType, string questId)
        {
            SendQuestEvent(eventType, questId, null);
        }

        private void SendQuestEvent(QuestEvent eventType, string questId, string questName)
        {
            EventManager.Instance.Send<QuestEvent, QuestStateEventData>(
                eventType,
                new QuestStateEventData
                {
                    QuestId = questId,
                    QuestName = questName
                });
        }

        private void SendObjectiveEvent(QuestRuntimeData runtime, QuestObjectiveData obj)
        {
            int current = runtime.ObjectiveProgress.TryGetValue(obj.objectiveId, out var c) ? c : 0;
            EventManager.Instance.Send<QuestEvent, QuestObjectiveEventData>(
                QuestEvent.QuestObjectiveUpdated,
                new QuestObjectiveEventData
                {
                    QuestId       = runtime.QuestSO.questId,
                    ObjectiveId   = obj.objectiveId,
                    CurrentCount  = current,
                    RequiredCount = obj.requiredCount
                });
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region ISaveable
        // 세이브/로드는 string ID로 직렬화 (enum 재생성과 무관하게 안정적)

        public void ExportSaveData(GameSaveData saveData)
        {
            saveData.quest.completedQuestIds = new List<string>();
            foreach (string id in _completedQuestIds)
                if (!_runtimeQuestDefinitions.ContainsKey(id)) saveData.quest.completedQuestIds.Add(id);
            saveData.quest.failedQuestIds = new List<string>();
            foreach (string id in _failedQuestIds)
                if (!_runtimeQuestDefinitions.ContainsKey(id)) saveData.quest.failedQuestIds.Add(id);
            saveData.quest.trackedQuestId = !_runtimeQuestDefinitions.ContainsKey(_trackedQuestId ?? string.Empty)
                ? _trackedQuestId
                : null;
            saveData.quest.questTrackingSuppressed = _isQuestTrackingSuppressed;

            saveData.quest.activeQuests.Clear();
            foreach (var kv in _activeQuests)
            {
                if (_runtimeQuestDefinitions.ContainsKey(kv.Key)) continue;
                saveData.quest.activeQuests.Add(new ActiveQuestSaveEntry
                {
                    questId           = kv.Key,
                    objectiveProgress = new Dictionary<string, int>(kv.Value.ObjectiveProgress)
                });
            }
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _pendingNewGameAutoAccept = false;
            _pendingLoad = saveData.quest;
            if (IsDBLoaded) ApplyPendingLoad();
        }

        public void ResetForNewGame()
        {
            // 신규 실행 직후와 동일하게 런타임 상태를 비운다(DB는 유지).
            _pendingLoad = null;
            _activeQuests.Clear();
            _completedQuestIds.Clear();
            _failedQuestIds.Clear();
            _pendingAcceptQuestIds.Clear();
            _pendingReachedLocationIds.Clear();
            _pendingStoryEventIds.Clear();
            _trackedQuestId = null;
            _isQuestTrackingSuppressed = false;
            _autoAcceptChainDepth = 0;
            _pendingNewGameAutoAccept = true;
            DestroyRuntimeQuestDefinitions();
        }

        private void ApplyPendingLoad()
        {
            var data = _pendingLoad;
            _pendingLoad = null;

            _completedQuestIds.Clear();
            foreach (var id in data.completedQuestIds ?? new List<string>())
                _completedQuestIds.Add(id);

            _failedQuestIds.Clear();
            foreach (var id in data.failedQuestIds ?? new List<string>())
                _failedQuestIds.Add(id);

            _activeQuests.Clear();
            foreach (var entry in data.activeQuests ?? new List<ActiveQuestSaveEntry>())
            {
                var questSO = _db.GetQuest(entry.questId);
                if (questSO == null)
                {
                    Debug.LogWarning($"[QuestManager] 세이브된 퀘스트 ID '{entry.questId}'가 현재 DB에 없습니다. 무시합니다.");
                    continue;
                }

                var runtime = new QuestRuntimeData(questSO) { Status = QuestStatus.Active };
                foreach (var kv in entry.objectiveProgress ?? new Dictionary<string, int>())
                    if (runtime.ObjectiveProgress.ContainsKey(kv.Key))
                        runtime.SetProgress(kv.Key, kv.Value);

                _activeQuests[entry.questId] = runtime;
            }

            _trackedQuestId = null;
            if (!string.IsNullOrEmpty(data.trackedQuestId) && _activeQuests.ContainsKey(data.trackedQuestId))
            {
                _trackedQuestId = data.trackedQuestId;
                _isQuestTrackingSuppressed = false;
            }
            else if (data.questTrackingSuppressed)
            {
                _isQuestTrackingSuppressed = true;
            }
            else
            {
                _isQuestTrackingSuppressed = false;
                TrackFirstActiveQuest(false);
            }

            Debug.Log($"[QuestManager] 로드 완료 — 완료: {_completedQuestIds.Count}개, 진행 중: {_activeQuests.Count}개");
        }

        private void FlushPendingQuestRequests()
        {
            if (_pendingAcceptQuestIds.Count > 0)
            {
                var questIds = new List<string>(_pendingAcceptQuestIds);
                _pendingAcceptQuestIds.Clear();

                foreach (var questId in questIds)
                    AcceptQuestById(questId);
            }

            if (_pendingReachedLocationIds.Count > 0)
            {
                var locationIds = new List<string>(_pendingReachedLocationIds);
                _pendingReachedLocationIds.Clear();

                foreach (var locationId in locationIds)
                    NotifyLocationReached(locationId);
            }

            if (_pendingStoryEventIds.Count > 0)
            {
                var eventIds = new List<string>(_pendingStoryEventIds);
                _pendingStoryEventIds.Clear();

                foreach (string eventId in eventIds)
                    NotifyStoryEvent(eventId);
            }
        }

        #endregion
    }
}
