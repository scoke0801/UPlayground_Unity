using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.UREnum;

namespace UPlayGround.Manager
{
    public partial class SceneManager : BaseManager<SceneManager>, IManager
    {
        // Loading씬에서 읽어갈 목적지. static이라 씬 전환 후에도 유지된다.
        public static string PendingSceneName { get; private set; }

        private bool _isLoading = false;

        // 진행률 (0~1). LoadingSceneController가 Update에서 폴링한다.
        public float LoadProgress { get; private set; }

        // true가 되면 LoadingSceneController가 씬 전환을 허용한다.
        public bool IsReadyToActivate { get; private set; }

        public event Action<string> OnLoadComplete;

        /// <summary>
        /// Loading씬을 경유하는 씬 전환. 인게임처럼 무거운 씬 전환에 사용.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneManager] 로딩 중 중복 요청 무시: {sceneName}");
                return;
            }

            _isLoading = true;
            LoadProgress = 0f;
            IsReadyToActivate = false;
            PendingSceneName = sceneName;

            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName.Loading);
        }

        /// <summary>
        /// Loading씬 없이 바로 전환. Boot → Title처럼 로딩 연출이 필요 없는 경우에 사용.
        /// </summary>
        public void LoadSceneDirect(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneManager] 로딩 중 중복 요청 무시: {sceneName}");
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// LoadingSceneController가 준비되면 호출. 비동기 로딩 시작.
        /// </summary>
        public void StartPendingLoad()
        {
            if (string.IsNullOrEmpty(PendingSceneName))
            {
                Debug.LogError("[SceneManager] PendingSceneName이 비어 있습니다.");
                return;
            }

            LoadPendingSceneAsync(PendingSceneName).Forget();
        }

        /// <summary>
        /// LoadingSceneController가 슬라이더 연출 완료 후 호출.
        /// allowSceneActivation을 열어 실제 씬 전환을 실행한다.
        /// </summary>
        public void ActivatePendingScene()
        {
            _activateCallback?.Invoke();
        }

        private Action _activateCallback;

        private async UniTaskVoid LoadPendingSceneAsync(string sceneName)
        {
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            // allowSceneActivation = false 상태에서 progress는 최대 0.9f까지만 증가한다.
            // 씬이 작아서 순식간에 끝나도 최소 연출 시간을 보장하기 위해 시간 조건을 병행한다.
            float elapsed = 0f;
            const float MinDisplayTime = 1.5f; // 로딩 화면 최소 표시 시간

            while (op.progress < 0.9f || elapsed < MinDisplayTime)
            {
                elapsed += Time.deltaTime;
                // 실제 로딩 진행률과 최소 시간 진행률 중 작은 값을 사용
                float realProgress = op.progress / 0.9f;
                float timeProgress = elapsed / MinDisplayTime;
                LoadProgress = Mathf.Min(realProgress, timeProgress);
                await UniTask.Yield();
            }

            LoadProgress = 1f;

            _activateCallback = () =>
            {
                op.allowSceneActivation = true;
                _isLoading = false;
                PendingSceneName = null;
                OnLoadComplete?.Invoke(sceneName);
            };

            IsReadyToActivate = true;
        }
    }
}
