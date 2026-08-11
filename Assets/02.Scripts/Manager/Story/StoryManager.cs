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
    }
    
    /// <summary>
    /// 게임 진행도를 관리하고, 스토리 이벤트의 트리거 여부를 판정합니다.
    /// - TryTriggerStory: 완료 여부 + 진행도 조건을 확인 후 DialogueManager에 전달
    /// - SetProgress: 진행도 변경 (보스 처치, 구역 진입 등 외부에서 호출)
    /// </summary>
    public class StoryManager : BaseManager<StoryManager>, IManager, ISaveable, IStoryFlowService
    {
        private const string MainStorySequenceResourceKey = "MainStorySequence";

        [SerializeField] private int _currentProgress;
        [SerializeField] private StorySequenceSO _mainStorySequence;

        // 완료된 storyId 집합. 세이브/로드 시 이 데이터를 직렬화.
        private readonly HashSet<string> _completedStories = new();
        private readonly Queue<StoryEntrySO> _pendingMainStories = new();
        private readonly HashSet<string> _pendingMainStoryIds = new();
        private System.IDisposable _activeMainStoryDialogue;

        // 자동 재생은 게임플레이 씬에서만 허용한다. Title/Boot/Loading에서 재생되면
        // 화자도 배경도 없이 대사가 뜨고, 시작과 동시에 완료로 소진돼 버린다.
        private string _currentSceneType;

        public int CurrentProgress => _currentProgress;

        #region IManager
        public void Init()
        {
            if (_mainStorySequence == null)
                _mainStorySequence = Resources.Load<StorySequenceSO>(MainStorySequenceResourceKey);
            SaveManager.Instance.RegisterSaveable(this);
        }

        public void AfterInit()
        {
        }

        public void Dispose()
        {
            ClearPendingMainStories();
            _mainStorySequence = null;
        }

        public void OnUpdate()
        {
            if (!IsAutoPlayAllowed) return;
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
            if (_completedStories.Contains(entry.storyId)) return false;
            if (!IsWithinProgressWindow(entry)) return false;
            return ResolveGraph(entry) != null;
        }

        public bool TryTriggerStory(StoryEntrySO entry)
        {
            if (entry == null) return false;
            if (_completedStories.Contains(entry.storyId)) return false;
            if (!IsWithinProgressWindow(entry)) return false;

            var graph = ResolveGraph(entry);
            if (graph == null)
            {
                Debug.LogWarning($"[Story] '{entry.storyId}' 에 유효한 대화 그래프 없음");
                return false;
            }

            System.IDisposable subscription = Svc.Dialogue?.TryStartDialogueTracked(graph, null);
            if (subscription == null)
                return false;

            // 실제 Runner가 요청을 수락한 뒤 완료로 등록해 바쁜 Main 채널에서 유실되지 않게 한다.
            _completedStories.Add(entry.storyId);
            return true;
        }

        public bool IsCompleted(string storyId) => _completedStories.Contains(storyId);

        // ── 세이브 / 로드 ────────────────────────────────────────────

        public StoryState ExportState() => new()
        {
            progress = _currentProgress,
            completedStories = new List<string>(_completedStories)
        };

        public void ImportState(StoryState state)
        {
            _currentProgress = state.progress;
            _completedStories.Clear();
            foreach (var id in state.completedStories)
                _completedStories.Add(id);
            ClearPendingMainStories();
            QueueEligibleMainStories();
        }

        #region ISaveable

        public void ExportSaveData(GameSaveData saveData)
        {
            saveData.story.progress = _currentProgress;
            saveData.story.completedStories = new List<string>(_completedStories);
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _currentProgress = saveData.story.progress;
            _completedStories.Clear();
            foreach (var id in saveData.story.completedStories ?? new List<string>())
                _completedStories.Add(id);
            ClearPendingMainStories();
            QueueEligibleMainStories();
        }

        public void ResetForNewGame()
        {
            _currentProgress = 0;
            _completedStories.Clear();
            ClearPendingMainStories();
        }

        #endregion

        // ── 내부 ────────────────────────────────────────────────────

        // variants 중 현재 진행도에 맞는 가장 높은 그래프를 반환.
        // 없으면 기본 dialogueGraph 사용.
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
                    || _completedStories.Contains(entry.storyId)
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

            System.IDisposable subscription = Svc.Dialogue?.TryStartDialogueTracked(
                graph,
                OnMainStoryDialogueCompleted);
            if (subscription == null)
                return;

            _activeMainStoryDialogue = subscription;
            _completedStories.Add(entry.storyId);
            RemovePendingMainStory(entry);
        }

        private void OnMainStoryDialogueCompleted()
        {
            _activeMainStoryDialogue = null;
            TryPlayNextMainStory();
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
            _pendingMainStories.Clear();
            _pendingMainStoryIds.Clear();
        }

    }

}
