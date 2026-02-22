using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;

public class UI_ActorHpBar : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private Image _fillDelayImage;
    [SerializeField] private float _delayFillSpeed = 3f;
    [SerializeField] private TextMeshProUGUI _textHp;
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

    private float _targetFill;
    private bool _isInitialized;
    private bool _isShowing = false;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public void Init(GameActor actor, Camera targetCamera, Canvas parentCanvas)
    {
        _target = actor.transform;
        _headSocket = actor?.GetSocket(socketType: ActorSocketType.UI_HpBar);

        MonsterActor monster = actor as MonsterActor;
        if (monster != null)
        {
            _detection = monster.Detection;
        }
        
        _mainCamera = targetCamera;
        _parentCanvasRect = parentCanvas.GetComponent<RectTransform>();

        _fillImage.fillAmount = 1f;
        _fillDelayImage.fillAmount = 1f;
        _targetFill = 1f;

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
        gameObject.SetActive(!behindCamera);
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
        if (_fillDelayImage.fillAmount > _targetFill)
        {
            _fillDelayImage.fillAmount = Mathf.Lerp(
                _fillDelayImage.fillAmount,
                _targetFill,
                Time.deltaTime * _delayFillSpeed
            );
        }
    }

    public void UpdateHealth(float current, float max)
    {
        Show();
        
        _targetFill = Mathf.Clamp01(current / max);
        _fillImage.fillAmount = _targetFill;

        _textHp.text = $"{(int)current}/{(int)max}";
        
        _lastDisplayedTime = Time.time;
    }
}