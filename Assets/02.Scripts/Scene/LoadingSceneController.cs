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
        [SerializeField] private string _tipsPrefix = "Tips | ";
        [SerializeField] private string[] _tips;

        private bool _activationTriggered = false;
        private GameObject _persistentCanvasRoot;
        private bool _isPresentationPersistent;
        private bool _releaseRequested;
        private int _currentTipIndex = -1;

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
            if (UnityEngine.Input.GetMouseButtonDown(0) || HasNewTouch())
                ShowNextTip();

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
            PreserveLoadingPresentation();
            SceneManager.Instance.ActivatePendingScene();
        }

        private void PreserveLoadingPresentation()
        {
            if (_isPresentationPersistent)
                return;

            Canvas childCanvas = _progressSlider != null
                ? _progressSlider.GetComponentInParent<Canvas>()
                : null;
            Canvas rootCanvas = childCanvas != null
                ? childCanvas.rootCanvas
                : null;
            if (rootCanvas == null)
            {
                Debug.LogWarning("[LoadingSceneController] 유지할 루트 로딩 Canvas를 찾지 못했습니다.");
                return;
            }

            _isPresentationPersistent = true;
            _persistentCanvasRoot = rootCanvas.gameObject;
            rootCanvas.sortingOrder = short.MaxValue;
            EnsureOpaqueBackground(rootCanvas.transform);
            DontDestroyOnLoad(_persistentCanvasRoot);
            DontDestroyOnLoad(gameObject);

            SceneManager.Instance.OnLoadComplete += HandleLoadComplete;
            SceneManager.Instance.OnLoadFailed += HandleLoadFailed;
        }

        private void HandleLoadComplete(string sceneName)
        {
            if (!_releaseRequested)
                StartCoroutine(ReleaseAfterRenderedFrames());
        }

        private void HandleLoadFailed(string sceneName, string reason)
        {
            ReleaseLoadingPresentation();
        }

        private IEnumerator ReleaseAfterRenderedFrames()
        {
            _releaseRequested = true;
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            ReleaseLoadingPresentation();
        }

        private static void EnsureOpaqueBackground(Transform canvasTransform)
        {
            var background = new GameObject(
                "LoadingPersistentBackground",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            background.transform.SetParent(canvasTransform, false);
            background.transform.SetAsFirstSibling();

            var rect = background.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = background.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
        }

        private void ReleaseLoadingPresentation()
        {
            if (!_isPresentationPersistent)
                return;

            _isPresentationPersistent = false;
            _releaseRequested = false;
            if (SceneManager.Instance != null)
            {
                SceneManager.Instance.OnLoadComplete -= HandleLoadComplete;
                SceneManager.Instance.OnLoadFailed -= HandleLoadFailed;
            }

            if (_persistentCanvasRoot != null)
                Destroy(_persistentCanvasRoot);

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (SceneManager.Instance == null)
                return;

            SceneManager.Instance.OnLoadComplete -= HandleLoadComplete;
            SceneManager.Instance.OnLoadFailed -= HandleLoadFailed;
        }

        private void ShowRandomTip()
        {
            if (_tips == null || _tips.Length == 0 || _tipsText == null) return;

            int nextIndex = Random.Range(0, _tips.Length);
            if (_tips.Length > 1 && nextIndex == _currentTipIndex)
                nextIndex = (nextIndex + 1) % _tips.Length;

            ShowTip(nextIndex);
        }

        private void ShowNextTip()
        {
            if (_tips == null || _tips.Length == 0 || _tipsText == null) return;

            int nextIndex = _currentTipIndex < 0
                ? 0
                : (_currentTipIndex + 1) % _tips.Length;
            ShowTip(nextIndex);
        }

        private void ShowTip(int index)
        {
            if (index < 0 || index >= _tips.Length) return;

            _currentTipIndex = index;
            _tipsText.text = $"{_tipsPrefix}{_tips[index]}";
        }

        private static bool HasNewTouch()
        {
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                if (UnityEngine.Input.GetTouch(i).phase == TouchPhase.Began)
                    return true;
            }

            return false;
        }
    }
}
