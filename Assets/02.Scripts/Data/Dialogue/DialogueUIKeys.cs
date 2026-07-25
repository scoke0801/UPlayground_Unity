namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 대화 관련 UI 프리팹 키.
    /// UIKeyType은 UIPrefabDatabase 기준 자동 생성 파일이라 신규 UI를 등록하기 전에는 값이 없으므로,
    /// 대화 모듈이 참조하는 키를 이곳에 모아 문자열 중복을 막는다.
    /// (프리팹 등록 후 ID Enum Generator를 재실행하면 UIKeyType에도 같은 키가 생성된다.)
    /// </summary>
    public static class DialogueUIKeys
    {
        public const string MainDialogue = "MainDialogue";
        public const string SystemDialogue = "SystemDialogue";
        public const string MonologueDialogue = "MonologueDialogue";
        public const string DialogueControlBar = "DialogueControlBar";
        public const string DialogueBacklog = "DialogueBacklog";
    }
}
