using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Event
{
    public class PlayerEquipChangeEvent : IEventData
    {
        // 아이템 키
        public int itemKey;
        
        // 장착인가 or 해제인가
        public bool isEquip;

        // 장착 부위
        public EquipPosition equipPosition;
        
        // 무기라면 무기 타입은
        public WeaponType weaponType;
    }

    public class PlayerInteractionEvent : IEventData
    {
        public int value;
    }
}