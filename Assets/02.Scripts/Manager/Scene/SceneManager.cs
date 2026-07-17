using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.Path;
using UPlayGround.MovementController;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager.World;

namespace UPlayGround.Manager
{
    public partial class SceneManager : BaseManager<SceneManager>, IManager, ISceneTransitionService,
        UPlayGround.UI.IUISceneService
    {
        private const float SceneStabilizationTimeout = 2f;
        private const float SceneWaitDiagnosticLogInterval = 5f;
        private const float NearbyMonsterRadius = 35f;
        private const int RequiredStableFixedFrames = 2;

        private string _currentSceneType;
        private string _currentMapID;

        public string CurrentSceneType => _currentSceneType;
        public string CurrentMapID     => _currentMapID;

        public void Init() { }

        public void AfterInit() { }

        public void Dispose()
        {
            DisposeLoadState();
        }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType) { }

        /// <summary>
        /// 씬에 배치된 SceneContext가 Start()에서 호출한다.
        /// </summary>
        public void NotifySceneContextReady(SceneContext context)
        {
            _currentMapID = context.MapID;
            ChangeSceneType(context.SceneType);

            OnSceneContextReady?.Invoke(context);

            if (!_isLoading)
                return;

            if (context.SceneType == SceneType.Loading &&
                LoadState == SceneLoadState.LoadingTransitionScene)
            {
                return;
            }

            string loadedSceneName = context.gameObject.scene.name;
            if (!string.Equals(
                    loadedSceneName,
                    _activeLoadSceneName,
                    System.StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    $"[SceneManager] 대기 중인 씬과 다른 SceneContext 준비 신호를 무시합니다. " +
                    $"대기={_activeLoadSceneName}, 수신={loadedSceneName}");
                return;
            }

            LoadState = SceneLoadState.Stabilizing;
            StabilizeSceneAndCompleteAsync(
                    context,
                    _loadCancellation != null
                        ? _loadCancellation.Token
                        : System.Threading.CancellationToken.None)
                .Forget(exception =>
                {
                    // 취소는 정상 흐름이다. CancelCurrentLoad는 이미 FailCurrentLoad를 호출했고,
                    // 새 로드로 인한 취소(ReplaceLoadCancellation)는 새 로드가 상태를 재설정한다.
                    // 여기서 다시 FailCurrentLoad를 부르면 정상 취소가 실패로 둔갑한다.
                    if (exception is System.OperationCanceledException)
                        return;

                    FailCurrentLoad(loadedSceneName, exception);
                });
        }

        private void ChangeSceneType(string sceneType)
        {
            if (UIManager.Instance.IsInitialized == false)
            {
                StartCoroutine(CoChangeSceneType(sceneType));
                return;
            }

            ApplySceneType(sceneType);
        }

        private void ApplySceneType(string sceneType)
        {
            _currentSceneType = sceneType;

            // 매니저들에 씬 전환 통보 (UI 처리보다 먼저 — 레퍼런스 재수집 선행)
            GameManager.Instance.NotifySceneChanged(sceneType);

            if (sceneType == SceneType.GamePlay)
            {
                UIManager.Instance.HideUI(UIKeyType.TitleMenu);
                UIManager.Instance.HideUI(UIKeyType.PauseMenu);
                UIManager.Instance.ShowUI(UIKeyType.GamePlay);
            }
            else if (sceneType == SceneType.Title)
            {
                UIManager.Instance.HideUI(UIKeyType.PauseMenu);
                UIManager.Instance.HideUI(UIKeyType.GamePlay);
                UIManager.Instance.ShowUI(UIKeyType.TitleMenu);
            }
            else
            {
                UIManager.Instance.HideUI(UIKeyType.PauseMenu);
                UIManager.Instance.HideUI(UIKeyType.GamePlay);
                UIManager.Instance.HideUI(UIKeyType.TitleMenu);
            }
        }

        private IEnumerator CoChangeSceneType(string sceneType)
        {
            yield return new WaitUntil(() => UIManager.Instance.IsInitialized);
            ApplySceneType(sceneType);
        }

        private async UniTask StabilizeSceneAndCompleteAsync(
            SceneContext context,
            CancellationToken cancellationToken)
        {
            if (context.SceneType == SceneType.GamePlay)
                await WaitForGameplayStabilizationAsync(cancellationToken);

            CompleteCurrentLoad();
        }

        /// <summary>
        /// 파스트트래블 도착 지점(PendingArrivalId)이 지정돼 있으면 플레이어를 그 위치에 1회 배치한다.
        /// 대상 씬에 해당 SceneArrivalPoint가 없으면 경고 후 씬 기본 스폰을 유지한다.
        /// </summary>
        private void TryApplyPendingArrival(PlayerActor player)
        {
            bool hasId  = !string.IsNullOrEmpty(PendingArrivalId);
            bool hasPos = PendingArrivalPosition.HasValue;
            if (player == null || (!hasId && !hasPos))
                return;

            Vector3    pos;
            Quaternion rot = Quaternion.identity;

            if (hasId)
            {
                if (!SceneArrivalRegistry.TryGet(PendingArrivalId, out var arrival))
                {
                    Debug.LogWarning(
                        $"[SceneManager] 도착 지점 '{PendingArrivalId}'을(를) 찾을 수 없어 씬 기본 스폰을 사용합니다.");
                    PendingArrivalId = null;
                    PendingArrivalPosition = null;
                    return;
                }
                pos = arrival.Position;
                rot = arrival.Rotation;
            }
            else
            {
                pos = PendingArrivalPosition.Value;
            }

            // KCC 모터가 준비되기 전 transform으로 옮기면 모터가 되돌린다.
            // 모터가 생성될 때까지 소비하지 않고 다음 프레임에 재시도한다.
            var motor = player.ActorController?.Motor;
            if (motor == null) return;

            motor.SetPositionAndRotation(pos, rot);
            CameraManager.Instance?.SnapToTarget(pos);

            PendingArrivalId = null;         // 1회성 소비
            PendingArrivalPosition = null;
        }

        private async UniTask WaitForGameplayStabilizationAsync(CancellationToken cancellationToken)
        {
            await WaitForRuntimePlacementLoadersAsync(cancellationToken);

            float startedAt = Time.realtimeSinceStartup;
            int stableFixedFrames = 0;
            PlayerActor player = null;
            bool cameraPreparationRequested = false;

            while (Time.realtimeSinceStartup - startedAt < SceneStabilizationTimeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (player == null)
                    player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();

                TryApplyPendingArrival(player);

                PartyManager partyManager = PartyManager.Instance;
                bool sceneRestoreReady = partyManager == null
                                         || partyManager.EnsurePendingSceneRestoreApplied(player);
                bool actorsStable = sceneRestoreReady
                                    && player != null
                                    && IsGrounded(player.PlayerController)
                                    && AreNearbyMonstersStable(player.transform.position);
                CameraManager cameraManager = CameraManager.Instance;

                if (sceneRestoreReady
                    && player != null
                    && !cameraPreparationRequested
                    && cameraManager != null
                    && cameraManager.IsSceneCameraInitialized)
                {
                    cameraPreparationRequested =
                        cameraManager.PrepareSceneCamera(player.transform);
                }

                bool cameraStable = cameraPreparationRequested
                                    && cameraManager != null
                                    && cameraManager.IsSceneCameraReadyFor(player.transform);

                if (actorsStable && cameraStable)
                {
                    stableFixedFrames++;
                    if (stableFixedFrames >= RequiredStableFixedFrames)
                    {
                        await UniTask.WaitForEndOfFrame(cancellationToken);
                        return;
                    }
                }
                else
                {
                    stableFixedFrames = 0;
                }

                await UniTask.WaitForFixedUpdate(cancellationToken);
            }

            Debug.LogWarning(
                $"[SceneManager] 액터 안정화가 {SceneStabilizationTimeout:F1}초 내 완료되지 않았습니다. " +
                $"로딩 화면을 유지한 채 카메라 준비를 계속 대기합니다. " +
                $"플레이어={player != null}, 카메라초기화={CameraManager.Instance?.IsSceneCameraInitialized ?? false}, " +
                $"카메라포즈={CameraManager.Instance?.IsSceneCameraReady ?? false}");

            // 게임플레이 화면은 카메라가 플레이어 기준 포즈를 실제 LateUpdate에 적용한 뒤에만 공개한다.
            // 플레이어/카메라 생성이 늦더라도 시간 초과로 로딩 화면을 먼저 제거하지 않는다.
            // 단, 비정상적으로 끝나지 않는 경우를 진단할 수 있도록 일정 간격마다 상태를 로그로 남긴다.
            float waitStartedAt = Time.realtimeSinceStartup;
            float lastDiagnosticLogAt = waitStartedAt;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (player == null)
                    player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();

                TryApplyPendingArrival(player);

                PartyManager partyManager = PartyManager.Instance;
                bool sceneRestoreReady = partyManager == null
                                         || partyManager.EnsurePendingSceneRestoreApplied(player);
                CameraManager cameraManager = CameraManager.Instance;
                if (sceneRestoreReady
                    && player != null
                    && !cameraPreparationRequested
                    && cameraManager != null
                    && cameraManager.IsSceneCameraInitialized)
                {
                    cameraPreparationRequested = cameraManager.PrepareSceneCamera(player.transform);
                }

                if (cameraPreparationRequested
                    && cameraManager != null
                    && cameraManager.IsSceneCameraReadyFor(player.transform))
                {
                    break;
                }

                float now = Time.realtimeSinceStartup;
                if (now - lastDiagnosticLogAt >= SceneWaitDiagnosticLogInterval)
                {
                    lastDiagnosticLogAt = now;
                    Debug.LogWarning(
                        $"[SceneManager] 씬 준비가 {now - waitStartedAt:F1}초째 완료되지 않아 로딩 화면을 유지 중입니다. " +
                        $"플레이어={player != null}, 씬복원완료={sceneRestoreReady}, " +
                        $"카메라준비요청={cameraPreparationRequested}, " +
                        $"카메라초기화={cameraManager?.IsSceneCameraInitialized ?? false}, " +
                        $"카메라포즈={cameraManager?.IsSceneCameraReady ?? false}");
                }

                await UniTask.WaitForFixedUpdate(cancellationToken);
            }

            await UniTask.WaitForEndOfFrame(cancellationToken);
        }

        private static async UniTask WaitForRuntimePlacementLoadersAsync(CancellationToken cancellationToken)
        {
            float waitStartedAt = Time.realtimeSinceStartup;
            float lastDiagnosticLogAt = waitStartedAt;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                RuntimePlacementLoader[] loaders =
                    UnityEngine.Object.FindObjectsByType<RuntimePlacementLoader>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);

                bool allComplete = true;
                int pendingCount = 0;
                for (int i = 0; i < loaders.Length; i++)
                {
                    RuntimePlacementLoader loader = loaders[i];
                    if (loader == null || !loader.isActiveAndEnabled)
                        continue;

                    if (loader.IsSpawnComplete)
                        continue;

                    allComplete = false;
                    pendingCount++;
                }

                if (allComplete)
                    return;

                float now = Time.realtimeSinceStartup;
                if (now - lastDiagnosticLogAt >= SceneWaitDiagnosticLogInterval)
                {
                    lastDiagnosticLogAt = now;
                    Debug.LogWarning(
                        $"[SceneManager] RuntimePlacementLoader {pendingCount}개가 {now - waitStartedAt:F1}초째 스폰 중입니다. " +
                        "로딩 화면을 유지합니다.");
                }

                await UniTask.Yield(cancellationToken);
            }
        }

        private static bool AreNearbyMonstersStable(Vector3 playerPosition)
        {
            MonsterActor[] monsters =
                UnityEngine.Object.FindObjectsByType<MonsterActor>(FindObjectsSortMode.None);
            float radiusSqr = NearbyMonsterRadius * NearbyMonsterRadius;

            for (int i = 0; i < monsters.Length; i++)
            {
                MonsterActor monster = monsters[i];
                if (monster == null
                    || !monster.isActiveAndEnabled
                    || !monster.IsAlive()
                    || monster.FlyingAIController != null)
                {
                    continue;
                }

                if ((monster.transform.position - playerPosition).sqrMagnitude > radiusSqr)
                    continue;

                if (!IsGrounded(monster.GetComponent<ActorMovementController>()))
                    return false;
            }

            return true;
        }

        private static bool IsGrounded(ActorMovementController movement)
        {
            if (movement == null || movement.Motor == null || !movement.Motor.isActiveAndEnabled)
                return true;

            return movement.Motor.GroundingStatus.IsStableOnGround;
        }

    }
}
