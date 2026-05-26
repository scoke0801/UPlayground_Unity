using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Save;
using UPlayGround.Data.EnumType;
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
    ///   MonsterKill   → NotifyMonsterKill(monsterId)
    ///   StoryProgress → NotifyStoryProgress(progress)
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
    public class QuestManager : BaseManager<QuestManager>, IManager, ISaveable
    {
        private const string QUEST_DATABASE_KEY = "QuestDatabase";
        private const int MAX_AUTO_ACCEPT_CHAIN_DEPTH = 32;

        // ──── 데이터 ────
        private QuestDatabase _db;
        private AsyncOperationHandle<QuestDatabase> _dbHandle;

        // ──── 런타임 상태 (내부는 string 키로 관리) ────
        private readonly Dictionary<string, QuestRuntimeData> _activeQuests     = new();
        private readonly HashSet<string>                      _completedQuestIds = new();
        private readonly HashSet<string>                      _pendingAcceptQuestIds = new();
        private readonly HashSet<string>                      _pendingReachedLocationIds = new();

        // DB 로드 전에 LoadGame()이 호출될 경우 보관
        private QuestSaveData _pendingLoad;
        private int _autoAcceptChainDepth;

        public bool IsDBLoaded { get; private set; } = false;

        // ──────────────────────────────────────────────────────────
        #region IManager

        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
            LoadDatabaseAsync();
        }

        public void AfterInit() { }

        public void Dispose()
        {
            _activeQuests.Clear();
            _completedQuestIds.Clear();
            _pendingAcceptQuestIds.Clear();
            _pendingReachedLocationIds.Clear();
            if (_dbHandle.IsValid())
                Addressables.Release(_dbHandle);
        }

        public void OnUpdate()      { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }
        public void OnSceneChanged(string sceneType) { }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region DB 로드

        private async void LoadDatabaseAsync()
        {
            _dbHandle = Addressables.LoadAssetAsync<QuestDatabase>(QUEST_DATABASE_KEY);
            try
            {
                _db = await _dbHandle.Task;

                if (_db == null)
                {
                    Debug.LogError($"[QuestManager] '{QUEST_DATABASE_KEY}' Addressable을 찾을 수 없습니다.");
                    return;
                }

                _db.Initialize();
                IsDBLoaded = true;

                if (_pendingLoad != null)
                    ApplyPendingLoad();

                FlushPendingQuestRequests();

                Debug.Log("[QuestManager] QuestDatabase 로드 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuestManager] QuestDatabase 로드 실패: {e.Message}");
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

        /// <summary>
        /// 퀘스트를 완료 처리한다.
        /// autoComplete=false 퀘스트는 외부(UI 등)에서 명시적으로 호출해야 한다.
        /// </summary>
        public bool CompleteQuest(QuestIdType questId) => CompleteQuestById(questId.ToQuestId());

        /// <summary>진행 중인 퀘스트를 포기한다.</summary>
        public bool AbandonQuest(QuestIdType questId) => AbandonQuestById(questId.ToQuestId());

        /// <summary>퀘스트 현재 상태를 반환한다.</summary>
        public QuestStatus GetQuestStatus(QuestIdType questId) => GetQuestStatusById(questId.ToQuestId());

        /// <summary>퀘스트 완료 여부를 반환한다.</summary>
        public bool IsQuestCompleted(QuestIdType questId) => _completedQuestIds.Contains(questId.ToQuestId());

        /// <summary>퀘스트가 현재 진행 중인지 반환한다.</summary>
        public bool IsQuestActive(QuestIdType questId) => _activeQuests.ContainsKey(questId.ToQuestId());

        /// <summary>진행 중인 퀘스트의 런타임 데이터를 반환한다. 없으면 null.</summary>
        public QuestRuntimeData GetActiveQuestRuntime(QuestIdType questId)
        {
            _activeQuests.TryGetValue(questId.ToQuestId(), out var r);
            return r;
        }

        /// <summary> 진행 중인 모든 퀘스트 런타임 데이터 </summary>
        public IEnumerable<QuestRuntimeData> GetActiveQuests() => _activeQuests.Values;

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
                if (!CheckPrerequisites(q)) continue;
                result.Add(q);
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

            if (!IsDBLoaded)
            {
                _pendingAcceptQuestIds.Add(questId);
                Debug.Log($"[QuestManager] DB 로드 전 퀘스트 수락 요청 보류: {questId}");
                return true;
            }

            var questSO = _db.GetQuest(questId);
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

            if (!CheckPrerequisites(questSO))
            {
                Debug.LogWarning($"[QuestManager] 선행 조건 미충족: {questId}");
                return false;
            }

            var runtime = new QuestRuntimeData(questSO) { Status = QuestStatus.Active };
            _activeQuests[questId] = runtime;

            // ItemCollect 목표는 수락 시 인벤토리 현황으로 즉시 갱신
            RefreshItemCollectObjectives(runtime);

            SendQuestEvent(QuestEvent.QuestAccepted, questId);
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

            GiveRewards(runtime.QuestSO.reward);
            SendQuestEvent(QuestEvent.QuestCompleted, questId);
            Debug.Log($"[QuestManager] 퀘스트 완료: {runtime.QuestSO.questName}");

            AutoAcceptNextQuests(runtime.QuestSO);
            return true;
        }

        private bool AbandonQuestById(string questId)
        {
            if (!_activeQuests.TryGetValue(questId, out var runtime)) return false;
            _activeQuests.Remove(questId);
            Debug.Log($"[QuestManager] 퀘스트 포기: {runtime.QuestSO.questName}");
            return true;
        }

        private QuestStatus GetQuestStatusById(string questId)
        {
            if (_activeQuests.TryGetValue(questId, out var runtime))
                return runtime.Status;
            if (_completedQuestIds.Contains(questId))
                return QuestStatus.Completed;
            return QuestStatus.Available;
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
        /// 몬스터 처치 시 호출.
        /// 연결 위치: EnemyCombat 또는 MonsterActor 사망 처리
        /// </summary>
        public void NotifyMonsterKill(int monsterId)
        {
            UpdateObjectives(QuestObjectiveType.MonsterKill, monsterId, 1);
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
            if (reward.gold > 0)
                InventoryManager.Instance.Gold += reward.gold;
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

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 이벤트 발송

        private void SendQuestEvent(QuestEvent eventType, string questId)
        {
            EventManager.Instance.Send<QuestEvent, QuestStateEventData>(
                eventType,
                new QuestStateEventData { QuestId = questId });
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
            saveData.quest.completedQuestIds = new List<string>(_completedQuestIds);

            saveData.quest.activeQuests.Clear();
            foreach (var kv in _activeQuests)
            {
                saveData.quest.activeQuests.Add(new ActiveQuestSaveEntry
                {
                    questId           = kv.Key,
                    objectiveProgress = new Dictionary<string, int>(kv.Value.ObjectiveProgress)
                });
            }
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _pendingLoad = saveData.quest;
            if (IsDBLoaded) ApplyPendingLoad();
        }

        private void ApplyPendingLoad()
        {
            var data = _pendingLoad;
            _pendingLoad = null;

            _completedQuestIds.Clear();
            foreach (var id in data.completedQuestIds ?? new List<string>())
                _completedQuestIds.Add(id);

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
        }

        #endregion
    }
}
