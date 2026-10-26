using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.EnumType;

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

        // 파스트트래블 도착 지점 Id. static이라 Loading 씬을 거쳐도 유지된다.
        // 대상 씬 진입·안정화 시 SceneArrivalRegistry에서 조회해 1회 적용 후 비운다.
        public static string PendingArrivalId { get; private set; }

        // 파스트트래블 도착 월드 좌표. arrivalId 없이 좌표로 직접 스폰할 때 사용(맵 포탈 클릭 등).
        public static Vector3? PendingArrivalPosition { get; private set; }

        private bool _isLoading;
        private bool _isWorldTravel;
        private bool _activationRequested;
        private bool _pendingLoadStarted;
        private string _activeLoadSceneName;
        private CancellationTokenSource _loadCancellation;
        [SerializeField, Min(5f)] private float _loadTimeoutSeconds = 60f;
        private IInputService _transitionInput;
        private bool _inputWasSuppressed;
        private string _returnScene;
        private Vector3? _returnPosition;
        private Quaternion? _arrivalRotation;
        private Quaternion _returnRotation;
        private bool _isRecovering;
        private AsyncOperation _targetOperation;
        private string _arrivedActivationId;
        private Vector3? _appliedArrivalPosition;
        [SerializeField, Min(0.1f)] private float _arrivalTolerance = 2f;

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

        /// <summary>
        /// 파스트트래블: 대상 씬으로 전환하고 도착 후 arrivalId 지점(SceneArrivalPoint)에 플레이어를 배치한다.
        /// arrivalId가 비면 기본 스폰을 사용하고, 지정한 ID가 없거나 중복이면 실패 복귀한다.
        /// </summary>
        public void LoadScene(string sceneName, string arrivalId)
        {
            TryLoadScene(sceneName, arrivalId);
        }

        /// <summary>도착 정보를 덮어쓰기 전에 요청 수락 여부를 반환한다. 완료는 OnLoadComplete로 구분한다.</summary>
        public bool TryLoadScene(string sceneName, string arrivalId) =>
            StartLoad(new SceneLoadRequest(sceneName, useTransitionScene: true), arrivalId);

        /// <summary>
        /// 파스트트래블: 대상 씬으로 전환하고 도착 후 지정 월드 좌표에 플레이어를 배치한다.
        /// SceneArrivalPoint가 없는 지점(맵 포탈 위치 등)으로 이동할 때 사용한다.
        /// </summary>
        public void LoadScene(string sceneName, Vector3 arrivalPosition)
        {
            StartLoad(new SceneLoadRequest(sceneName, useTransitionScene: true), arrivalPosition: arrivalPosition);
        }

        private bool StartLoad(SceneLoadRequest request, string arrivalId = null, Vector3? arrivalPosition = null)
        {
            var world = World.ContinuousWorldController.Active;
            if (world != null && world.ContainsDestination(request.SceneName)
                && (!string.IsNullOrEmpty(arrivalId) || arrivalPosition.HasValue))
            {
                if (_isLoading || !world.IsInitialized
                    || (Services.TryGet<IDialogueService>(out var activeDialogue) && activeDialogue.IsDialogueActive))
                    return false;
                _isLoading = _isWorldTravel = true;
                _activeLoadSceneName = request.SceneName;
                CurrentRequest = request;
                LastLoadFailure = null;
                LoadState = SceneLoadState.WaitingForSceneContext;
                ReplaceLoadCancellation();
                TravelWithinWorldAsync(world, request.SceneName, arrivalId, arrivalPosition, _loadCancellation.Token).Forget();
                return true;
            }

            // 수락된 요청만 도착 정보를 소유한다. 잘못된 중복 요청도 기존 값을 지우면 안 된다.
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneManager] 로딩 중 중복 요청 무시: {request.SceneName}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.SceneName))
            {
                LastLoadFailure = "빈 씬 이름으로 로드를 요청할 수 없습니다.";
                Debug.LogError("[SceneManager] 빈 씬 이름으로 로드를 요청할 수 없습니다.");
                return false;
            }

            // Build Profile/Build Settings에 없는 씬이면 로딩 흐름에 진입하기 전에 명확히 조기 실패한다.
            // (씬 이름 변경·오타·스테일 세이브의 loadSceneName 등을 크립틱한 비동기 오류 대신 바로 진단)
            if (!Application.CanStreamedLevelBeLoaded(request.SceneName))
            {
                Debug.LogError(
                    $"[SceneManager] 씬 '{request.SceneName}'을(를) 로드할 수 없습니다. " +
                    $"Build Profile(File▸Build Profiles)의 씬 목록에 등록되어 있는지 확인하세요. " +
                    $"(세이브의 loadSceneName이나 MapConfigDatabase의 mapId가 옛 씬 이름을 가리킬 수 있습니다.)");
                LastLoadFailure = $"등록되지 않은 씬: {request.SceneName}";
                return false;
            }

            if (request.UseTransitionScene && !Application.CanStreamedLevelBeLoaded(SceneName.Loading))
            {
                LastLoadFailure = "Loading 씬이 등록되지 않았습니다.";
                Debug.LogError($"[SceneManager] {LastLoadFailure}");
                return false;
            }

            if (!_isRecovering && Services.TryGet<IDialogueService>(out var dialogue)
                && dialogue.IsDialogueActive)
                return false;

            // 인계 완료 사실은 생산하지 않고, 씬을 떠나기 전에 기존 취소 경로로 보행과 소품을 정리한다.
            foreach (var interaction in FindObjectsByType<Gameplay.Interaction.NpcLifeInteractable>(FindObjectsSortMode.None))
                if (interaction.IsInteracting()) interaction.StopInteract();

            PendingArrivalId = string.IsNullOrEmpty(arrivalId) ? null : arrivalId;
            PendingArrivalPosition = arrivalPosition;
            _arrivedActivationId = null;
            _appliedArrivalPosition = null;
            if (!_isRecovering)
            {
                _returnScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                Transform player = Svc.ActorQuery?.PlayerTransform;
                _returnPosition = player != null ? player.position : null;
                _returnRotation = player != null ? player.rotation : Quaternion.identity;
                _arrivalRotation = null;
            }

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

            try
            {
                if (_transitionInput == null)
                {
                    _transitionInput = Svc.Input;
                    _inputWasSuppressed = _transitionInput?.IsPlayerActionInputSuppressed ?? false;
                }
                _transitionInput?.SetPlayerActionInputSuppressed(true);
                OnLoadStarted?.Invoke(request.SceneName);
                AssetManager.Instance.ReleaseSceneAssets();
                WatchLoadTimeoutAsync(_loadCancellation.Token).Forget();
                if (request.UseTransitionScene)
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName.Loading);
                    return true;
                }

                UnityEngine.SceneManagement.SceneManager.LoadScene(request.SceneName);
                LoadState = SceneLoadState.WaitingForSceneContext;
                OnSceneActivated?.Invoke(request.SceneName);
                return true;
            }
            catch (Exception e)
            {
                FailCurrentLoad(request.SceneName, e);
                return false;
            }
        }

        private async UniTask WatchLoadTimeoutAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_loadTimeoutSeconds), ignoreTimeScale: true,
                    cancellationToken: token);
                CancelCurrentLoad("씬/플레이어/도착 준비 제한 시간을 초과했습니다.");
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask TravelWithinWorldAsync(World.ContinuousWorldController world, string destination,
            string arrivalId, Vector3? position, CancellationToken token)
        {
            try
            {
                OnLoadStarted?.Invoke(destination);
                _loadCancellation.CancelAfterSlim(TimeSpan.FromSeconds(_loadTimeoutSeconds));
                string activationId = await world.TravelAsync(destination, arrivalId, position, token);
                if (!string.IsNullOrEmpty(activationId)) PortalActivationState.Activate(activationId);
                _isWorldTravel = false;
                CompleteCurrentLoad();
            }
            catch (Exception exception)
            {
                // 엔진 지역 로드는 계속 회수하되 플레이어는 현재 안전 위치에서 조작을 유지한다.
                LastLoadFailure = exception.Message;
                LoadState = SceneLoadState.Failed;
                _isLoading = _isWorldTravel = false;
                _activeLoadSceneName = null;
                CurrentRequest = null;
                ReleaseLoadCancellation();
                OnLoadFailed?.Invoke(destination, LastLoadFailure);
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
                .Forget(exception =>
                {
                    if (exception is not OperationCanceledException)
                        FailCurrentLoad(sceneName, exception);
                });
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
            if (_isWorldTravel) return;
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
            _targetOperation = op;

            LoadState = SceneLoadState.LoadingTargetScene;
            op.allowSceneActivation = false;

            float elapsed = 0f;
            float minimumDisplayTime = CurrentRequest?.MinimumDisplayTime ?? 1.5f;

            while (op.progress < 0.9f || elapsed < minimumDisplayTime)
            {
                // 로딩 화면의 최소 노출 시간은 게임 일시정지와 무관하게 진행한다.
                elapsed += Time.unscaledDeltaTime;
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
            if (!string.IsNullOrEmpty(PendingArrivalId) || PendingArrivalPosition.HasValue)
                throw new InvalidOperationException("요청한 도착 위치를 아직 플레이어에게 적용하지 못했습니다.");
            if (_appliedArrivalPosition.HasValue)
            {
                Transform player = Svc.ActorQuery?.PlayerTransform;
                if (player == null || (player.position - _appliedArrivalPosition.Value).sqrMagnitude
                    > _arrivalTolerance * _arrivalTolerance)
                    throw new InvalidOperationException("플레이어가 지정한 도착점에서 벗어났습니다.");
            }
            if (!string.IsNullOrEmpty(_arrivedActivationId))
                PortalActivationState.Activate(_arrivedActivationId);
            _arrivedActivationId = null;
            _appliedArrivalPosition = null;
            _isLoading = false;
            _activationRequested = false;
            _pendingLoadStarted = false;
            IsReadyToActivate = false;
            PendingSceneName = null;
            PendingArrivalId = null;
            PendingArrivalPosition = null;
            LoadState = SceneLoadState.Completed;
            _activeLoadSceneName = null;
            CurrentRequest = null;
            _targetOperation = null;
            _isRecovering = false;
            ReleaseLoadCancellation();
            ReleaseTransitionInput();
            OnLoadComplete?.Invoke(sceneName);
        }

        private void FailCurrentLoad(string sceneName, Exception exception)
        {
            if (!_isLoading)
                return;

            LastLoadFailure = exception?.Message ?? "알 수 없는 씬 로드 오류";
            _arrivedActivationId = null;
            _isLoading = false;
            _activationRequested = false;
            _pendingLoadStarted = false;
            IsReadyToActivate = false;
            PendingSceneName = null;
            PendingArrivalId = null;
            PendingArrivalPosition = null;
            LoadState = SceneLoadState.Failed;
            Debug.LogError($"[SceneManager] 씬 '{sceneName}' 로드 실패: {LastLoadFailure}");
            _activeLoadSceneName = null;
            CurrentRequest = null;
            ReleaseLoadCancellation();
            bool canRecover = !_isRecovering && _returnPosition.HasValue
                && Application.CanStreamedLevelBeLoaded(_returnScene);
            if (!canRecover) ReleaseTransitionInput();
            // 실패 이벤트에서 새 요청이 끼어들기 전에 복구가 전환을 소유한다.
            if (canRecover) { _isRecovering = true; _isLoading = true; }
            try { OnLoadFailed?.Invoke(sceneName, LastLoadFailure); }
            finally
            {
                if (canRecover) RecoverSourceSceneAsync().Forget();
                else _isRecovering = false;
            }
        }

        private async UniTask RecoverSourceSceneAsync()
        {
            try
            {
                // Unity의 시작된 씬 연산은 취소할 수 없다. 활성화 대기를 풀고 끝낸 뒤 복귀한다.
                if (_targetOperation != null && !_targetOperation.isDone)
                {
                    _targetOperation.allowSceneActivation = true;
                    await UniTask.WaitUntil(() => _targetOperation == null || _targetOperation.isDone,
                        cancellationToken: destroyCancellationToken);
                }
                if (!_isRecovering) return;
                _targetOperation = null;
                _isLoading = false;
                _arrivalRotation = _returnRotation;
                StartLoad(new SceneLoadRequest(_returnScene, false), arrivalPosition: _returnPosition);
            }
            catch (Exception exception)
            {
                _isLoading = false;
                _isRecovering = false;
                ReleaseTransitionInput();
                Debug.LogError($"[SceneManager] 원래 씬 복귀 실패: {exception.Message}");
            }
        }

        private void ReleaseTransitionInput()
        {
            _transitionInput?.SetPlayerActionInputSuppressed(_inputWasSuppressed);
            if (!_inputWasSuppressed) _transitionInput?.SuppressPlayerActionInputBriefly();
            _transitionInput = null;
        }

        private void ReleaseLoadCancellation()
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = null;
        }

        private void DisposeLoadState()
        {
            ReleaseTransitionInput();
            _isRecovering = false;
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = null;
            _isLoading = false;
            _activationRequested = false;
            _pendingLoadStarted = false;
            _activeLoadSceneName = null;
            PendingSceneName = null;
            PendingArrivalId = null;
            PendingArrivalPosition = null;
            CurrentRequest = null;
            IsReadyToActivate = false;
            LoadProgress = 0f;
            LoadState = SceneLoadState.Idle;
        }
    }
}
