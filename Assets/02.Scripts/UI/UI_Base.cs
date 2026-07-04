using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;
using UPlayGround.Data.Sound;
using UPlayGround.InputDefine;

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

    [SerializeField]protected Animator _animator;
    [Header("Sound")]
    [SerializeField] private bool _playDefaultButtonSound = true;
    private readonly List<Button> _soundBoundButtons = new();
    
    #endregion

    #region 속성

    /// <summary>
    /// UI가 속한 캔버스 레이어
    /// </summary>
    [SerializeField] protected CanvasLayer _layer = CanvasLayer.Scene;
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

    public bool IsCanCloseWithEsc => _canCloseWithEsc;
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
       //     Close();
        }
    }

    protected virtual void OnDestroy()
    {
        // Hide를 거치지 않고 파괴되는 경우(Close, 씬 전환 등)에도
        // 커서/입력 레이어/입력 이벤트 정리 누수를 막는다.
        if (IsVisible || _cursorVisiblePushed || _inputLayerRaised)
            Hide();

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
        BindDefaultButtonSounds();
        IsInitialized = true;
    }

    private void BindDefaultButtonSounds()
    {
        if (!_playDefaultButtonSound)
            return;

        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            if (button == null
                || button.GetComponentInParent<UI_Base>(true) != this
                || _soundBoundButtons.Contains(button))
                continue;

            button.onClick.AddListener(PlayDefaultButtonSound);
            _soundBoundButtons.Add(button);
        }
    }

    private static void PlayDefaultButtonSound()
    {
        SoundManager.Instance?.PlayUi(GameSoundKey.UiClick);
    }

    /// <summary>
    /// 이 UI가 표시되는 동안 마우스 커서를 보여야 하는 레이어인지 여부.
    /// Scene(Level_1) 이상의 모달 UI는 커서가 필요하고, HUD/WorldSpace는 제외한다.
    /// (HUD 레이어라도 커서가 필요한 특수 UI는 개별적으로 ShowCursor를 호출한다.)
    /// </summary>
    protected virtual bool RequiresCursorVisible =>
        _layer >= CanvasLayer.Scene && _layer < CanvasLayer.WorldSpace;

    // RequiresCursorVisible 조건으로 커서 스택을 push했는지 추적한다.
    // 가시성(wasVisible)이 아니라 이 플래그로 push/pop을 짝 맞춰,
    // 도중에 _layer가 바뀌거나 Hide 없이 파괴되는 경우에도 스택 누수를 막는다.
    private bool _cursorVisiblePushed;

    /// <summary>
    /// 이 UI가 열려 있는 동안 게임플레이 등 하위 레이어 입력을 차단할지 여부.
    /// 전체 화면 메뉴·다이얼로그처럼 입력을 독점해야 하는 모달은 true로 오버라이드한다.
    /// (Scene 레이어라도 월드 상호작용 프롬프트처럼 입력을 막으면 안 되는 UI는 기본값 false 유지.)
    /// </summary>
    protected virtual bool BlocksLowerInput => false;

    // UIManager가 입력 레이어 재계산 시 "차단 모달"만 필터링하기 위해 읽는다.
    public bool BlocksInput => BlocksLowerInput;

    // BlocksLowerInput으로 입력 레이어를 올렸는지 추적해 Show/Hide 짝을 맞춘다.
    private bool _inputLayerRaised;

    /// <summary>
    /// UI 표시
    /// </summary>
    public void Show()
    {
        if (!IsInitialized)
        {
            Initialize();
        }

        if (IsVisible)
            return;

        gameObject.SetActive(true);
        IsVisible = true;

        // Scene(Level_1) 이상 모달 UI가 열려 있는 동안 커서를 표시한다(스택 push).
        // _cursorVisiblePushed로 1회만 push해 중복 Show로 스택이 새는 것을 방지.
        if (!_cursorVisiblePushed && RequiresCursorVisible)
        {
            InputManager.Instance?.ShowCursor(true);
            _cursorVisiblePushed = true;
        }

        // 입력을 독점하는 모달이면 입력 레이어를 재계산해 하위(게임플레이 등) 입력을 차단한다.
        // IsVisible이 위에서 이미 true이므로 재계산 시 자신이 포함돼 자신의 레이어까지 올라간다.
        if (!_inputLayerRaised && BlocksLowerInput)
        {
            _inputLayerRaised = true;
            InputManager.Instance?.RefreshInputLayer();
        }

        RegisterInputEvents();

        OnShow();
    }

    /// <summary>
    /// UI 숨김
    /// </summary>
    public void Hide()
    {
        if (!IsVisible && !_cursorVisiblePushed && !_inputLayerRaised)
            return;

        IsVisible = false;

        // Show에서 push한 커서 표시를 짝 맞춰 pop.
        if (_cursorVisiblePushed)
        {
            InputManager.Instance?.ShowCursor(false);
            _cursorVisiblePushed = false;
        }

        // Show에서 올린 입력 레이어를 복원한다(가시 차단 모달 기준으로 재계산).
        // IsVisible은 위에서 이미 false이므로 재계산이 자신을 제외한다.
        // 남아 있는 차단 모달이 없으면 Level_0(게임플레이)으로 내려간다.
        if (_inputLayerRaised)
        {
            InputLayer previousLayer = InputManager.Instance != null
                ? InputManager.Instance.CurrentLayer
                : InputLayer.None;

            _inputLayerRaised = false;
            InputManager.Instance?.RefreshInputLayer();

            if (previousLayer > InputLayer.Level_0
                && InputManager.Instance != null
                && InputManager.Instance.CurrentLayer == InputLayer.Level_0)
            {
                InputManager.Instance.SuppressPlayerActionInputBriefly();
            }
        }

        UnRegisterInputEvents();

        OnHide();
        if(this.gameObject != null)
        {
            gameObject.SetActive(false);

        }
    }
    

    /// <summary>
    /// UI 닫기 (제거)
    /// </summary>
    public virtual void Close()
    {
        if (IsVisible)
            Hide();

        OnClose();
    }

    #endregion

    #region 추상/가상 메서드 (상속 클래스에서 구현)

    protected virtual void RegisterInputEvents()
    {
        
    }

    protected virtual void UnRegisterInputEvents()
    {
        
    }

    public virtual bool PerformBackFunction()
    {
        Hide();
        return true;
    }
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
        foreach (var button in _soundBoundButtons)
        {
            if (button != null)
                button.onClick.RemoveListener(PlayDefaultButtonSound);
        }

        _soundBoundButtons.Clear();
    }

    #endregion

    #region 유틸리티

    public void AnimationChange(string animKey)
    {
        if (_animator)
        {
            _animator.SetTrigger(animKey);
        }
    }

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
