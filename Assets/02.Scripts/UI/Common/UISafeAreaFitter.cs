using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>
    /// 노치·둥근 모서리·오버스캔 영역을 피해 UI 콘텐츠의 앵커를 보정한다.
    /// 저작된 앵커를 기준으로 안전 영역 안에 다시 매핑하므로 좌/우 또는 상/하 고정 HUD에도 쓸 수 있다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UISafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool _fitHorizontal = true;
        [SerializeField] private bool _fitVertical = true;

        private RectTransform _rect;
        private Vector2 _authoredAnchorMin;
        private Vector2 _authoredAnchorMax;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private bool _anchorsCached;

        private void Awake()
        {
            CacheAuthoredAnchors();
        }

        private void OnEnable()
        {
            CacheAuthoredAnchors();
            Apply(force: true);
        }

        private void LateUpdate()
        {
            Apply(force: false);
        }

        private void CacheAuthoredAnchors()
        {
            if (_anchorsCached)
                return;

            _rect = transform as RectTransform;
            if (_rect == null)
                return;

            _authoredAnchorMin = _rect.anchorMin;
            _authoredAnchorMax = _rect.anchorMax;
            _anchorsCached = true;
        }

        private void Apply(bool force)
        {
            if (!_anchorsCached || _rect == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (!force && safeArea == _lastSafeArea && screenSize == _lastScreenSize)
                return;

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;

            Vector2 safeMin = new(
                safeArea.xMin / Screen.width,
                safeArea.yMin / Screen.height);
            Vector2 safeMax = new(
                safeArea.xMax / Screen.width,
                safeArea.yMax / Screen.height);

            if (!_fitHorizontal)
            {
                safeMin.x = 0f;
                safeMax.x = 1f;
            }

            if (!_fitVertical)
            {
                safeMin.y = 0f;
                safeMax.y = 1f;
            }

            Vector2 safeSize = safeMax - safeMin;
            _rect.anchorMin = safeMin + Vector2.Scale(_authoredAnchorMin, safeSize);
            _rect.anchorMax = safeMin + Vector2.Scale(_authoredAnchorMax, safeSize);
        }
    }
}
