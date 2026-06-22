using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.UREnum;

namespace UPlayGround.Manager
{
    public enum SceneLoadState
    {
        Idle,
        LoadingTransitionScene,
        LoadingTargetScene,
        AwaitingActivation,
        Activating,
        WaitingForSceneContext,
        Stabilizing,
        Completed,
        Failed,
    }

    public readonly struct SceneLoadRequest
    {
        public string SceneName { get; }
        public bool UseTransitionScene { get; }
        public float MinimumDisplayTime { get; }

        public SceneLoadRequest(
            string sceneName,
            bool useTransitionScene,
            float minimumDisplayTime = 1.5f)
        {
            SceneName = sceneName;
            UseTransitionScene = useTransitionScene;
            MinimumDisplayTime = Mathf.Max(0f, minimumDisplayTime);
        }
    }

    public partial class SceneManager : BaseManager<SceneManager>, IManager
    {
        // Loading씬에서 읽어갈 목적지. static이라 씬 전환 후에도 유지된다.
        public static string PendingSceneName { get; private set; }

        private bool _isLoading;
        private bool _activationRequested;
        private bool _pendingLoadStarted;
        private string _activeLoadSceneName;
        private CancellationTokenSource _loadCancellation;

        // 진행률 (0~1). LoadingSceneController가 Update에서 폴링한다.
        public float LoadProgress { get; private set; }

        // true가 되면 LoadingSceneController가 씬 전환을 허용한다.
        public bool IsReadyToActivate { get; private set; }
        public bool IsLoading => _isLoading;
        public SceneLoadState LoadState { get; private set; } = SceneLoadState.Idle;
        public string LastLoadFailure { get; private set; }
        public SceneLoadRequest? CurrentRequest { get; private set; }

        public event Action<string> OnLoadStarted;
        public event Action<string> OnReadyToActivate;
        public event Action<string> OnSceneActivated;
        public event Action<SceneContext> OnSceneContextReady;
        public event Action<string, string> OnLoadFailed;
        public event Action<string> OnLoadComplete;

        /// <summary>
        /// Loading씬을 경유하는 씬 전환. 인게임처럼 무거운 씬 전환에 사용.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            StartLoad(new SceneLoadRequest(sceneName, useTransitionScene: true));
        }

        private void StartLoad(SceneLoadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SceneName))
            {
                Debug.LogError("[SceneManager] 빈 씬 이름으로 로드를 요청할 수 없습니다.");
                return;
            }

            if (_isLoading)
            {
                Debug.LogWarning($"[SceneManager] 로딩 중 중복 요청 무시: {request.SceneName}");
                return;
            }

            AssetManager.Instance.ReleaseSceneAssets();

            _isLoading = true;
            LoadState = request.UseTransitionScene
                ? SceneLoadState.LoadingTransitionScene
                : SceneLoadState.Activating;
            LoadProgress = request.UseTransitionScene ? 0f : 1f;
            IsReadyToActivate = false;
            LastLoadFailure = null;
            _activationRequested = false;
            _pendingLoadStarted = false;
            _activeLoadSceneName = request.SceneName;
            PendingSceneName = request.UseTransitionScene ? request.SceneName : null;
            CurrentRequest = request;
            ReplaceLoadCancellation();
            OnLoadStarted?.Invoke(request.SceneName);

            try
            {
                if (request.UseTransitionScene)
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName.Loading);
                    return;
                }

                UnityEngine.SceneManagement.SceneManager.LoadScene(request.SceneName);
                LoadState = SceneLoadState.WaitingForSceneContext;
                OnSceneActivated?.Invoke(request.SceneName);
            }
            catch (Exception e)
            {
                FailCurrentLoad(request.SceneName, e);
            }
        }

        /// <summary>
        /// Loading씬 없이 바로 전환. Boot → Title처럼 로딩 연출이 필요 없는 경우에 사용.
        /// </summary>
        public void LoadSceneDirect(string sceneName)
        {
            StartLoad(new SceneLoadRequest(sceneName, useTransitionScene: false));
        }

        /// <summary>
        /// LoadingSceneController가 준비되면 호출. 비동기 로딩 시작.
        /// </summary>
        public void StartPendingLoad()
        {
            if (!_isLoading || LoadState != SceneLoadState.LoadingTransitionScene)
            {
                Debug.LogWarning(
                    $"[SceneManager] 현재 상태에서는 대상 씬 로드를 시작할 수 없습니다: {LoadState}");
                return;
            }

            if (_pendingLoadStarted)
            {
                Debug.LogWarning("[SceneManager] 대상 씬 로드가 이미 시작되었습니다.");
                return;
            }

            if (string.IsNullOrEmpty(PendingSceneName))
            {
                FailCurrentLoad(
                    PendingSceneName,
                    new InvalidOperationException("PendingSceneName이 비어 있습니다."));
                return;
            }

            _pendingLoadStarted = true;
            string sceneName = PendingSceneName;
            LoadPendingSceneAsync(sceneName, _loadCancellation.Token)
                .Forget(exception => FailCurrentLoad(sceneName, exception));
        }

        /// <summary>
        /// LoadingSceneController가 슬라이더 연출 완료 후 호출.
        /// allowSceneActivation을 열어 실제 씬 전환을 실행한다.
        /// </summary>
        public void ActivatePendingScene()
        {
            if (LoadState != SceneLoadState.AwaitingActivation)
                return;

            _activationRequested = true;
        }

        public void CancelCurrentLoad(string reason = null)
        {
            if (!_isLoading)
                return;

            string sceneName = _activeLoadSceneName;
            _loadCancellation?.Cancel();
            FailCurrentLoad(
                sceneName,
                new OperationCanceledException(reason ?? "씬 로드가 취소되었습니다."));
        }

        private async UniTask LoadPendingSceneAsync(
            string sceneName,
            CancellationToken cancellationToken)
        {
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
                throw new InvalidOperationException($"씬 비동기 로드 생성 실패: {sceneName}");

            LoadState = SceneLoadState.LoadingTargetScene;
            op.allowSceneActivation = false;

            float elapsed = 0f;
            float minimumDisplayTime = CurrentRequest?.MinimumDisplayTime ?? 1.5f;

            while (op.progress < 0.9f || elapsed < minimumDisplayTime)
            {
                elapsed += Time.deltaTime;
                float realProgress = op.progress / 0.9f;
                float timeProgress = minimumDisplayTime <= 0f
                    ? 1f
                    : elapsed / minimumDisplayTime;
                LoadProgress = Mathf.Min(realProgress, timeProgress);
                await UniTask.Yield(cancellationToken);
            }

            LoadProgress = 1f;
            LoadState = SceneLoadState.AwaitingActivation;
            IsReadyToActivate = true;
            OnReadyToActivate?.Invoke(sceneName);

            await UniTask.WaitUntil(
                () => _activationRequested,
                cancellationToken: cancellationToken);

            IsReadyToActivate = false;
            LoadState = SceneLoadState.Activating;
            op.allowSceneActivation = true;

            await UniTask.WaitUntil(
                () => op.isDone,
                cancellationToken: cancellationToken);

            PendingSceneName = null;
            LoadState = SceneLoadState.WaitingForSceneContext;
            OnSceneActivated?.Invoke(sceneName);
        }

        private void ReplaceLoadCancellation()
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = new CancellationTokenSource();
        }

        private void CompleteCurrentLoad()
        {
            if (!_isLoading)
                return;

            string sceneName = _activeLoadSceneName;
            _isLoading = false;
            _activationRequested = false;
            _pendingLoadStarted = false;
            IsReadyToActivate = false;
            PendingSceneName = null;
            LoadState = SceneLoadState.Completed;
            OnLoadComplete?.Invoke(sceneName);
            _activeLoadSceneName = null;
            CurrentRequest = null;
            ReleaseLoadCancellation();
        }

        private void FailCurrentLoad(string sceneName, Exception exception)
        {
            if (!_isLoading)
                return;

            LastLoadFailure = exception?.Message ?? "알 수 없는 씬 로드 오류";
            _isLoading = false;
            _activationRequested = false;
            _pendingLoadStarted = false;
            IsReadyToActivate = false;
            PendingSceneName = null;
            LoadState = SceneLoadState.Failed;
            OnLoadFailed?.Invoke(sceneName, LastLoadFailure);
            Debug.LogError($"[SceneManager] 씬 '{sceneName}' 로드 실패: {LastLoadFailure}");
            _activeLoadSceneName = null;
            CurrentRequest = null;
            ReleaseLoadCancellation();
        }

        private void ReleaseLoadCancellation()
        {
            _loadCancellation?.Dispose();
            _loadCancellation = null;
        }

        private void DisposeLoadState()
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = null;
            _isLoading = false;
            _activationRequested = false;
            _pendingLoadStarted = false;
            _activeLoadSceneName = null;
            PendingSceneName = null;
            CurrentRequest = null;
            IsReadyToActivate = false;
            LoadProgress = 0f;
            LoadState = SceneLoadState.Idle;
        }
    }
}
