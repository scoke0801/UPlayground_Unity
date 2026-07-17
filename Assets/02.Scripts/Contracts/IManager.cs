using System.Threading;
using Cysharp.Threading.Tasks;

namespace UPlayGround.Manager
{
    /// <summary>
    /// GameManager의 부팅 진행 상태.
    /// </summary>
    public enum GameBootState
    {
        None,
        Initializing,
        Ready,
        Failed,
        Disposing,
    }

    /// <summary>
    /// 필수 비동기 작업이 완료되어야 사용 가능한 매니저가 구현한다.
    /// GameManager는 모든 구현체의 완료를 기다린 뒤 AfterInit을 호출한다.
    /// </summary>
    public interface IAsyncInitializableManager
    {
        UniTask InitializeAsync(CancellationToken cancellationToken);
    }

    public interface IUpdatableManager
    {
        void OnUpdate();
    }

    public interface IFixedUpdatableManager
    {
        void OnFixedUpdate();
    }

    public interface ILateUpdatableManager
    {
        void OnLateUpdate();
    }

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
