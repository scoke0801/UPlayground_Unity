using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Cycle;

namespace UPlayGround.UI
{
    /// <summary>
    /// 어시스트 로스터 목록 엔트리.
    /// 일반 모드 클릭 = 장착, 교체 대기 모드 클릭 = 이 어시스트를 방출하고 신규 영입 수락.
    /// </summary>
    public class UIAssistRosterEntry : MonoBehaviour
    {
        [SerializeField] private Image           _icon;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _roleText;
        [SerializeField] private TextMeshProUGUI _cooldownText;
        [SerializeField] private GameObject      _equippedMark;
        [SerializeField] private GameObject      _replaceCandidateMark; // 교체 대기 모드에서 "방출 대상 선택 가능" 표시
        [SerializeField] private Button          _button;

        public event Action<string> OnClicked;

        private string _assistId;

        public string AssistId => _assistId;

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(() => OnClicked?.Invoke(_assistId));
        }

        public void Bind(string assistId, BossAssistDefinitionSO definition, bool equipped, float cooldownRemaining, bool replaceMode)
        {
            _assistId = assistId;

            if (_icon != null)
            {
                _icon.sprite  = definition != null ? definition.icon : null;
                _icon.enabled = _icon.sprite != null;
            }
            if (_nameText != null) _nameText.text = definition != null ? definition.assistId : assistId;
            if (_roleText != null) _roleText.text = definition != null ? RoleLabel(definition.role) : string.Empty;
            if (_cooldownText != null)
                _cooldownText.text = cooldownRemaining > 0f ? $"{Mathf.CeilToInt(cooldownRemaining)}s" : string.Empty;

            if (_equippedMark != null) _equippedMark.SetActive(equipped && !replaceMode);
            if (_replaceCandidateMark != null) _replaceCandidateMark.SetActive(replaceMode);
        }

        public static string RoleLabel(BossAssistRole role) => role switch
        {
            BossAssistRole.Damage       => "공격",
            BossAssistRole.Break        => "브레이크",
            BossAssistRole.Defense      => "방어",
            BossAssistRole.Heal         => "회복",
            BossAssistRole.Buff         => "버프",
            BossAssistRole.Debuff       => "디버프",
            BossAssistRole.CrowdControl => "제어",
            _                           => role.ToString(),
        };
    }
}
