namespace UPlayGround.Data.EnumType
{
    public enum WeaponType
    {
        NoWeapon = 0,
        Sword = 1,
        SwordShield = 2,
        GreatSword = 3,
        Staff = 4,
        Bow = 5,
        Arrow = 6,
        
        Katana = 10,
        DoubleAxe = 11,
        Whip = 12,
        Spear = 13,
        DualBlade = 14,
        
    }

    public enum EquipPosition
    {
        None = 0,
        LeftHand,
        RightHand,
        
        Head,
        Chest,
        Pants,
        Shoes,
        Gloves
    }

    public enum EquipArmorType
    {
        None = 0,

        Head,
        Chest,
        Arm,
        Waist,
        Leg
    }

    public static class WeaponCompatibility
    {
        /// <summary>
        /// 캐릭터 고유 주무기 타입이 해당 주 무기 아이템 타입을 받아들일 수 있는지.
        /// 동일 타입은 항상 허용하고, 검+방패·쌍검처럼 검을 기반으로 하는 무기는
        /// 기본 검(Sword) 아이템도 주 무기로 장착할 수 있다. (비대칭: 검 캐릭터는 검만.)
        /// </summary>
        public static bool AcceptsMainWeaponItem(this WeaponType characterMainType, WeaponType itemType)
        {
            if (itemType == WeaponType.NoWeapon)
                return false;

            if (characterMainType == itemType)
                return true;

            // 검 기반 무기(검+방패, 쌍검)는 기본 검 아이템을 베이스로 받아들인다.
            if (itemType == WeaponType.Sword)
                return characterMainType == WeaponType.SwordShield ||
                       characterMainType == WeaponType.DualBlade;

            return false;
        }

        /// <summary>
        /// 캐릭터 고유 주무기 타입이 해당 보조 무기(왼손) 아이템 타입을 받아들일 수 있는지.
        /// 검+방패는 방패(SwordShield)를, 쌍검은 두 번째 검(Sword)을 보조로 장착한다.
        /// 그 외 무기 타입은 보조 무기 슬롯을 쓰지 않는다.
        /// </summary>
        public static bool AcceptsSubWeaponItem(this WeaponType characterMainType, WeaponType itemType)
        {
            if (itemType == WeaponType.NoWeapon)
                return false;

            return characterMainType switch
            {
                WeaponType.SwordShield => itemType == WeaponType.SwordShield, // 방패
                WeaponType.DualBlade   => itemType == WeaponType.Sword,       // 두 번째 검
                _                      => false,
            };
        }
    }

    public static class EquipDisplayExtensions
    {
        /// <summary> 무기 종류 한글 표기. </summary>
        public static string ToDisplayString(this WeaponType type)
        {
            return type switch
            {
                WeaponType.Sword       => "검",
                WeaponType.SwordShield => "검+방패",
                WeaponType.GreatSword  => "대검",
                WeaponType.Staff       => "지팡이",
                WeaponType.Bow         => "활",
                WeaponType.Arrow       => "화살",
                WeaponType.Katana      => "카타나",
                WeaponType.DoubleAxe   => "쌍도끼",
                WeaponType.Whip        => "채찍",
                WeaponType.Spear       => "창",
                WeaponType.DualBlade   => "쌍검",
                _                      => string.Empty
            };
        }

        /// <summary> 장착 부위 한글 표기. 무기 슬롯이면 무기 종류를 괄호로 덧붙인다. </summary>
        public static string ToDisplayString(this EquipPosition slot, WeaponType weaponType = WeaponType.NoWeapon)
        {
            string baseName = slot switch
            {
                EquipPosition.RightHand => "주 무기",
                EquipPosition.LeftHand  => "보조 무기",
                EquipPosition.Head      => "머리",
                EquipPosition.Chest     => "상의",
                EquipPosition.Pants     => "하의",
                EquipPosition.Shoes     => "신발",
                EquipPosition.Gloves    => "장갑",
                _                       => "없음"
            };

            bool isWeaponSlot = slot == EquipPosition.RightHand || slot == EquipPosition.LeftHand;
            if (isWeaponSlot && weaponType != WeaponType.NoWeapon)
                return $"{baseName} ({weaponType.ToDisplayString()})";

            return baseName;
        }
    }
}
