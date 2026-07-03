using UnityEngine;
using UPlayGround.Data.EnumType;

// 장비 전용 SO
[CreateAssetMenu(fileName = "EquipmentSO", menuName = "UPlayGround/아이템/Equipment")]
public class EquipmentSO : ItemSO
{
    [Header("Equipment Data")]
    public EquipPosition equipSlot;

    public GameObject equipmentPrefab;

    public WeaponType weaponType = WeaponType.NoWeapon;

    [Header("Equipment Stats")]
    [Tooltip("공격력")]              public float attackPower;
    [Tooltip("치명타 확률 (%). 예: 6.2 = 6.2%")] public float critChance;
    [Tooltip("치명타 피해 (%). 예: 115 = 115%")] public float critDamage = 100f;
    [Tooltip("공격 속도. 예: 1.05")] public float attackSpeed = 1f;
}
