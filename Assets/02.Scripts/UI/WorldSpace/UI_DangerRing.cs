using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Data.EnumType;

/// <summary>
/// 몬스터 공격 윈드업 동안 적 몸통(락온 포커스 지점) 위에 표시되는 Danger Ring.
/// 명조 Weakness Halo 스타일로 큰 크기에서 작은 크기로 수축하며, 가장 작아지는 순간이
/// 실제 타격 순간과 정렬되도록 한다.
///
/// 타이밍 소스: 윈드업 시작 시 받은 duration으로 수축 속도를 페이싱하되,
/// 실제 완료(최소 크기 스냅 + 해제)는 EnemyCombat의 Collision 이벤트 발화 시점(<see cref="CompleteNow"/>)에
/// 동기화한다. 타임라인 편집으로 타이밍이 어긋나도 충돌 순간에 정확히 맞춰진다.
///
/// 위치 추적·카메라 뒤 처리는 UI_ActorHpBar 패턴을 따른다.
/// 수축은 LateUpdate에서 Time.deltaTime(스케일 시간)으로 진행되므로
/// 히트스톱/일시정지(Time.timeScale 변화) 시 자동으로 함께 멈춘다.
/// Screen Space - Overlay Canvas(UI_WorldSpaceHudLayer) 아래에 부착된다.
/// </summary>
public class UI_DangerRing : MonoBehaviour
{
    [Header("Ring")]
    [Tooltip("링 스프라이트. 수축 동안 항상 가득 그려진다(fillAmount=1).")]
    [SerializeField] private Image _fillImage;

    [Header("Anchor")]
    [Tooltip("Center(가슴) 소켓도 없을 때 사용할 몸통 중심 오프셋. 락온 포커스 지점에 맞춘다.")]
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 1.0f, 0f);

    [Header("Defense Type Colors")]
    [Tooltip("패링 가능(Parryable/GuardableOnly) — 명조 Weakness Halo 노랑")]
    [SerializeField] private Color _parryableColor = new Color(1f, 0.85f, 0.1f, 1f);
    [Tooltip("패링 불가(Unblockable) — 회피 필수 빨강")]
    [SerializeField] private Color _unblockableColor = new Color(1f, 0.18f, 0.12f, 1f);

    [Header("Shrink")]
    [Tooltip("수축 시작 배율. 기본 스케일 × 이 값에서 시작해 기본 스케일로 줄어든다.")]
    [SerializeField] private float _startScaleMultiplier = 2.5f;

    private Transform     _target;
    private Transform     _socket;
    private Camera        _mainCamera;
    private RectTransform _rect;
    private RectTransform _parentCanvasRect;
    private CanvasGroup   _canvasGroup;

    private float   _duration;
    private float   _elapsed;
    private bool    _completed;
    private bool    _isInitialized;
    private Vector3 _baseScale = Vector3.one;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (_rect != null) _baseScale = _rect.localScale;
    }

    /// <param name="duration">큰 크기→작은 크기로 수축하는 시간(초). 윈드업 시작 → 타격 간격에 맞춘다.</param>
    /// <param name="defenseType">링 색 결정. Unblockable=빨강, 그 외=노랑.</param>
    public void Init(GameActor actor, Camera camera, Canvas parentCanvas, float duration, AttackDefenseType defenseType)
    {
        _target = actor != null ? actor.transform : null;
        _socket = ResolveAnchorSocket(actor);

        _mainCamera       = camera;
        _parentCanvasRect = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>() : null;

        _duration  = Mathf.Max(0.01f, duration);
        _elapsed   = 0f;
        _completed = false;

        if (_fillImage != null)
        {
            _fillImage.fillAmount = 1f; // 수축 방식 — 링은 항상 가득 그려진다
            _fillImage.color      = ResolveColor(defenseType);
        }

        if (_rect != null) _rect.localScale = _baseScale * _startScaleMultiplier;
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

        _isInitialized = true;
    }

    /// <summary>
    /// 링을 부착할 앵커 소켓 해석. 명시적 UI_DangerRing 소켓이 최우선,
    /// 없으면 Center(가슴) 소켓 — 락온 포커스 지점에 대응한다.
    /// </summary>
    private Transform ResolveAnchorSocket(GameActor actor)
    {
        if (actor == null) return null;

        Transform socket = actor.GetSocket(ActorSocketType.UI_DangerRing);
        if (socket != null) return socket;

        return actor.GetSocket(ActorSocketType.Center);
    }

    private Color ResolveColor(AttackDefenseType defenseType) => defenseType switch
    {
        AttackDefenseType.Unblockable => _unblockableColor,
        _                             => _parryableColor, // Parryable / GuardableOnly
    };

    private void LateUpdate()
    {
        if (!_isInitialized) return;

        // 타겟 소멸 시 자가 정리 (UI_ActorHpBar와 동일)
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdatePosition();
        UpdateShrink();
    }

    private void UpdatePosition()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null || _parentCanvasRect == null) return;

        Vector3 worldPos = _socket != null
            ? _socket.position
            : _target.position + _worldOffset;

        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

        // 카메라 뒤면 숨김 (SetActive 토글 대신 alpha — UI_ActorHpBar와 동일)
        bool behindCamera = screenPos.z < 0f;
        _canvasGroup.alpha = behindCamera ? 0f : 1f;
        if (behindCamera) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentCanvasRect,
            screenPos,
            null,
            out var localPoint);
        _rect.anchoredPosition = localPoint;
    }

    private void UpdateShrink()
    {
        if (_completed || _rect == null) return;

        _elapsed += Time.deltaTime; // 스케일 시간 — 히트스톱/일시정지 시 함께 멈춤
        float t = Mathf.Clamp01(_elapsed / _duration);
        _rect.localScale = Vector3.Lerp(_baseScale * _startScaleMultiplier, _baseScale, t);
    }

    /// <summary>
    /// Collision 이벤트 발화(타격 순간) 시 호출. 최소 크기로 스냅하고 즉시 해제한다.
    /// EnemyCombat.SetEnableCollision(true)에서 트리거된다.
    /// </summary>
    public void CompleteNow()
    {
        _completed = true;
        if (_rect != null) _rect.localScale = _baseScale; // 최소(타격 임박) 크기로 스냅
        Release();
    }

    /// <summary> 텔레그래프 정리 시 호출 (EnemyCombat.ClearTelegraphs). </summary>
    public void Release()
    {
        if (this != null) Destroy(gameObject);
    }
}
