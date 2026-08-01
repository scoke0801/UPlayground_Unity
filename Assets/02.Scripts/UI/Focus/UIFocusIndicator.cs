using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// EventSystem 포커스의 시각 표현을 각 UI가 소유할 수 있게 하는 계약.
    /// 구현이 없으면 <see cref="UIFocusIndicator"/>의 공통 fallback 테두리를 사용한다.
    /// </summary>
    public interface IUIFocusPresentation
    {
        /// <summary>
        /// true이면 UI 자체의 OnSelect/OnDeselect 표현을 사용하고 전역 테두리는 숨긴다.
        /// </summary>
        bool SuppressGlobalFocusIndicator { get; }

        /// <summary>
        /// 전역 fallback을 유지하면서 다른 RectTransform을 감싸야 할 때 지정한다.
        /// null이면 EventSystem이 선택한 RectTransform을 그대로 사용한다.
        /// </summary>
        RectTransform GlobalFocusIndicatorTarget { get; }
    }

    /// <summary>
    /// 게임패드로 현재 무엇이 선택돼 있는지 보여주는 전역 포커스 표시기.
    ///
    /// 왜 필요한가: 프로젝트 UI 버튼은 Unity 기본 ColorBlock을 그대로 쓰고 있어
    /// Normal(1,1,1)과 Selected(0.96,0.96,0.96)의 차이가 4%뿐이다. 선택은 정상적으로
    /// 이동하지만 화면에서 구분이 불가능하다. 프리팹 수십 개의 ColorBlock을 개별로
    /// 고치는 대신, EventSystem의 현재 선택을 따라다니는 테두리 하나를 UIRoot에 띄운다.
    ///
    /// 스프라이트 에셋 없이 4개의 얇은 Image로 사각 테두리를 런타임에 만든다.
    /// 마우스/키보드가 활성 장치일 때는 숨겨 기존 hover 표현과 충돌하지 않게 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIFocusIndicator : MonoBehaviour
    {
        [Header("모양")]
        [SerializeField] private Color _color = new(0.42f, 0.85f, 1f, 1f);
        [SerializeField] private float _thickness = 2.5f;
        [Tooltip("선택 대상 rect보다 바깥으로 얼마나 키울지(px).")]
        [SerializeField] private float _outset = 3f;

        [Header("동작")]
        [Tooltip("게임패드가 활성 장치일 때만 표시한다.")]
        [SerializeField] private bool _gamepadOnly = true;

        [Tooltip("0이면 즉시 이동. 값이 클수록 빠르게 따라간다.")]
        [SerializeField] private float _followSpeed = 22f;

        [Tooltip("테두리 밝기 맥동 주기(초). 0이면 맥동 없음.")]
        [SerializeField] private float _pulsePeriod = 1.4f;

        private RectTransform _self;
        private RectTransform _frame;
        private readonly Image[] _edges = new Image[4];
        private CanvasGroup _group;
        private GameObject _tracked;
        private bool _hasPose;
        private GameObject _presentationSelection;
        private bool _suppressForPresentation;
        private RectTransform _presentationTarget;

        private void Awake()
        {
            _self = transform as RectTransform;
            if (_self == null)
            {
                Debug.LogError("[UIFocusIndicator] RectTransform이 있는 UI 오브젝트에 붙여야 합니다.");
                enabled = false;
                return;
            }

            BuildFrame();
        }

        private void BuildFrame()
        {
            var frameObject = new GameObject("FocusFrame", typeof(RectTransform), typeof(CanvasGroup));
            frameObject.transform.SetParent(_self, false);
            _frame = (RectTransform)frameObject.transform;
            _frame.anchorMin = _frame.anchorMax = new Vector2(0.5f, 0.5f);
            _frame.pivot = new Vector2(0.5f, 0.5f);

            _group = frameObject.GetComponent<CanvasGroup>();
            // 테두리는 순수 장식이다. 레이캐스트를 먹으면 아래 버튼 클릭을 막는다.
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            for (int i = 0; i < _edges.Length; i++)
            {
                var edgeObject = new GameObject($"Edge_{i}", typeof(RectTransform), typeof(Image));
                edgeObject.transform.SetParent(_frame, false);
                var image = edgeObject.GetComponent<Image>();
                image.color = _color;
                image.raycastTarget = false;
                _edges[i] = image;
            }

            LayoutEdges();
        }

        // 상/하/좌/우 네 변을 프레임 rect에 맞춘다. 스프라이트 없이 Image는 흰 사각형을 그린다.
        private void LayoutEdges()
        {
            SetEdge(_edges[0], new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, _thickness)); // 상
            SetEdge(_edges[1], new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, _thickness)); // 하
            SetEdge(_edges[2], new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(_thickness, 0f)); // 좌
            SetEdge(_edges[3], new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(_thickness, 0f)); // 우
        }

        private static void SetEdge(Image image, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
        {
            // OnValidate는 프레임 자식이 복원되는 도중에도 호출될 수 있다.
            if (image == null)
                return;

            var rect = (RectTransform)image.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = sizeDelta;
        }

        private void LateUpdate()
        {
            if (_frame == null)
                return;

            RectTransform target = ResolveTarget();
            if (target == null)
            {
                _group.alpha = 0f;
                _tracked = null;
                _hasPose = false;
                return;
            }

            // 대상이 바뀌면 보간하지 않고 바로 스냅한다. 화면을 가로질러 날아가면 산만하다.
            bool snap = _followSpeed <= 0f || !_hasPose || target.gameObject != _tracked;
            _tracked = target.gameObject;

            if (!TryGetLocalRect(target, out Vector2 center, out Vector2 size))
            {
                _group.alpha = 0f;
                _hasPose = false;
                return;
            }

            size += Vector2.one * (_outset * 2f);

            if (snap)
            {
                _frame.anchoredPosition = center;
                _frame.sizeDelta = size;
                _hasPose = true;
            }
            else
            {
                float t = Mathf.Clamp01(Time.unscaledDeltaTime * _followSpeed);
                _frame.anchoredPosition = Vector2.Lerp(_frame.anchoredPosition, center, t);
                _frame.sizeDelta = Vector2.Lerp(_frame.sizeDelta, size, t);
            }

            _group.alpha = _pulsePeriod > 0f
                ? Mathf.Lerp(0.55f, 1f, Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / _pulsePeriod) * 0.5f + 0.5f)
                : 1f;
        }

        private RectTransform ResolveTarget()
        {
            if (_gamepadOnly && Svc.Input?.ActiveDevice != ActiveInputDevice.Gamepad)
                return null;

            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected == null || !selected.activeInHierarchy)
                return null;

            // 상호작용 불가 상태를 강조하면 오해를 준다.
            var selectable = selected.GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable())
                return null;

            ResolvePresentation(selected);
            if (_suppressForPresentation)
                return null;

            return _presentationTarget != null
                ? _presentationTarget
                : selected.transform as RectTransform;
        }

        /// <summary>
        /// 선택 오브젝트 또는 부모가 자체 포커스 표현을 제공하면 그 정책을 따른다.
        /// 같은 선택을 추적하는 동안에는 컴포넌트 탐색 결과를 재사용한다.
        /// </summary>
        private void ResolvePresentation(GameObject selected)
        {
            if (_presentationSelection == selected)
                return;

            _presentationSelection = selected;
            _suppressForPresentation = false;
            _presentationTarget = null;

            Transform current = selected.transform;
            while (current != null)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is not IUIFocusPresentation presentation)
                        continue;

                    _suppressForPresentation = presentation.SuppressGlobalFocusIndicator;
                    _presentationTarget = presentation.GlobalFocusIndicatorTarget;
                    return;
                }

                current = current.parent;
            }
        }

        /// <summary>선택 대상의 rect를 표시기 부모 좌표계로 옮긴다.</summary>
        private bool TryGetLocalRect(RectTransform target, out Vector2 center, out Vector2 size)
        {
            center = default;
            size = default;

            Rect rect = target.rect;
            if (rect.width <= 0f && rect.height <= 0f)
                return false;

            Vector3 worldCenter = target.TransformPoint(rect.center);
            Vector3 worldMax = target.TransformPoint(rect.max);

            center = _self.InverseTransformPoint(worldCenter);
            Vector2 localMax = _self.InverseTransformPoint(worldMax);
            size = (localMax - center) * 2f;
            size = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
            return true;
        }

        private void OnValidate()
        {
            if (_frame == null)
                return;

            for (int i = 0; i < _edges.Length; i++)
            {
                if (_edges[i] != null)
                    _edges[i].color = _color;
            }

            LayoutEdges();
        }
    }
}
