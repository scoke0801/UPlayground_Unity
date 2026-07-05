using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

[System.Serializable]
public struct EquipmentStatEntry
{
    public StatType statType;
    public ModifierType modifierType;
    public float value;
}

// 장비 전용 SO
[CreateAssetMenu(fileName = "EquipmentSO", menuName = "UPlayGround/아이템/Equipment")]
public class EquipmentSO : ItemSO
{
    [Header("Equipment Data")]
    public EquipPosition equipSlot;

    public GameObject equipmentPrefab;

    public WeaponType weaponType = WeaponType.NoWeapon;

    [Header("Equipment Stats")]
    [SerializeField] private System.Collections.Generic.List<EquipmentStatEntry> _statModifiers = new();

    [Header("Legacy Equipment Stats")]
    [Tooltip("공격력")]              public float attackPower;
    [Tooltip("치명타 확률 (%). 예: 6.2 = 6.2%")] public float critChance;
    [Tooltip("치명타 피해 (%). 예: 115 = 115%")] public float critDamage = 100f;
    [Tooltip("공격 속도. 예: 1.05")] public float attackSpeed = 1f;

    public System.Collections.Generic.IReadOnlyList<EquipmentStatEntry> StatModifiers => _statModifiers;

    public bool HasAnyStatModifier()
    {
        if (_statModifiers != null && _statModifiers.Count > 0)
            return true;

        return !Mathf.Approximately(attackPower, 0f)
               || !Mathf.Approximately(critChance, 0f)
               || !Mathf.Approximately(critDamage, 100f);
    }

    public void AddStatModifiersTo(System.Collections.Generic.List<StatModifier> target, object source)
    {
        if (target == null) return;

        if (_statModifiers != null)
        {
            for (int i = 0; i < _statModifiers.Count; i++)
            {
                EquipmentStatEntry entry = _statModifiers[i];
                target.Add(new StatModifier(
                    entry.statType,
                    entry.modifierType,
                    entry.value,
                    source));
            }
        }

        if (_statModifiers == null || _statModifiers.Count == 0)
            AddLegacyStatModifiersTo(target, source);
    }

    private void AddLegacyStatModifiersTo(System.Collections.Generic.List<StatModifier> target, object source)
    {
        if (!Mathf.Approximately(attackPower, 0f))
        {
            target.Add(new StatModifier(
                StatType.AttackPower,
                ModifierType.Flat,
                attackPower,
                source));
        }

        if (!Mathf.Approximately(critChance, 0f))
        {
            target.Add(new StatModifier(
                StatType.CritRate,
                ModifierType.Flat,
                critChance * 0.01f,
                source));
        }

        if (!Mathf.Approximately(critDamage, 100f))
        {
            target.Add(new StatModifier(
                StatType.CritMultiplier,
                ModifierType.Flat,
                (critDamage - 100f) * 0.01f,
                source));
        }
    }
}
