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
}
