using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Dialogue;
using UPlayGround.Manager;
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
        [SerializeField] private int _currentProgress;

        // 완료된 storyId 집합. 세이브/로드 시 이 데이터를 직렬화.
        private readonly HashSet<string> _completedStories = new();

        public int CurrentProgress => _currentProgress;

        #region IManager
        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
        }

        public void AfterInit()
        {
        }

        public void Dispose()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnSceneChanged(string sceneType)
        {
        }
        #endregion
        
        /// <summary>
        /// 진행도를 올립니다. 이전보다 낮은 값은 무시됩니다.
        /// </summary>
        public void SetProgress(int progress)
        {
            if (progress <= _currentProgress) return;
            _currentProgress = progress;
            QuestManager.Instance?.NotifyStoryProgress(_currentProgress);
            Debug.Log($"[Story] 진행도 변경: {_currentProgress}");
        }

        /// <summary>
        /// 트리거 존 등 외부에서 호출. 조건이 맞을 때만 대화를 시작합니다.
        /// </summary>
        /// <returns>대화가 실제로 시작되면 true</returns>
        public bool TryTriggerStory(StoryEntrySO entry)
        {
            if (entry == null) return false;
            if (_completedStories.Contains(entry.storyId)) return false;
            if (_currentProgress < entry.requiredProgress) return false;

            var graph = ResolveGraph(entry);
            if (graph == null)
            {
                Debug.LogWarning($"[Story] '{entry.storyId}' 에 유효한 대화 그래프 없음");
                return false;
            }

            // 완료로 등록 먼저 — 대화 중 재트리거 방지
            _completedStories.Add(entry.storyId);

            DialogueManager.Instance.StartDialogue(graph);
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
        }

        public void ResetForNewGame()
        {
            _currentProgress = 0;
            _completedStories.Clear();
        }

        #endregion

        // ── 내부 ────────────────────────────────────────────────────

        // variants 중 현재 진행도에 맞는 가장 높은 그래프를 반환.
        // 없으면 기본 dialogueGraph 사용.
        private DialogueGraphSO ResolveGraph(StoryEntrySO entry)
        {
            DialogueGraphSO best = null;
            int bestReq = -1;

            foreach (var v in entry.variants)
            {
                if (_currentProgress >= v.requiredProgress && v.requiredProgress > bestReq)
                {
                    best = v.dialogueGraph;
                    bestReq = v.requiredProgress;
                }
            }

            return best != null ? best : entry.dialogueGraph;
        }

    }

}
