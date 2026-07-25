using UnityEngine;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 대화 이력(Backlog) 한 줄.
    /// 색상 태그가 로그에서도 살아 있어야 하므로 평문이 아니라 TMP 리치 문자열을 보관합니다.
    /// </summary>
    public readonly struct DialogueLogEntry
    {
        public readonly string SpeakerName;
        public readonly string RichBody;
        public readonly DialogueChannel Channel;
        public readonly Sprite Portrait;

        public DialogueLogEntry(string speakerName, string richBody, DialogueChannel channel, Sprite portrait)
        {
            SpeakerName = speakerName;
            RichBody = richBody;
            Channel = channel;
            Portrait = portrait;
        }
    }
}
