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

    public void Init(GameActor actor, Camera targetCamera, Canvas parentCanvas)
    {
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
        _parentCanvasRect = parentCanvas.GetComponent<RectTransform>();

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

    private void LateUpdate()
    {
        if (!_isInitialized || _target == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdatePosition();
        UpdateDelayFill();

        if (_isShowing && (Time.time > _lastDisplayedTime + _displayTime))
        {
            if (_detection == null)
                return;
            
            bool hasTarget = (_target != null) && _detection.HasTarget;
            if (hasTarget == false)
            {
                Hide();
            }
        }
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
        _canvasGroup.alpha = behindCamera ? 0f : 1f;
        _canvasGroup.blocksRaycasts = !behindCamera;
        if (behindCamera) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentCanvasRect,
            screenPos,
            null,
            out var localPoint
        );
        _rect.anchoredPosition = localPoint;
    }

    private void UpdateDelayFill()
    {
        if (_fillHpDelayImage != null && _fillHpDelayImage.fillAmount > _targetHpFill)
        {
            _fillHpDelayImage.fillAmount = Mathf.Lerp(
                _fillHpDelayImage.fillAmount,
                _targetHpFill,
                Time.deltaTime * _delayFillSpeed
            );
        }
        
        if (_fillPoiseDelayImage != null && _fillPoiseDelayImage.fillAmount > _targetPoiseFill)
        {
            _fillPoiseDelayImage.fillAmount = Mathf.Lerp(
                _fillPoiseDelayImage.fillAmount,
                _targetPoiseFill,
                Time.deltaTime * _delayFillSpeed
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
}
