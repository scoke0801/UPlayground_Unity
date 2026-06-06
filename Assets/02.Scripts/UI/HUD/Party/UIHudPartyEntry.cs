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
    [Tooltip("대상 파티원이 궁극기를 사용할 수 있을 때 켜는 UI")]
    [SerializeField] private GameObject _glowObject;

    [SerializeField] private Animator _animator;

    private CharacterActorType _boundType = CharacterActorType.None;

    public CharacterActorType BoundType => _boundType;

    public void Bind(CharacterActorType type, PartyMemberDataSO memberData)
    {
        _boundType = type;

        if (_characterIcon != null && memberData != null)
            _characterIcon.sprite = memberData.GetHeadSprite(type);

        if (_hpFill != null) _hpFill.fillAmount = 1f;
        SetSpawned(false);
        SetUltimateReady(false);
        SetSwapCooldown(0f, 0f);

        gameObject.SetActive(true);
    }

    public void Unbind()
    {
        _boundType = CharacterActorType.None;

        SetSpawned(false);
        SetUltimateReady(false);
        SetSwapCooldown(0f, 0f);
        gameObject.SetActive(false);
    }

    // ─── 추후 연동 예정 ────────────────────────────────────────────

    public void SetIsInCombat(bool isInCombat)
    {
    }

    public void SetHealth(float current, float max)
    {
        if (_hpFill == null) return;

        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        _hpFill.fillAmount = ratio;
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
            _glowObject.SetActive(ready);
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
            _swapCooldownText.text = isVisible ? Mathf.CeilToInt(safeRemaining).ToString() : string.Empty;
        }
    }
}
