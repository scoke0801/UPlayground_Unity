using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

public class UIHudPartyEntry : MonoBehaviour
{
    [SerializeField] private Image _characterIcon;
    [SerializeField] private Image _characterIconBG;
    [SerializeField] private Image _skillGuageFill;

    [SerializeField] private GameObject _fxObject;
    [SerializeField] private Animator _animator;

    [Header("Animation Settings")]
    [SerializeField] private float _skillFillSpeed = 8.0f;

    private CharacterActorType _boundType = CharacterActorType.None;
    private Coroutine _skillGaugeCoroutine;
    private float _skillTargetRatio;
    private bool _isInCombat;

    public CharacterActorType BoundType => _boundType;

    public void Bind(CharacterActorType type, PartyMemberDataSO memberData)
    {
        _boundType = type;

        if (_characterIcon != null && memberData != null)
            _characterIcon.sprite = memberData.GetHeadSprite(type);

        if (_skillGuageFill != null) _skillGuageFill.fillAmount = 0f;
        _skillTargetRatio = 0f;
        _isInCombat = false;

        if (_fxObject != null) _fxObject.SetActive(false);

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

        gameObject.SetActive(false);
    }

    // ─── 추후 연동 예정 ────────────────────────────────────────────

    public void SetIsInCombat(bool isInCombat)
    {
        _isInCombat = isInCombat;
        if (!_isInCombat && _fxObject != null)
            _fxObject.SetActive(false);
    }

    public void SetSkillGauge(float current, float max)
    {
        if (_skillGuageFill == null) return;

        _skillTargetRatio = current / max;

        bool isFullGauge = Mathf.Approximately(_skillTargetRatio, 1f);
        if (_animator != null)
            _animator.SetBool("IsSkillGaugeFull", isFullGauge);

        if (_fxObject != null)
            _fxObject.SetActive(_isInCombat && isFullGauge);

        if (_skillGaugeCoroutine != null) StopCoroutine(_skillGaugeCoroutine);
        _skillGaugeCoroutine = StartCoroutine(SkillGaugeLerpCoroutine());
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
