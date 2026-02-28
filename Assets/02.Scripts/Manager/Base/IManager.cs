namespace UPlayGround.Manager
{
    /// <summary>
    /// 모든 매니저가 구현해야 하는 인터페이스
    /// </summary>
    public interface IManager
    {
        void Init();
        void AfterInit();
        void Dispose();
        void OnUpdate();
        void OnFixedUpdate();
        void OnLateUpdate();

        /// <summary>
        /// 씬 전환 완료 시 호출. 씬 의존 상태(오브젝트 레퍼런스 등)를 재초기화한다.
        /// </summary>
        void OnSceneChanged(string sceneType);
    }
}
