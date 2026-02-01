using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Event
{
    public class PlayerEquipChangeEvent : IEventData
    {
        // 아이템 키
        public int itemKey;
        
        // 무기 라면 사용할 용도로 무기 키
        public string weaponKey;
        
        // 장착인가 or 해제인가
        public bool isEquip;

        // 장착 부위
        public EquipPosition equipPosition;
        
        // 무기라면 무기 타입은
        public WeaponType weaponType;
    }
    
}