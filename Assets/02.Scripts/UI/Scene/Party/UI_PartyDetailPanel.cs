using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 동료(파티) 화면 우측 상세 패널.
    /// 선택된 캐릭터의 초상/등급/레벨·EXP/무기/전투력/HP/능력치/역할을 표시한다.
    /// (스킬·궁극기 아이콘은 아직 데이터 미연동 — 프리팹 플레이스홀더)
    /// </summary>
    public class UI_PartyDetailPanel : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("헤더")]
        [SerializeField] private Image           _portrait;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _starsText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Image           _expFill;
        [SerializeField] private TextMeshProUGUI _expText;

        [Header("무기 / 전투력 / HP")]
        [SerializeField] private Image           _weaponIcon;
        [SerializeField] private TextMeshProUGUI _weaponNameText;
        [SerializeField] private TextMeshProUGUI _combatPowerText;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private Image           _hpFill;

        [Header("능력치")]
        [SerializeField] private TextMeshProUGUI _statAttackText;
        [SerializeField] private TextMeshProUGUI _statDefenseText;
        [SerializeField] private TextMeshProUGUI _statHealthText;
        [SerializeField] private TextMeshProUGUI _statCritRateText;
        [SerializeField] private TextMeshProUGUI _statCritDmgText;
        [SerializeField] private TextMeshProUGUI _statAtkSpeedText;

        [Header("역할 하이라이트")]
        [SerializeField] private GameObject _roleMelee;
        [SerializeField] private GameObject _roleBalanced;
        [SerializeField] private GameObject _roleMobility;

        [Header("설정")]
        [SerializeField] private int _maxLevel = 40;

        public void Clear()
        {
            if (_root != null) _root.SetActive(false);
        }

        public void Show(CharacterActorType type)
        {
            var pm   = PartyManager.Instance;
            var data = pm?.PartyMemberDataSO;
            if (pm == null || data == null || type == CharacterActorType.None)
            {
                Clear();
                return;
            }

            if (_root != null) _root.SetActive(true);

            // 초상 / 이름 / 등급
            if (_portrait != null)
            {
                _portrait.sprite  = data.GetFullBodySprite(type);
                _portrait.enabled = _portrait.sprite != null;
            }
            if (_nameText != null)  _nameText.text  = data.GetName(type);
            if (_starsText != null) _starsText.text = new string('★', data.GetRarity(type));

            // 레벨 / EXP
            int  level = pm.GetLevel(type);
            long exp   = pm.GetExp(type);
            long req   = pm.GetRequiredExp(type);
            if (_levelText != null) _levelText.text = $"Lv.{level} / {_maxLevel}";
            if (_expFill != null)   _expFill.fillAmount = req > 0 ? Mathf.Clamp01((float)exp / req) : 1f;
            if (_expText != null)   _expText.text = $"{exp:N0} / {req:N0}";

            // 무기
            if (_weaponIcon != null)     _weaponIcon.sprite = data.GetWeaponIcon(type);
            if (_weaponNameText != null) _weaponNameText.text = data.GetWeaponName(type);

            // 전투력
            var cp = pm.GetEffectiveCombatPower(type);
            if (_combatPowerText != null) _combatPowerText.text = cp.CombatPower.ToString("N0");

            // HP (현재/최대) — 액티브/벤치 공통 조회
            var player = GameObjectManager.Instance?.Player;
            float curHp = player != null ? player.GetHealthForCharacter(type) : 0f;
            float maxHp = player != null ? player.GetMaxHealthForCharacter(type) : Stat(cp.GrowthStats, StatType.MaxHealth);
            if (_hpText != null) _hpText.text = $"{Mathf.RoundToInt(curHp):N0} / {Mathf.RoundToInt(maxHp):N0}";
            if (_hpFill != null) _hpFill.fillAmount = maxHp > 0f ? Mathf.Clamp01(curHp / maxHp) : 0f;

            // 능력치
            var s = cp.GrowthStats;
            if (_statAttackText != null)   _statAttackText.text   = StatDisplayFormatter.FormatValue(StatType.AttackPower, Stat(s, StatType.AttackPower));
            if (_statDefenseText != null)  _statDefenseText.text  = StatDisplayFormatter.FormatValue(StatType.Defense, Stat(s, StatType.Defense));
            if (_statHealthText != null)   _statHealthText.text   = StatDisplayFormatter.FormatValue(StatType.MaxHealth, Stat(s, StatType.MaxHealth));
            if (_statCritRateText != null) _statCritRateText.text = StatDisplayFormatter.FormatValue(StatType.CritRate, Stat(s, StatType.CritRate));
            if (_statCritDmgText != null)  _statCritDmgText.text  = StatDisplayFormatter.FormatValue(StatType.CritMultiplier, Stat(s, StatType.CritMultiplier));
            if (_statAtkSpeedText != null) _statAtkSpeedText.text = "-"; // 공격 속도 스탯 미정의

            // 역할 하이라이트
            var role = data.GetRole(type);
            if (_roleMelee != null)    _roleMelee.SetActive(role == PartyRole.Melee);
            if (_roleBalanced != null) _roleBalanced.SetActive(role == PartyRole.Balanced);
            if (_roleMobility != null) _roleMobility.SetActive(role == PartyRole.Mobility);
        }

        private static float Stat(IReadOnlyDictionary<StatType, float> stats, StatType type)
            => stats != null && stats.TryGetValue(type, out var v) ? v : 0f;
    }
}
