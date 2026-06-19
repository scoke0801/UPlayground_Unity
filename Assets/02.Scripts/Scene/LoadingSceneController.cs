using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// Loading 씬에 배치. SceneManager의 LoadProgress를 Update에서 폴링한다.
    /// 이벤트 구독 타이밍 문제를 원천 차단한다.
    /// </summary>
    public class LoadingSceneController : MonoBehaviour
    {
        [Header("Progress")]
        [SerializeField] private Slider _progressSlider;

        [Header("Tips")]
        [SerializeField] private TMP_Text _tipsText;
        [SerializeField] private string[] _tips;

        private bool _activationTriggered = false;

        private void Start()
        {
            _progressSlider.minValue = 0f;
            _progressSlider.maxValue = 100f;
            _progressSlider.value = 0f;
            _activationTriggered = false;
            ShowRandomTip();
            StartCoroutine(WaitAndLoad());
        }

        private IEnumerator WaitAndLoad()
        {
            // 부팅 실패 시 IsInitialized는 영영 true가 되지 않으므로 Failed도 함께 대기 종료 조건에 둔다.
            yield return new WaitUntil(() =>
                GameManager.Instance.BootState is GameBootState.Ready or GameBootState.Failed);

            if (GameManager.Instance.BootState == GameBootState.Failed)
            {
                Debug.LogError(
                    $"[LoadingSceneController] GameManager 초기화 실패로 로딩을 중단합니다: " +
                    $"{GameManager.Instance.InitializationFailure}");
                yield break;
            }

            if (string.IsNullOrEmpty(SceneManager.PendingSceneName))
            {
                Debug.LogWarning("[LoadingSceneController] PendingSceneName 없음. Title로 fallback.");
                SceneManager.Instance.CancelCurrentLoad("대상 씬 정보가 없어 Title로 복구합니다.");
                SceneManager.Instance.LoadSceneDirect(UREnum.SceneName.Title);
                yield break;
            }

            SceneManager.Instance.StartPendingLoad();
        }

        private void Update()
        {
            if (_activationTriggered) return;

            float target = SceneManager.Instance.LoadProgress * 100f;
            _progressSlider.value = Mathf.MoveTowards(_progressSlider.value, target, Time.deltaTime * 40f);

            if (SceneManager.Instance.IsReadyToActivate && _progressSlider.value >= 99.9f)
            {
                _activationTriggered = true;
                StartCoroutine(ActivateAfterDelay());
            }
        }

        private IEnumerator ActivateAfterDelay()
        {
            _progressSlider.value = 100f;
            yield return new WaitForSeconds(0.3f);
            SceneManager.Instance.ActivatePendingScene();
        }

        private void ShowRandomTip()
        {
            if (_tips == null || _tips.Length == 0 || _tipsText == null) return;
            _tipsText.text = $"Tips | {_tips[Random.Range(0, _tips.Length)]}";
        }
    }
}
