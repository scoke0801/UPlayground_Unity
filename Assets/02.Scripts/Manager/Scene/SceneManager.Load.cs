using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Enum;

namespace UPlayGround.Manager
{
    public partial class SceneManager : BaseManager<SceneManager>, IManager
    {
        // 씬 전환 중 중복 호출 방지
        private bool _isLoading = false;

        /// <summary>
        /// 씬 로드 진행률 (0~1). UI 로딩바 연결용
        /// </summary>
        public event Action<float> OnLoadProgress;

        /// <summary>
        /// 씬 로드 완료 이벤트
        /// </summary>
        public event Action<string> OnLoadComplete;

        /// <summary>
        /// 비동기 씬 전환
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneManager] 씬 로딩 중 중복 요청 무시: {sceneName}");
                return;
            }
            LoadSceneAsync(sceneName).Forget();
        }

        private async UniTaskVoid LoadSceneAsync(string sceneName)
        {
            _isLoading = true;

            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                OnLoadProgress?.Invoke(op.progress / 0.9f);
                await UniTask.Yield();
            }

            OnLoadProgress?.Invoke(1f);
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f)); // 로딩바 연출용 짧은 대기

            op.allowSceneActivation = true;

            await UniTask.WaitUntil(() => op.isDone);

            _isLoading = false;
            OnLoadComplete?.Invoke(sceneName);
        }
    }
}
