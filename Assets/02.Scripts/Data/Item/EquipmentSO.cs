using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Item
{
    public enum EquipmentVisualMode
    {
        [Tooltip("EquipmentSO의 equipmentPrefab을 생성해 장착한다.")]
        Prefab = 0,

        [Tooltip("캐릭터 모델에 포함된 고유 무기 외형을 그대로 사용한다.")]
        CharacterBuiltIn = 1,
    }

    [System.Serializable]
    public struct EquipmentStatEntry
    {
        [Tooltip("런타임에서 사용하는 안정 Attribute ID")]
        [AttributeIdSelector]
        public string attributeId;
        public ModifierType modifierType;
        public float value;

        public AttributeId AttributeId => new(attributeId);
    }

    // 장비 전용 SO
    [CreateAssetMenu(fileName = "EquipmentSO", menuName = "UPlayGround/아이템/Equipment")]
    public class EquipmentSO : ItemSO
    {
        [Header("Equipment Data")]
        public EquipPosition equipSlot;

        public GameObject equipmentPrefab;

        public WeaponType weaponType = WeaponType.NoWeapon;

        [Tooltip("고유 무기처럼 캐릭터 모델에 외형과 보조 애니메이터가 포함된 경우 CharacterBuiltIn을 사용한다.")]
        public EquipmentVisualMode visualMode = EquipmentVisualMode.Prefab;

        [Header("Equipment Stats")]
        [SerializeField] private System.Collections.Generic.List<EquipmentStatEntry> _statModifiers = new();

        [Header("Random Growth Attributes")]
        [Tooltip("획득할 때마다 성장 능력치를 새로 추첨한다.")]
        public bool grantRandomGrowthAttributes = true;
        [Min(1)] public int randomAttributeCountMin = 1;
        [Min(1)] public int randomAttributeCountMax = 1;
        [Min(1)] public int randomRankMin = 1;
        [Min(1)] public int randomRankMax = 3;
        [Tooltip("비어 있으면 활성 캐릭터의 성장 규칙을 후보로 사용한다. 같은 능력치는 한 장비에 중복 추첨하지 않는다.")]
        public System.Collections.Generic.List<AttributeReference> randomAttributePool = new();

        public System.Collections.Generic.IReadOnlyList<EquipmentStatEntry> StatModifiers => _statModifiers;

        public bool HasUsableWeaponVisual =>
            visualMode == EquipmentVisualMode.CharacterBuiltIn || equipmentPrefab != null;

        public bool HasAnyStatModifier() => _statModifiers is { Count: > 0 };

        public void AddAttributeModifiersTo(
            System.Collections.Generic.List<AttributeModifierValue> target)
        {
            if (target == null) return;

            if (_statModifiers != null)
            {
                for (int i = 0; i < _statModifiers.Count; i++)
                {
                    EquipmentStatEntry entry = _statModifiers[i];
                    if (!entry.AttributeId.IsValid)
                    {
                        Debug.LogError(
                            $"[EquipmentSO] '{name}' 장비 Modifier {i}번의 Attribute ID가 비어 있습니다.",
                            this);
                        continue;
                    }
                    target.Add(new AttributeModifierValue(
                        entry.AttributeId,
                        ToAttributeOperation(entry.modifierType),
                        entry.value));
                }
            }

        }

        private static AttributeModifierOperation ToAttributeOperation(ModifierType type) =>
            type switch
            {
                ModifierType.Flat => AttributeModifierOperation.Add,
                ModifierType.Percent => AttributeModifierOperation.Percent,
                ModifierType.Multiply => AttributeModifierOperation.Multiply,
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null),
            };
    }
}
