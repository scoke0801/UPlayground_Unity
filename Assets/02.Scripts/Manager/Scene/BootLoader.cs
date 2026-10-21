using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Manager
{
    /// <summary>
    /// Boot 씬 전용 스크립트.
    /// 모든 매니저의 초기화와 후처리가 완료된 뒤 Title 씬으로 전환한다.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        [Header("로딩 UI (선택)")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private float _minimumLoadingTime = 1f; // 최소 로딩 연출 시간(초)

        private void Start()
        {
            StartCoroutine(CoWaitAndLoad());
        }

        private IEnumerator CoWaitAndLoad()
        {
            var gameManager = GameManager.Instance;
            yield return WaitForInitialization(gameManager);

            if (gameManager == null || gameManager.BootState != GameBootState.Ready)
                yield break;

            _progressBar?.SetValueWithoutNotify(1f);
            SceneManager.Instance.LoadSceneDirect(SceneName.Title);
        }

        private IEnumerator WaitForInitialization(GameManager gameManager)
        {
            // 일시정지 상태에서 시작해도 최소 연출 시간이 끝나야 한다.
            double startedAt = Time.realtimeSinceStartupAsDouble;
            while (gameManager != null)
            {
                if (gameManager.BootState == GameBootState.Failed)
                {
                    Debug.LogError(
                        $"[BootLoader] GameManager 초기화 실패로 Boot을 중단합니다: " +
                        $"{gameManager.InitializationFailure}");
                    yield break;
                }

                if (gameManager.BootState == GameBootState.Disposing)
                    yield break;

                _progressBar?.SetValueWithoutNotify(gameManager.InitializationProgress);

                // 일부 DB가 먼저 준비되어도 다른 매니저의 비동기 작업·후처리가 남을 수 있다.
                if (gameManager.BootState == GameBootState.Ready &&
                    Time.realtimeSinceStartupAsDouble - startedAt >= _minimumLoadingTime)
                    yield break;

                yield return null;
            }
        }
    }
}
