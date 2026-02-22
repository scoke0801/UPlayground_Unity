using UnityEngine;
using UPlayGround.Data.EnumType;

// 장비 전용 SO
[CreateAssetMenu(fileName = "EquipmentSO", menuName = "UPlayGround/SO/EquipmentSO")]
public class EquipmentSO : ItemSO
{
    [Header("Equipment Data")]
    public EquipPosition equipSlot;
    
    public GameObject equipmentPrefab;

    public WeaponType weaponType = WeaponType.NoWeapon;
}
