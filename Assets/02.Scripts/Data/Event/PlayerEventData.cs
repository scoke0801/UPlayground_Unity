using UPlayGround.Data.EnumType;

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

        // 이벤트 발송 성공과 실제 장착 성공을 구분하기 위한 처리 결과
        public bool handled;
        public bool succeeded;
        public string failReason;

        public void MarkHandled(bool success, string reason = null)
        {
            handled = true;
            succeeded = success;
            failReason = reason;
        }
    }

    public class PlayerInteractionEvent : IEventData
    {
        public int value;
    }
}
