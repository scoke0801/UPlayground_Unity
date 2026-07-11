using UnityEngine;
using UPlayGround;
using UPlayGround.Data.EnumType;

namespace UPlayGround.UI
{
    /// <summary>
    /// 적의 Break 게이지가 가득 차 '브레이크 공격 가능' 상태(노출)가 되었을 때
    /// 적 몸통(Center 소켓) 위에 표시되는 입력 상호작용 표시(F키 아이콘 등).
    ///
    /// 노출 상태 동안에만 활성화되며, 대여/반환은 <see cref="MonsterActor"/>가
    /// <c>MonsterBreakGauge.OnBreakExposed / OnBreakRecovered</c> 이벤트로 제어한다.
    /// 위치 추적·카메라 뒤 처리는 UI_ActorHpBar / UI_DangerRing 패턴을 그대로 따른다.
    /// Screen Space Canvas(UI_WorldSpaceHudLayer) 아래에 부착된다.
    /// </summary>
    public class UI_BreakInteraction : MonoBehaviour
    {
        [Header("Anchor")]
        [Tooltip("Center 소켓 기준(소켓 없으면 루트 기준) 월드 오프셋.")]
        [SerializeField] private Vector3 _worldOffset = Vector3.zero;

        [Header("Pulse (선택)")]
        [Tooltip("주목도를 위한 스케일 펄스 진폭. 0이면 펄스 없음.")]
        [SerializeField] private float _pulseAmplitude = 0.12f;
        [Tooltip("스케일 펄스 주파수(Hz).")]
        [SerializeField] private float _pulseFrequency = 2.5f;

        private Transform     _target;
        private Transform     _socket;
        private Camera        _mainCamera;
        private RectTransform _rect;
        private RectTransform _parentCanvasRect;
        private CanvasGroup   _canvasGroup;
        private Vector3       _baseScale = Vector3.one;
        private bool          _isInitialized;
        private UI_WorldSpaceHudLayer _owner;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (_rect != null) _baseScale = _rect.localScale;
        }

        public void Init(
            GameActor actor,
            Camera camera,
            RectTransform parentCanvasRect,
            UI_WorldSpaceHudLayer owner)
        {
            _owner = owner;
            _target = actor != null ? actor.transform : null;
            _socket = actor != null ? actor.GetSocket(ActorSocketType.Center) : null;

            _mainCamera       = camera;
            _parentCanvasRect = parentCanvasRect;

            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            if (_rect != null) _rect.localScale = _baseScale;

            _isInitialized = true;
        }

        public bool ManagedLateTick(float deltaTime, float unscaledTime)
        {
            if (!_isInitialized) return false;

            // 타겟 소멸 시 자가 정리 (UI_ActorHpBar / UI_DangerRing 패턴)
            if (_target == null)
            {
                Release();
                return false;
            }

            UpdatePosition();
            UpdatePulse(unscaledTime);
            return true;
        }

        private void UpdatePosition()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null || _parentCanvasRect == null) return;

            Vector3 worldPos = (_socket != null ? _socket.position : _target.position) + _worldOffset;
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            // 카메라 뒤면 숨김 (SetActive 토글 대신 alpha — UI_ActorHpBar / UI_DangerRing과 동일)
            bool behindCamera = screenPos.z < 0f;
            _canvasGroup.alpha = behindCamera ? 0f : 1f;
            if (behindCamera) return;

            // UI_InteractionKey와 같은 방식으로 화면 좌표를 직접 반영한다.
            // 현재 프리팹 루트가 좌하단 anchor/pivot이므로 anchoredPosition에
            // 부모 로컬 좌표를 넣으면 기준점 차이만큼 위치가 밀린다.
            _rect.position = screenPos;
        }

        private void UpdatePulse(float unscaledTime)
        {
            if (_pulseAmplitude <= 0f || _rect == null) return;

            // 히트스톱/일시정지(Time.timeScale)와 무관하게 항상 눈에 띄도록 unscaledTime 사용.
            float s = 1f + Mathf.Sin(unscaledTime * _pulseFrequency * Mathf.PI * 2f) * _pulseAmplitude;
            _rect.localScale = _baseScale * s;
        }

        public void Release()
        {
            if (!_isInitialized)
                return;

            _isInitialized = false;
            _target = null;
            _socket = null;
            _owner?.ReturnBreakInteractionToPool(this);
            _owner = null;
        }
    }
}
