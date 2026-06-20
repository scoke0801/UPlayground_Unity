using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;

public class UI_ActorHpBar : MonoBehaviour
{
    [SerializeField] private Image _fillHpImage;
    [SerializeField] private Image _fillHpDelayImage;
    
    [SerializeField] private Image _fillPoiseImage;
    [SerializeField] private Image _fillPoiseDelayImage;

    [SerializeField] private Image _fillBreakGaugeImage;
    [SerializeField] private GameObject _breakActiveUI;
    
    [SerializeField] private float _delayFillSpeed = 3f;
    [SerializeField] private TextMeshProUGUI _textHp;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Animator _animator;
    
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 1.2f, 0f);

    [SerializeField] private float _displayTime = 5f;

    private EnemyDetection _detection;
    private Transform _target;
    private Transform _headSocket;         // 소켓 우선, 없으면 _worldOffset 사용
    private RectTransform _rect;
    private Camera _mainCamera;
    private RectTransform _parentCanvasRect;
    private float _lastDisplayedTime = 0.0f;

    private float _targetHpFill;
    private float _targetPoiseFill;
    private float _targetBreakFill;
    
    private bool _isInitialized;
    private bool _isShowing = false;
    private UI_WorldSpaceHudLayer _owner;
    
    // SetActive 토글 대신 사용 — Animator 트리거 소실 방지
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (_breakActiveUI == null)
            _breakActiveUI = transform.Find("breakEffect")?.gameObject;
    }

    public void Init(
        GameActor actor,
        Camera targetCamera,
        RectTransform parentCanvasRect,
        UI_WorldSpaceHudLayer owner)
    {
        _owner = owner;
        _target = actor.transform;
        _headSocket = actor?.GetSocket(socketType: ActorSocketType.UI_HpBar);

        int level = 1;
        MonsterActor monster = actor as MonsterActor;
        if (monster != null)
        {
            _detection = monster.Detection;
            level = monster.Level;
        }
        
        _mainCamera = targetCamera;
        _parentCanvasRect = parentCanvasRect;

        if (_fillHpImage != null) _fillHpImage.fillAmount = 1f;
        if (_fillHpDelayImage != null) _fillHpDelayImage.fillAmount = 1f;
        
        if (_fillPoiseDelayImage != null) _fillPoiseDelayImage.fillAmount = 1f;
        if (_fillPoiseImage != null) _fillPoiseImage.fillAmount = 1f;
        if (_fillBreakGaugeImage != null) _fillBreakGaugeImage.fillAmount = 0f;
        SetBreakGaugeEmptyUiActive(false);
        
        _targetHpFill = 1f;
        _targetPoiseFill = 1f;
        _targetBreakFill = 0f;

        _levelText.text = $"Lv. {level}";

        _isInitialized = true;
    }

    public bool ManagedLateTick(float deltaTime, float unscaledTime)
    {
        if (!_isInitialized || _target == null)
        {
            Release();
            return false;
        }

        // 적이 타겟을 상실하면 비전투 상태로 간주해 HP UI를 즉시 숨긴다.
        // 풀에는 반환하지 않아 같은 적이 다시 전투에 진입할 때 기존 연결을 그대로 재사용한다.
        if (_detection != null && !_detection.HasTarget)
        {
            Hide();
            SetCanvasVisible(false);
            return true;
        }

        UpdatePosition();
        UpdateDelayFill(deltaTime);

        if (_isShowing && (Time.time > _lastDisplayedTime + _displayTime))
        {
            if (_detection == null)
                return true;
            
            bool hasTarget = (_target != null) && _detection.HasTarget;
            if (hasTarget == false)
            {
                Hide();
            }
        }

        return true;
    }

    private void Show()
    {
        if (_isShowing)
        {
            return;
        }
        _isShowing = true;
        _animator.SetTrigger("Show");
    }
    private void Hide()
    {
        if (_isShowing == false)
        {
            return;
        }
        _isShowing = false;
        _animator.SetTrigger("Hide");
    }
    
    private void UpdatePosition()
    {
        // 소켓이 있으면 소켓 위치 사용, 없으면 오프셋 사용
        Vector3 worldPos = _headSocket != null
            ? _headSocket.position
            : _target.position + _worldOffset;

        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

        bool behindCamera = screenPos.z < 0f;
        // SetActive 대신 alpha 처리 — SetActive(false)시 Animator 트리거가 소실되는 버그 방지
        SetCanvasVisible(!behindCamera);
        if (behindCamera) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentCanvasRect,
            screenPos,
            null,
            out var localPoint
        );
        _rect.anchoredPosition = localPoint;
    }

    private void SetCanvasVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.blocksRaycasts = visible;
    }

    private void UpdateDelayFill(float deltaTime)
    {
        if (_fillHpDelayImage != null && _fillHpDelayImage.fillAmount > _targetHpFill)
        {
            _fillHpDelayImage.fillAmount = Mathf.Lerp(
                _fillHpDelayImage.fillAmount,
                _targetHpFill,
                deltaTime * _delayFillSpeed
            );
        }
        
        if (_fillPoiseDelayImage != null && _fillPoiseDelayImage.fillAmount > _targetPoiseFill)
        {
            _fillPoiseDelayImage.fillAmount = Mathf.Lerp(
                _fillPoiseDelayImage.fillAmount,
                _targetPoiseFill,
                deltaTime * _delayFillSpeed
            );
        }

    }

    public void UpdateHealth(float current, float max)
    {
        Show();
        
        _targetHpFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        if (_fillHpImage != null)
            _fillHpImage.fillAmount = _targetHpFill;

        int displayCurrent = current > 0f ? Mathf.CeilToInt(current) : 0;
        if (_textHp != null)
            _textHp.text = $"{displayCurrent}/{Mathf.CeilToInt(max)}";
        
        _lastDisplayedTime = Time.time;
    }
    
    public void UpdatePoise(float current, float max)
    {
        Show();
        
        _targetPoiseFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        if (_fillPoiseImage != null)
            _fillPoiseImage.fillAmount = _targetPoiseFill;
    }

    public void UpdateBreakGauge(float current, float max)
    {
        if (_fillBreakGaugeImage == null)
            return;

        Show();

        _targetBreakFill = max > 0f ? 1f - Mathf.Clamp01(current / max) : 1f;
        _fillBreakGaugeImage.fillAmount = _targetBreakFill;
    }

    public void SetBreakGaugeEmptyUiActive(bool active)
    {
        if (_breakActiveUI != null && _breakActiveUI.activeSelf != active)
            _breakActiveUI.SetActive(active);
    }

    public void Release()
    {
        if (!_isInitialized)
            return;

        _isInitialized = false;
        _target = null;
        _headSocket = null;
        _detection = null;
        _isShowing = false;
        _owner?.ReturnHpBarToPool(this);
        _owner = null;
    }
}
