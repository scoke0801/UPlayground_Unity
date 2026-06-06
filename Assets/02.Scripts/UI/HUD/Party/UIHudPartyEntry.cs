using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

public class UIHudPartyEntry : MonoBehaviour
{
    [SerializeField] private Image _characterIcon;
    
    [SerializeField] private Image _hpFill;
    
    [SerializeField] private GameObject _swapCooldownRoot;
    [SerializeField] private Image _swapCooldownFill;
    [SerializeField] private TextMeshProUGUI _swapCooldownText;
    [Tooltip("대상 파티원이 현재 필드에 스폰되어 있을 때 켜는 UI")]
    [SerializeField] private GameObject _spawnedObject;
    [Tooltip("대상 파티원이 Ultimate를 사용할 수 있을 때 켜는 UI")]
    [SerializeField] private GameObject _glowObject;

    [SerializeField] private Animator _animator;

    [Tooltip("쿨타임 텍스트 표시 형식. 예: 0.0 = 소수 1자리, 0.00 = 소수 2자리")]
    [SerializeField] private string _cooldownTextFormat = "0.0";

    [Tooltip("대상 파티원이 전투 불능(사망) 상태일 때 적용할 딤드 알파")]
    [SerializeField, Range(0f, 1f)] private float _deadDimAlpha = 0.4f;

    private CharacterActorType _boundType = CharacterActorType.None;

    private CanvasGroup _canvasGroup;
    private bool _isDead;

    public CharacterActorType BoundType => _boundType;

    public bool IsDead => _isDead;

    private CanvasGroup CanvasGroup
    {
        get
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            return _canvasGroup;
        }
    }

    public void Bind(CharacterActorType type, PartyMemberDataSO memberData)
    {
        _boundType = type;

        if (_characterIcon != null && memberData != null)
            _characterIcon.sprite = memberData.GetHeadSprite(type);

        if (_hpFill != null) _hpFill.fillAmount = 1f;
        SetSpawned(false);
        SetUltimateReady(false);
        SetSwapCooldown(0f, 0f);
        SetDead(false);

        gameObject.SetActive(true);
    }

    public void Unbind()
    {
        _boundType = CharacterActorType.None;

        SetSpawned(false);
        SetUltimateReady(false);
        SetSwapCooldown(0f, 0f);
        SetDead(false);
        gameObject.SetActive(false);
    }

    // ─── 추후 연동 예정 ────────────────────────────────────────────

    public void SetIsInCombat(bool isInCombat)
    {
    }

    public void SetHealth(float current, float max)
    {
        // max가 0인 초기화 프레임(데이터 미바인딩)에서 current=0으로 오탐 사망 처리되는 것을 방지
        SetDead(max > 0f && current <= 0f);

        if (_hpFill == null) return;

        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        _hpFill.fillAmount = ratio;
    }

    public void SetDead(bool isDead)
    {
        _isDead = isDead;
        
        CanvasGroup.alpha = isDead ? _deadDimAlpha : 1f;
        if (isDead)
        {
            SetUltimateReady(false);
        }
    }

    public void SetSkillGauge(float current, float max)
    {
        SetUltimateReady(max > 0f && current >= max);
    }

    public void SetSelected(bool selected)
    {
        SetSpawned(selected);
    }

    public void SetSpawned(bool spawned)
    {
        if (_spawnedObject != null)
            _spawnedObject.SetActive(spawned);
    }

    public void SetUltimateReady(bool ready)
    {
        if (_glowObject != null)
            _glowObject.SetActive(!_isDead && ready);
    }

    public void SetSwapCooldown(float remaining, float duration)
    {
        float safeRemaining = Mathf.Max(0f, remaining);
        float ratio = duration > 0f ? Mathf.Clamp01(safeRemaining / duration) : 0f;
        bool isVisible = safeRemaining > 0f;

        if (_swapCooldownRoot != null)
            _swapCooldownRoot.SetActive(isVisible);

        if (_swapCooldownFill != null)
            _swapCooldownFill.fillAmount = ratio;

        if (_swapCooldownText != null)
        {
            _swapCooldownText.gameObject.SetActive(isVisible);
            _swapCooldownText.text = isVisible ? safeRemaining.ToString(_cooldownTextFormat) : string.Empty;
        }
    }
}
