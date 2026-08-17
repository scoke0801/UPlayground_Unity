using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Dialogue;
using UPlayGround.Manager;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Save;

namespace UPlayGround.Story
{
    [System.Serializable]
    public class StoryState
    {
        public int progress;
        public List<string> completedStories;
        public List<UPlayGround.Data.Story.RecruitmentEncounterSaveEntry> recruitmentEncounters;
    }
    
    /// <summary>
    /// 게임 진행도를 관리하고, 스토리 이벤트의 트리거 여부를 판정합니다.
    /// - TryTriggerStory: 완료 여부 + 진행도 조건을 확인 후 DialogueManager에 전달
    /// - SetProgress: 진행도 변경 (보스 처치, 구역 진입 등 외부에서 호출)
    /// </summary>
    public partial class StoryManager : BaseManager<StoryManager>, IManager, ISaveable,
        IStoryFlowService, IRecruitmentEncounterService
    {
        private const string MainStorySequenceResourceKey = "MainStorySequence";
        private const string SelfEncounterGraphResourceKey = "Dialogue/DLG_CycleSelfEncounter";
        private const string SelfEncounterStoryPrefix = "cycle_self_encounter_";

        [SerializeField] private int _currentProgress;
        [SerializeField] private StorySequenceSO _mainStorySequence;
        private DialogueGraphSO _selfEncounterGraph;

        // 시작과 완료를 분리한다. 재생 중 저장/중단된 스토리는 완료로 소진하지 않는다.
        private readonly StoryPlaybackTracker _playbackTracker = new();
        private readonly Queue<StoryEntrySO> _pendingMainStories = new();
        private readonly HashSet<string> _pendingMainStoryIds = new();
        private System.IDisposable _activeMainStoryDialogue;
        private string _activeMainStoryId;
        private string _pendingSelfEncounterStoryId;
        private string _pendingSelfEncounterActorId;
        private int _playbackGeneration;

        // 자동 재생은 게임플레이 씬에서만 허용한다. Title/Boot/Loading에서 재생되면
        // 화자도 배경도 없이 대사가 뜨고, 시작과 동시에 완료로 소진돼 버린다.
        private string _currentSceneType;

        public int CurrentProgress => _currentProgress;

        #region IManager
        public void Init()
        {
            InitializeRecruitmentEncounters();
            if (_mainStorySequence == null)
                _mainStorySequence = Resources.Load<StorySequenceSO>(MainStorySequenceResourceKey);
            _selfEncounterGraph = Resources.Load<DialogueGraphSO>(SelfEncounterGraphResourceKey);
            SaveManager.Instance.RegisterSaveable(this);
        }

        public void AfterInit()
        {
            if (CycleRunManager.Instance != null)
            {
                CycleRunManager.Instance.OnBossDiscovered += OnCycleBossDiscovered;
                CycleRunManager.Instance.OnCycleCompleted += HandleRecruitmentCycleCompleted;
            }
        }

        public void Dispose()
        {
            DisposeRecruitmentEncounters();
            _playbackGeneration++;
            ClearPendingMainStories();
            if (CycleRunManager.Instance != null)
            {
                CycleRunManager.Instance.OnBossDiscovered -= OnCycleBossDiscovered;
                CycleRunManager.Instance.OnCycleCompleted -= HandleRecruitmentCycleCompleted;
            }
            _mainStorySequence = null;
            _selfEncounterGraph = null;
            _pendingSelfEncounterStoryId = null;
            _pendingSelfEncounterActorId = null;
        }

        public void OnUpdate()
        {
            if (!IsAutoPlayAllowed) return;
            if (TryPlayPendingSelfEncounter()) return;
            TryPlayNextMainStory();
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnSceneChanged(string sceneType)
        {
            _currentSceneType = sceneType;

            if (!IsAutoPlayAllowed)
            {
                // 게임플레이 밖으로 나갔으면 큐를 비운다. 남겨두면 다음 씬 진입 첫 프레임에
                // 조건 재평가 없이 그대로 재생된다.
                ClearPendingMainStories();
                return;
            }

            QueueEligibleMainStories();
        }
        #endregion

        private bool IsAutoPlayAllowed => _currentSceneType == SceneType.GamePlay;
        
        /// <summary>
        /// 진행도를 올립니다. 이전보다 낮은 값은 무시됩니다.
        /// </summary>
        public void SetProgress(int progress)
        {
            if (progress <= _currentProgress) return;
            _currentProgress = progress;
            QuestManager.Instance?.NotifyStoryProgress(_currentProgress);
            QueueEligibleMainStories();
            Debug.Log($"[Story] 진행도 변경: {_currentProgress}");
        }

        /// <summary>
        /// 트리거 존 등 외부에서 호출. 조건이 맞을 때만 대화를 시작합니다.
        /// </summary>
        /// <returns>대화가 실제로 시작되면 true</returns>
        public bool IsStoryEligible(StoryEntrySO entry)
        {
            if (entry == null) return false;
            if (!_playbackTracker.CanBegin(entry.storyId)) return false;
            if (!IsWithinProgressWindow(entry)) return false;
            return ResolveGraph(entry) != null;
        }

        public bool TryTriggerStory(StoryEntrySO entry)
        {
            if (entry == null) return false;
            if (!_playbackTracker.CanBegin(entry.storyId)) return false;
            if (!IsWithinProgressWindow(entry)) return false;

            var graph = ResolveGraph(entry);
            if (graph == null)
            {
                Debug.LogWarning($"[Story] '{entry.storyId}' 에 유효한 대화 그래프 없음");
                return false;
            }

            string storyId = entry.storyId;
            if (!_playbackTracker.TryBegin(storyId))
                return false;

            int generation = _playbackGeneration;
            System.IDisposable subscription = Svc.Dialogue?.TryStartDialogueTracked(
                graph,
                () => CompleteStoryPlayback(storyId, generation),
                onCancelled: () => CancelStoryPlayback(storyId, generation));
            if (subscription == null)
            {
                _playbackTracker.Cancel(storyId);
                return false;
            }

            return true;
        }

        public bool IsCompleted(string storyId) => _playbackTracker.IsCompleted(storyId);

        // ── 세이브 / 로드 ────────────────────────────────────────────

        public StoryState ExportState() => new()
        {
            progress = _currentProgress,
            completedStories = new List<string>(_playbackTracker.CompletedStoryIds),
            recruitmentEncounters = _recruitmentStateStore.Export(),
        };

        public void ImportState(StoryState state)
        {
            _playbackGeneration++;
            _currentProgress = state.progress;
            _playbackTracker.RestoreCompleted(state.completedStories);
            _recruitmentStateStore.Import(state.recruitmentEncounters);
            RestoreRegisteredRecruitmentDefinitions();
            _pendingSelfEncounterStoryId = null;
            _pendingSelfEncounterActorId = null;
            ClearPendingMainStories();
            QueueEligibleMainStories();
        }

        #region ISaveable

        public void ExportSaveData(GameSaveData saveData)
        {
            saveData.story.progress = _currentProgress;
            saveData.story.completedStories = new List<string>(_playbackTracker.CompletedStoryIds);
            saveData.story.recruitmentEncounters = _recruitmentStateStore.Export();
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _playbackGeneration++;
            _currentProgress = saveData.story.progress;
            _playbackTracker.RestoreCompleted(saveData.story.completedStories);
            _recruitmentStateStore.Import(saveData.story.recruitmentEncounters);
            RestoreRegisteredRecruitmentDefinitions();
            _pendingSelfEncounterStoryId = null;
            _pendingSelfEncounterActorId = null;
            ClearPendingMainStories();
            QueueEligibleMainStories();
        }

        public void ResetForNewGame()
        {
            _playbackGeneration++;
            _currentProgress = 0;
            _playbackTracker.Clear();
            ResetRecruitmentEncountersForNewGame();
            ClearPendingMainStories();
            _pendingSelfEncounterStoryId = null;
            _pendingSelfEncounterActorId = null;
        }

        #endregion

        // ── 내부 ────────────────────────────────────────────────────

        // variants 중 현재 진행도에 맞는 가장 높은 그래프를 반환.
        // 없으면 기본 dialogueGraph 사용.
        private void OnCycleBossDiscovered(UPlayGround.Data.Cycle.CycleBossPlacement placement)
        {
            if (placement == null || _selfEncounterGraph == null || Svc.Party == null)
                return;

            CharacterActorType opponent = placement.actorId switch
            {
                "MonsterBokusei" => CharacterActorType.Bokusei,
                "MonsterHonoka" => CharacterActorType.Honoka,
                "MonsterHichi" => CharacterActorType.Hichi,
                "MonsterLili" => CharacterActorType.Lili,
                _ => CharacterActorType.None,
            };
            if (opponent == CharacterActorType.None || opponent != Svc.Party.StoryProtagonistType)
                return;

            string storyId = SelfEncounterStoryPrefix + opponent;
            if (_playbackTracker.CanBegin(storyId))
            {
                _pendingSelfEncounterStoryId = storyId;
                _pendingSelfEncounterActorId = placement.actorId;
            }
        }

        private bool TryPlayPendingSelfEncounter()
        {
            if (string.IsNullOrEmpty(_pendingSelfEncounterStoryId) || _selfEncounterGraph == null)
                return false;

            string storyId = _pendingSelfEncounterStoryId;
            if (!_playbackTracker.TryBegin(storyId))
                return false;

            int generation = _playbackGeneration;
            System.IDisposable subscription = Svc.Dialogue?.TryStartDialogueTracked(
                _selfEncounterGraph,
                () => CompleteStoryPlayback(storyId, generation),
                partnerActorIdOverride: _pendingSelfEncounterActorId,
                onCancelled: () => CancelStoryPlayback(storyId, generation));
            if (subscription == null)
            {
                _playbackTracker.Cancel(storyId);
                return false;
            }

            _pendingSelfEncounterStoryId = null;
            _pendingSelfEncounterActorId = null;
            return true;
        }

        private DialogueGraphSO ResolveGraph(StoryEntrySO entry)
        {
            DialogueGraphSO best = null;
            int bestReq = -1;

            foreach (var v in entry.variants ?? System.Array.Empty<StoryVariant>())
            {
                if (_currentProgress >= v.requiredProgress && v.requiredProgress > bestReq)
                {
                    best = v.dialogueGraph;
                    bestReq = v.requiredProgress;
                }
            }

            return best != null ? best : entry.dialogueGraph;
        }

        private void QueueEligibleMainStories()
        {
            // SetProgress / 세이브 로드처럼 씬 전환 밖에서도 불린다. 여기서 한 번 더 막는다.
            if (!IsAutoPlayAllowed || _mainStorySequence?.entries == null)
                return;

            foreach (StoryEntrySO entry in _mainStorySequence.entries)
            {
                // NpcTalk/Zone 엔트리는 각자의 트리거가 소유한다. 자동 큐가 가로채면
                // 화자 없이 재생되고 완료로 소진돼 정작 해당 트리거에서 나오지 않는다.
                if (entry == null
                    || entry.triggerMode != StoryTriggerMode.Auto
                    || string.IsNullOrWhiteSpace(entry.storyId)
                    || !IsWithinProgressWindow(entry)
                    || !_playbackTracker.CanBegin(entry.storyId)
                    || !_pendingMainStoryIds.Add(entry.storyId))
                    continue;

                _pendingMainStories.Enqueue(entry);
            }

            TryPlayNextMainStory();
        }

        private bool IsWithinProgressWindow(StoryEntrySO entry)
        {
            return entry != null
                   && _currentProgress >= entry.requiredProgress
                   && (entry.maxProgressExclusive <= 0 || _currentProgress < entry.maxProgressExclusive);
        }

        private void TryPlayNextMainStory()
        {
            if (_activeMainStoryDialogue != null || _pendingMainStories.Count == 0)
                return;

            StoryEntrySO entry = _pendingMainStories.Peek();
            if (!IsWithinProgressWindow(entry))
            {
                RemovePendingMainStory(entry);
                TryPlayNextMainStory();
                return;
            }

            DialogueGraphSO graph = ResolveGraph(entry);
            if (graph == null)
            {
                Debug.LogWarning($"[Story] 자동 진행 '{entry.storyId}'에 유효한 대화 그래프 없음");
                RemovePendingMainStory(entry);
                return;
            }

            string storyId = entry.storyId;
            if (!_playbackTracker.TryBegin(storyId))
            {
                RemovePendingMainStory(entry);
                TryPlayNextMainStory();
                return;
            }

            int generation = _playbackGeneration;
            bool isStarting = true;
            bool completedSynchronously = false;
            System.IDisposable subscription = Svc.Dialogue?.TryStartDialogueTracked(
                graph,
                () =>
                {
                    CompleteStoryPlayback(storyId, generation);
                    if (isStarting)
                    {
                        completedSynchronously = true;
                        return;
                    }

                    OnMainStoryDialogueCompleted();
                },
                onCancelled: () => OnMainStoryDialogueCancelled(storyId, generation));
            isStarting = false;
            if (subscription == null)
            {
                _playbackTracker.Cancel(storyId);
                return;
            }

            RemovePendingMainStory(entry);

            if (completedSynchronously)
            {
                subscription.Dispose();
                return;
            }

            _activeMainStoryDialogue = subscription;
            _activeMainStoryId = storyId;
        }

        private void OnMainStoryDialogueCompleted()
        {
            _activeMainStoryDialogue = null;
            _activeMainStoryId = null;
            // DialogueRunner.End 콜백 안에서 다음 대화를 즉시 시작하면 이전 End의
            // 종료 통지가 새 대화 세션을 닫을 수 있다. 다음 OnUpdate에서 재생한다.
        }

        private void OnMainStoryDialogueCancelled(string storyId, int generation)
        {
            CancelStoryPlayback(storyId, generation);
            _activeMainStoryDialogue = null;
            _activeMainStoryId = null;
        }

        private void CompleteStoryPlayback(string storyId, int generation)
        {
            if (generation != _playbackGeneration)
                return;

            _playbackTracker.Complete(storyId);
        }

        private void CancelStoryPlayback(string storyId, int generation)
        {
            if (generation != _playbackGeneration)
                return;

            _playbackTracker.Cancel(storyId);
        }

        private void RemovePendingMainStory(StoryEntrySO entry)
        {
            _pendingMainStories.Dequeue();
            if (entry != null)
                _pendingMainStoryIds.Remove(entry.storyId);
        }

        private void ClearPendingMainStories()
        {
            _activeMainStoryDialogue?.Dispose();
            _activeMainStoryDialogue = null;
            if (!string.IsNullOrEmpty(_activeMainStoryId))
                _playbackTracker.Cancel(_activeMainStoryId);
            _activeMainStoryId = null;
            _pendingMainStories.Clear();
            _pendingMainStoryIds.Clear();
        }

    }

}
