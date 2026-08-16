using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;
using UPlayGround.Manager;
using UnityEngine.Serialization;

namespace UPlayGround.UI
{
    /// <summary>
    /// 동료(파티) 화면 우측 상세 패널.
    /// 선택된 캐릭터의 초상/등급/레벨·EXP/무기/전투력/HP/능력치/역할을 표시한다.
    /// (스킬·궁극기 아이콘은 아직 데이터 미연동 — 프리팹 플레이스홀더)
    /// </summary>
    public class UIPartyDetailPanel : MonoBehaviour
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

        [Header("속성")]
        [FormerlySerializedAs("_weightClassText")]
        [SerializeField] private TextMeshProUGUI _elementText;
        [FormerlySerializedAs("_weightDerivedText")]
        [SerializeField] private TextMeshProUGUI _elementDescriptionText;

        [Header("패시브")]
        [SerializeField] private GameObject[] _passiveSlots;

        [Header("설정")]
        [SerializeField] private int _maxLevel = 40;

        public void Clear()
        {
            if (_root != null) _root.SetActive(false);
        }

        public void Show(CharacterActorType type)
        {
            var pm   = UISvc.Party;
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
            var player = UISvc.Actors?.Player;
            float curHp = player != null ? player.GetHealthForCharacter(type) : 0f;
            float maxHp = player != null
                ? player.GetMaxHealthForCharacter(type)
                : Attribute(cp.Stats, global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth);
            if (_hpText != null) _hpText.text = $"{Mathf.RoundToInt(curHp):N0} / {Mathf.RoundToInt(maxHp):N0}";
            if (_hpFill != null) _hpFill.fillAmount = maxHp > 0f ? Mathf.Clamp01(curHp / maxHp) : 0f;

            // 능력치
            var s = cp.Stats;
            if (_statAttackText != null) _statAttackText.text = StatDisplayFormatter.FormatValue(global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, Attribute(s, global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower));
            if (_statDefenseText != null) _statDefenseText.text = StatDisplayFormatter.FormatValue(global::UPlayGround.Data.Stat.Attributes.Combat.Defense, Attribute(s, global::UPlayGround.Data.Stat.Attributes.Combat.Defense));
            if (_statHealthText != null) _statHealthText.text = StatDisplayFormatter.FormatValue(global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth, Attribute(s, global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth));
            if (_statCritRateText != null) _statCritRateText.text = StatDisplayFormatter.FormatValue(global::UPlayGround.Data.Stat.Attributes.Combat.CritRate, Attribute(s, global::UPlayGround.Data.Stat.Attributes.Combat.CritRate));
            if (_statCritDmgText != null) _statCritDmgText.text = StatDisplayFormatter.FormatValue(global::UPlayGround.Data.Stat.Attributes.Combat.CritMultiplier, Attribute(s, global::UPlayGround.Data.Stat.Attributes.Combat.CritMultiplier));
            if (_statAtkSpeedText != null) _statAtkSpeedText.text = StatDisplayFormatter.FormatValue(global::UPlayGround.Data.Stat.Attributes.Combat.AttackSpeed, Attribute(s, global::UPlayGround.Data.Stat.Attributes.Combat.AttackSpeed));

            // 역할 하이라이트
            var role = data.GetRole(type);
            if (_roleMelee != null)    _roleMelee.SetActive(role == PartyRole.Melee);
            if (_roleBalanced != null) _roleBalanced.SetActive(role == PartyRole.Balanced);
            if (_roleMobility != null) _roleMobility.SetActive(role == PartyRole.Mobility);

            RefreshElement(type, data);
            RefreshPassives(type);
        }

        private void RefreshElement(
            CharacterActorType type,
            PartyMemberDataSO data)
        {
            var element = data.GetCombatElement(type);
            if (_elementText != null)
            {
                _elementText.text = UICombatElementDisplay.Label(element);
                _elementText.color = UICombatElementDisplay.Color(element);
                TextMeshProUGUI title = _elementText.transform.parent != null
                    ? _elementText.transform.parent.Find("WeightTitle")
                        ?.GetComponent<TextMeshProUGUI>()
                    : null;
                if (title != null) title.text = "속성";
            }
            if (_elementDescriptionText != null)
                _elementDescriptionText.text =
                    element == CombatElement.None
                        ? "속성 상성에 따른 추가 효과 없음"
                        : "유리한 상성 공격 시 추가 피해";
        }

        private void RefreshPassives(CharacterActorType type)
        {
            EnsurePassiveSlots();
            CharacterPassiveSetSO set = Svc.Passives?.GetPassiveSet(type);
            for (int i = 0; i < _passiveSlots.Length; i++)
            {
                GameObject slot = _passiveSlots[i];
                PassiveAbilitySO passive =
                    set?.passives != null && i < set.passives.Count
                        ? set.passives[i]
                        : null;
                if (slot == null) continue;
                slot.SetActive(passive != null);
                if (passive == null) continue;

                TextMeshProUGUI label =
                    slot.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = passive.presentation?.displayName ?? passive.name;

                Transform iconTransform = FindDeepChild(slot.transform, "Icon");
                Image icon = iconTransform != null
                    ? iconTransform.GetComponent<Image>()
                    : null;
                if (icon != null)
                {
                    icon.sprite = passive.presentation?.icon;
                    icon.enabled = icon.sprite != null;
                }
            }
        }

        private void EnsurePassiveSlots()
        {
            if (_passiveSlots != null && _passiveSlots.Length > 0)
                return;

            Transform skills = _root != null
                ? FindDeepChild(_root.transform, "Skills")
                : null;
            if (skills == null)
            {
                _passiveSlots = System.Array.Empty<GameObject>();
                return;
            }

            Transform title = FindDeepChild(skills, "SkillTitle");
            TextMeshProUGUI titleText =
                title != null ? title.GetComponent<TextMeshProUGUI>() : null;
            if (titleText != null) titleText.text = "패시브";

            var slots = new List<GameObject>();
            for (int i = 1; i <= 4; i++)
            {
                Transform slot = skills.Find($"Skill{i}");
                if (slot != null) slots.Add(slot.gameObject);
            }
            _passiveSlots = slots.ToArray();
        }

        private static Transform FindDeepChild(Transform root, string objectName)
        {
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == objectName) return child;
                Transform nested = FindDeepChild(child, objectName);
                if (nested != null) return nested;
            }
            return null;
        }

        private static float Attribute(
            IReadOnlyDictionary<AttributeId, float> attributes,
            AttributeId attributeId)
            => attributes != null
               && attributes.TryGetValue(attributeId, out float value)
                ? value
                : UPlayGroundAttributeDefaults.Get(attributeId);
    }
}
