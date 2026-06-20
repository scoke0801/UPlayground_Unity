using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Data.Event;
using UPlayGround.Data.EnumType;

/// <summary>
/// 몬스터 공격 윈드업 동안 적 몸통(락온 포커스 지점) 위에 표시되는 Danger Ring.
/// 명조 Weakness Halo 스타일로 큰 크기에서 작은 크기로 수축하며, 가장 작아지는 순간이
/// 실제 타격 순간과 정렬되도록 한다.
///
/// 타이밍 소스: 수축 진행도를 고정 duration으로 흘려보내지 않고,
/// 매 프레임 타임라인의 다음 <see cref="BeginCollisionEvent"/>까지 남은 시간을 직접 질의해 재페이싱한다.
/// 따라서 애니메이션 재생 속도·히트스톱으로 타이밍이 변해도 충돌 순간에 정확히 최소 크기로 수렴한다.
/// 라이브 질의가 불가능할 때(모션셋 전환/이벤트 경과 등)만 초기 duration 기반 시간 진행으로 폴백한다.
///
/// 완료(<see cref="CompleteNow"/>)는 EnemyCombat의 Collision 이벤트 발화 시점에 동기화되며,
/// 즉시 사라지는 대신 짧은 ease-out 수축 + 페이드아웃으로 자연스럽게 꺼진다.
///
/// 위치 추적·카메라 뒤 처리는 UI_ActorHpBar 패턴을 따른다.
/// 진행은 UI_WorldSpaceHudLayer의 일괄 LateUpdate에서 스케일 시간을 전달받으므로
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

    [Header("Close (타격 순간)")]
    [Tooltip("Collision(타격) 발화 시 최소 크기로 마무리하며 페이드아웃하는 시간(초). 0이면 즉시 해제.")]
    [SerializeField] private float _closeDuration = 0.08f;

    private GameActor     _actor;
    private Transform     _target;
    private Transform     _socket;
    private Camera        _mainCamera;
    private RectTransform _rect;
    private RectTransform _parentCanvasRect;
    private CanvasGroup   _canvasGroup;

    private float   _duration;
    private float   _shrinkProgress; // 0(시작/큰 크기) → 1(최소 크기). 단조 증가.
    private bool    _completed;
    private bool    _isInitialized;
    private Vector3 _baseScale = Vector3.one;

    // Close(타격 순간 마무리) 상태
    private bool    _closing;
    private float   _closeElapsed;
    private Vector3 _closeStartScale;
    private UI_WorldSpaceHudLayer _owner;
    private GameObject _poolKey;

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
    public void Init(
        GameActor actor,
        Camera camera,
        RectTransform parentCanvasRect,
        float duration,
        AttackDefenseType defenseType,
        UI_WorldSpaceHudLayer owner,
        GameObject poolKey)
    {
        _owner = owner;
        _poolKey = poolKey;
        _actor  = actor;
        _target = actor != null ? actor.transform : null;
        _socket = ResolveAnchorSocket(actor);

        _mainCamera       = camera;
        _parentCanvasRect = parentCanvasRect;

        _duration       = Mathf.Max(0.01f, duration);
        _shrinkProgress = 0f;
        _completed      = false;
        _closing        = false;

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

    public bool ManagedLateTick(float deltaTime, float unscaledTime)
    {
        if (!_isInitialized) return false;

        // 타겟 소멸 시 자가 정리 (UI_ActorHpBar와 동일)
        if (_target == null)
        {
            Release();
            return false;
        }

        UpdatePosition();

        if (_closing)
        {
            UpdateClose(deltaTime);
            return _isInitialized;
        }

        UpdateShrink(deltaTime);
        return _isInitialized;
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
        if (behindCamera)
        {
            _canvasGroup.alpha = 0f;
            return;
        }
        // Close(페이드아웃) 중에는 alpha를 UpdateClose가 제어하므로 덮어쓰지 않는다.
        if (!_closing) _canvasGroup.alpha = 1f;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentCanvasRect,
            screenPos,
            null,
            out var localPoint);
        _rect.anchoredPosition = localPoint;
    }

    private void UpdateShrink(float deltaTime)
    {
        if (_completed || _rect == null) return;

        // 라이브 재페이싱: Collision/투사체 발사 이벤트까지 남은 시간으로 진행도를 직접 산출한다.
        // 질의가 불가능하면 초기 duration 기반 시간 진행으로 폴백한다.
        float target = TryGetCollisionProgress(out float live)
            ? live
            : _shrinkProgress + deltaTime / _duration;

        // 단조 증가 보장 — 다음 Collision 이벤트로 질의가 점프해도 링이 다시 커지지 않도록.
        _shrinkProgress  = Mathf.Clamp01(Mathf.Max(_shrinkProgress, target));
        _rect.localScale = Vector3.Lerp(_baseScale * _startScaleMultiplier, _baseScale, _shrinkProgress);
    }

    /// <summary>
    /// 타임라인의 다음 <see cref="BeginCollisionEvent"/> 또는 <see cref="SpawnProjectileEvent"/> 중
    /// 더 먼저 시작되는 이벤트까지 남은 시간으로 수축 진행도(0→1)를 산출한다.
    /// 근접 공격은 충돌, 원거리 공격은 투사체 발사 순간이 곧 타격 타이밍이므로 둘 다 목표 후보로 삼는다.
    /// 초기 윈드업 창(_duration) 대비 줄어든 비율이 곧 진행도이며, 목표 순간 1(최소 크기)에 수렴한다.
    /// 애니메이션 속도/히트스톱 변화에 자동으로 페이싱이 맞춰진다.
    /// </summary>
    private bool TryGetCollisionProgress(out float progress)
    {
        progress = 0f;

        ActorAnimator animator = _actor != null ? _actor.Animator : null;
        if (animator == null) return false;

        // Collision(타격)과 SpawnProjectile(투사체 발사) 중 더 먼저 시작되는 이벤트를 목표로 삼는다.
        // duration 산출부(EnemyCombat.ResolveDangerRingDuration)와 동일한 규칙을 공유해야
        // 진행도가 0에서 시작해 목표 순간 1에 수렴한다(어긋나면 링이 안 줄어듦).
        if (!animator.TryGetTimeUntilNextEvent<BeginCollisionEvent, SpawnProjectileEvent>(out float untilTarget))
            return false;

        // 남은 시간이 0 이하 = 사실상 타격/발사 순간 → 최소 크기.
        if (untilTarget <= 0f) { progress = 1f; return true; }

        progress = 1f - Mathf.Clamp01(untilTarget / _duration);
        return true;
    }

    /// <summary>
    /// Collision 이벤트 발화(타격 순간) 시 호출. 남은 수축을 짧은 ease-out + 페이드아웃으로
    /// 마무리한 뒤 해제한다(팝 없이 자연스럽게). EnemyCombat.SetEnableCollision(true)에서 트리거된다.
    /// </summary>
    public void CompleteNow()
    {
        if (_completed) return;
        _completed = true; // 수축 업데이트 중단

        // 즉시 해제 경로: 닫기 시간 0 이거나 참조 누락 시 최소 크기로 스냅 후 파괴.
        if (_rect == null || _canvasGroup == null || _closeDuration <= 0f)
        {
            if (_rect != null) _rect.localScale = _baseScale;
            Release();
            return;
        }

        _closing         = true;
        _closeElapsed    = 0f;
        _closeStartScale = _rect.localScale;
    }

    private void UpdateClose(float deltaTime)
    {
        _closeElapsed += deltaTime;
        float t     = Mathf.Clamp01(_closeElapsed / _closeDuration);
        float eased = 1f - (1f - t) * (1f - t); // ease-out — 남은 수축을 부드럽게 마무리

        _rect.localScale   = Vector3.Lerp(_closeStartScale, _baseScale, eased);
        _canvasGroup.alpha = 1f - t;

        if (t >= 1f) Release();
    }

    /// <summary> 텔레그래프 정리 시 호출 (EnemyCombat.ClearTelegraphs). </summary>
    public void Release()
    {
        if (!_isInitialized)
            return;

        _isInitialized = false;
        _actor = null;
        _target = null;
        _socket = null;
        _closing = false;
        _completed = false;
        _owner?.ReturnDangerRingToPool(this, _poolKey);
        _owner = null;
        _poolKey = null;
    }
}
