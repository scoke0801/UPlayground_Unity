using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모든 UI의 기본 클래스
/// UIManager와 연동하여 생명주기를 관리합니다.
/// </summary>
[RequireComponent(typeof(Canvas))]
public abstract class UI_Base : MonoBehaviour
{
    #region 컴포넌트

    protected Canvas _canvas;
    protected CanvasGroup _canvasGroup;
    protected RectTransform _rectTransform;

    #endregion

    #region 속성

    /// <summary>
    /// UI가 속한 캔버스 레이어
    /// </summary>
    [SerializeField] protected CanvasLayer _layer = CanvasLayer.Normal;
    public CanvasLayer Layer => _layer;

    /// <summary>
    /// UI가 표시되어 있는지 여부
    /// </summary>
    public bool IsVisible { get; private set; }

    /// <summary>
    /// 초기화 완료 여부
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// ESC 키로 닫기 가능 여부
    /// </summary>
    [SerializeField] protected bool _canCloseWithEsc = true;

    #endregion

    #region Unity 생명주기

    protected virtual void Awake()
    {
        // 컴포넌트 캐싱
        _canvas = GetComponent<Canvas>();
        _rectTransform = GetComponent<RectTransform>();

        // CanvasGroup이 없으면 추가 (페이드 효과용)
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    protected virtual void Update()
    {
        // ESC 키 입력 처리
        if (_canCloseWithEsc && IsVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    protected virtual void OnDestroy()
    {
        OnDispose();
    }

    #endregion

    #region 생명주기 메서드

    /// <summary>
    /// UI 초기화 (최초 1회만 호출)
    /// </summary>
    public void Initialize()
    {
        if (IsInitialized)
            return;

        OnInit();
        IsInitialized = true;
    }

    /// <summary>
    /// UI 표시
    /// </summary>
    public void Show()
    {
        if (!IsInitialized)
        {
            Initialize();
        }

        gameObject.SetActive(true);
        IsVisible = true;

        OnShow();
    }

    /// <summary>
    /// UI 숨김
    /// </summary>
    public void Hide()
    {
        IsVisible = false;
        OnHide();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// UI 닫기 (제거)
    /// </summary>
    public virtual void Close()
    {
        OnClose();
        Destroy(gameObject);
    }

    #endregion

    #region 추상/가상 메서드 (상속 클래스에서 구현)

    /// <summary>
    /// 초기화 로직 구현
    /// </summary>
    protected virtual void OnInit()
    {
        // 버튼 바인딩, 데이터 로드 등
    }

    /// <summary>
    /// 표시될 때 호출
    /// </summary>
    protected virtual void OnShow()
    {
        // 애니메이션, 데이터 갱신 등
    }

    /// <summary>
    /// 숨겨질 때 호출
    /// </summary>
    protected virtual void OnHide()
    {
        // 애니메이션 등
    }

    /// <summary>
    /// 닫힐 때 호출
    /// </summary>
    protected virtual void OnClose()
    {
        // 저장, 정리 작업 등
    }

    /// <summary>
    /// 파괴될 때 호출
    /// </summary>
    protected virtual void OnDispose()
    {
        // 이벤트 해제, 리소스 정리 등
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 페이드 인 효과
    /// </summary>
    public void FadeIn(float duration = 0.3f, Action onComplete = null)
    {
        if (_canvasGroup == null)
            return;

        StopAllCoroutines();
        StartCoroutine(FadeCoroutine(0f, 1f, duration, onComplete));
    }

    /// <summary>
    /// 페이드 아웃 효과
    /// </summary>
    public void FadeOut(float duration = 0.3f, Action onComplete = null)
    {
        if (_canvasGroup == null)
            return;

        StopAllCoroutines();
        StartCoroutine(FadeCoroutine(1f, 0f, duration, onComplete));
    }

    /// <summary>
    /// 페이드 효과 코루틴
    /// </summary>
    private System.Collections.IEnumerator FadeCoroutine(float from, float to, float duration, Action onComplete)
    {
        float elapsed = 0f;
        _canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        _canvasGroup.alpha = to;
        onComplete?.Invoke();
    }

    /// <summary>
    /// UI 상호작용 활성화/비활성화
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
        }
    }

    #endregion
}
