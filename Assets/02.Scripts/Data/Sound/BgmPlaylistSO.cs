using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Sound
{
    /// <summary>
    /// 한 씬/지역에서 여러 BGM을 번갈아 재생하기 위한 플레이리스트 정의.
    /// SoundManager가 한 트랙을 끝까지(loop=false) 재생한 뒤, 곡 사이에 무음 간격(gap)을 두고
    /// 다음 트랙으로 넘어간다(순차/셔플). 상용 게임의 "탐험 BGM"(트랙 → 무음 → 다음 트랙) 패턴.
    ///
    /// 각 항목은 SoundDatabase의 BGM key를 가리킨다(단일 곡 PlayBgm과 동일한 key 공간).
    /// </summary>
    [CreateAssetMenu(fileName = "BgmPlaylist_", menuName = "UPlayGround/오디오/BGM Playlist")]
    public sealed class BgmPlaylistSO : ScriptableObject
    {
        public enum PlaybackMode
        {
            [Tooltip("목록 순서대로 재생하고 끝나면 처음으로 순환")]
            Sequential,

            [Tooltip("무작위 재생(직전 곡 즉시 반복은 회피)")]
            Shuffle,
        }

        [Tooltip("재생할 BGM의 SoundDatabase key 목록")]
        [SerializeField] private List<string> _bgmKeys = new();

        [SerializeField] private PlaybackMode _mode = PlaybackMode.Sequential;

        [Header("곡 사이 무음 간격 (초)")]
        [Tooltip("한 곡이 끝난 뒤 다음 곡 시작까지의 무음 시간 최소값. 0이면 곡을 끊김 없이 바로 이어 재생.")]
        [SerializeField] private float _gapMin = 20f;

        [Tooltip("무음 시간 최대값. min과 같으면 고정 간격.")]
        [SerializeField] private float _gapMax = 45f;

        [Header("페이드")]
        [Tooltip("각 트랙 시작 시 페이드 인 시간(초). 곡 종료는 자연 종료(loop=false)이므로 별도 페이드아웃 없음.")]
        [SerializeField] private float _trackFadeTime = 1.5f;

        public PlaybackMode Mode => _mode;
        public int Count => _bgmKeys?.Count ?? 0;
        public float TrackFadeTime => Mathf.Max(0f, _trackFadeTime);

        public string GetKey(int index)
        {
            if (_bgmKeys == null || index < 0 || index >= _bgmKeys.Count)
                return null;

            return _bgmKeys[index];
        }

        /// <summary>곡 사이 무음 간격을 결정한다. min/max 사이 무작위.</summary>
        public float GetRandomGap()
        {
            float min = Mathf.Max(0f, Mathf.Min(_gapMin, _gapMax));
            float max = Mathf.Max(0f, Mathf.Max(_gapMin, _gapMax));

            if (Mathf.Approximately(min, max))
                return min;

            return Random.Range(min, max);
        }

        /// <summary>
        /// 현재 인덱스를 기준으로 다음 재생할 트랙 인덱스를 반환한다.
        /// current가 음수면 시작 인덱스로 간주하며, 곡이 2개 이상이면 Sequential/Shuffle 모두 무작위로 시작한다
        /// (씬 재진입 시 항상 같은 곡으로 시작하는 단조로움 방지).
        /// 이후 진행은 Sequential은 순서대로 순환, Shuffle은 매번 무작위(직전 곡 즉시 반복 회피)다.
        /// </summary>
        public int GetNextIndex(int current)
        {
            int count = Count;
            if (count <= 0)
                return -1;

            if (count == 1)
                return 0;

            // 시작(첫 곡)은 모드와 무관하게 무작위.
            if (current < 0)
                return Random.Range(0, count);

            if (_mode == PlaybackMode.Shuffle)
            {
                // 직전 곡을 제외한 범위에서 선택(즉시 반복 회피).
                int offset = Random.Range(1, count);
                return (current + offset) % count;
            }

            // Sequential: 무작위 시작점에서 순서대로 순환.
            return (current + 1) % count;
        }
    }
}
