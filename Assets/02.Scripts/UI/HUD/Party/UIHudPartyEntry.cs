using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

public class UIHudPartyEntry : MonoBehaviour
{
    [SerializeField] private Image _characterIcon;
    [SerializeField] private Image _characterIconBG;
    [SerializeField] private Image _hpFill;
    [SerializeField] private Image _skillGuageFill;
    [SerializeField] private GameObject _swapCooldownRoot;
    [SerializeField] private Image _swapCooldownFill;
    [SerializeField] private TextMeshProUGUI _swapCooldownText;
    [SerializeField] private GameObject _glowObject;

    [SerializeField] private Animator _animator;

    [Header("Animation Settings")]
    [SerializeField] private float _skillFillSpeed = 8.0f;

    private CharacterActorType _boundType = CharacterActorType.None;
    private Coroutine _skillGaugeCoroutine;
    private float _skillTargetRatio;

    public CharacterActorType BoundType => _boundType;

    public void Bind(CharacterActorType type, PartyMemberDataSO memberData)
    {
        _boundType = type;

        if (_characterIcon != null && memberData != null)
            _characterIcon.sprite = memberData.GetHeadSprite(type);

        if (_skillGuageFill != null) _skillGuageFill.fillAmount = 0f;
        if (_hpFill != null) _hpFill.fillAmount = 1f;
        if (_glowObject != null) _glowObject.SetActive(false);
        SetSwapCooldown(0f, 0f);
        _skillTargetRatio = 0f;

        gameObject.SetActive(true);
    }

    public void Unbind()
    {
        _boundType = CharacterActorType.None;

        if (_skillGaugeCoroutine != null)
        {
            StopCoroutine(_skillGaugeCoroutine);
            _skillGaugeCoroutine = null;
        }

        if (_glowObject != null) _glowObject.SetActive(false);
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
        if (_skillGuageFill == null) return;

        float nextRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        bool isFullGauge = Mathf.Approximately(nextRatio, 1f);
        if (_animator != null)
            _animator.SetBool("IsSkillGaugeFull", isFullGauge);

        if (_glowObject != null)
            _glowObject.SetActive(isFullGauge);

        if (Mathf.Approximately(_skillTargetRatio, nextRatio)) return;

        _skillTargetRatio = nextRatio;
        if (_skillGaugeCoroutine != null) StopCoroutine(_skillGaugeCoroutine);
        _skillGaugeCoroutine = StartCoroutine(SkillGaugeLerpCoroutine());
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

    private IEnumerator SkillGaugeLerpCoroutine()
    {
        while (Mathf.Abs(_skillGuageFill.fillAmount - _skillTargetRatio) > 0.001f)
        {
            _skillGuageFill.fillAmount = Mathf.Lerp(
                _skillGuageFill.fillAmount,
                _skillTargetRatio,
                Time.deltaTime * _skillFillSpeed);
            yield return null;
        }

        _skillGuageFill.fillAmount = _skillTargetRatio;
    }
}
