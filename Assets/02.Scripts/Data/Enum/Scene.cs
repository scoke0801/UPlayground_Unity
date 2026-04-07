namespace UPlayGround.Enum
{
    /// <summary>
    /// 씬의 논리적 타입 식별자.
    /// SceneContext 하위 클래스에서 SceneType 프로퍼티로 반환한다.
    /// </summary>
    public static class SceneType
    {
        public const string Boot     = "Boot";
        public const string Title    = "Title";
        public const string GamePlay = "GamePlay";
        public const string Loading  = "Loading";
        public const string Test     = "Test";
    }

    /// <summary>
    /// Build Settings에 등록된 물리 씬 파일명.
    /// SceneManager.LoadScene() 인자로 사용한다.
    /// </summary>
    public static class SceneName
    {
        public const string Boot            = "Boot";
        public const string Title           = "Title";
        public const string Loading         = "Loading";   // 로딩 전용 씬
        public const string InGame          = "InGame";
        public const string InteractionTest = "InteractionTest";
        public const string CameraTest      = "CameraTest";
        public const string KccTest         = "KccTest";
        public const string CombatTest      = "CombatTest";
    }
}