using UPlayGround.Data.Sound;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// BgmEvent 발화 시 함께 전달하는 페이로드.
    /// Restore/Stop은 bgmKey/playlist를 무시하고 fadeTime만 사용한다.
    /// </summary>
    public sealed class BgmRequestData : IEventData
    {
        /// <summary>재생할 BGM의 SoundDatabase key. (Change / Override 전용)</summary>
        public string bgmKey;

        /// <summary>재생할 BGM 플레이리스트. 설정 시 bgmKey보다 우선. (Change / Override 전용)</summary>
        public BgmPlaylistSO playlist;

        /// <summary>크로스페이드/페이드아웃 시간(초).</summary>
        public float fadeTime = 1.5f;
    }
}
