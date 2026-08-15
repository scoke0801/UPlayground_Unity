using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Story
{
    /// <summary>
    /// 진행도 상승만으로 순서대로 재생할 스토리 Entry 목록.
    /// 월드 트리거가 필요한 서브 스토리와 분리해 메인 진행 이벤트에만 사용한다.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/스토리/진행 시퀀스", fileName = "StorySequence_")]
    public class StorySequenceSO : ScriptableObject
    {
        public List<StoryEntrySO> entries = new();
    }

    /// <summary>
    /// 스토리의 재생 중/완료 상태를 분리해 관리한다.
    /// 대화가 시작됐다는 이유만으로 완료 처리하지 않으며, 취소된 재생은 다시 시작할 수 있다.
    /// </summary>
    public sealed class StoryPlaybackTracker
    {
        private readonly HashSet<string> _playingStoryIds = new(System.StringComparer.Ordinal);
        private readonly HashSet<string> _completedStoryIds = new(System.StringComparer.Ordinal);

        public IEnumerable<string> CompletedStoryIds => _completedStoryIds;

        public bool IsCompleted(string storyId)
        {
            return !string.IsNullOrWhiteSpace(storyId) && _completedStoryIds.Contains(storyId);
        }

        public bool IsPlaying(string storyId)
        {
            return !string.IsNullOrWhiteSpace(storyId) && _playingStoryIds.Contains(storyId);
        }

        public bool CanBegin(string storyId)
        {
            return !string.IsNullOrWhiteSpace(storyId)
                   && !_completedStoryIds.Contains(storyId)
                   && !_playingStoryIds.Contains(storyId);
        }

        public bool TryBegin(string storyId)
        {
            return CanBegin(storyId) && _playingStoryIds.Add(storyId);
        }

        public bool Complete(string storyId)
        {
            if (string.IsNullOrWhiteSpace(storyId) || !_playingStoryIds.Remove(storyId))
                return false;

            _completedStoryIds.Add(storyId);
            return true;
        }

        public bool Cancel(string storyId)
        {
            return !string.IsNullOrWhiteSpace(storyId) && _playingStoryIds.Remove(storyId);
        }

        public void RestoreCompleted(IEnumerable<string> completedStoryIds)
        {
            _playingStoryIds.Clear();
            _completedStoryIds.Clear();

            if (completedStoryIds == null)
                return;

            foreach (string storyId in completedStoryIds)
            {
                if (!string.IsNullOrWhiteSpace(storyId))
                    _completedStoryIds.Add(storyId);
            }
        }

        public void Clear()
        {
            _playingStoryIds.Clear();
            _completedStoryIds.Clear();
        }
    }
}
