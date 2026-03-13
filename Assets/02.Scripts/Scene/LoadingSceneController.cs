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
            yield return new WaitUntil(() => GameManager.Instance.IsInitialized);

            if (string.IsNullOrEmpty(SceneManager.PendingSceneName))
            {
                Debug.LogWarning("[LoadingSceneController] PendingSceneName 없음. Title로 fallback.");
                SceneManager.Instance.LoadScene(Enum.SceneName.Title);
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
