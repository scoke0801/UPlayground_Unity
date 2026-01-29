using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Event
{
    public class PlayerEquipChangeEvent : IEventData
    {
        public WeaponType weaponType;
        public string weaponKey;
        public bool isEquip;
        
        public EquipPosition equipPosition;
    }
}