namespace UPlayGround.UREnum
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
        // 인프라 씬만 코드 상수로 유지한다(구조적으로 코드가 직접 참조).
        public const string Boot    = "Boot";
        public const string Title   = "Title";
        public const string Loading = "Loading";   // 로딩 전용 씬

        // 게임플레이(지역) 씬은 코드 상수로 두지 않는다.
        // 씬 파일명 = 지역 식별자(mapId)로, MapConfigDatabaseSO(데이터)에서 관리한다.
        // 새 게임 시작 씬 역시 MapConfigDatabaseSO.DefaultStartMapId 로 지정한다.
    }
}