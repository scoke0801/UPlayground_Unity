using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.Path;
using UPlayGround.MovementController;
using UPlayGround.UREnum;

namespace UPlayGround.Manager
{
    public partial class SceneManager : BaseManager<SceneManager>, IManager
    {
        private const float SceneStabilizationTimeout = 2f;
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

        private async UniTask WaitForGameplayStabilizationAsync(CancellationToken cancellationToken)
        {
            float startedAt = Time.realtimeSinceStartup;
            int stableFixedFrames = 0;
            PlayerActor player = null;
            bool cameraPreparationRequested = false;

            while (Time.realtimeSinceStartup - startedAt < SceneStabilizationTimeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (player == null)
                    player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();

                bool actorsStable = player != null
                                    && IsGrounded(player.PlayerController)
                                    && AreNearbyMonstersStable(player.transform.position);
                CameraManager cameraManager = CameraManager.Instance;

                if (actorsStable
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
                $"[SceneManager] 액터 안정화가 {SceneStabilizationTimeout:F1}초 내 완료되지 않아 카메라 준비 단계로 진행합니다. " +
                $"플레이어={player != null}, 카메라초기화={CameraManager.Instance?.IsSceneCameraInitialized ?? false}, " +
                $"카메라포즈={CameraManager.Instance?.IsSceneCameraReady ?? false}");

            // 타임아웃 폴백: 액터가 끝내 안정화되지 않아도 카메라만은 best-effort로 준비시킨 뒤 진행한다.
            // 단, 플레이어/카메라가 끝내 나타나지 않는 비정상 상황에서도 영구 대기하지 않도록
            // 추가 예산(SceneStabilizationTimeout)을 두고, 소진되면 그대로 완료 단계로 넘어간다.
            float fallbackStartedAt = Time.realtimeSinceStartup;
            bool fallbackPrepareRequested = false;

            while (Time.realtimeSinceStartup - fallbackStartedAt < SceneStabilizationTimeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (player == null)
                    player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();

                CameraManager cameraManager = CameraManager.Instance;
                if (player != null
                    && !fallbackPrepareRequested
                    && cameraManager != null
                    && cameraManager.IsSceneCameraInitialized)
                {
                    fallbackPrepareRequested = cameraManager.PrepareSceneCamera(player.transform);
                }

                if (fallbackPrepareRequested
                    && cameraManager != null
                    && cameraManager.IsSceneCameraReadyFor(player.transform))
                {
                    break;
                }

                await UniTask.WaitForFixedUpdate(cancellationToken);
            }

            await UniTask.WaitForEndOfFrame(cancellationToken);
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
